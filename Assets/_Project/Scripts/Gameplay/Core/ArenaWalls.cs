using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>Tường vật lý của lòng bàn</summary>
    [RequireComponent(typeof(EdgeCollider2D))]
    public class ArenaWalls : MonoBehaviour
    {
        [Tooltip("Cao độ trần (mép trong nhìn thấy của khung)")] [SerializeField] float _top = 7.18f;
        [Tooltip("Bán kính bo hai góc trên")] [SerializeField] float _cornerRadius = 0.25f;

        /// <summary>Đảm bảo có collider và điểm</summary>
        public void EnsureBuilt(GameConfig config)
        {
            var edge = GetComponent<EdgeCollider2D>();
            if (edge.pointCount < 2) { Rebuild(config); return; }
            SyncFloor(edge, config);
        }

        /// <summary>Điểm đã nung sẵn nên không tự theo boardOffsetY</summary>
        void SyncFloor(EdgeCollider2D edge, GameConfig config)
        {
            Vector2[] p = edge.points;
            float minY = float.MaxValue;
            foreach (Vector2 v in p) minY = Mathf.Min(minY, v.y);
            float targetLocalY = config.arena.yMin + config.boardOffsetY - transform.position.y;
            if (Mathf.Abs(minY - targetLocalY) < 0.0001f) return;
            for (int i = 0; i < p.Length; i++)
                if (Mathf.Abs(p[i].y - minY) < 0.0001f) p[i].y = targetLocalY;
            edge.points = p;
        }

        [ContextMenu("Dựng lại từ GameConfig.arena")]
        void RebuildFromManager()
        {
            var gm = FindObjectOfType<GameManager>();
            if (gm != null && gm.config != null) Rebuild(gm.config);
        }

        /// <summary>Xử lý dựng lại tường vật lý của bàn chơi</summary>
        public void Rebuild(GameConfig config)
        {
            var edge = GetComponent<EdgeCollider2D>();
            if (edge.sharedMaterial == null) edge.sharedMaterial = new PhysicsMaterial2D("ArenaBounce") { bounciness = 1f, friction = 0f };
            Rect a = config.arena;
            a.yMin += config.boardOffsetY;
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
