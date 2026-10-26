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
        /// <summary>
        /// Hệ số phóng "Ice Model" của prefab (mesh băng gốc đã to ≈ 1.23 × hộp). Hộp đóng băng cỡ như hộp active, băng bọc ngoài
        /// ≈ 1.15 × cạnh hộp (ảnh FreezeAmmo: băng chỉ nhô nhẹ quanh hộp, không tràn sang ô kề).
        /// </summary>
        const float SizeVsBox = 0.95f;

        static readonly string[] StageNames = { "IceCube", "IceCube2", "IceCube3", "IceCube4" };

        GameObject[] _stages;
        Transform[] _fractures;
        ParticleSystem _snow;
        int _maxHits;
        int _shown = -1;

        Vector3 _baseScale;


        /// <summary>
        /// Co giãn khối băng cho PHỦ KÍN toàn bộ hộp (cả nắp lẫn thân dài phía dưới) + viền (ngang hẹp hơn dọc để 2 hộp băng kề nhau không dính), và căn tâm vào hộp.
        /// "Ice Model" nằm dưới RiggedCube nên bị kéo theo độ dài cột (pose.length) và xoay theo camera → co theo 2 hệ số:
        /// X (bề ngang) và Y=Z (chiều cao trên màn hình), đo lại bounds thật vài vòng cho khớp.
        /// </summary>
        public void FitCover(Bounds target, GameConfig cfg, float cell)
        {
            float padX = cfg.icePadSide * cell, padTop = cfg.icePadTop * cell, padBottom = cfg.icePadBottom * cell;
            for (int k = 0; k < _stages.Length; k++)
            {
                if (_stages[k] != null) { _stages[k].transform.localScale = cfg.iceStageScale; _stages[k].transform.localPosition = cfg.iceStagePos; }
                if (_fractures[k] != null) { _fractures[k].localScale = cfg.iceStageScale; _fractures[k].localPosition = cfg.iceStagePos; }   // mảnh vỡ cùng tỉ lệ khối băng
            }
            Renderer r = _stages != null && _stages[0] != null ? _stages[0].GetComponent<Renderer>() : null;
            MeshFilter mf = r != null ? r.GetComponent<MeshFilter>() : null;
            if (mf == null || mf.sharedMesh == null) return;
            if (_baseScale == Vector3.zero) _baseScale = transform.localScale / SizeVsBox;
            Vector3 sc = transform.localScale;
            for (int it = 0; it < 3; it++)
            {
                Bounds b = WorldBounds(mf.sharedMesh.bounds, r.transform.parent.localToWorldMatrix);   // đo khối băng gốc (chưa áp chỉnh tay của từng mức)
                float kx = (target.size.x + 2f * padX) / Mathf.Max(b.size.x, 1e-4f);
                float ky = (target.size.y + padTop + padBottom) / Mathf.Max(b.size.y, 1e-4f);
                sc = new Vector3(sc.x * kx, sc.y * ky, sc.z * ky);
                transform.localScale = sc;
            }
            Bounds f = WorldBounds(mf.sharedMesh.bounds, r.transform.parent.localToWorldMatrix);
            Vector3 d = target.center + new Vector3(0f, (padTop - padBottom) * 0.5f, 0f) - f.center;   // băng dày thêm phía trên (nắp hộp không lòi ra khỏi băng)
            transform.position += new Vector3(d.x, d.y, 0f);   // camera nhìn thẳng: chỉ căn trong mặt phẳng màn hình

            // Mỗi mức băng (và bộ mảnh vỡ của nó) là mesh riêng có tâm lệch nhau (mức 3 thấp hơn ~0.4) → kéo tâm về tâm mức 0 để khi băng nứt không nhảy chỗ / lệch khỏi hộp
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
