using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>Văng mảnh vỡ có sẵn trong prefab gốc</summary>
    public static class DebrisFx
    {
        /// <summary>Xử lý văng mảnh vỡ ra xa tâm</summary>
        public static void Fling(Transform t, Vector3 center, float power = 1f)
        {
            t.gameObject.SetActive(true);
            t.SetParent(Vfx.Root, true);
            var rb = t.GetComponent<Rigidbody>();
            if (rb == null) rb = t.gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.useGravity = true;
            Vector3 away = t.position - center; away.z = 0f;
            rb.velocity = new Vector3(Mathf.Sign(away.x + 1e-3f) * Random.Range(0.6f, 2.2f), Random.Range(0.5f, 3f), Random.Range(-2.5f, -0.5f)) * power;
            rb.angularVelocity = Random.insideUnitSphere * 9f;
            Object.Destroy(t.gameObject, Random.Range(0.9f, 1.4f));
        }

        /// <summary>Văng mọi mảnh "_cell" nằm dưới root</summary>
        public static void FlingCells(Transform root, Vector3 center, float power = 1f)
        {
            if (root == null) return;
            var cells = new System.Collections.Generic.List<Transform>();
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t != root && t.name.Contains("_cell") && t.GetComponent<MeshFilter>() != null) cells.Add(t);
            foreach (Transform t in cells) Fling(t, center, power);
        }
    }
}
