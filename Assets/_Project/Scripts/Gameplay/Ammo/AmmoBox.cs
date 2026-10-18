using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>
    /// Hộp đạn (jelly) TRONG KHAY. Dùng prefab Ammunation gốc.
    /// Trạng thái: bấm được (to, sáng) / chưa thông đường (nhỏ, lún, tối), đóng băng, hộp "?" bí ẩn, cửa kính; phản hồi khi chạm/giữ.
    /// Phần bay lên máng + đậu trong máy xay ở <see cref="Flight"/> (<see cref="AmmoBoxFlight"/>); hình cửa kính ở <see cref="AmmoDoorView"/>.
    /// </summary>
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
        /// <summary>Bay lên máng & đậu trong máy xay.</summary>
        public AmmoBoxFlight Flight { get; private set; }

        GameConfig _config;
        ColorLibrary _colors;
        Transform _visual;

        /// <summary>
        /// Khung (x, y) của phần HÌNH THẬT đang hiện ra của hộp – gộp mọi renderer đang bật (thân jelly, nắp, hộp mù, cửa…).
        /// Dùng để dò hộp khi người chơi bấm: bấm vào chỗ NHÌN THẤY hộp. Không dùng gốc xoay của model vì gốc đó nằm lệch
        /// hẳn so với phần nhìn thấy (tilt −68°, cột jelly dài) và đổi theo mọi thông số tư thế trong GameConfig.
        /// </summary>
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
        Vector3 _capLocal;          // tâm mặt nắp (đầu cột hướng về camera) trong toạ độ mesh của jelly
        bool _hasCap;
        Camera _cam;
        float _cell;
        float _scaleNow;
        float _scaleTarget;
        float _tint = 1f;
        float _tintTarget = 1f;
        float _wobble;
        JellyDeformer _jellyFx;   // lắc thạch khi nhấn giữ (tạo khi cần)
        float _press, _pressTarget;   // 0..1: hộp lún nhỏ lại ngay khi bị chạm (phản hồi tức thì)
        float _sink;        // 0 = trồi lên (bấm được), 1 = lún trong lỗ khay
        float _sinkTarget;

        public static float SinkDepth => AmmoBoxTuning.SinkDepth; // chỉnh trong GameConfig.ammoSinkDepth
        MaterialPropertyBlock _mpb;
        Color _baseColor;

        // Tư thế hộp (nghiêng / chiều dài cột / dịch / cỡ) dùng chung cho TẤT CẢ hộp – chỉnh trong GameConfig (ammoActivePose/ammoInactivePose).
        internal static Quaternion GridTilt => Quaternion.Euler(AmmoBoxTuning.Active.tiltX, 0f, 0f);
        int _tuningVersion = -1;

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

            // Game gốc chiếu jelly bằng đèn riêng (culling mask = layer Ammunation)
            int ammoLayer = LayerMask.NameToLayer("Ammunation");
            if (ammoLayer >= 0) foreach (Transform t in GetComponentsInChildren<Transform>(true)) t.gameObject.layer = ammoLayer;

            ComputeCapPoint();
            AmmoFlightTube.Prewarm(_jelly);   // mesh gắn xương dựng sẵn → lần bay đầu không khựng
            Flight = gameObject.AddComponent<AmmoBoxFlight>();
            Flight.Init(this, config, _jelly, _mpb, _visual, _cell);
            if (data.IceHits > 0) CreateIce();
            if (data.Door > 0)
            {
                _door = new AmmoDoorView(transform, config, data.DoorClosed);
                _doorSink = data.DoorClosed ? 1f : 0f;
            }
            ApplyAppearance();

            _scaleNow = _scaleTarget = config.ammoInactiveScale;
            _sink = _sinkTarget = 1f;
            ApplyScale();
        }

        AmmoDoorView _door;              // cửa kính (chỉ hộp có /door)
        float _doorSink;                 // 0..1, nội suy theo trạng thái cửa

        /// <summary>Mỗi lần bắn 1 hộp bất kỳ: cửa đảo trạng thái (gốc BlockerCover.OnAmmoUsed).</summary>
        public void ToggleDoor()
        {
            if (Data.Door <= 0) return;
            Data.DoorClosed = !Data.DoorClosed;
            _door?.SetClosed(Data.DoorClosed);
            _wobble = 1f;
        }

        /// <summary>Đặt cửa lên miệng hố: tâm world, kích thước ô hố (bán kính bo góc không dùng – mesh gốc đã bo).</summary>
        public void SetCoverPlacement(Vector3 worldCenter, Vector2 size, float radius) => _door?.Place(worldCenter, size);

        /// <summary>Tâm mặt nắp của cột jelly: trung bình các đỉnh ở đầu cột (trục dài nhất của mesh) quay về phía camera.</summary>
        void ComputeCapPoint()
        {
            _cam = Camera.main;
            _hasCap = CapOf(_jelly, out _capLocal);
            // hộp mù: mảnh "?" gốc (Qmark Model) là cột cùng cỡ jelly nhưng pivot khác → căn nắp của nó trùng nắp jelly
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
            // đầu cột nào đang hướng về camera (khi hộp ở tư thế nghiêng hiện tại)?
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

        /// <summary>Đưa tâm mặt nắp về ĐÚNG tâm hố trên màn hình (camera ortho: chỉ cần dời trong mặt phẳng vuông góc hướng nhìn).</summary>
        void CenterCapOnHole()
        {
            if (!_hasCap || _jelly == null) return;
            if (_cam == null) _cam = Camera.main;
            if (_cam == null) return;
            Vector3 view = _cam.transform.forward;
            Vector3 cap = _jelly.transform.TransformPoint(_capLocal);
            Vector3 d = transform.position - cap;           // transform.position = tâm ô
            d -= view * Vector3.Dot(d, view);
            _visual.position += d;
        }

        /// <summary>Băng (/ice-N): dùng nguyên "Ice Model" có sẵn trong prefab gốc – xem <see cref="IceBlockerView"/>.</summary>
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

        // ------------------------------------------------------------------ Trạng thái trong khay
        public void SetClickable(bool value, bool instant = false)
        {
            IsClickable = value;
            _scaleTarget = value ? _config.ammoActiveScale : _config.ammoInactiveScale;
            _tintTarget = value ? 1f : 0.92f; // Tươi sáng, phân biệt active qua độ nhô & scale
            _sinkTarget = value ? 0f : 1f;
            if (instant) { _scaleNow = _scaleTarget; _tint = _tintTarget; _sink = _sinkTarget; ApplyScale(); ApplyTint(); }
        }

        /// <summary>Lộ màu hộp "?" khi nó thông đường ra (gốc: Ammunation.InitializeQmark / IsHidden).</summary>
        public void Reveal()
        {
            if (!Data.Hidden) return;
            Data.Hidden = false;
            ApplyAppearance();
            _wobble = 1f;
            Vfx.Spawn(_config.ammoLaunchVfx, transform.position, Color.white, _cell * 0.6f);
            AudioManager.Play(Sfx.QmarkReveal);
        }

        /// <summary>Hộp kề (trên/dưới/trái/phải) vừa được bắn → băng nứt thêm 1 nấc (hình: <see cref="IceBlockerView"/>).</summary>
        public void CrackIce()
        {
            if (Data.IceHits <= 0) return;
            Data.IceHits--;
            _wobble = 1f;
            if (_ice != null) _ice.Crack(Data.IceHits);
            AudioManager.Play(Data.IceHits <= 0 ? Sfx.IceBreak : Sfx.IceCrack);
        }

        /// <summary>
        /// Nhấn giữ hộp (bấm được) → lắc lư như thạch; thả ra thì lắc tắt dần. Chỉ là hiệu ứng, việc bắn do InputManager quyết định.
        /// Thông số trong GameConfig → "Hộp đạn – lắc khi nhấn giữ".
        /// </summary>
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
                    // nắp = đầu cột hướng về camera (cùng cách ComputeCapPoint chọn): nằm ở y lớn hay y nhỏ của mesh?
                    Mesh m = _jelly.GetComponent<MeshFilter>() != null ? _jelly.GetComponent<MeshFilter>().sharedMesh : null;
                    bool capAtMax = !_hasCap || m == null || _capLocal.y >= m.bounds.center.y;
                    if (!_jellyFx.Init(_config, capAtMax)) { Destroy(_jellyFx); _jellyFx = null; return; }
                }
            }
            _jellyFx.SetHeld(held);
        }

        /// <summary>Phản hồi tức thì lúc chạm xuống hộp: hộp lún nhỏ lại; thả / huỷ thì bật lại. Không đụng tới logic bắn.</summary>
        public void SetPressed(bool pressed)
        {
            if (pressed && (IsLaunched || _hover || Data == null)) return;
            _pressTarget = pressed ? 1f : 0f;
        }

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
            float doorTarget = IsDoorClosed ? 1f : 0f;
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
            GameConfig.AmmoPose pose = AmmoBoxTuning.Lerp(1f - kt);   // active ↔ chưa active theo độ lún
            float s = _cell * _scaleNow * pose.scale;
            _visual.localRotation = Quaternion.Euler(pose.tiltX, 0f, 0f);
            if (_jelly != null) _jelly.transform.localScale = new Vector3(1f, pose.length, 1f);
            if (_qRenderer != null) _qRenderer.transform.localScale = new Vector3(1f, pose.length, 1f); // hộp mù theo cùng chiều dài
            ModelUtil.FitVisualUniform(_visual, s * w);
            if (_sink > 0f)
            {
                float k = Mathf.SmoothStep(0f, 1f, _sink);
                // cửa đóng: hộp tụt sâu hẳn sau mặt khay như buồng thang máy, cửa 2 cánh khép trên miệng hố
                float dz = (SinkDepth + _doorSink * _config.doorExtraSink) * _cell * k;
                // xuống tới mặt khay thì bám theo độ lệch y của miệng hố, sâu hơn nữa thì tụt THẲNG theo trục giếng (không trôi sang giếng dưới)
                float dyz = Mathf.Min(dz, 0.8f * _cell * k); // bù 80% → hộp hơi thấp hơn tâm hố, gờ dưới của hố che mép dưới như ảnh gốc
                _visual.localPosition += new Vector3(0f, 0f, dz);
            }
            CenterCapOnHole();
            // Dịch tay (AmmoLayoutTuner): x phải+, y lên+ trong mặt phẳng màn hình; z ra xa camera+ (đổi thứ tự che giữa các hộp)
            if (_cam != null)
            {
                Vector3 o = pose.offset * _cell;
                _visual.position += _cam.transform.right * o.x + _cam.transform.up * o.y + _cam.transform.forward * o.z;
            }
            AlignQmarkToJelly();
            _door?.Apply();
        }

        /// <summary>Hộp mù: dời mảnh "?" để nắp của nó trùng đúng nắp jelly (jelly đang ẩn nhưng transform vẫn đúng tư thế).</summary>
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

        // ------------------------------------------------------------------ Bay lên máng / đậu dock
        public void BeginLaunch()
        {
            IsLaunched = true;
            if (_jellyFx != null) _jellyFx.ResetNow();
            _press = _pressTarget = 0f;
            _hover = false;
            Flight.CancelUnlessHolding();   // đang dừng giữa chuyến bay (hết sức chứa) thì để nó bay tiếp
            _door?.Remove();
            _tint = 1f;
            _sink = _sinkTarget = 0f;
            ApplyTint();
        }

        bool _hover;                 // đang dừng giữa chuyến bay (hết sức chứa): Update không đụng tới hình

        /// <summary>
        /// Bấm hộp làm vượt sức chứa: chuẩn bị hộp trong khay (tắt nảy/lún, bỏ cửa) rồi cho <see cref="AmmoBoxFlight"/>
        /// chạy hoạt ảnh bay và dừng giữa chừng (xem <see cref="AmmoBoxFlight.HoldAt"/>).
        /// </summary>
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

        public void Finish()
        {
            Vfx.Spawn(_config.ammoLaunchVfx, transform.position, _colors.ColorOf(Data.ColorCode), 0.6f);
            Destroy(gameObject);
        }
    }
}
