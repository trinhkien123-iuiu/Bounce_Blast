using System;
using System.Collections.Generic;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>
    /// Bảng màu của game: mã màu trong CSV → material gốc (rip) cho cube / hộp đạn / bi.
    /// Asset được sinh bởi menu Tools/Bounce Blast/Setup Gameplay Scene.
    /// </summary>
    [CreateAssetMenu(menuName = "Bounce Blast/Color Library")]
    public class ColorLibrary : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public string code;           // g, r, b, ...
            public string name;           // Green, Red, ...
            public Color color = Color.white;
            public Material cubeMaterial; // BounceBlast/CubeIsolatedOptimized
            public Material ammoMaterial; // TCP2 Directional Cube (Outline)
            public Material ballMaterial; // TCP2 Directional Cube
            [Tooltip("Dải _CubeShadeOffset ngẫu nhiên cho từng cube (Colors_v3.txt) – tạo vân sáng/tối như game gốc")]
            public Vector2 shadeOffset = new Vector2(-0.05f, 0.05f);
        }

        public List<Entry> entries = new List<Entry>();
        public Material fallbackCube;
        public Material fallbackAmmo;
        public Material fallbackBall;

        Dictionary<string, Entry> _lookup;

        public Entry Get(string code)
        {
            if (_lookup == null || _lookup.Count != entries.Count)
            {
                _lookup = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);
                foreach (Entry e in entries) _lookup[e.code] = e;
            }
            return code != null && _lookup.TryGetValue(code, out Entry entry) ? entry : null;
        }

        public Color ColorOf(string code) => Get(code)?.color ?? Color.white;
        public Material CubeMat(string code) => Get(code)?.cubeMaterial ?? fallbackCube;
        public Material AmmoMat(string code) => Get(code)?.ammoMaterial ?? fallbackAmmo;
        public Material BallMat(string code) => Get(code)?.ballMaterial ?? fallbackBall;
    }
}
