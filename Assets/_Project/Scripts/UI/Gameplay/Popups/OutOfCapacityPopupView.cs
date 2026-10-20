using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BounceBlast.Gameplay;

namespace BounceBlast.UI
{
    /// <summary>
    /// Script quản lý của popup HẾT SỨC CHỨA ("OUT OF CAPACITY") – gắn trên gốc OutOfCapacityPopup.prefab.
    /// Prefab này bọc (lồng) panel gốc OutOfCapacityPanel trong một Canvas riêng, nên Canvas không còn phải dựng bằng code.
    /// Phần nảy vào / mở CanvasGroup nằm ở <see cref="PopupViewBase"/>; GameUI chỉ gọi <see cref="Show"/> và nghe 2 sự kiện.
    /// </summary>
    [DisallowMultipleComponent]
    public class OutOfCapacityPopupView : PopupViewBase
    {
        [Header("Riêng popup Hết sức chứa")]
        [Tooltip("Nút \"Add Capacity / Play On\" – tăng sức chứa rồi chơi tiếp")] [SerializeField] Button playOnButton;
        [Tooltip("Nút \"Give Up\" – bỏ cuộc (thua)")] [SerializeField] Button giveUpButton;

        /// <summary>Chọn Play On: tăng sức chứa và cho hộp bay tiếp.</summary>
        public event Action OnPlayOn;
        /// <summary>Chọn Give Up: thua.</summary>
        public event Action OnGiveUp;

        void Awake()
        {
            if (playOnButton != null) playOnButton.onClick.AddListener(() => { AudioManager.Play(Sfx.Button); OnPlayOn?.Invoke(); });
            if (giveUpButton != null) giveUpButton.onClick.AddListener(() => { AudioManager.Play(Sfx.Button); OnGiveUp?.Invoke(); });
        }

        public void Show(LevelDifficulty difficulty) => OpenBase(difficulty);

        protected override void CollectMissing(List<string> missing)
        {
            if (playOnButton == null) missing.Add("playOnButton");
            if (giveUpButton == null) missing.Add("giveUpButton");
        }
    }
}
