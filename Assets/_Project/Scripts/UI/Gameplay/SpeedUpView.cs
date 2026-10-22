using TMPro;
using UnityEngine;

namespace BounceBlast.UI
{
    /// <summary>
    /// Script quản lý của prefab SpeedUpCanvas – tách từ "TapToSpeedUp" của game gốc: viền trắng phủ màn hình
    /// (Vignette Speed, sprite Vignette_mir) + dòng chữ báo "SPEED UP xN" hiện khi người chơi nhấn giữ để tăng tốc.
    /// Hình (màu, độ đậm viền, kiểu chữ, vị trí) nằm sẵn trong prefab; code chỉ làm mờ CanvasGroup gốc giữa 0 (ẩn) và 1 (hiện),
    /// cho viền nháy (độ đậm dao động blinkMin..1, <see cref="blinkPerSecond"/> lần/giây) còn chữ thì đứng yên, rõ chữ.
    /// </summary>
    [DisallowMultipleComponent]
    public class SpeedUpView : MonoBehaviour
    {
        [Tooltip("CanvasGroup gốc (Canvas Base) – lưu alpha 0 trong prefab")] [SerializeField] CanvasGroup group;
        [Tooltip("CanvasGroup của viền trắng (Vignette Speed) – chỉ viền nháy")] [SerializeField] CanvasGroup vignette;
        [Tooltip("Dòng chữ báo đang tăng tốc")] [SerializeField] TMP_Text label;
        [Tooltip("Mẫu chữ, {0} = tốc độ (VD 1.5)")] [SerializeField] string labelFormat = "SPEED UP x{0:0.#}";
        [Tooltip("Giây để hiện / ẩn hẳn")] [SerializeField] float fadeSeconds = 0.15f;
        [Tooltip("Số lần nháy mỗi giây (như viền đỏ cảnh báo capacity)")] [SerializeField] float blinkPerSecond = 1f;
        [Tooltip("Độ đậm thấp nhất khi nháy (so với độ đậm trong prefab)")] [SerializeField, Range(0f, 1f)] float blinkMin = 0.1f;
        [Tooltip("Chữ nảy to lên rồi về đúng cỡ khi bắt đầu giữ (giây, 0 = tắt)")] [SerializeField] float popSeconds = 0.25f;

        bool _on;
        float _fade, _blinkStart;
        Vector3 _labelScale = Vector3.one;

        public void SetOn(bool on, float speed = 1.5f)
        {
            if (on && !_on)
            {
                _blinkStart = Time.unscaledTime;   // mỗi lần giữ bắt đầu nháy từ lúc sáng nhất
                if (label != null) label.text = string.Format(System.Globalization.CultureInfo.InvariantCulture, labelFormat, speed);   // dấu chấm, không theo máy
            }
            _on = on;
        }

        void Awake()
        {
            if (group != null) { group.alpha = 0f; group.blocksRaycasts = false; group.interactable = false; }
            if (label != null) _labelScale = label.rectTransform.localScale;   // cỡ trong prefab
        }

        void Update()
        {
            if (group == null) return;
            if (!_on && _fade <= 0f) return;
            _fade = Mathf.MoveTowards(_fade, _on ? 1f : 0f, Time.unscaledDeltaTime / Mathf.Max(0.01f, fadeSeconds));
            group.alpha = _fade;
            float t = Time.unscaledTime - _blinkStart;
            if (vignette != null)
            {
                // cos bắt đầu ở 1 (sáng nhất) rồi tối dần, lặp blinkPerSecond lần/giây
                float k = 0.5f + 0.5f * Mathf.Cos(t * Mathf.PI * 2f * blinkPerSecond);
                vignette.alpha = Mathf.Lerp(blinkMin, 1f, k);
            }
            if (label != null)
            {
                float pop = popSeconds > 0f && _on ? Mathf.Clamp01(t / popSeconds) : 1f;
                label.rectTransform.localScale = _labelScale * (pop >= 1f ? 1f : Mathf.LerpUnclamped(0.6f, 1f, EaseOutBack(pop)));
            }
        }

        static float EaseOutBack(float x)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }

        /// <summary>Báo ô tham chiếu nào còn trống (null nếu đủ).</summary>
        public string Validate() => group == null ? "group" : vignette == null ? "vignette" : label == null ? "label" : null;
    }
}
