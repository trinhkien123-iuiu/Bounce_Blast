using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>
    /// Tường vật lý của lòng bàn (theo khung collider gốc, có lỗ phun ở đỉnh được bịt bằng mép y = 6.92).
    /// Đặt sẵn trong scene (Systems/GameManager/ArenaWalls) cùng EdgeCollider2D: các điểm đã NUNG sẵn nên xem được bằng Gizmo
    /// và sửa trực tiếp. Chỉ khi collider chưa có điểm nào thì <see cref="Rebuild"/> mới dựng từ <c>GameConfig.arena</c>.
    /// Menu chuột phải component → "Dựng lại từ GameConfig.arena" để dựng lại thủ công sau khi đổi arena.
    /// </summary>
    [RequireComponent(typeof(EdgeCollider2D))]
    public class ArenaWalls : MonoBehaviour
    {
        // Đo theo mép trong NHÌN THẤY của khung (render 2×): tường dọc x = arena.xMin/xMax (1.07 / 9.53),
        // trần y ≈ 7.18, góc trên bo ~0.25; khe cửa phun x 4.74..5.86 được bịt bởi mép y ≈ 6.92.
        // (Collider máng gốc "SMALL/BIG CONVEYOR COLLIDERS", "Orbit_Collider" đã xoá khỏi scene: máng chạy kinematic.)
        [Tooltip("Cao độ trần (mép trong nhìn thấy của khung)")] [SerializeField] float _top = 7.18f;
        [Tooltip("Bán kính bo hai góc trên")] [SerializeField] float _cornerRadius = 0.25f;

        /// <summary>Đảm bảo có collider và điểm: scene đã nung sẵn thì giữ nguyên.</summary>
        public void EnsureBuilt(GameConfig config)
        {
            var edge = GetComponent<EdgeCollider2D>();
            if (edge.pointCount < 2) Rebuild(config);
        }

        [ContextMenu("Dựng lại từ GameConfig.arena")]
        void RebuildFromManager()
        {
            var gm = FindObjectOfType<GameManager>();
            if (gm != null && gm.config != null) Rebuild(gm.config);
        }

        public void Rebuild(GameConfig config)
        {
            var edge = GetComponent<EdgeCollider2D>();
            if (edge.sharedMaterial == null) edge.sharedMaterial = new PhysicsMaterial2D("ArenaBounce") { bounciness = 1f, friction = 0f };
            Rect a = config.arena;
            a.yMin += config.boardOffsetY; // sàn nâng cùng bàn gạch (hàng đáy khỏi bị gờ sàn che)
            float top = _top, rc = _cornerRadius;
            edge.points = new[]
            {
                new Vector2(4.95f, 6.92f), new Vector2(4.74f, top), new Vector2(a.xMin + rc, top), new Vector2(a.xMin, top - rc),
                new Vector2(a.xMin, a.yMin), new Vector2(a.xMax, a.yMin), new Vector2(a.xMax, top - rc), new Vector2(a.xMax - rc, top),
                new Vector2(5.86f, top), new Vector2(5.65f, 6.93f), new Vector2(4.95f, 6.92f)
            };
            edge.edgeRadius = 0.02f;
        }

        void OnDrawGizmosSelected()
        {
            var edge = GetComponent<EdgeCollider2D>();
            if (edge == null) return;
            Gizmos.color = Color.cyan;
            Vector2[] p = edge.points;
            for (int i = 1; i < p.Length; i++) Gizmos.DrawLine(transform.TransformPoint(p[i - 1]), transform.TransformPoint(p[i]));
        }
    }
}
