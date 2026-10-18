using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BounceBlast.Gameplay;

namespace BounceBlast.UI
{
    /// <summary>
    /// Script quản lý của prefab GameCanvas (HUD: badge Level + nút Settings) – gắn trên gốc prefab.
    ///
    /// Giữ tham chiếu bằng Inspector, và giữ luôn "kiểu dáng theo độ khó" (sprite badge, sprite nút Settings, cỡ/vị trí badge,
    /// bố cục chữ, đối tượng chỉ hiện ở độ khó đó) – trước đây rải ở UiSkin + hằng số trong GameUI.
    /// GameUI chỉ gọi <see cref="Apply"/> và nghe <see cref="OnSettingsPressed"/>.
    /// Tài liệu kiến trúc: Docs/kien-truc-ui-dung.md
    /// </summary>
    [DisallowMultipleComponent]
    public class HudView : MonoBehaviour
    {
        /// <summary>Kiểu dáng của HUD ở một độ khó (kích thước/vị trí đo từ ảnh gốc 858×1908, ×1.448 ra canvas 1242).</summary>
        [Serializable]
        public class Style
        {
            [Tooltip("Sprite badge level (Normal = viên xanh; Hard/Super Hard = khung có tab đầu lâu)")]
            public Sprite badge;
            [Tooltip("Sprite nút Settings ở độ khó này")]
            public Sprite settingsIcon;
            [Tooltip("Cỡ badge (đơn vị canvas)")]
            public Vector2 badgeSize = new Vector2(203f, 90f);
            [Tooltip("Vị trí badge so với góc trên-trái")]
            public Vector2 badgePosition = new Vector2(40f, -43f);
            [Tooltip("Vùng chữ \"Lv n\" trong badge (anchor min/max, chừa lề rộng như gốc)")]
            public Vector2 levelTextMin = new Vector2(0.15f, 0.2f);
            public Vector2 levelTextMax = new Vector2(0.85f, 0.8f);
            [Tooltip("Chữ độ khó ở tab dưới badge; để trống = ẩn (Normal)")]
            public string difficultyLabel = "";
            [Tooltip("Vùng chữ độ khó (anchor min/max): nằm gọn trong tab dưới badge")]
            public Vector2 difficultyTextMin = new Vector2(0.22f, 0.05f);
            public Vector2 difficultyTextMax = new Vector2(0.78f, 0.30f);
            [Tooltip("Các đối tượng CHỈ hiện ở độ khó này (vd dải nền tiền tệ theo độ khó)")]
            public GameObject[] onlyHere = new GameObject[0];
        }

        [SerializeField] Canvas canvas;
        [Tooltip("Ảnh nền badge level")] [SerializeField] Image badge;
        [Tooltip("Chữ \"Lv n\"")] [SerializeField] TMP_Text levelText;
        [Tooltip("Chữ \"Hard\" / \"Super Hard\"")] [SerializeField] TMP_Text difficultyText;
        [Tooltip("Ảnh nút Settings")] [SerializeField] Image settingsIcon;
        [Tooltip("Nút Settings – để trống thì lấy Button trên settingsIcon")] [SerializeField] Button settingsButton;

        [Header("Kiểu dáng theo độ khó")]
        [SerializeField] Style normal = new Style();
        [SerializeField] Style hard = new Style();
        [SerializeField] Style superHard = new Style();

        /// <summary>Bấm nút Settings.</summary>
        public event Action OnSettingsPressed;

        public Canvas Canvas => canvas != null ? canvas : GetComponent<Canvas>();

        /// <summary>RectTransform nút Settings – GameManager đo tâm dọc của nó để đặt mép trên khung máng.</summary>
        public RectTransform SettingsRect => settingsIcon != null ? settingsIcon.rectTransform : null;

        void Awake()
        {
            if (settingsButton == null && settingsIcon != null) settingsButton = settingsIcon.GetComponent<Button>();
            if (settingsButton == null) return;
            settingsButton.onClick.RemoveAllListeners();
            settingsButton.onClick.AddListener(() => { AudioManager.Play(Sfx.Button); OnSettingsPressed?.Invoke(); });
        }

        Style StyleFor(LevelDifficulty d) => d == LevelDifficulty.SuperHard ? superHard : d == LevelDifficulty.Hard ? hard : normal;

        /// <summary>Hiện level và đổi toàn bộ kiểu dáng theo độ khó.</summary>
        public void Apply(int level, LevelDifficulty difficulty)
        {
            Style s = StyleFor(difficulty);

            if (levelText != null)
            {
                levelText.text = "Lv " + level;
                RectTransform t = levelText.rectTransform;
                t.anchorMin = s.levelTextMin;
                t.anchorMax = s.levelTextMax;
                t.offsetMin = t.offsetMax = Vector2.zero;
            }
            if (badge != null)
            {
                if (s.badge != null) badge.sprite = s.badge;
                RectTransform rt = badge.rectTransform;
                rt.sizeDelta = s.badgeSize;
                rt.anchoredPosition = s.badgePosition;
            }
            if (difficultyText != null)
            {
                bool show = !string.IsNullOrEmpty(s.difficultyLabel);
                difficultyText.gameObject.SetActive(show);
                if (show)
                {
                    difficultyText.text = s.difficultyLabel;
                    RectTransform dt = difficultyText.rectTransform;
                    dt.anchorMin = s.difficultyTextMin;
                    dt.anchorMax = s.difficultyTextMax;
                    dt.offsetMin = dt.offsetMax = Vector2.zero;
                }
            }
            if (settingsIcon != null && s.settingsIcon != null) settingsIcon.sprite = s.settingsIcon;

            // đối tượng chỉ hiện ở đúng một độ khó
            SetGroup(normal, difficulty == LevelDifficulty.Normal);
            SetGroup(hard, difficulty == LevelDifficulty.Hard);
            SetGroup(superHard, difficulty == LevelDifficulty.SuperHard);
        }

        static void SetGroup(Style s, bool on)
        {
            if (s.onlyHere == null) return;
            foreach (GameObject go in s.onlyHere) if (go != null) go.SetActive(on);
        }

        /// <summary>Báo ô tham chiếu nào còn trống (gọi khi dựng để lỗi hiện sớm, rõ ràng).</summary>
        public string Validate()
        {
            var miss = new List<string>();
            if (Canvas == null) miss.Add("canvas");
            if (badge == null) miss.Add("badge");
            if (levelText == null) miss.Add("levelText");
            if (difficultyText == null) miss.Add("difficultyText");
            if (settingsIcon == null) miss.Add("settingsIcon");
            if (normal.badge == null) miss.Add("normal.badge");
            if (hard.badge == null) miss.Add("hard.badge");
            if (superHard.badge == null) miss.Add("superHard.badge");
            return miss.Count == 0 ? null : string.Join(", ", miss);
        }
    }
}
