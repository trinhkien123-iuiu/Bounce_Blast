using TMPro;
using UnityEngine;

namespace BounceBlast.UI
{
    /// <summary>
    /// Script quản lý của prefab MenuToast – dòng thông báo giữa màn hình MenuGame ("coming soon", "+50 coins"…).
    /// Hiện rõ rồi mờ dần trong nửa giây cuối.
    /// </summary>
    [DisallowMultipleComponent]
    public class ToastView : MonoBehaviour
    {
        [Tooltip("Dòng chữ – lưu sẵn alpha 0 trong prefab")] [SerializeField] TMP_Text text;

        float _time;

        public void Show(string message, float seconds = 1.6f)
        {
            if (text == null) return;
            text.text = message;
            _time = seconds;
            text.alpha = 1f;
        }

        void LateUpdate()
        {
            if (text == null || _time <= 0f) return;
            _time -= Time.unscaledDeltaTime;
            text.alpha = Mathf.Clamp01(_time * 2f);
        }

        /// <summary>Báo ô tham chiếu nào còn trống (null nếu đủ).</summary>
        public string Validate() => text == null ? "text" : null;
    }
}
