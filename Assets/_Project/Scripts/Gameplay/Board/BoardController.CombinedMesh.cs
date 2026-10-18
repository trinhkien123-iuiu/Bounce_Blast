using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    // Một phần của BoardController (partial) – xem BoardController.cs.
    /// <summary>Khối gộp (chữ L/T…): dựng một mesh liền theo viền và cache mesh.</summary>
    public partial class BoardController
    {
        // ------------------------------------------------------------------ Khối gộp: 1 mesh liền theo viền
        // Dải vát & độ dày khối gộp: chỉnh trong GameConfig (dải mặt trên = depth·tan20 ≈ 0.4 ô ≈ gạch 1 ô)
        readonly Dictionary<string, Mesh> _solidMeshes = new Dictionary<string, Mesh>();

        /// <summary>
        /// Khối gộp (chữ nhật, chữ L, chữ T…) dựng thành MỘT mesh: mặt trước phẳng (đa giác viền thu vào 1 dải vát),
        /// dải vát 45° + vách sau chạy liên tục quanh mép ngoài, rồi xiên theo góc nhìn như ShearForView → lộ dải mặt trên
        /// & sáng tối y như gạch 1 ô, không còn mảnh ghép lệch cỡ ở chỗ nối.
        /// </summary>
        internal Transform CreateSolidVisual(Transform parent, BoardItemData item, RectInt r, HashSet<Vector2Int> cells)
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
    }
}
