using UnityEngine;
using BounceBlast.UI;

namespace BounceBlast.Gameplay
{
    /// <summary>Cân camera game theo màn hình thật</summary>
    public class CameraFitter : MonoBehaviour
    {
        const float AmmoBoxZ = -0.6f;

        GameConfig _config;
        GameUI _ui;
        Camera _camera;
        LevelModel _level;
        float _fitAspect;
        Rect _fitSafe;

        static float AmmoBoxShift => ViewSpace.LegacyShift(AmmoBoxZ);

        /// <summary>Xử lý khởi tạo cân camera</summary>
        public void Init(GameConfig config, GameUI ui, Camera camera)
        {
            _config = config;
            _ui = ui;
            _camera = camera;
        }

        /// <summary>Level đang chơi (cần cho cỡ khay)</summary>
        public void SetLevel(LevelModel level) => _level = level;

        /// <summary>Mép dưới vùng khay đạn = mép trên hàng nút booster THẬT trên màn hình</summary>
        public float AmmoAreaBottom()
        {
            float px = _ui != null ? _ui.BoosterTopScreenY : -1f;
            if (_camera == null || px <= 0f) return float.NaN;
            Vector3 w = _camera.ScreenToWorldPoint(new Vector3(Screen.width * 0.5f, px, 24.2f));
            return w.y - AmmoBoxShift + _config.ammoBoosterGap;
        }

        /// <summary>Xử lý cân camera theo màn hình thật</summary>
        public void Fit()
        {
            if (_camera == null || _config == null) return;
            float frameTop = _config.frameTopY, gap = _config.ammoBoosterGap;
            float baseHalfW = _config.viewHalfWidth, maxZoomOut = _config.viewMaxZoomOut, minHalfHeight = _config.viewMinHalfHeight;
            float aspect = Mathf.Max(0.3f, _camera.aspect);
            float W = Mathf.Max(1, Screen.width), H = Mathf.Max(1, Screen.height);
            float hudY = _ui != null ? _ui.HudCenterScreenY : -1f;
            if (hudY <= 0f) hudY = Screen.safeArea.yMax;
            float boosterY = _ui != null ? _ui.BoosterTopScreenY : -1f;

            float halfW = baseHalfW;
            if (_level != null && boosterY > 0f && hudY > boosterY)
            {
                Vector2Int ext = AmmoGridController.VisibleExtent(_level);
                float target = Mathf.Min(_config.ammoCellMax, _config.ammoTargetCell, _config.ammoAreaWidth / ext.x);
                float need = (frameTop - _config.ammoAreaTop) + ext.y * target - AmmoBoxShift + gap;
                float k = need / (hudY - boosterY);
                float halfWNeed = k * ViewSpace.LegacyCos * W * 0.5f;
                halfW = Mathf.Clamp(halfWNeed, baseHalfW, baseHalfW * maxZoomOut);
            }
            float size = Mathf.Max(halfW / aspect, minHalfHeight);
            float halfH = size / ViewSpace.LegacyCos;
            halfW = size * aspect;
            float centerY = frameTop - (Mathf.Clamp01(hudY / H) - 0.5f) * 2f * halfH;
            _camera.orthographic = true;
            _camera.orthographicSize = size;
            _camera.transform.SetPositionAndRotation(new Vector3(5.308f, centerY, -24.8f), Quaternion.identity);
            _camera.nearClipPlane = 0.1f;
            _camera.farClipPlane = 100f;
            _camera.projectionMatrix = Matrix4x4.Ortho(-halfW, halfW, -halfH, halfH, _camera.nearClipPlane, _camera.farClipPlane);
            _fitAspect = _camera.aspect;
            _fitSafe = Screen.safeArea;
        }

        void Update()
        {
            if (_camera != null && (!Mathf.Approximately(_camera.aspect, _fitAspect) || Screen.safeArea != _fitSafe)) Fit();
        }
    }
}
