using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    // Một phần của BoardController (partial) – xem BoardController.cs.
    /// <summary>Truy vấn cho bi và booster: khối mục tiêu, ngắm thẳng (duyệt lưới), khối lộ ra, khối tại điểm chạm.</summary>
    public partial class BoardController
    {
        // ------------------------------------------------------------------ Truy vấn
        /// <summary>Khối mà bi màu này nên nhắm tới (cube cùng màu). Hòm/cột nhận bi mọi màu nhưng không phải mục tiêu.</summary>
        static bool IsTarget(BoardBlock b, string color) => b.Accepts(color) && !b.IsFreeHit;

        public bool HasTargetFor(string color)
        {
            // hòm gỗ còn máu (bi nảy tiếp & trừ máu, tính vào điều kiện thắng) cũng là việc còn phải làm → chưa bế tắc
            foreach (BoardBlock b in _blocks) if (IsTarget(b, color) || (b.IsFreeHit && b.IsDestructible && !b.IsDestroyed)) return true;
            return false;
        }

        /// <summary>Khối nhận màu này gần nhất mà bi có thể chạm tới (ưu tiên mặt trên đang lộ).</summary>
        public bool TryGetNearestTarget(string color, Vector2 from, out Vector2 point)
        {
            point = default;
            float best = float.MaxValue;
            foreach (BoardBlock b in _blocks)
            {
                if (!IsTarget(b, color)) continue;
                RectInt r = b.Cells;
                // chỉ nhắm khối có ít nhất 1 cạnh lộ ra ngoài
                if (!IsExposed(b)) continue;
                Vector2 c = CellToWorld(r);
                float d = (c - from).sqrMagnitude;
                if (d < best) { best = d; point = c; }
            }
            return best < float.MaxValue;
        }

        readonly List<BoardBlock> _losCandidates = new List<BoardBlock>();

        // Bộ nhớ đệm các khối LỘ RA theo màu. Trước đây mỗi lần một viên bi cần ngắm đều duyệt TOÀN BỘ khối (màn dày 1344 khối) và
        // gọi IsExposed (cấp phát iterator mỗi khối) → với ~140 bi cùng nảy là điểm nóng CPU + GC. Giờ chỉ dựng lại khi lưới đổi
        // (khối vỡ / rơi) và chỉ khi có bi hỏi.
        int _gridVersion, _losCacheVersion = -1;
        readonly Dictionary<string, List<BoardBlock>> _losByColor = new Dictionary<string, List<BoardBlock>>();
        readonly List<BoardBlock> _losCrates = new List<BoardBlock>();

        void RefreshLosCache()
        {
            if (_losCacheVersion == _gridVersion) return;
            _losCacheVersion = _gridVersion;
            foreach (List<BoardBlock> l in _losByColor.Values) l.Clear();
            _losCrates.Clear();
            for (int i = 0; i < _blocks.Count; i++)
            {
                BoardBlock b = _blocks[i];
                if (b == null || b.IsDestroyed || !IsExposed(b)) continue;
                if (b.IsFreeHit) { if (b.IsDestructible) _losCrates.Add(b); continue; }
                if (!b.Rules.IsColorTarget) continue;
                string key = b.ColorCode != null ? b.ColorCode.ToLowerInvariant() : "";
                if (!_losByColor.TryGetValue(key, out List<BoardBlock> list)) _losByColor[key] = list = new List<BoardBlock>();
                list.Add(b);
            }
        }
        /// <summary>
        /// Khối nhận màu này mà bi bay thẳng tới được (đường bay không vướng khối khác màu) – ngắm khi đường thông, không bẻ cong xuyên vật cản.
        /// Duyệt THẲNG trên lưới ô (3 tia song song cách nhau ~bán kính bi) thay cho Physics2D.CircleCast: đo ở màn dày (≈240 bi),
        /// 5 CircleCast/lần nảy chiếm ~17 ms/khung trên Editor; duyệt lưới chỉ là vài trăm lần tra mảng. Tường khung lồi nên đoạn bay
        /// trong lòng bàn luôn thông tường.
        /// </summary>
        public bool TryGetLineOfSightTarget(string color, Vector2 from, float radius, int mask, out Vector2 point)
        {
            point = default;
            RefreshLosCache();
            _losCandidates.Clear();
            string key = color != null ? color.ToLowerInvariant() : "";
            if (_losByColor.TryGetValue(key, out List<BoardBlock> byColor)) _losCandidates.AddRange(byColor);
            // hết khối cùng màu → ngắm hòm gỗ còn lại (để bi phá hòm thay vì nảy vô định)
            bool aimCrates = _losCandidates.Count == 0;
            if (aimCrates) _losCandidates.AddRange(_losCrates);
            if (_losCandidates.Count == 0) return false;

            // Tìm tối đa 3 khối gần nhất không dùng lambda sort (Zero GC)
            int checkCount = Mathf.Min(3, _losCandidates.Count);
            for (int k = 0; k < checkCount; k++)
            {
                int bestIdx = k;
                float bestDistSqr = ((Vector2)_losCandidates[k].transform.position - from).sqrMagnitude;
                for (int i = k + 1; i < _losCandidates.Count; i++)
                {
                    float dSqr = ((Vector2)_losCandidates[i].transform.position - from).sqrMagnitude;
                    if (dSqr < bestDistSqr)
                    {
                        bestDistSqr = dSqr;
                        bestIdx = i;
                    }
                }
                if (bestIdx != k)
                {
                    var temp = _losCandidates[k];
                    _losCandidates[k] = _losCandidates[bestIdx];
                    _losCandidates[bestIdx] = temp;
                }

                Vector2 target = _losCandidates[k].transform.position;
                BoardBlock hit = FirstBlockOnPath(from, target, radius * 0.7f);
                if (hit != null && (IsTarget(hit, color) || (aimCrates && hit.IsFreeHit && hit.IsDestructible)))
                {
                    point = hit.transform.position;
                    return true;
                }
            }
            return false;
        }

        /// <summary>Khối đầu tiên gặp trên đoạn from→to (3 tia: tâm và hai bên lệch <paramref name="halfWidth"/>); null nếu không gặp gì.</summary>
        BoardBlock FirstBlockOnPath(Vector2 from, Vector2 to, float halfWidth)
        {
            Vector2 d = to - from;
            float dist = d.magnitude;
            if (dist < 0.001f) return null;
            Vector2 dir = d / dist, perp = new Vector2(-dir.y, dir.x) * halfWidth;
            int cols = _grid.GetLength(0), rows = _grid.GetLength(1);
            float step = _cell * 0.4f;
            for (float t = halfWidth * 2f; t <= dist + _cell * 0.5f; t += step)   // bỏ đoạn sát bi (khối vừa va chạm)
            {
                Vector2 c = from + dir * t;
                for (int ray = 0; ray < 3; ray++)
                {
                    Vector2 p = ray == 0 ? c : (ray == 1 ? c + perp : c - perp);
                    int x = Mathf.FloorToInt((p.x - _origin.x) / _cell), y = Mathf.FloorToInt((p.y - _origin.y) / _cell);
                    if (x < 0 || y < 0 || x >= cols || y >= rows) continue;
                    BoardBlock b = _grid[x, y];
                    if (b == null || b.IsDestroyed) continue;
                    return b;
                }
            }
            return null;
        }

        bool IsExposed(BoardBlock b)
        {
            int cols = _grid.GetLength(0), rows = _level.BoardRows;
            RectInt r = b.Cells;
            if (b.Mask == null)
            {
                for (int x = r.xMin; x < r.xMax; x++)
                    for (int y = r.yMin; y < r.yMax; y++)
                        if (CellExposed(x, y, cols, rows, b)) return true;
                return false;
            }
            for (int i = 0; i < b.Mask.Count; i++)
                if (CellExposed(r.x + b.Mask[i].x, r.y + b.Mask[i].y, cols, rows, b)) return true;
            return false;
        }

        bool CellExposed(int x, int y, int cols, int rows, BoardBlock b) =>
            y + 1 >= rows || Free(x, y + 1, b) || x == 0 || Free(x - 1, y, b) || x + 1 >= cols || Free(x + 1, y, b);

        bool Free(int x, int y, BoardBlock self) => _grid[x, y] == null || _grid[x, y] == self;

        public void ClearAll()
        {
            // tắt ngay (Destroy chỉ chạy cuối frame) → nạp lại màn trong cùng frame không bị chồng 2 bàn
            foreach (BoardBlock b in _blocks) if (b != null) { b.gameObject.SetActive(false); Destroy(b.gameObject); }
            _blocks.Clear();
        }

        /// <summary>Booster búa: phá ngay khối tại điểm chạm.</summary>
        public BoardBlock BlockAt(Vector2 world)
        {
            Collider2D hit = Physics2D.OverlapPoint(world);
            return hit != null ? hit.GetComponent<BoardBlock>() : null;
        }
    }
}
