using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>
    /// Dựng HÌNH cho khối trên bàn (không có luật chơi): gạch 1 ô từ prefab cube gốc (mesh bo tròn, vân sáng tối),
    /// khối nhiều ô giãn 9-slice, khối hình chữ L… thành một mesh liền theo viền, xiên mesh theo góc nhìn, nhãn số máu.
    /// Cache mesh dùng chung giữa các màn. <see cref="BoardController"/> giữ một bản; các lớp loại khối (Board/Kinds) gọi qua board.Visuals.
    /// </summary>
    public class BlockVisualFactory
    {
        GameConfig _config;
        ColorLibrary _colors;
        float _cell;
        TMP_FontAsset _font;
        Material _fontMaterial;
        Material _labelMaterial;

        /// <summary>Gọi mỗi lần dựng màn (cỡ ô đổi theo số cột của level).</summary>
        public void Setup(GameConfig config, ColorLibrary colors, float cell)
        {
            _config = config;
            _colors = colors;
            _cell = cell;
            // TMP trong prefab rip bị mất script → dùng font của GameConfig
            _font = config.worldFont;
            _fontMaterial = _font != null ? _font.material : null;
        }

        /// <summary>Khối nhiều ô / tường: xiên mesh thay vì xoay (xem ShearForView) → mặt trước phẳng khít lưới như gạch 1 ô.</summary>
        public void ApplyViewShear(Transform visual)
        {
            if (visual == null) return;
            foreach (MeshFilter smf in visual.GetComponentsInChildren<MeshFilter>(true))
                if (smf.sharedMesh != null && smf.sharedMesh.isReadable && smf.GetComponent<TextMeshPro>() == null && !smf.sharedMesh.name.StartsWith("Solid"))
                    smf.sharedMesh = ShearForView(smf.sharedMesh);
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

        public Transform CreateVisual(Transform parent, BoardItemData item, Vector2 size, float forcedDepth = -1f)
        {
            GameObject v = Object.Instantiate(_config.cubePrefab, parent);
            v.name = "Visual";
            ModelUtil.StripPhysics(v);
            foreach (TextMeshPro t in v.GetComponentsInChildren<TextMeshPro>(true)) Object.Destroy(t.gameObject);
            ModelUtil.RemoveStrayText(v.transform);

            Renderer rend = v.GetComponentInChildren<MeshRenderer>(true);
            BlockKind kind = BlockKinds.Of(item.Kind);
            Material mat = kind.Material(_config, _colors, item);
            if (rend != null && mat != null) rend.sharedMaterial = mat;

            // Vân sáng/tối ngẫu nhiên cho từng cube nhỏ như game gốc (Colors_v3: MinOffset..MaxOffset).
            // Tối ưu: shader rip không có biến thể instancing → MaterialPropertyBlock làm mỗi cube 1 draw call riêng.
            // Thay bằng vài material dùng chung theo (màu, mức vân) → cube cùng material được dynamic batching gộp lại.
            if (rend != null && kind.CubeLook && mat != null)
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
            if (model != null) model.localRotation = kind.CubeLook ? Quaternion.Euler(-ViewSpace.LegacyPitch, 0f, 0f) : Quaternion.identity;

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


            // Viên đơn (1x1): co giãn ĐỀU như prefab (không ép từng trục) → giữ đúng tỉ lệ & pháp tuyến mặt vát của mesh gốc.
            // Khối nhiều ô (nx > 1 hoặc ny > 1): mesh đã được giãn 9-slice chuẩn theo tỉ lệ (nx, ny), scale ĐỒNG ĐỀU
            // theo bề ngang width = size.x để không bị méo trục Z khi Model xoay -20°, giữ mép 2 đầu chuẩn từng ô.
            if (kind.CubeLook && nx <= 1.01f && ny <= 1.01f)
            {
                if (_config.cubeCornerRadius > 0f)
                {
                    MeshFilter round = v.GetComponentInChildren<MeshFilter>(true);
                    if (round != null) round.sharedMesh = RoundedCubeMesh.Get(_config.cubeCornerRadius, _config.cubeEdgeRound, _config.cubeBodyDepth);
                }
                ModelUtil.FitVisualUniform(v.transform, size.x * _config.cubeOverlap);
                return v.transform;
            }
            ModelUtil.FitVisualUniform(v.transform, size.x);
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
            var m = Object.Instantiate(src);
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

        public TextMeshPro CreateLabel(Transform parent, BoardItemData item, Vector2 size)
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
                _labelMaterial.SetFloat(ShaderUtilities.ID_FaceDilate, 0.18f);                 // chữ đậm hơn
                _labelMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.2f);
                _labelMaterial.SetFloat(ShaderUtilities.ID_OutlineSoftness, 0f);                // viền sắc, không nhoè
                _labelMaterial.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0.1f, 0.1f, 0.14f, 1f));
                _labelMaterial.EnableKeyword(ShaderUtilities.Keyword_Outline);
            }
            if (_labelMaterial != null) tmp.fontSharedMaterial = _labelMaterial;
            return tmp;
        }

        // Dải vát & độ dày khối gộp: chỉnh trong GameConfig (dải mặt trên = depth·tan20 ≈ 0.4 ô ≈ gạch 1 ô)
        readonly Dictionary<string, Mesh> _solidMeshes = new Dictionary<string, Mesh>();

        /// <summary>
        /// Khối gộp (chữ nhật, chữ L, chữ T…) dựng thành MỘT mesh: mặt trước phẳng (đa giác viền thu vào 1 dải vát),
        /// dải vát 45° + vách sau chạy liên tục quanh mép ngoài, rồi xiên theo góc nhìn như ShearForView → lộ dải mặt trên
        /// & sáng tối y như gạch 1 ô, không còn mảnh ghép lệch cỡ ở chỗ nối.
        /// </summary>
        public Transform CreateSolidVisual(Transform parent, BoardItemData item, RectInt r, HashSet<Vector2Int> cells)
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

        readonly Dictionary<Mesh, Mesh> _shearedMeshes = new Dictionary<Mesh, Mesh>();

        /// <summary>
        /// Gạch 1 ô ngả -20° quanh tâm nên lộ dải mặt trên. Khối to mà xoay nguyên khối thì 2 đầu chìa khỏi mặt phẳng bàn
        /// (không khít). Thay vào đó XIÊN mesh: giữ mặt trước (z min) đúng chỗ, đẩy phần sâu lên trên y += (z - zFront)·tan20,
        /// pháp tuyến xoay Rx(-20) → nhìn thẳng thấy dải mặt trên & ánh sáng y hệt gạch 1 ô, mặt trước vẫn phẳng khít lưới.
        /// </summary>
        Mesh ShearForView(Mesh src)
        {
            if (_shearedMeshes.TryGetValue(src, out Mesh cached) && cached != null) return cached;
            var m = Object.Instantiate(src);
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
        /// Khối lớn trong game gốc là tấm phẳng đồng màu (mesh CombinedCubic sinh lúc chạy). Mesh cube 1 ô có pháp tuyến bo
        /// tròn, giãn to thì lộ dải sáng-tối → đặt mọi pháp tuyến = mặt trên (+Y mesh) cho màu đồng đều.
        /// </summary>
        static void FlattenFrontFace(Mesh m)
        {
            Vector3[] n = m.normals;
            for (int i = 0; i < n.Length; i++) if (n[i].z < -0.9f) n[i] = Vector3.back; // chỉ mặt trước (-Z); mép vát (z≈-0.7) giữ nguyên → khối lớn có viền 3D
            m.normals = n;
        }

        /// <summary>Chia tập ô thành ít hình chữ nhật lớn (quét trái→phải, kéo dài ngang rồi dọc).</summary>
        public static List<RectInt> Decompose(List<Vector2Int> cells, int w, int h)
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

        /// <summary>
        /// Đường dựng mặc định cho khối theo prefab cube: khối chữ nhật → <see cref="CreateVisual"/> (gạch 1 ô / 9-slice);
        /// khối có hình dạng (chữ L…) → một mesh liền theo viền + collider từng hình chữ nhật con, nhãn số đặt ở phần lớn nhất.
        /// </summary>
        public BlockView BuildDefaultView(BoardItemData item, Transform parent, Vector2 size, List<RectInt> parts, bool withLabel)
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
    }
}
