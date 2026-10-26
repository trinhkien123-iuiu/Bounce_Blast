using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BounceBlast.Gameplay;

namespace BounceBlast.UI
{
    /// <summary>Script quản lý của prefab MenuHomeCanvas</summary>
    [DisallowMultipleComponent]
    public class HomeView : MonoBehaviour
    {
        [Header("Nút Play (bật đúng một nút theo độ khó level kế tiếp)")]
        [SerializeField] Button playNormal;
        [SerializeField] Button playHard;
        [SerializeField] Button playSuperHard;
        [Tooltip("Chữ 'Level N' của ba nút Play (cùng thứ tự Normal, Hard, Super Hard)")] [SerializeField] TMP_Text[] levelTexts = new TMP_Text[3];

        [Header("Xu / tim")]
        [SerializeField] TMP_Text coinText;
        [SerializeField] TMP_Text heartText;
        [Tooltip("Chữ trong ô đồng hồ tim: 'Full', hoặc thời gian tim vô hạn còn lại")] [SerializeField] TMP_Text heartTimerText;
        [Tooltip("Nút '+' cạnh xu / tim – mở trang Shop như bản gốc")] [SerializeField] Button[] shopOpeners = new Button[0];
        [Tooltip("Nút … tính năng chưa làm – bấm báo 'coming soon'")] [SerializeField] Button[] comingSoonButtons = new Button[0];

        [Header("Avatar (góc trên trái)")]
        [Tooltip("Bấm mở popup Profile")] [SerializeField] Button profileButton;
        [SerializeField] Image avatarImage;

        [Header("View con")]
        [Tooltip("Thanh Bounce Streak")] [SerializeField] StreakBarView streak;

        public event Action OnPlay;
        public event Action OnOpenShop;
        public event Action OnComingSoon;
        public event Action OnOpenProfile;

        public StreakBarView Streak => streak;

        void Awake()
        {
            foreach (Button b in new[] { playNormal, playHard, playSuperHard })
                if (b != null) b.onClick.AddListener(() => { AudioManager.Play(Sfx.Button); OnPlay?.Invoke(); });
            foreach (Button b in shopOpeners)
                if (b != null) b.onClick.AddListener(() => { AudioManager.Play(Sfx.Button); OnOpenShop?.Invoke(); });
            foreach (Button b in comingSoonButtons)
                if (b != null) b.onClick.AddListener(() => { AudioManager.Play(Sfx.Button); OnComingSoon?.Invoke(); });
            if (profileButton != null) profileButton.onClick.AddListener(() => { AudioManager.Play(Sfx.Button); OnOpenProfile?.Invoke(); });
        }

        /// <summary>Xử lý đổi ảnh avatar</summary>
        public void SetAvatar(Sprite sprite) { if (avatarImage != null && sprite != null) avatarImage.sprite = sprite; }

        /// <summary>Hiện level kế tiếp: bật đúng kiểu nút Play theo độ khó</summary>
        public void SetLevel(int level, LevelDifficulty difficulty)
        {
            if (playNormal != null) playNormal.gameObject.SetActive(difficulty == LevelDifficulty.Normal);
            if (playHard != null) playHard.gameObject.SetActive(difficulty == LevelDifficulty.Hard);
            if (playSuperHard != null) playSuperHard.gameObject.SetActive(difficulty == LevelDifficulty.SuperHard);
            foreach (TMP_Text t in levelTexts) if (t != null) t.text = "Level " + level;
        }

        /// <summary>Xử lý hiện số xu</summary>
        public void SetCoins(int coins) { if (coinText != null) coinText.text = coins.ToString(); }

        /// <summary>Số tim + ô đồng hồ: thời gian tim vô hạn còn lại, hoặc đếm ngược tới tim kế, hoặc "Full"</summary>
        public void SetHearts(int hearts, TimeSpan infiniteLeft, TimeSpan nextHeartIn)
        {
            if (heartText != null) heartText.text = infiniteLeft > TimeSpan.Zero ? "\u221e" : hearts.ToString();
            if (heartTimerText != null)
                heartTimerText.text = infiniteLeft > TimeSpan.Zero ? ShopPanelView.FormatSpan(infiniteLeft)
                                    : nextHeartIn > TimeSpan.Zero ? ShopPanelView.FormatSpan(nextHeartIn) : "Full";
        }

        /// <summary>Báo ô tham chiếu nào còn trống</summary>
        public string Validate()
        {
            var miss = new List<string>();
            if (playNormal == null) miss.Add("playNormal");
            if (playHard == null) miss.Add("playHard");
            if (playSuperHard == null) miss.Add("playSuperHard");
            if (coinText == null) miss.Add("coinText");
            if (heartText == null) miss.Add("heartText");
            if (heartTimerText == null) miss.Add("heartTimerText");
            if (profileButton == null) miss.Add("profileButton");
            if (avatarImage == null) miss.Add("avatarImage");
            if (streak == null) miss.Add("streak");
            else { string s = streak.Validate(); if (s != null) miss.Add("streak(" + s + ")"); }
            return miss.Count == 0 ? null : string.Join(", ", miss);
        }
    }
}
