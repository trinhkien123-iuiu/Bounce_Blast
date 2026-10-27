using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>Điều phối bàn chơi: dựng màn, cho khối mất chỗ đỡ rơi xuống, xử lý bi trúng / khối vỡ, báo thắng khi bàn sạch</summary>
    public class BoardController : MonoBehaviour
    {
        public event Action OnBoardCleared;
        public event Action<BoardBlock> OnBlockDestroyed;

        GameConfig _config;
        ColorLibrary _colors;
        BoardGrid _grid;
        BoardTargeting _targeting;
        readonly List<BoardBlock> _blocks = new List<BoardBlock>();
        readonly BlockVisualFactory _visuals = new BlockVisualFactory();
        readonly BlockBreakFx _breakFx = new BlockBreakFx();
        BoardTunnels _tunnels;
        bool _settleRequested;
        bool _cleared;

        public float CellSize => _grid != null ? _grid.Cell : 0f;
        public int DestructibleCount { get; private set; }
        /// <summary>Hệ số nhân của level: một ô CSV = Multiplier×Multiplier ô mịn (rèm dựng theo ô CSV)</summary>
        public int Multiplier { get; private set; } = 1;
        public IReadOnlyList<BoardBlock> Blocks => _blocks;

        public GameConfig Config => _config;
        public ColorLibrary Colors => _colors;
        public BlockVisualFactory Visuals => _visuals;
        public BlockBreakFx BreakFx => _breakFx;

        /// <summary>Xử lý dựng bàn chơi theo level</summary>
        public void Build(LevelModel level, GameConfig config, ColorLibrary colors)
        {
            _config = config;
            _colors = colors;
            Multiplier = Mathf.Max(1, level.Multiplier);
            float cell = config.arena.width / level.BoardCols;
            var origin = new Vector2(config.arena.xMin, config.arena.yMin + config.boardOffsetY);
            _grid = new BoardGrid(level.BoardCols, level.BoardRows, origin, cell);
            _targeting = new BoardTargeting(_grid, _blocks);
            _visuals.Setup(config, colors, cell);
            _breakFx.Setup(config, colors, _grid);
            _cleared = false;
            _tunnels = new BoardTunnels(this, _grid, config);

            foreach (BoardItemData item in level.BoardItems) Spawn(item);
            _tunnels.Init();
            RecountDestructibles();
            _settleRequested = true;
        }

        /// <summary>Dùng cho đổi ô lưới sang toạ độ thế giới</summary>
        public Vector2 CellToWorld(RectInt r) => _grid.CellToWorld(r);

        /// <summary>Xử lý tạo một khối từ dữ liệu</summary>
        public BoardBlock Spawn(BoardItemData item)
        {
            RectInt r = item.Cells;
            if (!_grid.Contains(r))
            {
                Debug.LogWarning($"[Board] Bỏ qua vật nằm ngoài lưới: {item.Kind} {r}");
                return null;
            }

            var go = new GameObject($"{item.Kind}_{item.ColorCode}_{r.x}_{r.y}");
            go.transform.SetParent(transform, false);
            go.layer = gameObject.layer;

            Vector2 size = new Vector2(r.width, r.height) * _grid.Cell;
            List<RectInt> parts = item.Mask != null ? BlockVisualFactory.Decompose(item.Mask, r.width, r.height) : null;
            BlockKind kind = BlockKinds.Of(item.Kind);
            BlockView view = kind.Build(this, item, go.transform, size, parts);
            if (kind.ShearMesh) _visuals.ApplyViewShear(view.visual);

            var block = go.AddComponent<BoardBlock>();
            block.Init(item, view.visual, view.label, _grid.CellToWorld(r), size, view.colliders);
            if (item.Kind == BoardItemKind.BoardTunnel && _tunnels != null)
                _tunnels.Register(block, item, view.visual != null ? view.visual.GetComponent<BoardTunnelView>() : null);
            if (item.Hidden)
            {
                Material cover = r.width > 1 || r.height > 1 ? _config.qmarkBoardBigMaterial : _config.qmarkBoardMaterial;
                block.SetHidden(QMarkCover.Attach(view.visual, cover, _config.qmarkBackground, _config.qmarkGlossiness, go.layer, _config.qmarkDissolveSeconds, item.Mask == null));
            }
            _blocks.Add(block);
            _grid.Occupy(block, true);
            return block;
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
                    while (_grid.CanFall(b))
                    {
                        _grid.Occupy(b, false);
                        RectInt r = b.Cells;
                        r.y -= 1;
                        b.Cells = r;
                        _grid.Occupy(b, true);
                        moved = true;
                    }
                    b.SetTarget(_grid.CellToWorld(b.Cells));
                }
            }
        }

        void Update()
        {
            if (_tunnels != null && _tunnels.Any && !_cleared) _tunnels.Tick(Time.time);
        }

        void LateUpdate()
        {
            if (!_settleRequested) return;
            _settleRequested = false;
            Settle();
            RevealUncovered();
        }

        /// <summary>Yêu cầu cho khối rơi/dồn lại ở cuối khung hình (khi có khối vừa được thả khỏi tunnel…)</summary>
        public void RequestSettle() => _settleRequested = true;

        /// <summary>Board Qmark: khối nào đã hở mặt trên / hai bên thì dấu "?" tan biến, lộ màu thật</summary>
        void RevealUncovered()
        {
            foreach (BoardBlock b in _blocks.ToArray())
                if (b != null && b.Hidden && !b.IsDestroyed && _grid.IsUncovered(b)) b.Reveal();
        }

        /// <summary>Bi màu color chạm khối. Trả về true nếu bi bị tiêu hao</summary>
        public bool HandleBallHit(BoardBlock block, string color)
        {
            if (block == null || !block.Accepts(color)) return false;
            if (block.TakeHit()) DestroyBlock(block);
            return !block.IsFreeHit;
        }

        /// <summary>Xử lý phá một khối và hiệu ứng</summary>
        public void DestroyBlock(BoardBlock block)
        {
            if (block == null) return;
            block.ForceDestroy();
            _grid.Occupy(block, false);
            _blocks.Remove(block);

            block.Rules.OnBroken(this, block);

            Destroy(block.gameObject);
            OnBlockDestroyed?.Invoke(block);
            TickCurtains(block);
            RecountDestructibles();
            _settleRequested = true;

            if (DestructibleCount == 0 && !_cleared)
            {
                _cleared = true;
                OnBoardCleared?.Invoke();
            }
        }

        /// <summary>Gạch 1 máu vừa vỡ: mỗi rèm cùng màu nhận một mảnh gạch bay lên con lắc (CurtainPiece)</summary>
        void TickCurtains(BoardBlock broken)
        {
            if (broken.Kind != BoardItemKind.Cube || broken.MaxHp != 1) return;
            Vector3 from = broken.transform.position;
            Material mat = _colors.CubeMat(broken.ColorCode);
            Color tint = mat != null && mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : Color.white;
            foreach (BoardBlock b in _blocks.ToArray())
            {
                if (b.Kind != BoardItemKind.Curtain || b.IsDestroyed) continue;
                if (!string.Equals(b.ColorCode, broken.ColorCode, StringComparison.OrdinalIgnoreCase)) continue;
                CurtainPiece.Launch(this, b, b.GetComponentInChildren<CurtainView>(), from, mat, tint, _grid.Cell * 0.8f, gameObject.layer);
            }
        }

        /// <summary>Bom nổ (hết giờ): mọi khối vỡ nát lần lượt từ gần tâm nổ ra xa trong duration giây. CHỈ là hiệu ứng hình (không phá khối theo luật, không tính thắng); tường giữ nguyên</summary>
        public void ShatterAll(Vector3 origin, float duration)
        {
            StartCoroutine(ShatterRoutine(origin, Mathf.Max(0.1f, duration)));
        }

        IEnumerator ShatterRoutine(Vector3 origin, float duration)
        {
            var list = new List<BoardBlock>();
            foreach (BoardBlock b in _blocks)
                if (b != null && !b.IsDestroyed && b.Kind != BoardItemKind.Wall && b.Kind != BoardItemKind.Bomb) list.Add(b);
            list.Sort((a, c) => ((Vector2)(a.transform.position - origin)).sqrMagnitude.CompareTo(((Vector2)(c.transform.position - origin)).sqrMagnitude));
            float start = Time.unscaledTime;
            int i = 0;
            while (i < list.Count)
            {
                float k = Mathf.Clamp01((Time.unscaledTime - start) / duration);
                int upto = k >= 1f ? list.Count : Mathf.CeilToInt(k * list.Count);
                for (; i < upto; i++)
                {
                    BoardBlock b = list[i];
                    if (b == null || b.IsDestroyed) continue;
                    if (!string.IsNullOrEmpty(b.ColorCode)) _breakFx.SpawnJellyBreak(b, false);
                    b.gameObject.SetActive(false);
                }
                yield return null;
            }
        }

        /// <summary>Mảnh gạch chạm con lắc: số giảm 1, về 0 thì rèm mở</summary>
        public void CurtainPieceArrived(BoardBlock curtain)
        {
            if (curtain == null || curtain.IsDestroyed) return;
            if (curtain.TakeHit()) DestroyBlock(curtain);
        }

        void RecountDestructibles()
        {
            int n = 0;
            foreach (BoardBlock b in _blocks) if (b.IsDestructible && !b.IsDestroyed) n++;
            DestructibleCount = n;
        }

        /// <summary>Kiểm tra còn khối cùng màu để bắn</summary>
        public bool HasTargetFor(string color) => _targeting != null && _targeting.HasTargetFor(color);

        /// <summary>Dùng cho tìm khối cùng màu gần nhất</summary>
        public bool TryGetNearestTarget(string color, Vector2 from, out Vector2 point)
        {
            point = default;
            return _targeting != null && _targeting.TryGetNearestTarget(color, from, out point);
        }

        /// <summary>Dùng cho tìm khối cùng màu nhìn thấy được</summary>
        public bool TryGetLineOfSightTarget(string color, Vector2 from, float radius, int mask, out Vector2 point)
        {
            point = default;
            return _targeting != null && _targeting.TryGetLineOfSightTarget(color, from, radius, mask, out point);
        }

        /// <summary>Booster búa: khối tại điểm chạm</summary>
        public BoardBlock BlockAt(Vector2 world)
        {
            Collider2D hit = Physics2D.OverlapPoint(world);
            return hit != null ? hit.GetComponent<BoardBlock>() : null;
        }

        /// <summary>Xử lý xoá toàn bộ bàn chơi</summary>
        public void ClearAll()
        {
            foreach (BoardBlock b in _blocks) if (b != null) { b.gameObject.SetActive(false); Destroy(b.gameObject); }
            _blocks.Clear();
        }
    }
}
