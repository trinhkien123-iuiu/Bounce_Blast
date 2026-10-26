using System.Collections.Generic;
using UnityEngine;
using BounceBlast.UI;

namespace BounceBlast.Gameplay
{
    /// <summary>Công cụ DEBUG (chỉ để test, không ảnh hưởng game thường)</summary>
    public class DebugManager : MonoBehaviour
    {
        [Tooltip("Tự bấm hộp đạn khi còn sức chứa – để test chơi hết màn")]
        public bool autoPlay;

        GameUI _ui;
        BoardController _board;
        LaunchManager _launch;
        bool _playing;
        float _autoTimer;

        /// <summary>Xử lý khởi tạo công cụ debug</summary>
        public void Init(GameUI ui, BoardController board, LaunchManager launch)
        {
            _ui = ui; _board = board; _launch = launch;
        }

        /// <summary>Nhận trạng thái đang chơi</summary>
        public void SetPlaying(bool playing) => _playing = playing;

        void Update()
        {
            if (_playing && autoPlay) AutoPlayStep();
        }

        void AutoPlayStep()
        {
            _autoTimer -= Time.deltaTime;
            if (_autoTimer > 0f || _ui.IsPopupOpen) return;
            _autoTimer = 0.6f;
            foreach (AmmoBox b in FindObjectsOfType<AmmoBox>())
            {
                if (!b.IsClickable || b.IsLaunched || !_launch.Capacity.CanAdd(b.Data.Balls)) continue;
                _launch.TryLaunch(b, false);
                return;
            }
        }

        [ContextMenu("Debug/Win now")]
        void DebugWin() { foreach (BoardBlock b in new List<BoardBlock>(_board.Blocks)) _board.DestroyBlock(b); }
    }
}
