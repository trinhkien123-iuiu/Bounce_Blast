using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Vệt sáng chéo quét ngang qua nút theo chu kỳ (video coin_panel gốc: nút Continue / x2 lóe một dải sáng
/// nghiêng ~25° chạy trái → phải mỗi ~2.5s). Dải sáng bị cắt theo hình nút bằng Mask.
/// </summary>
public class UiShineSweep : MonoBehaviour
{
    RectTransform _band;
    RawImage _img;
    float _t;
    float _period = 2.4f, _sweep = 0.55f, _delay;

    public static UiShineSweep Attach(RectTransform button, Texture softTex, float delay)
    {
        var s = button.GetComponent<UiShineSweep>();
        if (s == null) s = button.gameObject.AddComponent<UiShineSweep>();
        s.Build(softTex);
        s._delay = delay;
        s._t = 0f;
        return s;
    }

    void Build(Texture tex)
    {
        if (_band != null) return;
        // Lớp mặt nạ riêng cùng hình nút (không đặt Mask lên chính nút: sẽ cắt luôn icon thò ra ngoài như clapperboard)
        var maskGo = new GameObject("ShineMask", typeof(RectTransform));
        maskGo.transform.SetParent(transform, false);
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
        var mask = maskGo.AddComponent<Mask>();
        mask.showMaskGraphic = false;
        mrt.SetSiblingIndex(0); // dưới chữ/icon, trên nền nút
        var go = new GameObject("ShineBand", typeof(RectTransform));
        go.transform.SetParent(maskGo.transform, false);
        _band = (RectTransform)go.transform;
        _band.anchorMin = _band.anchorMax = new Vector2(0.5f, 0.5f);
        var r = ((RectTransform)transform).rect;
        _band.sizeDelta = new Vector2(Mathf.Max(40f, r.height * 0.35f), r.height * 2.2f);
        _band.localEulerAngles = new Vector3(0f, 0f, -25f);
        _img = go.AddComponent<RawImage>();
        _img.texture = tex;                    // texture mềm (glow) → mép dải mờ dần
        _img.color = new Color(1f, 1f, 0.85f, 0.75f);
        _img.raycastTarget = false;
        _band.gameObject.SetActive(false);
    }

    void Update()
    {
        if (_band == null) return;
        float dt = Time.unscaledDeltaTime;
        if (_delay > 0f) { _delay -= dt; return; }
        _t += dt;
        if (_t > _period) _t -= _period;
        bool on = _t < _sweep;
        _band.gameObject.SetActive(on);
        if (!on) return;
        float w = ((RectTransform)transform).rect.width;
        float k = _t / _sweep;
        _band.anchoredPosition = new Vector2(Mathf.Lerp(-0.75f, 0.75f, k) * w, 0f);
    }
}
