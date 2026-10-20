using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BounceBlast.Gameplay;

namespace BounceBlast.UI
{
    /// <summary>
    /// Script quản lý của popup THUA ("Level Failed!") – gắn trên gốc FailCanvas.prefab.
    /// Giữ tham chiếu bằng Inspector; phần nảy vào / khung theo độ khó nằm ở <see cref="PopupViewBase"/>.
    /// GameUI chỉ gọi <see cref="Show"/> và nghe <see cref="OnRetry"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public class FailPopupView : PopupViewBase
    {
        [Header("Riêng popup Thua")]
        [Tooltip("Chữ \"Level n\" trên tab tiêu đề")] [SerializeField] TMP_Text levelText;
        [Tooltip("Nút Try Again")] [SerializeField] Button retryButton;
        [Tooltip("Nút đóng (X) – cũng là chơi lại")] [SerializeField] Button closeButton;

        /// <summary>Người chơi chọn chơi lại (Try Again hoặc đóng).</summary>
        public event Action OnRetry;

        void Awake()
        {
            if (retryButton != null) retryButton.onClick.AddListener(Retry);
            if (closeButton != null) closeButton.onClick.AddListener(Retry);
        }

        void Retry()
        {
            AudioManager.Play(Sfx.Button);
            OnRetry?.Invoke();
        }

        public void Show(int level, LevelDifficulty difficulty)
        {
            if (levelText != null) levelText.text = "Level " + level;
            OpenBase(difficulty);
        }

        protected override void CollectMissing(List<string> missing)
        {
            if (levelText == null) missing.Add("levelText");
            if (retryButton == null) missing.Add("retryButton");
            if (closeButton == null) missing.Add("closeButton");
        }
    }
}
