using TMPro;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>Hiển thị TUNNEL TRÊN BÀN (/btu-X) bằng mảnh gốc ModularPrimitiveBoardTunnel (Left/Mid/Right × Front/Back, đặt theo ô CSV)
    /// + tấm số của CombinedCubicBoardTunnel; số = cube còn trong hàng đợi (NumCubesInTunnel của game gốc)</summary>
    public class BoardTunnelView : MonoBehaviour
    {
        const float BreakSeconds = 0.25f;
        /// <summary>Tấm số: cỡ so với một ô CSV và độ nhô ra trước mặt tunnel (đo từ video: tấm số ≈ 0.7 ô, nằm giữa khe)</summary>
        const float BadgeScaleUnits = 0.9f;
        const float BadgeDepthUnits = 0.8f;
        /// <summary>Số nổi lên trước tấm số (theo ô CSV)</summary>
        const float LabelLiftUnits = 0.12f;

        Transform _badge;
        TextMeshPro _label;
        float _break = -1f;

        /// <summary>Dựng tunnel dưới parent</summary>
        public static BoardTunnelView Build(Transform parent, BoardItemData item, float cell, int mult, GameConfig cfg, BlockVisualFactory visuals, int layer)
        {
            var root = new GameObject("Visual").transform;
            root.SetParent(parent, false);
            var view = root.gameObject.AddComponent<BoardTunnelView>();
            float unit = cell * Mathf.Max(1, mult);
            int cols = Mathf.Max(1, item.Cells.width / Mathf.Max(1, mult));
            int rows = Mathf.Max(1, item.Cells.height / Mathf.Max(1, mult));

            if (cfg.boardTunnelPrefab != null)
            {
                for (int j = 0; j < rows; j++)
                    for (int i = 0; i < cols; i++)
                    {
                        // mọi ô dùng mảnh Mid: mảnh Left/Right có nắp đầu bo tròn + viền dày, bản gốc trông phẳng (hai đầu bị khối kề che)
                        string key = "Mid";
                        var cellT = new GameObject("Cell").transform;
                        cellT.SetParent(root, false);
                        cellT.localPosition = new Vector3((i + 0.5f - cols * 0.5f) * unit, (j + 0.5f - rows * 0.5f) * unit, 0f);
                        cellT.localRotation = Quaternion.Euler(cfg.boardTunnelEuler);
                        cellT.localScale = Vector3.one * unit;
                        // chỉ mảnh Front (khung + khe tối); mảnh Back nhô lên che mất hàng cube phía trên miệng nên bỏ
                        foreach (string side in new[] { "Front" })
                        {
                            Transform src = cfg.boardTunnelPrefab.transform.Find("Board Tunnel " + key + " " + side);
                            if (src == null) continue;
                            GameObject inst = Instantiate(src.gameObject, cellT, false);
                            inst.SetActive(true);
                            // offset gốc (0, 0.22, −0.7) dời mảnh xuống 0.7 ô theo trục dọc của prefab: bỏ phần dọc để khung nằm đúng giữa hàng miệng
                            // lùi tấm ra sau các khối kề (trục y của prefab = hướng ra trước camera) để cube hai bên che viền, cube trồi lên không đè lên tấm
                            inst.transform.localPosition = new Vector3(src.localPosition.x, src.localPosition.y - cfg.boardTunnelDepthShift, 0f);
                            inst.transform.localRotation = src.localRotation;
                            inst.transform.localScale = src.localScale;
                            ModelUtil.StripPhysics(inst);
                            foreach (ParticleSystem ps in inst.GetComponentsInChildren<ParticleSystem>(true)) Destroy(ps.gameObject);
                            foreach (Transform t in inst.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
                        }
                    }
            }

            view.BuildBadge(root, parent, item, unit, cfg, visuals, layer);
            return view;
        }

        /// <summary>Tấm số ở giữa miệng tunnel + nhãn số cube còn lại</summary>
        void BuildBadge(Transform root, Transform blockRoot, BoardItemData item, float unit, GameConfig cfg, BlockVisualFactory visuals, int layer)
        {
            int total = 0;
            foreach (TunnelBatch b in item.Queue) total += b.Cubes;

            // tấm số xám bo tròn (mesh "Number" gốc nằm phẳng theo XZ → xoay −90° quanh X cho mặt hướng camera)
            Transform src = cfg.boardTunnelBadgePrefab != null ? cfg.boardTunnelBadgePrefab.transform.Find("Model/3D Text/Number") : null;
            MeshFilter smf = src != null ? src.GetComponent<MeshFilter>() : null;
            MeshRenderer smr = src != null ? src.GetComponent<MeshRenderer>() : null;
            if (smf != null && smf.sharedMesh != null && smr != null)
            {
                var go = new GameObject("Badge") { layer = layer };
                _badge = go.transform;
                _badge.SetParent(root, false);
                _badge.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                _badge.localScale = Vector3.one * unit * BadgeScaleUnits;
                _badge.localPosition = new Vector3(0f, 0f, -BadgeDepthUnits * unit);
                go.AddComponent<MeshFilter>().sharedMesh = smf.sharedMesh;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = smr.sharedMaterial;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
            }

            if (visuals == null) return;
            float d = unit * 0.7f;
            _label = visuals.CreateLabel(blockRoot, item, new Vector2(d, d));
            if (_label == null) return;
            _label.fontSize = Mathf.Clamp(d * 7f, 1.2f, 10f);
            _label.SetText("{0}", total);
            _label.transform.localPosition = new Vector3(0f, 0f, -(BadgeDepthUnits + LabelLiftUnits) * unit);
        }

        /// <summary>Cập nhật số cube còn trong hàng đợi</summary>
        public void SetCount(int count)
        {
            if (_label != null) _label.SetText("{0}", count);
        }

        /// <summary>Tunnel hết việc: thu nhỏ rồi biến mất; tách khỏi khối để chạy nốt</summary>
        public void Break()
        {
            if (_label != null) _label.gameObject.SetActive(false);
            transform.SetParent(Vfx.Root, true);
            _break = 0f;
        }

        void Update()
        {
            if (_break < 0f) return;
            _break += Time.deltaTime;
            float k = Mathf.Clamp01(_break / BreakSeconds);
            transform.localScale = Vector3.Lerp(Vector3.one, new Vector3(1f, 0.01f, 1f), k * k);
            if (k >= 1f) Destroy(gameObject);
        }
    }
}
