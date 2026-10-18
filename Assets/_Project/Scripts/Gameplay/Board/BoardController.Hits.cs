using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    // Một phần của BoardController (partial) – xem BoardController.cs.
    /// <summary>Va chạm: bi trúng khối, phá khối/hòm/cột, đếm khối còn lại.</summary>
    public partial class BoardController
    {
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
            Occupy(block, false);
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

        /// <summary>Vỡ kiểu thạch cùng màu khối. <paramref name="perCell"/>: khối lớn nổ mỗi ô một cụm (tối đa MaxBigBreakBursts) thay vì phóng to một hiệu ứng.</summary>
        internal void SpawnJellyBreak(BoardBlock block, bool perCell)
        {
            EnsureBreakTune();
            Color tint = _colors.ColorOf(block.ColorCode);
            Material mat = _colors.CubeMat(block.ColorCode);
            if (perCell)
            {
                var cells = new List<Vector2Int>(block.OccupiedCells());
                int step = Mathf.Max(1, Mathf.CeilToInt(cells.Count / (float)MaxBigBreakBursts));
                for (int i = 0; i < cells.Count; i += step)
                    Vfx.Spawn(_config.cubeBreakVfx, CellToWorld(new RectInt(cells[i].x, cells[i].y, 1, 1)), tint, _cell, mat);
                AudioManager.Play(Sfx.BigBlockBreak);
            }
            else
                Vfx.Spawn(_config.cubeBreakVfx, block.transform.position, tint, Mathf.Max(block.Cells.width, block.Cells.height) * _cell, mat);
        }

        void RecountDestructibles()
        {
            int n = 0;
            foreach (BoardBlock b in _blocks) if (b.IsDestructible && !b.IsDestroyed) n++;
            DestructibleCount = n;
        }
    }
}
