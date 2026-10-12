using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BounceBlast.Gameplay
{
    /// <summary>
    /// Script quản lý của popup SETTINGS – gắn trên gốc SettingsCanvas.prefab.
    ///
    /// Ba công tắc (âm thanh / nhạc / rung) giữ trạng thái bật-tắt của riêng nó và chỉ báo ra ngoài bằng sự kiện;
    /// việc ghi vào AudioManager / PlayerPrefs là của GameUI, View không biết đến.
    /// Phần nảy vào / mở CanvasGroup nằm ở <see cref="PopupViewBase"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public class SettingsPopupView : PopupViewBase
    {
        /// <summary>Một hàng công tắc: nút bấm + hình khi BẬT + hình khi TẮT.</summary>
        [Serializable]
        public class ToggleRow
        {
            [Tooltip("Nút bấm")] public Button button;
            [Tooltip("Hình khi đang BẬT (Parent Active)")] public GameObject onState;
            [Tooltip("Hình khi đang TẮT (Parent Passive)")] public GameObject offState;

            [NonSerialized] public bool value;

            public void Refresh()
            {
                if (onState != null) onState.SetActive(value);
                if (offState != null) offState.SetActive(!value);
            }
        }

        [Header("Riêng popup Settings")]
        [SerializeField] Button closeButton;
        [SerializeField] Button restartButton;
        [Tooltip("Nút Home – hiện tại cũng chơi lại màn")] [SerializeField] Button homeButton;
        [SerializeField] ToggleRow sound = new ToggleRow();
        [SerializeField] ToggleRow music = new ToggleRow();
        [SerializeField] ToggleRow vibration = new ToggleRow();

        public event Action OnClose;
        public event Action OnRestart;
        public event Action<bool> OnSoundChanged;
        public event Action<bool> OnMusicChanged;
        public event Action<bool> OnVibrationChanged;

        void Awake()
        {
            Hook(closeButton, () => OnClose?.Invoke());
            Hook(restartButton, () => OnRestart?.Invoke());
            Hook(homeButton, () => OnRestart?.Invoke());
            HookToggle(sound, v => OnSoundChanged?.Invoke(v));
            HookToggle(music, v => OnMusicChanged?.Invoke(v));
            HookToggle(vibration, v => OnVibrationChanged?.Invoke(v));
        }

        static void Hook(Button b, Action act)
        {
            if (b == null) return;
            b.onClick.AddListener(() => { AudioManager.Play(Sfx.Button); act(); });
        }

        static void HookToggle(ToggleRow row, Action<bool> changed)
        {
            if (row == null || row.button == null) return;
            row.button.onClick.AddListener(() =>
            {
                row.value = !row.value;
                row.Refresh();
                changed(row.value);              // báo ra ngoài TRƯỚC (có thể vừa tắt âm) rồi mới phát tiếng bấm
                AudioManager.Play(Sfx.Button);
            });
        }

        /// <summary>Mở popup với trạng thái hiện tại của ba công tắc.</summary>
        public void Show(LevelDifficulty difficulty, bool soundOn, bool musicOn, bool vibrationOn)
        {
            sound.value = soundOn; sound.Refresh();
            music.value = musicOn; music.Refresh();
            vibration.value = vibrationOn; vibration.Refresh();
            OpenBase(difficulty);
        }

        protected override void CollectMissing(List<string> missing)
        {
            if (closeButton == null) missing.Add("closeButton");
            if (restartButton == null) missing.Add("restartButton");
            if (homeButton == null) missing.Add("homeButton");
            Check(sound, "sound", missing);
            Check(music, "music", missing);
            Check(vibration, "vibration", missing);
        }

        static void Check(ToggleRow r, string name, List<string> missing)
        {
            if (r == null) { missing.Add(name); return; }
            if (r.button == null) missing.Add(name + ".button");
            if (r.onState == null) missing.Add(name + ".onState");
            if (r.offState == null) missing.Add(name + ".offState");
        }
    }
}
