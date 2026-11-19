using System;
using System.Collections.Generic;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>Ammo nhỏ trong giỏ /ba rồi nhảy ra thành ống cong (video giỏ.mp4): dùng prefab gốc TrainEngine (mesh Cube.008 gắn 23 xương).
    /// Script gốc (TrainEngine + Dreamteck SplineFollower) đã mất khi rip nên ở đây làm phần việc của nó, theo 3 pha:
    /// chờ nắp mở (ẩn) → nằm trong giỏ như cục nhỏ, nhún nhẹ (hold) → nhảy ra: ống dãn dài bay theo đường cong tới ô đích rồi co lại thành hộp (flight)</summary>
    public class TrainTube : MonoBehaviour
    {
        Transform[] _bones;
        Quaternion[] _restRot;
        float[] _along;
        Vector3 _axis;
        float _total;
        float _baseScale, _cell, _width, _miniScale, _bounce, _arcLen;
        Vector3 _p0, _p1, _p2;
        float _delay, _hold, _seconds, _t, _clock;
        Action _onArrive;
        Renderer[] _renderers;
        bool _shown;

        /// <summary>Tạo ống: from = chỗ nằm trong giỏ, to = ô đích. delay = chờ nắp mở, hold = thời gian nằm trong giỏ, seconds = thời gian bay</summary>
        public static TrainTube Launch(GameConfig cfg, Material material, Vector3 from, Vector3 to, float cell, float seconds, float delay, float hold, Action onArrive,
                                       Vector3? basketCenter = null, int miniIndex = 0)
        {
            if (cfg.trainEnginePrefab == null) { onArrive?.Invoke(); return null; }
            GameObject inst = Instantiate(cfg.trainEnginePrefab, from, Quaternion.identity);
            inst.name = "TrainTube";
            var tube = inst.AddComponent<TrainTube>();
            tube._onArrive = onArrive;
            tube._seconds = Mathf.Max(0.05f, seconds);
            tube._delay = delay;
            tube._hold = hold;
            tube._miniScale = Mathf.Clamp(cfg.trainTubeMiniScale, 0.1f, 1f);
            if (basketCenter.HasValue) { tube._center = basketCenter.Value; tube._useOffsets = true; tube._miniIndex = miniIndex; }
            tube.Setup(inst, material, from, to, cell, cfg);
            return tube;
        }

        void Setup(GameObject inst, Material material, Vector3 from, Vector3 to, float cell, GameConfig cfg)
        {
            SkinnedMeshRenderer body = null;
            foreach (SkinnedMeshRenderer s in inst.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (body == null || s.localBounds.size.y > body.localBounds.size.y) body = s;
            foreach (SkinnedMeshRenderer s in inst.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (s != body) s.gameObject.SetActive(false);   // Cube.009 là tấm số mỏng (chữ số gốc do script đã mất)
            foreach (Collider c in inst.GetComponentsInChildren<Collider>(true)) Destroy(c);
            if (body == null) { gameObject.SetActive(false); return; }
            if (material != null) body.sharedMaterial = material;
            body.updateWhenOffscreen = true;
            body.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            body.receiveShadows = false;
            int layer = LayerMask.NameToLayer("Ammunation");
            if (layer >= 0) foreach (Transform t in inst.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;

            float meshWidth = Mathf.Max(body.localBounds.size.x, body.localBounds.size.z);
            _cell = cell;
            _width = cell * cfg.trainTubeWidth;
            _baseScale = _width / Mathf.Max(meshWidth, 1e-3f);
            inst.transform.localScale = Vector3.one * _baseScale;

            var list = new List<Transform>(body.bones);
            list.RemoveAll(b => b == null);
            list.Sort((a, b) => Depth(a).CompareTo(Depth(b)));
            _bones = list.ToArray();
            int n = _bones.Length;
            _restRot = new Quaternion[n];
            _along = new float[n];
            Vector3 b0 = _bones[0].position;
            _axis = (_bones[n - 1].position - b0).normalized;
            for (int i = 0; i < n; i++)
            {
                _restRot[i] = _bones[i].rotation;
                _along[i] = Mathf.Max(0f, Vector3.Dot(_bones[i].position - b0, _axis));
            }
            _total = Mathf.Max(1e-4f, _along[n - 1]);

            _p0 = from;
            _p2 = to;
            // đường cong: nhảy lên rồi hạ xuống ô đích (video: ống cong, nghiêng về phía đích)
            Vector3 mid = (from + to) * 0.5f;
            _p1 = mid + Vector3.up * cell * cfg.trainTubeArc + Vector3.back * cell * 0.4f;
            BuildTable();
            // hướng nằm trong giỏ: về phía camera (−Z) + nghiêng ngẫu nhiên nhẹ (trainTubeHoldTilt = độ lệch tối đa, đơn vị tan)
            _cfg = cfg;
            _tiltSeed = UnityEngine.Random.insideUnitCircle;
            _renderers = inst.GetComponentsInChildren<Renderer>(true);
            Show(false);
            PlaceHold(0f);
        }

        void Show(bool on)
        {
            _shown = on;
            if (_renderers == null) return;
            foreach (Renderer r in _renderers) if (r != null) r.enabled = on;
        }

        static int Depth(Transform t) { int d = 0; for (; t.parent != null; t = t.parent) d++; return d; }

        Vector3 Bezier(float u)
        {
            float v = 1f - u;
            return v * v * _p0 + 2f * v * u * _p1 + u * u * _p2;
        }

        Vector3 Tangent(float u)
        {
            float v = 1f - u;
            Vector3 d = 2f * v * (_p1 - _p0) + 2f * u * (_p2 - _p1);
            return d.sqrMagnitude > 1e-8f ? d.normalized : (_p2 - _p0).normalized;
        }

        /// <summary>max mềm của a và b (đạo hàm liên tục), k = độ mềm</summary>
        static float SmoothMax(float a, float b, float k)
        {
            float h = Mathf.Clamp01(0.5f + 0.5f * (a - b) / Mathf.Max(1e-5f, k));
            return Mathf.Lerp(b, a, h) + k * h * (1f - h);
        }

        const int TableSize = 40;
        float[] _cumLen;

        /// <summary>Bảng chiều dài cung: để đi theo quãng đường thật (s mét) thay vì tham số u của Bezier (u không tỉ lệ với quãng đường nên tốc độ bị méo → khựng)</summary>
        void BuildTable()
        {
            _cumLen = new float[TableSize + 1];
            Vector3 prev = _p0;
            for (int i = 1; i <= TableSize; i++)
            {
                Vector3 p = Bezier(i / (float)TableSize);
                _cumLen[i] = _cumLen[i - 1] + Vector3.Distance(prev, p);
                prev = p;
            }
            _arcLen = Mathf.Max(_cell * 0.3f, _cumLen[TableSize]);
        }

        /// <summary>Điểm cách đầu đường s theo cung; ngoài hai đầu thì kéo dài theo tiếp tuyến</summary>
        Vector3 PathPoint(float s, out Vector3 tangent)
        {
            if (s <= 0f) { tangent = Tangent(0f); return _p0 + tangent * s; }
            if (s >= _arcLen) { tangent = Tangent(1f); return _p2 + tangent * (s - _arcLen); }
            int lo = 0, hi = TableSize;
            while (hi - lo > 1) { int mid = (lo + hi) >> 1; if (_cumLen[mid] <= s) lo = mid; else hi = mid; }
            float seg = Mathf.Max(1e-6f, _cumLen[hi] - _cumLen[lo]);
            float u = (lo + (s - _cumLen[lo]) / seg) / TableSize;
            tangent = Tangent(u);
            return Bezier(u);
        }

        static float Smooth(float x) { x = Mathf.Clamp01(x); return x * x * (3f - 2f * x); }

        /// <summary>Pha nằm trong giỏ: cục nhỏ (cỡ miniScale), nhún lên xuống, cuối pha vươn dài một chút trước khi nhảy</summary>
        void PlaceHold(float k)
        {
            float scale = _baseScale * _miniScale;
            transform.localScale = Vector3.one * scale;
            float w = _width * _miniScale * 1.05f;
            float lead = Smooth((k - 0.7f) / 0.3f);
            float stretch = 1f + 0.9f * lead;
            float bob = Mathf.Sin(_clock * 14f) * _cell * 0.02f * (1f - lead);
            // nằm trong giỏ (video): trục ống chúi về phía camera, thấy mặt vuông của hộp, mỗi cục nghiêng một kiểu; sắp nhảy thì xoay dần về hướng bay
            Vector3 dir = Vector3.Slerp(_holdDir, Tangent(0f), lead).normalized;
            for (int i = 0; i < _bones.Length; i++)
            {
                float f = _along[i] / _total - 0.5f;
                _bones[i].position = _p0 + Vector3.up * bob + dir * (f * w * stretch + w * 0.5f * (stretch - 1f));
                _bones[i].rotation = Quaternion.FromToRotation(_axis, dir) * _restRot[i];
            }
        }

        /// <summary>Pha bay: đầu ống đi theo đường cong, thân dãn dài ở giữa đường rồi co lại thành hộp ở ô đích</summary>
        void PlaceFlight(float t)
        {
            float scaleF = Mathf.Lerp(_miniScale, 1f, Smooth(t / 0.45f));
            transform.localScale = Vector3.one * (_baseScale * scaleF);
            float w0 = _width * _miniScale * 1.05f;      // cỡ lúc nằm trong giỏ (khớp PlaceHold để không nhảy cóc)
            float wN = _width * scaleF * 1.05f;           // cỡ hiện tại
            // đầu ống bật ra trước (ease-out: đi nhanh rồi chậm lại), đuôi ống đứng yên như lúc trong giỏ rồi mới đuổi theo (ease-in-out)
            // → liền mạch với pha hold: tại t=0 đuôi ở −0.5·w0 và đầu ở 1.4·w0 giống hệt khung cuối của PlaceHold
            float eHead = 1f - (1f - t) * (1f - t);
            float eTail = Smooth((t - 0.12f) / 0.88f);
            float head = Mathf.Lerp(1.4f * w0, _arcLen + 0.5f * wN, eHead);
            float tail = Mathf.Lerp(-0.5f * w0, _arcLen - 0.5f * wN, eTail);
            float maxLen = Mathf.Max(wN, _total * scaleF * 0.95f);
            tail = SmoothMax(tail, head - maxLen, maxLen * 0.35f);   // không dãn quá dài so với thân gốc; chặn MỀM để đuôi không giật khi bắt đầu bị kéo
            for (int i = 0; i < _bones.Length; i++)
            {
                float f = _along[i] / _total;
                float s = Mathf.Lerp(tail, head, f);
                _bones[i].position = PathPoint(s, out Vector3 tan);
                _bones[i].rotation = Quaternion.FromToRotation(_axis, tan) * _restRot[i];
            }
        }

        void Update()
        {
            _clock += Time.deltaTime;
            if (_cfg != null)
            {
                // đọc lại từ GameConfig mỗi khung hình: kéo Inspector lúc Play thấy ngay
                _miniScale = Mathf.Clamp(_cfg.trainTubeMiniScale, 0.1f, 1f);
                _holdDir = (Vector3.back + new Vector3(_tiltSeed.x, _tiltSeed.y, 0f) * _cfg.trainTubeHoldTilt).normalized;
                if (_useOffsets && _t <= 0f && _cfg.basketMiniOffsets != null && _cfg.basketMiniOffsets.Length > 0)
                {
                    // vị trí trong giỏ đọc lại từ GameConfig.basketMiniOffsets mỗi khung (chỉ trước khi nhảy)
                    Vector2 off = _cfg.basketMiniOffsets[_miniIndex % _cfg.basketMiniOffsets.Length];
                    _p0 = _center + new Vector3(off.x * _cell, off.y * _cell, -0.25f * _cell) + _cfg.basketOffset * _cell;
                    Vector3 mid = (_p0 + _p2) * 0.5f;
                    _p1 = mid + Vector3.up * _cell * _cfg.trainTubeArc + Vector3.back * _cell * 0.4f;
                    BuildTable();
                }
            }
            if (_delay > 0f)
            {
                _delay -= Time.deltaTime;
                if (_delay > 0f) return;
            }
            if (!_shown) Show(true);
            if (_hold > 0f)
            {
                _hold -= Time.deltaTime;
                float total = Mathf.Max(0.01f, _holdTotal > 0f ? _holdTotal : (_holdTotal = _hold + Time.deltaTime));
                PlaceHold(Mathf.Clamp01(1f - _hold / total));
                if (_hold > 0f) return;
            }
            _t += Time.deltaTime / _seconds;
            PlaceFlight(Mathf.Clamp01(_t));
            if (_t < 1f) return;
            _onArrive?.Invoke();
            Destroy(gameObject);
        }

        float _holdTotal;
        GameConfig _cfg;
        Vector3 _center;
        bool _useOffsets;
        int _miniIndex;
        Vector2 _tiltSeed;
        Vector3 _holdDir = Vector3.back;
    }
}
