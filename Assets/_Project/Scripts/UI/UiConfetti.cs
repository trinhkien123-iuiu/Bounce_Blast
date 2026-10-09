using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pháo giấy trong UI – dựng lại plugin UI-particle của game gốc (component đã bị strip khi build, chỉ còn
/// RectTransform). Thông số lấy từ dữ liệu gốc trong ManagerScene:
///  - Burst (LogoArea/Confetti/ConfettiBurst): nổ 100 mảnh, texture confetti 3×3 (frame ngẫu nhiên),
///    size 40–65, sống 1s, tốc độ 2–10 (giảm dần về 0), trọng lực 9.8, xoay ngẫu nhiên + quay tới 1200°/s.
///  - Rain (UI Particle ConfettiFullscreen): trễ 1s, rate 300/s trong 1s, rải trên mép trên toàn màn,
///    mảnh chữ nhật 10×30 màu cầu vồng, sống 2–4s, rơi 1–7 + trọng lực, lật 360–720°/s.
/// Đơn vị tốc độ/trọng lực của plugin nhân 100 ra pixel canvas.
/// </summary>
public class UiConfetti : MonoBehaviour
{
    public enum Mode { Burst, Rain }

    class P
    {
        public RectTransform rt;
        public Graphic g;
        public Vector2 vel;
        public float speed0, life, age, rot, spin, flip, flipSpeed, size;
        public Vector2 dir;
    }

    static readonly Color[] Rainbow =
    {
        new Color(1f, 0f, 0.168f), new Color(1f, 0.446f, 0f), new Color(1f, 0.923f, 0f), new Color(0.422f, 1f, 0f),
        new Color(0f, 1f, 0.941f), new Color(0f, 0.777f, 1f), new Color(0f, 0.476f, 1f)
    };

    // Pool: trước đây mỗi mảnh là 1 GameObject mới + Destroy → hàng nghìn lần cấp phát/giây (GC giật) và làm Canvas cha
    // dựng lại lưới mỗi khung. Giờ mảnh được tái dùng, và cả đống confetti nằm trong Canvas lồng riêng (overrideSorting)
    // nên bảng thắng không bị dựng lại theo từng mảnh giấy.
    readonly Stack<P> _pool = new Stack<P>();
    Mode _mode;
    Texture _sheet;
    float _delay, _emitLeft, _emitAcc;
    readonly List<P> _live = new List<P>();
    RectTransform _rt;

    public static UiConfetti Play(Transform host, Mode mode, Texture sheet, float delay = 0f)
    {
        var c = host.GetComponent<UiConfetti>();
        if (c == null) c = host.gameObject.AddComponent<UiConfetti>();
        c._mode = mode;
        c._sheet = sheet;
        c._delay = delay;
        c._emitLeft = mode == Mode.Burst ? 0f : 1f;
        c._rt = host as RectTransform;
        c.EnsureOwnCanvas();
        // con cũ do plugin gốc sinh (đã mất script) → ẩn
        foreach (Transform child in host) if (child.name != "confetti") child.gameObject.SetActive(false);
        host.gameObject.SetActive(true);
        if (mode == Mode.Burst && delay <= 0f) c.EmitBurst();
        return c;
    }

    void EnsureOwnCanvas()
    {
        if (GetComponent<Canvas>() != null) return;
        Canvas parent = GetComponentInParent<Canvas>();
        var cv = gameObject.AddComponent<Canvas>();
        cv.overrideSorting = true;
        cv.sortingLayerID = parent != null ? parent.sortingLayerID : 0;
        cv.sortingOrder = (parent != null ? parent.sortingOrder : 0) + 2;
    }

    void EmitBurst()
    {
        for (int i = 0; i < 100; i++) Spawn(Vector2.zero, Random.insideUnitCircle.normalized);
    }

    void Update()
    {
        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f); // khung bị khựng không làm mảnh giấy nhảy cóc
        if (_delay > 0f)
        {
            _delay -= dt;
            if (_delay <= 0f && _mode == Mode.Burst) EmitBurst();
        }
        else if (_mode == Mode.Rain && _emitLeft > 0f)
        {
            _emitLeft -= dt;
            _emitAcc += 300f * dt;
            Rect r = _rt != null ? _rt.rect : new Rect(-500, -1000, 1000, 2000);
            while (_emitAcc >= 1f)
            {
                _emitAcc -= 1f;
                Spawn(new Vector2(Random.Range(r.xMin, r.xMax), r.yMax), Vector2.down);
            }
        }

        for (int i = _live.Count - 1; i >= 0; i--)
        {
            P p = _live[i];
            p.age += dt;
            float t = p.age / p.life;
            if (t >= 1f) { p.rt.gameObject.SetActive(false); _pool.Push(p); _live.RemoveAt(i); continue; }

            Vector2 v;
            float s;
            if (_mode == Mode.Burst)
            {
                float speedK = 1f - t;                       // speedOverLifetime 1 → 0
                v = p.dir * p.speed0 * speedK + Vector2.down * 980f * p.age * 0.35f;
                // sizeOverLifetime gốc: 0.76 → 0.98 (t 0.64) → 0
                s = t < 0.64f ? Mathf.Lerp(0.76f, 0.98f, t / 0.64f) : Mathf.Lerp(0.98f, 0f, (t - 0.64f) / 0.36f);
            }
            else
            {
                v = p.dir * p.speed0 * Mathf.Lerp(1f, 0.22f, t) + Vector2.down * 981f * p.age * 0.25f;
                s = 1f;
            }
            p.rt.anchoredPosition += v * dt;
            p.rot += p.spin * dt;
            p.flip += p.flipSpeed * dt;
            p.rt.localEulerAngles = new Vector3(0f, 0f, p.rot);
            float fx = _mode == Mode.Rain ? Mathf.Abs(Mathf.Cos(p.flip * Mathf.Deg2Rad)) : 1f;
            p.rt.localScale = new Vector3(s * Mathf.Max(0.15f, fx), s, 1f);
        }
    }

    void Spawn(Vector2 pos, Vector2 dir)
    {
        P p;
        if (_pool.Count > 0)
        {
            p = _pool.Pop();
            p.rt.gameObject.SetActive(true);
        }
        else
        {
            var go = new GameObject("confetti", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            p = new P { rt = rt };
            p.g = _mode == Mode.Burst ? (Graphic)go.AddComponent<RawImage>() : go.AddComponent<Image>();
            p.g.raycastTarget = false;
        }
        p.rt.anchoredPosition = pos;
        p.dir = dir; p.age = 0f; p.flip = 0f; p.flipSpeed = 0f;
        RectTransform r = p.rt;
        if (_mode == Mode.Burst)
        {
            var img = (RawImage)p.g;
            img.texture = _sheet;
            int f = Random.Range(0, 9);
            img.uvRect = new Rect((f % 3) / 3f, (f / 3) / 3f, 1f / 3f, 1f / 3f);
            float size = Random.Range(40f, 65f);
            r.sizeDelta = new Vector2(size, size);
            p.speed0 = Random.Range(2f, 10f) * 100f;
            p.life = 1f;
            p.rot = Random.Range(0f, 360f);
            p.spin = Random.Range(-1200f, 1200f);
        }
        else
        {
            var img = (Image)p.g;
            img.color = Rainbow[Random.Range(0, Rainbow.Length)];
            r.sizeDelta = new Vector2(10f, 30f) * 1.6f;
            p.speed0 = Random.Range(1f, 7f) * 100f;
            p.life = Random.Range(2f, 4f);
            p.rot = Random.Range(-90f, 90f);
            p.spin = Random.Range(-180f, 180f);
            p.flipSpeed = Random.Range(360f, 720f);
        }
        _live.Add(p);
    }

    void OnDisable()
    {
        foreach (P p in _live) if (p.rt != null) Destroy(p.rt.gameObject);
        foreach (P p in _pool) if (p.rt != null) Destroy(p.rt.gameObject);
        _live.Clear();
        _pool.Clear();
    }
}
