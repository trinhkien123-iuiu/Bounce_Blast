using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BounceBlast.UI;

namespace BounceBlast.Gameplay
{
    /// <summary>Quản lý BOOSTER (Hammer, Magic Hand, Shuffle, Capacity)</summary>
    public class BoosterManager : MonoBehaviour
    {
        public BoosterSystem Boosters { get; } = new BoosterSystem();

        /// <summary>Booster đang lên cò (Hammer / MagicHand)</summary>
        public BoosterType? Armed => Boosters.Armed;

        GameConfig _config;
        GameUI _ui;
        BoardController _board;
        AmmoGridController _grid;
        LaunchManager _launch;
        LevelManager _level;
        bool _playing;

        /// <summary>Xử lý khởi tạo quản lý booster</summary>
        public void Init(GameConfig config, Camera cam, GameUI ui, BoardController board, AmmoGridController grid, LaunchManager launch, LevelManager level)
        {
            _config = config;
            BoosterFx.Create(config, cam, Vfx.Root != null ? Vfx.Root : transform);
            _ui = ui; _board = board; _grid = grid; _launch = launch; _level = level;
            _ui.OnBoosterPressed += HandleBooster;
            Boosters.OnChanged += RefreshUi;
            _level.OnLevelLoaded += _ => { Boosters.ResetArmed(); RefreshLocks(); RefreshUi(); };
        }

        /// <summary>Nhận trạng thái đang chơi</summary>
        public void SetPlaying(bool playing) => _playing = playing;

        void HandleBooster(BoosterType type)
        {
            if (!_playing) return;
            if (IsLocked(type))
            {
                _ui.ShowBoosterLockHint(type, _config.BoosterUnlockLevel(type));
                AudioManager.Play(Sfx.Error);
                return;
            }
            if (Boosters.Count(type) <= 0)
            {
                _ui.ShowBoosterBuy(type, _level.Level.Difficulty, (t, n, price) =>
                {
                    if (!PlayerData.TrySpendCoins(price))
                    {
                        AudioManager.Play(Sfx.Error);
                        _ui.Toast("Not enough coins!");
                        return;
                    }
                    Boosters.Add(t, n);
                    AudioManager.Play(Sfx.Magic);
                    _ui.Toast("+" + n + " " + t);
                });
                return;
            }

            switch (type)
            {
                case BoosterType.Hammer:
                case BoosterType.MagicHand:
                    Boosters.Toggle(type);
                    if (Boosters.Armed == BoosterType.Hammer) _ui.Toast("Tap a block to smash it!");
                    else if (Boosters.Armed == BoosterType.MagicHand) _ui.Toast("Tap any ammo box!");
                    break;
                case BoosterType.Shuffle:
                    Boosters.Consume(type);
                    List<Vector3> pos = _grid.BoxPositions(out Vector3 center, out float radius);
                    if (BoosterFx.Instance != null) BoosterFx.Instance.Shuffle(center, pos, radius);
                    _grid.ShuffleColors();
                    break;
                case BoosterType.Capacity:
                    Boosters.Consume(type);
                    const float capacityBonus = 0.25f;
                    Transform bar = _ui.CapacityBarTransform;
                    if (BoosterFx.Instance != null && bar != null) BoosterFx.Instance.Capacity(bar.position, bar, capacityBonus, () => _launch.Capacity.Increase(capacityBonus));
                    else { _launch.Capacity.Increase(capacityBonus); AudioManager.Play(Sfx.Magic); }
                    _ui.Toast("Capacity +25%");
                    break;
            }
        }

        /// <summary>Dùng búa (InputManager gọi khi Hammer đang lên cò và người chơi bấm vào khối)</summary>
        public void UseHammer(Vector2 point, BoardBlock block)
        {
            if (block == null || !block.IsDestructible || !block.Rules.Tappable) return;
            Boosters.Consume(BoosterType.Hammer);
            float radius = _board.CellSize * _config.hammerRadiusCells;
            if (BoosterFx.Instance != null) BoosterFx.Instance.Hammer(point, radius, () => StartCoroutine(SmashArea(point, radius, block)));
            else { AudioManager.Play(Sfx.Hammer); StartCoroutine(SmashArea(point, radius, block)); }
        }

        IEnumerator SmashArea(Vector2 point, float radius, BoardBlock tapped)
        {
            var hits = new List<BoardBlock>();
            var seen = new HashSet<BoardBlock>();
            foreach (Collider2D col in Physics2D.OverlapCircleAll(point, radius))
            {
                BoardBlock b = col.GetComponent<BoardBlock>();
                if (b == null || !b.IsDestructible || !b.Rules.Tappable || b.IsDestroyed || (b != tapped && (b.Cells.width > 1 || b.Cells.height > 1))) continue;
                if (seen.Add(b)) hits.Add(b);
            }
            if (tapped != null && seen.Add(tapped)) hits.Add(tapped);
            var dist = new Dictionary<BoardBlock, float>();
            foreach (BoardBlock b in hits) dist[b] = ((Vector2)b.transform.position - point).magnitude;
            hits.Sort((a, c) => dist[a].CompareTo(dist[c]));
            float cell = Mathf.Max(_board.CellSize, 0.01f), start = Time.time;
            foreach (BoardBlock b in hits)
            {
                float due = dist[b] / cell * _config.hammerWaveDelay;
                while (b != null && Time.time - start < due) yield return null;
                if (b != null && !b.IsDestroyed) { _board.DestroyBlock(b); AudioManager.Play(Sfx.BigBlockBreak); }
            }
        }

        /// <summary>Trừ một lượt Magic Hand (LaunchManager gọi khi bắn hộp bằng Magic Hand) và chiếu hiệu ứng bàn tay phép lên hộp</summary>
        public void ConsumeMagicHand(AmmoBox box)
        {
            Boosters.Consume(BoosterType.MagicHand);
            if (box == null || BoosterFx.Instance == null) { AudioManager.Play(Sfx.Magic); return; }
            Color c = box.Data != null && _colors != null ? _colors.ColorOf(box.Data.ColorCode) : Color.white;
            BoosterFx.Instance.MagicHand(box.transform.position, c);
            box.Pop();
        }

        ColorLibrary _colors;
        /// <summary>Nhận bảng màu cho hiệu ứng</summary>
        public void SetColors(ColorLibrary colors) => _colors = colors;

        bool IsLocked(BoosterType t) => _level.CurrentLevel < _config.BoosterUnlockLevel(t);

        /// <summary>Khoá booster chưa tới level mở</summary>
        void RefreshLocks()
        {
            var fresh = new List<BoosterType>();
            foreach (BoosterType t in System.Enum.GetValues(typeof(BoosterType)))
            {
                if (IsLocked(t)) { _ui.SetBoosterLocked(t, true); continue; }
                if (PlayerPrefs.GetInt("bb_booster_unlocked_" + t, 0) == 0) { fresh.Add(t); _ui.SetBoosterLocked(t, true); }
                else _ui.SetBoosterLocked(t, false);
            }
            fresh.Sort((a, b) => _config.BoosterUnlockLevel(a).CompareTo(_config.BoosterUnlockLevel(b)));
            _pending = fresh;
        }

        List<BoosterType> _pending = new List<BoosterType>();

        /// <summary>Chơi hoạt cảnh + popup mở khoá các booster còn chờ, xong thì gọi done</summary>
        public void PlayPendingUnlocks(Action done)
        {
            var list = _pending; _pending = new List<BoosterType>();
            if (list.Count == 0) { done?.Invoke(); return; }
            AnnounceUnlock(list, 0, done);
        }

        void AnnounceUnlock(List<BoosterType> list, int index, Action done)
        {
            if (index >= list.Count) { done?.Invoke(); return; }
            BoosterType t = list[index];
            PlayerPrefs.SetInt("bb_booster_unlocked_" + t, 1);
            int free = _config.freeBoosterOnUnlock;
            Boosters.Add(t, free);
            _ui.PlayBoosterUnlock(t, () =>
            {
                AudioManager.Play(Sfx.BoosterUnlock);
                RefreshUi();
                if (BoosterFx.Instance != null) BoosterFx.Instance.Unlock(_ui.BoosterButtonScreenPos(t));
            },
            () => _ui.ShowBoosterTutorial(t, _level.Level.Difficulty, HandleBooster, () => AnnounceUnlock(list, index + 1, done), free));
        }

        void RefreshUi()
        {
            foreach (BoosterType t in System.Enum.GetValues(typeof(BoosterType)))
                _ui.SetBooster(t, Boosters.Count(t), Boosters.Armed == t);
        }
    }
}
