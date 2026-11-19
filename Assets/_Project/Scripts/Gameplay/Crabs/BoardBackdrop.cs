using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>Crab Mode: tấm nền giấy trắng (sprite Sliced) nằm dưới tranh. Mỗi level tự căn theo khung bao các khối + lề.
    /// Đặt sẵn trong scene (World/BoardBackdrop); lề, độ sâu, sprite chỉnh trong Inspector.</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class BoardBackdrop : MonoBehaviour
    {
        public BoardController board;
        [Tooltip("Lề hai bên trái/phải, tính theo số ô")] public float paddingSide = 3f;
        [Tooltip("Lề phía trên, tính theo số ô")] public float paddingTop = 2f;
        [Tooltip("Lề phía dưới, tính theo số ô (đừng để chạm máy xay)")] public float paddingBottom = 1.2f;
        [Tooltip("Độ sâu z (lớn hơn z của khối = nằm sau khối)")] public float z = 2f;

        SpriteRenderer _sr;

        void OnEnable()
        {
            _sr = GetComponent<SpriteRenderer>();
            if (board != null) board.OnBuilt += Fit;
        }

        void OnDisable()
        {
            if (board != null) board.OnBuilt -= Fit;
        }

        /// <summary>Căn tấm nền theo khung tranh của level hiện tại</summary>
        public void Fit()
        {
            if (board == null || !board.TryGetBlocksBounds(out Rect r)) return;
            float c = board.CellSize;
            float l = r.xMin - paddingSide * c, rt = r.xMax + paddingSide * c, b = r.yMin - paddingBottom * c, t = r.yMax + paddingTop * c;
            transform.position = new Vector3((l + rt) * 0.5f, (b + t) * 0.5f, z);
            transform.localScale = Vector3.one;
            _sr.drawMode = SpriteDrawMode.Sliced;
            _sr.size = new Vector2(rt - l, t - b);
        }
    }
}
