using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>
    /// Đổi màu khung máng / nền theo độ khó (Normal giữ art config gốc của scene, Hard tông đỏ, Super Hard tông tối
    /// – gốc: KPHardLevelUIElement + ArtConfig). Field nào trong Theme để trống thì dùng lại material gốc.
    /// </summary>
    public class ThemeApplier : MonoBehaviour
    {
        Renderer _conveyor;
        Renderer _plane;
        SpriteRenderer _gradient;
        Camera _cam;
        Material[] _origConveyor;
        Material _origPlane;
        Color _origGradient;
        Color _origCamBg;
        Renderer _wall;
        Sprite _origMask;
        Renderer _pins;
        MaterialPropertyBlock _mpb;
        ConveyorSystem _conveyorSystem;

        public void Init(Transform boardRoot, Camera cam)
        {
            _cam = cam;
            if (_cam != null) _origCamBg = _cam.backgroundColor;
            if (boardRoot == null) return;
            Transform conv = boardRoot.Find("Conveyor_Container/Conveyor");
            if (conv != null) { _conveyor = conv.GetComponent<Renderer>(); _origConveyor = _conveyor.sharedMaterials; }
            Transform plane = boardRoot.Find("Environment Base (1)/Plane");
            if (plane != null) { _plane = plane.GetComponent<Renderer>(); _origPlane = _plane.sharedMaterial; }
            Transform wall = boardRoot.Find("Environment Base (1)/Wall");
            if (wall != null) _wall = wall.GetComponent<Renderer>();
            Transform pins = boardRoot.Find("Environment Base (1)/Triangle/Model/Triangle");
            if (pins != null) _pins = pins.GetComponent<Renderer>();
            _mpb = new MaterialPropertyBlock();
            Transform grad = boardRoot.Find("Gradient_Back/Mask NORMAL");
            if (grad != null) { _gradient = grad.GetComponent<SpriteRenderer>(); _origGradient = _gradient.color; }
        }

        public void SetConveyor(ConveyorSystem conveyor) => _conveyorSystem = conveyor;

        /// <summary>
        /// Theo ArtConfig gốc: ColorModularWallBoardGround là màu nền sau bàn (camera + gradient) và tông khung
        /// (Wall dùng chung material c1 cho mọi theme nên phải tô theo màu này); tam giác chia bi cùng tông;
        /// mũi tên máng = ColorConveyorArrows.
        /// </summary>
        void ApplyGround(GameConfig.Theme theme)
        {
            if (theme == null) return;
            if (_mpb == null) _mpb = new MaterialPropertyBlock(); // Apply có thể chạy trước Init (LoadLevel sớm ngay khi vào Play)
            if (_wall != null)
            {
                _wall.GetPropertyBlock(_mpb);
                Color w = theme.groundColor * theme.wallTint; w.a = 1f;
                _mpb.SetColor("_BaseColor", w);
                _wall.SetPropertyBlock(_mpb);
            }
            if (_pins != null)
            {
                _pins.GetPropertyBlock(_mpb);
                _mpb.SetColor("_BaseColor", theme.pinsColor);
                _mpb.SetColor("_HColor", Color.Lerp(theme.pinsColor, Color.white, 0.25f));
                _mpb.SetColor("_SColor", theme.pinsColor * 0.8f);
                _pins.SetPropertyBlock(_mpb);
            }
            if (_conveyorSystem != null) _conveyorSystem.SetArrowColor(theme.arrowColor);
        }

        public void Apply(GameConfig.Theme theme)
        {
            ApplyGround(theme);
            if (_conveyor != null && _origConveyor != null)
            {
                var mats = (Material[])_origConveyor.Clone();
                if (theme != null && mats.Length >= 4)
                {
                    if (theme.conveyorSurface) { mats[0] = theme.conveyorSurface; mats[2] = theme.conveyorSurface; }
                    if (theme.conveyorSide) mats[1] = theme.conveyorSide;
                    if (theme.conveyorGround) mats[3] = theme.conveyorGround;
                }
                _conveyor.sharedMaterials = mats;
            }
            if (_plane != null) _plane.sharedMaterial = theme != null && theme.plane ? theme.plane : _origPlane;
            if (_gradient != null)
            {
                // Sprite Mask/MaskHard/MaskSuperHard đã mang sẵn màu gradient gốc → để trắng (đo khớp ảnh gốc)
                if (_origMask == null) _origMask = _gradient.sprite;
                _gradient.sprite = theme != null && theme.backgroundMask != null ? theme.backgroundMask : _origMask;
                _gradient.color = Color.white;
            }
            if (_cam != null) _cam.backgroundColor = theme != null ? theme.groundColor : _origCamBg;
        }
    }
}
