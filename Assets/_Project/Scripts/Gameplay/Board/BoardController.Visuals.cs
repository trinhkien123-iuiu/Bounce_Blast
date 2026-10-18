using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    // Một phần của BoardController (partial) – xem BoardController.cs.
    /// <summary>Hình ảnh khối: mesh gạch/9-slice, nhãn số HP, căn kích thước visual, gỡ vật lý của prefab gốc.</summary>
    public partial class BoardController
    {
        bool _breakTuned;
        const int MaxBigBreakBursts = 16;

        /// <summary>
        /// Mảnh vỡ gạch: prefab gốc chỉ 5 khối lập phương bay chậm 0.4s (và vật liệu cố định "Blue" bỏ qua màu tô).
        /// Bắn tung toé: 16–22 mảnh mọi hướng, nhanh, có trọng lực, xoay, nhỏ dần; vật liệu = vật liệu của viên gạch vỡ.
        /// </summary>
        internal void EnsureBreakTune()
        {
            if (_breakTuned || _config.cubeBreakVfx == null) return;
            _breakTuned = true;
            // Kiểu "thạch": giọt tròn mềm (mesh cầu), ít & chậm, trọng lực nhẹ, phồng nhẹ rồi teo dần – không vỡ vụn như bê tông
            Mesh blob = Resources.GetBuiltinResource<Mesh>("New-Sphere.fbx");
            Vfx.Tune(_config.cubeBreakVfx, ps =>
            {
                var main = ps.main;
                main.duration = 0.6f;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.7f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(1.8f, 4.2f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.22f, 0.42f); // ×cỡ ô
                main.gravityModifier = 0.9f;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.startRotation3D = false;
                main.startRotation = 0f;
                var em = ps.emission;
                em.rateOverTime = 0f;
                em.SetBursts(new[] { new ParticleSystem.Burst(0f, 8, 12) });
                var sh = ps.shape;
                sh.enabled = true;
                sh.shapeType = ParticleSystemShapeType.Sphere;
                sh.radius = 0.2f;
                var rot = ps.rotationOverLifetime;
                rot.enabled = false;
                var vel = ps.limitVelocityOverLifetime;
                vel.enabled = true;
                vel.limit = 3.5f;
                vel.dampen = 0.15f; // chậm dần, mềm như thạch
                var sz = ps.sizeOverLifetime;
                sz.enabled = true;
                sz.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                    new Keyframe(0f, 0.55f), new Keyframe(0.18f, 1.05f), new Keyframe(0.35f, 0.92f), new Keyframe(1f, 0f)));
                var pr = ps.GetComponent<ParticleSystemRenderer>();
                if (pr != null && blob != null) { pr.renderMode = ParticleSystemRenderMode.Mesh; pr.mesh = blob; }
            });
        }

        const int ShadeBuckets = 4;
        static readonly Dictionary<(Material, int), Material> ShadeMats = new Dictionary<(Material, int), Material>();

        static Material ShadeVariant(Material baseMat, Vector2 range, int bucket)
        {
            if (!baseMat.HasProperty("_CubeShadeOffset")) return baseMat;
            if (ShadeMats.TryGetValue((baseMat, bucket), out Material m) && m != null) return m;
            m = new Material(baseMat) { name = baseMat.name + " shade" + bucket };
            m.SetFloat("_CubeShadeOffset", Mathf.Lerp(range.x, range.y, (bucket + 0.5f) / ShadeBuckets));
            ShadeMats[(baseMat, bucket)] = m;
            return m;
        }

        /// <summary>Prefab gốc còn "3D Text/Text (TMP)": component TMP đã bị rip mất, chỉ còn MeshRenderer rỗng → xoá.</summary>
        public static void RemoveStrayText(Transform root)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t != null && t.name == "3D Text") { Destroy(t.gameObject); }
        }

        internal Transform CreateVisual(Transform parent, BoardItemData item, Vector2 size, float forcedDepth = -1f)
        {
            GameObject v = Instantiate(_config.cubePrefab, parent);
            v.name = "Visual";
            StripPhysics(v);
            foreach (TextMeshPro t in v.GetComponentsInChildren<TextMeshPro>(true)) Destroy(t.gameObject);
            RemoveStrayText(v.transform);

            Renderer rend = v.GetComponentInChildren<MeshRenderer>(true);
            BlockKind kind = BlockKinds.Of(item.Kind);
            Material mat = kind.Material(_config, _colors, item);
            if (rend != null && mat != null) rend.sharedMaterial = mat;

            // Vân sáng/tối ngẫu nhiên cho từng cube nhỏ như game gốc (Colors_v3: MinOffset..MaxOffset).
            // Tối ưu: shader rip không có biến thể instancing → MaterialPropertyBlock làm mỗi cube 1 draw call riêng.
            // Thay bằng vài material dùng chung theo (màu, mức vân) → cube cùng material được dynamic batching gộp lại.
            if (rend != null && kind.CubeLook && mat != null)
            {
                ColorLibrary.Entry e = _colors.Get(item.ColorCode);
                Vector2 range = e != null ? e.shadeOffset : Vector2.zero;
                rend.sharedMaterial = ShadeVariant(mat, range, UnityEngine.Random.Range(0, ShadeBuckets));
            }

            // Game gốc nhìn bàn từ trên xuống: lật model để mặt trên (+Y) hướng về camera (-Z).
            // Mesh CubeNew có sẵn nghiêng 10° nên vẫn lộ một chút mặt trước như trong video.
            // Mặt trên (+Y) của mesh cube hướng thẳng về camera (camera nghiêng 20° → lật -70°).
            // Mesh CubeNew có sẵn nghiêng 10° nên vẫn lộ chút mặt trước như game gốc.
            Transform model = v.transform.Find("Model");
            if (model != null) model.localRotation = kind.CubeLook ? Quaternion.Euler(-ViewSpace.LegacyPitch, 0f, 0f) : Quaternion.identity;

            // Khối nhiều ô (CombinedCube/hòm/tường lớn): mesh gốc CombinedCubic được sinh lúc chạy bởi script đã bị
            // strip → dùng mesh cube 1 ô nhưng giãn kiểu 9-slice (giữ nguyên phần vát), tránh vát bị kéo thành bát giác.
            // Bù tỉ lệ mesh gốc pCube35 (chiều cao Y = 0.873 bề rộng X → cần nhân extents.x / extents.y = 1.145).
            float nx = size.x / _cell, ny = size.y / _cell;
            if (nx > 1.01f || ny > 1.01f)
            {
                MeshFilter mf = v.GetComponentInChildren<MeshFilter>(true);
                if (mf != null && mf.sharedMesh != null && mf.sharedMesh.isReadable)
                {
                    Bounds b = mf.sharedMesh.bounds;
                    float aspectY = b.extents.x / Mathf.Max(b.extents.y, 1e-4f);
                    mf.sharedMesh = SlicedCubeMesh(mf.sharedMesh, nx, ny * aspectY);
                }

                if (model != null) model.localRotation = Quaternion.identity; // không xoay: mesh được xiên ở Spawn (ShearForView)

                Transform cubeNew = model != null ? model.Find("CubeNew") : v.transform.Find("Model/CubeNew");
                if (cubeNew != null)
                {
                    cubeNew.localRotation = Quaternion.identity;
                    cubeNew.localScale = new Vector3(1.05f, 1.05f, 1.05f);
                }
            }


            // Viên đơn (1x1): co giãn ĐỀU như prefab (không ép từng trục) → giữ đúng tỉ lệ & pháp tuyến mặt vát của mesh gốc.
            // Khối nhiều ô (nx > 1 hoặc ny > 1): mesh đã được giãn 9-slice chuẩn theo tỉ lệ (nx, ny), scale ĐỒNG ĐỀU
            // theo bề ngang width = size.x để không bị méo trục Z khi Model xoay -20°, giữ mép 2 đầu chuẩn từng ô.
            if (kind.CubeLook && nx <= 1.01f && ny <= 1.01f)
            {
                if (_config.cubeCornerRadius > 0f)
                {
                    MeshFilter round = v.GetComponentInChildren<MeshFilter>(true);
                    if (round != null) round.sharedMesh = RoundedCubeMesh.Get(_config.cubeCornerRadius, _config.cubeEdgeRound, _config.cubeBodyDepth);
                }
                FitVisualUniform(v.transform, size.x * _config.cubeOverlap);
                return v.transform;
            }
            FitVisualUniform(v.transform, size.x);
            return v.transform;
        }

        readonly Dictionary<Vector2Int, Mesh> _slicedMeshes = new Dictionary<Vector2Int, Mesh>();

        /// <summary>
        /// Giãn mesh cube (pCube35 đứng thẳng: mặt trước -Z rộng theo x, cao theo y, sâu theo z) thành khối nx × ny ô:
        /// đỉnh trong vùng vát (20% ngoài mỗi cạnh) giữ khoảng cách tới mép, phần giữa giãn tuyến tính.
        /// </summary>
        Mesh SlicedCubeMesh(Mesh src, float nx, float ny)
        {
            var key = new Vector2Int(Mathf.RoundToInt(nx * 100f), Mathf.RoundToInt(ny * 100f));
            if (_slicedMeshes.TryGetValue(key, out Mesh cached) && cached != null) return cached;
            Bounds b = src.bounds;
            Vector3[] vs = src.vertices;
            for (int i = 0; i < vs.Length; i++)
            {
                vs[i].x = Slice(vs[i].x, b.center.x, b.extents.x, nx);
                vs[i].y = Slice(vs[i].y, b.center.y, b.extents.y, ny); // mặt trước nằm trong mặt phẳng X-Y của mesh
            }
            var m = Instantiate(src);
            m.name = src.name + $" {nx:0.##}x{ny:0.##}";
            m.vertices = vs;
            // Mesh gốc ít đỉnh: pháp tuyến góc mặt trước được làm mượt chung với mặt vát → giãn ra thì cả mặt trước bị tô như
            // mặt cong (tối, nhạt). Dựng thẳng pháp tuyến các đỉnh thuộc mặt trước (+Y), GIỮ pháp tuyến mặt cạnh/vát → mặt trước
            // tươi đúng màu, vẫn có dải mặt trên & viền 3D như game gốc.
            FlattenFrontFace(m);
            m.RecalculateBounds();
            _slicedMeshes[key] = m;
            return m;
        }

        static float Slice(float c, float center, float half, float n)
        {
            float d = c - center, band = half * 0.4f, inner = half - band, target = half * n;
            float a = Mathf.Abs(d);
            float r = a <= inner ? a * (target - band) / Mathf.Max(inner, 1e-4f) : target - (half - a);
            return center + Mathf.Sign(d) * r;
        }

        internal TextMeshPro CreateLabel(Transform parent, BoardItemData item, Vector2 size)
        {
            if (_font == null) return null;
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 0f, -Mathf.Min(size.x, size.y) * 0.5f - 0.05f);
            var tmp = go.AddComponent<TextMeshPro>();
            tmp.font = _font;
            if (_fontMaterial != null) tmp.fontSharedMaterial = _fontMaterial;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = false;
            tmp.color = Color.white;
            tmp.rectTransform.sizeDelta = size;
            float minSide = Mathf.Min(size.x, size.y);
            tmp.fontSize = Mathf.Clamp(minSide * 5.5f, 1.6f, 9f);
            tmp.sortingOrder = 5;
            // viền tối để số vẫn đọc được trên khối trắng / vàng
            if (_labelMaterial == null && tmp.fontSharedMaterial != null)
            {
                _labelMaterial = new Material(tmp.fontSharedMaterial);
                _labelMaterial.SetFloat(ShaderUtilities.ID_FaceDilate, 0.18f);                 // chữ đậm hơn
                _labelMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.2f);
                _labelMaterial.SetFloat(ShaderUtilities.ID_OutlineSoftness, 0f);                // viền sắc, không nhoè
                _labelMaterial.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0.1f, 0.1f, 0.14f, 1f));
                _labelMaterial.EnableKeyword(ShaderUtilities.Keyword_Outline);
            }
            if (_labelMaterial != null) tmp.fontSharedMaterial = _labelMaterial;
            return tmp;
        }

        /// <summary>Scale/căn giữa visual sao cho bounds của renderer khớp đúng kích thước mong muốn.</summary>
        public static void FitVisual(Transform v, Vector3 size)
        {
            v.localScale = Vector3.one;
            v.localPosition = Vector3.zero;
            Renderer[] rs = v.GetComponentsInChildren<Renderer>(true);
            if (rs.Length == 0) return;
            Bounds b = default;
            bool has = false;
            foreach (Renderer r in rs)
            {
                if (r is ParticleSystemRenderer || r is TrailRenderer || !r.enabled || !r.gameObject.activeInHierarchy) continue;
                if (r.GetComponent<TextMeshPro>() != null) continue;
                if (!has) { b = r.bounds; has = true; } else b.Encapsulate(r.bounds);
            }
            if (!has) return;
            Vector3 bs = b.size;
            var scale = new Vector3(size.x / Mathf.Max(bs.x, 1e-4f), size.y / Mathf.Max(bs.y, 1e-4f), size.z / Mathf.Max(bs.z, 1e-4f));
            Vector3 offset = v.position - b.center;
            v.localScale = scale;
            v.localPosition = new Vector3(offset.x * scale.x, offset.y * scale.y, offset.z * scale.z);
        }

        /// <summary>Scale ĐỒNG ĐỀU sao cho bề ngang bounds = width (giữ đúng tỉ lệ mesh đã xoay, VD jelly) và căn giữa.</summary>
        public static void FitVisualUniform(Transform v, float width)
        {
            v.localScale = Vector3.one;
            v.localPosition = Vector3.zero;
            Bounds b = default;
            bool has = false;
            foreach (Renderer r in v.GetComponentsInChildren<Renderer>(true))
            {
                if (r is ParticleSystemRenderer || r is TrailRenderer || !r.enabled || !r.gameObject.activeInHierarchy) continue;
                if (r.GetComponent<TextMeshPro>() != null) continue;
                if (!has) { b = r.bounds; has = true; } else b.Encapsulate(r.bounds);
            }
            if (!has) return;
            float s = width / Mathf.Max(b.size.x, 1e-4f);
            Vector3 offset = v.position - b.center;
            v.localScale = Vector3.one * s;
            v.localPosition = offset * s;
        }

        public static void StripPhysics(GameObject go)
        {
            bool playing = Application.isPlaying;
            foreach (Collider2D c in go.GetComponentsInChildren<Collider2D>(true)) { if (playing) Destroy(c); else DestroyImmediate(c); }
            foreach (Rigidbody2D rb in go.GetComponentsInChildren<Rigidbody2D>(true)) { if (playing) Destroy(rb); else DestroyImmediate(rb); }
            foreach (Collider c in go.GetComponentsInChildren<Collider>(true)) { if (playing) Destroy(c); else DestroyImmediate(c); }
            foreach (Rigidbody rb in go.GetComponentsInChildren<Rigidbody>(true)) { if (playing) Destroy(rb); else DestroyImmediate(rb); }
            foreach (Animator a in go.GetComponentsInChildren<Animator>(true)) { if (playing) Destroy(a); else DestroyImmediate(a); }
        }
    }
}
