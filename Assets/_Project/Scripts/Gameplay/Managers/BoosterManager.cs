using UnityEngine;
using BounceBlast.UI;

namespace BounceBlast.Gameplay
{
    /// <summary>
    /// Quản lý BOOSTER (Hammer, Magic Hand, Shuffle, Capacity): xử lý khi bấm nút booster, giữ booster đang "lên cò",
    /// và đẩy số lượng / trạng thái lên thanh booster của UI. Dữ liệu số lượng nằm trong <see cref="BoosterSystem"/>.
    /// </summary>
    public class BoosterManager : MonoBehaviour
    {
        public BoosterSystem Boosters { get; } = new BoosterSystem();

        /// <summary>Booster đang lên cò (Hammer / MagicHand) – InputManager đọc để biết cú bấm tiếp theo làm gì.</summary>
        public BoosterType? Armed => Boosters.Armed;

        GameUI _ui;
        BoardController _board;
        AmmoGridController _grid;
        LaunchManager _launch;
        LevelManager _level;
        bool _playing;

        public void Init(GameUI ui, BoardController board, AmmoGridController grid, LaunchManager launch, LevelManager level)
        {
            _ui = ui; _board = board; _grid = grid; _launch = launch; _level = level;
            _ui.OnBoosterPressed += HandleBooster;
            Boosters.OnChanged += RefreshUi;
            _level.OnLevelLoaded += _ => { Boosters.ResetArmed(); RefreshUi(); };
        }

        public void SetPlaying(bool playing) => _playing = playing;

        void HandleBooster(BoosterType type)
        {
            if (!_playing) return;
            if (Boosters.Count(type) <= 0)
            {
                // Hết lượt → popup mua (game gốc trả coin; bản clone chưa có cửa hàng nên bấm mua là nhận luôn)
                _ui.ShowBoosterBuy(type, _level.Level.Difficulty, (t, n) =>
                {
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
                    _grid.ShuffleColors();
                    AudioManager.Play(Sfx.Magic);
                    break;
                case BoosterType.Capacity:
                    Boosters.Consume(type);
                    _launch.Capacity.Increase(0.25f);
                    AudioManager.Play(Sfx.Magic);
                    _ui.Toast("Capacity +25%");
                    break;
            }
        }

        /// <summary>Dùng búa lên một khối (InputManager gọi khi Hammer đang lên cò và người chơi bấm vào khối).</summary>
        public void UseHammer(BoardBlock block)
        {
            if (block == null || !block.IsDestructible) return;
            Boosters.Consume(BoosterType.Hammer);
            AudioManager.Play(Sfx.Hammer);
            _board.DestroyBlock(block);
        }

        /// <summary>Trừ một lượt Magic Hand (LaunchManager gọi khi bắn hộp bằng Magic Hand).</summary>
        public void ConsumeMagicHand() => Boosters.Consume(BoosterType.MagicHand);

        void RefreshUi()
        {
            foreach (BoosterType t in System.Enum.GetValues(typeof(BoosterType)))
                _ui.SetBooster(t, Boosters.Count(t), Boosters.Armed == t);
        }
    }
}
