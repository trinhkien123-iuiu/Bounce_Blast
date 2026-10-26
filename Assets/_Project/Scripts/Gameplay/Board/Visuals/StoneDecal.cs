using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>Mặt đá cố định: các đường rạn như đất khô phủ kín mặt cube be</summary>
    public class StoneDecal : MonoBehaviour
    {
        const int Size = 128;
        const int Variants = 6;

        static Sprite[] _sprites;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetCache() => _sprites = null;

        /// <summary>Dựng lớp rạn phủ mặt trước viên đá, kích thước theo ô</summary>
        public static void Create(Transform visual, Vector3 frontCenter, float size, int layer)
        {
            EnsureSprites();
            var go = new GameObject("StoneDecal");
            go.layer = layer;
            go.transform.SetParent(visual, false);
            go.transform.position = frontCenter;
            go.transform.rotation = Quaternion.identity;
            Vector3 ps = visual.lossyScale;
            go.transform.localScale = new Vector3(size / Mathf.Max(ps.x, 1e-4f), size / Mathf.Max(ps.y, 1e-4f), 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 2;
            sr.sprite = _sprites[Random.Range(0, Variants)];
        }

        static void EnsureSprites()
        {
            if (_sprites != null) return;
            _sprites = new Sprite[Variants];
            for (int v = 0; v < Variants; v++)
                _sprites[v] = Sprite.Create(Paint(v * 37 + 5), new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size);
        }

        /// <summary>Xử lý vẽ các đường rạn dạng Voronoi mảnh lên texture trong suốt</summary>
        static Texture2D Paint(int seed)
        {
            var rnd = new System.Random(seed);
            const int n = 3;
            var pts = new Vector2[n];
            for (int i = 0; i < n; i++) pts[i] = new Vector2((float)rnd.NextDouble() * Size, (float)rnd.NextDouble() * Size);
            var px = new Color[Size * Size];
            Color dark = new Color(0.4f, 0.37f, 0.36f, 1f);
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    float d1 = float.MaxValue, d2 = float.MaxValue;
                    int i1 = 0, i2 = 0;
                    for (int i = 0; i < n; i++)
                    {
                        float d = (new Vector2(x, y) - pts[i]).sqrMagnitude;
                        if (d < d1) { d2 = d1; i2 = i1; d1 = d; i1 = i; } else if (d < d2) { d2 = d; i2 = i; }
                    }
                    float edge = (d2 - d1) / (2f * Mathf.Max((pts[i1] - pts[i2]).magnitude, 1f));   // khoảng cách thật tới đường rạn
                    float a = Mathf.Clamp01(1f - edge / 2.2f);
                    px[y * Size + x] = a > 0f ? new Color(dark.r, dark.g, dark.b, a * 0.75f) : Color.clear;
                }
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.HideAndDontSave };
            tex.SetPixels(px);
            tex.Apply(false, true);
            return tex;
        }
    }
}
