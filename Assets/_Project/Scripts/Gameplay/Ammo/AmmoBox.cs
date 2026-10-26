using System;
using TMPro;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>Hộp đạn (jelly) TRONG KHAY</summary>
    public class AmmoBox : MonoBehaviour
    {
        public AmmoBoxData Data { get; private set; }
        public Vector2Int Cell { get; set; }
        public bool IsClickable { get; private set; }
        public bool IsFrozen => Data != null && Data.IceHits > 0;
        public bool IsHidden => Data != null && Data.Hidden;
        public bool HasDoor => Data != null && Data.Door > 0;
        public bool IsDoorClosed => Data != null && Data.Door > 0 && Data.DoorClosed;
        public bool IsLaunched { get; private set; }
        /// <summary>Bay lên máng &amp; đậu trong máy xay</summary>
        public AmmoBoxFlight Flight { get; private set; }

        GameConfig _config;
        ColorLibrary _colors;
        Transform _visual;

        /// <summary>Khung (x, y) của phần HÌNH THẬT đang hiện ra của hộp</summary>
        public bool TryGetVisibleBounds(out Bounds bounds)
        {
            bounds = new Bounds(transform.position, Vector3.zero);
            bool has = false;
            foreach (Renderer r in GetComponentsInChildren<Renderer>())
            {
                if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
                if (r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer) continue;
                if (!has) { bounds = r.bounds; has = true; } else bounds.Encapsulate(r.bounds);
            }
            return has;
        }
        Renderer _jelly;
        GameObject _qmark;
        IceBlockerView _ice;
        Vector3 _capLocal;
        bool _hasCap;
        Camera _cam;
        float _cell;
        float _scaleNow;
        float _scaleTarget;
        float _tint = 1f;
        float _tintTarget = 1f;
        float _wobble;
        JellyDeformer _jellyFx;
        float _press, _pressTarget;
        float _sink;
        float _sinkTarget;

        public static float SinkDepth => AmmoBoxTuning.SinkDepth;
        MaterialPropertyBlock _mpb;
        Color _baseColor;

        internal static Quaternion GridTilt => Quaternion.Euler(AmmoBoxTuning.Active.tiltX, 0f, 0f);
        int _tuningVersion = -1;

        /// <summary>Xử lý khởi tạo hộp đạn theo dữ liệu, ô khay và màu</summary>
        public void Init(AmmoBoxData data, Vector2Int cell, float cellSize, GameConfig config, ColorLibrary colors)
        {
            Data = data;
            Cell = cell;
            _cell = cellSize;
            _config = config;
            _colors = colors;
            _mpb = new MaterialPropertyBlock();

            GameObject v = Instantiate(config.ammoPrefab, transform);
            v.name = "Visual";
            ModelUtil.StripPhysics(v);
            foreach (TextMeshPro t in v.GetComponentsInChildren<TextMeshPro>(true)) t.gameObject.SetActive(false);
            ModelUtil.RemoveStrayText(v.transform);
            _visual = v.transform;
            _visual.localRotation = GridTilt;

            Transform jellyT = _visual.Find("Model/Cube/Jelly Cube/RiggedCube (8)");
            _jelly = jellyT != null ? jellyT.GetComponent<Renderer>() : v.GetComponentInChildren<Renderer>();
            Transform q = _visual.Find("Qmark Model");
            _qmark = q != null ? q.gameObject : null;

            int ammoLayer = LayerMask.NameToLayer("Ammunation");
            if (ammoLayer >= 0) foreach (Transform t in GetComponentsInChildren<Transform>(true)) t.gameObject.layer = ammoLayer;

            ComputeCapPoint();
            AmmoFlightTube.Prewarm(_jelly);
            Flight = gameObject.AddComponent<AmmoBoxFlight>();
            Flight.Init(this, config, _jelly, _mpb, _visual, _cell);
            if (data.IceHits > 0) CreateIce();
            if (data.Door > 0)
            {
                _door = new AmmoDoorView(transform, config, data.DoorClosed);
            }
            ApplyAppearance();

            _scaleNow = _scaleTarget = config.ammoInactiveScale;
            _sink = _sinkTarget = 1f;
            ApplyScale();
        }

        AmmoDoorView _door;
        float _doorSink;
        bool _pathOpen;
        float DoorSinkTarget => IsDoorClosed && _pathOpen ? 1f : 0f;

        /// <summary>Mỗi lần bắn 1 hộp bất kỳ: cửa đảo trạng thái</summary>
        public void ToggleDoor()
        {
            if (Data.Door <= 0) return;
            Data.DoorClosed = !Data.DoorClosed;
            _door?.SetClosed(Data.DoorClosed);
            _wobble = 1f;
        }

        /// <summary>Đặt cửa lên miệng hố: tâm world, kích thước ô hố</summary>
        public void SetCoverPlacement(Vector3 worldCenter, Vector2 size, Quaternion plate) => _door?.Place(worldCenter, size, plate);

        /// <summary>Tâm mặt nắp của cột jelly</summary>
        void ComputeCapPoint()
        {
            _cam = Camera.main;
            _hasCap = CapOf(_jelly, out _capLocal);
            _qRenderer = _qmark != null ? _qmark.GetComponentInChildren<Renderer>(true) : null;
            _hasQCap = CapOf(_qRenderer, out _qCapLocal);
        }

        bool CapOf(Renderer r, out Vector3 local)
        {
            local = default;
            if (r == null) return false;
            var mf = r.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) return false;
            Mesh m = mf.sharedMesh; Bounds b = m.bounds;
            int ax = b.extents.x >= b.extents.y && b.extents.x >= b.extents.z ? 0 : (b.extents.y >= b.extents.z ? 1 : 2);
            Vector3 axis = ax == 0 ? Vector3.right : (ax == 1 ? Vector3.up : Vector3.forward);
            Vector3 camFwd = _cam != null ? _cam.transform.forward : Vector3.forward;
            float sign = Vector3.Dot(r.transform.TransformDirection(axis), camFwd) < 0f ? 1f : -1f;
            float ext = b.extents[ax], thr = b.center[ax] + sign * ext * 0.8f;
            Vector3 sum = Vector3.zero; int n = 0;
            foreach (Vector3 v in m.vertices)
                if (sign > 0f ? v[ax] >= thr : v[ax] <= thr) { sum += v; n++; }
            if (n == 0) return false;
            local = sum / n;
            return true;
        }

        Renderer _qRenderer;
        Vector3 _qCapLocal;
        bool _hasQCap;

        /// <summary>Đưa tâm mặt nắp về ĐÚNG tâm hố trên màn hình</summary>
        void CenterCapOnHole()
        {
            if (!_hasCap || _jelly == null) return;
            if (_cam == null) _cam = Camera.main;
            if (_cam == null) return;
            Vector3 view = _cam.transform.forward;
            Vector3 cap = _jelly.transform.TransformPoint(_capLocal);
            Vector3 d = transform.position - cap;
            d -= view * Vector3.Dot(d, view);
            _visual.position += d;
        }

        /// <summary>Băng (/ice-N): dùng nguyên "Ice Model" có sẵn trong prefab gốc</summary>
        void CreateIce()
        {
            _ice = IceBlockerView.Attach(_visual, Data.IceHits);
            if (_ice != null) _ice.Show(Data.IceHits);
        }

        void ApplyAppearance()
        {
            bool hidden = Data.Hidden;
            if (_jelly != null)
            {
                _jelly.gameObject.SetActive(!hidden);
                if (!hidden)
                {
                    _jelly.sharedMaterial = _colors.AmmoMat(Data.ColorCode);
                    _baseColor = _jelly.sharedMaterial != null && _jelly.sharedMaterial.HasProperty("_BaseColor")
                        ? _jelly.sharedMaterial.GetColor("_BaseColor") : _colors.ColorOf(Data.ColorCode);
                }
            }
            if (_qmark != null) _qmark.SetActive(hidden);
        }

        /// <summary>Xử lý trạng thái bấm được, nhô lên và tư thế của hộp</summary>
        public void SetClickable(bool value, bool raised, bool pathOpen, bool instant = false)
        {
            IsClickable = value;
            _pathOpen = pathOpen;
            _scaleTarget = raised ? _config.ammoActiveScale : _config.ammoInactiveScale;
            _tintTarget = raised ? 1f : 0.92f;
            _sinkTarget = raised ? 0f : 1f;
            if (instant) { _scaleNow = _scaleTarget; _tint = _tintTarget; _sink = _sinkTarget; _doorSink = DoorSinkTarget; ApplyScale(); ApplyTint(); }
        }

        /// <summary>Lộ màu hộp "?" khi nó thông đường ra</summary>
        public void Reveal()
        {
            if (!Data.Hidden) return;
            Data.Hidden = false;
            ApplyAppearance();
            _wobble = 1f;
            Vfx.Spawn(_config.ammoLaunchVfx, transform.position, Color.white, _cell * 0.6f);
            AudioManager.Play(Sfx.QmarkReveal);
        }

        /// <summary>Hộp kề (trên/dưới/trái/phải) vừa được bắn → băng nứt thêm 1 nấc (hình</summary>
        public void Pop() => _wobble = 1f;

        /// <summary>Xử lý nứt lớp băng bọc hộp</summary>
        public void CrackIce()
        {
            if (Data.IceHits <= 0) return;
            Data.IceHits--;
            _wobble = 1f;
            if (_ice != null) _ice.Crack(Data.IceHits);
            AudioManager.Play(Data.IceHits <= 0 ? Sfx.IceBreak : Sfx.IceCrack);
        }

        /// <summary>Nhấn giữ hộp (bấm được) → lắc lư như thạch</summary>
        public void SetHeld(bool held)
        {
            if (held && (IsLaunched || _hover || Data == null)) return;
            if (_jellyFx == null)
            {
                if (!held || _jelly == null) return;
                _jellyFx = _jelly.gameObject.GetComponent<JellyDeformer>();
                if (_jellyFx == null)
                {
                    _jellyFx = _jelly.gameObject.AddComponent<JellyDeformer>();
                    Mesh m = _jelly.GetComponent<MeshFilter>() != null ? _jelly.GetComponent<MeshFilter>().sharedMesh : null;
                    bool capAtMax = !_hasCap || m == null || _capLocal.y >= m.bounds.center.y;
                    if (!_jellyFx.Init(_config, capAtMax)) { Destroy(_jellyFx); _jellyFx = null; return; }
                }
            }
            _jellyFx.SetHeld(held);
        }

        /// <summary>Phản hồi tức thì lúc chạm xuống hộp</summary>
        public void SetPressed(bool pressed)
        {
            if (pressed && (IsLaunched || _hover || Data == null)) return;
            _pressTarget = pressed ? 1f : 0f;
        }

        /// <summary>Xử lý hiệu ứng lắc khi bấm vào hộp chưa bấm được</summary>
        public void PlayDenied()
        {
            _wobble = 1f;
            if (_jellyFx != null) _jellyFx.Kick(0.6f);
            AudioManager.Play(Sfx.Error);
        }

        void Update()
        {
            if (Data == null || IsLaunched || _hover) return;

            bool scaleChanged = false;
            if (_scaleNow != _scaleTarget) { _scaleNow = Mathf.MoveTowards(_scaleNow, _scaleTarget, Time.deltaTime * 0.8f); scaleChanged = true; }
            if (_sink != _sinkTarget) { _sink = Mathf.MoveTowards(_sink, _sinkTarget, Time.deltaTime * 4f); scaleChanged = true; }
            float doorTarget = DoorSinkTarget;
            if (_doorSink != doorTarget) { _doorSink = Mathf.MoveTowards(_doorSink, doorTarget, Time.deltaTime * 4f); scaleChanged = true; }
            if (_wobble > 0f) { _wobble = Mathf.Max(0f, _wobble - Time.deltaTime * 3f); scaleChanged = true; }
            if (_press != _pressTarget)
            {
                _press = Mathf.MoveTowards(_press, _pressTarget, Time.unscaledDeltaTime / Mathf.Max(0.01f, _config.pressInTime));
                scaleChanged = true;
            }

            bool tintChanged = false;
            if (_tint != _tintTarget) { _tint = Mathf.MoveTowards(_tint, _tintTarget, Time.deltaTime * 3f); tintChanged = true; }
            _door?.Tick(Time.deltaTime);

            if (_tuningVersion != AmmoBoxTuning.Version) { _tuningVersion = AmmoBoxTuning.Version; scaleChanged = true; }
            if (scaleChanged) ApplyScale();
            if (tintChanged) ApplyTint();
        }

        void ApplyScale()
        {
            if (_visual == null) return;
            float w = 1f + Mathf.Sin(_wobble * Mathf.PI * 3f) * 0.12f * _wobble;
            if (_press > 0f && _config != null) w *= 1f - _config.pressInScale * Mathf.SmoothStep(0f, 1f, _press);

            float kt = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_sink));
            GameConfig.AmmoPose pose = AmmoBoxTuning.Lerp(1f - kt);
            float s = _cell * _scaleNow * pose.scale * _config.ammoBoxVsHole;
            _visual.localRotation = Quaternion.Euler(pose.tiltX, 0f, 0f);
            if (_jelly != null) _jelly.transform.localScale = new Vector3(1f, pose.length, 1f);
            if (_qRenderer != null) _qRenderer.transform.localScale = new Vector3(1f, pose.length, 1f);
            ModelUtil.FitVisualUniform(_visual, s * w);
            if (_sink > 0f)
            {
                float k = Mathf.SmoothStep(0f, 1f, _sink);
                float dz = (SinkDepth + _doorSink * _config.doorExtraSink) * _cell * k;
                _visual.localPosition += new Vector3(0f, 0f, dz);
            }
            CenterCapOnHole();
            if (_cam != null)
            {
                Vector3 o = pose.offset * _cell;
                _visual.position += _cam.transform.right * o.x + _cam.transform.up * o.y + _cam.transform.forward * o.z;
            }
            AlignQmarkToJelly();
            if (_ice != null && _jelly != null) _ice.FitCover(_jelly.bounds, _config, _cell);
            _door?.Apply();
        }

        /// <summary>Hộp mù: dời mảnh "?" để nắp của nó trùng đúng nắp jelly</summary>
        void AlignQmarkToJelly()
        {
            if (_qmark == null || !_qmark.activeSelf || !_hasQCap || !_hasCap || _qRenderer == null || _jelly == null) return;
            Vector3 d = _jelly.transform.TransformPoint(_capLocal) - _qRenderer.transform.TransformPoint(_qCapLocal);
            _qmark.transform.position += d;
        }

        void ApplyTint()
        {
            if (_jelly == null || !_jelly.gameObject.activeSelf || !_jelly.sharedMaterial || !_jelly.sharedMaterial.HasProperty("_BaseColor")) return;
            _jelly.GetPropertyBlock(_mpb);
            Color c = _baseColor * _tint;
            c.a = _baseColor.a;
            _mpb.SetColor("_BaseColor", c);
            Material sm = _jelly.sharedMaterial;
            if (sm.HasProperty("_HColor")) { Color hc = sm.GetColor("_HColor") * Mathf.Lerp(0.9f, 1f, _tint); hc.a = 1f; _mpb.SetColor("_HColor", hc); }
            if (sm.HasProperty("_SColor")) { Color sc = sm.GetColor("_SColor") * Mathf.Lerp(0.85f, 1f, _tint); sc.a = 1f; _mpb.SetColor("_SColor", sc); }
            if (sm.HasProperty("_SpecularColor")) _mpb.SetColor("_SpecularColor", sm.GetColor("_SpecularColor") * Mathf.Lerp(0.6f, 1f, _tint));
            _jelly.SetPropertyBlock(_mpb);
        }

        /// <summary>Xử lý bắt đầu bay của hộp lên máng</summary>
        public void BeginLaunch()
        {
            IsLaunched = true;
            if (_jellyFx != null) _jellyFx.ResetNow();
            _press = _pressTarget = 0f;
            _hover = false;
            Flight.CancelUnlessHolding();
            _door?.Remove();
            _tint = 1f;
            _sink = _sinkTarget = 0f;
            ApplyTint();
        }

        bool _hover;

        /// <summary>Bấm hộp làm vượt sức chứa</summary>
        public void HoldFlightAt(Vector3 target, Quaternion dockRotation, float dockWidth, float duration, float holdU)
        {
            if (_hover || IsLaunched || Flight.IsHoldingFlight) return;
            _hover = true;
            _press = _pressTarget = 0f;
            if (_jellyFx != null) _jellyFx.ResetNow();
            _door?.Remove();
            _tint = 1f; _sink = _sinkTarget = 0f;
            ApplyTint();
            Flight.HoldAt(target, dockRotation, dockWidth, duration, holdU);
        }

        /// <summary>Xử lý kết thúc và huỷ hộp</summary>
        public void Finish()
        {
            Vfx.Spawn(_config.ammoLaunchVfx, transform.position, _colors.ColorOf(Data.ColorCode), 0.6f);
            Destroy(gameObject);
        }
    }
}
