using System;
using System.Collections.Generic;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>Pool bi + thả bi vào bàn từ cửa phun trên đỉnh (gốc: InGameBallManager / BallSpawner).</summary>
    public class BallManager : MonoBehaviour
    {
        public event Action<Ball> OnBallConsumed;

        public GameConfig Config { get; private set; }
        public BoardController Board { get; private set; }

        ColorLibrary _colors;
        readonly Stack<Ball> _pool = new Stack<Ball>();
        readonly List<Ball> _active = new List<Ball>();
        PhysicsMaterial2D _physMat;
        Material _trailMat;
        int _ballLayer;

        public int ActiveCount => _active.Count;
        public IReadOnlyList<Ball> Active => _active;
        public Color ColorOf(string code) => _colors != null ? _colors.ColorOf(code) : Color.white;

        public void Init(GameConfig config, ColorLibrary colors, BoardController board)
        {
            Config = config;
            _colors = colors;
            Board = board;
            _ballLayer = LayerMask.NameToLayer("Ball");
            if (_ballLayer >= 0) Physics2D.IgnoreLayerCollision(_ballLayer, _ballLayer, true);
            _physMat = new PhysicsMaterial2D("BallBounce") { bounciness = 1f, friction = 0f };
            _trailMat = config.trailMaterial != null ? config.trailMaterial : new Material(Shader.Find("Sprites/Default"));
        }

        public Ball Get(string colorCode)
        {
            Ball b = _pool.Count > 0 ? _pool.Pop() : Create();
            b.Activate(colorCode, _colors.BallMat(colorCode), _colors.ColorOf(colorCode));
            _active.Add(b);
            return b;
        }

        Ball Create()
        {
            var go = new GameObject("Ball");
            go.transform.SetParent(transform, false);
            if (_ballLayer >= 0) go.layer = _ballLayer;

            GameObject visual = Instantiate(Config.ballPrefab, go.transform);
            visual.name = "Visual";
            BoardController.StripPhysics(visual);
            // Scale đều theo đường kính mesh bán cầu "Sphere" (≈1.03) – không FitVisual theo bounds
            // vì bounds gồm cả đuôi tam giác và bán cầu chỉ sâu 0.5 → bi bị kéo dẹt/méo.
            float d = Config.ballRadius * 2f;
            visual.transform.localScale = Vector3.one * (d / 1.03f);

            var ball = go.AddComponent<Ball>();
            ball.Setup(this, visual.transform, Config.ballRadius, _trailMat, _physMat);
            return ball;
        }

        /// <summary>Bi rời máng, phun xuống bàn từ cửa trên đỉnh (nêm tam giác phía dưới sẽ tách đôi luồng bi).</summary>
        public void ReleaseIntoArena(Ball ball, float exitLateral = 0f)
        {
            float spreadAngle = Mathf.Clamp(exitLateral * 80f, -Config.ballSpawnSpread, Config.ballSpawnSpread);
            spreadAngle += UnityEngine.Random.Range(-5f, 5f);
            Vector2 dir = Quaternion.Euler(0f, 0f, spreadAngle) * Vector2.down;
            Vector2 pos = Config.dispenserOutlet + new Vector2(Mathf.Clamp(exitLateral * 0.45f, -0.16f, 0.16f), 0f);
            ball.EnterArena(pos, dir.normalized * Config.ballSpeed);
        }

        public void Respawn(Ball ball) => ReleaseIntoArena(ball, 0f);

        public void Consume(Ball ball, Vector2 at)
        {
            AudioManager.Play(Sfx.BallHit);
            Recycle(ball);
            OnBallConsumed?.Invoke(ball);
        }

        void Recycle(Ball ball)
        {
            _active.Remove(ball);
            ball.EnterLane();
            ball.gameObject.SetActive(false);
            _pool.Push(ball);
        }

        public void ClearAll()
        {
            for (int i = _active.Count - 1; i >= 0; i--) Recycle(_active[i]);
        }

        /// <summary>Có bi nào trên bàn còn mục tiêu cùng màu không (dùng để xét deadlock).</summary>
        public bool AnyBallHasTarget()
        {
            foreach (Ball b in _active) if (Board.HasTargetFor(b.ColorCode)) return true;
            return false;
        }
    }
}
