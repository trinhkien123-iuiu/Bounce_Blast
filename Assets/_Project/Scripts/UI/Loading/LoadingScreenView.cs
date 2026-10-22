using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BounceBlast.UI
{
    /// <summary>
    /// Script quản lý của prefab LoadingCanvas – màn loading gốc (TSPreloadScene: ảnh Splash + thanh tiến độ + %).
    /// Mọi hình nằm sẵn trong prefab; code chỉ đổ tiến độ (fillAmount + chữ %) và số phiên bản.
    /// </summary>
    [DisallowMultipleComponent]
    public class LoadingScreenView : MonoBehaviour
    {
        [Tooltip("Thanh đầy (Fill Bar – Image kiểu Filled Horizontal)")] [SerializeField] Image fillBar;
        [Tooltip("Chữ phần trăm trên thanh")] [SerializeField] TMP_Text percentText;
        [Tooltip("Chữ phiên bản góc trái – để trống thì giữ chữ trong prefab")] [SerializeField] TMP_Text versionText;
        [Tooltip("Mẫu chữ phiên bản, {0} = Application.version")] [SerializeField] string versionFormat = "v{0}";

        void Awake()
        {
            if (versionText != null) versionText.text = string.Format(versionFormat, Application.version);
        }

        /// <param name="progress">0..1</param>
        public void SetProgress(float progress)
        {
            progress = Mathf.Clamp01(progress);
            if (fillBar != null) fillBar.fillAmount = progress;
            if (percentText != null) percentText.text = Mathf.FloorToInt(progress * 100f) + "%";
        }

        /// <summary>Báo ô tham chiếu nào còn trống (null nếu đủ).</summary>
        public string Validate() => fillBar == null ? "fillBar" : percentText == null ? "percentText" : null;
    }
}
