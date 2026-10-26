using System.Collections.Generic;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>Một hướng nhìn duy nhất cho cả game</summary>
    public static class ViewSpace
    {
        /// <summary>Góc nghiêng của camera gốc</summary>
        public const float LegacyPitch = 20f;
        public static readonly float LegacyTan = Mathf.Tan(LegacyPitch * Mathf.Deg2Rad);
        public static readonly float LegacyCos = Mathf.Cos(LegacyPitch * Mathf.Deg2Rad);

        /// <summary>Độ dời y mà góc nhìn cũ gây ra cho một điểm ở độ sâu z</summary>
        public static float LegacyShift(float z) => z * LegacyTan;

        static readonly HashSet<int> Flattened = new HashSet<int>();

        /// <summary>Là phẳng khung bàn tĩnh theo góc nhìn cũ</summary>
        public static void FlattenStaticScene(Transform root)
        {
            if (root == null || !Flattened.Add(root.GetInstanceID())) return;
            var animatedRoots = new List<Transform>();
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name.StartsWith("Crusher ")) animatedRoots.Add(t);
            foreach (Transform a in animatedRoots)
            {
                Transform frame = null;
                foreach (Transform c in a.GetComponentsInChildren<Transform>(true)) if (c.name == "Plane.028") frame = c;
                Renderer fr = frame != null ? frame.GetComponent<Renderer>() : null;
                Vector3 pivot = fr != null ? fr.bounds.center : a.position;
                a.RotateAround(pivot, Vector3.right, -LegacyPitch);
                a.position += new Vector3(0f, LegacyShift(pivot.z), 0f);
            }

            foreach (MeshFilter mf in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null || UnderAny(mf.transform, animatedRoots)) continue;
                if (!mf.sharedMesh.isReadable) { mf.transform.position += new Vector3(0f, LegacyShift(mf.transform.position.z), 0f); continue; }
                Matrix4x4 l2w = mf.transform.localToWorldMatrix, w2l = mf.transform.worldToLocalMatrix;
                Mesh m = Object.Instantiate(mf.sharedMesh);
                m.name = mf.sharedMesh.name + " (flat)";
                Vector3[] v = m.vertices;
                for (int i = 0; i < v.Length; i++)
                {
                    Vector3 w = l2w.MultiplyPoint3x4(v[i]);
                    w.y += LegacyShift(w.z);
                    v[i] = w2l.MultiplyPoint3x4(w);
                }
                m.vertices = v;
                m.RecalculateBounds();
                mf.sharedMesh = m;
            }
            foreach (SpriteRenderer sr in root.GetComponentsInChildren<SpriteRenderer>(true))
                if (!UnderAny(sr.transform, animatedRoots))
                    sr.transform.position += new Vector3(0f, LegacyShift(sr.transform.position.z), 0f);
        }

        static bool UnderAny(Transform t, List<Transform> roots)
        {
            foreach (Transform r in roots) if (t == r || t.IsChildOf(r)) return true;
            return false;
        }
    }
}
