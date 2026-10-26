using UnityEngine;
using UnityEngine.UI;

/// <summary>Sao lấp lánh quanh đống xu ở bảng thắng</summary>
public class UiSparkles : MonoBehaviour
{
    /// <summary>Dùng cho một ngôi sao lấp lánh</summary>
    class Star { public RawImage img; public float age, life; public bool live; }

    [Tooltip("Các ngôi sao (con của object này, lưu tắt sẵn). Chuột phải ▸ \"Tạo sao\" để sinh thêm theo số lượng dưới")] [SerializeField] RawImage[] stars = new RawImage[0];
    [Tooltip("Vùng rải sao quanh tâm (đơn vị canvas)")] [SerializeField] Vector2 area = new Vector2(520f, 380f);
    [Tooltip("Sao mới mỗi giây")] [SerializeField] float rate = 9f;
    [Tooltip("Cỡ sao (video gốc: ~30–50px trên màn 480px ≈ 60–140 đơn vị canvas 1242)")] [SerializeField] Vector2 sizeRange = new Vector2(60f, 140f);
    [Tooltip("Thời gian sống mỗi sao (giây)")] [SerializeField] Vector2 lifeRange = new Vector2(0.45f, 0.8f);
#if UNITY_EDITOR
    [Tooltip("Số sao tạo khi bấm \"Tạo sao\"")] [SerializeField] int createCount = 10;
    [Tooltip("Ảnh sao dùng khi bấm \"Tạo sao\" (sparkle3.png)")] [SerializeField] Texture createTexture;
#endif

    Star[] _state = new Star[0];
    float _acc;
    bool _running;

    /// <summary>Bắt đầu lấp lánh (gọi khi bảng thắng hiện)</summary>
    public void Play()
    {
        if (_state.Length != stars.Length)
        {
            _state = new Star[stars.Length];
            for (int i = 0; i < stars.Length; i++) _state[i] = new Star { img = stars[i] };
        }
        _acc = 0f;
        _running = true;
    }

    /// <summary>Xử lý dừng sao lấp lánh</summary>
    public void Stop()
    {
        _running = false;
        foreach (Star s in _state) { s.live = false; if (s.img != null) s.img.gameObject.SetActive(false); }
    }

    void OnDisable() => Stop();

    void Update()
    {
        if (!_running) return;
        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
        _acc += rate * dt;
        while (_acc >= 1f) { _acc -= 1f; if (!Spawn()) { _acc = 0f; break; } }
        foreach (Star s in _state)
        {
            if (!s.live || s.img == null) continue;
            s.age += dt;
            float t = s.age / s.life;
            if (t >= 1f) { s.live = false; s.img.gameObject.SetActive(false); continue; }
            float k = Mathf.Sin(t * Mathf.PI);
            RectTransform rt = s.img.rectTransform;
            rt.localScale = Vector3.one * k;
            rt.localEulerAngles = new Vector3(0f, 0f, t * 90f);
            Color c = s.img.color; c.a = k; s.img.color = c;
        }
    }

    bool Spawn()
    {
        foreach (Star s in _state)
        {
            if (s.live || s.img == null) continue;
            RectTransform rt = s.img.rectTransform;
            rt.anchoredPosition = new Vector2(Random.Range(-0.5f, 0.5f) * area.x, Random.Range(-0.5f, 0.5f) * area.y);
            float size = Random.Range(sizeRange.x, sizeRange.y);
            rt.sizeDelta = new Vector2(size, size);
            rt.localScale = Vector3.zero;
            s.age = 0f; s.life = Random.Range(lifeRange.x, lifeRange.y); s.live = true;
            s.img.gameObject.SetActive(true);
            return true;
        }
        return false;
    }

#if UNITY_EDITOR
    [ContextMenu("Tạo sao")]
    void CreateStars()
    {
        var list = new System.Collections.Generic.List<RawImage>(stars);
        for (int i = 0; i < createCount; i++)
        {
            var go = new GameObject("sparkle", typeof(RectTransform));
            UnityEditor.Undo.RegisterCreatedObjectUndo(go, "Tạo sao");
            go.transform.SetParent(transform, false);
            go.layer = gameObject.layer;
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = Vector2.one * sizeRange.y;
            var img = go.AddComponent<RawImage>();
            img.texture = createTexture;
            img.raycastTarget = false;
            go.SetActive(false);
            list.Add(img);
        }
        stars = list.ToArray();
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}
