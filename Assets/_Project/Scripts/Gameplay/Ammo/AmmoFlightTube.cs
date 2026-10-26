using System.Collections.Generic;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>Thân hộp thạch khi bay lên máng</summary>
    public class AmmoFlightTube : MonoBehaviour
    {
        const int Bones = 12;

        static readonly Dictionary<Mesh, Mesh> SkinnedCache = new Dictionary<Mesh, Mesh>();
        static readonly Dictionary<Mesh, Vector2> AxisCache = new Dictionary<Mesh, Vector2>();

        Transform[] _bones;
        float _yMin, _yMax, _cross;
        Vector3[] _path;
        float[] _cum;

        public float Length => _cum != null ? _cum[_cum.Length - 1] : 0f;

        /// <summary>Dựng ống bay từ mesh hộp nguồn</summary>
        public static AmmoFlightTube Create(Renderer source, MaterialPropertyBlock mpb)
        {
            var mf = source != null ? source.GetComponent<MeshFilter>() : null;
            Mesh src = mf != null ? mf.sharedMesh : null;
            var go = new GameObject("JellyBody");
            var tube = go.AddComponent<AmmoFlightTube>();
            if (src == null) return tube;

            Mesh skinned = GetSkinned(src);
            Vector2 ax = AxisCache[src];
            tube._yMin = ax.x; tube._yMax = ax.y;
            tube._cross = Mathf.Max(src.bounds.size.x, src.bounds.size.z);

            tube._bones = new Transform[Bones];
            for (int i = 0; i < Bones; i++)
            {
                tube._bones[i] = new GameObject("Bone" + i).transform;
                tube._bones[i].SetParent(go.transform, false);
            }
            var smr = go.AddComponent<SkinnedMeshRenderer>();
            smr.sharedMesh = skinned;
            smr.bones = tube._bones;
            smr.rootBone = tube._bones[0];
            smr.updateWhenOffscreen = true;
            smr.quality = SkinQuality.Bone2;
            smr.sharedMaterial = source.sharedMaterial;
            smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            smr.receiveShadows = false;
            smr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            smr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            go.layer = source.gameObject.layer;
            if (mpb != null) smr.SetPropertyBlock(mpb);
            return tube;
        }

        /// <summary>Dựng sẵn mesh gắn xương (cache dùng chung) lúc nạp level để lần bấm hộp đầu tiên không bị khựng trên máy yếu</summary>
        public static void Prewarm(Renderer source)
        {
            var mf = source != null ? source.GetComponent<MeshFilter>() : null;
            if (mf != null && mf.sharedMesh != null) GetSkinned(mf.sharedMesh);
        }

        static Mesh GetSkinned(Mesh src)
        {
            if (SkinnedCache.TryGetValue(src, out Mesh m) && m != null) return m;
            m = Subdivide(src, 10);
            m.name = src.name + " (skinned)";
            Vector3[] v = m.vertices;
            float yMin = float.MaxValue, yMax = float.MinValue;
            foreach (Vector3 p in v) { yMin = Mathf.Min(yMin, p.y); yMax = Mathf.Max(yMax, p.y); }
            var w = new BoneWeight[v.Length];
            for (int i = 0; i < v.Length; i++)
            {
                float f = Mathf.InverseLerp(yMin, yMax, v[i].y) * (Bones - 1);
                int a = Mathf.Clamp(Mathf.FloorToInt(f), 0, Bones - 2);
                float t = Mathf.Clamp01(f - a);
                w[i] = new BoneWeight { boneIndex0 = a, weight0 = 1f - t, boneIndex1 = a + 1, weight1 = t };
            }
            m.boneWeights = w;
            var bind = new Matrix4x4[Bones];
            for (int i = 0; i < Bones; i++)
                bind[i] = Matrix4x4.Translate(new Vector3(0f, Mathf.Lerp(yMin, yMax, i / (float)(Bones - 1)), 0f)).inverse;
            m.bindposes = bind;
            SkinnedCache[src] = m;
            AxisCache[src] = new Vector2(yMin, yMax);
            return m;
        }

        /// <summary>Mesh gốc Cube_1 chỉ có đỉnh ở 2 đầu cột</summary>
        static Mesh Subdivide(Mesh src, int slices)
        {
            var pos = new List<Vector3>(src.vertices);
            var nrm = new List<Vector3>(src.normals);
            var uv = new List<Vector2>(src.uv);
            var col = new List<Color>(src.colors);
            var tan = new List<Vector4>(src.tangents);
            bool hasN = nrm.Count == pos.Count, hasUv = uv.Count == pos.Count, hasC = col.Count == pos.Count, hasT = tan.Count == pos.Count;
            float maxDy = src.bounds.size.y / slices;
            var mid = new Dictionary<long, int>();
            int Mid(int a, int b)
            {
                long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
                if (mid.TryGetValue(key, out int i)) return i;
                pos.Add((pos[a] + pos[b]) * 0.5f);
                if (hasN) nrm.Add((nrm[a] + nrm[b]).normalized);
                if (hasUv) uv.Add((uv[a] + uv[b]) * 0.5f);
                if (hasC) col.Add((col[a] + col[b]) * 0.5f);
                if (hasT) tan.Add((tan[a] + tan[b]) * 0.5f);
                i = pos.Count - 1;
                mid[key] = i;
                return i;
            }
            var subs = new List<int[]>();
            for (int sm = 0; sm < src.subMeshCount; sm++)
            {
                var tris = new List<int>(src.GetTriangles(sm));
                for (int pass = 0; pass < 12; pass++)
                {
                    var o = new List<int>(tris.Count * 2);
                    bool changed = false;
                    for (int t = 0; t < tris.Count; t += 3)
                    {
                        int i0 = tris[t], i1 = tris[t + 1], i2 = tris[t + 2];
                        float d01 = Mathf.Abs(pos[i0].y - pos[i1].y), d12 = Mathf.Abs(pos[i1].y - pos[i2].y), d20 = Mathf.Abs(pos[i2].y - pos[i0].y);
                        float dm = Mathf.Max(d01, Mathf.Max(d12, d20));
                        if (dm <= maxDy) { o.Add(i0); o.Add(i1); o.Add(i2); continue; }
                        changed = true;
                        if (dm == d01) { int m01 = Mid(i0, i1); o.Add(i0); o.Add(m01); o.Add(i2); o.Add(m01); o.Add(i1); o.Add(i2); }
                        else if (dm == d12) { int m12 = Mid(i1, i2); o.Add(i0); o.Add(i1); o.Add(m12); o.Add(i0); o.Add(m12); o.Add(i2); }
                        else { int m20 = Mid(i2, i0); o.Add(i0); o.Add(i1); o.Add(m20); o.Add(m20); o.Add(i1); o.Add(i2); }
                    }
                    tris = o;
                    if (!changed) break;
                }
                subs.Add(tris.ToArray());
            }
            var m = new Mesh { indexFormat = pos.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16 };
            m.SetVertices(pos);
            if (hasN) m.SetNormals(nrm);
            if (hasUv) m.SetUVs(0, uv);
            if (hasC) m.SetColors(col);
            if (hasT) m.SetTangents(tan);
            m.subMeshCount = subs.Count;
            for (int i = 0; i < subs.Count; i++) m.SetTriangles(subs[i], i);
            m.RecalculateBounds();
            return m;
        }

        /// <summary>Quỹ đạo của ĐẦU ống: các điểm gấp khúc, mỗi góc bo bằng CUNG TRÒN bán kính ≥ minRadius</summary>
        public void SetPath(IList<Vector3> corners, float minRadius)
        {
            var pts = new List<Vector3> { corners[0] };
            for (int i = 1; i < corners.Count - 1; i++)
            {
                Vector3 p0 = corners[i - 1], p1 = corners[i], p2 = corners[i + 1];
                Vector3 d0 = (p1 - p0).normalized, d1 = (p2 - p1).normalized;
                float turn = Vector3.Angle(d0, d1) * Mathf.Deg2Rad;
                if (turn < 1e-3f) { pts.Add(p1); continue; }
                float maxCut = 0.45f * Mathf.Min(Vector3.Distance(p0, p1), Vector3.Distance(p1, p2));
                float r = Mathf.Min(minRadius, maxCut / Mathf.Tan(turn * 0.5f));
                float cut = r * Mathf.Tan(turn * 0.5f);
                Vector3 a0 = p1 - d0 * cut, a1 = p1 + d1 * cut;
                Vector3 n = Vector3.Cross(Vector3.Cross(d0, d1), d0).normalized;
                Vector3 c = a0 + n * r;
                int steps = Mathf.Max(4, Mathf.CeilToInt(turn * 12f));
                for (int k = 0; k <= steps; k++)
                    pts.Add(c + Vector3.Slerp(a0 - c, a1 - c, k / (float)steps));
            }
            pts.Add(corners[corners.Count - 1]);
            _path = pts.ToArray();
            _cum = new float[_path.Length];
            for (int i = 1; i < _path.Length; i++) _cum[i] = _cum[i - 1] + Vector3.Distance(_path[i - 1], _path[i]);
        }

        /// <summary>Dùng cho lấy điểm trên đường bay theo quãng đường</summary>
        public Vector3 PointAt(float s)
        {
            int i = 1;
            return PointAt(s, ref i);
        }

        Vector3 PointAt(float s, ref int i)
        {
            s = Mathf.Clamp(s, 0f, Length);
            while (i < _cum.Length - 1 && _cum[i] < s) i++;
            float seg = _cum[i] - _cum[i - 1];
            return Vector3.Lerp(_path[i - 1], _path[i], seg > 1e-5f ? (s - _cum[i - 1]) / seg : 0f);
        }

        readonly Vector3[] _pos = new Vector3[Bones];

        /// <summary>Uốn thân theo quỹ đạo từ tail tới head</summary>
        public void Build(float tail, float head, float width, Quaternion startRot, Quaternion endRot, float toEnd = 0f)
        {
            if (_bones == null) return;
            if (head - tail < 1e-3f) head = tail + 1e-3f;
            int idx = 1;
            float totalLen = Length > 0.01f ? Length : 1f;
            for (int i = 0; i < Bones; i++)
            {
                float sPos = Mathf.Lerp(tail, head, i / (float)(Bones - 1));
                _pos[i] = PointAt(sPos, ref idx);
            }
            float s = width / Mathf.Max(1e-4f, _cross);
            for (int i = 0; i < Bones; i++)
            {
                Vector3 t = _pos[Mathf.Min(i + 1, Bones - 1)] - _pos[Mathf.Max(i - 1, 0)];
                if (t.sqrMagnitude < 1e-10f) t = Vector3.up;
                t.Normalize();
                float u = Mathf.Clamp01((Mathf.Lerp(tail, head, i / (float)(Bones - 1))) / totalLen);
                Quaternion rot = Quaternion.Slerp(startRot, endRot, u);
                if (toEnd > 0f) rot = Quaternion.Slerp(rot, endRot, toEnd);
                Vector3 targetFwd = rot * Vector3.forward;
                Vector3 fwd = targetFwd - t * Vector3.Dot(targetFwd, t);
                if (fwd.sqrMagnitude < 1e-6f) fwd = rot * Vector3.forward;
                _bones[i].SetPositionAndRotation(_pos[i], Quaternion.LookRotation(fwd, t));
                _bones[i].localScale = new Vector3(s, s, s);
            }
        }

        /// <summary>Xử lý dựng lại mesh ống theo đuôi, đầu và bề ngang</summary>
        public void Build(float tail, float head, float width)
        {
            Build(tail, head, width, Quaternion.identity, Quaternion.identity);
        }

        /// <summary>Thân đậu ở máy xay: đáy ở bottom, cao height, xoay khớp theo góc nghiêng của máy xay</summary>
        public static bool HeadAtBottom = true;

        /// <summary>Xử lý đặt ống thẳng đứng tại máy xay</summary>
        public void PoseStraight(Vector3 bottom, float height, float width, Quaternion rotation)
        {
            if (_bones == null) return;
            Vector3 up = rotation * Vector3.up;
            Vector3 fwd = rotation * Vector3.forward;
            float s = width / Mathf.Max(1e-4f, _cross);
            Quaternion rot = HeadAtBottom ? Quaternion.LookRotation(fwd, -up) : Quaternion.LookRotation(fwd, up);
            for (int i = 0; i < Bones; i++)
            {
                float f = HeadAtBottom ? (Bones - 1 - i) : i;
                _pos[i] = bottom + up * (height * f / (Bones - 1));
                _bones[i].SetPositionAndRotation(_pos[i], rot);
                _bones[i].localScale = new Vector3(s, s, s);
            }
        }

        /// <summary>Pha lắng cuối chuyến bay: kéo từng xương</summary>
        public void BlendToStraight(Vector3 bottom, float height, Quaternion rotation, float blend)
        {
            if (_bones == null || blend <= 0f) return;
            Vector3 up = rotation * Vector3.up;
            Vector3 fwd = rotation * Vector3.forward;
            Quaternion rot = HeadAtBottom ? Quaternion.LookRotation(fwd, -up) : Quaternion.LookRotation(fwd, up);
            for (int i = 0; i < Bones; i++)
            {
                float f = HeadAtBottom ? (Bones - 1 - i) : i;
                Vector3 target = bottom + up * (height * f / (Bones - 1));
                _bones[i].SetPositionAndRotation(Vector3.Lerp(_bones[i].position, target, blend), Quaternion.Slerp(_bones[i].rotation, rot, blend));
            }
        }

        /// <summary>Xử lý đặt ống thẳng đứng tại máy xay</summary>
        public void PoseStraight(Vector3 bottom, float height, float width)
        {
            PoseStraight(bottom, height, width, Quaternion.identity);
        }
    }
}
