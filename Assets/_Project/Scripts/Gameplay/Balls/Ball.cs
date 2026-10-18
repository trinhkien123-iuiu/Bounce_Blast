using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>
    /// Một viên bi. Hai trạng thái:
    ///  - Lane: chạy kinematic trên máng (ConveyorLane điều khiển vị trí, tắt va chạm).
    ///  - Arena: vật lý 2D, tốc độ không đổi, nảy tường/khối; chạm khối cùng màu thì trừ máu và biến mất.
    /// Va chạm theo code BouncingBall cũ: tốc độ cố định, bay thẳng, nảy phản xạ + lệch nhỏ, chống nảy ngang mãi;
    /// sau vài lần nảy chỉ ngắm thẳng vào khối cùng màu khi đường bay thông (không bẻ cong xuyên vật cản).
    /// </summary>
    public class Ball : MonoBehaviour
    {
        public string ColorCode { get; private set; }
        public bool InArena { get; private set; }

        Rigidbody2D _rb;
        CircleCollider2D _col;
        Transform _visual;
        GameObject _tail;
        static readonly Quaternion VisualBaseRotation = Quaternion.Euler(90f, 0f, 0f); // xoay gốc của prefab Ball
        Renderer _renderer;
        BallManager _owner;
        Renderer _tailRenderer;
        Material _tailBase;
        static readonly System.Collections.Generic.Dictionary<string, Material> TailMats = new System.Collections.Generic.Dictionary<string, Material>();

        static Material TailMaterial(Material baseMat, string code, Color color)
        {
            if (baseMat == null) return null;
            if (TailMats.TryGetValue(code ?? "", out Material m) && m != null) return m;
            Color tc = Color.Lerp(color, Color.white, 0.15f);
            tc.a = 0.7f;
            m = new Material(baseMat) { name = "Tail " + code };
            m.SetColor("_ColorA", tc);
            TailMats[code ?? ""] = m;
            return m;
        }
        /// <summary>Tạo sẵn material vệt bi của một màu (BallManager.Prewarm).</summary>
        public static Material WarmTailMaterial(Material baseMat, string code, Color color) => TailMaterial(baseMat, code, color);

        int _bounceCount;
        float _lastUsefulHit;
        float _flatTime;
        float _losCooldown;
        static int _blockMask = -1;
        int _safetyTick;

        // Thông số va chạm (lấy theo code BouncingBall cũ: đi thẳng, nảy phản xạ, chỉ ngắm khi đường bay thông)
        // 3 thông số dưới chỉnh trong GameConfig (ballAimMinBounces / ballMinVerticalSpeed / ballDeflectDeg)
        int LosMinBounces => _owner.Config.ballAimMinBounces;
        float MinVerticalSpeed => _owner.Config.ballMinVerticalSpeed;
        float DeflectDeg => _owner.Config.ballDeflectDeg;

        Vector3 _baseScale = Vector3.one;

        public void Setup(BallManager owner, Transform visual, float radius, Material trailMat, PhysicsMaterial2D physMat)
        {
            _owner = owner;
            _rb = gameObject.AddComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.drag = 0f;
            _rb.angularDrag = 0f;
            _rb.freezeRotation = true;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            _col = gameObject.AddComponent<CircleCollider2D>();
            _col.radius = radius;
            _col.sharedMaterial = physMat;
            _visual = visual;
            if (_visual != null) _baseScale = _visual.localScale;
            Transform model = visual.Find("Model/lowpollyball");
            _renderer = model != null ? model.GetComponent<Renderer>() : visual.GetComponentInChildren<Renderer>();
            if (_renderer != null)
            {
                _renderer.sortingOrder = 100; // Luôn hiển thị trên cùng, không bị che khuất
                _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _renderer.receiveShadows = false;
            }
            // Đuôi gốc của prefab: tam giác "Trail" (PerformantTrailShader, trắng 27% alpha) nằm phía -y local
            // → chỉ cần xoay cả visual cho đuôi ngược hướng bay (không dùng TrailRenderer).
            Transform tail = visual.Find("Model/lowpollyball/Line Renderer Parent");
            _tail = tail != null ? tail.gameObject : null;
            // Mặt phẳng tam giác đuôi chứa trục đuôi (-z model) nhưng đang đứng nghiêng so với camera
            // → xoay 90° quanh trục đó để tam giác quay mặt về camera (đuôi dày như video gốc).
            if (tail != null)
            {
                tail.localRotation = Quaternion.Euler(0f, 0f, 90f);
                tail.localScale = Vector3.one * 1.15f;
                _tailRenderer = tail.GetComponentInChildren<Renderer>(true);
                if (_tailRenderer != null) _tailRenderer.sortingOrder = 99;
                _tailBase = _tailRenderer != null ? _tailRenderer.sharedMaterial : null;
            }
            if (_tail != null) _tail.SetActive(false);
            if (_blockMask == -1)
            {
                int ballLayer = LayerMask.NameToLayer("Ball");
                _blockMask = Physics2D.DefaultRaycastLayers & ~(ballLayer >= 0 ? 1 << ballLayer : 0);
            }
        }

        void LateUpdate()
        {
            if (!InArena || _tail == null) return;
            Vector2 v = _rb.velocity;
            if (v.sqrMagnitude < 0.01f) return;
            float ang = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg + 90f;
            _visual.rotation = Quaternion.Euler(0f, 0f, ang) * VisualBaseRotation;
        }

        public void Activate(string colorCode, Material mat, Color color)
        {
            ColorCode = colorCode;
            // Mesh "Sphere" chỉ có 1 submesh nhưng prefab gốc gán 2 material (…, Blue):
            // material thừa sẽ vẽ đè lên submesh cuối → bi luôn ra màu xanh. Chỉ giữ 1 material.
            if (_renderer != null && mat != null) _renderer.sharedMaterials = new[] { mat };
            // Đuôi mang màu của chính viên bi (pha trắng cho sáng), đậm hơn bản gốc 27% để nhìn rõ
            if (_tailRenderer != null)
            {
                // material dùng chung theo màu (không MaterialPropertyBlock) → các đuôi cùng màu được batching
                _tailRenderer.sharedMaterial = TailMaterial(_tailBase, colorCode, color);
            }
            gameObject.SetActive(true);
        }

        public void EnterLane()
        {
            InArena = false;
            _rb.simulated = false;
            if (_tail != null) _tail.SetActive(false);
            if (_visual != null)
            {
                _visual.rotation = VisualBaseRotation;
                _visual.localScale = _baseScale;
            }
        }

        public void SetLaneMotion(Vector3 pos, Vector3 forwardDir, float rollDistance)
        {
            transform.position = pos;
            if (_visual != null)
            {
                // Giữ VisualBaseRotation để mặt lồi bán cầu luôn hướng thẳng vào camera,
                // tránh bị back-face culling lộn ngược làm mất hình bóng lúc lăn.
                _visual.rotation = VisualBaseRotation;
                // Nhấp nhô bồng bềnh nhẹ như nổi trên mặt nước
                float bob = 1f + 0.035f * Mathf.Sin(Time.time * 4.5f + rollDistance * 2.5f);
                _visual.localScale = _baseScale * bob;
            }
        }

        public void SetLanePosition(Vector3 pos) => transform.position = pos;

        public void EnterArena(Vector2 pos, Vector2 velocity)
        {
            InArena = true;
            if (_visual != null) _visual.localScale = _baseScale;
            transform.position = new Vector3(pos.x, pos.y, 0f);
            _rb.position = pos;
            _rb.simulated = true;
            _rb.velocity = velocity;
            if (_tail != null) _tail.SetActive(true);
            _bounceCount = 0;
            _flatTime = 0f;
            _losCooldown = 0f;
            _lastUsefulHit = Time.time;
            _safetyTick = Random.Range(0, 10);
        }

        void FixedUpdate()
        {
            if (!InArena) return;
            float dt = Time.fixedDeltaTime;
            if (_losCooldown > 0f) _losCooldown -= dt;
            float speed = _owner.Config.ballSpeed;
            Vector2 v = _rb.velocity;
            Vector2 dir = v.sqrMagnitude > 0.0001f ? v.normalized : Vector2.down;
            Rect a = _owner.Config.arena;
            Vector2 p = _rb.position;

            // Chống nảy ngang vô tận giữa 2 vách: gần như ngang quá 0.2s → bẻ lên/xuống về phía giữa bàn
            if (Mathf.Abs(dir.y) < 0.15f)
            {
                _flatTime += dt;
                if (_flatTime > 0.2f)
                {
                    dir.y = (p.y < a.center.y ? 1f : -1f) * 0.45f;
                    dir.Normalize();
                    _flatTime = 0f;
                }
            }
            else _flatTime = 0f;

            // Bay THẲNG với tốc độ không đổi – không uốn cong trên không trung
            _rb.velocity = dir * speed;

            // Lưới an toàn (~5 lần/giây, lệch pha theo từng bi): tâm bi lọt vào trong vật cản thì đẩy ra ngay
            if ((_safetyTick = (_safetyTick + 1) % 10) == 0)
            {
                Collider2D inside = Physics2D.OverlapPoint(p, _blockMask);
                if (inside != null && inside != _col && inside.attachedRigidbody != _rb) Depenetrate(inside);
            }

            // Văng ra ngoài lòng bàn → thả lại từ cửa phun
            if (p.x < a.xMin - 0.3f || p.x > a.xMax + 0.3f || p.y < a.yMin - 0.3f || p.y > _owner.Config.arenaTopY + 0.6f)
                _owner.Respawn(this);
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            if (!InArena) return;
            var block = collision.collider.GetComponent<BoardBlock>();
            if (block != null && _owner.Board.HandleBallHit(block, ColorCode))
            {
                _lastUsefulHit = Time.time;
                _owner.Consume(this, collision.GetContact(0).point);
                return;
            }
            if (block != null) AudioManager.Play(Sfx.BallHit);
            _bounceCount++;

            // Vận tốc sau va chạm: phản xạ theo pháp tuyến nếu bộ giải vật lý chưa bật ra
            Vector2 post = _rb.velocity;
            if (collision.contactCount > 0)
            {
                Vector2 n = collision.GetContact(0).normal;
                if (Vector2.Dot(post, n) < 0f) post = Vector2.Reflect(post, n);
            }
            if (post.sqrMagnitude < 0.0001f) post = Vector2.down;
            float speed = _owner.Config.ballSpeed;

            // Đủ số lần nảy (hoặc lâu không trúng gì) → nếu có đường bay thông tới khối cùng màu thì ngắm thẳng
            bool idle = Time.time - _lastUsefulHit > 5f;
            if ((_bounceCount >= LosMinBounces || idle) && _losCooldown <= 0f)
            {
                if (_owner.Board.TryGetLineOfSightTarget(ColorCode, _rb.position, _col.radius, _blockMask, out Vector2 target))
                {
                    _rb.velocity = (target - _rb.position).normalized * speed;
                    _losCooldown = 0.35f;
                    return;
                }
                _losCooldown = 0.15f;
            }

            // Không thì nảy phản xạ tự nhiên + giữ thành phần dọc tối thiểu + lệch ngẫu nhiên nhỏ
            if (Mathf.Abs(post.y) < MinVerticalSpeed)
                post.y = (post.y >= 0f ? 1f : -1f) * MinVerticalSpeed;
            post = Quaternion.Euler(0f, 0f, Random.Range(-DeflectDeg, DeflectDeg)) * post;
            _rb.velocity = post.normalized * speed;
        }

        void OnCollisionStay2D(Collision2D collision)
        {
            // Bị khối đang rơi đè lên / kẹp giữa tường và khối: trước đây chỉ nhích 0.02/bước nhưng FixedUpdate ép vận tốc
            // 11 đv/s đâm tiếp vào trong → bi lún hẳn vào khối khác màu (vd gạch đen) và kẹt mãi.
            // Giờ: đo đúng độ lún (Physics2D.Distance) → đẩy bi ra hẳn, vận tốc bật ra ngoài.
            if (!InArena || collision.contactCount == 0) return;
            var block = collision.collider.GetComponent<BoardBlock>();
            if (block != null && !block.IsFreeHit && _owner.Board.HandleBallHit(block, ColorCode))
            {
                _lastUsefulHit = Time.time;
                _owner.Consume(this, collision.GetContact(0).point);
                return;
            }
            Depenetrate(collision.collider);
        }

        void Depenetrate(Collider2D other)
        {
            if (other == null || other.isTrigger) return;
            ColliderDistance2D d = _col.Distance(other);
            if (!d.isValid || !d.isOverlapped) return;
            Vector2 outward = -d.normal;                       // normal hướng từ bi sang vật cản
            _rb.position += outward * (-d.distance + 0.01f);   // distance âm = độ lún
            Vector2 v = _rb.velocity;
            if (Vector2.Dot(v, outward) < 0f) _rb.velocity = Vector2.Reflect(v, outward);
        }
    }
}
