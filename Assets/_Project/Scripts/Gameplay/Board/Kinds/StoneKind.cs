using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>Đá "/st-hp": viên be phủ lên một cube, bi màu nào cũng trừ máu, vỡ thì lộ cube bên dưới</summary>
    public class StoneKind : BlockKind
    {
        static readonly Color StoneBase = new Color(0.84f, 0.8f, 0.77f);
        static readonly Color StoneHigh = new Color(0.89f, 0.86f, 0.83f);
        static readonly Color StoneShade = new Color(0.62f, 0.55f, 0.55f);

        static Material _material;
        static GameConfig _owner;

        public override BoardItemKind Kind => BoardItemKind.Stone;
        public override bool HitByAnyColor => true;
        public override bool ShearMesh => false;
        public override bool CubeLook => true;
        public override float HitPunch => 0.18f;

        /// <summary>Dùng cho dải sáng/tối của đá: rộng hơn cube thường để nhìn sần, không phẳng một màu</summary>
        public override Vector2 ShadeRange(ColorLibrary.Entry entry) => new Vector2(0f, 0.04f);
        public override bool ShowsHpLabel(BoardBlock block) => false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetCache() { _material = null; _owner = null; }

        /// <summary>Dùng cho chọn vật liệu đá: cube trắng nhuộm màu be</summary>
        public override Material Material(GameConfig config, ColorLibrary colors, BoardItemData item)
        {
            if (_material == null || _owner != config)
            {
                _owner = config;
                _material = new Material(colors.CubeMat("w")) { name = "Stone" };
                if (_material.HasProperty("_BaseColor")) _material.SetColor("_BaseColor", StoneBase);
                if (_material.HasProperty("_HColor")) _material.SetColor("_HColor", StoneHigh);
                if (_material.HasProperty("_SColor")) _material.SetColor("_SColor", StoneShade);
                if (_material.HasProperty("_SpecularColor")) _material.SetColor("_SpecularColor", new Color(0.3f, 0.28f, 0.28f));
                if (_material.HasProperty("_SpecularRoughness")) _material.SetFloat("_SpecularRoughness", 0.9f);
            }
            return _material;
        }

        /// <summary>Xử lý dựng hình đá: cube be phủ lớp nứt sần để người chơi nhận ra đây là cơ chế đá</summary>
        public override BlockView Build(BoardController board, BoardItemData item, Transform parent, Vector2 size, System.Collections.Generic.List<RectInt> parts)
        {
            BlockView view = base.Build(board, item, parent, size, parts);
            Renderer rend = view.visual != null ? view.visual.GetComponentInChildren<Renderer>() : null;
            if (rend != null)
            {
                Bounds b = rend.bounds;
                Vector3 front = new Vector3(parent.position.x + (b.center.x - parent.position.x), parent.position.y + (b.center.y - parent.position.y), b.min.z - 0.03f);
                StoneDecal.Create(view.visual, front, Mathf.Max(size.x, size.y) * 0.98f, parent.gameObject.layer);
            }
            return view;
        }

        /// <summary>Xử lý khi đá bị trúng nhưng chưa vỡ: vài mảnh đá nhỏ văng ra và tiếng gõ đá</summary>
        public override void OnHit(BoardBlock block)
        {
            Chips(block, 2);
            AudioManager.Play(Sfx.StoneHit);
        }

        /// <summary>Xử lý văng các mảnh đá nhỏ (khối be) từ vị trí viên đá</summary>
        void Chips(BoardBlock block, int count)
        {
            float cell = Mathf.Max(block.Cells.width, block.Cells.height) * GameManager.Instance.board.CellSize;
            for (int i = 0; i < count; i++)
            {
                GameObject chip = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.Destroy(chip.GetComponent<Collider>());
                chip.name = "StoneChip";
                chip.GetComponent<MeshRenderer>().sharedMaterial = _material;
                chip.layer = block.gameObject.layer;
                chip.transform.SetParent(Vfx.Root, false);
                chip.transform.position = block.transform.position + new Vector3(Random.Range(-0.3f, 0.3f) * cell, Random.Range(-0.3f, 0.3f) * cell, -0.8f);
                chip.transform.rotation = Random.rotation;
                chip.transform.localScale = Vector3.one * cell * Random.Range(0.14f, 0.28f);
                DebrisFx.Fling(chip.transform, block.transform.position, 1.1f);
            }
        }

        /// <summary>Xử lý khi đá vỡ: mảnh đá be văng ra, lộ cube bên dưới</summary>
        public override void OnBroken(BoardController board, BoardBlock block)
        {
            Chips(block, 6);
            AudioManager.Play(Sfx.StoneBreak);
            Vector2Int shift = block.Cells.min - block.OriginCells.min;
            foreach (BoardItemData inner in block.Contents)
            {
                RectInt r = inner.Cells;
                r.position += shift;
                inner.Cells = r;
                board.Spawn(inner);
            }
        }
    }
}
