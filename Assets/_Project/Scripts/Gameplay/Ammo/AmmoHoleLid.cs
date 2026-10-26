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

        Transform _lid;
        GameObject _dark;
        // Lớp kem/thạch đọng ở mép trên miệng hố: MỘT dải liền, mép dưới lượn mềm với vài giọt tròn (như phết kem), dựng lại mỗi khung theo độ chảy
        Mesh _cream;   // một lớp, đúng màu hộp
        float _halfW, _radius, _band, _cell;
        float _phase;

        /// <param name="halfW">nửa bề ngang miệng hố</param>
        /// <param name="yBottom">mép dưới miệng hố (so với tâm ô)</param>
        /// <param name="yTop">mép trên miệng hố (so với tâm ô)</param>
        public void Init(float cell, float halfW, float yBottom, float yTop, float radius,
                         Material dark, Material jelly, Material lid, GameConfig config = null)
        {
            _config = config;
            // lòng hố tối (sau cùng)
            _dark = Part("Dark", RoundedRectMesh(-halfW, yBottom, halfW, yTop, radius, radius, radius, radius), dark, 0.004f).gameObject;

            // lớp kem đọng ở mép trên + vài giọt tròn chảy xuống
            _halfW = halfW;
            _radius = Mathf.Min(radius, halfW, (yTop - yBottom) * 0.5f);
            _band = cell * (config != null ? config.holeLidBand : 0.09f);
            var rnd = new System.Random(GetInstanceID());
            _cell = cell;
            _phase = (float)rnd.NextDouble() * 6.28f;
            _cream = new Mesh { name = "HoleCream" };
            AddCreamLayer("Cream", _cream, jelly, yTop, -0.002f);
            BuildCream(0f);

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
                BuildCream(Mathf.Clamp01(t / (LidDelay * 0.9f)), Mathf.Clamp01(t / DripIn));
                float lk = Mathf.Clamp01((t - LidDelay) / LidTime);
                _lid.localScale = new Vector3(1f, 1f - (1f - lk) * (1f - lk), 1f);
                yield return null;
            }
            _lid.localScale = Vector3.one;
            Destroy(_cream);
            Transform cr = transform.Find("Cream"); if (cr != null) Destroy(cr.gameObject);
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

        /// <summary>
        /// Dựng dải kem: mép trên bám theo mép trên miệng hố (kể cả góc bo), mép dưới = lớp nền mỏng + vài mảng lồi tròn rộng (chữ U).
        /// </summary>
        /// <param name="flow">0..1 mức đã chảy (độ sâu mảng lồi)</param>
        /// <param name="spread">0..1 lớp kem phủ ra (mỏng → đủ dày)</param>
        void BuildCream(float flow, float spread = 1f) => FillCream(_cream, flow, spread);

        // Màng mỏng lượn sóng (2 sóng sin: chậm + gợn) – độ dày thay đổi 0.4–1.6 lần
        float BandBottom(float x, float top, float spread)
        {
            float taper = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((_halfW - Mathf.Abs(x)) / (_halfW * 0.06f)));
            float wob = 1f + 0.55f * Mathf.Sin(x / _cell * 4.6f + _phase) + 0.25f * Mathf.Sin(x / _cell * 10.5f + _phase * 1.7f);
            return top - _band * wob * spread * taper;
        }

        void FillCream(Mesh mesh, float flow, float spread)
        {
            const int N = 96;
            var v = new List<Vector3>(); var tri = new List<int>();
            float flowK = Mathf.Sqrt(flow);

            void Quad(int a, int b, int c, int d)   // a,b,c,d theo vòng; 2 mặt
            {
                tri.AddRange(new[] { a, b, c, a, c, d, a, c, b, a, d, c });
            }

            // lớp kem: dải bám mép trên miệng hố
            int start = v.Count;
            for (int i = 0; i <= N; i++)
            {
                float x = Mathf.Lerp(-_halfW, _halfW, i / (float)N);
                float top = TopEdge(x, _halfW, _radius);
                float bottom = BandBottom(x, top, spread);
                v.Add(new Vector3(x, top, 0f));
                v.Add(new Vector3(x, Mathf.Min(bottom, top - 0.0005f), 0f));
                if (i > 0) { int a0 = start + (i - 1) * 2, b0 = start + i * 2; Quad(a0, b0, b0 + 1, a0 + 1); }
            }


            mesh.Clear();
            mesh.SetVertices(v);
            var n = new List<Vector3>(v.Count); for (int i = 0; i < v.Count; i++) n.Add(Vector3.back);
            mesh.SetNormals(n);
            mesh.SetTriangles(tri, 0);
            mesh.RecalculateBounds();
        }

        void AddCreamLayer(string name, Mesh mesh, Material mat, float yTop, float z)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, yTop, z);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }

        /// <summary>Độ cao mép trên miệng hố tại x (so với mép trên, ≤ 0): phẳng ở giữa, cong xuống theo góc bo.</summary>
        static float TopEdge(float x, float halfW, float r)
        {
            float d = Mathf.Abs(x) - (halfW - r);
            return d <= 0f ? 0f : -r + Mathf.Sqrt(Mathf.Max(0f, r * r - d * d));
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
