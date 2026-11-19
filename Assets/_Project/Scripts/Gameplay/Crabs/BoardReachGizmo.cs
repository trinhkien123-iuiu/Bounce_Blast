using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>Crab Mode – công cụ kiểm tra bằng mắt: vẽ vùng cát đi được và các block ăn được (Gizmo, chỉ thấy trong Scene view / Game view bật Gizmos).
    /// Gắn trên World/Board. Tắt tick "show" khi không cần.</summary>
    [RequireComponent(typeof(BoardController))]
    public class BoardReachGizmo : MonoBehaviour
    {
        public bool showReachable = true;
        public bool showEatable = true;
        [Tooltip("Chỉ tô block ăn được của màu này (mã màu, vd \"r\"); trống = mọi màu")] public string colorFilter = "";

        [Header("Debug (read only)")]
        public int eatableCount;
        public int totalCount;

        BoardController _board;

        void OnDrawGizmos()
        {
            if (_board == null) _board = GetComponent<BoardController>();
            BoardReach reach = _board != null ? _board.Reach : null;
            if (reach == null || !Application.isPlaying) return;
            reach.Refresh();
            float cell = _board.CellSize;

            if (showReachable)
            {
                Gizmos.color = new Color(1f, 0.9f, 0.5f, 0.18f);
                for (int y = 0; y < reach.Height; y++)
                    for (int x = 0; x < reach.Width; x++)
                        if (reach.IsReachable(x, y))
                            Gizmos.DrawCube(reach.PaddedToWorld(x, y) + Vector2.zero, new Vector3(cell * 0.9f, cell * 0.9f, 0.05f));
            }

            int e = 0, t = 0;
            foreach (BoardBlock b in _board.Blocks)
            {
                if (b == null || b.IsDestroyed || !b.IsDestructible) continue;
                t++;
                if (!reach.IsEatable(b)) continue;
                e++;
                if (!showEatable) continue;
                if (!string.IsNullOrEmpty(colorFilter) && !string.Equals(b.ColorCode, colorFilter, System.StringComparison.OrdinalIgnoreCase)) continue;
                RectInt r = b.Cells;
                Vector3 c = _board.CellToWorld(r);
                Gizmos.color = Color.white;
                Gizmos.DrawWireCube(new Vector3(c.x, c.y, -2f), new Vector3(r.width * cell, r.height * cell, 0.05f));
            }
            eatableCount = e;
            totalCount = t;
        }
    }
}
