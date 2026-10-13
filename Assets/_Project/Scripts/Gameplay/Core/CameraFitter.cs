using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>
    /// Cân camera game theo màn hình thật: bố cục (như game gốc) mép trên khung máng NGANG TÂM hàng HUD (Lv, Settings đè lên
    /// góc khung), khung phủ ~90% bề ngang. Khay đạn nằm giữa thanh Capacity và hàng booster; màn hình thấp (tai thỏ,
    /// 19:9…) không đủ chỗ cho ô khay ≥ ammoTargetCell thì THU NHỎ cả cảnh (đỉnh khung vẫn ở tâm HUD) tối đa viewMaxZoomOut,
    /// quá nữa thì ô khay mới co lại. Không ô nào bị booster đè. Mọi số nằm ở GameConfig ("Bố cục màn hình").
    /// Camera game nhìn THẲNG (một hướng nhìn cho cả game – xem ViewSpace); camera gốc nghiêng 20° nên chiều dọc bị ép cos20,
    /// giữ đúng tỉ lệ đó bằng ma trận chiếu ortho ép dọc.
    /// </summary>
    public class CameraFitter : MonoBehaviour
    {
        const float AmmoBoxZ = -0.6f; // mặt hộp đạn

        GameConfig _config;
        GameUI _ui;
        Camera _camera;
        LevelModel _level;
        float _fitAspect;
        Rect _fitSafe;

        static float AmmoBoxShift => ViewSpace.LegacyShift(AmmoBoxZ); // độ dời y của mặt đó theo góc nhìn cũ

        public void Init(GameConfig config, GameUI ui, Camera camera)
        {
            _config = config;
            _ui = ui;
            _camera = camera;
        }

        /// <summary>Level đang chơi (cần cho cỡ khay); gọi trước <see cref="Fit"/>.</summary>
        public void SetLevel(LevelModel level) => _level = level;

        /// <summary>
        /// Mép dưới vùng khay đạn = mép trên hàng nút booster THẬT trên màn hình (+ khe) → không ô nào bị booster đè.
        /// NaN nếu chưa đo được.
        /// </summary>
        public float AmmoAreaBottom()
        {
            float px = _ui != null ? _ui.BoosterTopScreenY : -1f;
            if (_camera == null || px <= 0f) return float.NaN;
            Vector3 w = _camera.ScreenToWorldPoint(new Vector3(Screen.width * 0.5f, px, 24.2f)); // mặt hộp z = -0.6
            return w.y - AmmoBoxShift + _config.ammoBoosterGap;
        }

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
                // chiều cao khay cần (đv thế giới, theo góc nhìn cũ như ammoAreaTop) – khoảng HUD→booster trên màn hình phải chứa
                // frameTop → ammoAreaTop (cố định theo thế giới) + khay + khe
                float need = (frameTop - _config.ammoAreaTop) + ext.y * target - AmmoBoxShift + gap;
                float k = need / (hudY - boosterY);                                 // đv thế giới / pixel dọc cần có
                float halfWNeed = k * ViewSpace.LegacyCos * W * 0.5f;               // k = 2·halfW / (cos20 · W)
                halfW = Mathf.Clamp(halfWNeed, baseHalfW, baseHalfW * maxZoomOut);
            }
            float size = Mathf.Max(halfW / aspect, minHalfHeight);   // nửa chiều cao màn hình (đv màn hình); máy bè: kẹp dọc
            float halfH = size / ViewSpace.LegacyCos;                 // nửa vùng nhìn trên z = 0 (đv thế giới)
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
            // ma trận chiếu tự đặt không tự cập nhật khi đổi tỉ lệ màn hình (xoay máy / đổi device trong Simulator)
            if (_camera != null && (!Mathf.Approximately(_camera.aspect, _fitAspect) || Screen.safeArea != _fitSafe)) Fit();
        }
    }
}
