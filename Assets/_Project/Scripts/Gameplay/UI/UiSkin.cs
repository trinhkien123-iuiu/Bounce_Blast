using TMPro;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>Sprite/font UI gốc (Assets/_Ripped/UI) – được điền tự động bởi menu Setup.</summary>
    [CreateAssetMenu(menuName = "Bounce Blast/UI Skin")]
    public class UiSkin : ScriptableObject
    {
        [Header("Prefab UI gốc (tách từ ManagerScene bằng Tools/UnityYaml/ui_extract.py)")]
        public GameObject hudPrefab;
        public GameObject successPrefab;
        public GameObject failPrefab;
        public GameObject settingsPrefab;
        public GameObject outOfCapacityPrefab;
        public GameObject capacityBarPrefab;

        [Header("Font")]
        public TMP_FontAsset font;
        public Material fontOutlineMaterial;

        // Sprite badge Level + nút Settings theo độ khó nay nằm trong HudView (GameCanvas.prefab), không còn ở đây.
        [Header("Thanh sức chứa")]
        public Sprite capacityBg;
        public Sprite capacityFill;

        // 4 texture hiệu ứng màn thắng (pháo giấy, quầng sáng, tia, sao) nay nằm trong SuccessPopupView (SuccessCanvas.prefab).

        [Header("Lớp UI phủ lên bàn chơi")]
        [Tooltip("Assets/_Project/UI/Prefabs/OverlayCanvas.prefab - Canvas + BoosterBar + Vignette + Toast. " +
                 "Hình ảnh và bố cục sửa trong prefab; code chỉ nối camera rồi tìm con theo tên.")]
        public GameObject overlayCanvasPrefab;

        [Header("Popup")]
        public Sprite popupNormal;
        public Sprite popupHard;
        public Sprite popupSuperHard;
        public Sprite greenButton;
        public Sprite redButton;
        public Sprite addCapacityButton;
        public Sprite closeButton;
        public Sprite blastWord;
        public Sprite coin;
        public Sprite brokenHeart;
        public Sprite sadFace;
        public Sprite soundOn, soundOff, musicOn, musicOff;
        public Sprite dim;
    }
}
