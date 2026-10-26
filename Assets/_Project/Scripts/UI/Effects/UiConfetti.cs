using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Pháo giấy trong UI – dựng lại plugin UI-particle của game gốc</summary>
public class UiConfetti : MonoBehaviour
{
    /// <summary>Dùng cho kiểu pháo giấy: nổ hoặc rơi</summary>
    public enum Mode { Burst, Rain }

    /// <summary>Dùng cho một mảnh pháo giấy</summary>
    class P
    {
        public RectTransform rt;
        public Graphic g;
        public float speed0, life, age, rot, spin, flip, flipSpeed;
        public Vector2 dir;
    }

    [SerializeField] Mode mode = Mode.Burst;
    [Tooltip("Burst: ảnh pháo giấy 3×3 khung (confetti_0.png)")] [SerializeField] Texture sheet;
    [Tooltip("Burst: số mảnh nổ")] [SerializeField] int burstCount = 100;
    [Tooltip("Rain: mảnh/giây")] [SerializeField] float rainRate = 300f;
    [Tooltip("Rain: rải trong bao lâu (giây)")] [SerializeField] float rainDuration = 1f;
    [Tooltip("Burst: cạnh mảnh (min, max)")] [SerializeField] Vector2 burstSize = new Vector2(40f, 65f);
    [Tooltip("Rain: cỡ mảnh chữ nhật")] [SerializeField] Vector2 rainSize = new Vector2(16f, 48f);
    [Tooltip("Tốc độ đầu (min, max) – đơn vị canvas/giây")] [SerializeField] Vector2 speed = new Vector2(200f, 1000f);
    [Tooltip("Thời gian sống (min, max) giây")] [SerializeField] Vector2 life = new Vector2(1f, 1f);
    [Tooltip("Trọng lực (đơn vị canvas/giây²)")] [SerializeField] float gravity = 343f;
    [Tooltip("Tốc độ quay (độ/giây, ±)")] [SerializeField] float spinMax = 1200f;
    [Tooltip("Rain: màu ngẫu nhiên của mảnh")] [SerializeField] Color[] colors =
    {
        new Color(1f, 0f, 0.168f), new Color(1f, 0.446f, 0f), new Color(1f, 0.923f, 0f), new Color(0.422f, 1f, 0f),
        new Color(0f, 1f, 0.941f), new Color(0f, 0.777f, 1f), new Color(0f, 0.476f, 1f)
    };

    readonly Stack<P> _pool = new Stack<P>();
    readonly List<P> _live = new List<P>();
    float _delay, _emitLeft, _emitAcc;
    bool _pendingBurst;

    /// <summary>Phát pháo sau delay giây</summary>
    public void Play(float delay = 0f)
    {
        gameObject.SetActive(true);
        _delay = delay;
        _pendingBurst = mode == Mode.Burst;
        _emitLeft = mode == Mode.Rain ? rainDuration : 0f;
        _emitAcc = 0f;
        if (_pendingBurst && delay <= 0f) EmitBurst();
    }

    void EmitBurst()
    {
        _pendingBurst = false;
        for (int i = 0; i < burstCount; i++) Spawn(Vector2.zero, Random.insideUnitCircle.normalized);
    }

    void Update()
    {
        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
        if (_delay > 0f)
        {
            _delay -= dt;
            if (_delay <= 0f && _pendingBurst) EmitBurst();
        }
        else if (mode == Mode.Rain && _emitLeft > 0f)
        {
            _emitLeft -= dt;
            _emitAcc += rainRate * dt;
            Rect r = ((RectTransform)transform).rect;
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
            if (mode == Mode.Burst)
            {
                v = p.dir * p.speed0 * (1f - t) + Vector2.down * gravity * p.age;
                s = t < 0.64f ? Mathf.Lerp(0.76f, 0.98f, t / 0.64f) : Mathf.Lerp(0.98f, 0f, (t - 0.64f) / 0.36f);
            }
            else
            {
                v = p.dir * p.speed0 * Mathf.Lerp(1f, 0.22f, t) + Vector2.down * gravity * p.age;
                s = 1f;
            }
            p.rt.anchoredPosition += v * dt;
            p.rot += p.spin * dt;
            p.flip += p.flipSpeed * dt;
            p.rt.localEulerAngles = new Vector3(0f, 0f, p.rot);
            float fx = mode == Mode.Rain ? Mathf.Abs(Mathf.Cos(p.flip * Mathf.Deg2Rad)) : 1f;
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
            go.hideFlags = HideFlags.DontSave;
            go.transform.SetParent(transform, false);
            go.layer = gameObject.layer;
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            p = new P { rt = rt };
            p.g = mode == Mode.Burst ? (Graphic)go.AddComponent<RawImage>() : go.AddComponent<Image>();
            p.g.raycastTarget = false;
        }
        p.rt.anchoredPosition = pos;
        p.dir = dir; p.age = 0f; p.flip = 0f; p.flipSpeed = 0f;
        p.speed0 = Random.Range(speed.x, speed.y);
        p.life = Random.Range(life.x, life.y);
        p.spin = Random.Range(-spinMax, spinMax);
        if (mode == Mode.Burst)
        {
            var img = (RawImage)p.g;
            img.texture = sheet;
            int f = Random.Range(0, 9);
            img.uvRect = new Rect((f % 3) / 3f, (f / 3) / 3f, 1f / 3f, 1f / 3f);
            float size = Random.Range(burstSize.x, burstSize.y);
            p.rt.sizeDelta = new Vector2(size, size);
            p.rot = Random.Range(0f, 360f);
        }
        else
        {
            var img = (Image)p.g;
            if (colors.Length > 0) img.color = colors[Random.Range(0, colors.Length)];
            p.rt.sizeDelta = rainSize;
            p.rot = Random.Range(-90f, 90f);
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
