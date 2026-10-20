using UnityEngine;

namespace BounceBlast.UI
{
    /// <summary>
    /// Co RectTransform vào vùng an toàn của màn hình (tránh tai thỏ / thanh điều hướng) – thay cho <c>KPSafeArea</c> của bản gốc
    /// (gắn trên <c>Main Panel/Container</c> của Home: hàng avatar / xu / tim, thanh Streak, nút Play). Object phải nằm trong một Canvas phủ cả màn hình.
    ///
    /// Chạy cả trong Edit mode (giống Layout Group của Unity): Game view chọn máy có tai thỏ thì Edit và Play hiện y như nhau.
    /// Neo bị "drive" – Inspector khoá xám và ghi "Some values driven by SafeAreaFitter"; giá trị này KHÔNG lưu vào scene/prefab
    /// (trong prefab neo luôn là phủ kín 0..1). Muốn tự đặt neo thì tắt component này.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    public class SafeAreaFitter : MonoBehaviour
    {
        DrivenRectTransformTracker _tracker;
        Rect _applied;
        Vector2Int _screen;

        void OnEnable() => Apply();

        void OnDisable()
        {
            _tracker.Clear();   // trả neo về giá trị đã lưu
            var rt = (RectTransform)transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
        }

        void Update()
        {
            if (Screen.safeArea != _applied || Screen.width != _screen.x || Screen.height != _screen.y) Apply();
        }

        void Apply()
        {
            Rect safe = Screen.safeArea;
            _applied = safe;
            _screen = new Vector2Int(Screen.width, Screen.height);
            if (Screen.width <= 0 || Screen.height <= 0) return;

            var rt = (RectTransform)transform;
            _tracker.Clear();
            _tracker.Add(this, rt, DrivenTransformProperties.Anchors | DrivenTransformProperties.AnchoredPosition | DrivenTransformProperties.SizeDelta);
            rt.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            rt.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
        }
    }
}
