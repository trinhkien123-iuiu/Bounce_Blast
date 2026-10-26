using TMPro;
using UnityEngine;

namespace BounceBlast.UI
{
    /// <summary>Script quản lý của prefab MenuToast</summary>
    [DisallowMultipleComponent]
    public class ToastView : MonoBehaviour
    {
        [Tooltip("Dòng chữ – lưu sẵn alpha 0 trong prefab")] [SerializeField] TMP_Text text;

        float _time;

        /// <summary>Xử lý hiện thông báo ngắn ở menu</summary>
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

        /// <summary>Báo ô tham chiếu nào còn trống</summary>
        public string Validate() => text == null ? "text" : null;
    }
}
