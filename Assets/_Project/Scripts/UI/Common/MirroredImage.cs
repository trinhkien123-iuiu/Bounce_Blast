using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

/// <summary>Khôi phục component "MirroredImage" của game gốc</summary>
[AddComponentMenu("UI/Mirrored Image")]
public class MirroredImage : Image
{
    [SerializeField] private bool m_MirrorHorizontally;
    [SerializeField] private bool m_MirrorVertically;
    [SerializeField] private bool m_UseSlicing;

    public bool MirrorHorizontally { get => m_MirrorHorizontally; set { m_MirrorHorizontally = value; SetVerticesDirty(); } }
    public bool MirrorVertically { get => m_MirrorVertically; set { m_MirrorVertically = value; SetVerticesDirty(); } }
    public bool UseSlicing { get => m_UseSlicing; set { m_UseSlicing = value; SetVerticesDirty(); } }

    Sprite ActiveSprite => overrideSprite != null ? overrideSprite : sprite;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        Sprite sp = ActiveSprite;
        if (sp == null || (!m_MirrorHorizontally && !m_MirrorVertically) || type == Type.Filled || type == Type.Tiled)
        {
            base.OnPopulateMesh(vh);
            return;
        }

        vh.Clear();
        Rect rect = GetPixelAdjustedRect();
        if (preserveAspect && type == Type.Simple && !m_UseSlicing) rect = FitToAspect(rect, sp);
        Color32 c = color;

        if (m_UseSlicing || type == Type.Sliced) PopulateSliced(vh, sp, rect, c);
        else PopulateSimple(vh, sp, rect, c);
    }

    void ForEachPart(Rect rect, System.Action<Rect, bool, bool> draw)
    {
        float hw = m_MirrorHorizontally ? rect.width * 0.5f : rect.width;
        float hh = m_MirrorVertically ? rect.height * 0.5f : rect.height;
        draw(new Rect(rect.x, rect.y, hw, hh), false, false);
        if (m_MirrorHorizontally) draw(new Rect(rect.x + hw, rect.y, hw, hh), true, false);
        if (m_MirrorVertically)
        {
            draw(new Rect(rect.x, rect.y + hh, hw, hh), false, true);
            if (m_MirrorHorizontally) draw(new Rect(rect.x + hw, rect.y + hh, hw, hh), true, true);
        }
    }

    /// <summary>Lùi UV mép nối (cạnh phải khi lật ngang, cạnh trên khi lật dọc) vào nửa texel để không ăn pixel của sprite kế…</summary>
    Vector4 SeamInset(Vector4 uv, Sprite sp)
    {
        Texture t = sp.texture;
        if (t == null) return uv;
        if (m_MirrorHorizontally) uv.z -= 0.5f / t.width;
        if (m_MirrorVertically) uv.w -= 0.5f / t.height;
        return uv;
    }

    void PopulateSimple(VertexHelper vh, Sprite sp, Rect rect, Color32 color32)
    {
        Vector4 uv = SeamInset(DataUtility.GetOuterUV(sp), sp);
        ForEachPart(rect, (r, fx, fy) =>
        {
            float u0 = fx ? uv.z : uv.x, u1 = fx ? uv.x : uv.z;
            float v0 = fy ? uv.w : uv.y, v1 = fy ? uv.y : uv.w;
            GenerateGrid(vh, new[] { r.xMin, r.xMax }, new[] { r.yMin, r.yMax }, new[] { u0, u1 }, new[] { v0, v1 }, color32);
        });
    }

    void PopulateSliced(VertexHelper vh, Sprite sp, Rect rect, Color32 color32)
    {
        Vector4 outer = SeamInset(DataUtility.GetOuterUV(sp), sp);
        Vector4 inner = DataUtility.GetInnerUV(sp);
        Vector4 border = sp.border / Mathf.Max(0.0001f, multipliedPixelsPerUnit);

        float bl = border.x, bb = border.y;
        float br = m_MirrorHorizontally ? 0f : border.z;
        float bt = m_MirrorVertically ? 0f : border.w;

        ForEachPart(rect, (r, fx, fy) =>
        {
            Vector4 b = ClampBorder(new Vector4(bl, bb, br, bt), r.width, r.height);
            float[] lx = { 0f, b.x, r.width - b.z, r.width };
            float[] ly = { 0f, b.y, r.height - b.w, r.height };
            float[] us = { outer.x, bl > 0f ? inner.x : outer.x, br > 0f ? inner.z : outer.z, outer.z };
            float[] vs = { outer.y, bb > 0f ? inner.y : outer.y, bt > 0f ? inner.w : outer.w, outer.w };

            if (fx) { lx = new[] { r.width - lx[3], r.width - lx[2], r.width - lx[1], r.width - lx[0] }; us = new[] { us[3], us[2], us[1], us[0] }; }
            if (fy) { ly = new[] { r.height - ly[3], r.height - ly[2], r.height - ly[1], r.height - ly[0] }; vs = new[] { vs[3], vs[2], vs[1], vs[0] }; }

            float[] xs = { r.x + lx[0], r.x + lx[1], r.x + lx[2], r.x + lx[3] };
            float[] ys = { r.y + ly[0], r.y + ly[1], r.y + ly[2], r.y + ly[3] };
            GenerateGrid(vh, xs, ys, us, vs, color32, !fillCenter);
        });
    }

    Rect FitToAspect(Rect rect, Sprite sp)
    {
        float spriteW = sp.rect.width * (m_MirrorHorizontally ? 2f : 1f);
        float spriteH = sp.rect.height * (m_MirrorVertically ? 2f : 1f);
        float ratio = spriteW / Mathf.Max(1f, spriteH);
        float rectRatio = rect.width / Mathf.Max(0.0001f, rect.height);
        if (ratio > rectRatio)
        {
            float h = rect.width / ratio;
            rect.y += (rect.height - h) * rectTransform.pivot.y;
            rect.height = h;
        }
        else
        {
            float w = rect.height * ratio;
            rect.x += (rect.width - w) * rectTransform.pivot.x;
            rect.width = w;
        }
        return rect;
    }

    static Vector4 ClampBorder(Vector4 border, float width, float height)
    {
        float sx = border.x + border.z > width && border.x + border.z > 0f ? width / (border.x + border.z) : 1f;
        float sy = border.y + border.w > height && border.y + border.w > 0f ? height / (border.y + border.w) : 1f;
        return new Vector4(border.x * sx, border.y * sy, border.z * sx, border.w * sy);
    }

    static void GenerateGrid(VertexHelper vh, float[] xPos, float[] yPos, float[] uvX, float[] uvY, Color32 color32, bool skipCenter = false)
    {
        int nx = xPos.Length, ny = yPos.Length;
        for (int x = 0; x < nx - 1; x++)
        {
            for (int y = 0; y < ny - 1; y++)
            {
                if (skipCenter && nx == 4 && ny == 4 && x == 1 && y == 1) continue;
                if (xPos[x + 1] - xPos[x] <= 0f || yPos[y + 1] - yPos[y] <= 0f) continue;
                int start = vh.currentVertCount;
                vh.AddVert(new Vector3(xPos[x], yPos[y]), color32, new Vector2(uvX[x], uvY[y]));
                vh.AddVert(new Vector3(xPos[x], yPos[y + 1]), color32, new Vector2(uvX[x], uvY[y + 1]));
                vh.AddVert(new Vector3(xPos[x + 1], yPos[y + 1]), color32, new Vector2(uvX[x + 1], uvY[y + 1]));
                vh.AddVert(new Vector3(xPos[x + 1], yPos[y]), color32, new Vector2(uvX[x + 1], uvY[y]));
                vh.AddTriangle(start, start + 1, start + 2);
                vh.AddTriangle(start + 2, start + 3, start);
            }
        }
    }

    /// <summary>Xử lý đặt kích thước theo ảnh gốc (nhân đôi nếu lật gương)</summary>
    public override void SetNativeSize()
    {
        Sprite sp = ActiveSprite;
        if (sp == null) { base.SetNativeSize(); return; }
        float w = sp.rect.width / pixelsPerUnit * (m_MirrorHorizontally ? 2f : 1f);
        float h = sp.rect.height / pixelsPerUnit * (m_MirrorVertically ? 2f : 1f);
        rectTransform.anchorMax = rectTransform.anchorMin;
        rectTransform.sizeDelta = new Vector2(w, h);
        SetAllDirty();
    }
}
