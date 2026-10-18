using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>
    /// Xoay liên tục một ảnh UI (quầng tia sáng sau icon ở popup mua booster). Dùng thời gian không bị co giãn
    /// vì popup mở lúc game tạm dừng (timeScale = 0).
    /// </summary>
    public class UiSpin : MonoBehaviour
    {
        [Tooltip("Độ/giây; âm = ngược chiều kim đồng hồ")] [SerializeField] float degreesPerSecond = 25f;

        void OnEnable() => transform.localRotation = Quaternion.identity;

        void Update() => transform.Rotate(0f, 0f, -degreesPerSecond * Time.unscaledDeltaTime);
    }
}
