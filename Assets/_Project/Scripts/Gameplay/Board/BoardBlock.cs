using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>Một vật thể trên bàn: cube nhỏ, khối gộp</summary>
    public class BoardBlock : MonoBehaviour
    {
        public BoardItemKind Kind { get; private set; }
        public string ColorCode { get; private set; }
        public int Hp { get; private set; }
        public int MaxHp { get; private set; }
        public RectInt Cells { get; set; }
        public RectInt OriginCells { get; private set; }
        /// <summary>Ô chiếm thực tế (tính từ Cells.min) với khối gộp hình bất kỳ</summary>
        public List<Vector2Int> Mask { get; private set; }
        public List<BoardItemData> Contents { get; private set; }
        public bool IsDestroyed { get; private set; }

        /// <summary>Luật của loại khối này (Board/Kinds)</summary>
        public BlockKind Rules { get; private set; }
        /// <summary>Tính vào điều kiện thắng (tường, cột nhà thì không)</summary>
        public bool IsDestructible => Rules.CountsForWin;
        /// <summary>Hòm gỗ / cột nhà: bi màu nào cũng trừ máu nhưng KHÔNG bị tiêu hao</summary>
        public bool IsFreeHit => Rules.HitByAnyColor;
        public bool IsStatic => Rules.IsStatic;

        Transform _visual;
        TextMeshPro _label;
        Rigidbody2D _body;
        Vector3 _visualBaseScale;
        float _visualBaseZ;
        const float RowDepth = 0.02f;
        float _punch;
        float _fallVelocity;
        Vector2 _targetPos;

        /// <summary>Các ô bàn cờ khối đang chiếm</summary>
        public IEnumerable<Vector2Int> OccupiedCells()
        {
            RectInt r = Cells;
            if (Mask == null)
            {
                for (int x = r.xMin; x < r.xMax; x++)
                    for (int y = r.yMin; y < r.yMax; y++) yield return new Vector2Int(x, y);
            }
            else foreach (Vector2Int m in Mask) yield return r.min + m;
        }

        /// <summary>Xử lý khởi tạo khối trên bàn</summary>
        public void Init(BoardItemData data, Transform visual, TextMeshPro label, Vector2 worldPos, Vector2 size,
                         List<Rect> colliderParts = null)
        {
            Mask = data.Mask;
            Kind = data.Kind;
            Rules = BlockKinds.Of(data.Kind);
            ColorCode = data.ColorCode;
            Hp = MaxHp = Mathf.Max(1, data.Hp);
            Cells = OriginCells = data.Cells;
            Contents = data.Contents;
            _visual = visual;
            _label = label;
            _visualBaseScale = visual != null ? visual.localScale : Vector3.one;
            _visualBaseZ = visual != null ? visual.localPosition.z : 0f;

            transform.position = new Vector3(worldPos.x, worldPos.y, 0f);
            _targetPos = worldPos;

            _body = gameObject.AddComponent<Rigidbody2D>();
            _body.bodyType = RigidbodyType2D.Kinematic;
            _body.interpolation = RigidbodyInterpolation2D.None;

            if (colliderParts == null || colliderParts.Count == 0)
            {
                var col = gameObject.AddComponent<BoxCollider2D>();
                col.size = size * 0.995f;
            }
            else
                foreach (Rect part in colliderParts)
                {
                    var col = gameObject.AddComponent<BoxCollider2D>();
                    col.offset = part.center;
                    col.size = part.size * 0.995f;
                }

            RefreshLabel();
            ApplyDepth();
            Sleep();
        }

        /// <summary>Màn dày đặc có 1000+ khối</summary>
        void Wake(bool moving)
        {
            if (IsDestroyed) return;
            enabled = true;
            if (moving && _body != null)
            {
                if (_body.bodyType != RigidbodyType2D.Kinematic) _body.bodyType = RigidbodyType2D.Kinematic;
                _body.interpolation = RigidbodyInterpolation2D.Interpolate;
            }
        }

        /// <summary>Đứng yên → Static: 1000+ khối không còn là 1000+ vật thể kinematic trong mô phỏng Physics2D</summary>
        void Sleep()
        {
            if (_body != null)
            {
                _body.interpolation = RigidbodyInterpolation2D.None;
                if (_body.bodyType != RigidbodyType2D.Static) _body.bodyType = RigidbodyType2D.Static;
            }
            enabled = false;
        }

        /// <summary>Hàng thấp gần camera hơn (xem RowDepth)</summary>
        void ApplyDepth()
        {
            if (_visual == null) return;
            Vector3 lp = _visual.localPosition;
            lp.z = _visualBaseZ + transform.position.y * RowDepth;
            _visual.localPosition = lp;
        }

        /// <summary>Bi màu ballColor có trừ máu được khối này không</summary>
        public bool Accepts(string ballColor)
        {
            return !IsDestroyed && Rules.Accepts(this, ballColor);
        }

        /// <summary>Trừ 1 máu. Trả về true nếu khối vừa bị phá</summary>
        public bool TakeHit()
        {
            if (IsDestroyed) return false;
            Hp--;
            _punch = 1f;
            Wake(false);
            RefreshLabel();
            if (Hp > 0) { Rules.OnHit(this); return false; }
            IsDestroyed = true;
            return true;
        }

        /// <summary>Xử lý phá khối ngay, không tính máu</summary>
        public void ForceDestroy()
        {
            Hp = 0;
            IsDestroyed = true;
        }

        /// <summary>Xử lý đặt vị trí đích cho khối đang nhúc nhích</summary>
        public void SetTarget(Vector2 worldPos)
        {
            if (((Vector2)transform.position - worldPos).sqrMagnitude > 0.00001f || (_targetPos - worldPos).sqrMagnitude > 0.00001f) Wake(true);
            _targetPos = worldPos;
        }
        public bool IsMoving => ((Vector2)transform.position - _targetPos).sqrMagnitude > 0.0001f;

        void RefreshLabel()
        {
            if (_label == null) return;
            bool show = Rules.ShowsHpLabel(this);
            _label.gameObject.SetActive(show && Hp > 0);
            if (show) _label.SetText("{0}", Hp);
        }

        void FixedUpdate()
        {
            Vector2 pos = _body.position;
            if ((pos - _targetPos).sqrMagnitude > 0.00001f)
            {
                _fallVelocity += 40f * Time.fixedDeltaTime;
                Vector2 next = Vector2.MoveTowards(pos, _targetPos, _fallVelocity * Time.fixedDeltaTime);
                _body.MovePosition(next);
            }
            else
            {
                _fallVelocity = 0f;
            }
        }

        void LateUpdate()
        {
            ApplyDepth();
        }

        void Update()
        {
            if (_visual != null && _punch > 0f)
            {
                _punch = Mathf.Max(0f, _punch - Time.deltaTime * 6f);
                float s = 1f + Mathf.Sin(_punch * Mathf.PI) * Rules.HitPunch;
                _visual.localScale = _visualBaseScale * s;
            }
            if (_punch <= 0f && !IsMoving) { ApplyDepth(); Sleep(); }
        }
    }
}
