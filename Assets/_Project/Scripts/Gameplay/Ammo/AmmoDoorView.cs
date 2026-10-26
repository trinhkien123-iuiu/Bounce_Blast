using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>Cửa kính 2 cánh trên miệng hố</summary>
    public class AmmoDoorView
    {
        const float CoverMeshSize = 1.6314f;

        const float CoverDropY = 0.05f;

        Transform _root;
        SkinnedMeshRenderer _smr;
        float _blend, _blendTarget;

        public AmmoDoorView(Transform parent, GameConfig config, bool closed)
        {
            _root = new GameObject("DoorCover").transform;
            _root.SetParent(parent, false);
            GameObject pf = config != null ? config.ammoCoverPrefab : null;
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

        /// <summary>Xử lý đặt đích đóng/mở của cửa kính</summary>
        public void SetClosed(bool closed) => _blendTarget = closed ? 0f : 90f;

        /// <summary>Đặt cửa lên miệng hố: tâm world, kích thước miệng hố</summary>
        public void Place(Vector3 worldCenter, Vector2 size, Quaternion plate)
        {
            if (_root == null || _smr == null) return;
            _root.SetPositionAndRotation(worldCenter, plate);
            _root.localScale = Vector3.one;
            Transform c = _smr.transform;
            c.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            float k = size.x / CoverMeshSize * 0.98f;
            c.localScale = new Vector3(k * 1.03f, k, k * 1.035f * size.y / size.x);
            c.localPosition = Vector3.zero;
            Apply();
            Bounds b = _smr.bounds;
            Vector3 d = Quaternion.Inverse(plate) * (worldCenter - b.center);
            c.position += plate * new Vector3(d.x, d.y - size.y * CoverDropY, 0f);
        }

        /// <summary>Trượt cánh cửa tới trạng thái đích</summary>
        public void Tick(float dt)
        {
            if (_blend != _blendTarget) _blend = Mathf.MoveTowards(_blend, _blendTarget, dt * 360f);
        }

        /// <summary>Xử lý đẩy trọng số blend shape lên mesh cửa</summary>
        public void Apply()
        {
            if (_smr == null) return;
            _smr.SetBlendShapeWeight(0, Mathf.Clamp(_blend, 0f, 100f));
            _smr.enabled = _blend < 89f;
        }

        /// <summary>Hộp rời khay: bỏ cửa</summary>
        public void Remove()
        {
            if (_root != null) Object.Destroy(_root.gameObject);
            _root = null;
            _smr = null;
        }
    }
}
