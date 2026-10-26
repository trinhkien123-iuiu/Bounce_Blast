using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>
    /// Hiệu ứng hình + tiếng cho 4 booster. Dựng hoàn toàn bằng sprite sinh lúc chạy (vòng sóng, sao, tia, quầng) + ParticleSystem,
    /// ghép với icon booster gốc (búa, bàn tay, mũi tên xáo, dấu cộng) – không cần prefab riêng, mọi cỡ/độ lâu chỉnh trong GameConfig → "Booster VFX".
    ///  - Búa: búa to vung lên, đập xuống; sóng xung kích + nứt toả tia + rung camera; khối quanh điểm đập vỡ lan dần từ tâm ra.
    ///  - Magic Hand: bàn tay phép hiện trên hộp, gõ một cái → vòng sáng + sao bay lên cùng màu hộp.
    ///  - Shuffle: xoáy sao + mũi tên xáo quay ở giữa khay, từng hộp "nổ" sao theo vòng xoáy.
    ///  - Capacity: dấu cộng bật to trên thanh Capacity, tia sáng xoay, hạt xanh bay lên, thanh nảy.
    /// </summary>
    public class BoosterFx : MonoBehaviour
    {
        public static BoosterFx Instance { get; private set; }

        GameConfig _config;
        Camera _cam;
        Material _spriteMat;
        Sprite _soft, _ring, _star, _streak;
        int _shakeId;

        const int Order = 600;   // trên chữ HP (5–6) và mọi sprite khác
        const float Z = -4f;     // trước bàn & khay (camera ở z = -24.8 nhìn +Z)

        public static BoosterFx Create(GameConfig config, Camera cam, Transform parent)
        {
            var go = new GameObject("BoosterFx");
            go.transform.SetParent(parent, false);
            var fx = go.AddComponent<BoosterFx>();
            fx._config = config; fx._cam = cam;
            Instance = fx;
            return fx;
        }

        void Awake()
        {
            Shader sh = Shader.Find("Sprites/Default");
            _spriteMat = new Material(sh);
            _soft = MakeSprite(128, (x, y, r) => Mathf.Pow(Mathf.Clamp01(1f - r), 2f));
            _ring = MakeSprite(128, (x, y, r) => Mathf.Clamp01(1f - Mathf.Abs(r - 0.82f) / 0.1f) * Mathf.Clamp01((1f - r) * 8f + 0.2f));
            _star = MakeSprite(128, (x, y, r) =>
            {
                float v = Mathf.Sqrt(Mathf.Abs(x)) + Mathf.Sqrt(Mathf.Abs(y));   // đường astroid → sao 4 cánh nhọn
                return Mathf.Clamp01(1f - v * 1.05f) * Mathf.Clamp01(1.4f - r) + Mathf.Pow(Mathf.Clamp01(1f - r * 2.2f), 2f);
            });
            _streak = MakeSprite(128, (x, y, r) => Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(y) * 5f), 2f) * Mathf.Clamp01(1f - Mathf.Abs(x)));
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        // ------------------------------------------------------------------ BÚA
        /// <summary>
        /// Búa vung xuống điểm <paramref name="point"/>. <paramref name="onImpact"/> gọi đúng khoảnh khắc búa chạm (lúc đó mới phá khối).
        /// Trả về coroutine đã chạy; có thể chờ để biết khi nào xong cả hiệu ứng.
        /// </summary>
        public Coroutine Hammer(Vector3 point, float radius, Action onImpact)
        {
            return StartCoroutine(HammerRoutine(new Vector3(point.x, point.y, Z), radius, onImpact));
        }

        IEnumerator HammerRoutine(Vector3 p, float radius, Action onImpact)
        {
            float c = Mathf.Max(radius, 0.5f);
            AudioManager.Play(Sfx.HammerSwing);

            // vùng sẽ vỡ: vòng cảnh báo mờ thu nhỏ lại khi búa hạ xuống
            SpriteRenderer warn = Spawn(_ring, p, new Color(1f, 0.85f, 0.3f, 0f), radius * 2.6f, Order - 2);

            Sprite icon = _config != null ? _config.boosterHammerSprite : null;
            Transform hammer = null;
            Vector2 d = Vector2.zero;   // từ chỗ cầm (cuối cán đỏ) tới mặt đầu búa (đầu vàng), theo đơn vị world đã nhân cỡ
            if (icon != null)
            {
                // Sprite búa gốc: đầu vàng ở trên-trái, cán đỏ ở dưới-phải. Gốc của object cha = chỗ cầm, quay quanh đó → ĐẦU búa là thứ chạm đất.
                hammer = new GameObject("Hammer").transform;
                hammer.SetParent(transform, false);
                float sz = Mathf.Clamp(c * 3.4f, 1.8f, 3f) / Mathf.Max(icon.bounds.size.x, icon.bounds.size.y);
                float w = icon.bounds.size.x * sz, h = icon.bounds.size.y * sz;
                Vector2 grip = new Vector2(-0.17f * w, -0.48f * h), face = new Vector2(-0.38f * w, 0.38f * h);
                d = face - grip;
                var sr = new GameObject("Icon").AddComponent<SpriteRenderer>();
                sr.transform.SetParent(hammer, false);
                sr.sprite = icon; sr.sortingOrder = Order + 5; sr.sharedMaterial = _spriteMat;
                sr.transform.localScale = Vector3.one * sz;
                sr.transform.localPosition = new Vector3(-grip.x, -grip.y, 0f);
                var shadow = new GameObject("Shadow").AddComponent<SpriteRenderer>();
                shadow.transform.SetParent(hammer, false); shadow.sprite = icon; shadow.sortingOrder = Order + 4; shadow.sharedMaterial = _spriteMat;
                shadow.color = new Color(0f, 0f, 0f, 0.35f);
                shadow.transform.localScale = sr.transform.localScale; shadow.transform.localPosition = sr.transform.localPosition + new Vector3(0.12f, -0.18f, 0.01f);
            }

            // Vị trí chỗ cầm sao cho mặt đầu búa nằm đúng điểm p khi búa xoay góc rot
            Func<float, Vector3> gripFor = rot => p - (Vector3)(Quaternion.Euler(0f, 0f, rot) * d);
            const float rotBack = -40f, rotHit = 90f;   // 0° = tư thế sprite gốc (mặt đầu quay sang trái); +90° = mặt đầu quay xuống
            Vector3 high = gripFor(rotBack) + new Vector3(c * 0.5f, c * 1.6f, 0f);
            Vector3 far = high + new Vector3(c * 1.8f, c * 1.4f, 0f);
            // 1) lấy đà: búa bay vào từ xa, nhấc cao ra sau (đầu búa hướng lên)
            yield return Anim(0.28f, t =>
            {
                float e = EaseOutBack(t);
                if (hammer != null) { hammer.position = Vector3.LerpUnclamped(far, high, e); hammer.rotation = Quaternion.Euler(0f, 0f, Mathf.LerpUnclamped(15f, rotBack, e)); hammer.localScale = Vector3.one * Mathf.Lerp(0.4f, 1f, Mathf.Clamp01(t * 2f)); }
                warn.color = new Color(1f, 0.85f, 0.3f, 0.5f * t);
                warn.transform.localScale = Vector3.one * radius * 2.6f * Mathf.Lerp(1.5f, 1f, t);
            });
            yield return new WaitForSeconds(0.05f);
            // 2) vung xuống nhanh (ease-in): xoay quanh chỗ cầm, đầu búa vạch cung rồi đập xuống p
            Vector3 hitGrip = gripFor(rotHit);
            yield return Anim(0.13f, t =>
            {
                float e = t * t * t;
                if (hammer != null) { hammer.position = Vector3.Lerp(high, hitGrip, e); hammer.rotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(rotBack, rotHit, e)); }
            });

            // 3) VA CHẠM
            onImpact?.Invoke();
            AudioManager.Play(Sfx.Hammer);
            Shake(c * 0.34f, 0.42f);
            Destroy(warn.gameObject);
            Flash(p, new Color(1f, 0.95f, 0.7f), c * 5.5f, 0.32f);
            Ring(p, new Color(1f, 0.92f, 0.55f, 1f), c * 0.5f, radius * 4.6f, 0.65f);
            Ring(p, new Color(1f, 0.55f, 0.2f, 0.9f), c * 0.3f, radius * 3.2f, 0.55f, 0.06f);
            Ring(p, new Color(1f, 1f, 1f, 0.8f), c * 0.2f, radius * 2.2f, 0.4f, 0.12f);
            Cracks(p, radius * 2.8f);
            Particles(p, _star, new Color(1f, 0.92f, 0.45f), new Color(1f, 0.5f, 0.15f), 56, c * 9f, 1f, c * 0.5f, c * 6f, 360f);
            Particles(p, _soft, new Color(1f, 1f, 1f, 0.6f), new Color(0.8f, 0.8f, 0.9f, 0f), 18, c * 3.2f, 1f, c * 1.3f, 0f, 0f);
            if (hammer != null)
            {
                // nảy ngược nhẹ rồi mờ dần
                Vector3 from = hammer.position; float rot0 = hammer.eulerAngles.z;
                var renderers = hammer.GetComponentsInChildren<SpriteRenderer>();
                yield return Anim(0.3f, t =>
                {
                    hammer.position = from + new Vector3(c * 0.5f * t, c * 0.9f * Mathf.Sin(t * Mathf.PI) , 0f);
                    hammer.rotation = Quaternion.Euler(0f, 0f, rot0 - 22f * EaseOutBack(t));
                    foreach (SpriteRenderer r in renderers) { Color col = r.color; col.a = (r.name == "Shadow" ? 0.35f : 1f) * (1f - t); r.color = col; }
                });
                Destroy(hammer.gameObject);
            }
        }

        // ------------------------------------------------------------------ MAGIC HAND
        public void MagicHand(Vector3 point, Color tint)
        {
            StartCoroutine(MagicHandRoutine(new Vector3(point.x, point.y, Z), tint));
        }

        IEnumerator MagicHandRoutine(Vector3 p, Color tint)
        {
            float u = _config != null ? _config.boosterFxUnit : 1.5f;
            Color glow = Color.Lerp(tint, Color.white, 0.35f);
            AudioManager.Play(Sfx.MagicHandTap);
            Sprite icon = _config != null ? _config.boosterMagicSprite : null;
            SpriteRenderer hand = null, shadow = null;
            float baseS = 1f;
            Vector3 tip = Vector3.zero;   // từ tâm sprite tới đầu ngón trỏ (đã nhân cỡ) – đầu ngón chạm đúng tâm hộp
            if (icon != null)
            {
                baseS = u * 1.7f / Mathf.Max(icon.bounds.size.x, icon.bounds.size.y);
                tip = new Vector3(-0.16f * icon.bounds.size.x, 0.27f * icon.bounds.size.y, 0f) * baseS;
                hand = Spawn(icon, p, Color.white, baseS, Order + 5);
                shadow = Spawn(icon, p, new Color(0f, 0f, 0f, 0.3f), baseS, Order + 4);
            }
            // gõ tức thì (cùng lúc hộp bay lên): tay hiện từ trên-phải, nhấn xuống hộp, rồi nhấc lên – giữ nguyên tỉ lệ, chỉ phóng đều
            Vector3 from = p + new Vector3(u * 0.35f, u * 0.55f, 0f);
            Action<Vector3, float, float> place = (target, scale, alpha) =>
            {
                if (hand == null) return;
                hand.transform.position = target - tip * scale; hand.transform.localScale = Vector3.one * baseS * scale; hand.color = new Color(1f, 1f, 1f, alpha);
                shadow.transform.position = hand.transform.position + new Vector3(0.1f, -0.14f, 0.01f); shadow.transform.localScale = hand.transform.localScale; shadow.color = new Color(0f, 0f, 0f, 0.3f * alpha);
            };
            yield return Anim(0.07f, t => place(Vector3.Lerp(from, p, t * t), Mathf.Lerp(1.15f, 1f, t), Mathf.Clamp01(t * 4f)));
            // chạm: vòng sáng + sao cùng màu hộp
            Flash(p, glow, u * 2.2f, 0.25f);
            Ring(p, new Color(glow.r, glow.g, glow.b, 0.95f), u * 0.3f, u * 2.4f, 0.5f);
            Ring(p, new Color(1f, 1f, 1f, 0.8f), u * 0.2f, u * 1.6f, 0.4f, 0.06f);
            Particles(p, _star, Color.white, glow, 26, u * 3.8f, 0.9f, u * 0.3f, -u * 0.8f, 200f, up: true);
            Particles(p, _soft, new Color(glow.r, glow.g, glow.b, 0.8f), new Color(glow.r, glow.g, glow.b, 0f), 10, u * 1.5f, 0.7f, u * 0.5f, 0f, 0f);
            yield return Anim(0.08f, t => place(p, Mathf.Lerp(1f, 0.88f, t), 1f));
            yield return Anim(0.3f, t => place(Vector3.Lerp(p, p + new Vector3(u * 0.3f, u * 0.5f, 0f), EaseOutBack(t)), Mathf.Lerp(0.88f, 1f, t), 1f - t));
            if (hand != null) { Destroy(hand.gameObject); Destroy(shadow.gameObject); }
        }

        // ------------------------------------------------------------------ SHUFFLE
        /// <param name="center">tâm khay</param>
        /// <param name="boxes">vị trí (world) các hộp để nổ sao</param>
        public void Shuffle(Vector3 center, IList<Vector3> boxes, float radius)
        {
            StartCoroutine(ShuffleRoutine(new Vector3(center.x, center.y, Z), boxes, radius));
        }

        IEnumerator ShuffleRoutine(Vector3 center, IList<Vector3> boxes, float radius)
        {
            float u = _config != null ? _config.boosterFxUnit : 1.5f;
            AudioManager.Play(Sfx.ShuffleWhoosh);
            Color pink = new Color(1f, 0.45f, 0.85f), cyan = new Color(0.4f, 0.9f, 1f);
            Sprite icon = _config != null ? _config.boosterShuffleSprite : null;
            SpriteRenderer arrows = null;
            if (icon != null)
            {
                arrows = Spawn(icon, center, Color.white, u * 2.8f / Mathf.Max(icon.bounds.size.x, icon.bounds.size.y), Order + 5);
            }
            SpriteRenderer halo = Spawn(_soft, center, new Color(pink.r, pink.g, pink.b, 0f), radius * 2.6f, Order - 3);
            Ring(center, new Color(cyan.r, cyan.g, cyan.b, 0.9f), u * 0.3f, radius * 1.5f, 0.6f);
            Ring(center, new Color(pink.r, pink.g, pink.b, 0.8f), u * 0.2f, radius * 1.1f, 0.5f, 0.12f);
            Ring(center, new Color(cyan.r, cyan.g, cyan.b, 0.7f), u * 0.3f, radius * 1.7f, 0.6f, 0.3f);   // sóng xanh thứ hai như bản gốc
            // xoáy sao quanh tâm (hạt có vận tốc quỹ đạo)
            SwirlParticles(center, radius, pink, cyan);

            // hộp nổ sao nối tiếp theo góc quanh tâm (cảm giác bị quét xoáy)
            var order = new List<int>(); for (int i = 0; i < boxes.Count; i++) order.Add(i);
            order.Sort((a, b) => Mathf.Atan2(boxes[a].y - center.y, boxes[a].x - center.x).CompareTo(Mathf.Atan2(boxes[b].y - center.y, boxes[b].x - center.x)));
            float total = 0.7f;
            StartCoroutine(ShuffleBursts(boxes, order, total, pink, cyan));

            yield return Anim(total + 0.25f, t =>
            {
                float sc = t < 0.2f ? EaseOutBack(t / 0.2f) : 1f - Mathf.SmoothStep(0f, 1f, (t - 0.75f) / 0.25f) * 1f;
                halo.color = new Color(pink.r, pink.g, pink.b, 0.5f * Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI));
                halo.transform.localScale = Vector3.one * radius * 2.6f * (0.7f + 0.5f * t);
                if (arrows != null)
                {
                    arrows.transform.rotation = Quaternion.Euler(0f, 0f, -t * 540f);
                    arrows.transform.localScale = Vector3.one * (u * 3.6f / Mathf.Max(icon.bounds.size.x, icon.bounds.size.y)) * Mathf.Max(0.01f, sc) * (1f + 0.22f * Mathf.Sin(t * 19f));
                    arrows.color = new Color(1f, 1f, 1f, Mathf.Clamp01(1f - (t - 0.8f) * 5f));
                }
            });
            Destroy(halo.gameObject);
            if (arrows != null) Destroy(arrows.gameObject);
        }

        IEnumerator ShuffleBursts(IList<Vector3> boxes, List<int> order, float total, Color a, Color b)
        {
            float u = _config != null ? _config.boosterFxUnit : 1.5f;
            float step = order.Count > 0 ? total / order.Count : 0f;
            for (int k = 0; k < order.Count; k++)
            {
                Vector3 p = new Vector3(boxes[order[k]].x, boxes[order[k]].y, Z);
                Color col = (k & 1) == 0 ? a : b;
                Flash(p, col, u * 1.1f, 0.2f);
                Ring(p, new Color(col.r, col.g, col.b, 0.9f), u * 0.15f, u * 0.95f, 0.35f);
                Particles(p, _star, Color.white, col, 7, u * 2.2f, 0.55f, u * 0.2f, 0f, 240f);
                AudioManager.Play(Sfx.ShufflePop);
                if (step > 0f) yield return new WaitForSeconds(step);
            }
        }

        // ------------------------------------------------------------------ CAPACITY
        /// <param name="onArrive">gọi khi dấu cộng "chui" vào thanh – lúc đó mới cộng sức chứa thật để thanh chạy theo</param>
        public void Capacity(Vector3 barPos, Transform bar, float percent, Action onArrive)
        {
            StartCoroutine(CapacityRoutine(new Vector3(barPos.x, barPos.y, Z), bar, percent, onArrive));
        }

        IEnumerator CapacityRoutine(Vector3 p, Transform bar, float percent, Action onArrive)
        {
            float u = _config != null ? _config.boosterFxUnit : 1.5f;
            AudioManager.Play(Sfx.CapacityUp);
            Color green = new Color(0.45f, 1f, 0.5f), gold = new Color(1f, 0.9f, 0.3f);
            Sprite icon = _config != null ? _config.boosterPlusSprite : null;
            SpriteRenderer plus = null;
            float ps = 1f;
            Vector3 hover = p + new Vector3(0f, u * 1.5f, 0f);   // dấu cộng bật lên phía trên thanh rồi rơi xuống thanh
            if (icon != null) { ps = u * 2.1f / Mathf.Max(icon.bounds.size.x, icon.bounds.size.y); plus = Spawn(icon, hover, Color.white, ps, Order + 5); }
            // chữ "+25%" bên phải dấu cộng (không đè lên chữ CAPACITY của thanh)
            TextMeshPro txt = null;
            if (_config != null)
            {
                txt = new GameObject("PercentText").AddComponent<TextMeshPro>();
                txt.transform.SetParent(transform, false);
                if (_config.worldFont != null) txt.font = _config.worldFont;
                txt.text = "+" + Mathf.RoundToInt(percent * 100f) + "%";
                txt.alignment = TextAlignmentOptions.Center; txt.fontSize = u * 6f; txt.color = new Color(1f, 0.95f, 0.45f);
                txt.outlineWidth = 0.25f; txt.outlineColor = new Color32(120, 40, 0, 255);
                txt.sortingOrder = Order + 6; txt.rectTransform.sizeDelta = new Vector2(u * 4f, u * 1.4f);
            }
            SpriteRenderer rays = Spawn(_star, hover, new Color(gold.r, gold.g, gold.b, 0f), u * 5f, Order - 1);
            SpriteRenderer halo = Spawn(_soft, hover, new Color(green.r, green.g, green.b, 0f), u * 5.5f, Order - 3);
            Vector3 barScale = bar != null ? bar.localScale : Vector3.one;
            Ring(hover, new Color(green.r, green.g, green.b, 0.9f), u * 0.4f, u * 4.2f, 0.6f);
            Ring(hover, new Color(gold.r, gold.g, gold.b, 0.8f), u * 0.2f, u * 3f, 0.45f, 0.1f);
            Particles(hover, _star, Color.white, green, 30, u * 4f, 1.1f, u * 0.28f, -u * 1.2f, 220f, up: true);

            // 1) bật lên + giữ (0.45s)
            yield return Anim(0.45f, t =>
            {
                float pop = EaseOutBack(Mathf.Clamp01(t / 0.4f));
                float bob = 0.12f * u * Mathf.Sin(t * 9f);
                if (plus != null) { plus.transform.localScale = Vector3.one * ps * pop; plus.transform.position = hover + new Vector3(0f, bob, 0f); }
                if (txt != null) { txt.transform.position = hover + new Vector3(u * 2.1f, bob, 0f); txt.transform.localScale = Vector3.one * pop; }
                rays.transform.rotation = Quaternion.Euler(0f, 0f, t * 120f); rays.transform.position = hover;
                rays.color = new Color(gold.r, gold.g, gold.b, 0.85f * Mathf.Clamp01(t * 3f));
                halo.color = new Color(green.r, green.g, green.b, 0.45f * t);
            });
            // 2) rơi vào thanh: dấu cộng + chữ thu nhỏ dần về giữa thanh, thanh bị kéo giãn (phồng ngang + dày lên) rồi bật lại
            yield return Anim(0.3f, t =>
            {
                float e = t * t;
                Vector3 pos = Vector3.Lerp(hover, p, e);
                float sc = Mathf.Lerp(1f, 0.35f, e);
                if (plus != null) { plus.transform.position = pos; plus.transform.localScale = Vector3.one * ps * sc; }
                if (txt != null) { txt.transform.position = pos + new Vector3(u * 2.1f * (1f - e), 0f, 0f); txt.transform.localScale = Vector3.one * sc; }
                rays.transform.position = pos; rays.color = new Color(gold.r, gold.g, gold.b, 0.85f * (1f - e));
                halo.color = new Color(green.r, green.g, green.b, 0.45f);
                if (bar != null) bar.localScale = new Vector3(barScale.x * (1f + 0.08f * e), barScale.y * (1f + 0.5f * e), barScale.z);
            });
            onArrive?.Invoke();
            if (plus != null) plus.color = new Color(1f, 1f, 1f, 0f);
            if (txt != null) txt.color = new Color(1f, 1f, 1f, 0f);
            Flash(p, new Color(1f, 0.95f, 0.5f), u * 3.2f, 0.3f);
            Ring(p, new Color(gold.r, gold.g, gold.b, 0.95f), u * 0.4f, u * 3.6f, 0.5f);
            Particles(p, _star, Color.white, gold, 22, u * 3.5f, 0.8f, u * 0.25f, 0f, 240f);
            // 3) thanh bật lại kiểu lò xo (kéo giãn rồi co về)
            yield return Anim(0.45f, t =>
            {
                float k = Mathf.Exp(-5f * t) * Mathf.Cos(t * 18f);   // dao động tắt dần quanh 1
                if (bar != null) bar.localScale = new Vector3(barScale.x * (1f + 0.08f * k), barScale.y * (1f + 0.5f * k), barScale.z);
                halo.color = new Color(green.r, green.g, green.b, 0.45f * (1f - t));
            });
            if (bar != null) bar.localScale = barScale;
            if (plus != null) Destroy(plus.gameObject);
            if (txt != null) Destroy(txt.gameObject);
            Destroy(rays.gameObject); Destroy(halo.gameObject);
        }

        // ------------------------------------------------------------------ MỞ KHOÁ BOOSTER
        /// <summary>Nổ sao + vòng sáng ngay trên nút booster vừa mở khoá (screenPos = toạ độ màn hình của nút).</summary>
        public void Unlock(Vector2 screenPos)
        {
            if (_cam == null) return;
            Ray ray = _cam.ScreenPointToRay(screenPos);
            float dz = ray.direction.z;
            if (Mathf.Abs(dz) < 1e-4f) return;
            Vector3 p = ray.origin + ray.direction * ((Z - ray.origin.z) / dz);
            float u = _config != null ? _config.boosterFxUnit : 1.5f;
            Color gold = new Color(1f, 0.9f, 0.35f);
            Flash(p, new Color(1f, 0.95f, 0.6f), u * 3f, 0.35f);
            Ring(p, new Color(gold.r, gold.g, gold.b, 0.95f), u * 0.3f, u * 3.2f, 0.55f);
            Ring(p, new Color(1f, 1f, 1f, 0.8f), u * 0.2f, u * 2.2f, 0.4f, 0.08f);
            Particles(p, _star, Color.white, gold, 34, u * 4.2f, 0.9f, u * 0.3f, -u * 0.6f, 260f);
        }

        // ------------------------------------------------------------------ Khối dựng chung
        /// <summary>Rung camera: biên độ theo đơn vị thế giới, giảm dần.</summary>
        public void Shake(float amplitude, float duration)
        {
            if (_cam == null) return;
            StartCoroutine(ShakeRoutine(amplitude, duration, ++_shakeId));
        }

        IEnumerator ShakeRoutine(float amp, float dur, int id)
        {
            Vector3 basePos = _cam.transform.position;
            for (float t = 0f; t < dur; t += Time.unscaledDeltaTime)
            {
                if (id != _shakeId || _cam == null) yield break;   // có cú rung mới hơn → nhường
                float k = 1f - t / dur;
                _cam.transform.position = basePos + (Vector3)(UnityEngine.Random.insideUnitCircle * amp * k * k);
                yield return null;
            }
            if (_cam != null) _cam.transform.position = basePos;
        }

        void Ring(Vector3 p, Color col, float from, float to, float dur, float delay = 0f) => StartCoroutine(RingRoutine(p, col, from, to, dur, delay));

        IEnumerator RingRoutine(Vector3 p, Color col, float from, float to, float dur, float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            SpriteRenderer r = Spawn(_ring, p, col, from, Order - 1);
            yield return Anim(dur, t =>
            {
                float e = 1f - (1f - t) * (1f - t) * (1f - t);
                r.transform.localScale = Vector3.one * Mathf.Lerp(from, to, e);
                r.color = new Color(col.r, col.g, col.b, col.a * (1f - t));
            });
            Destroy(r.gameObject);
        }

        void Flash(Vector3 p, Color col, float size, float dur) => StartCoroutine(FlashRoutine(p, col, size, dur));

        IEnumerator FlashRoutine(Vector3 p, Color col, float size, float dur)
        {
            SpriteRenderer a = Spawn(_soft, p, new Color(col.r, col.g, col.b, 0.9f), size, Order - 2);
            SpriteRenderer b = Spawn(_star, p, new Color(1f, 1f, 1f, 1f), size * 0.9f, Order);
            float spin = UnityEngine.Random.Range(0f, 90f);
            yield return Anim(dur, t =>
            {
                float e = 1f - t;
                a.transform.localScale = Vector3.one * size * (0.5f + 0.7f * t); a.color = new Color(col.r, col.g, col.b, 0.9f * e);
                b.transform.localScale = Vector3.one * size * (0.4f + 0.6f * Mathf.Sin(t * Mathf.PI * 0.5f)); b.transform.rotation = Quaternion.Euler(0f, 0f, spin + t * 50f); b.color = new Color(1f, 1f, 1f, e);
            });
            Destroy(a.gameObject); Destroy(b.gameObject);
        }

        /// <summary>Nứt toả tia từ điểm đập: những vệt sáng mảnh dài ra rồi mờ dần.</summary>
        void Cracks(Vector3 p, float length)
        {
            int n = 9;
            float start = UnityEngine.Random.Range(0f, 360f);
            for (int i = 0; i < n; i++)
            {
                float ang = start + i * 360f / n + UnityEngine.Random.Range(-14f, 14f);
                float len = length * UnityEngine.Random.Range(0.7f, 1.25f);
                StartCoroutine(CrackRoutine(p, ang, len, i % 2 == 0 ? new Color(1f, 0.95f, 0.75f) : new Color(1f, 0.6f, 0.25f)));
            }
        }

        IEnumerator CrackRoutine(Vector3 p, float ang, float len, Color col)
        {
            SpriteRenderer r = Spawn(_streak, p, col, 1f, Order - 1);
            r.transform.rotation = Quaternion.Euler(0f, 0f, ang);
            float w = len * 0.13f;
            yield return Anim(0.85f, t =>
            {
                float grow = 1f - Mathf.Pow(1f - Mathf.Clamp01(t * 3f), 3f);
                r.transform.localScale = new Vector3(len * grow, w, 1f);
                r.transform.position = p + Quaternion.Euler(0f, 0f, ang) * new Vector3(len * grow * 0.5f, 0f, 0f);
                r.color = new Color(col.r, col.g, col.b, 1f - Mathf.SmoothStep(0.2f, 1f, t));
            });
            Destroy(r.gameObject);
        }

        // ------------------------------------------------------------------ Hạt
        void Particles(Vector3 p, Sprite sprite, Color c0, Color c1, int count, float speed, float life, float size, float gravity, float spin, bool up = false)
        {
            var go = new GameObject("Particles"); go.transform.SetParent(transform, false); go.transform.position = p;
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.playOnAwake = false; main.loop = false; main.duration = 0.1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.6f, life);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.35f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
            main.startColor = new ParticleSystem.MinMaxGradient(c0, c1);
            main.gravityModifier = gravity / 9.81f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.maxParticles = 200;
            var em = ps.emission; em.rateOverTime = 0f; em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
            var sh = ps.shape; sh.shapeType = up ? ParticleSystemShapeType.Cone : ParticleSystemShapeType.Circle; sh.radius = 0.05f;
            if (up) { sh.angle = 50f; go.transform.rotation = Quaternion.Euler(-90f, 0f, 0f); }
            var rot = ps.rotationOverLifetime; rot.enabled = spin > 0f; rot.z = new ParticleSystem.MinMaxCurve(-spin * Mathf.Deg2Rad, spin * Mathf.Deg2Rad);
            var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.2f), new Keyframe(0.15f, 1f), new Keyframe(1f, 0f)));
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient(); g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.55f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var r = go.GetComponent<ParticleSystemRenderer>(); r.sortingOrder = Order + 2; r.sharedMaterial = SpriteMaterial(sprite);
            ps.Play();
            Destroy(go, life + 0.5f);
        }

        void SwirlParticles(Vector3 center, float radius, Color a, Color b)
        {
            var go = new GameObject("Swirl"); go.transform.SetParent(transform, false); go.transform.position = center;
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main; main.playOnAwake = false; main.loop = false; main.duration = 0.8f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1f); main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(radius * 0.05f, radius * 0.11f);
            main.startColor = new ParticleSystem.MinMaxGradient(a, b); main.simulationSpace = ParticleSystemSimulationSpace.World; main.maxParticles = 150;
            var em = ps.emission; em.rateOverTime = 140f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = radius * 0.9f; sh.radiusThickness = 0.15f;
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.Local;
            vel.orbitalZ = new ParticleSystem.MinMaxCurve(5f); vel.radial = new ParticleSystem.MinMaxCurve(-radius * 0.9f);
            var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.2f, 1f), new Keyframe(1f, 0f)));
            var r = go.GetComponent<ParticleSystemRenderer>(); r.sortingOrder = Order + 1; r.sharedMaterial = SpriteMaterial(_star);
            ps.Play();
            Destroy(go, 2.2f);
        }

        readonly Dictionary<Sprite, Material> _mats = new Dictionary<Sprite, Material>();
        Material SpriteMaterial(Sprite s)
        {
            if (!_mats.TryGetValue(s, out Material m)) { m = new Material(_spriteMat) { mainTexture = s.texture }; _mats[s] = m; }
            return m;
        }

        // ------------------------------------------------------------------ Tiện ích
        SpriteRenderer Spawn(Sprite s, Vector3 p, Color col, float size, int order)
        {
            var go = new GameObject("Fx"); go.transform.SetParent(transform, false); go.transform.position = p;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s; sr.color = col; sr.sortingOrder = order; sr.sharedMaterial = _spriteMat;
            go.transform.localScale = Vector3.one * size;   // sprite sinh ra có cỡ 1 đơn vị; icon booster có cỡ riêng, nơi gọi tự chia
            return sr;
        }

        static IEnumerator Anim(float dur, Action<float> step)
        {
            for (float t = 0f; t < dur; t += Time.deltaTime) { step(Mathf.Clamp01(t / dur)); yield return null; }
            step(1f);
        }

        static float EaseOutBack(float x) { const float c1 = 1.70158f, c3 = c1 + 1f; x = Mathf.Clamp01(x); return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f); }

        static Sprite MakeSprite(int size, Func<float, float, float, float> alpha)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float fx = (x + 0.5f) / size * 2f - 1f, fy = (y + 0.5f) / size * 2f - 1f;
                    byte a = (byte)(Mathf.Clamp01(alpha(fx, fy, Mathf.Sqrt(fx * fx + fy * fy))) * 255f);
                    px[y * size + x] = new Color32(255, 255, 255, a);
                }
            tex.SetPixels32(px); tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);   // 1 đơn vị thế giới
        }
    }
}
