using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>
    /// Một vật thể trên bàn: cube nhỏ, khối gộp (nhiều ô dùng chung máu), hòm gỗ hoặc tường.
    /// Có BoxCollider2D để bi nảy vào; vị trí logic là <see cref="Cells"/> trên lưới bàn.
    /// </summary>
    public class BoardBlock : MonoBehaviour
    {
        public BoardItemKind Kind { get; private set; }
        public string ColorCode { get; private set; }
        public int Hp { get; private set; }
        public int MaxHp { get; private set; }
        public RectInt Cells { get; set; }
        public RectInt OriginCells { get; private set; }
        /// <summary>Ô chiếm thực tế (tính từ Cells.min) với khối gộp hình bất kỳ; null = phủ kín Cells.</summary>
        public List<Vector2Int> Mask { get; private set; }
        public List<BoardItemData> Contents { get; private set; }
        public bool IsDestroyed { get; private set; }

        // Cột nhà không tính vào điều kiện thắng (như tường): máu cột không nằm trong ngân sách bi của level
        public bool IsDestructible => Kind != BoardItemKind.Wall && Kind != BoardItemKind.Column;
        /// <summary>Hòm gỗ / cột nhà: bi màu nào cũng trừ máu nhưng KHÔNG bị tiêu hao (nảy tiếp), không phải mục tiêu ngắm.</summary>
        public bool IsFreeHit => Kind == BoardItemKind.Crate || Kind == BoardItemKind.Column;
        public bool IsStatic => Kind == BoardItemKind.Wall;

        Transform _visual;
        TextMeshPro _label;
        Rigidbody2D _body;
        Vector3 _visualBaseScale;
        float _visualBaseZ;
        const float RowDepth = 0.02f; // hàng thấp hơn gần camera hơn (giữ thứ tự che như cảnh nghiêng gốc)
        float _punch;
        float _fallVelocity;
        Vector2 _targetPos;

        /// <summary>Các ô bàn cờ khối đang chiếm.</summary>
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

        public void Init(BoardItemData data, Transform visual, TextMeshPro label, Vector2 worldPos, Vector2 size,
                         List<Rect> colliderParts = null)
        {
            Mask = data.Mask;
            Kind = data.Kind;
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
            _body.interpolation = RigidbodyInterpolation2D.None;   // chỉ bật nội suy trong lúc khối RƠI (Wake) – 1000+ khối đứng yên không phải nội suy mỗi khung

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
            Sleep();   // đứng yên cho tới khi bị bi trúng hoặc mất chỗ đỡ
        }

        /// <summary>
        /// Màn dày đặc có 1000+ khối: mỗi khối chạy FixedUpdate + Update + LateUpdate MỖI khung dù đứng yên (đo: ~3.6 ms/khung trên
        /// Editor). Giờ component chỉ BẬT khi có việc (rơi xuống ô đích, nảy khi bị trúng) và tự tắt khi xong.
        /// </summary>
        /// <param name="moving">true = khối sắp rơi: chuyển Kinematic + nội suy. false (bị bi trúng, chỉ nảy hình) giữ nguyên Static để không dựng lại tiếp xúc.</param>
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

        /// <summary>Đứng yên → Static: 1000+ khối không còn là 1000+ vật thể kinematic trong mô phỏng Physics2D (đo ở màn dày: phần tốn nhất khung hình).</summary>
        void Sleep()
        {
            if (_body != null)
            {
                _body.interpolation = RigidbodyInterpolation2D.None;
                if (_body.bodyType != RigidbodyType2D.Static) _body.bodyType = RigidbodyType2D.Static;
            }
            enabled = false;
        }

        /// <summary>Hàng thấp gần camera hơn (xem RowDepth) – chỉ cần tính lại khi khối đổi chỗ.</summary>
        void ApplyDepth()
        {
            if (_visual == null) return;
            Vector3 lp = _visual.localPosition;
            lp.z = _visualBaseZ + transform.position.y * RowDepth;
            _visual.localPosition = lp;
        }

        /// <summary>Bi màu <paramref name="ballColor"/> có trừ máu được khối này không.</summary>
        public bool Accepts(string ballColor)
        {
            if (IsDestroyed) return false;
            if (IsFreeHit) return true;
            if (Kind == BoardItemKind.Cube || Kind == BoardItemKind.CombinedCube)
                return string.Equals(ballColor, ColorCode, System.StringComparison.OrdinalIgnoreCase);
            return false;
        }

        /// <summary>Trừ 1 máu. Trả về true nếu khối vừa bị phá.</summary>
        public bool TakeHit()
        {
            if (IsDestroyed) return false;
            Hp--;
            _punch = 1f;
            Wake(false);
            RefreshLabel();
            if (Hp > 0) return false;
            IsDestroyed = true;
            return true;
        }

        public void ForceDestroy()
        {
            Hp = 0;
            IsDestroyed = true;
        }

        public void SetTarget(Vector2 worldPos)
        {
            if (((Vector2)transform.position - worldPos).sqrMagnitude > 0.00001f || (_targetPos - worldPos).sqrMagnitude > 0.00001f) Wake(true);
            _targetPos = worldPos;
        }
        public bool IsMoving => ((Vector2)transform.position - _targetPos).sqrMagnitude > 0.0001f;

        void RefreshLabel()
        {
            if (_label == null) return;
            bool show = IsFreeHit || Kind == BoardItemKind.CombinedCube || MaxHp > 1;
            _label.gameObject.SetActive(show && Hp > 0);
            if (show) _label.SetText("{0}", Hp);
        }

        void FixedUpdate()
        {
            // Rơi theo trọng lực (gia tốc) tới vị trí ô đích
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
                float s = 1f + Mathf.Sin(_punch * Mathf.PI) * (Kind == BoardItemKind.Cube ? 0.18f : 0.04f);
                _visual.localScale = _visualBaseScale * s;
            }
            // xong việc (đã tới ô đích, hết nảy) → ngủ
            if (_punch <= 0f && !IsMoving) { ApplyDepth(); Sleep(); }
        }
    }
}
