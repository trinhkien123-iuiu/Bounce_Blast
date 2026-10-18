using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>
    /// Dựng và quản lý bàn chơi: lưới chiếm chỗ, trọng lực (khối mất chỗ đỡ thì rơi xuống),
    /// xử lý bi trúng khối, phá hòm lộ vật bên trong, báo thắng khi bàn sạch.
    /// Mã chia nhiều file (partial): BoardController.cs (dựng khối + lưới), .CombinedMesh.cs, .Visuals.cs, .Hits.cs, .Queries.cs.
    /// </summary>
    public partial class BoardController : MonoBehaviour
    {
        public event Action OnBoardCleared;
        public event Action<BoardBlock> OnBlockDestroyed;

        GameConfig _config;
        ColorLibrary _colors;
        LevelModel _level;
        float _cell;
        Vector2 _origin;
        BoardBlock[,] _grid;
        readonly List<BoardBlock> _blocks = new List<BoardBlock>();
        bool _settleRequested;
        bool _cleared;
        TMP_FontAsset _font;
        Material _fontMaterial;
        Material _labelMaterial;

        public float CellSize => _cell;
        public int DestructibleCount { get; private set; }
        public IReadOnlyList<BoardBlock> Blocks => _blocks;

        public void Build(LevelModel level, GameConfig config, ColorLibrary colors)
        {
            _config = config;
            _colors = colors;
            _level = level;
            _cell = config.arena.width / level.BoardCols;
            _origin = new Vector2(config.arena.xMin, config.arena.yMin + config.boardOffsetY); // nâng lên khỏi gờ sàn của khung
            _grid = new BoardBlock[level.BoardCols, Mathf.Max(level.BoardRows, 64)];
            _cleared = false;

            // TMP trong prefab rip bị mất script → dùng font Fredoka gốc
            _font = config.worldFont;
            _fontMaterial = _font != null ? _font.material : null;

            foreach (BoardItemData item in level.BoardItems) Spawn(item);
            RecountDestructibles();
            _settleRequested = true;
        }

        public Vector2 CellToWorld(RectInt r) =>
            _origin + new Vector2((r.x + r.width * 0.5f) * _cell, (r.y + r.height * 0.5f) * _cell);

        // ------------------------------------------------------------------ Dựng khối
        internal BoardBlock Spawn(BoardItemData item)
        {
            RectInt r = item.Cells;
            if (r.xMin < 0 || r.xMax > _grid.GetLength(0) || r.yMin < 0 || r.yMax > _grid.GetLength(1))
            {
                Debug.LogWarning($"[Board] Bỏ qua vật nằm ngoài lưới: {item.Kind} {r}");
                return null;
            }

            var go = new GameObject($"{item.Kind}_{item.ColorCode}_{r.x}_{r.y}");
            go.transform.SetParent(transform, false);
            go.layer = gameObject.layer;

            Vector2 size = new Vector2(r.width, r.height) * _cell;
            List<RectInt> parts = item.Mask != null ? Decompose(item.Mask, r.width, r.height) : null;
            BlockKind kind = BlockKinds.Of(item.Kind);
            BlockView view = kind.Build(this, item, go.transform, size, parts);   // mỗi loại khối tự dựng hình (Board/Kinds)

            // Khối nhiều ô / tường: xiên mesh thay vì xoay (xem ShearForView) → mặt trước phẳng khít lưới như gạch 1 ô
            if (kind.ShearMesh && view.visual != null)
                foreach (MeshFilter smf in view.visual.GetComponentsInChildren<MeshFilter>(true))
                    if (smf.sharedMesh != null && smf.sharedMesh.isReadable && smf.GetComponent<TextMeshPro>() == null && !smf.sharedMesh.name.StartsWith("Solid"))
                        smf.sharedMesh = ShearForView(smf.sharedMesh);

            var block = go.AddComponent<BoardBlock>();
            block.Init(item, view.visual, view.label, CellToWorld(r), size, view.colliders);
            _blocks.Add(block);
            Occupy(block, true);
            return block;
        }

        internal GameConfig Config => _config;
        internal ColorLibrary Colors => _colors;

        /// <summary>
        /// Đường dựng mặc định cho khối theo prefab cube: khối chữ nhật → <see cref="CreateVisual"/> (gạch 1 ô / 9-slice);
        /// khối có hình dạng (chữ L…) → một mesh liền theo viền + collider từng hình chữ nhật con, nhãn số đặt ở phần lớn nhất.
        /// </summary>
        internal BlockView BuildDefaultView(BoardItemData item, Transform parent, Vector2 size, List<RectInt> parts, bool withLabel)
        {
            if (parts == null || parts.Count <= 1)
                return new BlockView { visual = CreateVisual(parent, item, size), label = withLabel ? CreateLabel(parent, item, size) : null };

            // Khối hình chữ L…: một mesh LIỀN theo đường viền (không ghép mảnh → không lộ bậc/đường nối);
            // collider là các hình chữ nhật con; nhãn số đặt giữa phần chữ nhật lớn nhất.
            RectInt r = item.Cells;
            var colliders = new List<Rect>();
            RectInt biggest = parts[0];
            foreach (RectInt p in parts)
            {
                Vector2 ps = new Vector2(p.width, p.height) * _cell;
                Vector2 off = new Vector2((p.x + p.width * 0.5f - r.width * 0.5f) * _cell, (p.y + p.height * 0.5f - r.height * 0.5f) * _cell);
                colliders.Add(new Rect(off - ps * 0.5f, ps));
                if (p.width * p.height > biggest.width * biggest.height) biggest = p;
            }
            Transform visual = CreateSolidVisual(parent, item, r, new HashSet<Vector2Int>(item.Mask));
            TextMeshPro label = withLabel ? CreateLabel(parent, item, new Vector2(biggest.width, biggest.height) * _cell) : null;
            if (label != null)
                label.transform.localPosition += new Vector3((biggest.x + biggest.width * 0.5f - r.width * 0.5f) * _cell,
                                                             (biggest.y + biggest.height * 0.5f - r.height * 0.5f) * _cell, 0f);
            return new BlockView { visual = visual, label = label, colliders = colliders };
        }

        readonly Dictionary<int, Mesh> _joinedMeshes = new Dictionary<int, Mesh>();
        readonly Dictionary<Mesh, Mesh> _shearedMeshes = new Dictionary<Mesh, Mesh>();

        // ------------------------------------------------------------------ Lưới
        void Occupy(BoardBlock b, bool on)
        {
            _gridVersion++;   // lưới đổi → bộ nhớ đệm "khối lộ ra" của LOS hết hạn
            foreach (Vector2Int c in b.OccupiedCells())
                _grid[c.x, c.y] = on ? b : (_grid[c.x, c.y] == b ? null : _grid[c.x, c.y]);
        }

        bool CanFall(BoardBlock b)
        {
            if (b.IsStatic) return false;
            foreach (Vector2Int c in b.OccupiedCells())
            {
                if (c.y == 0) return false;
                BoardBlock below = _grid[c.x, c.y - 1];
                if (below != null && below != b) return false;
            }
            return true;
        }

        void Settle()
        {
            _blocks.Sort((a, c) => a.Cells.yMin.CompareTo(c.Cells.yMin));
            bool moved = true;
            while (moved)
            {
                moved = false;
                foreach (BoardBlock b in _blocks)
                {
                    if (b.IsDestroyed) continue;
                    while (CanFall(b))
                    {
                        Occupy(b, false);
                        RectInt r = b.Cells;
                        r.y -= 1;
                        b.Cells = r;
                        Occupy(b, true);
                        moved = true;
                    }
                    b.SetTarget(CellToWorld(b.Cells));
                }
            }
        }

        void LateUpdate()
        {
            if (!_settleRequested) return;
            _settleRequested = false;
            Settle();
        }
    }
}
