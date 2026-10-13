using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BounceBlast.Gameplay
{
    /// <summary>
    /// Script quản lý của prefab MainMenuCanvas (màn hình chính MenuGame) – gắn trên gốc prefab.
    /// Giữ nút Play (3 kiểu theo độ khó), thanh xu / tim, thanh 5 tab dưới đáy (Shop, Journey, Home, Collection, Settings)
    /// và trang Settings. Chỉ báo ra ngoài bằng sự kiện; việc đổi scene / ghi âm thanh là của <c>MenuManager</c>.
    /// Tab nào chưa có tính năng (Shop, Journey, Collection) thì bấm báo <see cref="OnComingSoon"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public class MainMenuView : MonoBehaviour
    {
        /// <summary>Một tab của thanh dưới đáy.</summary>
        [Serializable]
        public class Tab
        {
            [Tooltip("Nút bấm của tab")] public Button button;
            [Tooltip("Biểu tượng (phóng to khi được chọn)")] public RectTransform icon;
            [Tooltip("Tên tab – chỉ hiện khi được chọn")] public GameObject label;
            [Tooltip("Trang hiện ra khi chọn tab; để TRỐNG nếu tính năng chưa làm")] public GameObject panel;
            [Tooltip("Tên hiển thị trong thông báo 'sắp ra mắt'")] public string title;
        }

        [Header("Chung")]
        [SerializeField] Canvas canvas;
        [Tooltip("Dòng thông báo giữa màn hình (alpha 0 lúc bình thường)")] [SerializeField] TMP_Text toast;

        [Header("Nút Play (bật đúng một nút theo độ khó level kế tiếp)")]
        [SerializeField] Button playNormal;
        [SerializeField] Button playHard;
        [SerializeField] Button playSuperHard;
        [Tooltip("Chữ 'Level N' của ba nút Play (cùng thứ tự Normal, Hard, Super Hard)")] [SerializeField] TMP_Text[] levelTexts = new TMP_Text[3];

        [Header("Xu / tim")]
        [SerializeField] TMP_Text coinText;
        [SerializeField] TMP_Text heartText;
        [Tooltip("Đồng hồ hồi tim – ẩn vì bản dựng lại chưa có hệ thống tim")] [SerializeField] GameObject heartTimer;
        [Tooltip("Nút '+' cạnh xu / tim, nút hồ sơ… – tính năng chưa làm")] [SerializeField] Button[] comingSoonButtons = new Button[0];

        [Header("Thanh tab dưới đáy")]
        [SerializeField] Tab[] tabs = new Tab[0];
        [Tooltip("Tab đang chọn lúc mở Menu")] [SerializeField] int startTab = 2;
        [Tooltip("Dải sáng nằm sau tab đang chọn")] [SerializeField] RectTransform hover;
        [SerializeField] float selectedIconScale = 1.2f;
        [SerializeField] float hoverSpeed = 14f;

        [Header("Trang Settings")]
        [SerializeField] SettingsPopupView.ToggleRow sound = new SettingsPopupView.ToggleRow();
        [SerializeField] SettingsPopupView.ToggleRow music = new SettingsPopupView.ToggleRow();
        [SerializeField] SettingsPopupView.ToggleRow vibration = new SettingsPopupView.ToggleRow();
        [Tooltip("Mục chưa làm (Support, Terms, Privacy, Restore Purchases) – tắt đi")] [SerializeField] GameObject[] unavailable = new GameObject[0];

        public event Action OnPlay;
        public event Action<string> OnComingSoon;
        public event Action<bool> OnSoundChanged;
        public event Action<bool> OnMusicChanged;
        public event Action<bool> OnVibrationChanged;

        int _selected = -1;
        float _toastTime;
        float _hoverX;
        bool _hoverSnap = true;

        public Canvas Canvas => canvas != null ? canvas : GetComponent<Canvas>();

        void Awake()
        {
            if (toast != null) toast.alpha = 0f;
            foreach (GameObject go in unavailable) if (go != null) go.SetActive(false);
            if (heartTimer != null) heartTimer.SetActive(false);

            foreach (Button b in new[] { playNormal, playHard, playSuperHard })
                if (b != null) b.onClick.AddListener(() => { AudioManager.Play(Sfx.Button); OnPlay?.Invoke(); });
            foreach (Button b in comingSoonButtons)
                if (b != null) b.onClick.AddListener(() => { AudioManager.Play(Sfx.Button); OnComingSoon?.Invoke(null); });

            for (int i = 0; i < tabs.Length; i++)
            {
                int index = i;
                if (tabs[i].button != null) tabs[i].button.onClick.AddListener(() => { AudioManager.Play(Sfx.Button); SelectTab(index); });
            }
            HookToggle(sound, v => OnSoundChanged?.Invoke(v));
            HookToggle(music, v => OnMusicChanged?.Invoke(v));
            HookToggle(vibration, v => OnVibrationChanged?.Invoke(v));
        }

        void Start() => SelectTab(startTab);

        static void HookToggle(SettingsPopupView.ToggleRow row, Action<bool> changed)
        {
            if (row == null || row.button == null) return;
            row.button.onClick.AddListener(() =>
            {
                row.value = !row.value;
                row.Refresh();
                changed(row.value);
                AudioManager.Play(Sfx.Button);
            });
        }

        // ------------------------------------------------------------------ API cho MenuManager
        /// <summary>Hiện level kế tiếp: bật đúng kiểu nút Play theo độ khó.</summary>
        public void SetLevel(int level, LevelDifficulty difficulty)
        {
            if (playNormal != null) playNormal.gameObject.SetActive(difficulty == LevelDifficulty.Normal);
            if (playHard != null) playHard.gameObject.SetActive(difficulty == LevelDifficulty.Hard);
            if (playSuperHard != null) playSuperHard.gameObject.SetActive(difficulty == LevelDifficulty.SuperHard);
            foreach (TMP_Text t in levelTexts) if (t != null) t.text = "Level " + level;
        }

        public void SetCoins(int coins) { if (coinText != null) coinText.text = coins.ToString(); }
        public void SetHearts(int hearts) { if (heartText != null) heartText.text = hearts.ToString(); }

        public void SetSettings(bool soundOn, bool musicOn, bool vibrationOn)
        {
            sound.value = soundOn; sound.Refresh();
            music.value = musicOn; music.Refresh();
            vibration.value = vibrationOn; vibration.Refresh();
        }

        public void ShowToast(string message, float seconds = 1.6f)
        {
            if (toast == null) return;
            toast.text = message;
            _toastTime = seconds;
            toast.alpha = 1f;
        }

        /// <summary>Chọn một tab. Tab chưa có trang (tính năng chưa làm) → chỉ báo "sắp ra mắt", không đổi tab.</summary>
        public void SelectTab(int index)
        {
            if (index < 0 || index >= tabs.Length) return;
            if (tabs[index].panel == null)
            {
                OnComingSoon?.Invoke(tabs[index].title);
                return;
            }
            _selected = index;
            for (int i = 0; i < tabs.Length; i++)
            {
                bool on = i == index;
                if (tabs[i].icon != null) tabs[i].icon.localScale = Vector3.one * (on ? selectedIconScale : 1f);
                if (tabs[i].label != null) tabs[i].label.SetActive(on);
                if (tabs[i].panel != null) tabs[i].panel.SetActive(on);
            }
        }

        void LateUpdate()
        {
            if (hover != null && _selected >= 0 && tabs[_selected].button != null)
            {
                // dải sáng trượt tới giữa tab đang chọn
                var parent = (RectTransform)hover.parent;
                float target = parent.InverseTransformPoint(tabs[_selected].button.transform.position).x;
                _hoverX = _hoverSnap ? target : Mathf.Lerp(_hoverX, target, 1f - Mathf.Exp(-hoverSpeed * Time.unscaledDeltaTime));
                _hoverSnap = false;
                Vector2 p = hover.anchoredPosition;
                p.x = _hoverX;
                hover.anchoredPosition = p;
            }
            if (toast != null && _toastTime > 0f)
            {
                _toastTime -= Time.unscaledDeltaTime;
                toast.alpha = Mathf.Clamp01(_toastTime * 2f);
            }
        }

        /// <summary>Báo ô tham chiếu nào còn trống (null nếu đủ).</summary>
        public string Validate()
        {
            var miss = new List<string>();
            if (Canvas == null) miss.Add("canvas");
            if (toast == null) miss.Add("toast");
            if (playNormal == null) miss.Add("playNormal");
            if (playHard == null) miss.Add("playHard");
            if (playSuperHard == null) miss.Add("playSuperHard");
            if (coinText == null) miss.Add("coinText");
            if (heartText == null) miss.Add("heartText");
            if (hover == null) miss.Add("hover");
            if (tabs.Length == 0) miss.Add("tabs");
            for (int i = 0; i < tabs.Length; i++)
                if (tabs[i].button == null || tabs[i].icon == null || tabs[i].label == null) miss.Add("tabs[" + i + "]");
            Check(sound, "sound", miss); Check(music, "music", miss); Check(vibration, "vibration", miss);
            return miss.Count == 0 ? null : string.Join(", ", miss);
        }

        static void Check(SettingsPopupView.ToggleRow r, string name, List<string> miss)
        {
            if (r == null || r.button == null || r.onState == null || r.offState == null) miss.Add(name);
        }
    }
}
