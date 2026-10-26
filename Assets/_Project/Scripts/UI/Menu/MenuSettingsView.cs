using System;
using System.Collections.Generic;
using UnityEngine;
using BounceBlast.Gameplay;

namespace BounceBlast.UI
{
    /// <summary>Script quản lý của prefab MenuSettingsCanvas</summary>
    [DisallowMultipleComponent]
    public class MenuSettingsView : MonoBehaviour
    {
        [SerializeField] SettingsPopupView.ToggleRow sound = new SettingsPopupView.ToggleRow();
        [SerializeField] SettingsPopupView.ToggleRow music = new SettingsPopupView.ToggleRow();
        [SerializeField] SettingsPopupView.ToggleRow vibration = new SettingsPopupView.ToggleRow();

        public event Action<bool> OnSoundChanged;
        public event Action<bool> OnMusicChanged;
        public event Action<bool> OnVibrationChanged;

        void Awake()
        {
            HookToggle(sound, v => OnSoundChanged?.Invoke(v));
            HookToggle(music, v => OnMusicChanged?.Invoke(v));
            HookToggle(vibration, v => OnVibrationChanged?.Invoke(v));
        }

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

        /// <summary>Xử lý hiện trạng thái âm thanh, nhạc, rung</summary>
        public void SetValues(bool soundOn, bool musicOn, bool vibrationOn)
        {
            sound.value = soundOn; sound.Refresh();
            music.value = musicOn; music.Refresh();
            vibration.value = vibrationOn; vibration.Refresh();
        }

        /// <summary>Báo ô tham chiếu nào còn trống</summary>
        public string Validate()
        {
            var miss = new List<string>();
            Check(sound, "sound", miss); Check(music, "music", miss); Check(vibration, "vibration", miss);
            return miss.Count == 0 ? null : string.Join(", ", miss);
        }

        static void Check(SettingsPopupView.ToggleRow r, string name, List<string> miss)
        {
            if (r == null || r.button == null || r.onState == null || r.offState == null) miss.Add(name);
        }
    }
}
