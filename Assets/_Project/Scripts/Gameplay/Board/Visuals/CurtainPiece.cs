using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>Mảnh gạch nhỏ bay từ gạch vừa vỡ lên con lắc của rèm cùng màu; chạm tới thì con lắc đung đưa và số giảm 1 (BlockerCurtainHandler.MoveBrokenPieceToMedal của game gốc)</summary>
    public class CurtainPiece : MonoBehaviour
    {
        const float FlightSeconds = 0.45f;
        const float ArcCells = 2.5f;

        BoardController _board;
        BoardBlock _curtain;
        CurtainView _view;
        Vector3 _from;
        Vector3 _spin;
        float _size;
        float _t;
        Color _tint;

        /// <summary>Phóng một mảnh từ vị trí from lên con lắc của curtain</summary>
        public static void Launch(BoardController board, BoardBlock curtain, CurtainView view, Vector3 from, Material material, Color tint, float size, int layer)
        {
            if (view == null) { board.CurtainPieceArrived(curtain); return; }
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(go.GetComponent<Collider>());
            go.name = "CurtainPiece";
            go.layer = layer;
            if (material != null) go.GetComponent<MeshRenderer>().sharedMaterial = material;
            go.transform.SetParent(Vfx.Root, false);
            go.transform.position = from;
            go.transform.localScale = Vector3.one * size;
            var p = go.AddComponent<CurtainPiece>();
            p._board = board;
            p._curtain = curtain;
            p._view = view;
            p._from = from;
            p._size = size;
            p._tint = tint;
            p._spin = Random.insideUnitSphere * 540f;
        }

        void Update()
        {
            if (_curtain == null || _curtain.IsDestroyed || _view == null) { Destroy(gameObject); return; }
            _t += Time.deltaTime / FlightSeconds;
            float k = Mathf.Clamp01(_t);
            Vector3 to = _view.MedalPosition;
            float e = k * k * (3f - 2f * k);
            Vector3 pos = Vector3.Lerp(_from, to, e);
            pos.y += Mathf.Sin(k * Mathf.PI) * ArcCells * _size;
            pos.z = Mathf.Lerp(_from.z, to.z, Mathf.Min(1f, k * 3f));
            transform.position = pos;
            transform.Rotate(_spin * Time.deltaTime);
            transform.localScale = Vector3.one * _size * Mathf.Lerp(1f, 0.6f, k);
            if (_t < 1f) return;

            _board.BreakFx.SpawnBurst(to, _curtain.ColorCode, _size * 2f);
            _board.CurtainPieceArrived(_curtain);
            Destroy(gameObject);
        }
    }
}
