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
        readonly System.Collections.Generic.List<CrusherHousing> _housings = new System.Collections.Generic.List<CrusherHousing>();
        Color _normalGround = new Color(0.282f, 0.314f, 0.682f);
        readonly System.Collections.Generic.Dictionary<Color, Material[]> _crusherCache = new System.Collections.Generic.Dictionary<Color, Material[]>();

        /// <summary>Vỏ máy xay nhìn thấy (Shredder/Plane.028, material "Tunnel") và cụm vỏ dự phòng polySurface11 ("Blue" + đế). Chỉ đổi các slot đầu, không đụng lưỡi cắt.</summary>
        struct CrusherHousing { public Renderer renderer; public Material[] original; public int slots; }
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
            foreach (Renderer r in boardRoot.GetComponentsInChildren<Renderer>(true))
            {
                Material[] sm = r.sharedMaterials;
                if (sm.Length == 0 || sm[0] == null) continue;
                if (r.name == "polySurface11" && sm.Length >= 2 && sm[0].name == "Blue")
                    _housings.Add(new CrusherHousing { renderer = r, original = sm, slots = 2 });
                else if (r.name == "Plane.028" && sm[0].name == "Tunnel" && r.transform.parent != null && r.transform.parent.name == "Shredder")
                    _housings.Add(new CrusherHousing { renderer = r, original = sm, slots = 1 });
            }
            Transform grad = boardRoot.Find("Gradient_Back/Mask NORMAL");
            if (grad != null) { _gradient = grad.GetComponent<SpriteRenderer>(); _origGradient = _gradient.color; }
        }

        public void SetConveyor(ConveyorSystem conveyor) => _conveyorSystem = conveyor;

        /// <summary>groundColor của theme Normal – mốc để tính hướng đổi màu cho các theme khác.</summary>
        public void SetReferenceGround(Color normalGround) => _normalGround = normalGround;

        /// <summary>
        /// Vỏ máy xay Normal là xanh thép. Theme khác đổi theo CÙNG hướng với nền: tông (hue) dời bằng chênh lệch hue của groundColor,
        /// độ bão hòa nhân theo tỉ lệ bão hòa, độ sáng nhân căn bậc hai tỉ lệ sáng (Super Hard xám tối → vỏ xám tối).
        /// </summary>
        void ApplyCrusher(GameConfig.Theme theme)
        {
            if (_housings.Count == 0) return;
            foreach (CrusherHousing h in _housings)
            {
                if (h.renderer == null) continue;
                if (theme == null) { h.renderer.sharedMaterials = h.original; continue; }
                var mats = (Material[])h.original.Clone();
                for (int i = 0; i < h.slots && i < mats.Length; i++) mats[i] = RecolorCached(h.original[i], theme);
                h.renderer.sharedMaterials = mats;
            }
        }

        // cache theo (material gốc, màu nền) để các renderer dùng chung bản đã tô
        readonly System.Collections.Generic.Dictionary<string, Material> _recolored = new System.Collections.Generic.Dictionary<string, Material>();

        Material RecolorCached(Material src, GameConfig.Theme theme)
        {
            if (src == null) return null;
            string key = src.GetInstanceID() + "|" + theme.groundColor + "|" + theme.crusherBrightness;
            if (_recolored.TryGetValue(key, out Material m) && m != null) return m;
            m = Recolor(src, theme);
            _recolored[key] = m;
            return m;
        }

        Material Recolor(Material src, GameConfig.Theme theme)
        {
            var m = new Material(src) { name = src.name + " (theme)" };
            float hn, sn, vn, hg, sg, vg;
            Color.RGBToHSV(_normalGround, out hn, out sn, out vn);
            Color.RGBToHSV(theme.groundColor, out hg, out sg, out vg);
            float dh = sg < 0.02f || sn < 0.02f ? 0f : hg - hn;
            float ds = sn > 1e-3f ? sg / sn : 1f;
            float dv = (vn > 1e-3f ? Mathf.Sqrt(vg / vn) : 1f) * theme.crusherBrightness;
            foreach (string prop in new[] { "_BaseColor", "_Color", "_HColor", "_SColor" })
            {
                if (!m.HasProperty(prop)) continue;
                Color c = m.GetColor(prop);
                float h, s, v;
                Color.RGBToHSV(c, out h, out s, out v);
                h = Mathf.Repeat(h + dh, 1f);
                s = Mathf.Clamp01(s * ds);
                v = Mathf.Clamp01(v * dv);
                Color nc = Color.HSVToRGB(h, s, v); nc.a = c.a;
                m.SetColor(prop, nc);
            }
            return m;
        }

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
            ApplyCrusher(theme);
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
                _gradient.color = new Color(1f, 1f, 1f, theme != null ? theme.maskAlpha : 1f);
            }
            if (_cam != null) _cam.backgroundColor = theme == null ? _origCamBg : theme.cameraBackground.a > 0f ? theme.cameraBackground : theme.groundColor;
        }
    }
}
