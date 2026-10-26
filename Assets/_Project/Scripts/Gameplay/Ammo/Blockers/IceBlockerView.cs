using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>Hiển thị BĂNG trên hộp đạn</summary>
    public class IceBlockerView : MonoBehaviour
    {
        /// <summary>Hệ số phóng "Ice Model" của prefab</summary>
        const float SizeVsBox = 0.95f;

        static readonly string[] StageNames = { "IceCube", "IceCube2", "IceCube3", "IceCube4" };

        GameObject[] _stages;
        Transform[] _fractures;
        ParticleSystem _snow;
        int _maxHits;
        int _shown = -1;

        Vector3 _baseScale;

        /// <summary>Co giãn khối băng cho PHỦ KÍN toàn bộ hộp</summary>
        public void FitCover(Bounds target, GameConfig cfg, float cell)
        {
            float padX = cfg.icePadSide * cell, padTop = cfg.icePadTop * cell, padBottom = cfg.icePadBottom * cell;
            for (int k = 0; k < _stages.Length; k++)
            {
                if (_stages[k] != null) { _stages[k].transform.localScale = cfg.iceStageScale; _stages[k].transform.localPosition = cfg.iceStagePos; }
                if (_fractures[k] != null) { _fractures[k].localScale = cfg.iceStageScale; _fractures[k].localPosition = cfg.iceStagePos; }
            }
            Renderer r = _stages != null && _stages[0] != null ? _stages[0].GetComponent<Renderer>() : null;
            MeshFilter mf = r != null ? r.GetComponent<MeshFilter>() : null;
            if (mf == null || mf.sharedMesh == null) return;
            if (_baseScale == Vector3.zero) _baseScale = transform.localScale / SizeVsBox;
            Vector3 sc = transform.localScale;
            for (int it = 0; it < 3; it++)
            {
                Bounds b = WorldBounds(mf.sharedMesh.bounds, r.transform.parent.localToWorldMatrix);
                float kx = (target.size.x + 2f * padX) / Mathf.Max(b.size.x, 1e-4f);
                float ky = (target.size.y + padTop + padBottom) / Mathf.Max(b.size.y, 1e-4f);
                sc = new Vector3(sc.x * kx, sc.y * ky, sc.z * ky);
                transform.localScale = sc;
            }
            Bounds f = WorldBounds(mf.sharedMesh.bounds, r.transform.parent.localToWorldMatrix);
            Vector3 d = target.center + new Vector3(0f, (padTop - padBottom) * 0.5f, 0f) - f.center;
            transform.position += new Vector3(d.x, d.y, 0f);

            Vector3 c0 = StageCenter(0);
            for (int k = 1; k < _stages.Length; k++)
            {
                if (_stages[k] == null) continue;
                Vector3 delta = c0 - StageCenter(k);
                _stages[k].transform.position += delta;
                if (_fractures[k] != null) _fractures[k].position += delta;
            }
        }

        Vector3 StageCenter(int k)
        {
            Transform st = _stages[k] != null ? _stages[k].transform : null;
            MeshFilter m = st != null ? st.GetComponent<MeshFilter>() : null;
            return m != null && m.sharedMesh != null ? WorldBounds(m.sharedMesh.bounds, st.localToWorldMatrix).center : Vector3.zero;
        }

        static Bounds WorldBounds(Bounds local, Matrix4x4 m)
        {
            Bounds b = new Bounds(m.MultiplyPoint3x4(local.center), Vector3.zero);
            for (int i = 0; i < 8; i++)
                b.Encapsulate(m.MultiplyPoint3x4(local.center + Vector3.Scale(local.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1))));
            return b;
        }

        /// <summary>Gắn vào "Ice Model" của prefab hộp đạn</summary>
        public static IceBlockerView Attach(Transform ammoVisual, int maxHits)
        {
            Transform model = FindDeep(ammoVisual, "Ice Model");
            if (model == null) return null;
            var view = model.gameObject.AddComponent<IceBlockerView>();
            view.Setup(maxHits);
            return view;
        }

        void Setup(int maxHits)
        {
            _maxHits = Mathf.Max(1, maxHits);
            gameObject.SetActive(true);
            transform.localScale *= SizeVsBox;
            Transform parts = transform.Find("Parent Of Renderer");
            _stages = new GameObject[StageNames.Length];
            _fractures = new Transform[StageNames.Length];
            for (int i = 0; i < StageNames.Length; i++)
            {
                Transform s = parts != null ? parts.Find(StageNames[i]) : null;
                Transform f = parts != null ? parts.Find(StageNames[i] + "Fracture") : null;
                _stages[i] = s != null ? s.gameObject : null;
                _fractures[i] = f;
                if (s != null) s.gameObject.SetActive(false);
                if (f != null) f.gameObject.SetActive(true);
            }
            Transform snow = transform.Find("SnowExplosion");
            _snow = snow != null ? snow.GetComponent<ParticleSystem>() : null;
            if (_snow != null)
            {
                foreach (ParticleSystem ps in _snow.GetComponentsInChildren<ParticleSystem>(true))
                {
                    ParticleSystem.MainModule m = ps.main;
                    m.playOnAwake = false;
                }
                _snow.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        /// <summary>Mức băng ứng với số lần còn lại</summary>
        int StageFor(int remaining)
        {
            if (remaining <= 0) return -1;
            int lost = _maxHits - remaining;
            int last = StageNames.Length - 1;
            return Mathf.Clamp(Mathf.RoundToInt(lost * (float)last / _maxHits), 0, last);
        }

        /// <summary>Hiện đúng mức băng (không hiệu ứng)</summary>
        public void Show(int remaining)
        {
            int stage = StageFor(remaining);
            for (int i = 0; i < _stages.Length; i++)
                if (_stages[i] != null) _stages[i].SetActive(i == stage);
            _shown = stage;
        }

        /// <summary>Băng vừa nứt một nấc (còn remaining lần)</summary>
        public void Crack(int remaining)
        {
            int stage = StageFor(remaining);
            if (stage != _shown) ExplodeStage(_shown);
            PlaySnow();
            if (remaining <= 0) { HideAll(); return; }
            Show(remaining);
        }

        void ExplodeStage(int stage)
        {
            if (stage < 0 || stage >= _fractures.Length || _fractures[stage] == null) return;
            DebrisFx.FlingCells(_fractures[stage], transform.position, 0.6f);
        }

        void PlaySnow()
        {
            if (_snow == null) return;
            _snow.Play(true);
        }

        void HideAll()
        {
            foreach (GameObject s in _stages) if (s != null) s.SetActive(false);
            _shown = -1;
        }

        static Transform FindDeep(Transform root, string name)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
            return null;
        }
    }
}
