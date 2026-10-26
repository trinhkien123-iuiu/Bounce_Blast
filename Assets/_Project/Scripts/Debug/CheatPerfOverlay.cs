using System.Text;
using TMPro;
using UnityEngine;
using BounceBlast.Gameplay;

namespace BounceBlast.Cheats
{
    /// <summary>Số liệu hiệu năng góc màn hình</summary>
    public class CheatPerfOverlay : MonoBehaviour
    {
        [SerializeField] TMP_Text label;

#if UNITY_EDITOR || DEVELOPMENT_BUILD || BB_CHEATS
        public bool Visible
        {
            get => label != null && label.gameObject.activeSelf;
            set { if (label != null) label.gameObject.SetActive(value); }
        }

        float _acc; int _frames; float _fps;
        float _worst, _worstWindow;
        int _slow, _slowWindow;
        float _window;
        readonly StringBuilder _sb = new StringBuilder(128);

        void Update()
        {
            if (!Visible) return;
            float dt = Time.unscaledDeltaTime;
            _acc += dt; _frames++;
            _window += dt;
            if (dt > _worstWindow) _worstWindow = dt;
            if (dt > 0.033f) _slowWindow++;
            if (_window >= 3f) { _worst = _worstWindow; _slow = _slowWindow; _worstWindow = 0f; _slowWindow = 0; _window = 0f; }
            if (_acc < 0.5f) return;
            _fps = _frames / _acc; _acc = 0f; _frames = 0;

            GameManager gm = GameManager.Instance;
            _sb.Length = 0;
            _sb.Append("FPS ").Append(_fps.ToString("0")).Append("   worst ").Append((_worst * 1000f).ToString("0")).Append(" ms   slow ").Append(_slow).Append('\n');
            _sb.Append("mem ").Append((UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / 1048576L)).Append(" MB");
            if (gm != null && gm.board != null && gm.balls != null)
                _sb.Append("   blocks ").Append(gm.board.Blocks.Count).Append("   balls ").Append(gm.balls.ActiveCount);
            if (gm != null && gm.levelManager != null) _sb.Append("   Lv ").Append(gm.levelManager.currentLevel);
            label.text = _sb.ToString();
        }
#endif
    }
}
