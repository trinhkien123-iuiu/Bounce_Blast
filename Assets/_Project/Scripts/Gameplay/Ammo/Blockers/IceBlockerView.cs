using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>
    /// Hiển thị BĂNG trên hộp đạn (/ice-N) bằng đúng asset gốc có sẵn trong prefab Ammunation:
    /// <c>Model/Cube/Jelly Cube/RiggedCube (8)/Ice Model</c> (gốc: BlockerIce).
    /// <list type="bullet">
    /// <item>4 mức băng <c>IceCube, IceCube2, IceCube3, IceCube4</c>: nguyên khối → sứt dần → còn mẩu (mỗi mức một mesh riêng).</item>
    /// <item>Mỗi mức có bộ mảnh vỡ <c>IceCubeNFracture</c> (mesh "_cell" của KPExploder gốc) – văng ra khi băng chuyển mức / vỡ hẳn.</item>
    /// <item><c>SnowExplosion</c> (Glow/Smoke/Snow) – bụi tuyết phát mỗi lần băng nứt (gốc: particleOnDamage).</item>
    /// </list>
    /// Không có số đếm: bản gốc không hiện số trên băng. Luật nứt (hộp kề được bắn) nằm ở AmmoGridController/AmmoBox,
    /// component này chỉ lo phần nhìn.
    /// </summary>
    public class IceBlockerView : MonoBehaviour
    {
        /// <summary>Băng to hơn hộp (ảnh gốc Mechanics/hộp băng nguyên vẹn: cạnh băng ≈ 1.2 × cạnh hộp, tràn sang ô kề).</summary>
        const float SizeVsBox = 1.18f;

        static readonly string[] StageNames = { "IceCube", "IceCube2", "IceCube3", "IceCube4" };

        GameObject[] _stages;
        Transform[] _fractures;
        ParticleSystem _snow;
        int _maxHits;
        int _shown = -1;

        /// <summary>Gắn vào "Ice Model" của prefab hộp đạn. Trả null nếu prefab không có băng.</summary>
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
                if (f != null) f.gameObject.SetActive(true); // các mảnh con vẫn tắt cho tới lúc văng
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

        /// <summary>Mức băng ứng với số lần còn lại: còn đủ = mức 0 (nguyên), càng ít càng sứt; luôn chừa mức cuối (mẩu) cho lần cuối khi băng dày.</summary>
        int StageFor(int remaining)
        {
            if (remaining <= 0) return -1;
            int lost = _maxHits - remaining;
            int last = StageNames.Length - 1;
            return Mathf.Clamp(Mathf.RoundToInt(lost * (float)last / _maxHits), 0, last);
        }

        /// <summary>Hiện đúng mức băng (không hiệu ứng) – dùng lúc dựng hộp.</summary>
        public void Show(int remaining)
        {
            int stage = StageFor(remaining);
            for (int i = 0; i < _stages.Length; i++)
                if (_stages[i] != null) _stages[i].SetActive(i == stage);
            _shown = stage;
        }

        /// <summary>Băng vừa nứt một nấc (còn <paramref name="remaining"/> lần): mức cũ văng mảnh + bụi tuyết, hiện mức mới.</summary>
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
