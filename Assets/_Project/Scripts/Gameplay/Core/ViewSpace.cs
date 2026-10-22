using System.Collections.Generic;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>
    /// Một hướng nhìn duy nhất cho cả game: camera ortho NHÌN THẲNG (không nghiêng), mặt phẳng chơi = XY, z hướng vào màn hình.
    ///
    /// Game gốc dùng camera nghiêng xuống 20°; trên camera đó một điểm (x, y, z) hiện ở độ cao y + z·tan20 (so với mặt z = 0).
    /// Khung bàn 3D gốc (Level/Board - Default) được dựng cho góc nhìn đó, nên lúc khởi động ta "là phẳng" nó một lần:
    /// dời từng đỉnh mesh y += z·tan20 (giữ nguyên z → không che vật phía trước) → trên camera thẳng trông y hệt bản gốc.
    /// Vật dựng lúc chạy (cube, hộp đạn, cột, khay…) thì tự quay mặt về camera, không cần bù gì thêm.
    /// </summary>
    public static class ViewSpace
    {
        /// <summary>Góc nghiêng của camera gốc – chỉ dùng để chuyển dữ liệu/khung đo theo góc nhìn cũ.</summary>
        public const float LegacyPitch = 20f;
        public static readonly float LegacyTan = Mathf.Tan(LegacyPitch * Mathf.Deg2Rad);
        public static readonly float LegacyCos = Mathf.Cos(LegacyPitch * Mathf.Deg2Rad);

        /// <summary>Độ dời y mà góc nhìn cũ gây ra cho một điểm ở độ sâu z.</summary>
        public static float LegacyShift(float z) => z * LegacyTan;

        static readonly HashSet<int> Flattened = new HashSet<int>(); // theo instance: scene nạp lại thì là phẳng lại

        /// <summary>
        /// Là phẳng khung bàn tĩnh theo góc nhìn cũ (gọi 1 lần lúc khởi động, trước khi đọc vị trí máy xay/máng).
        /// Mesh tĩnh: dời đỉnh. Vật có chuyển động riêng (lưỡi máy xay quay, sprite): dời transform theo tâm.
        /// </summary>
        public static void FlattenStaticScene(Transform root)
        {
            if (root == null || !Flattened.Add(root.GetInstanceID())) return;
            // Máy xay (Crusher LEFT/RIGHT): là phẳng thì khung bị vênh/méo → XOAY CẢ KHỐI -20° quanh tâm của nó cho mặt trước
            // hướng thẳng camera (y như trên camera nghiêng cũ), rồi dời y theo độ sâu tâm. Lưỡi cắt quay theo cùng khối.
            var animatedRoots = new List<Transform>();
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name.StartsWith("Crusher ")) animatedRoots.Add(t);
            foreach (Transform a in animatedRoots)
            {
                Transform frame = null;
                foreach (Transform c in a.GetComponentsInChildren<Transform>(true)) if (c.name == "Plane.028") frame = c;
                Renderer fr = frame != null ? frame.GetComponent<Renderer>() : null;
                Vector3 pivot = fr != null ? fr.bounds.center : a.position;   // tâm khung (tĩnh), không dùng bounds lưỡi đang quay
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
