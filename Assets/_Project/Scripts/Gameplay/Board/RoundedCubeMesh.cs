using System.Collections.Generic;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>Mesh viên gạch 1 ô bo tròn kiểu "gối"</summary>
    public static class RoundedCubeMesh
    {
        const float HalfX = 0.5f, HalfY = 0.4365f, CenterY = 0.5591f;
        const float FrontZ = -0.484f;
        const int CornerSegs = 1, EdgeSegs = 3;

        static readonly Dictionary<(int, int, int), Mesh> Cache = new Dictionary<(int, int, int), Mesh>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetCache() => Cache.Clear();

        /// <summary>Dùng cho lấy mesh khối bo góc (có cache)</summary>
        public static Mesh Get(float corner, float edge, float depth)
        {
            corner = Mathf.Clamp(corner, 0.05f, 0.4f);
            edge = Mathf.Clamp(edge, 0.02f, corner);
            depth = Mathf.Clamp(depth, edge + 0.02f, 1.5f);
            var key = (Mathf.RoundToInt(corner * 1000f), Mathf.RoundToInt(edge * 1000f), Mathf.RoundToInt(depth * 1000f));
            if (Cache.TryGetValue(key, out Mesh m) && m != null) return m;
            m = Build(corner, edge, depth);
            Cache[key] = m;
            return m;
        }

        static Mesh Build(float R, float b, float depth)
        {
            int n = 4 * (CornerSegs + 1);
            var centers = new Vector2[n];
            var dirs = new Vector2[n];
            Vector2[] cc = { new Vector2(HalfX - R, HalfY - R), new Vector2(-(HalfX - R), HalfY - R),
                             new Vector2(-(HalfX - R), -(HalfY - R)), new Vector2(HalfX - R, -(HalfY - R)) };
            for (int c = 0; c < 4; c++)
                for (int s = 0; s <= CornerSegs; s++)
                {
                    float a = (c * 90f + 90f * s / CornerSegs) * Mathf.Deg2Rad;
                    int i = c * (CornerSegs + 1) + s;
                    centers[i] = cc[c];
                    dirs[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                }

            var V = new List<Vector3>(); var N = new List<Vector3>(); var T = new List<int>();

            var ringStart = new List<int>();
            void Ring(float inset, float z, float nOut, float nFront)
            {
                ringStart.Add(V.Count);
                for (int i = 0; i < n; i++)
                {
                    Vector2 p = centers[i] + dirs[i] * (R - inset);
                    V.Add(new Vector3(p.x, CenterY + p.y, z));
                    N.Add(new Vector3(dirs[i].x * nOut, dirs[i].y * nOut, -nFront).normalized);
                }
            }
            for (int k = 0; k <= EdgeSegs; k++)
            {
                float phi = 90f * k / EdgeSegs * Mathf.Deg2Rad;
                Ring(b * (1f - Mathf.Sin(phi)), FrontZ + b * (1f - Mathf.Cos(phi)), Mathf.Sin(phi), Mathf.Cos(phi));
            }
            Ring(0f, FrontZ + depth, 1f, 0f);

            Vector3 front = Vector3.back;
            int centerIdx = V.Count;
            V.Add(new Vector3(0f, CenterY, FrontZ)); N.Add(front);
            int faceStart = V.Count;
            for (int i = 0; i < n; i++) { V.Add(V[ringStart[0] + i]); N.Add(front); }
            for (int i = 0; i < n; i++) Tri(centerIdx, faceStart + i, faceStart + (i + 1) % n, front);

            for (int r = 0; r + 1 < ringStart.Count; r++)
                for (int i = 0; i < n; i++)
                {
                    int a0 = ringStart[r] + i, a1 = ringStart[r] + (i + 1) % n;
                    int b0 = ringStart[r + 1] + i, b1 = ringStart[r + 1] + (i + 1) % n;
                    Vector3 nrm = N[a0] + N[a1] + N[b0] + N[b1];
                    Tri(a0, b0, a1, nrm);
                    Tri(a1, b0, b1, nrm);
                }

            {
                Vector3 back = Vector3.forward;
                float z = FrontZ + depth * 0.95f, hx = HalfX, hy = HalfX;
                int b0 = V.Count;
                V.Add(new Vector3(-hx, CenterY - hy, z)); V.Add(new Vector3(hx, CenterY - hy, z));
                V.Add(new Vector3(hx, CenterY + hy, z)); V.Add(new Vector3(-hx, CenterY + hy, z));
                for (int i = 0; i < 4; i++) N.Add(back);
                Tri(b0, b0 + 1, b0 + 2, Vector3.back); Tri(b0, b0 + 2, b0 + 3, Vector3.back);
            }

            void Tri(int i0, int i1, int i2, Vector3 outward)
            {
                Vector3 cross = Vector3.Cross(V[i1] - V[i0], V[i2] - V[i0]);
                if (Vector3.Dot(cross, outward) >= 0f) { T.Add(i0); T.Add(i1); T.Add(i2); }
                else { T.Add(i0); T.Add(i2); T.Add(i1); }
            }

            var mesh = new Mesh { name = $"RoundedCube r{R:0.##} e{b:0.##}" };
            mesh.SetVertices(V);
            mesh.SetNormals(N);
            mesh.SetUVs(0, new Vector2[V.Count]);
            mesh.SetTriangles(T, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
