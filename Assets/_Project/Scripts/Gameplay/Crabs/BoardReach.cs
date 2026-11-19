using System.Collections.Generic;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>Crab Mode: "có đường ăn". Bàn không còn tường, quanh tranh là cát trống (viền margin ô).
    /// Flood fill 4 hướng từ viền ngoài qua các ô trống: ô nào nối được ra viền thì cua đi tới được.
    /// Block ăn được = có ít nhất một ô kề (4 hướng) là ô đi được. Port từ computeReach/bfs/findTargetOn của demo crab-chomp.
    /// Toạ độ "padded": (x, y) = ô lưới + margin. Tự tính lại khi BoardGrid.Version đổi.</summary>
    public class BoardReach
    {
        static readonly Vector2Int[] Dirs = { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1) };

        readonly BoardGrid _grid;
        readonly int _cols, _rows, _margin;
        readonly int _fw, _fh;
        readonly bool[] _reach;
        readonly bool[] _occ;
        int _version = -1;

        public int Margin => _margin;
        public int Width => _fw;
        public int Height => _fh;

        public BoardReach(BoardGrid grid, int cols, int rows, int margin)
        {
            _grid = grid;
            _cols = cols;
            _rows = rows;
            _margin = Mathf.Max(1, margin);
            _fw = cols + _margin * 2;
            _fh = rows + _margin * 2;
            _reach = new bool[_fw * _fh];
            _occ = new bool[_fw * _fh];
        }

        int Idx(int x, int y) => y * _fw + x;
        bool In(int x, int y) => x >= 0 && y >= 0 && x < _fw && y < _fh;

        /// <summary>Tâm ô padded (x, y) trong toạ độ thế giới</summary>
        public Vector2 PaddedToWorld(int x, int y) =>
            _grid.Origin + new Vector2((x - _margin + 0.5f) * _grid.Cell, (y - _margin + 0.5f) * _grid.Cell);

        /// <summary>Ô padded chứa điểm thế giới (có thể nằm ngoài lưới, trong vùng cát)</summary>
        public Vector2Int WorldToPadded(Vector2 w)
        {
            int x = Mathf.FloorToInt((w.x - _grid.Origin.x) / _grid.Cell) + _margin;
            int y = Mathf.FloorToInt((w.y - _grid.Origin.y) / _grid.Cell) + _margin;
            return new Vector2Int(Mathf.Clamp(x, 0, _fw - 1), Mathf.Clamp(y, 0, _fh - 1));
        }

        /// <summary>Tính lại flood fill nếu bàn đã đổi từ lần trước</summary>
        public void Refresh()
        {
            if (_version == _grid.Version) return;
            _version = _grid.Version;

            for (int y = 0; y < _fh; y++)
                for (int x = 0; x < _fw; x++)
                    _occ[Idx(x, y)] = _grid.BlockAt(x - _margin, y - _margin) != null;

            System.Array.Clear(_reach, 0, _reach.Length);
            var q = new Queue<int>();
            for (int x = 0; x < _fw; x++) { Seed(x, 0, q); Seed(x, _fh - 1, q); }
            for (int y = 0; y < _fh; y++) { Seed(0, y, q); Seed(_fw - 1, y, q); }
            while (q.Count > 0)
            {
                int i = q.Dequeue();
                int cx = i % _fw, cy = i / _fw;
                foreach (Vector2Int d in Dirs)
                {
                    int nx = cx + d.x, ny = cy + d.y;
                    if (!In(nx, ny)) continue;
                    int n = Idx(nx, ny);
                    if (_reach[n] || _occ[n]) continue;
                    _reach[n] = true;
                    q.Enqueue(n);
                }
            }
        }

        void Seed(int x, int y, Queue<int> q)
        {
            int i = Idx(x, y);
            if (_occ[i] || _reach[i]) return;
            _reach[i] = true;
            q.Enqueue(i);
        }

        /// <summary>Ô padded trống và nối được ra viền ngoài</summary>
        public bool IsReachable(int x, int y)
        {
            Refresh();
            return In(x, y) && _reach[Idx(x, y)];
        }

        /// <summary>Ô padded có block chiếm</summary>
        public bool IsOccupied(int x, int y)
        {
            Refresh();
            return In(x, y) && _occ[Idx(x, y)];
        }

        /// <summary>Block có ít nhất một ô kề (4 hướng) là ô đi được → cua tới cạp được</summary>
        public bool IsEatable(BoardBlock b)
        {
            if (b == null || b.IsDestroyed) return false;
            Refresh();
            foreach (Vector2Int c in b.OccupiedCells())
            {
                int px = c.x + _margin, py = c.y + _margin;
                foreach (Vector2Int d in Dirs)
                {
                    int nx = px + d.x, ny = py + d.y;
                    if (In(nx, ny) && _reach[Idx(nx, ny)]) return true;
                }
            }
            return false;
        }

        /// <summary>Có block cùng màu nào đang ăn được không (máy xay dùng để quyết định kích hoạt hay chờ)</summary>
        public bool AnyEatable(IReadOnlyList<BoardBlock> blocks, string color)
        {
            foreach (BoardBlock b in blocks)
                if (b != null && !b.IsDestroyed && BoardTargeting.IsTarget(b, color) && IsEatable(b)) return true;
            return false;
        }

        /// <summary>Kết quả tìm mục tiêu: block, ô cua đứng cạp, đường đi (tâm ô, thế giới)</summary>
        public struct Target
        {
            public BoardBlock Block;
            public Vector2Int Stand;
            public int Distance;
            public List<Vector2> Path;
        }

        /// <summary>Block cùng màu ăn được GẦN NHẤT theo đường đi từ ô xuất phát (toạ độ thế giới). Bỏ qua block có trong reserved.
        /// Hoà thì chọn block thấp hơn rồi trái hơn cho ổn định.</summary>
        public bool TryFindTarget(IReadOnlyList<BoardBlock> blocks, string color, Vector2 startWorld,
                                  HashSet<BoardBlock> reserved, bool wantPath, out Target result)
        {
            result = default;
            Refresh();
            Vector2Int s = WorldToPadded(startWorld);
            int start = Idx(s.x, s.y);
            if (!_reach[start]) { start = NearestReachable(s); if (start < 0) return false; }

            int[] dist = new int[_fw * _fh];
            int[] prev = new int[_fw * _fh];
            for (int i = 0; i < dist.Length; i++) { dist[i] = -1; prev[i] = -1; }
            var q = new Queue<int>();
            dist[start] = 0;
            q.Enqueue(start);
            while (q.Count > 0)
            {
                int i = q.Dequeue();
                int cx = i % _fw, cy = i / _fw;
                foreach (Vector2Int d in Dirs)
                {
                    int nx = cx + d.x, ny = cy + d.y;
                    if (!In(nx, ny)) continue;
                    int n = Idx(nx, ny);
                    if (!_reach[n] || dist[n] >= 0) continue;
                    dist[n] = dist[i] + 1;
                    prev[n] = i;
                    q.Enqueue(n);
                }
            }

            BoardBlock best = null;
            int bestD = int.MaxValue, bestStand = -1;
            foreach (BoardBlock b in blocks)
            {
                if (b == null || b.IsDestroyed || !BoardTargeting.IsTarget(b, color)) continue;
                if (reserved != null && reserved.Contains(b)) continue;
                int d = int.MaxValue, stand = -1;
                foreach (Vector2Int c in b.OccupiedCells())
                {
                    int px = c.x + _margin, py = c.y + _margin;
                    foreach (Vector2Int dd in Dirs)
                    {
                        int nx = px + dd.x, ny = py + dd.y;
                        if (!In(nx, ny)) continue;
                        int n = Idx(nx, ny);
                        if (dist[n] >= 0 && dist[n] < d) { d = dist[n]; stand = n; }
                    }
                }
                if (stand < 0) continue;
                if (best == null || d < bestD || (d == bestD && Better(b, best)))
                { best = b; bestD = d; bestStand = stand; }
            }
            if (best == null) return false;

            result.Block = best;
            result.Stand = new Vector2Int(bestStand % _fw, bestStand / _fw);
            result.Distance = bestD;
            if (wantPath)
            {
                var path = new List<Vector2>();
                for (int c = bestStand; c >= 0; c = prev[c]) path.Add(PaddedToWorld(c % _fw, c / _fw));
                path.Reverse();
                result.Path = path;
            }
            return true;
        }

        /// <summary>Một ứng viên mục tiêu: block + ô đứng cạp + khoảng cách đường đi</summary>
        public struct Candidate { public BoardBlock Block; public int Stand; public int Distance; }

        int[] _sDist, _sPrev;
        readonly Queue<int> _sQueue = new Queue<int>();
        readonly List<Candidate> _cands = new List<Candidate>();
        readonly Dictionary<BoardBlock, int> _candIndex = new Dictionary<BoardBlock, int>();

        /// <summary>MỘT lần BFS từ điểm xuất phát → mọi block cùng màu ăn được, sắp gần → xa (dùng để nhả nhiều cua một lúc).
        /// Danh sách trả về dùng chung, chỉ hợp lệ tới lần gọi Search kế tiếp; MakeTarget phải gọi trước lần Search sau</summary>
        public List<Candidate> Search(IReadOnlyList<BoardBlock> blocks, string color, Vector2 startWorld)
        {
            Refresh();
            _cands.Clear();
            int n = _fw * _fh;
            if (_sDist == null || _sDist.Length != n) { _sDist = new int[n]; _sPrev = new int[n]; }
            Vector2Int s = WorldToPadded(startWorld);
            int start = Idx(s.x, s.y);
            if (!_reach[start]) { start = NearestReachable(s); if (start < 0) return _cands; }
            for (int i = 0; i < n; i++) { _sDist[i] = -1; _sPrev[i] = -1; }
            _sQueue.Clear();
            _sDist[start] = 0;
            _sQueue.Enqueue(start);
            while (_sQueue.Count > 0)
            {
                int i = _sQueue.Dequeue();
                int cx = i % _fw, cy = i / _fw;
                foreach (Vector2Int d in Dirs)
                {
                    int nx = cx + d.x, ny = cy + d.y;
                    if (!In(nx, ny)) continue;
                    int k = Idx(nx, ny);
                    if (!_reach[k] || _sDist[k] >= 0) continue;
                    _sDist[k] = _sDist[i] + 1;
                    _sPrev[k] = i;
                    _sQueue.Enqueue(k);
                }
            }
            foreach (BoardBlock b in blocks)
            {
                if (b == null || b.IsDestroyed || !BoardTargeting.IsTarget(b, color)) continue;
                int best = int.MaxValue, stand = -1;
                foreach (Vector2Int c in b.OccupiedCells())
                {
                    int px = c.x + _margin, py = c.y + _margin;
                    foreach (Vector2Int dd in Dirs)
                    {
                        int nx = px + dd.x, ny = py + dd.y;
                        if (!In(nx, ny)) continue;
                        int k = Idx(nx, ny);
                        if (_sDist[k] >= 0 && _sDist[k] < best) { best = _sDist[k]; stand = k; }
                    }
                }
                if (stand >= 0) _cands.Add(new Candidate { Block = b, Stand = stand, Distance = best });
            }
            _cands.Sort((a, b) => a.Distance != b.Distance ? a.Distance.CompareTo(b.Distance) : (Better(a.Block, b.Block) ? -1 : 1));
            return _cands;
        }

        /// <summary>Dựng đường đi cho một ứng viên của lần Search gần nhất</summary>
        public Target MakeTarget(Candidate c)
        {
            var path = new List<Vector2>();
            for (int k = c.Stand; k >= 0; k = _sPrev[k]) path.Add(PaddedToWorld(k % _fw, k / _fw));
            path.Reverse();
            return new Target { Block = c.Block, Stand = new Vector2Int(c.Stand % _fw, c.Stand / _fw), Distance = c.Distance, Path = path };
        }

        static bool Better(BoardBlock a, BoardBlock b)
        {
            RectInt ra = a.Cells, rb = b.Cells;
            return ra.y != rb.y ? ra.y < rb.y : ra.x < rb.x;
        }

        int NearestReachable(Vector2Int from)
        {
            int best = -1, bd = int.MaxValue;
            for (int y = 0; y < _fh; y++)
                for (int x = 0; x < _fw; x++)
                {
                    if (!_reach[Idx(x, y)]) continue;
                    int d = Mathf.Abs(x - from.x) + Mathf.Abs(y - from.y);
                    if (d < bd) { bd = d; best = Idx(x, y); }
                }
            return best;
        }
    }
}
