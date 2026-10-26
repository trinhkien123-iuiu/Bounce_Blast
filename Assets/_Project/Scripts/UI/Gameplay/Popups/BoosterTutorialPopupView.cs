using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BounceBlast.Gameplay;

namespace BounceBlast.UI
{
    /// <summary>Script quản lý của popup HƯỚNG DẪN BOOSTER</summary>
    [DisallowMultipleComponent]
    public class BoosterTutorialPopupView : PopupViewBase
    {
        /// <summary>Nội dung của một booster (chữ tutorial_booster_* trong Localization.txt của game gốc)</summary>
        [Serializable]
        public class Entry
        {
            public BoosterType type;
            public string title;
            [TextArea(1, 3)] public string description;
            public Sprite icon;
        }

        [Header("Riêng popup Hướng dẫn booster")]
        [SerializeField] Button closeButton;
        [Tooltip("Nút xanh lá \"Use\"")] [SerializeField] Button useButton;
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text descriptionText;
        [SerializeField] Image iconImage;
        [Tooltip("Chữ trên nút xanh: \"Use\" (booster) / \"OK\" (cơ chế mới)")] [SerializeField] TMP_Text useLabel;
        [Tooltip("Nhãn cam \"x2\" trên nút Use: số lượt miễn phí được tặng khi mở khoá")] [SerializeField] GameObject countBadge;
        [SerializeField] TMP_Text countText;
        [SerializeField] List<Entry> entries = new List<Entry>();

        /// <summary>Bấm Use – kèm loại booster</summary>
        public event Action<BoosterType> OnUse;
        public event Action OnClose;

        BoosterType _type;

        void Awake()
        {
            if (closeButton != null) closeButton.onClick.AddListener(() => { AudioManager.Play(Sfx.Button); OnClose?.Invoke(); });
            if (useButton != null) useButton.onClick.AddListener(() => { AudioManager.Play(Sfx.Button); OnUse?.Invoke(_type); });
        }

        /// <summary>Xử lý hiện popup hướng dẫn booster mới mở</summary>
        public void Show(BoosterType type, LevelDifficulty difficulty, int freeCount = 0)
        {
            Entry e = entries.Find(x => x != null && x.type == type);
            if (e != null)
            {
                _type = type;
                if (titleText != null) titleText.text = e.title;
                if (descriptionText != null) descriptionText.text = e.description;
                if (iconImage != null && e.icon != null) iconImage.sprite = e.icon;
            }
            if (useLabel != null) useLabel.text = "Use";
            if (countBadge != null) countBadge.SetActive(freeCount > 0);
            if (countText != null) countText.text = "x" + freeCount;
            OpenBase(difficulty);
        }

        /// <summary>Popup "New Feature" cho một cơ chế mới</summary>
        public void ShowFeature(string title, string description, Sprite icon, LevelDifficulty difficulty)
        {
            if (titleText != null) titleText.text = title.ToUpperInvariant();
            if (descriptionText != null) descriptionText.text = description;
            if (iconImage != null && icon != null) iconImage.sprite = icon;
            if (useLabel != null) useLabel.text = "OK";
            if (countBadge != null) countBadge.SetActive(false);
            OpenBase(difficulty);
        }

        protected override void CollectMissing(List<string> missing)
        {
            if (closeButton == null) missing.Add("closeButton");
            if (useButton == null) missing.Add("useButton");
            if (titleText == null) missing.Add("titleText");
            if (descriptionText == null) missing.Add("descriptionText");
            if (iconImage == null) missing.Add("iconImage");
            if (entries.Count == 0) missing.Add("entries");
        }
    }
}
