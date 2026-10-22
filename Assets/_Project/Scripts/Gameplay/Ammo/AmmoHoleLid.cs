using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>
    /// Hố trống sau khi bắn một hộp (đối chiếu video gốc): lòng hố tối, thạch cùng màu hộp còn đọng ở mép trên
    /// rồi nhỏ giọt xuống; ~0.5s sau một tấm nắp màu gạch trượt từ đáy lên đậy kín hố (ô trở thành gạch phẳng).
    /// Dựng bằng mesh phẳng cùng mặt phẳng với mặt khay (AmmoGridController).
    /// </summary>
    public class AmmoHoleLid : MonoBehaviour
    {
        /// <summary>AmmoGridController gán khi Init – 4 thông số dưới chỉnh trong GameConfig.</summary>
        GameConfig _config;
        float DripIn => _config != null ? _config.holeLidDripTime : 0.18f;
        float LidDelay => _config != null ? _config.holeLidDelay : 0.55f;
        float LidTime => _config != null ? _config.holeLidTime : 0.22f;

        Transform _drips, _lid;
        GameObject _dark;
        readonly List<Transform> _strands = new List<Transform>();
        readonly List<float> _strandLen = new List<float>();

        /// <param name="halfW">nửa bề ngang miệng hố</param>
        /// <param name="yBottom">mép dưới miệng hố (so với tâm ô)</param>
        /// <param name="yTop">mép trên miệng hố (so với tâm ô)</param>
        public void Init(float cell, float halfW, float yBottom, float yTop, float radius,
                         Material dark, Material jelly, Material lid, GameConfig config = null)
        {
            _config = config;
            // lòng hố tối (sau cùng)
            _dark = Part("Dark", RoundedRectMesh(-halfW, yBottom, halfW, yTop, radius, radius, radius, radius), dark, 0.004f).gameObject;

            // thạch đọng ở mép trên + các sợi nhỏ giọt (pivot ở mép trên để kéo dài xuống)
            _drips = new GameObject("Drips").transform;
            _drips.SetParent(transform, false);
            _drips.localPosition = new Vector3(0f, yTop, -0.002f);
            float band = cell * (config != null ? config.holeLidBand : 0.09f);
            radius = Mathf.Min(radius, halfW, (yTop - yBottom) * 0.5f);
            // dải thạch ôm theo mép trên miệng hố (cạnh trên + 2 góc bo), mỏng dần xuống 2 bên
            Part("Band", TopRimMesh(halfW, radius, band), jelly, 0f, _drips);
            var rnd = new System.Random(GetInstanceID());
            float w = cell * 0.045f;
            for (float x = -halfW + cell * 0.08f; x < halfW - cell * 0.06f; x += cell * (0.07f + (float)rnd.NextDouble() * 0.05f))
            {
                float len = cell * (0.06f + (float)rnd.NextDouble() * 0.24f);
                Transform s = Part("Strand", RoundedRectMesh(x - w * 0.5f, -1f, x + w * 0.5f, 0f, w * 0.5f, 0f, 0f, w * 0.5f), jelly, 0f, _drips);
                s.localPosition = new Vector3(0f, TopEdge(x, halfW, radius) - band + 0.001f, 0f);   // treo dưới mép thạch tại x đó
                s.localScale = new Vector3(1f, 0.001f, 1f);
                _strands.Add(s);
                _strandLen.Add(len);
            }
            _drips.localScale = new Vector3(1f, 0f, 1f);

            // nắp: pivot ở mép dưới, trượt lên phủ kín
            _lid = new GameObject("Lid").transform;
            _lid.SetParent(transform, false);
            _lid.localPosition = new Vector3(0f, yBottom, -0.006f);
            Part("Plate", RoundedRectMesh(-halfW, 0f, halfW, yTop - yBottom, radius, radius, radius, radius), lid, 0f, _lid);
            _lid.localScale = new Vector3(1f, 0f, 1f);

            StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            for (float t = 0f; t < LidDelay + LidTime; t += Time.deltaTime)
            {
                if (_drips != null) _drips.localScale = new Vector3(1f, Mathf.Clamp01(t / DripIn), 1f);
                for (int i = 0; i < _strands.Count; i++)
                {
                    float k = Mathf.Clamp01((t - DripIn * 0.5f) / LidDelay);
                    _strands[i].localScale = new Vector3(1f, Mathf.Max(0.001f, _strandLen[i] * Mathf.Sqrt(k)), 1f);
                }
                float lk = Mathf.Clamp01((t - LidDelay) / LidTime);
                _lid.localScale = new Vector3(1f, 1f - (1f - lk) * (1f - lk), 1f);
                yield return null;
            }
            _lid.localScale = Vector3.one;
            if (_drips != null) Destroy(_drips.gameObject);
            if (_dark != null) Destroy(_dark);
        }

        Transform Part(string name, Mesh mesh, Material mat, float z, Transform parent = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent != null ? parent : transform, false);
            go.transform.localPosition = new Vector3(0f, 0f, z);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return go.transform;
        }

        static Mesh RoundedRectMesh(float x0, float y0, float x1, float y1, float rBR, float rTR, float rTL, float rBL)
        {
            var pts = new List<Vector2>();
            Corner(pts, new Vector2(x1, y0), rBR, -1f, 1f, 270f);
            Corner(pts, new Vector2(x1, y1), rTR, -1f, -1f, 0f);
            Corner(pts, new Vector2(x0, y1), rTL, 1f, -1f, 90f);
            Corner(pts, new Vector2(x0, y0), rBL, 1f, 1f, 180f);
            var v = new List<Vector3>();
            var tri = new List<int>();
            Vector2 c = Vector2.zero;
            foreach (Vector2 p in pts) c += p;
            v.Add(c / pts.Count);
            foreach (Vector2 p in pts) v.Add(p);
            for (int i = 0; i < pts.Count; i++)
            {
                int a = 1 + i, b = 1 + (i + 1) % pts.Count;
                tri.Add(0); tri.Add(b); tri.Add(a);
                tri.Add(0); tri.Add(a); tri.Add(b);
            }
            var m = new Mesh { name = "HoleFx" };
            m.SetVertices(v);
            var n = new List<Vector3>(); for (int i = 0; i < v.Count; i++) n.Add(Vector3.back);
            m.SetNormals(n);
            m.SetTriangles(tri, 0);
            m.RecalculateBounds();
            return m;
        }

        /// <summary>Độ cao mép trên miệng hố tại x (so với mép trên, ≤ 0): phẳng ở giữa, cong xuống theo góc bo.</summary>
        static float TopEdge(float x, float halfW, float r)
        {
            float d = Mathf.Abs(x) - (halfW - r);
            return d <= 0f ? 0f : -r + Mathf.Sqrt(Mathf.Max(0f, r * r - d * d));
        }

        /// <summary>Dải cong dày "band" bám mép trên miệng hố: đường ngoài = mép trên + 2 cung góc, đường trong = đường ngoài dời xuống band.</summary>
        static Mesh TopRimMesh(float halfW, float r, float band)
        {
            const int N = 24;
            var v = new List<Vector3>(); var tri = new List<int>();
            for (int i = 0; i <= N; i++)
            {
                // tham số theo cung: góc trái (180°→90°), cạnh thẳng, góc phải (90°→0°)
                float t = i / (float)N, x, y;
                float arc = Mathf.PI * 0.5f * r, flat = 2f * (halfW - r), len = 2f * arc + flat, u = t * len;
                if (u < arc) { float a = Mathf.PI - u / r; x = -(halfW - r) + Mathf.Cos(a) * r; y = -r + Mathf.Sin(a) * r; }
                else if (u < arc + flat) { x = -(halfW - r) + (u - arc); y = 0f; }
                else { float a = Mathf.PI * 0.5f - (u - arc - flat) / r; x = (halfW - r) + Mathf.Cos(a) * r; y = -r + Mathf.Sin(a) * r; }
                float k = Mathf.Clamp01(Mathf.Min(t, 1f - t) * 6f);   // 2 đầu mỏng dần
                v.Add(new Vector3(x, y, 0f));
                v.Add(new Vector3(x, y - band * k, 0f));
                if (i > 0)
                {
                    int a0 = (i - 1) * 2, b0 = i * 2;
                    tri.AddRange(new[] { a0, b0, a0 + 1, b0, b0 + 1, a0 + 1, a0, a0 + 1, b0, b0, a0 + 1, b0 + 1 });   // 2 mặt
                }
            }
            var m = new Mesh { name = "HoleJellyRim" };
            m.SetVertices(v);
            var n = new List<Vector3>(); for (int i = 0; i < v.Count; i++) n.Add(Vector3.back);
            m.SetNormals(n);
            m.SetTriangles(tri, 0);
            m.RecalculateBounds();
            return m;
        }

        static void Corner(List<Vector2> pts, Vector2 corner, float r, float sx, float sy, float startDeg)
        {
            if (r <= 0f) { pts.Add(corner); return; }
            Vector2 c = corner + new Vector2(sx * r, sy * r);
            for (int i = 0; i <= 5; i++)
            {
                float a = (startDeg + 90f * i / 5f) * Mathf.Deg2Rad;
                pts.Add(c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r);
            }
        }
    }
}
