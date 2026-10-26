using System;
using System.Collections.Generic;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>Pool bi + thả bi vào bàn từ cửa phun trên đỉnh</summary>
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
        /// <summary>Dùng cho lấy màu từ mã màu</summary>
        public Color ColorOf(string code) => _colors != null ? _colors.ColorOf(code) : Color.white;

        /// <summary>Xử lý khởi tạo quản lý bi</summary>
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

        /// <summary>Làm nóng trước khi chơi: dựng sẵn count viên bi vào pool</summary>
        public System.Collections.IEnumerator Prewarm(int count, int perFrame = 4)
        {
            if (_trailMat != null) _trailMat.SetPass(0);
            foreach (ColorLibrary.Entry e in _colors.entries)
            {
                Material m = _colors.BallMat(e.code);
                if (m != null) m.SetPass(0);
                Material tail = Ball.WarmTailMaterial(m, e.code, e.color);
                if (tail != null) tail.SetPass(0);
                yield return null;
            }
            int made = 0;
            while (made < count)
            {
                for (int i = 0; i < perFrame && made < count; i++, made++)
                {
                    Ball b = Create();
                    b.EnterLane();
                    b.gameObject.SetActive(false);
                    _pool.Push(b);
                }
                yield return null;
            }
        }

        /// <summary>Dùng cho lấy một bi từ pool theo màu</summary>
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
            ModelUtil.StripPhysics(visual);
            float d = Config.ballRadius * 2f;
            visual.transform.localScale = Vector3.one * (d / 1.03f);

            var ball = go.AddComponent<Ball>();
            ball.Setup(this, visual.transform, Config.ballRadius, _trailMat, _physMat);
            return ball;
        }

        /// <summary>Bi rời máng, phun xuống bàn từ cửa trên đỉnh</summary>
        public void ReleaseIntoArena(Ball ball, float exitLateral = 0f)
        {
            float spreadAngle = Mathf.Clamp(exitLateral * 80f, -Config.ballSpawnSpread, Config.ballSpawnSpread);
            spreadAngle += UnityEngine.Random.Range(-5f, 5f);
            Vector2 dir = Quaternion.Euler(0f, 0f, spreadAngle) * Vector2.down;
            Vector2 pos = Config.dispenserOutlet + new Vector2(Mathf.Clamp(exitLateral * 0.45f, -0.16f, 0.16f), 0f);
            ball.EnterArena(pos, dir.normalized * Config.ballSpeed);
        }

        /// <summary>Xử lý thả lại bi vào bàn</summary>
        public void Respawn(Ball ball) => ReleaseIntoArena(ball, 0f);

        /// <summary>Xử lý huỷ bi đã trúng khối</summary>
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

        /// <summary>Xử lý xoá toàn bộ bi</summary>
        public void ClearAll()
        {
            for (int i = _active.Count - 1; i >= 0; i--) Recycle(_active[i]);
        }

        /// <summary>Có bi nào trên bàn còn mục tiêu cùng màu không</summary>
        public bool AnyBallHasTarget()
        {
            foreach (Ball b in _active) if (Board.HasTargetFor(b.ColorCode)) return true;
            return false;
        }
    }
}
