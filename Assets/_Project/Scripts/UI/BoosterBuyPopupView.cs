using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BounceBlast.Gameplay;

namespace BounceBlast.UI
{
    /// <summary>
    /// Script quản lý của popup MUA BOOSTER – hiện khi bấm nút booster đã hết lượt (gắn trên gốc BoosterBuyPopup.prefab).
    /// Một khung dùng chung cho 4 booster: chỉ đổi tên, icon, dòng mô tả, giá và số lượt theo <see cref="Entry"/>.
    /// Bản clone chưa có cửa hàng coin: bấm nút mua là nhận booster luôn (giá chỉ để hiển thị như game gốc).
    /// </summary>
    [DisallowMultipleComponent]
    public class BoosterBuyPopupView : PopupViewBase
    {
        /// <summary>Nội dung hiển thị của một booster (chữ lấy từ Localization.txt của game gốc).</summary>
        [Serializable]
        public class Entry
        {
            public BoosterType type;
            public string title;
            [TextArea(1, 3)] public string description;
            public Sprite icon;
            [Tooltip("Giá hiển thị trên nút mua")] public int price = 1800;
            [Tooltip("Số lượt nhận được (nhãn ×N)")] public int count = 3;
        }

        [Header("Riêng popup Mua booster")]
        [SerializeField] Button closeButton;
        [Tooltip("Nút xanh lá có đồng xu + giá")] [SerializeField] Button buyButton;
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text descriptionText;
        [SerializeField] Image iconImage;
        [SerializeField] TMP_Text priceText;
        [SerializeField] TMP_Text countText;
        [SerializeField] List<Entry> entries = new List<Entry>();

        /// <summary>Bấm mua (bản clone: miễn phí) – kèm loại booster và số lượt được nhận.</summary>
        public event Action<BoosterType, int> OnBuy;
        public event Action OnClose;

        BoosterType _type;
        int _count;

        void Awake()
        {
            if (closeButton != null) closeButton.onClick.AddListener(() => { AudioManager.Play(Sfx.Button); OnClose?.Invoke(); });
            if (buyButton != null) buyButton.onClick.AddListener(() => { AudioManager.Play(Sfx.Button); OnBuy?.Invoke(_type, _count); });
        }

        public void Show(BoosterType type, LevelDifficulty difficulty)
        {
            Entry e = entries.Find(x => x != null && x.type == type);
            if (e != null)
            {
                _type = type;
                _count = e.count;
                if (titleText != null) titleText.text = e.title;
                if (descriptionText != null) descriptionText.text = e.description;
                if (iconImage != null && e.icon != null) iconImage.sprite = e.icon;
                if (priceText != null) priceText.text = e.price.ToString();
                if (countText != null) countText.text = "x" + e.count;
            }
            OpenBase(difficulty);
        }

        protected override void CollectMissing(List<string> missing)
        {
            if (closeButton == null) missing.Add("closeButton");
            if (buyButton == null) missing.Add("buyButton");
            if (titleText == null) missing.Add("titleText");
            if (descriptionText == null) missing.Add("descriptionText");
            if (iconImage == null) missing.Add("iconImage");
            if (priceText == null) missing.Add("priceText");
            if (countText == null) missing.Add("countText");
            if (entries.Count == 0) missing.Add("entries");
        }
    }
}
