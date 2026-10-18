using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>Tiện ích cho prefab rip từ game gốc (dùng chung cho gạch, hộp đạn, bi, hòm…): gỡ vật lý/TMP hỏng, căn cỡ hình.</summary>
    public static class ModelUtil
    {
        /// <summary>Prefab gốc còn "3D Text/Text (TMP)": component TMP đã bị rip mất, chỉ còn MeshRenderer rỗng → xoá.</summary>
        public static void RemoveStrayText(Transform root)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t != null && t.name == "3D Text") { Object.Destroy(t.gameObject); }
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
            foreach (Collider2D c in go.GetComponentsInChildren<Collider2D>(true)) { if (playing) Object.Destroy(c); else Object.DestroyImmediate(c); }
            foreach (Rigidbody2D rb in go.GetComponentsInChildren<Rigidbody2D>(true)) { if (playing) Object.Destroy(rb); else Object.DestroyImmediate(rb); }
            foreach (Collider c in go.GetComponentsInChildren<Collider>(true)) { if (playing) Object.Destroy(c); else Object.DestroyImmediate(c); }
            foreach (Rigidbody rb in go.GetComponentsInChildren<Rigidbody>(true)) { if (playing) Object.Destroy(rb); else Object.DestroyImmediate(rb); }
            foreach (Animator a in go.GetComponentsInChildren<Animator>(true)) { if (playing) Object.Destroy(a); else Object.DestroyImmediate(a); }
        }
    }
}
