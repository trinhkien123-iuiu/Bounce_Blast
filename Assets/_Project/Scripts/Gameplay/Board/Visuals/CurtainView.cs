using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>Hiển thị RÈM (/cur-hp-id-màu) bằng asset gốc: vải ghép từ mảnh ModularPrimitiveCurtain, xích + con lắc từ CombinedCubicCurtain.
    /// Mọi thứ dựng theo ô CSV (Multiplier×Multiplier ô mịn), đúng tỉ lệ game gốc</summary>
    public class CurtainView : MonoBehaviour
    {
        /// <summary>BlockerCurtainHandler.ScaleOverrideOfPrimitives của game gốc: mảnh vải rộng ~0.91 ô nên phóng 1.15 để các mảnh chồng khít, không hở khe</summary>
        const float PieceOverlapScale = 1.15f;
        const float LongChainRows = 12f;
        const float MediumChainRows = 7f;
        const float LiftSeconds = 0.35f;
        /// <summary>Thời gian rèm rút hết (khối bên dưới chờ hết thời gian này mới hiện số máu)</summary>
        public const float LiftDuration = LiftSeconds;
        /// <summary>Con lắc đứng yên bấy lâu sau khi hết máu rồi mới biến mất (video gốc ~0.3s)</summary>
        const float MedalSeconds = 0.3f;
        /// <summary>Đường kính con lắc ≈ 1.25 ô CSV (đo từ video gốc)</summary>
        const float MedalDiameterUnits = 1.25f;
        /// <summary>Nhãn số nổi lên trước mặt con lắc</summary>
        const float LabelLiftUnits = 0.3f;

        Transform _pivot;
        Transform _chain;
        Animator _anim;
        Transform _textAnchor;
        TextMeshPro _label;
        readonly List<SkinnedMeshRenderer> _clothRenderers = new List<SkinnedMeshRenderer>();
        float _lift = -1f;

        struct ClothCell { public Transform t; public Vector3 basePos; public int row; }
        readonly List<ClothCell> _clothCells = new List<ClothCell>();
        int _clothRows;
        float _clothPitch;

        /// <summary>Dựng rèm dưới parent; viền con lắc mang màu của rèm</summary>
        public static CurtainView Build(Transform parent, BoardItemData item, float cell, int mult, GameConfig cfg, BlockVisualFactory visuals, Material medalSource, int layer)
        {
            var root = new GameObject("Visual").transform;
            root.SetParent(parent, false);
            var view = root.gameObject.AddComponent<CurtainView>();
            float unit = cell * Mathf.Max(1, mult);
            int cols = Mathf.Max(1, item.Cells.width / Mathf.Max(1, mult));
            int rows = Mathf.Max(1, item.Cells.height / Mathf.Max(1, mult));

            // vải: gốc xoay quanh mép trên để kéo rèm lên
            view._pivot = new GameObject("Pivot").transform;
            view._pivot.SetParent(root, false);
            view._pivot.localPosition = new Vector3(0f, rows * unit * 0.5f, 0f);
            var cloth = new GameObject("Cloth").transform;
            cloth.SetParent(view._pivot, false);
            cloth.localPosition = new Vector3(0f, -rows * unit * 0.5f, 0f);
            view.BuildCloth(cloth, item, unit, mult, cols, rows, cfg.curtainModularPrefab, layer);
            view.FitCloth(root, cloth, unit, cols, rows);

            float chainUnit = unit * Mathf.Max(0.1f, cfg.curtainChainScale);
            if (cfg.curtainChainPrefab != null)
                view.BuildChain(root, rows, unit, chainUnit, cfg.curtainChainOffsetZ, cfg.curtainChainPrefab, medalSource, layer);
            view.BuildLabel(root, item, chainUnit, visuals);
            return view;
        }

        /// <summary>Mảnh Top/Bot gốc cao ~1.33 ô (Mid ~1.07) nên tràn sang hàng bên cạnh: ép theo chiều dọc về đúng một ô, căn giữa ô</summary>
        static void FitCellVertical(Transform cloth, Transform placed, float unit)
        {
            float minY = float.MaxValue, maxY = float.MinValue;
            foreach (SkinnedMeshRenderer smr in placed.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (smr.sharedMesh == null) continue;
                Bounds b = smr.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 corner = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    float y = cloth.InverseTransformPoint(smr.transform.TransformPoint(corner)).y;
                    minY = Mathf.Min(minY, y);
                    maxY = Mathf.Max(maxY, y);
                }
            }
            if (maxY <= minY) return;
            float y0 = placed.localPosition.y;
            float f = unit * PieceOverlapScale / (maxY - minY);
            float centre = (minY + maxY) * 0.5f;
            Vector3 s = placed.localScale;
            placed.localScale = new Vector3(s.x, s.y * f, s.z);
            placed.localPosition = new Vector3(placed.localPosition.x, y0 - f * (centre - y0), placed.localPosition.z);
        }

        /// <summary>Mảnh vải gốc tràn ra ngoài ô (cao ~1.3 ô, lệch xuống): co lại cho khít đúng vùng các ô rèm che</summary>
        void FitCloth(Transform root, Transform cloth, float unit, int cols, int rows)
        {
            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, 0f), max = new Vector3(float.MinValue, float.MinValue, 0f);
            foreach (SkinnedMeshRenderer smr in _clothRenderers)
            {
                if (smr.sharedMesh == null) continue;
                Bounds b = smr.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 corner = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    Vector3 p = root.InverseTransformPoint(smr.transform.TransformPoint(corner));
                    min = Vector3.Min(min, p);
                    max = Vector3.Max(max, p);
                }
            }
            if (max.x <= min.x || max.y <= min.y) return;
            float sx = cols * unit / (max.x - min.x), sy = rows * unit / (max.y - min.y);
            Vector2 c = new Vector2((min.x + max.x) * 0.5f, (min.y + max.y) * 0.5f);
            cloth.localScale = new Vector3(sx, sy, 1f);
            cloth.localPosition = new Vector3(-sx * c.x, -rows * unit * 0.5f - sy * c.y, 0f);
        }

        /// <summary>Ghép mảnh Top/Mid/Bot theo tập hàng xóm của ô CSV (tên mảnh gốc: CurtainMid-T-B-L…)</summary>
        void BuildCloth(Transform cloth, BoardItemData item, float unit, int mult, int cols, int rows, GameObject modular, int layer)
        {
            _clothRows = rows;
            _clothPitch = unit;
            if (modular == null) return;
            Transform container = modular.transform.Find("CurtainContainer");
            if (container == null) container = modular.transform;
            Dictionary<string, Transform> pieces = ModularPrimitiveBuilder.Pieces(container);
            var cells = new HashSet<Vector2Int>();
            foreach (Vector2Int f in ModularPrimitiveBuilder.Cells(item)) cells.Add(new Vector2Int(f.x / mult, f.y / mult));
            var size = new RectInt(0, 0, cols, rows);
            foreach (Vector2Int c in cells)
            {
                // thân vải dùng mảnh Mid (user chốt); chỉ hàng trên cùng / dưới cùng giữ mảnh Top / Bot để còn 2 viền vàng
                string key = ModularPrimitiveBuilder.NeighbourKey(cells, c, false);
                if (key.Contains("T") && key.Contains("B"))
                {
                    bool l = key.Contains("L"), r = key.Contains("R");
                    key = Join("B", l && r ? "L-R" : l ? "L" : r ? "R" : "", "T");
                }
                if (!Find(pieces, key, out Transform piece)) continue;
                Transform placed = ModularPrimitiveBuilder.Place(cloth, piece, c, size, unit, container.localScale, layer);
                // prefab gốc gắn sẵn "Block Break Particle" (vật liệu gốc đã mất trong bản rip → hạt xanh mặc định): bỏ, hiệu ứng vỡ do BlockBreakFx lo
                foreach (ParticleSystem ps in placed.GetComponentsInChildren<ParticleSystem>(true)) Destroy(ps.gameObject);
                placed.localScale *= PieceOverlapScale;
                FitCellVertical(cloth, placed, unit);
                _clothCells.Add(new ClothCell { t = placed, basePos = placed.localPosition, row = c.y });
                foreach (SkinnedMeshRenderer smr in placed.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    smr.updateWhenOffscreen = true;
                    _clothRenderers.Add(smr);
                }
            }
            UnifyClothMaterial();
        }

        /// <summary>Mảnh Top gốc dùng thêm MainDark còn Mid chỉ có Main nên lệch tông: thống nhất mọi mảnh dùng Main (viền vàng giữ nguyên)</summary>
        void UnifyClothMaterial()
        {
            Material main = null;
            foreach (SkinnedMeshRenderer smr in _clothRenderers)
                foreach (Material m in smr.sharedMaterials)
                    if (m != null && m.name == "Main") { main = m; break; }
            if (main == null) return;
            foreach (SkinnedMeshRenderer smr in _clothRenderers)
            {
                Material[] mats = smr.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                    if (mats[i] != null && mats[i].name == "MainDark") mats[i] = main;
                smr.sharedMaterials = mats;
            }
        }

        /// <summary>Tìm mảnh theo khoá; hàng 1 ô cao (không có T/B) thì dùng mảnh thân cùng cột trái/phải</summary>
        static bool Find(Dictionary<string, Transform> pieces, string key, out Transform piece)
        {
            if (pieces.TryGetValue(key, out piece)) return true;
            string lr = key.Contains("L") && key.Contains("R") ? "L-R" : key.Contains("L") ? "L" : key.Contains("R") ? "R" : "";
            string[] tries = { Join("B", lr, "T"), Join("B", lr, ""), Join("", lr, "T") };
            foreach (string t in tries) if (pieces.TryGetValue(t, out piece)) return true;
            foreach (Transform any in pieces.Values) { piece = any; return true; }
            return false;
        }

        static string Join(string b, string lr, string t)
        {
            var l = new List<string>();
            if (b != "") l.Add(b);
            if (lr != "") l.AddRange(lr.Split('-'));
            if (t != "") l.Add(t);
            l.Sort(System.StringComparer.Ordinal);
            return string.Join("-", l);
        }

        /// <summary>Xích treo từ mép trên giữa rèm; dài/vừa/ngắn theo số hàng CSV (ngưỡng 12 và 7 như game gốc)</summary>
        void BuildChain(Transform root, int rows, float unit, float chainUnit, float offsetZ, GameObject prefab, Material medalSource, int layer)
        {
            string name = rows >= LongChainRows ? "CurtainChain3x" : rows >= MediumChainRows ? "CurtainChain2x" : "CurtainChain";
            Transform src = prefab.transform.Find("Chain Container/" + name);
            if (src == null) return;

            GameObject inst = Instantiate(src.gameObject);
            inst.SetActive(true);
            inst.name = name;
            foreach (Collider2D c in inst.GetComponentsInChildren<Collider2D>(true)) Destroy(c);
            foreach (Collider c in inst.GetComponentsInChildren<Collider>(true)) Destroy(c);
            foreach (Rigidbody2D rb in inst.GetComponentsInChildren<Rigidbody2D>(true)) Destroy(rb);
            foreach (Transform t in inst.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;

            // trục xích trong prefab gốc nằm theo Z; đo thật từ xương gốc → xương cuối rồi xoay cho thẳng xuống
            var anchor = new GameObject("Chain").transform;
            anchor.SetParent(root, false);
            inst.transform.SetParent(anchor, false);
            anchor.localScale = Vector3.one * chainUnit;
            Transform top = FindDeep(inst.transform, "Bone");
            Transform end = FindDeep(inst.transform, "FinalMedalPos");
            _textAnchor = HideTexts(inst.transform);
            if (top != null && end != null)
            {
                Vector3 axis = end.position - top.position;
                Vector3 down = root.TransformDirection(Vector3.down);
                anchor.rotation = Quaternion.FromToRotation(axis, down) * anchor.rotation;
                // quay quanh trục xích để mặt số máu hướng về camera (-Z)
                if (_textAnchor != null)
                {
                    Vector3 face = -_textAnchor.forward;
                    Vector3 toCam = root.TransformDirection(Vector3.back);
                    Vector3 f = Vector3.ProjectOnPlane(face, down), t = Vector3.ProjectOnPlane(toCam, down);
                    if (f.sqrMagnitude > 1e-6f && t.sqrMagnitude > 1e-6f)
                        anchor.rotation = Quaternion.AngleAxis(Vector3.SignedAngle(f, t, down), down) * anchor.rotation;
                }
                Vector3 attach = root.TransformPoint(new Vector3(0f, rows * unit * 0.5f, -(1.1f + offsetZ) * unit));
                anchor.position += attach - top.position;
            }
            _chain = anchor;
            _anim = inst.GetComponent<Animator>();
            TintMedal(inst, medalSource);
        }

        /// <summary>Nhãn số máu riêng (outline, nổi trước con lắc) – chữ trên prefab gốc dễ bị chìm sau mặt con lắc nên chỉ lấy vị trí</summary>
        void BuildLabel(Transform root, BoardItemData item, float unit, BlockVisualFactory visuals)
        {
            if (_textAnchor == null || visuals == null) return;
            float d = MedalDiameterUnits * unit;
            _label = visuals.CreateLabel(root, item, new Vector2(d, d));
            if (_label == null) return;
            _label.fontSize = Mathf.Clamp(d * 5f, 0.28f, 8.6f);
            _label.SetText("{0}", item.Hp);
            LateUpdate();
        }

        /// <summary>Đổi màu viền con lắc theo màu rèm (material Pentagon gốc xám trắng)</summary>
        static void TintMedal(GameObject inst, Material medalSource)
        {
            if (medalSource == null) return;
            // mặt con lắc (Pentagon) lấy đúng màu rèm; viền (Gold 2) cùng tông nhưng đậm hơn; mắt xích giữ vàng gốc
            foreach (Renderer rend in inst.GetComponentsInChildren<Renderer>(true))
            {
                if (!rend.sharedMaterial || !rend.GetComponent<SkinnedMeshRenderer>() || !HasPentagon(rend)) continue;
                Material[] mats = rend.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    bool face = mats[i].name.StartsWith("Pentagon");
                    if (!face && !mats[i].name.StartsWith("Gold")) continue;
                    var m = new Material(mats[i]);
                    foreach (string p in new[] { "_BaseColor", "_HColor", "_SColor" })
                    {
                        if (!m.HasProperty(p) || !medalSource.HasProperty(p)) continue;
                        Color c = medalSource.GetColor(p);
                        m.SetColor(p, face ? c : Color.Lerp(c, Color.black, 0.3f));
                    }
                    mats[i] = m;
                }
                rend.sharedMaterials = mats;
            }
        }

        static bool HasPentagon(Renderer rend)
        {
            foreach (Material m in rend.sharedMaterials) if (m != null && m.name.StartsWith("Pentagon")) return true;
            return false;
        }

        static Transform FindDeep(Transform root, string name)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }

        /// <summary>Tắt chữ có sẵn của prefab (giữ lại vị trí chữ đầu tiên làm tâm con lắc)</summary>
        static Transform HideTexts(Transform root)
        {
            // prefab rip đã mất script TextMeshPro → tìm theo tên node ("3D Text (1)" / "Text (TMP)"), chỉ còn MeshRenderer rỗng
            Transform anchor = null;
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (!t.name.StartsWith("3D Text") && !t.name.StartsWith("Text (TMP)")) continue;
                if (anchor == null && t.name.StartsWith("3D Text")) anchor = t;
                foreach (Renderer r in t.GetComponents<Renderer>()) r.enabled = false;
            }
            return anchor;
        }

        /// <summary>Tâm con lắc trong thế giới (đích bay của mảnh gạch)</summary>
        public Vector3 MedalPosition =>
            _textAnchor != null ? _textAnchor.position + transform.TransformDirection(Vector3.back) * (LabelLiftUnits * _textAnchor.lossyScale.x) : transform.position;

        /// <summary>Hiện số hiện tại trên con lắc</summary>
        public void SetHp(int hp)
        {
            if (_label != null) _label.SetText("{0}", hp);
        }

        /// <summary>Số giảm: cập nhật số, con lắc đung đưa</summary>
        public void Hit(int hp)
        {
            SetHp(hp);
            if (_anim != null) _anim.Play("Hit", 0, 0f);
        }

        /// <summary>Số về 0: con lắc rơi, vải kéo lên mép trên rồi biến mất (khối bên dưới đã lộ ra)</summary>
        public void Break()
        {
            transform.SetParent(Vfx.Root, true);
            // KHÔNG chơi clip Break của prefab: clip ghi đè vị trí xương theo toạ độ gốc prefab nên con lắc bị giật về đó.
            // Video gốc: con lắc đứng yên tại chỗ, số ẩn, rồi biến mất khi rèm gần rút xong → chỉ đóng băng animator
            if (_anim != null) _anim.enabled = false;
            if (_label != null) _label.gameObject.SetActive(false);
            _lift = 0f;
        }

        void LateUpdate()
        {
            if (_label == null || _textAnchor == null) return;
            Vector3 toCam = transform.TransformDirection(Vector3.back);
            _label.transform.position = _textAnchor.position + toCam * (LabelLiftUnits * _textAnchor.lossyScale.x);
            _label.transform.rotation = transform.rotation;
        }

        void Update()
        {
            if (_lift < 0f) return;
            _lift += Time.deltaTime;
            if (_chain != null && _lift >= MedalSeconds) { Destroy(_chain.gameObject); _chain = null; }
            float k = Mathf.Clamp01(_lift / LiftSeconds);
            float e = k * k * (3f - 2f * k);
            // KHÔNG scale vải (làm méo nếp): mỗi hàng giữ nguyên hình, trượt lên mép trên rồi xếp chồng (hàng dưới nằm sau hàng trên) → mép dưới rút lên liền một mạch
            foreach (ClothCell c in _clothCells)
            {
                if (c.t == null) continue;
                int below = _clothRows - 1 - c.row;
                c.t.localPosition = c.basePos + new Vector3(0f, below * _clothPitch * e, below * _clothPitch * 0.02f);
            }
            if (k >= 1f) Destroy(gameObject);
        }
    }
}
