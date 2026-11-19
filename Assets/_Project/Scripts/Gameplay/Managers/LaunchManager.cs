using System;
using UnityEngine;
using BounceBlast.UI;

namespace BounceBlast.Gameplay
{
    /// <summary>Bấm hộp ammo → bay vào máy xay trống (GrinderQueue) + luật thua (hàng chờ kẹt / hết nước đi)</summary>
    public class LaunchManager : MonoBehaviour
    {
        [Header("Debug (read only)")]
        public int grindersBusy;
        public int blocksLeft;

        /// <summary>Hết cách chơi: "Queue full" (5 máy kẹt) hoặc "No more moves"</summary>
        public event Action<string> OnGameOver;

        GameUI _ui;
        BoardController _board;
        AmmoGridController _grid;
        GrinderQueue _grinders;
        BoosterManager _boosters;
        LevelManager _level;
        bool _playing;
        float _idleTimer;
        float _deadlockTimer;

        public GrinderQueue Grinders => _grinders;

        /// <summary>Xử lý khởi tạo</summary>
        public void Init(GameUI ui, BoardController board, AmmoGridController grid, GrinderQueue grinders, BoosterManager boosters, LevelManager level)
        {
            _ui = ui; _board = board; _grid = grid; _grinders = grinders; _boosters = boosters; _level = level;
            _grid.OnBoxLaunched += box => _grinders.Receive(box);
            _level.OnLevelLoading += () => _grinders.ClearAll();
            _level.OnLevelLoaded += _ => _deadlockTimer = 0f;
        }

        /// <summary>GameManager báo trạng thái</summary>
        public void SetPlaying(bool playing) => _playing = playing;

        void Update()
        {
            grindersBusy = _grinders != null ? _grinders.BusyCount : 0;
            blocksLeft = _board != null ? _board.DestructibleCount : 0;
            if (_playing) CheckDeadlock();
        }

        /// <summary>Bấm hộp: ammo nào cũng vào được, miễn còn máy xay trống</summary>
        public void TryLaunch(AmmoBox box, bool forced, bool tapSoundDone = false)
        {
            if (box == null || box.Data == null) return;
            if (!_grinders.HasFree) { AudioManager.Play(Sfx.Error); return; }
            if (forced) _boosters.ConsumeMagicHand(box);
            if (!tapSoundDone) AudioManager.Play(Sfx.TapAmmo);
            AudioManager.Play(Sfx.BoxLaunch);
            _grid.Launch(box, forced);
            _deadlockTimer = 0f;
        }

        /// <summary>Thua khi không còn gì làm được trong 1.5 giây</summary>
        void CheckDeadlock()
        {
            _idleTimer += Time.deltaTime;
            if (_idleTimer < 0.5f) return;
            _idleTimer = 0f;
            if (_grinders.IsProgressing || (_grinders.HasFree && _grid.AnyClickable())) { _deadlockTimer = 0f; return; }
            _deadlockTimer += 0.5f;
            if (_deadlockTimer >= 1.5f) OnGameOver?.Invoke(_grinders.HasFree ? "No more moves" : "Queue full");
        }
    }
}
