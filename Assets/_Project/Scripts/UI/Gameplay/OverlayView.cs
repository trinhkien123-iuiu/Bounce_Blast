using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BounceBlast.UI
{
    /// <summary>Script quản lý của prefab OverlayCanvas</summary>
    [DisallowMultipleComponent]
    public class OverlayView : MonoBehaviour
    {
        [SerializeField] Canvas canvas;
        [Tooltip("Thanh booster (BoosterBar)")] [SerializeField] BoosterBarView booster;
        [Tooltip("Viền đỏ phủ cả màn hình, hiện khi sức chứa gần đầy – alpha màu = 0 lúc bình thường")] [SerializeField] Image vignette;
        [Tooltip("Dòng thông báo giữa màn hình – rỗng và alpha 0 lúc bình thường")] [SerializeField] TMP_Text toast;

        [Header("Viền cảnh báo")]
        [Tooltip("Sức chứa bắt đầu hiện viền")] [SerializeField, Range(0f, 1f)] float dangerStart = 0.75f;
        [Tooltip("Độ đậm tối đa của viền")] [SerializeField, Range(0f, 1f)] float vignetteMaxAlpha = 0.55f;
        [Tooltip("Tốc độ viền đậm lên / nhạt đi")] [SerializeField] float vignetteFadeSpeed = 2f;

        float _dangerTarget;
        float _toastTime;

        public Canvas Canvas => canvas != null ? canvas : GetComponent<Canvas>();
        public BoosterBarView Booster => booster;

        /// <summary>Xử lý hiện viền đỏ cảnh báo theo mức sức chứa</summary>
        public void SetDanger(float fill01)
            => _dangerTarget = fill01 >= dangerStart ? Mathf.InverseLerp(dangerStart, 1f, fill01) : 0f;

        /// <summary>Xử lý hiện thông báo ngắn</summary>
        public void ShowToast(string message, float seconds = 1.6f)
        {
            if (toast == null) return;
            toast.text = message;
            _toastTime = seconds;
            toast.alpha = 1f;
        }

        void Awake()
        {
            if (toast != null) toast.alpha = 0f;
        }

        void LateUpdate()
        {
            if (vignette != null)
            {
                float breathe = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f);
                Color c = vignette.color;
                c.a = Mathf.MoveTowards(c.a, _dangerTarget * vignetteMaxAlpha * breathe, Time.unscaledDeltaTime * vignetteFadeSpeed);
                vignette.color = c;
            }
            if (toast != null && _toastTime > 0f)
            {
                _toastTime -= Time.unscaledDeltaTime;
                toast.alpha = Mathf.Clamp01(_toastTime * 2f);
            }
        }

        /// <summary>Báo ô tham chiếu nào còn trống</summary>
        public string Validate()
        {
            var miss = new List<string>();
            if (Canvas == null) miss.Add("canvas");
            if (booster == null) miss.Add("booster");
            if (vignette == null) miss.Add("vignette");
            if (toast == null) miss.Add("toast");
            return miss.Count == 0 ? null : string.Join(", ", miss);
        }
    }
}
