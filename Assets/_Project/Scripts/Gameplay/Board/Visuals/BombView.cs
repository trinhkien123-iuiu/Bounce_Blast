using TMPro;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>Hiển thị + chạy đồng hồ của BOM (/bomb-timer-id) bằng prefab gốc CombinedCubicBomb.
    /// Đồng hồ đếm ngay khi vào màn (chỉ khi đang chơi, popup/tạm dừng thì đứng), 1 nhịp = bombSecondsPerTick giây; về 0 mà chưa gỡ được bom thì nổ lớn và thua</summary>
    public class BombView : MonoBehaviour
    {
        /// <summary>Gỡ bom: phồng nhẹ rồi co biến mất (không nổ để khác với lúc thua)</summary>
        const float VanishSeconds = 0.3f;
        const float PopScale = 1.18f;
        const float PopPortion = 0.3f;
        /// <summary>Số nổi lên trước mặt quả bom (theo bề ngang quả bom)</summary>
        const float LabelLiftRatio = 0.06f;

        GameConfig _cfg;
        Transform _model;
        Transform _textAnchor;
        TextMeshPro _label;
        Transform _fuse;
        ParticleSystem _nuke;
        Bounds _body;
        float _width;
        int _remaining;
        float _acc;
        bool _done;
        float _shrink = -1f;
        float _loseIn = -1f;

        /// <summary>Dựng quả bom dưới parent, căn vừa các ô nó chiếm</summary>
        public static BombView Build(Transform parent, BoardItemData item, float cell, GameConfig cfg, BlockVisualFactory visuals, int layer)
        {
            var root = new GameObject("Visual").transform;
            root.SetParent(parent, false);
            var view = root.gameObject.AddComponent<BombView>();
            view._cfg = cfg;
            view._remaining = Mathf.Max(1, item.Timer);
            if (cfg.bombPrefab == null) return view;

            GameObject inst = Instantiate(cfg.bombPrefab, root);
            inst.name = "Bomb";
            // prefab dựng trong khung "nhìn từ trên" (chữ quay 90° quanh X, ngòi theo +Z): xoay −90° để dựng đứng, mặt trước hướng camera
            inst.transform.localPosition = Vector3.zero;
            // thêm góc ngả về phía camera để lộ mặt trên quả bom (giống gạch ngả −20°)
            inst.transform.localRotation = Quaternion.Euler(-90f - cfg.bombTiltDegrees, 0f, 0f);
            inst.transform.localScale = Vector3.one;
            ModelUtil.StripPhysics(inst);
            foreach (Transform t in inst.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;

            view._model = inst.transform.Find("Model");
            view._textAnchor = FindStartingWith(inst.transform, "3D Text");
            HideTexts(inst.transform);
            Transform nuke = inst.transform.Find("NukeExplosionFire");
            view._nuke = nuke != null ? nuke.GetComponent<ParticleSystem>() : null;
            view._fuse = FindDeep(inst.transform, "BombFuse2");

            // căn cỡ: bề ngang quả bom = bề ngang các ô chiếm × bombWidthRatio (hạt nổ co giãn theo)
            view._width = item.Cells.width * cell * cfg.bombWidthRatio;
            ModelUtil.FitVisualUniform(root, view._width);
            Renderer body = view._model != null ? view._model.GetComponentInChildren<Renderer>(true) : null;
            if (body != null)
            {
                view._body = body.bounds;
                BrightenBody(body, cfg);
            }
            view.BuildLabel(parent, item, visuals);
            return view;
        }

        /// <summary>Material TNT gốc dùng đúng như rip (TCP2 Hybrid, bóng _SColor 0.22) nhưng sáng của scene này làm quả bom tối: nâng bóng và mở rộng vùng sáng, chỉnh trong GameConfig</summary>
        static void BrightenBody(Renderer body, GameConfig cfg)
        {
            Material src = body.sharedMaterial;
            if (src == null) return;
            var m = new Material(src);
            float s = cfg.bombShadowBrightness;
            if (m.HasProperty("_SColor")) m.SetColor("_SColor", new Color(s, s, s, 1f));
            if (m.HasProperty("_RampThreshold")) m.SetFloat("_RampThreshold", cfg.bombRampThreshold);
            body.sharedMaterial = m;
        }

        static Transform FindDeep(Transform root, string name)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }

        static Transform FindStartingWith(Transform root, string prefix)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) if (t.name.StartsWith(prefix)) return t;
            return null;
        }

        /// <summary>Prefab rip đã mất script TextMeshPro: tắt MeshRenderer rỗng của chữ có sẵn (số do nhãn riêng hiển thị)</summary>
        static void HideTexts(Transform root)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name.StartsWith("3D Text") || t.name.StartsWith("Text (TMP)"))
                    foreach (Renderer r in t.GetComponents<Renderer>()) r.enabled = false;
        }

        /// <summary>Số đếm ngược nằm trên dải vàng giữa quả bom</summary>
        void BuildLabel(Transform blockRoot, BoardItemData item, BlockVisualFactory visuals)
        {
            if (_textAnchor == null || visuals == null) return;
            float d = _width * 0.5f;
            // nhãn gắn vào khối (không gắn vào Visual vì Visual đã bị co theo cỡ quả bom)
            _label = visuals.CreateLabel(blockRoot, item, new Vector2(d, d));
            if (_label == null) return;
            _label.fontSize = Mathf.Clamp(d * 8f, 1.6f, 12f);
            _label.SetText("{0}", _remaining);
            // đặt ngay trước mặt trước quả bom (camera nhìn về +Z nên mặt trước là z nhỏ nhất); lưu độ lệch để bám theo khi khối rơi
            float anchorZ = _textAnchor.position.z;
            float front = _body.size.sqrMagnitude > 0f ? Mathf.Min(anchorZ, _body.min.z) : anchorZ;
            _labelZ = front - LabelLiftRatio * _width - anchorZ;
            LateUpdate();
        }

        float _labelZ;

        void LateUpdate()
        {
            if (_label == null || _textAnchor == null) return;
            Vector3 p = _textAnchor.position;
            p.z += _labelZ;
            _label.transform.position = p;
            _label.transform.rotation = transform.rotation;
        }

        void Update()
        {
            if (_shrink >= 0f) UpdateShrink();
            GameManager gm = GameManager.Instance;
            if (Exploded)
            {
                // đợi hiệu ứng nổ chạy gần hết rồi mới hiện popup thua (thời gian thật, không phụ thuộc timeScale)
                if (_loseIn >= 0f)
                {
                    _loseIn -= Time.unscaledDeltaTime;
                    if (_loseIn < 0f && gm != null) gm.LoseByBomb();
                }
                return;
            }
            if (_done) return;
            if (gm == null || gm.state != GameState.Playing) return;
            _acc += Time.deltaTime;
            float tick = Mathf.Max(0.05f, _cfg.bombSecondsPerTick);
            while (_acc >= tick && _remaining > 0) { _acc -= tick; _remaining--; }
            if (_label != null) _label.SetText("{0}", _remaining);
            if (_remaining <= 0) Explode();
        }

        /// <summary>Hết giờ: nổ lớn, chờ hiệu ứng nổ chạy gần hết (bombLoseDelay) rồi mới thua level</summary>
        void Explode()
        {
            _done = true;
            Exploded = true;
            _loseIn = Mathf.Max(0f, _cfg.bombLoseDelay);
            StopFuse();
            if (_nuke != null) _nuke.Play(true);
            AudioManager.Play(Sfx.BombExploded);
            // mọi khối vỡ nát lan ra từ chỗ bom, xong trước khi hiện popup thua
            GameManager gm = GameManager.Instance;
            if (gm != null && gm.board != null) gm.board.ShatterAll(transform.position, _cfg.bombLoseDelay * 0.7f);
        }

        /// <summary>Đã nổ (hết giờ): không còn gỡ được nữa</summary>
        public bool Exploded { get; private set; }

        /// <summary>Gỡ bom kịp: ngòi tắt, quả bom phồng nhẹ rồi co biến mất (KHÔNG nổ); tách khỏi khối để chạy nốt hiệu ứng</summary>
        public void Defuse()
        {
            if (Exploded) return;
            _done = true;
            StopFuse();
            transform.SetParent(Vfx.Root, true);
            if (_label != null) _label.gameObject.SetActive(false);
            AudioManager.Play(Sfx.BombDefused);
            _shrink = 0f;
        }

        void StopFuse()
        {
            if (_fuse == null) return;
            foreach (ParticleSystem ps in _fuse.GetComponentsInChildren<ParticleSystem>(true)) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        void UpdateShrink()
        {
            _shrink += Time.deltaTime;
            float k = Mathf.Clamp01(_shrink / VanishSeconds);
            float s = k < PopPortion
                ? Mathf.Lerp(1f, PopScale, k / PopPortion)
                : Mathf.Lerp(PopScale, 0f, (k - PopPortion) / (1f - PopPortion));
            if (_model != null) _model.localScale = Vector3.one * s;
            if (k >= 1f) Destroy(gameObject);
        }
    }
}
