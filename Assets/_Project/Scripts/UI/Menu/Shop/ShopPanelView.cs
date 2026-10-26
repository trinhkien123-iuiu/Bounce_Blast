using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BounceBlast.Gameplay;

namespace BounceBlast.UI
{
    /// <summary>Script quản lý của prefab ShopCanvas</summary>
    [DisallowMultipleComponent]
    public class ShopPanelView : MonoBehaviour
    {
        [Header("Chung")]
        [Tooltip("Số xu trên thanh trên cùng")] [SerializeField] TMP_Text coinText;
        [SerializeField] ScrollRect scroll;

        [Header("Gói hàng (thứ tự như bản gốc)")]
        [SerializeField] ShopOfferItem[] offers = new ShopOfferItem[0];

        [Header("Xu miễn phí (gốc: xem quảng cáo nhận xu)")]
        [Tooltip("Số xu nhận – nhớ sửa cả chữ '50' trên ô cho khớp")] [SerializeField] int freeCoins = 50;
        [SerializeField] float freeCoinCooldownHours = 24f;
        [SerializeField] Button freeCoinButton;
        [Tooltip("Nút xám đếm ngược – hiện khi chưa nhận lại được")] [SerializeField] GameObject freeCoinCountdown;
        [SerializeField] TMP_Text freeCoinTimerText;

        public event Action<ShopOfferItem> OnBuy;
        public event Action<int> OnFreeCoin;

        public float FreeCoinCooldownHours => freeCoinCooldownHours;

        DateTime _freeReadyAt;
        float _timerTick;

        void Awake()
        {
            foreach (ShopOfferItem o in offers)
                if (o != null) o.OnBuy += item => OnBuy?.Invoke(item);
            if (freeCoinButton != null) freeCoinButton.onClick.AddListener(() =>
            {
                if (DateTime.UtcNow < _freeReadyAt) return;
                AudioManager.Play(Sfx.Button);
                OnFreeCoin?.Invoke(freeCoins);
            });
        }

        void OnEnable()
        {
            if (scroll != null) scroll.verticalNormalizedPosition = 1f;
        }

        /// <summary>Xử lý hiện số xu trong Shop</summary>
        public void SetCoins(int coins) { if (coinText != null) coinText.text = coins.ToString(); }

        /// <summary>Mốc nhận lại xu miễn phí (UTC)</summary>
        public void SetFreeCoinReadyAt(DateTime utc)
        {
            _freeReadyAt = utc;
            UpdateFreeCoin();
        }

        void Update()
        {
            _timerTick -= Time.unscaledDeltaTime;
            if (_timerTick > 0f) return;
            _timerTick = 1f;
            UpdateFreeCoin();
        }

        void UpdateFreeCoin()
        {
            TimeSpan left = _freeReadyAt - DateTime.UtcNow;
            bool ready = left <= TimeSpan.Zero;
            if (freeCoinButton != null) freeCoinButton.gameObject.SetActive(ready);
            if (freeCoinCountdown != null) freeCoinCountdown.SetActive(!ready);
            if (!ready && freeCoinTimerText != null) freeCoinTimerText.text = FormatSpan(left);
        }

        /// <summary>3000 → "3 000" (ảnh gốc tách nghìn bằng khoảng trắng)</summary>
        public static string FormatNumber(int n) => n.ToString("#,0", System.Globalization.CultureInfo.InvariantCulture).Replace(',', ' ');

        /// <summary>30 → "30m", 60 → "1h", 90 → "1h 30m", 2880 → "2d"</summary>
        public static string FormatMinutes(int minutes)
        {
            if (minutes < 60) return minutes + "m";
            int h = minutes / 60, m = minutes % 60;
            if (h >= 48 && m == 0 && h % 24 == 0) return (h / 24) + "d";
            return m == 0 ? h + "h" : h + "h " + m + "m";
        }

        /// <summary>Đồng hồ kiểu gốc: "18h 52m", "3d 7h", "9m 05s"</summary>
        public static string FormatSpan(TimeSpan t)
        {
            if (t.TotalDays >= 1) return (int)t.TotalDays + "d " + t.Hours + "h";
            if (t.TotalHours >= 1) return (int)t.TotalHours + "h " + t.Minutes + "m";
            return t.Minutes + "m " + t.Seconds.ToString("00") + "s";
        }

        /// <summary>Báo ô tham chiếu nào còn trống</summary>
        public string Validate()
        {
            var miss = new List<string>();
            if (coinText == null) miss.Add("coinText");
            if (offers.Length == 0) miss.Add("offers");
            for (int i = 0; i < offers.Length; i++)
            {
                if (offers[i] == null) { miss.Add("offers[" + i + "]"); continue; }
                string s = offers[i].Validate(); if (s != null) miss.Add("offer " + s);
            }
            if (freeCoinButton == null) miss.Add("freeCoinButton");
            if (freeCoinCountdown == null) miss.Add("freeCoinCountdown");
            return miss.Count == 0 ? null : string.Join(", ", miss);
        }
    }
}
