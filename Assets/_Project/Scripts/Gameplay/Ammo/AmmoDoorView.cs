using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>
    /// Cửa kính 2 cánh trên miệng hố (BlockerCover gốc). Dùng NGUYÊN node "Cover" của prefab AmmunationBlockerCover:
    /// SkinnedMeshRenderer có blend shape 0 (0 = đóng khít miệng hố, 90 = 2 cánh trượt vào 2 bên và biến mất), vật liệu Cover
    /// (vạch sáng chéo + khe giữa). Lớp này chỉ dựng, đặt vị trí/cỡ theo miệng hố và đẩy trọng số blend shape;
    /// trạng thái đóng/mở (luật) nằm ở <see cref="AmmoBoxData.DoorClosed"/>.
    /// </summary>
    public class AmmoDoorView
    {
        const float CoverMeshSize = 1.6314f; // bề rộng mesh Cover gốc ở scale 1 (đóng): bounds prefab 2.06 / scale node 1.2627

        const float CoverDropY = 0.05f;      // dời nắp xuống một chút (theo chiều cao hố): che hết khe hở ở mép dưới

        Transform _root;
        SkinnedMeshRenderer _smr;
        float _blend, _blendTarget;

        public AmmoDoorView(Transform parent, GameConfig config, bool closed)
        {
            _root = new GameObject("DoorCover").transform;
            _root.SetParent(parent, false);
            GameObject pf = config != null ? config.ammoCoverPrefab : null; // GameConfig.ammoCoverPrefab
            Transform src = pf != null ? pf.transform.Find("Cover") : null;
            if (src != null)
            {
                GameObject inst = Object.Instantiate(src.gameObject, _root, false);
                inst.name = "Cover";
                inst.SetActive(true);
                _smr = inst.GetComponent<SkinnedMeshRenderer>();
                if (_smr != null)
                {
                    _smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    _smr.updateWhenOffscreen = true;
                }
                int ammoLayer = LayerMask.NameToLayer("Ammunation");
                if (ammoLayer >= 0) foreach (Transform t in inst.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = ammoLayer;
            }
            _blend = _blendTarget = closed ? 0f : 90f;
        }

        public void SetClosed(bool closed) => _blendTarget = closed ? 0f : 90f;

        /// <summary>Đặt cửa lên miệng hố: tâm world, kích thước miệng hố (mesh gốc đã bo góc), hướng mặt khay (+Y lên màn hình, −Z về camera).</summary>
        public void Place(Vector3 worldCenter, Vector2 size, Quaternion plate)
        {
            if (_root == null || _smr == null) return;
            _root.SetPositionAndRotation(worldCenter, plate);
            _root.localScale = Vector3.one;
            Transform c = _smr.transform;
            c.localRotation = Quaternion.Euler(-90f, 0f, 0f);          // mesh gốc nằm trên mặt XZ → dựng về mặt XY của mặt khay
            float k = size.x / CoverMeshSize * 0.98f;                    // = số người dùng chỉnh tay (Lv75, ô ≈ 1.25: y ≈ 0.7556); 1.04 làm các nắp liền kề dính nhau
            c.localScale = new Vector3(k * 1.03f, k, k * 1.035f * size.y / size.x);   // tỉ lệ x:y:z = 1.03 : 1 : 1.035 đo từ cỡ người dùng chỉnh tay (0.778, 0.7556, 0.7823) ở Lv75; mesh gốc nằm mặt XZ: trục Z thành chiều dọc trên màn hình (miệng hố co dọc)
            c.localPosition = Vector3.zero;
            Apply();
            Bounds b = _smr.bounds;                                     // căn tâm hình thật vào tâm hố (trong mặt khay)
            Vector3 d = Quaternion.Inverse(plate) * (worldCenter - b.center);
            c.position += plate * new Vector3(d.x, d.y - size.y * CoverDropY, 0f);
        }

        /// <summary>Trượt cánh cửa tới trạng thái đích (gọi mỗi khung).</summary>
        public void Tick(float dt)
        {
            if (_blend != _blendTarget) _blend = Mathf.MoveTowards(_blend, _blendTarget, dt * 360f);
        }

        public void Apply()
        {
            if (_smr == null) return;
            _smr.SetBlendShapeWeight(0, Mathf.Clamp(_blend, 0f, 100f));
            _smr.enabled = _blend < 89f;
        }

        /// <summary>Hộp rời khay: bỏ cửa.</summary>
        public void Remove()
        {
            if (_root != null) Object.Destroy(_root.gameObject);
            _root = null;
            _smr = null;
        }
    }
}
