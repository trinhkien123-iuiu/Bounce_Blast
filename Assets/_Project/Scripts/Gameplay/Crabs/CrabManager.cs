using System.Collections.Generic;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>Pool cua + đặt trước block (mỗi lần cạp = 1 hit; block Hp&gt;1 cần nhiều cua) + xử lý cạp</summary>
    public class CrabManager : MonoBehaviour
    {
        GameConfig _cfg;
        ColorLibrary _colors;
        BoardController _board;
        readonly Stack<Crab> _pool = new Stack<Crab>();
        readonly List<Crab> _active = new List<Crab>();
        readonly Dictionary<string, Material> _mats = new Dictionary<string, Material>();
        readonly Dictionary<BoardBlock, int> _reservedHits = new Dictionary<BoardBlock, int>();
        readonly HashSet<BoardBlock> _full = new HashSet<BoardBlock>();

        public int ActiveCount => _active.Count;
        public bool CanSpawn => _active.Count < _cfg.maxCrabs;
        /// <summary>Block đã đủ cua đặt trước — bỏ qua khi tìm mục tiêu mới</summary>
        public HashSet<BoardBlock> FullyReserved => _full;
        /// <summary>Block còn nhận thêm cua được không (chưa đủ cua đặt trước theo số máu)</summary>
        public bool CanReserve(BoardBlock b) => b != null && !b.IsDestroyed && !_full.Contains(b);

        public void Init(GameConfig cfg, ColorLibrary colors, BoardController board)
        {
            _cfg = cfg; _colors = colors; _board = board;
        }

        Material MatFor(string color, Crab sample)
        {
            string key = color != null ? color.ToLowerInvariant() : "";
            if (_mats.TryGetValue(key, out Material m)) return m;
            Material baseMat = sample.tinted.Length > 0 && sample.tinted[0] != null ? sample.tinted[0].sharedMaterial : null;
            m = baseMat != null ? new Material(baseMat) : new Material(Shader.Find("Unlit/Color"));
            Color c = _colors.ColorOf(color);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            _mats[key] = m;
            return m;
        }

        Crab Take()
        {
            while (_pool.Count > 0) { Crab c = _pool.Pop(); if (c != null) return c; }
            GameObject go = Instantiate(_cfg.crabPrefab, transform);
            go.name = "Crab";
            return go.GetComponent<Crab>();
        }

        /// <summary>Sinh một cua tại điểm start, đi theo target.Path tới target.Block</summary>
        public void Spawn(string color, Vector3 start, BoardReach.Target target)
        {
            Crab c = Take();
            Material mat = MatFor(color, c);
            Reserve(target.Block);
            _active.Add(c);
            Vector2 jitter = Random.insideUnitCircle * _cfg.crabSpawnJitter; // nhả nhiều con cùng lúc không chồng khít lên nhau
            c.Begin(this, _cfg, color, mat, target.Block, SmoothPath(target.Path), new Vector3(start.x + jitter.x, start.y + jitter.y, _cfg.crabZ));
            AudioManager.Play(Sfx.ShufflePop);
        }

        void Reserve(BoardBlock b)
        {
            _reservedHits.TryGetValue(b, out int n);
            _reservedHits[b] = n + 1;
            Refresh(b);
        }

        void Release(BoardBlock b)
        {
            if (b == null) return;
            if (_reservedHits.TryGetValue(b, out int n))
            {
                if (n <= 1) _reservedHits.Remove(b); else _reservedHits[b] = n - 1;
            }
            Refresh(b);
        }

        void Refresh(BoardBlock b)
        {
            _reservedHits.TryGetValue(b, out int n);
            if (b != null && !b.IsDestroyed && n >= b.Hp && n > 0) _full.Add(b); else _full.Remove(b);
        }

        /// <summary>Đường BFS theo ô lưới gấp khúc → đường mềm: bỏ điểm thẳng hàng, bo góc (Chaikin), mỗi con lệch làn một chút.
        /// Giữ nguyên điểm cuối (ô đứng cạp) để cua vẫn đứng sát block</summary>
        List<Vector2> SmoothPath(List<Vector2> path)
        {
            if (path == null || path.Count < 3) return path;
            var pts = new List<Vector2> { path[0] };
            for (int i = 1; i < path.Count - 1; i++)
            {
                Vector2 a = path[i] - path[i - 1], b = path[i + 1] - path[i];
                if (Mathf.Abs(a.x * b.y - a.y * b.x) > 1e-5f) pts.Add(path[i]); // chỉ giữ điểm rẽ
            }
            pts.Add(path[path.Count - 1]);
            for (int it = 0; it < _cfg.crabPathSmoothing; it++)
            {
                if (pts.Count < 3) break;
                var o = new List<Vector2> { pts[0] };
                for (int i = 0; i < pts.Count - 1; i++)
                {
                    Vector2 p = pts[i], q = pts[i + 1];
                    if (i > 0) o.Add(Vector2.Lerp(p, q, 0.25f));
                    if (i < pts.Count - 2) o.Add(Vector2.Lerp(p, q, 0.75f));
                }
                o.Add(pts[pts.Count - 1]);
                pts = o;
            }
            float lane = Random.Range(-1f, 1f) * _cfg.crabLaneJitter * _board.CellSize;
            for (int i = 1; i < pts.Count - 1; i++)
            {
                Vector2 dir = (pts[i + 1] - pts[i - 1]).normalized;
                float fade = Mathf.Clamp01(Mathf.Min(i, pts.Count - 1 - i) / 2f); // về sát đích thì hết lệch
                pts[i] += new Vector2(-dir.y, dir.x) * lane * fade;
            }
            return pts;
        }

        /// <summary>Nhát cạp chưa vỡ: vụn thạch nhỏ cùng màu bắn ra ở mép block + tiếng gặm</summary>
        public void Nibble(Crab c)
        {
            BoardBlock b = c.Target;
            if (b == null || b.IsDestroyed) return;
            b.Punch();
            Vector3 at = Vector3.Lerp(c.transform.position, b.transform.position, 0.6f);
            _board.BreakFx.SpawnBurst(new Vector3(at.x, at.y, b.transform.position.z - 0.3f), b.ColorCode, _board.CellSize * _cfg.crabNibbleFxSize);
            AudioManager.Play(Sfx.StoneHit);
        }

        /// <summary>Cua biến mất: bụi trắng nhỏ</summary>
        public void OnPoof(Crab c)
        {
            if (_cfg.ammoLaunchVfx != null) Vfx.Spawn(_cfg.ammoLaunchVfx, c.transform.position + new Vector3(0f, 0f, -0.2f), _cfg.crabBurrowDustColor, _cfg.crabPoofFxSize);
        }

        /// <summary>Cua tới nơi và cạp xong: 1 hit lên block</summary>
        public void Bite(Crab c)
        {
            BoardBlock b = c.Target;
            if (b == null || b.IsDestroyed) return;
            c.CarryChunk(_colors.CubeMat(c.ColorCode), _board.CellSize);
            _board.HandleBallHit(b, c.ColorCode);
            AudioManager.Play(Sfx.BallHit);
            Release(b);
            Refresh(b);
        }

        /// <summary>Mục tiêu của cua biến mất giữa đường: tìm block khác từ vị trí hiện tại</summary>
        public void Retarget(Crab c)
        {
            Release(c.Target);
            BoardReach reach = _board.Reach;
            if (reach != null && reach.TryFindTarget(_board.Blocks, c.ColorCode, c.transform.position, _full, true, out BoardReach.Target t))
            {
                Reserve(t.Block);
                c.Reroute(t.Block, SmoothPath(t.Path));
            }
            else c.Reroute(null, null);
        }

        public void Recycle(Crab c)
        {
            _active.Remove(c);
            c.ResetState();
            c.gameObject.SetActive(false);
            _pool.Push(c);
        }

        /// <summary>Xoá hết cua (đổi level / thắng)</summary>
        public void ClearAll()
        {
            for (int i = _active.Count - 1; i >= 0; i--) Recycle(_active[i]);
            _reservedHits.Clear();
            _full.Clear();
        }
    }
}
