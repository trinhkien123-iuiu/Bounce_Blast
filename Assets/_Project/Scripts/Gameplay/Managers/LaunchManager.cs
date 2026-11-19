using System;
using System.Collections;
using UnityEngine;
using BounceBlast.UI;

namespace BounceBlast.Gameplay
{
    /// <summary>Quản lý BẮN HỘP ĐẠN và SỨC CHỨA</summary>
    public class LaunchManager : MonoBehaviour
    {
        [Header("Debug (read only)")]
        public int capacityUsed;
        public float capacityMax;
        [Tooltip("Bi đã rời khay mà chưa tiêu hao (máng + bàn + hộp đang nhả)")] public int ballsInSystem;
        public int blocksLeft;

        /// <summary>Hết cách chơi (hết sức chứa mà bỏ cuộc, hoặc không còn nước đi)</summary>
        public event Action<string> OnGameOver;

        public CapacitySystem Capacity { get; } = new CapacitySystem();

        GameUI _ui;
        BoardController _board;
        AmmoGridController _grid;
        ConveyorSystem _conveyor;
        GrinderQueue _grinders;
        BallManager _balls;
        BoosterManager _boosters;
        LevelManager _level;
        bool _playing;
        bool _started;
        bool _overflowPending;
        float _idleTimer;
        float _deadlockTimer;

        /// <summary>Xử lý khởi tạo quản lý bắn hộp</summary>
        public void Init(GameUI ui, BoardController board, AmmoGridController grid, ConveyorSystem conveyor, BallManager balls,
                         BoosterManager boosters, LevelManager level)
        {
            _ui = ui; _board = board; _grid = grid; _conveyor = conveyor; _balls = balls; _boosters = boosters; _level = level;
            _balls.OnBallConsumed += _ => Capacity.Remove(1);
            _grid.OnBoxLaunched += box => { if (_grinders != null) _grinders.Receive(box); else _conveyor.Send(box); };
            _level.OnLevelLoading += AbortPending;
            _level.OnLevelLoaded += model => ResetForLevel(model);
        }

        /// <summary>Crab Mode: dùng hàng máy xay thay cho máng + sức chứa (null = mode cũ)</summary>
        public void UseGrinders(GrinderQueue grinders) => _grinders = grinders;

        /// <summary>GameManager báo trạng thái</summary>
        public void SetPlaying(bool playing) => _playing = playing;

        void ResetForLevel(LevelModel model)
        {
            _overflowPending = false;
            Capacity.Reset(model.Capacity);
            _started = false;
            _deadlockTimer = 0f;
        }

        void AbortPending()
        {
            StopAllCoroutines();
            if (_grinders != null) _grinders.ClearAll();
            _overflowPending = false;
            Capacity.ForceFull = false;
        }

        void Update()
        {
            _ui.SetCapacity(Capacity.Fill, _started, Capacity.Infinite);
            capacityUsed = Capacity.Used;
            capacityMax = Capacity.Max;
            ballsInSystem = Capacity.Used;
            blocksLeft = _board.DestructibleCount;
            if (_playing) CheckDeadlock();
        }

        /// <summary>Xử lý bắn hộp, hết sức chứa thì hiện Out Of Capacity</summary>
        public void TryLaunch(AmmoBox box, bool forced, bool tapSoundDone = false)
        {
            if (_overflowPending) return;
            if (_grinders != null)
            {
                // Crab Mode: ammo nào cũng vào được, miễn còn máy xay trống
                if (!_grinders.HasFree) { AudioManager.Play(Sfx.Error); return; }
                LaunchNow(box, forced, tapSoundDone);
                return;
            }
            if (!Capacity.CanAdd(box.Data.Balls))
            {
                _overflowPending = true;
                _started = true;
                Capacity.ForceFull = true;
                _conveyor.HoldBoxMidFlight(box);
                AudioManager.Play(Sfx.Error);
                StartCoroutine(OutOfCapacityRoutine(box, forced, tapSoundDone));
                return;
            }
            LaunchNow(box, forced, tapSoundDone);
        }

        void LaunchNow(AmmoBox box, bool forced, bool tapSoundDone)
        {
            if (forced)
            {
                _boosters.ConsumeMagicHand(box);
            }
            _started = true;
            if (_grinders == null) Capacity.Add(box.Data.Balls);
            if (!tapSoundDone) AudioManager.Play(Sfx.TapAmmo);
            AudioManager.Play(Sfx.BoxLaunch);
            _grid.Launch(box, forced);
            _deadlockTimer = 0f;
        }

        IEnumerator OutOfCapacityRoutine(AmmoBox box, bool forced, bool tapSoundDone)
        {
            yield return new WaitForSeconds(0.6f);
            if (!_playing || box == null) { _overflowPending = false; Capacity.ForceFull = false; yield break; }
            ShowOutOfCapacityPopup(box, forced, tapSoundDone);
        }

        /// <summary>Xử lý hiện popup hết sức chứa; Play On trừ xu, không đủ xu thì báo và hiện lại popup</summary>
        void ShowOutOfCapacityPopup(AmmoBox box, bool forced, bool tapSoundDone)
        {
            _ui.ShowOutOfCapacity(_level.Level.Difficulty,
                onAddCapacity: () =>
                {
                    if (!PlayerData.TrySpendCoins(PlayerData.PlayOnCost))
                    {
                        AudioManager.Play(Sfx.Error);
                        _ui.Toast("Not enough coins!");
                        ShowOutOfCapacityPopup(box, forced, tapSoundDone);
                        return;
                    }
                    Capacity.Increase(0.25f);
                    AudioManager.Play(Sfx.Magic);
                    Capacity.ForceFull = false;
                    _overflowPending = false;
                    LaunchNow(box, forced, tapSoundDone);
                },
                onGiveUp: () => { Capacity.ForceFull = false; _overflowPending = false; OnGameOver?.Invoke("Out of capacity"); });
        }

        /// <summary>Thua khi không còn gì làm được</summary>
        void CheckDeadlock()
        {
            _idleTimer += Time.deltaTime;
            if (_idleTimer < 0.5f) return;
            _idleTimer = 0f;

            if (_grinders != null)
            {
                // Crab Mode: còn tiến triển, hoặc còn máy trống + hộp bấm được = người chơi còn nước đi
                if (_grinders.IsProgressing || (_grinders.HasFree && _grid.AnyClickable())) { _deadlockTimer = 0f; return; }
                _deadlockTimer += 0.5f;
                if (_deadlockTimer >= 1.5f) OnGameOver?.Invoke(_grinders.HasFree ? "No more moves" : "Queue full");
                return;
            }
            bool canAct = _grid.AnyClickable() || (_grinders != null ? _grinders.IsBusy : _conveyor.IsBusy || _balls.AnyBallHasTarget());
            if (canAct) { _deadlockTimer = 0f; return; }
            _deadlockTimer += 0.5f;
            if (_deadlockTimer >= 2.5f) OnGameOver?.Invoke("No more moves");
        }
    }
}
