using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>
    /// Dựng DỰ PHÒNG đèn riêng cho gạch khi scene chưa đặt sẵn (camera UI giờ bắt buộc đặt sẵn trong scene).
    /// Scene Gameplay hiện đã có sẵn nên thường không làm gì; tách khỏi GameManager để GameManager chỉ điều phối.
    /// </summary>
    public static class SceneRig
    {
        /// <summary>
        /// Gạch/khối bàn quay thêm -20° để nhìn thẳng camera (camera gốc nghiêng 20°) → đèn chiếu gạch phải xoay bù +20°
        /// thì ánh sáng trên mặt gạch mới y hệt game gốc.
        /// DỰ PHÒNG: nếu scene chưa có đèn riêng cho layer Cube thì tạo từ đèn chung (xoay +20° cho khớp gạch ngả −20°).
        /// Scene hiện tại đã có sẵn đèn "(Cube, view-aligned)" nên hàm này không làm gì. Đèn riêng của thạch cũng đã nung sẵn trong scene.
        /// </summary>
        public static void EnsureCubeLight(Transform root)
        {
            int cube = LayerMask.NameToLayer("Cube");
            if (root == null || cube < 0) return;
            foreach (Light src in root.GetComponentsInChildren<Light>(true))
            {
                if (src.type != LightType.Directional || (src.cullingMask & (1 << cube)) == 0 || src.cullingMask == 1 << cube) continue;
                var go = new GameObject(src.name + " (Cube, view-aligned)");
                go.transform.SetParent(src.transform.parent, false);
                go.transform.rotation = Quaternion.Euler(ViewSpace.LegacyPitch, 0f, 0f) * src.transform.rotation; // gạch đứng ngả -20° + đèn +20° (đo Lv44: S 197/199)
                var l = go.AddComponent<Light>();
                l.type = LightType.Directional;
                l.color = src.color;
                l.intensity = src.intensity;
                l.shadows = src.shadows;
                l.renderMode = src.renderMode;
                l.cullingMask = 1 << cube;
                src.cullingMask &= ~(1 << cube);
                break;
            }
        }
    }
}
