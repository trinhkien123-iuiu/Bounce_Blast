using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>
    /// Hộp đạn (jelly) trong khay. Dùng prefab Ammunation gốc.
    /// Trạng thái: bấm được (to, sáng) / chưa thông đường (nhỏ, tối), đóng băng, hộp "?" bí ẩn,
    /// bay lên máng, đậu ở dock và co dần khi nhả bi.
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
        Transform _coverT;
        Vector3 _capLocal;          // tâm mặt nắp (đầu cột hướng về camera) trong toạ độ mesh của jelly
        bool _hasCap;
        Camera _cam;
        float _coverBlend, _coverBlendTarget;
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
        Coroutine _move;

        // Tư thế hộp (nghiêng / chiều dài cột / dịch / cỡ) dùng chung cho TẤT CẢ hộp – chỉnh trong GameConfig (ammoActivePose/ammoInactivePose).
        static Quaternion GridTilt => Quaternion.Euler(AmmoBoxTuning.Active.tiltX, 0f, 0f);
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
            EnsureDockLabel(); _dockLabel.gameObject.SetActive(false);
            if (data.IceHits > 0) CreateIce();
            if (data.Door > 0) CreateCover();
            ApplyAppearance();

            _scaleNow = _scaleTarget = config.ammoInactiveScale;
            _sink = _sinkTarget = 1f;
            ApplyScale();
        }

        /// <summary>
        /// Cửa kính 2 cánh (BlockerCover gốc). Dùng NGUYÊN node "Cover" của prefab AmmunationBlockerCover: SkinnedMeshRenderer có
        /// blend shape 0 (0 = đóng khít miệng hố, 90 = 2 cánh trượt vào 2 bên và biến mất), vật liệu Cover (vạch sáng chéo + khe giữa).
        /// Code chỉ đặt vị trí/cỡ theo miệng hố và đẩy trọng số blend shape (<see cref="ApplyCover"/>).
        /// </summary>
        void CreateCover()
        {
            var root = new GameObject("DoorCover");
            root.transform.SetParent(transform, false);
            _coverT = root.transform;
            var pf = _config != null ? _config.ammoCoverPrefab : null; // GameConfig.ammoCoverPrefab
            Transform src = pf != null ? pf.transform.Find("Cover") : null;
            if (src != null)
            {
                GameObject inst = Instantiate(src.gameObject, _coverT, false);
                inst.name = "Cover";
                inst.SetActive(true);
                _coverSmr = inst.GetComponent<SkinnedMeshRenderer>();
                if (_coverSmr != null)
                {
                    _coverSmr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    _coverSmr.updateWhenOffscreen = true;
                }
                int ammoLayer = LayerMask.NameToLayer("Ammunation");
                if (ammoLayer >= 0) foreach (Transform t in inst.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = ammoLayer;
            }
            _coverBlend = _coverBlendTarget = Data.DoorClosed ? 0f : 90f;
            _doorSink = Data.DoorClosed ? 1f : 0f;
        }

        SkinnedMeshRenderer _coverSmr;
        float _doorSink;                 // 0..1, nội suy theo trạng thái cửa

        /// <summary>Mỗi lần bắn 1 hộp bất kỳ: cửa đảo trạng thái (gốc BlockerCover.OnAmmoUsed).</summary>
        public void ToggleDoor()
        {
            if (Data.Door <= 0) return;
            Data.DoorClosed = !Data.DoorClosed;
            _coverBlendTarget = Data.DoorClosed ? 0f : 90f;
            _wobble = 1f;
        }

        const float CoverMeshSize = 1.6314f; // bề rộng mesh Cover gốc ở scale 1 (đóng): bounds prefab 2.06 / scale node 1.2627

        /// <summary>Đặt cửa lên miệng hố: tâm world, kích thước ô hố (bán kính bo góc không dùng – mesh gốc đã bo).</summary>
        public void SetCoverPlacement(Vector3 worldCenter, Vector2 size, float radius)
        {
            if (_coverT == null || _coverSmr == null) return;
            _coverT.SetPositionAndRotation(worldCenter, Quaternion.identity);
            _coverT.localScale = Vector3.one;
            Transform c = _coverSmr.transform;
            c.localRotation = Quaternion.Euler(-90f, 0f, 0f);          // mesh gốc nằm trên mặt XZ → dựng về mặt XY hướng camera
            c.localScale = Vector3.one * (size.x / CoverMeshSize * 1.03f); // +3%: che hẳn viền màu hộp phía sau
            c.localPosition = Vector3.zero;
            ApplyCover();
            Bounds b = _coverSmr.bounds;                                // căn tâm hình thật vào tâm hố
            c.position += new Vector3(worldCenter.x - b.center.x, worldCenter.y - b.center.y, 0f);
        }

        void ApplyCover()
        {
            if (_coverSmr == null) return;
            _coverSmr.SetBlendShapeWeight(0, Mathf.Clamp(_coverBlend, 0f, 100f));
            _coverSmr.enabled = _coverBlend < 89f;
        }

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
            if (_coverBlend != _coverBlendTarget) { _coverBlend = Mathf.MoveTowards(_coverBlend, _coverBlendTarget, Time.deltaTime * 360f); }

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
            ApplyCover();
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
            if (!_holdingFlight) { StopAllCoroutines(); _move = null; }   // đang dừng giữa chuyến bay (hết sức chứa) thì để nó bay tiếp
            if (_coverT != null) Destroy(_coverT.gameObject);
            _tint = 1f;
            _sink = _sinkTarget = 0f;
            ApplyTint();
        }

        bool _hover;                 // đang dừng giữa chuyến bay (hết sức chứa): Update không đụng tới hình
        bool _holdingFlight;         // chuyến bay đang bị giữ ở tiến độ _holdU
        bool _flightReleased;
        float _holdU;
        Action _doneAfterHold;

        /// <summary>Máng đã chọn cho hộp đang dừng giữa chuyến bay (Send dùng lại đúng máng này khi bay tiếp).</summary>
        public ConveyorLane HeldLane { get; set; }
        public bool IsHoldingFlight => _holdingFlight;

        /// <summary>
        /// Bấm hộp làm vượt sức chứa (video gốc lose_coche): hộp chạy ĐÚNG hoạt ảnh bay lên máng như lúc chơi bình thường
        /// (cùng ống thạch, cùng quỹ đạo, cùng chiều) rồi DỪNG giữa chừng ở tiến độ <paramref name="holdU"/> trong lúc hiện
        /// Out Of Capacity. Chọn Play On → <see cref="FlyTo"/> thả giữ, hộp bay tiếp từ chỗ đó; Give Up → hộp đứng yên (đã dừng).
        /// </summary>
        public void HoldFlightAt(Vector3 target, Quaternion dockRotation, float dockWidth, float duration, float holdU)
        {
            if (_hover || IsLaunched || _holdingFlight) return;
            _hover = true;
            _press = _pressTarget = 0f;
            if (_jellyFx != null) _jellyFx.ResetNow();
            if (_coverT != null) Destroy(_coverT.gameObject);
            _tint = 1f; _sink = _sinkTarget = 0f;
            ApplyTint();
            DockRotation = dockRotation;
            DockWidth = dockWidth;
            _holdingFlight = true;
            _flightReleased = false;
            _holdU = Mathf.Clamp(holdU, 0.02f, 0.95f);
            _move = StartCoroutine(FlyRoutine(target, duration, null));
        }

        /// <param name="done">gọi khi ống đã yên vị hẳn (sau khi lò xo đuôi lắng)</param>
        /// <param name="headArrived">gọi sớm hơn: ngay khi ĐẦU ống chạm máy xay (tiến độ <see cref="HeadArriveU"/>) – để máy xay chạy luôn, không đợi đuôi lắng</param>
        public void FlyTo(Vector3 target, float duration, Action done, Action headArrived = null)
        {
            _headArrived = headArrived;
            if (_holdingFlight)    // đang dừng giữa chuyến bay → thả giữ, bay tiếp, xong thì gọi done
            {
                _doneAfterHold = done;
                _flightReleased = true;
                return;
            }
            if (_move != null) StopCoroutine(_move);
            _move = StartCoroutine(FlyRoutine(target, duration, done));
        }

        /// <summary>Tiến độ chuyến bay mà tại đó đầu ống coi như đã vào máy xay (easing phóng nhanh: ≈ 0.8 → đầu đã ~98% quãng đường).</summary>
        const float HeadArriveU = 0.8f;
        Action _headArrived;
        Vector3 _flightTarget;   // chỗ đậu của chuyến bay hiện tại – nhãn số bi neo vào đây khi ống còn đang bay
        bool _hasPendingMove;
        Vector3 _pendingMoveTarget;
        float _pendingMoveDuration;
        bool _fractionSet;   // đã có ai đặt độ cao thanh đậu (máy xay đã bắt đầu xay khi ống còn đang lắng) → cuối chuyến bay đừng đặt lại 1

        /// <summary>
        /// Video gốc: hộp bay lên máng như một ỐNG THẠCH DẺO – đầu đi trước theo quỹ đạo (lên khỏi hố → chéo tới trên máng
        /// → cắm xuống chân máng), đuôi bám đúng đường đầu đã đi nên rời hố thì gập khuỷu, vào máng thì cong mượt.
        /// Cuối hành trình ống co về đúng thanh đậu ở máng (ShowDocked: dài 1.5, đáy dưới dock 0.3).
        /// </summary>
        IEnumerator FlyRoutine(Vector3 target, float duration, Action done)
        {
            // ống bay ở mặt phẳng z = -2 (trước bàn chơi); điểm đầu/cuối được dời theo trục up của camera nghiêng
            // để trên màn hình vẫn trùng đúng ô trong khay và tâm máy xay
            Vector3 start = transform.position;
            Vector3 startUp = GridTilt * Vector3.up;
            Vector3 dock = target;
            Vector3 dockUp = DockRotation * Vector3.up;
            float sBox = _cell * _config.ammoActiveScale;
            float dw = DockWidthResolved;
            float DockLen = dw * DockHeightRatio, DockBottom = dw * DockBottomRatio, DockR = dw * 0.5f;

            if (_jelly != null) _jelly.GetPropertyBlock(_mpb);
            if (_body != null) Destroy(_body.gameObject);
            AmmoFlightTube tube = _body = AmmoFlightTube.Create(_jelly, _mpb);
            _flying = true;
            _fractionSet = false;
            bool headFired = false;
            tube.SetPath(new[]
            {
                start,
                start + startUp * (_cell * 0.9f),
                dock + dockUp * (DockLen + 1.2f), // vào máng cắm theo đúng trục nghiêng của máy xay
                dock - dockUp * DockBottom,
            }, Mathf.Max(sBox, dw) * 1.3f);
            _visual.gameObject.SetActive(false);
            transform.rotation = DockRotation;

            // Lò xo (stretch & squash): nén lấy đà → ĐẦU phóng đi trước trong khi đuôi còn kẹt trong hố (kéo giãn từ đầu),
            // đuôi là khối lượng nối lò xo đuổi theo (giãn dần tới cuối); đầu dừng ở máng thì đuôi lao tới quá đà → ống nén
            // ngắn & phình, bật lại rồi yên. Bán kính giữ thể tích: r ~ sqrt(dài nghỉ / dài hiện tại).
            float total = tube.Length, h0 = sBox * 0.9f;
            const float Anticip = 0.035f, TailHold = 0.05f, K = 300f, Damp = 0.45f;
            // Pha LẮNG bắt đầu từ lúc đầu ống còn đang tiến vào (u = 0.6) và xong chỉ 0.1 s sau khi hết chuyến bay: ống được kéo MƯỢT về
            // đúng dáng + cỡ thanh đậu (khớp miệng máy xay) NGAY trong lúc máy xay bắt đầu chạy, thay vì còn nghiêng/phình ~0.4 s sau đó.
            // Lò xo tích phân bước cố định 1/120 s: không phụ thuộc fps máy.
            const float SettleStartU = 0.6f, SettleTail = 0.1f, Sub = 1f / 120f;
            float c = 2f * Mathf.Sqrt(K) * Damp;
            float tail = 0f, tailV = 0f, time = 0f, flyEnd = Anticip + duration, acc = 0f;
            float restTail = total - DockLen;
            _flightTarget = target;
            while (true)
            {
                float dt = Mathf.Min(Time.deltaTime, 0.05f);
                // dừng giữa chuyến bay (hết sức chứa): thời gian của hoạt ảnh đứng yên → ống giữ nguyên dáng đang có
                if (_holdingFlight && !_flightReleased && time >= Anticip + _holdU * duration) dt = 0f;
                float head, rest;
                if (time < Anticip)
                {
                    float k = Mathf.Sin(time / Anticip * Mathf.PI);
                    head = h0 * (1f - 0.15f * k);   // nén nhẹ lấy đà
                    rest = h0;
                }
                else
                {
                    float u = Mathf.Clamp01((time - Anticip) / duration);
                    float e = 1f - Mathf.Pow(1f - u, 2.4f); // phóng nhanh ngay khi bấm (không khựng), chậm dần khi tới máng
                    head = Mathf.Lerp(h0, total, e);
                    rest = Mathf.Lerp(h0, DockLen, u);
                    // đuôi: lò xo hướng về (đầu - dài nghỉ), kẹt trong hố một nhịp đầu
                    if (time - Anticip > TailHold * duration)
                    {
                        acc += dt;
                        while (acc >= Sub)
                        {
                            float a = K * ((head - rest) - tail) - c * tailV;
                            tailV += a * Sub;
                            tail += tailV * Sub;
                            acc -= Sub;
                        }
                    }
                    tail = Mathf.Clamp(tail, 0f, head - 0.25f * rest);
                }
                float settleStart = Anticip + SettleStartU * duration;
                float settle = time <= settleStart ? 0f : Mathf.Clamp01((time - settleStart) / (flyEnd + SettleTail - settleStart));
                float sm = settle * settle * (3f - 2f * settle);
                if (settle > 0f) { head = Mathf.Lerp(head, total, sm); tail = Mathf.Lerp(tail, restTail, sm); }
                if (!headFired && time >= Anticip + HeadArriveU * duration)
                {
                    headFired = true;
                    Action ha = _headArrived; _headArrived = null;
                    ha?.Invoke();
                }
                float len = head - tail;
                float p01 = Mathf.Clamp01((time - Anticip) / duration);
                float r0 = Mathf.Lerp(sBox * 0.5f, DockR, p01 * p01);
                float r = r0 * Mathf.Clamp(Mathf.Pow(rest / Mathf.Max(0.05f, len), 0.25f), 0.85f, 1.12f); // bề ngang gần như giữ nguyên, chỉ phình/thon nhẹ khi nén/giãn
                if (settle > 0f) r = Mathf.Lerp(r, DockR, sm);
                float toEnd = Mathf.Max(Mathf.SmoothStep(0.6f, 1f, p01) * 0.8f, sm);
                tube.Build(tail, head, r * 2f, GridTilt, DockRotation, toEnd);
                if (settle > 0f)
                {
                    float h = DockLen * (_fractionSet ? _dockFraction : 1f);
                    tube.BlendToStraight(target - dockUp * DockBottom, Mathf.Max(0.03f, h), DockRotation, sm);
                }
                Vector3 bodyCenter = tube.PointAt(tail + len * 0.5f);
                transform.position = settle > 0f ? Vector3.Lerp(bodyCenter, target, sm) : bodyCenter;
                if (time >= flyEnd + SettleTail) break;
                yield return null;
                time += dt;
            }
            // giữ thân đã gắn xương làm thanh đậu ở máy xay
            transform.position = target;
            transform.rotation = DockRotation;
            _flying = false;
            if (!_fractionSet) _dockFraction = 1f;
            _move = null;
            _holdingFlight = false;
            if (!headFired) { Action ha = _headArrived; _headArrived = null; ha?.Invoke(); }
            if (_hasPendingMove) { _hasPendingMove = false; _move = StartCoroutine(MoveRoutine(_pendingMoveTarget, _pendingMoveDuration)); }
            done?.Invoke();
            if (_doneAfterHold != null) { Action d = _doneAfterHold; _doneAfterHold = null; d(); }
        }

        public void MoveTo(Vector3 target, float duration)
        {
            if (_flying)   // đang bay: đừng cắt chuyến bay – dời chỗ ngay sau khi ống yên vị
            {
                _hasPendingMove = true; _pendingMoveTarget = target; _pendingMoveDuration = duration;
                return;
            }
            _hasPendingMove = false;
            if (_move != null) StopCoroutine(_move);
            _move = StartCoroutine(MoveRoutine(target, duration));
        }

        IEnumerator MoveRoutine(Vector3 target, float duration)
        {
            Vector3 start = transform.position;
            transform.rotation = DockRotation;
            for (float t = 0f; t < 1f; t += Time.deltaTime / Mathf.Max(duration, 0.01f))
            {
                transform.position = Vector3.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }
            transform.position = target;
            _move = null;
        }

        Vector3 ToPlane(Vector3 p, float z)
        {
            Vector3 up = _cam != null ? _cam.transform.up : Vector3.up;
            return new Vector3(p.x, p.y + (p.z - z) * up.z / Mathf.Max(0.1f, up.y), z);
        }

        AmmoFlightTube _body;
        bool _flying;
        float _dockFraction = 1f;

        /// <summary>Bề ngang hộp trong khay.</summary>
        public float BoxWidth => _cell * _config.ammoActiveScale;
        /// <summary>Bề ngang khi đậu = lòng khung máy xay (ConveyorSystem đo từ mesh khung gốc); 0 = dùng BoxWidth.</summary>
        public float DockWidth { get; set; }
        /// <summary>Góc xoay / nghiêng của máy xay tại dock.</summary>
        public Quaternion DockRotation { get; set; } = Quaternion.identity;
        float DockWidthResolved => DockWidth > 0f ? DockWidth : BoxWidth;
        /// <summary>Chiều cao thanh đậu / bề ngang lòng máy xay (GameConfig.dockHeightRatio).</summary>
        public float DockHeightRatio => _config != null ? _config.dockHeightRatio : DefaultDockHeightRatio;
        /// <summary>Đáy thanh đậu nằm dưới tâm 2 lưỡi cắt bao nhiêu lần bề ngang (GameConfig.dockBottomRatio).</summary>
        public float DockBottomRatio => _config != null ? _config.dockBottomRatio : 0.3f;
        /// <summary>Mặc định khi chưa có config (ConveyorLane tính chỗ xếp hàng trước khi Init).</summary>
        public const float DefaultDockHeightRatio = 2.6f;

        TextMeshPro _dockLabel;

        /// <summary>
        /// Hộp đậu trong lòng khung máy xay (tâm = tâm 2 lưỡi cắt, viền khung vẫn lộ 2 bên & dưới); đáy cố định ở
        /// dock − W/2, chiều cao giảm theo số bi còn lại → hộp lún dần vào máy xay (video gốc).
        /// </summary>
        public void ShowDocked(float fraction, int remainingCount = -1)
        {
            _fractionSet = true;
            _dockFraction = Mathf.Clamp01(fraction);
            PoseDocked();
            UpdateDockLabel(remainingCount);
        }

        /// <summary>Tạo sẵn nhãn số bi (tắt) lúc dựng hộp: tạo TextMeshPro ngay lúc ống chạm máy xay gây khựng trên máy yếu.</summary>
        void EnsureDockLabel()
        {
            if (_dockLabel == null)
            {
                var labelGo = new GameObject("DockCount");
                labelGo.transform.SetParent(transform, false);
                _dockLabel = labelGo.AddComponent<TextMeshPro>();
                if (_config != null && _config.worldFont != null) _dockLabel.font = _config.worldFont;
                _dockLabel.alignment = TextAlignmentOptions.Center;
                float w = DockWidthResolved;
                _dockLabel.fontSize = Mathf.Max(w * 3.5f, 3.2f);
                _dockLabel.color = Color.white;
                _dockLabel.fontStyle = FontStyles.Bold;
                _dockLabel.outlineColor = new Color32(0, 0, 0, 220);
                _dockLabel.outlineWidth = 0.22f;
                _dockLabel.sortingOrder = 105;
            }
        }

        void UpdateDockLabel(int count)
        {
            if (count <= 0)
            {
                if (_dockLabel != null) _dockLabel.gameObject.SetActive(false);
                return;
            }
            EnsureDockLabel();
            _dockLabel.fontSize = Mathf.Max(DockWidthResolved * 3.5f, 3.2f);   // bề ngang máy xay chỉ biết sau khi hộp được gán máng
            _dockLabel.gameObject.SetActive(true);
            _dockLabel.SetText("{0}", count);
            _dockLabel.transform.rotation = DockRotation;
            float dw = DockWidthResolved;
            Vector3 dockUp = DockRotation * Vector3.up;
            Vector3 dockFwd = DockRotation * Vector3.forward;
            _dockLabel.transform.position = LabelAnchor + dockUp * (dw * 0.15f) - dockFwd * 0.08f;
        }

        Vector3 LabelAnchor => _flying ? _flightTarget : transform.position;

        void PoseDocked()
        {
            if (_body == null || _flying) return;
            float w = DockWidthResolved;
            float h = Mathf.Max(0.03f, w * DockHeightRatio * _dockFraction);
            Vector3 dockUp = DockRotation * Vector3.up;
            Vector3 bottom = transform.position - dockUp * (w * DockBottomRatio);
            _body.PoseStraight(bottom, h, w, DockRotation);
            if (_dockLabel != null && _dockLabel.gameObject.activeSelf)
            {
                _dockLabel.transform.rotation = DockRotation;
                Vector3 dockFwd = DockRotation * Vector3.forward;
                _dockLabel.transform.position = transform.position + dockUp * (w * 0.15f) - dockFwd * 0.08f;
            }
        }

        void LateUpdate()
        {
            if (IsLaunched && _body != null && !_flying) PoseDocked(); // bám theo khi xếp hàng chờ / MoveTo
            else if (_flying && _dockLabel != null && _dockLabel.gameObject.activeSelf)
            {
                // ống đang bay: nhãn số bi đứng yên tại máy xay (không trôi theo thân ống)
                float dw = DockWidthResolved;
                _dockLabel.transform.position = _flightTarget + (DockRotation * Vector3.up) * (dw * 0.15f) - (DockRotation * Vector3.forward) * 0.08f;
            }
        }

        void OnDestroy()
        {
            if (_body != null) Destroy(_body.gameObject);
        }

        public void Finish()
        {
            Vfx.Spawn(_config.ammoLaunchVfx, transform.position, _colors.ColorOf(Data.ColorCode), 0.6f);
            Destroy(gameObject);
        }
    }
}
