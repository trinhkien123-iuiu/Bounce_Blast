using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>
    /// Bảo hiểm cho máy yếu: nếu fps trung bình 3 giây liên tiếp thấp hơn <see cref="LowFps"/> (hai lần liền) thì hạ độ phân giải
    /// một nấc (×0.85, sàn <see cref="MinWidth"/> px). Chỉ hạ, không tăng lại để khỏi nhấp nháy. Chỉ đếm khi đang chơi (timeScale &gt; 0).
    /// </summary>
    public class AdaptiveResolution : MonoBehaviour
    {
        const float LowFps = 45f, Window = 3f, Step = 0.85f;
        const int MinWidth = 640;

        float _t, _acc; int _frames, _strikes;

        void Update()
        {
            if (Time.timeScale <= 0f) { _t = _acc = 0f; _frames = 0; return; }
            float dt = Time.unscaledDeltaTime;
            if (dt > 0.25f) return;            // bỏ khung nạp màn / tạm dừng app
            _t += dt; _acc += dt; _frames++;
            if (_t < Window) return;
            float fps = _frames / _acc;
            _t = _acc = 0f; _frames = 0;
            _strikes = fps < LowFps ? _strikes + 1 : 0;
            if (_strikes < 2) return;
            _strikes = 0;
            int w = Mathf.RoundToInt(Screen.width * Step);
            if (w < MinWidth) return;
            Screen.SetResolution(w, Mathf.RoundToInt(Screen.height * Step), true);
        }
    }
}
