using UnityEngine;

namespace BounceBlast.UI
{
    /// <summary>Xoay liên tục một ảnh UI (quầng tia sáng sau icon ở popup mua booster)</summary>
    public class UiSpin : MonoBehaviour
    {
        [Tooltip("Độ/giây; âm = ngược chiều kim đồng hồ")] [SerializeField] float degreesPerSecond = 25f;

        Quaternion _base;
        bool _saved;

        void OnEnable()
        {
            if (!_saved) { _base = transform.localRotation; _saved = true; }
            transform.localRotation = _base;
        }

        void Update() => transform.Rotate(0f, 0f, -degreesPerSecond * Time.unscaledDeltaTime);
    }
}
