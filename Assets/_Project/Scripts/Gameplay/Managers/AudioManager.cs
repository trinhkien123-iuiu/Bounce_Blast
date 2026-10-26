using System;
using System.Collections.Generic;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>Sự kiện âm thanh – map sang clip gốc theo enum KPAudioName của game</summary>
    public enum Sfx
    {
        Button, Error, TapAmmo, BoxLaunch, BallHit, BigBlockBreak, CrateBreak,
        IceCrack, IceBreak, QmarkReveal, TunnelOut, CapacityWarning, Win, Lose, Confetti, Hammer, Magic,
        HammerSwing, ShuffleWhoosh, ShufflePop, MagicHandTap, CapacityUp, BoosterUnlock, OutOfCapacity, StoneBreak, StoneHit, CoinCollect
    }

    /// <summary>Dùng cho cấu hình một âm thanh</summary>
    [Serializable]
    public class SoundEntry
    {
        public Sfx id;
        public AudioClip[] clips;
        [Range(0f, 1f)] public float volume = 0.8f;
        public float minInterval = 0.05f;
        public Vector2 pitch = new Vector2(1f, 1f);
    }

    /// <summary>Xử lý phát âm thanh và nhạc nền</summary>
    public class AudioManager : MonoBehaviour
    {
        static AudioManager _instance;

        public static bool SoundOn
        {
            get => PlayerPrefs.GetInt("bb_sound", 1) == 1;
            set { PlayerPrefs.SetInt("bb_sound", value ? 1 : 0); if (_instance) _instance.ApplyMute(); }
        }

        public static bool MusicOn
        {
            get => PlayerPrefs.GetInt("bb_music", 1) == 1;
            set { PlayerPrefs.SetInt("bb_music", value ? 1 : 0); if (_instance) _instance.ApplyMute(); }
        }

        readonly Dictionary<Sfx, SoundEntry> _table = new Dictionary<Sfx, SoundEntry>();
        readonly Dictionary<Sfx, float> _lastPlay = new Dictionary<Sfx, float>();
        AudioSource[] _sources;
        AudioSource _music;
        int _next;

        /// <summary>Xử lý khởi tạo bảng âm thanh</summary>
        public static void Init(GameObject host, SoundEntry[] sounds)
        {
            if (_instance == null)
            {
                _instance = host.GetComponent<AudioManager>();
                if (_instance == null) _instance = host.AddComponent<AudioManager>();
            }
            _instance.Setup(sounds);
        }

        void Setup(SoundEntry[] sounds)
        {
            _table.Clear();
            if (sounds != null) foreach (SoundEntry s in sounds) _table[s.id] = s;
            if (_sources == null)
            {
                _sources = new AudioSource[12];
                for (int i = 0; i < _sources.Length; i++)
                {
                    _sources[i] = gameObject.AddComponent<AudioSource>();
                    _sources[i].playOnAwake = false;
                }
                _music = gameObject.AddComponent<AudioSource>();
                _music.loop = true;
                _music.volume = 0.35f;
            }
            ApplyMute();
        }

        void ApplyMute()
        {
            if (_sources != null) foreach (AudioSource s in _sources) s.mute = !SoundOn;
            if (_music != null) _music.mute = !MusicOn;
        }

        /// <summary>Xử lý phát một hiệu ứng âm thanh</summary>
        public static void Play(Sfx id)
        {
            if (_instance == null || !_instance._table.TryGetValue(id, out SoundEntry e) || e.clips == null || e.clips.Length == 0) return;
            float now = Time.unscaledTime;
            if (_instance._lastPlay.TryGetValue(id, out float last) && now - last < e.minInterval) return;
            _instance._lastPlay[id] = now;

            AudioSource src = _instance._sources[_instance._next];
            _instance._next = (_instance._next + 1) % _instance._sources.Length;
            src.pitch = UnityEngine.Random.Range(e.pitch.x, e.pitch.y);
            src.PlayOneShot(e.clips[UnityEngine.Random.Range(0, e.clips.Length)], e.volume);
        }

        /// <summary>Xử lý phát nhạc nền</summary>
        public static void PlayMusic(AudioClip clip)
        {
            if (_instance == null || clip == null || _instance._music.clip == clip) return;
            _instance._music.clip = clip;
            _instance._music.Play();
        }
    }
}
