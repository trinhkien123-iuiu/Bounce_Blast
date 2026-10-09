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

        [Header("HUD")]
        public Sprite levelBadge;
        public Sprite settingsButton;
        [Tooltip("Badge level gốc theo độ khó (_Ripped/Sprite: Level / Hard Level_0 / Super Hard Level)")]
        public Sprite levelBadgeNormal;
        public Sprite levelBadgeHard;
        public Sprite levelBadgeSuperHard;
        [Tooltip("Nút Settings gốc theo độ khó (Hard Settings / Super Hard Settings)")]
        public Sprite settingsHard;
        public Sprite settingsSuperHard;
        public Sprite capacityBg;
        public Sprite capacityFill;
        public Sprite vignette;

        [Header("Hiệu ứng thắng")]
        [Tooltip("Texture pháo giấy 3×3 gốc (_Ripped/UI/Texture2D/confetti_0.png)")]
        public Texture2D confettiTexture;
        [Tooltip("Quầng sáng tròn mềm (_Ripped/UI/Texture2D/fx_glow.png)")]
        public Texture2D glowTexture;
        [Tooltip("Tia sáng toả (_Ripped/Texture2D/glow7.png)")]
        public Texture2D raysTexture;
        [Tooltip("Sao lấp lánh 4 cánh (_Ripped/Texture2D/sparkle3.png)")]
        public Texture2D sparkleTexture;

        [Header("Booster")]
        public Sprite boosterFrame;
        public Sprite boosterMagicHand;
        public Sprite boosterHammer;
        public Sprite boosterShuffle;
        public Sprite boosterCapacity;
        public Sprite plusBadge;

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
