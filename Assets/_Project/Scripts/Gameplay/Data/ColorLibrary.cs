using System;
using System.Collections.Generic;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>Bảng màu của game: mã màu trong CSV → material gốc</summary>
    [CreateAssetMenu(menuName = "Bounce Blast/Color Library")]
    public class ColorLibrary : ScriptableObject
    {
        /// <summary>Dùng cho một màu và vật liệu của nó</summary>
        [Serializable]
        public class Entry
        {
            public string code;
            public string name;
            public Color color = Color.white;
            public Material cubeMaterial;
            public Material ammoMaterial;
            public Material ballMaterial;
            [Tooltip("Dải _CubeShadeOffset ngẫu nhiên cho từng cube (Colors_v3.txt) – tạo vân sáng/tối như game gốc")]
            public Vector2 shadeOffset = new Vector2(-0.05f, 0.05f);
        }

        public List<Entry> entries = new List<Entry>();
        public Material fallbackCube;
        public Material fallbackAmmo;
        public Material fallbackBall;

        Dictionary<string, Entry> _lookup;

        /// <summary>Dùng cho lấy mục màu theo mã</summary>
        public Entry Get(string code)
        {
            if (_lookup == null || _lookup.Count != entries.Count)
            {
                _lookup = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);
                foreach (Entry e in entries) _lookup[e.code] = e;
            }
            return code != null && _lookup.TryGetValue(code, out Entry entry) ? entry : null;
        }

        /// <summary>Dùng cho lấy màu theo mã</summary>
        public Color ColorOf(string code) => Get(code)?.color ?? Color.white;
        /// <summary>Dùng cho lấy vật liệu khối theo mã màu</summary>
        public Material CubeMat(string code) => Get(code)?.cubeMaterial ?? fallbackCube;
        /// <summary>Dùng cho lấy vật liệu hộp theo mã màu</summary>
        public Material AmmoMat(string code) => Get(code)?.ammoMaterial ?? fallbackAmmo;
        /// <summary>Dùng cho lấy vật liệu bi theo mã màu</summary>
        public Material BallMat(string code) => Get(code)?.ballMaterial ?? fallbackBall;
    }
}
