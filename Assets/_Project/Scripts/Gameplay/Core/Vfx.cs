using System.Collections.Generic;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>
    /// Sinh particle gốc (Block Break Particle, CFXR...) tại vị trí, nhuộm màu.
    /// Tối ưu: dùng POOL theo prefab (trước đây Instantiate + GetComponentsInChildren + Destroy cho MỖI khối vỡ
    /// → hàng trăm lần/giây, rác bộ nhớ & giật). Mỗi prefab giữ tối đa MaxLive hiệu ứng sống cùng lúc;
    /// vượt quá thì tái dùng cái cũ nhất (mắt không phân biệt được khi khối vỡ dồn dập).
    /// </summary>
    public static class Vfx
    {
        /// <summary>Object cha chứa mọi hiệu ứng (để Hierarchy lúc chạy gọn gàng). GameManager gán.</summary>
        public static Transform Root;

        const int MaxLive = 24;

        class Item
        {
            public GameObject go;
            public ParticleSystem[] systems;
            public float life;
            public float until;
        }

        class Pool
        {
            public readonly Queue<Item> free = new Queue<Item>();
            public readonly LinkedList<Item> live = new LinkedList<Item>();
        }

        static readonly Dictionary<GameObject, Pool> Pools = new Dictionary<GameObject, Pool>();
        static VfxTicker _ticker;

        static readonly Dictionary<GameObject, System.Action<ParticleSystem>> Tunes = new Dictionary<GameObject, System.Action<ParticleSystem>>();

        /// <summary>Chỉnh thông số particle của 1 prefab (áp khi tạo instance trong pool).</summary>
        public static void Tune(GameObject prefab, System.Action<ParticleSystem> tune)
        {
            if (prefab == null) return;
            Tunes[prefab] = tune;
            if (Pools.TryGetValue(prefab, out Pool pool)) Pools.Remove(prefab); // instance cũ chưa áp → tạo lại
        }

        /// <param name="material">vật liệu cho hạt dạng mesh (VD mảnh gạch cùng màu khối vỡ); null = giữ của prefab</param>
        public static GameObject Spawn(GameObject prefab, Vector3 position, Color tint, float size = 1f, Material material = null)
        {
            if (prefab == null) return null;
            EnsureTicker();
            if (!Pools.TryGetValue(prefab, out Pool pool)) { pool = new Pool(); Pools[prefab] = pool; }

            Item it;
            if (pool.free.Count > 0) it = pool.free.Dequeue();
            else if (pool.live.Count >= MaxLive) { it = pool.live.First.Value; pool.live.RemoveFirst(); }
            else it = Create(prefab);
            if (it.go == null) it = Create(prefab); // bị huỷ khi đổi scene

            Transform t = it.go.transform;
            t.SetPositionAndRotation(new Vector3(position.x, position.y, -1.2f), prefab.transform.rotation);
            t.localScale = prefab.transform.localScale * Mathf.Clamp(size, 0.3f, 3f);
            it.go.SetActive(true);
            var grad = new ParticleSystem.MinMaxGradient(tint, Color.Lerp(tint, Color.white, 0.35f));
            foreach (ParticleSystem ps in it.systems)
            {
                if (material != null) { var pr = ps.GetComponent<ParticleSystemRenderer>(); if (pr != null) pr.sharedMaterial = material; }
                ParticleSystem.MainModule main = ps.main;
                main.startColor = grad;
                ps.Clear(true);
                ps.Play(true);
            }
            it.until = Time.time + it.life;
            pool.live.AddLast(it);
            return it.go;
        }

        static Item Create(GameObject prefab)
        {
            GameObject go = Object.Instantiate(prefab, Root);
            var it = new Item { go = go, systems = go.GetComponentsInChildren<ParticleSystem>(true), life = 0.5f };
            Tunes.TryGetValue(prefab, out System.Action<ParticleSystem> tune);
            foreach (ParticleSystem ps in it.systems)
            {
                tune?.Invoke(ps);
                ParticleSystem.MainModule main = ps.main;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                main.playOnAwake = false;
                it.life = Mathf.Max(it.life, main.duration + main.startLifetime.constantMax);
            }
            it.life = Mathf.Min(it.life, 4f);
            return it;
        }

        static void EnsureTicker()
        {
            if (_ticker != null) return;
            var go = new GameObject("VfxTicker");
            _ticker = go.AddComponent<VfxTicker>();
        }

        /// <summary>Trả hiệu ứng đã chạy xong về pool.</summary>
        internal static void Tick()
        {
            float now = Time.time;
            foreach (Pool pool in Pools.Values)
                while (pool.live.Count > 0 && pool.live.First.Value.until <= now)
                {
                    Item it = pool.live.First.Value;
                    pool.live.RemoveFirst();
                    if (it.go == null) continue;
                    it.go.SetActive(false);
                    pool.free.Enqueue(it);
                }
        }

        /// <summary>Đổi scene / dọn: bỏ toàn bộ pool (object đã bị huỷ cùng scene).</summary>
        internal static void Reset() => Pools.Clear();
    }

    class VfxTicker : MonoBehaviour
    {
        void Update() => Vfx.Tick();
        void OnDestroy() => Vfx.Reset();
    }
}
