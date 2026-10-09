using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sao lấp lánh quanh đống xu ở bảng thắng (video coin_panel gốc: vài ngôi sao trắng 4 cánh liên tục hiện–tắt
/// ngẫu nhiên trong vùng quanh xu). Texture sao gốc: _Ripped/Texture2D/sparkle3.png.
/// </summary>
public class UiSparkles : MonoBehaviour
{
    class Star { public RectTransform rt; public RawImage img; public float age, life, size; }

    Texture _tex;
    Vector2 _area;
    float _acc;
    readonly List<Star> _stars = new List<Star>();
    // Pool + Canvas lồng riêng (xem UiConfetti): không Instantiate/Destroy mỗi sao, không làm bảng thắng dựng lại lưới liên tục
    readonly Stack<Star> _pool = new Stack<Star>();
    RectTransform _layer;
    const float Rate = 9f;          // sao/giây
    const int MaxStars = 10;

    public static UiSparkles Play(RectTransform host, Texture tex, Vector2 area)
    {
        var s = host.GetComponent<UiSparkles>();
        if (s == null) s = host.gameObject.AddComponent<UiSparkles>();
        s._tex = tex;
        s._area = area;
        s.EnsureLayer();
        return s;
    }

    void EnsureLayer()
    {
        if (_layer != null) return;
        var go = new GameObject("SparkleLayer", typeof(RectTransform));
        go.transform.SetParent(transform, false);
        _layer = (RectTransform)go.transform;
        _layer.anchorMin = _layer.anchorMax = new Vector2(0.5f, 0.5f);
        _layer.sizeDelta = Vector2.zero;
        Canvas parent = GetComponentInParent<Canvas>();
        var cv = go.AddComponent<Canvas>();
        cv.overrideSorting = true;
        cv.sortingLayerID = parent != null ? parent.sortingLayerID : 0;
        cv.sortingOrder = (parent != null ? parent.sortingOrder : 0) + 1;
    }

    void Update()
    {
        if (_tex == null) return;
        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
        _acc += Rate * dt;
        while (_acc >= 1f && _stars.Count < MaxStars) { _acc -= 1f; Spawn(); }
        if (_acc > 1f) _acc = 1f;
        for (int i = _stars.Count - 1; i >= 0; i--)
        {
            Star s = _stars[i];
            s.age += dt;
            float t = s.age / s.life;
            if (t >= 1f) { s.rt.gameObject.SetActive(false); _pool.Push(s); _stars.RemoveAt(i); continue; }
            float k = Mathf.Sin(t * Mathf.PI);                 // hiện rồi tắt
            s.rt.localScale = Vector3.one * k;
            s.rt.localEulerAngles = new Vector3(0f, 0f, t * 90f);
            Color c = s.img.color; c.a = k; s.img.color = c;
        }
    }

    void Spawn()
    {
        Star st;
        if (_pool.Count > 0)
        {
            st = _pool.Pop();
            st.rt.gameObject.SetActive(true);
        }
        else
        {
            var go = new GameObject("sparkle", typeof(RectTransform));
            go.transform.SetParent(_layer != null ? _layer : transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            var img = go.AddComponent<RawImage>();
            img.texture = _tex;
            img.raycastTarget = false;
            st = new Star { rt = rt, img = img };
        }
        st.rt.anchoredPosition = new Vector2(Random.Range(-0.5f, 0.5f) * _area.x, Random.Range(-0.5f, 0.5f) * _area.y);
        float size = Random.Range(60f, 140f); // video gốc: sao ~30–50px trên màn 480px ≈ 60–140 đơn vị canvas 1242
        st.rt.sizeDelta = new Vector2(size, size);
        st.rt.localScale = Vector3.zero;
        st.age = 0f; st.life = Random.Range(0.45f, 0.8f); st.size = size;
        _stars.Add(st);
    }

    void OnDisable()
    {
        foreach (Star s in _stars) if (s.rt != null) Destroy(s.rt.gameObject);
        foreach (Star s in _pool) if (s.rt != null) Destroy(s.rt.gameObject);
        _stars.Clear();
        _pool.Clear();
    }
}
