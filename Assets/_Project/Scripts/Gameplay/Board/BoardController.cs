using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>
    /// Dựng và quản lý bàn chơi: lưới chiếm chỗ, trọng lực (khối mất chỗ đỡ thì rơi xuống),
    /// xử lý bi trúng khối, phá hòm lộ vật bên trong, báo thắng khi bàn sạch.
    /// </summary>
    public class BoardController : MonoBehaviour
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
        BoardBlock Spawn(BoardItemData item)
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
            Transform visual;
            TextMeshPro label;
            List<Rect> colliders = null;
            List<RectInt> parts = item.Mask != null ? Decompose(item.Mask, r.width, r.height) : null;
            if (item.Kind == BoardItemKind.Column)
            {
                visual = CreateColumnVisual(go.transform, item);
                label = CreateColumnBadge(go.transform, item, size);
                // collider chỉ bằng thân cột (mảnh, ~60% bề ngang ô) – bi không bật vào khoảng trống hai bên
                float cw = size.x * _config.columnWidthRatio;
                colliders = new List<Rect> { new Rect(-cw * 0.5f, -size.y * 0.5f, cw, size.y) };
            }
            else if ((parts == null || parts.Count <= 1) && item.Kind == BoardItemKind.CombinedCube)
            {
                var full = new HashSet<Vector2Int>();
                for (int x = 0; x < r.width; x++) for (int y = 0; y < r.height; y++) full.Add(new Vector2Int(x, y));
                visual = CreateSolidVisual(go.transform, item, r, full);
                label = CreateLabel(go.transform, item, size);
            }
            else if (parts == null || parts.Count <= 1)
            {
                visual = CreateVisual(go.transform, item, size);
                label = item.Kind == BoardItemKind.Wall ? null : CreateLabel(go.transform, item, size);
            }
            else
            {
                // Khối gộp hình chữ L…: dựng từng ô một, cạnh nào giáp ô cùng khối thì kéo phần vát của mesh sang ô kề
                // (mặt trên phẳng chạm đúng mép ô, phần vát chui dưới mặt phẳng của ô kề) → liền một khối, không lộ đường nối.
                // Collider vẫn dùng các hình chữ nhật con.
                visual = new GameObject("Visual").transform;
                visual.SetParent(go.transform, false);
                colliders = new List<Rect>();
                RectInt biggest = parts[0];
                foreach (RectInt p in parts)
                {
                    Vector2 ps = new Vector2(p.width, p.height) * _cell;
                    Vector2 off = new Vector2((p.x + p.width * 0.5f - r.width * 0.5f) * _cell, (p.y + p.height * 0.5f - r.height * 0.5f) * _cell);
                    colliders.Add(new Rect(off - ps * 0.5f, ps));
                    if (p.width * p.height > biggest.width * biggest.height) biggest = p;
                }
                // Một mesh LIỀN theo đường viền hình dạng (không ghép mảnh → không lộ bậc/đường nối)
                Destroy(visual.gameObject);
                visual = CreateSolidVisual(go.transform, item, r, new HashSet<Vector2Int>(item.Mask));
                Vector2 bs = new Vector2(biggest.width, biggest.height) * _cell;
                label = CreateLabel(go.transform, item, bs);
                if (label != null)
                    label.transform.localPosition += new Vector3((biggest.x + biggest.width * 0.5f - r.width * 0.5f) * _cell,
                                                                 (biggest.y + biggest.height * 0.5f - r.height * 0.5f) * _cell, 0f);
            }

            // Khối nhiều ô / hòm / tường: xiên mesh thay vì xoay (xem ShearForView) → mặt trước phẳng khít lưới như gạch 1 ô
            if (item.Kind != BoardItemKind.Cube && item.Kind != BoardItemKind.Column && visual != null)
                foreach (MeshFilter smf in visual.GetComponentsInChildren<MeshFilter>(true))
                    if (smf.sharedMesh != null && smf.sharedMesh.isReadable && smf.GetComponent<TextMeshPro>() == null && !smf.sharedMesh.name.StartsWith("Solid"))
                        smf.sharedMesh = ShearForView(smf.sharedMesh);

            var block = go.AddComponent<BoardBlock>();
            block.Init(item, visual, label, CellToWorld(r), size, colliders);
            _blocks.Add(block);
            Occupy(block, true);
            return block;
        }

        readonly Dictionary<int, Mesh> _joinedMeshes = new Dictionary<int, Mesh>();
        readonly Dictionary<Mesh, Mesh> _shearedMeshes = new Dictionary<Mesh, Mesh>();

        // ------------------------------------------------------------------ Khối gộp: 1 mesh liền theo viền
        // Dải vát & độ dày khối gộp: chỉnh trong GameConfig (dải mặt trên = depth·tan20 ≈ 0.4 ô ≈ gạch 1 ô)
        readonly Dictionary<string, Mesh> _solidMeshes = new Dictionary<string, Mesh>();

        /// <summary>
        /// Khối gộp (chữ nhật, chữ L, chữ T…) dựng thành MỘT mesh: mặt trước phẳng (đa giác viền thu vào 1 dải vát),
        /// dải vát 45° + vách sau chạy liên tục quanh mép ngoài, rồi xiên theo góc nhìn như ShearForView → lộ dải mặt trên
        /// & sáng tối y như gạch 1 ô, không còn mảnh ghép lệch cỡ ở chỗ nối.
        /// </summary>
        Transform CreateSolidVisual(Transform parent, BoardItemData item, RectInt r, HashSet<Vector2Int> cells)
        {
            var v = new GameObject("Visual").transform;
            v.SetParent(parent, false);
            v.localPosition = new Vector3(0f, 0f, _config.blockFrontZ); // mặt trước ngang gạch 1 ô (mesh dựng với mặt trước ở z = 0)
            var keyB =new System.Text.StringBuilder().Append(r.width).Append('x').Append(r.height).Append(':');
            var sorted = new List<Vector2Int>(cells);
            sorted.Sort((p, q) => p.y != q.y ? p.y.CompareTo(q.y) : p.x.CompareTo(q.x));
            foreach (Vector2Int c in sorted) keyB.Append(c.x).Append(',').Append(c.y).Append(';');
            float bevel = _config.blockBevel, depth = _config.blockDepth, extra = _config.blockBottomExtra, trim = _config.blockTopTrim;
            string key = keyB.ToString() + _cell.ToString("F4") + '|' + bevel.ToString("F3") + '|' + depth.ToString("F3") + '|' + extra.ToString("F3") + '|' + trim.ToString("F3");
            if (!_solidMeshes.TryGetValue(key, out Mesh mesh) || mesh == null)
            {
                mesh = BuildSolidMesh(cells, r.width, r.height, _cell, bevel, depth, extra, trim);
                _solidMeshes[key] = mesh;
            }
            var go = new GameObject("Solid");
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(v, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = _colors.CubeMat(item.ColorCode);
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return v;
        }

        static Mesh BuildSolidMesh(HashSet<Vector2Int> cells, int w, int h, float cell, float bevelRatio, float depthRatio, float bottomExtraRatio, float topTrimRatio)
        {
            // 1) viền ngoài (cạnh có hướng, CCW, bỏ cạnh chung)
            var edges = new Dictionary<Vector2Int, Vector2Int>();
            var dirs = new[] { (new Vector2Int(0, 0), new Vector2Int(1, 0)), (new Vector2Int(1, 0), new Vector2Int(1, 1)),
                               (new Vector2Int(1, 1), new Vector2Int(0, 1)), (new Vector2Int(0, 1), new Vector2Int(0, 0)) };
            var set = new HashSet<(Vector2Int, Vector2Int)>();
            foreach (Vector2Int c in cells)
                foreach (var d in dirs) set.Add((c + d.Item1, c + d.Item2));
            foreach (var e in set) if (!set.Contains((e.Item2, e.Item1))) edges[e.Item1] = e.Item2;
            var loops = new List<List<Vector2>>();
            var used = new HashSet<Vector2Int>();
            foreach (Vector2Int start in edges.Keys)
            {
                if (used.Contains(start)) continue;
                var loop = new List<Vector2>();
                Vector2Int cur = start;
                int guard = 0;
                while (!used.Contains(cur) && guard++ < 10000)
                {
                    used.Add(cur);
                    loop.Add(cur);
                    if (!edges.TryGetValue(cur, out cur)) break;
                }
                loops.Add(Simplify(loop));
            }
            List<Vector2> outer = loops[0];
            foreach (var l in loops) if (Area(l) > Area(outer)) outer = l;

            Vector2 off = new Vector2(w * 0.5f, h * 0.5f);
            int n = outer.Count;
            var P = new Vector2[n];
            for (int i = 0; i < n; i++) P[i] = (outer[i] - off) * cell;
            float b = bevelRatio * cell, depth = depthRatio * cell;
            // đỉnh thu vào: P + b·(pháp tuyến trong cạnh trước + cạnh sau) (đa giác vuông góc → góc lồi/lõm đều đúng)
            var Q = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                Vector2 dPrev = (P[i] - P[(i + n - 1) % n]).normalized, dNext = (P[(i + 1) % n] - P[i]).normalized;
                Vector2 inPrev = new Vector2(-dPrev.y, dPrev.x), inNext = new Vector2(-dNext.y, dNext.x);
                Q[i] = P[i] + b * (inPrev + inNext);
            }

            var V = new List<Vector3>(); var N = new List<Vector3>(); var T = new List<int>();
            void Tri(Vector3 a, Vector3 bb, Vector3 c, Vector3 nrm)
            {
                if (Vector3.Dot(Vector3.Cross(bb - a, c - a), nrm) < 0f) { var t = bb; bb = c; c = t; }
                int o = V.Count; V.Add(a); V.Add(bb); V.Add(c); N.Add(nrm); N.Add(nrm); N.Add(nrm);
                T.Add(o); T.Add(o + 1); T.Add(o + 2);
            }
            void Quad(Vector3 a, Vector3 bb, Vector3 c, Vector3 d, Vector3 nrm) { Tri(a, bb, c, nrm); Tri(a, c, d, nrm); }

            // 2) mặt trước (z = 0)
            foreach (int[] t in EarClip(Q))
                Tri(Q[t[0]], Q[t[1]], Q[t[2]], Vector3.back);
            // 3) dải vát + vách quanh viền
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                Vector2 d = (P[j] - P[i]).normalized;
                Vector3 outN = new Vector3(d.y, -d.x, 0f);
                Quad(new Vector3(Q[i].x, Q[i].y, 0f), new Vector3(Q[j].x, Q[j].y, 0f),
                     new Vector3(P[j].x, P[j].y, b), new Vector3(P[i].x, P[i].y, b), (outN + Vector3.back).normalized);
                Quad(new Vector3(P[i].x, P[i].y, b), new Vector3(P[j].x, P[j].y, b),
                     new Vector3(P[j].x, P[j].y, depth), new Vector3(P[i].x, P[i].y, depth), outN);
            }
            // Kéo đáy ra thêm (gạch 1 ô nhô khỏi ô do cubeOverlap): dời các đỉnh ở đáy (viền ngoài + mép mặt trước) xuống cùng một đoạn
            float extra = bottomExtraRatio * cell;
            if (extra > 0f)
            {
                float minY = float.MaxValue;
                for (int i = 0; i < V.Count; i++) minY = Mathf.Min(minY, V[i].y);
                for (int i = 0; i < V.Count; i++)
                    if (V[i].y <= minY + b + 1e-4f) { Vector3 q = V[i]; q.y -= extra; V[i] = q; }
            }
            // Thụt đỉnh lại (dải mặt trên của khối gộp nhô cao hơn gạch 1 ô): dời các đỉnh ở đỉnh xuống cùng một đoạn
            float trimY = topTrimRatio * cell;
            if (trimY != 0f)
            {
                float maxY = float.MinValue;
                for (int i = 0; i < V.Count; i++) maxY = Mathf.Max(maxY, V[i].y);
                for (int i = 0; i < V.Count; i++)
                    if (V[i].y >= maxY - b - 1e-4f) { Vector3 q = V[i]; q.y -= trimY; V[i] = q; }
            }
            // 4) xiên theo góc nhìn (như ShearForView)
            float tan = ViewSpace.LegacyTan;
            Quaternion rot = Quaternion.Euler(-ViewSpace.LegacyPitch, 0f, 0f);
            // Xiên đẩy mép dưới của dải vát (z = b) lên b·tan → đáy khối co lên khỏi mép ô, lộ khe + dải sẫm so với hàng gạch dưới.
            // Hạ cả mesh xuống b·tan để mép dưới ngoài cùng đúng mép ô (mép trên chỉ dài thêm đúng bấy nhiêu).
            float drop = b * tan;
            for (int i = 0; i < V.Count; i++)
            {
                Vector3 p = V[i]; p.y += p.z * tan - drop; V[i] = p;
                N[i] = rot * N[i];
            }
            var cols = new List<Color>(V.Count);
            for (int i = 0; i < V.Count; i++) cols.Add(Color.white);
            var m = new Mesh { name = "Solid" };
            if (V.Count > 65000) m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            m.SetVertices(V); m.SetNormals(N); m.SetColors(cols); m.SetTriangles(T, 0);
            m.RecalculateBounds();
            return m;
        }

        static List<Vector2> Simplify(List<Vector2> loop)
        {
            var o = new List<Vector2>();
            int n = loop.Count;
            for (int i = 0; i < n; i++)
            {
                Vector2 a = loop[(i + n - 1) % n], p = loop[i], c = loop[(i + 1) % n];
                float cross = (p.x - a.x) * (c.y - p.y) - (p.y - a.y) * (c.x - p.x);
                if (Mathf.Abs(cross) > 1e-5f) o.Add(p);
            }
            return o;
        }

        static float Area(List<Vector2> l)
        {
            float a = 0f;
            for (int i = 0; i < l.Count; i++) { Vector2 p = l[i], q = l[(i + 1) % l.Count]; a += p.x * q.y - q.x * p.y; }
            return a * 0.5f;
        }

        /// <summary>Tam giác hoá đa giác đơn CCW bằng cắt tai.</summary>
        static List<int[]> EarClip(Vector2[] pts)
        {
            var res = new List<int[]>();
            var idx = new List<int>();
            for (int i = 0; i < pts.Length; i++) idx.Add(i);
            int guard = 0;
            while (idx.Count > 3 && guard++ < 10000)
            {
                bool cut = false;
                for (int k = 0; k < idx.Count; k++)
                {
                    int ia = idx[(k + idx.Count - 1) % idx.Count], ib = idx[k], ic = idx[(k + 1) % idx.Count];
                    Vector2 a = pts[ia], b = pts[ib], c = pts[ic];
                    float cr = (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
                    if (cr <= 1e-7f) continue; // góc lõm
                    bool inside = false;
                    foreach (int j in idx)
                    {
                        if (j == ia || j == ib || j == ic) continue;
                        if (InTri(pts[j], a, b, c)) { inside = true; break; }
                    }
                    if (inside) continue;
                    res.Add(new[] { ia, ib, ic });
                    idx.RemoveAt(k);
                    cut = true;
                    break;
                }
                if (!cut) break;
            }
            if (idx.Count == 3) res.Add(new[] { idx[0], idx[1], idx[2] });
            return res;
        }

        static bool InTri(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y);
            float d2 = (p.x - c.x) * (b.y - c.y) - (b.x - c.x) * (p.y - c.y);
            float d3 = (p.x - a.x) * (c.y - a.y) - (c.x - a.x) * (p.y - a.y);
            bool neg = d1 < 0 || d2 < 0 || d3 < 0, pos = d1 > 0 || d2 > 0 || d3 > 0;
            return !(neg && pos);
        }

        /// <summary>
        /// Gạch 1 ô ngả -20° quanh tâm nên lộ dải mặt trên. Khối to mà xoay nguyên khối thì 2 đầu chìa khỏi mặt phẳng bàn
        /// (không khít). Thay vào đó XIÊN mesh: giữ mặt trước (z min) đúng chỗ, đẩy phần sâu lên trên y += (z - zFront)·tan20,
        /// pháp tuyến xoay Rx(-20) → nhìn thẳng thấy dải mặt trên & ánh sáng y hệt gạch 1 ô, mặt trước vẫn phẳng khít lưới.
        /// </summary>
        Mesh ShearForView(Mesh src)
        {
            if (_shearedMeshes.TryGetValue(src, out Mesh cached) && cached != null) return cached;
            var m = Instantiate(src);
            m.name = src.name + " view";
            float z0 = src.bounds.min.z, t = ViewSpace.LegacyTan;
            Vector3[] v = m.vertices;
            for (int i = 0; i < v.Length; i++) v[i].y += (v[i].z - z0) * t;
            Quaternion r = Quaternion.Euler(-ViewSpace.LegacyPitch, 0f, 0f);
            Vector3[] n = m.normals;
            for (int i = 0; i < n.Length; i++) n[i] = r * n[i];
            m.vertices = v;
            m.normals = n;
            m.RecalculateBounds();
            _shearedMeshes[src] = m;
            return m;
        }

        /// <summary>
        /// Mesh cube 1 ô với các cạnh "nối" (giáp ô cùng khối, tính theo hướng world): phần phẳng kéo tới đúng mép ô,
        /// dải vát (20% ngoài) dời ra ngoài mép. Trục mesh ↔ trục world xác định qua transform (mesh bị xoay 10/180°, model -70°).
        /// </summary>
        Mesh JoinedCellMesh(MeshFilter mf, int cellsX, int cellsY, bool wl, bool wr, bool wb, bool wt)
        {
            Mesh src = mf.sharedMesh;
            Vector3 ax = mf.transform.TransformDirection(Vector3.right), az = mf.transform.TransformDirection(Vector3.up);
            bool xIsWorldX = Mathf.Abs(ax.x) >= Mathf.Abs(ax.y);
            // trục mesh ứng với world X / world Y và dấu
            float sxX = xIsWorldX ? Mathf.Sign(ax.x) : Mathf.Sign(az.x);
            float syY = xIsWorldX ? Mathf.Sign(az.y) : Mathf.Sign(ax.y);
            // cạnh nối theo trục mesh: (âm, dương) của trục ứng với world X và world Y
            bool xNeg = sxX > 0 ? wl : wr, xPos = sxX > 0 ? wr : wl;
            bool yNeg = syY > 0 ? wb : wt, yPos = syY > 0 ? wt : wb;
            int key = src.GetInstanceID() * 16 + cellsX * 7919 + cellsY * 104729 + (xNeg ? 1 : 0) + (xPos ? 2 : 0) + (yNeg ? 4 : 0) + (yPos ? 8 : 0) + (xIsWorldX ? 0 : 1 << 20);
            if (_joinedMeshes.TryGetValue(key, out Mesh cached) && cached != null) return cached;
            Bounds b = src.bounds;
            Vector3[] vs = src.vertices;
            for (int i = 0; i < vs.Length; i++)
            {
                float ex = xIsWorldX ? vs[i].x : vs[i].y, ez = xIsWorldX ? vs[i].y : vs[i].x;
                float cx = xIsWorldX ? b.center.x : b.center.y, cz = xIsWorldX ? b.center.y : b.center.x;
                float hx = xIsWorldX ? b.extents.x : b.extents.y, hz = xIsWorldX ? b.extents.y : b.extents.x;
                // dải vát của mesh 1 ô = 40% nửa cạnh gốc; mesh đã giãn 9-slice nên nửa cạnh = gốc × số ô
                ex = Join(ex, cx, hx, hx * 0.4f / Mathf.Max(1, cellsX), xNeg, xPos);
                ez = Join(ez, cz, hz, hz * 0.4f / Mathf.Max(1, cellsY), yNeg, yPos);
                if (xIsWorldX) { vs[i].x = ex; vs[i].y = ez; } else { vs[i].y = ex; vs[i].x = ez; }
            }
            var m = Instantiate(src);
            m.name = src.name + " joined";
            m.vertices = vs;
            FlattenFrontFace(m); // mép ngoài giữ vát 3D như gạch nhỏ; mép nối đã chui dưới mảnh kề
            m.RecalculateBounds();
            _joinedMeshes[key] = m;
            return m;
        }

        /// <summary>
        /// Khối lớn trong game gốc là tấm phẳng đồng màu (mesh CombinedCubic sinh lúc chạy). Mesh cube 1 ô có pháp tuyến bo
        /// tròn, giãn to thì lộ dải sáng-tối → đặt mọi pháp tuyến = mặt trên (+Y mesh) cho màu đồng đều.
        /// </summary>
        static void FlattenFrontFace(Mesh m)
        {
            Vector3[] n = m.normals;
            for (int i = 0; i < n.Length; i++) if (n[i].z < -0.9f) n[i] = Vector3.back; // chỉ mặt trước (-Z); mép vát (z≈-0.7) giữ nguyên → khối lớn có viền 3D
            m.normals = n;
        }

        static void FlattenShading(Mesh m)
        {
            var n = new Vector3[m.vertexCount];
            for (int i = 0; i < n.Length; i++) n[i] = Vector3.back; // mặt trước (-Z)
            m.normals = n;
        }

        static float Join(float c, float center, float half, float band, bool joinNeg, bool joinPos)
        {
            float d = c - center;
            bool join = d < 0f ? joinNeg : joinPos;
            if (!join) return c;
            float a = Mathf.Abs(d), inner = half - band;
            float r = a <= inner ? a * half / inner : half + (a - inner);
            return center + Mathf.Sign(d) * r;
        }

        /// <summary>Chia tập ô thành ít hình chữ nhật lớn (quét trái→phải, kéo dài ngang rồi dọc).</summary>
        static List<RectInt> Decompose(List<Vector2Int> cells, int w, int h)
        {
            var free = new bool[w, h];
            foreach (Vector2Int c in cells) if (c.x >= 0 && c.y >= 0 && c.x < w && c.y < h) free[c.x, c.y] = true;
            var result = new List<RectInt>();
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    if (!free[x, y]) continue;
                    int x2 = x;
                    while (x2 + 1 < w && free[x2 + 1, y]) x2++;
                    int y2 = y;
                    bool ok = true;
                    while (ok && y2 + 1 < h)
                    {
                        for (int i = x; i <= x2; i++) if (!free[i, y2 + 1]) { ok = false; break; }
                        if (ok) y2++;
                    }
                    for (int yy = y; yy <= y2; yy++)
                        for (int xx = x; xx <= x2; xx++) free[xx, yy] = false;
                    result.Add(new RectInt(x, y, x2 - x + 1, y2 - y + 1));
                }
            return result;
        }

        // ------------------------------------------------------------------ Cột nhà (/wl)
        static readonly Color ColumnDust = new Color(0.96f, 0.9f, 0.8f);
        static GameObject _columnPrefab, _columnBadge;
        static Dictionary<string, Transform> _columnPieces;

        static string NeighbourKey(IEnumerable<string> parts)
        {
            var l = new List<string>(parts);
            l.Sort(System.StringComparer.Ordinal);
            return string.Join("-", l);
        }

        /// <summary>
        /// Cột dựng từ mảnh gốc của ModularPrimitiveColumn: mỗi ô chọn mảnh có tên = tập ô hàng xóm cùng cột
        /// (T = đế cột, B = đầu cột, T-B = thân; cột rộng 2 ô dùng nửa trái/phải như T-TR-R, L-TL-T…).
        /// </summary>
        Transform CreateColumnVisual(Transform parent, BoardItemData item)
        {
            var root = new GameObject("Visual").transform;
            root.SetParent(parent, false);
            if (_columnPrefab == null) _columnPrefab = _config.columnPrefab; // GameConfig.columnPrefab
            if (_columnPrefab == null) return root;
            if (_columnPieces == null)
            {
                _columnPieces = new Dictionary<string, Transform>();
                foreach (Transform t in _columnPrefab.transform)
                    if (t.GetComponent<MeshRenderer>() != null) _columnPieces[NeighbourKey(t.name.Split('-'))] = t;
            }
            RectInt r = item.Cells;
            var cells = new HashSet<Vector2Int>();
            if (item.Mask != null) foreach (Vector2Int m in item.Mask) cells.Add(m);
            else for (int x = 0; x < r.width; x++) for (int y = 0; y < r.height; y++) cells.Add(new Vector2Int(x, y));

            Transform tilt = root; // mảnh gốc xoay -90° (mặt trên hướng camera thẳng) – giữ nguyên
            var n = new List<string>(8);
            foreach (Vector2Int c in cells)
            {
                n.Clear();
                if (cells.Contains(c + new Vector2Int(0, 1))) n.Add("T");
                if (cells.Contains(c + new Vector2Int(0, -1))) n.Add("B");
                if (cells.Contains(c + new Vector2Int(-1, 0))) n.Add("L");
                if (cells.Contains(c + new Vector2Int(1, 0))) n.Add("R");
                if (cells.Contains(c + new Vector2Int(-1, 1))) n.Add("TL");
                if (cells.Contains(c + new Vector2Int(1, 1))) n.Add("TR");
                if (cells.Contains(c + new Vector2Int(-1, -1))) n.Add("BL");
                if (cells.Contains(c + new Vector2Int(1, -1))) n.Add("BR");
                if (!_columnPieces.TryGetValue(NeighbourKey(n), out Transform src))
                {
                    // ngoài các biến thể có sẵn: lấy theo hướng dọc (đế / thân / đầu cột)
                    bool up = n.Contains("T"), down = n.Contains("B");
                    _columnPieces.TryGetValue(up && down ? "B-T" : up ? "T" : down ? "B" : "B-T", out src);
                }
                if (src == null) continue;
                var cellT = new GameObject("Cell").transform;
                cellT.SetParent(tilt, false);
                cellT.localPosition = new Vector3((c.x + 0.5f - r.width * 0.5f) * _cell, (c.y + 0.5f - r.height * 0.5f) * _cell, 0f);
                cellT.localScale = Vector3.one * _cell;
                GameObject inst = Instantiate(src.gameObject, cellT, false);
                inst.SetActive(true);
                inst.transform.localPosition = src.localPosition;
                inst.transform.localRotation = src.localRotation;
                inst.transform.localScale = src.localScale;
                StripPhysics(inst);
                foreach (Transform t in inst.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = parent.gameObject.layer;
            }
            return root;
        }

        /// <summary>Huy hiệu số máu ở giữa thân cột: đĩa kem gốc (CombinedCubicColumn/…/Cylinder) + số.</summary>
        TextMeshPro CreateColumnBadge(Transform parent, BoardItemData item, Vector2 size)
        {
            float d = Mathf.Min(size.x * 0.85f, size.y);  // video gốc: huy hiệu rộng hơn hẳn thân cột (~1.4 lần)
            TextMeshPro tmp = CreateLabel(parent, item, new Vector2(d, d));
            if (tmp == null) return null;
            tmp.transform.localPosition = new Vector3(0f, 0f, -_cell * 1.1f);
            tmp.fontSize = Mathf.Clamp(d * 7.5f, 2f, 10f); // số to, lấp ~65% huy hiệu
            if (_columnBadge == null)
            {
                var pf = _config.combinedColumnPrefab; // GameConfig.combinedColumnPrefab
                Transform cyl = pf != null ? pf.transform.Find("Model/3D Text/Text (TMP)/Cylinder") : null;
                _columnBadge = cyl != null ? cyl.gameObject : null;
            }
            if (_columnBadge != null)
            {
                GameObject disc = Instantiate(_columnBadge, tmp.transform, false);
                disc.name = "Badge";
                disc.transform.localPosition = new Vector3(0f, 0f, 0.03f);
                disc.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                disc.transform.localScale = new Vector3(d, 0.02f, d);   // mesh Cylinder: đường kính 1, cao 2
                StripPhysics(disc);
            }
            return tmp;
        }

        /// <summary>Cột vỡ vụn (như video gốc): ẩn mảnh liền, bật các mảnh vỡ _cell gốc và cho văng/rơi, kèm bụi kem.</summary>
        void BreakColumn(BoardBlock block)
        {
            Vector3 center = block.transform.position;
            foreach (MeshRenderer mr in block.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (!mr.gameObject.activeInHierarchy || mr.GetComponent<TextMeshPro>() != null || mr.name == "Badge") continue;
                if (mr.name.Contains("_cell")) continue;
                bool hasCells = false;
                foreach (Transform t in mr.GetComponentsInChildren<Transform>(true))
                {
                    if (!t.name.Contains("_cell")) continue;
                    hasCells = true;
                    Fling(t, center);
                }
                if (hasCells) mr.enabled = false; else Fling(mr.transform, center);
            }
            float h = block.Cells.height * _cell;
            for (int i = 0; i < 3; i++)
                Vfx.Spawn(_config.cubeBreakVfx, center + new Vector3(0f, (i - 1) * h * 0.3f, 0f), ColumnDust, block.Cells.width * _cell);
            AudioManager.Play(Sfx.CrateBreak);
        }

        static void Fling(Transform t, Vector3 center)
        {
            t.gameObject.SetActive(true);
            t.SetParent(Vfx.Root, true);
            var rb = t.GetComponent<Rigidbody>();
            if (rb == null) rb = t.gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.useGravity = true;
            Vector3 away = t.position - center; away.z = 0f;
            rb.velocity = new Vector3(Mathf.Sign(away.x + 1e-3f) * UnityEngine.Random.Range(0.6f, 2.2f), UnityEngine.Random.Range(0.5f, 3f), UnityEngine.Random.Range(-2.5f, -0.5f));
            rb.angularVelocity = UnityEngine.Random.insideUnitSphere * 9f;
            Destroy(t.gameObject, UnityEngine.Random.Range(0.9f, 1.4f));
        }

        bool _breakTuned;

        /// <summary>
        /// Mảnh vỡ gạch: prefab gốc chỉ 5 khối lập phương bay chậm 0.4s (và vật liệu cố định "Blue" bỏ qua màu tô).
        /// Bắn tung toé: 16–22 mảnh mọi hướng, nhanh, có trọng lực, xoay, nhỏ dần; vật liệu = vật liệu của viên gạch vỡ.
        /// </summary>
        void EnsureBreakTune()
        {
            if (_breakTuned || _config.cubeBreakVfx == null) return;
            _breakTuned = true;
            // Kiểu "thạch": giọt tròn mềm (mesh cầu), ít & chậm, trọng lực nhẹ, phồng nhẹ rồi teo dần – không vỡ vụn như bê tông
            Mesh blob = Resources.GetBuiltinResource<Mesh>("New-Sphere.fbx");
            Vfx.Tune(_config.cubeBreakVfx, ps =>
            {
                var main = ps.main;
                main.duration = 0.6f;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.7f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(1.8f, 4.2f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.22f, 0.42f); // ×cỡ ô
                main.gravityModifier = 0.9f;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.startRotation3D = false;
                main.startRotation = 0f;
                var em = ps.emission;
                em.rateOverTime = 0f;
                em.SetBursts(new[] { new ParticleSystem.Burst(0f, 8, 12) });
                var sh = ps.shape;
                sh.enabled = true;
                sh.shapeType = ParticleSystemShapeType.Sphere;
                sh.radius = 0.2f;
                var rot = ps.rotationOverLifetime;
                rot.enabled = false;
                var vel = ps.limitVelocityOverLifetime;
                vel.enabled = true;
                vel.limit = 3.5f;
                vel.dampen = 0.15f; // chậm dần, mềm như thạch
                var sz = ps.sizeOverLifetime;
                sz.enabled = true;
                sz.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                    new Keyframe(0f, 0.55f), new Keyframe(0.18f, 1.05f), new Keyframe(0.35f, 0.92f), new Keyframe(1f, 0f)));
                var pr = ps.GetComponent<ParticleSystemRenderer>();
                if (pr != null && blob != null) { pr.renderMode = ParticleSystemRenderMode.Mesh; pr.mesh = blob; }
            });
        }

        const int ShadeBuckets = 4;
        static readonly Dictionary<(Material, int), Material> ShadeMats = new Dictionary<(Material, int), Material>();

        static Material ShadeVariant(Material baseMat, Vector2 range, int bucket)
        {
            if (!baseMat.HasProperty("_CubeShadeOffset")) return baseMat;
            if (ShadeMats.TryGetValue((baseMat, bucket), out Material m) && m != null) return m;
            m = new Material(baseMat) { name = baseMat.name + " shade" + bucket };
            m.SetFloat("_CubeShadeOffset", Mathf.Lerp(range.x, range.y, (bucket + 0.5f) / ShadeBuckets));
            ShadeMats[(baseMat, bucket)] = m;
            return m;
        }

        /// <summary>Prefab gốc còn "3D Text/Text (TMP)": component TMP đã bị rip mất, chỉ còn MeshRenderer rỗng → xoá.</summary>
        public static void RemoveStrayText(Transform root)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t != null && t.name == "3D Text") { Destroy(t.gameObject); }
        }

        Transform CreateVisual(Transform parent, BoardItemData item, Vector2 size, float forcedDepth = -1f)
        {
            GameObject v = Instantiate(_config.cubePrefab, parent);
            v.name = "Visual";
            StripPhysics(v);
            foreach (TextMeshPro t in v.GetComponentsInChildren<TextMeshPro>(true)) Destroy(t.gameObject);
            RemoveStrayText(v.transform);

            Renderer rend = v.GetComponentInChildren<MeshRenderer>(true);
            Material mat;
            switch (item.Kind)
            {
                case BoardItemKind.Crate: mat = _config.crateMaterial; break;
                case BoardItemKind.Wall: mat = _config.wallMaterial; break;
                default: mat = _colors.CubeMat(item.ColorCode); break;
            }
            if (rend != null && mat != null) rend.sharedMaterial = mat;

            // Vân sáng/tối ngẫu nhiên cho từng cube nhỏ như game gốc (Colors_v3: MinOffset..MaxOffset).
            // Tối ưu: shader rip không có biến thể instancing → MaterialPropertyBlock làm mỗi cube 1 draw call riêng.
            // Thay bằng vài material dùng chung theo (màu, mức vân) → cube cùng material được dynamic batching gộp lại.
            if (rend != null && item.Kind == BoardItemKind.Cube && mat != null)
            {
                ColorLibrary.Entry e = _colors.Get(item.ColorCode);
                Vector2 range = e != null ? e.shadeOffset : Vector2.zero;
                rend.sharedMaterial = ShadeVariant(mat, range, UnityEngine.Random.Range(0, ShadeBuckets));
            }

            // Game gốc nhìn bàn từ trên xuống: lật model để mặt trên (+Y) hướng về camera (-Z).
            // Mesh CubeNew có sẵn nghiêng 10° nên vẫn lộ một chút mặt trước như trong video.
            // Mặt trên (+Y) của mesh cube hướng thẳng về camera (camera nghiêng 20° → lật -70°).
            // Mesh CubeNew có sẵn nghiêng 10° nên vẫn lộ chút mặt trước như game gốc.
            Transform model = v.transform.Find("Model");
            if (model != null) model.localRotation = item.Kind == BoardItemKind.Cube ? Quaternion.Euler(-ViewSpace.LegacyPitch, 0f, 0f) : Quaternion.identity;

            // Khối nhiều ô (CombinedCube/hòm/tường lớn): mesh gốc CombinedCubic được sinh lúc chạy bởi script đã bị
            // strip → dùng mesh cube 1 ô nhưng giãn kiểu 9-slice (giữ nguyên phần vát), tránh vát bị kéo thành bát giác.
            // Bù tỉ lệ mesh gốc pCube35 (chiều cao Y = 0.873 bề rộng X → cần nhân extents.x / extents.y = 1.145).
            float nx = size.x / _cell, ny = size.y / _cell;
            if (nx > 1.01f || ny > 1.01f)
            {
                MeshFilter mf = v.GetComponentInChildren<MeshFilter>(true);
                if (mf != null && mf.sharedMesh != null && mf.sharedMesh.isReadable)
                {
                    Bounds b = mf.sharedMesh.bounds;
                    float aspectY = b.extents.x / Mathf.Max(b.extents.y, 1e-4f);
                    mf.sharedMesh = SlicedCubeMesh(mf.sharedMesh, nx, ny * aspectY);
                }

                if (model != null) model.localRotation = Quaternion.identity; // không xoay: mesh được xiên ở Spawn (ShearForView)

                Transform cubeNew = model != null ? model.Find("CubeNew") : v.transform.Find("Model/CubeNew");
                if (cubeNew != null)
                {
                    cubeNew.localRotation = Quaternion.identity;
                    cubeNew.localScale = new Vector3(1.05f, 1.05f, 1.05f);
                }
            }

            if (item.Kind == BoardItemKind.Crate) AddCratePlanks(v.transform, size);

            // Viên đơn (1x1): co giãn ĐỀU như prefab (không ép từng trục) → giữ đúng tỉ lệ & pháp tuyến mặt vát của mesh gốc.
            // Khối nhiều ô (nx > 1 hoặc ny > 1): mesh đã được giãn 9-slice chuẩn theo tỉ lệ (nx, ny), scale ĐỒNG ĐỀU
            // theo bề ngang width = size.x để không bị méo trục Z khi Model xoay -20°, giữ mép 2 đầu chuẩn từng ô.
            if (item.Kind == BoardItemKind.Cube && nx <= 1.01f && ny <= 1.01f)
            {
                FitVisualUniform(v.transform, size.x * _config.cubeOverlap);
                return v.transform;
            }
            FitVisualUniform(v.transform, size.x);
            return v.transform;
        }

        readonly Dictionary<Vector2Int, Mesh> _slicedMeshes = new Dictionary<Vector2Int, Mesh>();

        /// <summary>
        /// Giãn mesh cube (pCube35 đứng thẳng: mặt trước -Z rộng theo x, cao theo y, sâu theo z) thành khối nx × ny ô:
        /// đỉnh trong vùng vát (20% ngoài mỗi cạnh) giữ khoảng cách tới mép, phần giữa giãn tuyến tính.
        /// </summary>
        Mesh SlicedCubeMesh(Mesh src, float nx, float ny)
        {
            var key = new Vector2Int(Mathf.RoundToInt(nx * 100f), Mathf.RoundToInt(ny * 100f));
            if (_slicedMeshes.TryGetValue(key, out Mesh cached) && cached != null) return cached;
            Bounds b = src.bounds;
            Vector3[] vs = src.vertices;
            for (int i = 0; i < vs.Length; i++)
            {
                vs[i].x = Slice(vs[i].x, b.center.x, b.extents.x, nx);
                vs[i].y = Slice(vs[i].y, b.center.y, b.extents.y, ny); // mặt trước nằm trong mặt phẳng X-Y của mesh
            }
            var m = Instantiate(src);
            m.name = src.name + $" {nx:0.##}x{ny:0.##}";
            m.vertices = vs;
            // Mesh gốc ít đỉnh: pháp tuyến góc mặt trước được làm mượt chung với mặt vát → giãn ra thì cả mặt trước bị tô như
            // mặt cong (tối, nhạt). Dựng thẳng pháp tuyến các đỉnh thuộc mặt trước (+Y), GIỮ pháp tuyến mặt cạnh/vát → mặt trước
            // tươi đúng màu, vẫn có dải mặt trên & viền 3D như game gốc.
            FlattenFrontFace(m);
            m.RecalculateBounds();
            _slicedMeshes[key] = m;
            return m;
        }

        static float Slice(float c, float center, float half, float n)
        {
            float d = c - center, band = half * 0.4f, inner = half - band, target = half * n;
            float a = Mathf.Abs(d);
            float r = a <= inner ? a * (target - band) / Mathf.Max(inner, 1e-4f) : target - (half - a);
            return center + Mathf.Sign(d) * r;
        }

        /// <summary>Hòm gỗ: phủ 3 tấm ván gốc (PlaneLeft / PlaneMid chéo / PlaneRight) của CombinedCubicCrate lên mặt trước.</summary>
        void AddCratePlanks(Transform visual, Vector2 size)
        {
            if (_config.cratePrefab == null) return;
            Transform planes = _config.cratePrefab.transform.Find("Model/Planes");
            if (planes == null) return;
            GameObject p = Instantiate(planes.gameObject, visual);
            p.name = "Planks";
            StripPhysics(p);
            // ván nằm trên mặt XZ của prefab → lật về mặt XY hướng camera, phủ toàn bộ hòm
            p.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            p.transform.localPosition = Vector3.zero;
            Bounds b = default;
            bool has = false;
            foreach (Renderer r in p.GetComponentsInChildren<Renderer>())
            {
                if (!has) { b = r.bounds; has = true; } else b.Encapsulate(r.bounds);
            }
            if (!has) return;
            var s = new Vector3(1f / Mathf.Max(b.size.x, 1e-3f), 1f / Mathf.Max(b.size.y, 1e-3f), 1f);
            p.transform.localScale = Vector3.Scale(p.transform.localScale, s);
            p.transform.localPosition = new Vector3(-(b.center.x - visual.position.x) * s.x, -(b.center.y - visual.position.y) * s.y, -0.62f);
        }

        TextMeshPro CreateLabel(Transform parent, BoardItemData item, Vector2 size)
        {
            if (_font == null) return null;
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 0f, -Mathf.Min(size.x, size.y) * 0.5f - 0.05f);
            var tmp = go.AddComponent<TextMeshPro>();
            tmp.font = _font;
            if (_fontMaterial != null) tmp.fontSharedMaterial = _fontMaterial;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = false;
            tmp.color = Color.white;
            tmp.rectTransform.sizeDelta = size;
            float minSide = Mathf.Min(size.x, size.y);
            tmp.fontSize = Mathf.Clamp(minSide * 5.5f, 1.6f, 9f);
            tmp.sortingOrder = 5;
            // viền tối để số vẫn đọc được trên khối trắng / vàng
            if (_labelMaterial == null && tmp.fontSharedMaterial != null)
            {
                _labelMaterial = new Material(tmp.fontSharedMaterial);
                _labelMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.22f);
                _labelMaterial.SetFloat(ShaderUtilities.ID_OutlineSoftness, 0.08f);
                _labelMaterial.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0.15f, 0.15f, 0.18f, 0.8f));
                _labelMaterial.EnableKeyword(ShaderUtilities.Keyword_Outline);
            }
            if (_labelMaterial != null) tmp.fontSharedMaterial = _labelMaterial;
            return tmp;
        }

        /// <summary>Scale/căn giữa visual sao cho bounds của renderer khớp đúng kích thước mong muốn.</summary>
        public static void FitVisual(Transform v, Vector3 size)
        {
            v.localScale = Vector3.one;
            v.localPosition = Vector3.zero;
            Renderer[] rs = v.GetComponentsInChildren<Renderer>(true);
            if (rs.Length == 0) return;
            Bounds b = default;
            bool has = false;
            foreach (Renderer r in rs)
            {
                if (r is ParticleSystemRenderer || r is TrailRenderer || !r.enabled || !r.gameObject.activeInHierarchy) continue;
                if (r.GetComponent<TextMeshPro>() != null) continue;
                if (!has) { b = r.bounds; has = true; } else b.Encapsulate(r.bounds);
            }
            if (!has) return;
            Vector3 bs = b.size;
            var scale = new Vector3(size.x / Mathf.Max(bs.x, 1e-4f), size.y / Mathf.Max(bs.y, 1e-4f), size.z / Mathf.Max(bs.z, 1e-4f));
            Vector3 offset = v.position - b.center;
            v.localScale = scale;
            v.localPosition = new Vector3(offset.x * scale.x, offset.y * scale.y, offset.z * scale.z);
        }

        /// <summary>Scale ĐỒNG ĐỀU sao cho bề ngang bounds = width (giữ đúng tỉ lệ mesh đã xoay, VD jelly) và căn giữa.</summary>
        public static void FitVisualUniform(Transform v, float width)
        {
            v.localScale = Vector3.one;
            v.localPosition = Vector3.zero;
            Bounds b = default;
            bool has = false;
            foreach (Renderer r in v.GetComponentsInChildren<Renderer>(true))
            {
                if (r is ParticleSystemRenderer || r is TrailRenderer || !r.enabled || !r.gameObject.activeInHierarchy) continue;
                if (r.GetComponent<TextMeshPro>() != null) continue;
                if (!has) { b = r.bounds; has = true; } else b.Encapsulate(r.bounds);
            }
            if (!has) return;
            float s = width / Mathf.Max(b.size.x, 1e-4f);
            Vector3 offset = v.position - b.center;
            v.localScale = Vector3.one * s;
            v.localPosition = offset * s;
        }

        public static void StripPhysics(GameObject go)
        {
            bool playing = Application.isPlaying;
            foreach (Collider2D c in go.GetComponentsInChildren<Collider2D>(true)) { if (playing) Destroy(c); else DestroyImmediate(c); }
            foreach (Rigidbody2D rb in go.GetComponentsInChildren<Rigidbody2D>(true)) { if (playing) Destroy(rb); else DestroyImmediate(rb); }
            foreach (Collider c in go.GetComponentsInChildren<Collider>(true)) { if (playing) Destroy(c); else DestroyImmediate(c); }
            foreach (Rigidbody rb in go.GetComponentsInChildren<Rigidbody>(true)) { if (playing) Destroy(rb); else DestroyImmediate(rb); }
            foreach (Animator a in go.GetComponentsInChildren<Animator>(true)) { if (playing) Destroy(a); else DestroyImmediate(a); }
        }

        // ------------------------------------------------------------------ Lưới
        void Occupy(BoardBlock b, bool on)
        {
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

            Vector3 pos = block.transform.position;
            if (block.Kind == BoardItemKind.Crate)
            {
                Vfx.Spawn(_config.crateBreakVfx, pos, new Color(0.85f, 0.6f, 0.35f), Mathf.Max(block.Cells.width, block.Cells.height) * _cell);
                AudioManager.Play(Sfx.CrateBreak);
                // hòm có thể đã rơi xuống → dời vật bên trong theo cùng độ lệch
                Vector2Int shift = block.Cells.min - block.OriginCells.min;
                foreach (BoardItemData inner in block.Contents)
                {
                    RectInt r = inner.Cells;
                    r.position += shift;
                    inner.Cells = r;
                    Spawn(inner);
                }
            }
            else if (block.Kind == BoardItemKind.Column)
            {
                BreakColumn(block);
            }
            else
            {
                float size = Mathf.Max(block.Cells.width, block.Cells.height) * _cell;
                EnsureBreakTune();
                Vfx.Spawn(_config.cubeBreakVfx, pos, _colors.ColorOf(block.ColorCode), size, _colors.CubeMat(block.ColorCode));
                if (block.Kind == BoardItemKind.CombinedCube) AudioManager.Play(Sfx.BigBlockBreak);
            }

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
        static readonly RaycastHit2D[] LosHits = new RaycastHit2D[4];

        /// <summary>
        /// Khối nhận màu này mà bi bay thẳng tới được (CircleCast không vướng khối khác màu/tường) – lấy theo code
        /// va chạm cũ (TryGetLineOfSightTarget): chỉ ngắm khi đường bay thông, không bẻ cong xuyên vật cản.
        /// </summary>
        public bool TryGetLineOfSightTarget(string color, Vector2 from, float radius, int mask, out Vector2 point)
        {
            point = default;
            _losCandidates.Clear();
            for (int i = 0; i < _blocks.Count; i++)
            {
                BoardBlock b = _blocks[i];
                if (b != null && !b.IsDestroyed && IsTarget(b, color) && IsExposed(b))
                    _losCandidates.Add(b);
            }
            // hết khối cùng màu → ngắm hòm gỗ còn lại (để bi phá hòm thay vì nảy vô định)
            bool aimCrates = _losCandidates.Count == 0;
            if (aimCrates)
                foreach (BoardBlock b in _blocks)
                    if (b != null && !b.IsDestroyed && b.IsFreeHit && b.IsDestructible && IsExposed(b)) _losCandidates.Add(b);
            if (_losCandidates.Count == 0) return false;

            // Tìm tối đa 5 khối gần nhất không dùng lambda sort (Zero GC)
            int checkCount = Mathf.Min(5, _losCandidates.Count);
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
                Vector2 d = target - from;
                float dist = d.magnitude;
                if (dist < 0.001f) continue;

                int hits = Physics2D.CircleCastNonAlloc(from, radius * 0.9f, d / dist, LosHits, dist, mask);
                for (int h = 0; h < hits; h++)
                {
                    if (LosHits[h].collider == null) continue;
                    BoardBlock hb = LosHits[h].collider.GetComponent<BoardBlock>();
                    if (hb != null && (IsTarget(hb, color) || (aimCrates && hb.IsFreeHit && hb.IsDestructible)))
                    {
                        point = LosHits[h].collider.bounds.center;
                        return true;
                    }
                    if (hb != null && !IsTarget(hb, color) && !(aimCrates && hb.IsFreeHit)) break;
                }
            }
            return false;
        }

        bool IsExposed(BoardBlock b)
        {
            int cols = _grid.GetLength(0), rows = _level.BoardRows;
            foreach (Vector2Int c in b.OccupiedCells())
            {
                if (c.y + 1 >= rows || Free(c.x, c.y + 1, b)) return true;
                if (c.x == 0 || Free(c.x - 1, c.y, b)) return true;
                if (c.x + 1 >= cols || Free(c.x + 1, c.y, b)) return true;
            }
            return false;
        }

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
