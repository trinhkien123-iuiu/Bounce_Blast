using System;
using System.Collections.Generic;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>
    /// Điều phối bàn chơi: dựng màn, cho khối mất chỗ đỡ rơi xuống, xử lý bi trúng / khối vỡ, báo thắng khi bàn sạch.
    /// Việc chi tiết giao cho các lớp riêng:
    /// <see cref="BoardGrid"/> (ô nào có khối nào) · <see cref="BoardTargeting"/> (bi ngắm khối nào) ·
    /// <see cref="BlockVisualFactory"/> (dựng hình khối) · <see cref="BlockBreakFx"/> (hiệu ứng vỡ) ·
    /// <c>Board/Kinds</c> (luật + cách dựng + cách vỡ riêng của từng loại khối).
    /// </summary>
    public class BoardController : MonoBehaviour
    {
        public event Action OnBoardCleared;
        public event Action<BoardBlock> OnBlockDestroyed;

        GameConfig _config;
        ColorLibrary _colors;
        BoardGrid _grid;
        BoardTargeting _targeting;
        readonly List<BoardBlock> _blocks = new List<BoardBlock>();
        readonly BlockVisualFactory _visuals = new BlockVisualFactory();   // giữ qua các màn để dùng lại cache mesh
        readonly BlockBreakFx _breakFx = new BlockBreakFx();
        bool _settleRequested;
        bool _cleared;

        public float CellSize => _grid != null ? _grid.Cell : 0f;
        public int DestructibleCount { get; private set; }
        public IReadOnlyList<BoardBlock> Blocks => _blocks;

        // Dành cho các lớp loại khối (Board/Kinds)
        public GameConfig Config => _config;
        public ColorLibrary Colors => _colors;
        public BlockVisualFactory Visuals => _visuals;
        public BlockBreakFx BreakFx => _breakFx;

        public void Build(LevelModel level, GameConfig config, ColorLibrary colors)
        {
            _config = config;
            _colors = colors;
            float cell = config.arena.width / level.BoardCols;
            var origin = new Vector2(config.arena.xMin, config.arena.yMin + config.boardOffsetY); // nâng lên khỏi gờ sàn của khung
            _grid = new BoardGrid(level.BoardCols, level.BoardRows, origin, cell);
            _targeting = new BoardTargeting(_grid, _blocks);
            _visuals.Setup(config, colors, cell);
            _breakFx.Setup(config, colors, _grid);
            _cleared = false;

            foreach (BoardItemData item in level.BoardItems) Spawn(item);
            RecountDestructibles();
            _settleRequested = true;
        }

        public Vector2 CellToWorld(RectInt r) => _grid.CellToWorld(r);

        // ------------------------------------------------------------------ Dựng khối
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
            BlockView view = kind.Build(this, item, go.transform, size, parts);   // mỗi loại khối tự dựng hình (Board/Kinds)
            if (kind.ShearMesh) _visuals.ApplyViewShear(view.visual);

            var block = go.AddComponent<BoardBlock>();
            block.Init(item, view.visual, view.label, _grid.CellToWorld(r), size, view.colliders);
            _blocks.Add(block);
            _grid.Occupy(block, true);
            return block;
        }

        // ------------------------------------------------------------------ Rơi
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

        void LateUpdate()
        {
            if (!_settleRequested) return;
            _settleRequested = false;
            Settle();
        }

        // ------------------------------------------------------------------ Va chạm
        /// <summary>Bi màu <paramref name="color"/> chạm khối. Trả về true nếu bi bị tiêu hao (đã trừ máu).</summary>
        public bool HandleBallHit(BoardBlock block, string color)
        {
            if (block == null || !block.Accepts(color)) return false;
            if (block.TakeHit()) DestroyBlock(block);
            // Hòm gỗ / cột nhà: ngân sách bi của level chỉ đủ cho cube (tổng bi == tổng HP cube) → bi trừ máu rồi nảy tiếp, không bị tiêu hao
            return !block.IsFreeHit;
        }

        public void DestroyBlock(BoardBlock block)
        {
            if (block == null) return;
            block.ForceDestroy();
            _grid.Occupy(block, false);
            _blocks.Remove(block);

            block.Rules.OnBroken(this, block);   // hiệu ứng / hệ quả riêng của từng loại khối (Board/Kinds)

            Destroy(block.gameObject);
            OnBlockDestroyed?.Invoke(block);
            RecountDestructibles();
            _settleRequested = true;

            if (DestructibleCount == 0 && !_cleared)
            {
                _cleared = true;
                OnBoardCleared?.Invoke();
            }
        }

        void RecountDestructibles()
        {
            int n = 0;
            foreach (BoardBlock b in _blocks) if (b.IsDestructible && !b.IsDestroyed) n++;
            DestructibleCount = n;
        }

        // ------------------------------------------------------------------ Truy vấn (chuyển cho BoardTargeting)
        public bool HasTargetFor(string color) => _targeting != null && _targeting.HasTargetFor(color);

        public bool TryGetNearestTarget(string color, Vector2 from, out Vector2 point)
        {
            point = default;
            return _targeting != null && _targeting.TryGetNearestTarget(color, from, out point);
        }

        public bool TryGetLineOfSightTarget(string color, Vector2 from, float radius, int mask, out Vector2 point)
        {
            point = default;
            return _targeting != null && _targeting.TryGetLineOfSightTarget(color, from, radius, mask, out point);
        }

        /// <summary>Booster búa: khối tại điểm chạm.</summary>
        public BoardBlock BlockAt(Vector2 world)
        {
            Collider2D hit = Physics2D.OverlapPoint(world);
            return hit != null ? hit.GetComponent<BoardBlock>() : null;
        }

        public void ClearAll()
        {
            // tắt ngay (Destroy chỉ chạy cuối frame) → nạp lại màn trong cùng frame không bị chồng 2 bàn
            foreach (BoardBlock b in _blocks) if (b != null) { b.gameObject.SetActive(false); Destroy(b.gameObject); }
            _blocks.Clear();
        }
    }
}
