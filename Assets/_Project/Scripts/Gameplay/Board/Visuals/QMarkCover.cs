using System.Collections.Generic;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>Lớp phủ dấu "?" của BOARD QMARK (/bqm), vẽ bằng shader Custom/HiddenBlocker (dấu "?" theo toạ độ thế giới nên liền mạch giữa các khối kề nhau).
    /// Khối hình chữ nhật: một tấm hộp phẳng phủ kín cả vùng các ô (không bo tròn từng viên nên không hở khe); khối hình chữ L…: bản sao mesh to 1.005× (QMarkScaleMultiplier của game gốc).
    /// Khi lộ màu thì _DissolveAmount 0→1 cho tan biến, để lộ màu thật bên dưới</summary>
    public class QMarkCover : MonoBehaviour
    {
        const float ScaleMultiplier = 1.005f;
        const string DissolveProperty = "_DissolveAmount";
        /// <summary>Độ dày tấm phủ, theo bề rộng khối (rất mỏng, chỉ để nằm sát mặt trước)</summary>
        const float PlateDepthRatio = 0.02f;

        static readonly Dictionary<(Material, Color), Material> Tinted = new Dictionary<(Material, Color), Material>();
        static Mesh _box;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetCache() { Tinted.Clear(); _box = null; }

        readonly List<MeshRenderer> _renderers = new List<MeshRenderer>();
        Material _shared;
        Material _instance;
        float _seconds = 0.6f;
        float _t = -1f;

        /// <summary>Phủ dấu "?" lên visual; plate = true thì dùng một tấm phẳng phủ kín vùng chữ nhật, false thì sao chép mesh</summary>
        public static QMarkCover Attach(Transform visual, Material material, Color background, float glossiness, int layer, float dissolveSeconds, bool plate)
        {
            if (visual == null || material == null) return null;
            var cover = visual.gameObject.AddComponent<QMarkCover>();
            cover._shared = TintedMaterial(material, background, glossiness);
            cover._seconds = Mathf.Max(0.05f, dissolveSeconds);

            if (plate && cover.TryAddPlate(visual, layer)) return cover;

            var sources = new List<MeshFilter>(visual.GetComponentsInChildren<MeshFilter>(true));
            foreach (MeshFilter mf in sources)
            {
                MeshRenderer src = mf.GetComponent<MeshRenderer>();
                if (src == null || !src.enabled || mf.sharedMesh == null || mf.name == "QMarkCover") continue;
                var go = new GameObject("QMarkCover") { layer = layer };
                go.transform.SetParent(mf.transform, false);
                go.transform.localScale = Vector3.one * ScaleMultiplier;
                go.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
                cover.AddRenderer(go);
            }
            return cover;
        }

        /// <summary>Nền "?" của material gốc là đen (_Color 0); video gốc đo được xám xanh nên dùng màu nền chỉnh được</summary>
        static Material TintedMaterial(Material src, Color background, float glossiness)
        {
            if (!src.HasProperty("_Color")) return src;
            if (Tinted.TryGetValue((src, background), out Material m) && m != null) return m;
            m = new Material(src) { name = src.name + " (nền)" };
            m.SetColor("_Color", background);
            // shader HiddenBlocker tính phản xạ môi trường theo vị trí → khối ở trên sáng hơn khối ở dưới; tắt bóng + thống nhất màu bóng đổ (2 material gốc đặt khác nhau)
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", glossiness);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
            if (m.HasProperty("_ShadowColor")) m.SetColor("_ShadowColor", background);
            Tinted[(src, background)] = m;
            return m;
        }

        void AddRenderer(GameObject go)
        {
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = _shared;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            _renderers.Add(mr);
        }

        /// <summary>Tấm hộp phẳng phủ đúng khung (x, y) của mọi mesh dưới visual, nằm sát mặt trước</summary>
        bool TryAddPlate(Transform visual, int layer)
        {
            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue), max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            foreach (MeshFilter mf in visual.GetComponentsInChildren<MeshFilter>(true))
            {
                MeshRenderer src = mf.GetComponent<MeshRenderer>();
                if (src == null || !src.enabled || mf.sharedMesh == null || mf.name == "QMarkCover") continue;
                Bounds b = mf.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 corner = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    Vector3 p = visual.InverseTransformPoint(mf.transform.TransformPoint(corner));
                    min = Vector3.Min(min, p);
                    max = Vector3.Max(max, p);
                }
            }
            if (max.x <= min.x || max.y <= min.y) return false;
            if (_box == null) _box = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            if (_box == null) return false;
            float depth = (max.x - min.x) * PlateDepthRatio;
            var go = new GameObject("QMarkCover") { layer = layer };
            go.transform.SetParent(visual, false);
            // mặt trước của khối là z nhỏ nhất (camera nhìn về +Z): đặt tấm phủ ngay trước mặt đó
            go.transform.localPosition = new Vector3((min.x + max.x) * 0.5f, (min.y + max.y) * 0.5f, min.z - depth * 0.5f);
            go.transform.localScale = new Vector3(max.x - min.x, max.y - min.y, depth);
            go.AddComponent<MeshFilter>().sharedMesh = _box;
            AddRenderer(go);
            return true;
        }

        /// <summary>Bắt đầu tan biến (mỗi khối có bản material riêng để chỉnh _DissolveAmount)</summary>
        public void Dissolve()
        {
            if (_t >= 0f) return;
            _instance = new Material(_shared);
            foreach (MeshRenderer mr in _renderers) if (mr != null) mr.sharedMaterial = _instance;
            _t = 0f;
        }

        void Update()
        {
            if (_t < 0f) return;
            _t += Time.deltaTime / _seconds;
            float k = Mathf.Clamp01(_t);
            if (_instance != null) _instance.SetFloat(DissolveProperty, k);
            if (k < 1f) return;
            foreach (MeshRenderer mr in _renderers) if (mr != null) Destroy(mr.gameObject);
            _renderers.Clear();
            if (_instance != null) Destroy(_instance);
            Destroy(this);
        }
    }
}
