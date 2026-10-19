using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Vệt sáng chéo quét ngang qua nút theo chu kỳ (video coin_panel gốc: nút Continue / x2 lóe một dải sáng
/// nghiêng ~25° chạy trái → phải mỗi ~2.4s). Gắn trên nút; lớp mặt nạ <c>ShineMask</c> (cùng hình nút) và dải <c>ShineBand</c>
/// nằm SẴN trong prefab dưới nút – sửa màu / độ rộng / góc nghiêng của dải ngay trong Hierarchy.
/// Lúc chạy chỉ bật dải và trượt nó ngang qua nút. Nút mới chưa có lớp: chuột phải component ▸ "Tạo lớp vệt sáng".
/// </summary>
public class UiShineSweep : MonoBehaviour
{
    [Tooltip("Dải sáng (con của ShineMask) – lưu tắt sẵn")] [SerializeField] RectTransform band;
    [Tooltip("Giây giữa hai lần quét")] [SerializeField] float period = 2.4f;
    [Tooltip("Giây một lần quét")] [SerializeField] float sweep = 0.55f;
    [Tooltip("Trễ trước lần quét đầu (lệch nhịp giữa các nút)")] [SerializeField] float startDelay;
    [Tooltip("Dải chạy từ -x đến +x lần bề ngang nút")] [SerializeField] float travel = 0.75f;

    float _t, _delay;
    bool _running;

    /// <summary>Bắt đầu quét (gọi khi nút hiện ra).</summary>
    public void Restart()
    {
        _t = 0f;
        _delay = startDelay;
        _running = true;
    }

    public void Stop()
    {
        _running = false;
        if (band != null) band.gameObject.SetActive(false);
    }

    void OnDisable() => Stop();

    void Update()
    {
        if (!_running || band == null) return;
        float dt = Time.unscaledDeltaTime;
        if (_delay > 0f) { _delay -= dt; return; }
        _t += dt;
        if (_t > period) _t -= period;
        bool on = _t < sweep;
        if (band.gameObject.activeSelf != on) band.gameObject.SetActive(on);
        if (!on) return;
        float w = ((RectTransform)transform).rect.width;
        band.anchoredPosition = new Vector2(Mathf.Lerp(-travel, travel, _t / sweep) * w, 0f);
    }

#if UNITY_EDITOR
    /// <summary>Dựng ShineMask + ShineBand dưới nút (chỉ dùng trong Editor, kết quả lưu vào prefab).</summary>
    [ContextMenu("Tạo lớp vệt sáng")]
    void CreateLayers()
    {
        if (band != null) return;
        // Lớp mặt nạ riêng cùng hình nút (không đặt Mask lên chính nút: sẽ cắt luôn icon thò ra ngoài như clapperboard)
        var maskGo = new GameObject("ShineMask", typeof(RectTransform));
        UnityEditor.Undo.RegisterCreatedObjectUndo(maskGo, "Tạo lớp vệt sáng");
        maskGo.transform.SetParent(transform, false);
        maskGo.layer = gameObject.layer;
        var mrt = (RectTransform)maskGo.transform;
        mrt.anchorMin = Vector2.zero; mrt.anchorMax = Vector2.one; mrt.offsetMin = mrt.offsetMax = Vector2.zero;
        var src = GetComponent<Image>();
        Image maskImg;
        if (src is MirroredImage srcMir)
        {
            var mi = maskGo.AddComponent<MirroredImage>();
            mi.MirrorHorizontally = srcMir.MirrorHorizontally;
            mi.MirrorVertically = srcMir.MirrorVertically;
            mi.UseSlicing = srcMir.UseSlicing;
            maskImg = mi;
        }
        else maskImg = maskGo.AddComponent<Image>();
        if (src != null)
        {
            maskImg.sprite = src.sprite;
            maskImg.type = src.type;
            maskImg.pixelsPerUnitMultiplier = src.pixelsPerUnitMultiplier;
            maskImg.preserveAspect = src.preserveAspect;
        }
        maskImg.raycastTarget = false;
        maskGo.AddComponent<Mask>().showMaskGraphic = false;
        mrt.SetSiblingIndex(0); // dưới chữ/icon, trên nền nút

        var go = new GameObject("ShineBand", typeof(RectTransform));
        go.transform.SetParent(maskGo.transform, false);
        go.layer = gameObject.layer;
        band = (RectTransform)go.transform;
        band.anchorMin = band.anchorMax = new Vector2(0.5f, 0.5f);
        Rect r = ((RectTransform)transform).rect;
        band.sizeDelta = new Vector2(Mathf.Max(40f, r.height * 0.35f), r.height * 2.2f);
        band.localEulerAngles = new Vector3(0f, 0f, -25f);
        var img = go.AddComponent<RawImage>();
        img.color = new Color(1f, 1f, 0.85f, 0.75f);
        img.raycastTarget = false;
        go.SetActive(false);
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}
