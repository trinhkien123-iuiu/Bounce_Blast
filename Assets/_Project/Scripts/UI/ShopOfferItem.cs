using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BounceBlast.Gameplay;

namespace BounceBlast.UI
{
    /// <summary>
    /// Một gói hàng trong Shop – gắn thẳng trên object của gói (VD <c>Content/IAP Content - Bundle 1_4_1</c>), chọn gói trong Hierarchy là thấy dữ liệu.
    /// Hình của gói (tên, giá, số xu, ô booster / tim / No Ads bật hay tắt) LƯU SẴN trong prefab; lúc chạy code không ghi đè.
    /// Sửa dữ liệu trong Inspector thì chữ trên gói tự cập nhật theo (chỉ trong Editor), hoặc chuột phải component ▸ "Ghi dữ liệu ra hình".
    /// Bấm nút mua thì báo <see cref="OnBuy"/>; cộng phần thưởng là việc của <c>MenuManager</c>.
    /// </summary>
    [DisallowMultipleComponent]
    public class ShopOfferItem : MonoBehaviour
    {
        [Header("Dữ liệu gói")]
        public string title = "Bundle";
        [Tooltip("Chuỗi giá hiển thị trên nút (không có IAP thật nên chỉ là chữ)")] public string price = "0 ₫";
        public int coins;
        [Tooltip("Số lượt MỖI loại booster (0 = không có)")] public int boosters;
        [Tooltip("Phút tim vô hạn (0 = không có)")] public int infiniteHeartMinutes;
        public bool noAds;

        [Header("Hình của gói")]
        [SerializeField] Button buyButton;
        [SerializeField] TMP_Text priceText;
        [Tooltip("Có thể trống (gói xu một dòng không có tên)")] [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text coinText;
        [Tooltip("Ô chứa 4 booster – có thể trống")] [SerializeField] GameObject boosterGroup;
        [SerializeField] TMP_Text[] boosterTexts = new TMP_Text[0];
        [Tooltip("Ô tim vô hạn – có thể trống")] [SerializeField] GameObject heartSlot;
        [SerializeField] TMP_Text heartText;
        [Tooltip("Biểu tượng No Ads – có thể trống")] [SerializeField] GameObject noAdsSlot;

        public event Action<ShopOfferItem> OnBuy;

        void Awake()
        {
            if (buyButton != null) buyButton.onClick.AddListener(() => { AudioManager.Play(Sfx.Button); OnBuy?.Invoke(this); });
        }

        /// <summary>Ghi dữ liệu gói ra các chữ / ô trong prefab (chỉ dùng trong Editor).</summary>
        [ContextMenu("Ghi dữ liệu ra hình")]
        public void ApplyToVisuals()
        {
            if (titleText != null) titleText.text = title;
            if (priceText != null) priceText.text = price;
            if (coinText != null) coinText.text = ShopPanelView.FormatNumber(coins);
            if (boosterGroup != null) boosterGroup.SetActive(boosters > 0);
            foreach (TMP_Text t in boosterTexts) if (t != null) t.text = "x" + boosters;
            if (heartSlot != null) heartSlot.SetActive(infiniteHeartMinutes > 0);
            if (heartText != null) heartText.text = ShopPanelView.FormatMinutes(infiniteHeartMinutes);
            if (noAdsSlot != null) noAdsSlot.SetActive(noAds);
#if UNITY_EDITOR
            foreach (UnityEngine.Object o in new UnityEngine.Object[] { titleText, priceText, coinText, boosterGroup, heartSlot, heartText, noAdsSlot })
                if (o != null) UnityEditor.EditorUtility.SetDirty(o);
            foreach (TMP_Text t in boosterTexts) if (t != null) UnityEditor.EditorUtility.SetDirty(t);
#endif
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            // sửa số trong Inspector → chữ trên gói đổi theo ngay (hoãn 1 nhịp vì OnValidate không cho bật/tắt object)
            if (Application.isPlaying) return;
            UnityEditor.EditorApplication.delayCall += () => { if (this != null) ApplyToVisuals(); };
        }
#endif

        public string Validate() => buyButton == null || priceText == null || coinText == null ? name : null;
    }
}
