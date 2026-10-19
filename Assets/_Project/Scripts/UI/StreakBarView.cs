using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BounceBlast.Gameplay;

namespace BounceBlast.UI
{
    /// <summary>
    /// Script quản lý của prefab StreakMainBar – thanh "Bounce Streak" trên trang Home (huy chương, thanh "0/1", đồng hồ sự kiện,
    /// phần thưởng tim vô hạn bên phải). Hình tách từ <c>BounceStreakLoadAsync/MainPanelCanvas/MainPanelBarMain</c> của bản gốc. View chỉ hiển thị tiến độ;
    /// bấm vào thanh khi đã đầy thì báo <see cref="OnClaim"/> (việc cộng thưởng là của <c>MenuManager</c>).
    /// Lúc chạy chỉ đổi: độ đầy, chữ x/y, chữ phút thưởng, đồng hồ, nhịp phóng to phần thưởng khi nhận được. Hình (băng đỏ, cỡ, vị trí) lưu sẵn trong prefab.
    /// </summary>
    [DisallowMultipleComponent]
    public class StreakBarView : MonoBehaviour
    {
        [Tooltip("Thanh tô (Image kiểu Filled)")] [SerializeField] Image fill;
        [Tooltip("Chữ 'x/y' giữa thanh")] [SerializeField] TMP_Text progressText;
        [Tooltip("Ảnh phần thưởng (tim vô hạn)")] [SerializeField] Image rewardIcon;
        [Tooltip("Chữ thời lượng trên băng rôn ('10m')")] [SerializeField] TMP_Text rewardText;
        [Tooltip("Bấm để nhận thưởng – nằm trên gốc thanh")] [SerializeField] Button claimButton;
        [Tooltip("Đồng hồ còn lại của sự kiện ('3d 18h') dưới thanh")] [SerializeField] TMP_Text timerText;
        [SerializeField] float fillSpeed = 3f;

        public event Action OnClaim;

        float _target;
        bool _ready;
        Vector3 _iconScale = Vector3.one;

        void Awake()
        {
            if (rewardIcon != null) _iconScale = rewardIcon.rectTransform.localScale;
            if (claimButton != null) claimButton.onClick.AddListener(() =>
            {
                if (!_ready) return;
                AudioManager.Play(Sfx.Button);
                OnClaim?.Invoke();
            });
        }

        /// <summary>Hiện tiến độ: <paramref name="wins"/>/<paramref name="target"/> trận thắng liên tiếp, thưởng <paramref name="rewardMinutes"/> phút tim vô hạn.</summary>
        public void Set(int wins, int target, int rewardMinutes, bool snap = false)
        {
            target = Mathf.Max(1, target);
            wins = Mathf.Clamp(wins, 0, target);
            _ready = wins >= target;
            _target = (float)wins / target;
            if (snap && fill != null) fill.fillAmount = _target;
            if (progressText != null) progressText.text = wins + "/" + target;
            if (rewardText != null) rewardText.text = ShopPanelView.FormatMinutes(rewardMinutes);
        }

        public void SetTimer(TimeSpan left)
        {
            if (timerText != null) timerText.text = ShopPanelView.FormatSpan(left);
        }

        void Update()
        {
            if (fill != null) fill.fillAmount = Mathf.MoveTowards(fill.fillAmount, _target, fillSpeed * Time.unscaledDeltaTime);
            if (rewardIcon != null)
            {
                // nhịp nhẹ phần thưởng khi đã nhận được
                float s = _ready ? 1f + 0.06f * Mathf.Sin(Time.unscaledTime * 6f) : 1f;
                rewardIcon.rectTransform.localScale = _iconScale * s;
            }
        }

        /// <summary>Báo ô tham chiếu nào còn trống (null nếu đủ).</summary>
        public string Validate()
        {
            var miss = new List<string>();
            if (fill == null) miss.Add("fill");
            if (progressText == null) miss.Add("progressText");
            if (rewardText == null) miss.Add("rewardText");
            if (claimButton == null) miss.Add("claimButton");
            if (timerText == null) miss.Add("timerText");
            return miss.Count == 0 ? null : string.Join(", ", miss);
        }
    }
}
