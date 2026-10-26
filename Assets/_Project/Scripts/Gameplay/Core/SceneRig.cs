using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>Dựng DỰ PHÒNG đèn riêng cho gạch khi scene chưa đặt sẵn</summary>
    public static class SceneRig
    {
        /// <summary>Gạch/khối bàn quay thêm -20° để nhìn thẳng camera</summary>
        public static void EnsureCubeLight(Transform root)
        {
            int cube = LayerMask.NameToLayer("Cube");
            if (root == null || cube < 0) return;
            foreach (Light src in root.GetComponentsInChildren<Light>(true))
            {
                if (src.type != LightType.Directional || (src.cullingMask & (1 << cube)) == 0 || src.cullingMask == 1 << cube) continue;
                var go = new GameObject(src.name + " (Cube, view-aligned)");
                go.transform.SetParent(src.transform.parent, false);
                go.transform.rotation = Quaternion.Euler(ViewSpace.LegacyPitch, 0f, 0f) * src.transform.rotation;
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
