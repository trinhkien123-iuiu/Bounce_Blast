using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using BounceBlast.Gameplay;

namespace BounceBlast.WallTools
{
    /// <summary>Gộp từng biến thể của prefab tường gốc thành 1 mesh (từ wall_variants.json do script rip xuất) và lưu vào WallKit.asset.</summary>
    public static class WallKitBaker
    {
        const string Dir = "Assets/_Project/Art/Ammo/Meshes/Wall";


        [MenuItem("Tools/Bounce Blast/Bake Wall Kit")]
        public static void Bake()
        {
            string json = File.ReadAllText("Assets/_Project/Editor/WallKit/wall_variants.json");
            var root = (Dictionary<string, object>)MiniJson(json);
            var variants = (Dictionary<string, object>)root["variants"];
            var cache = new Dictionary<string, Mesh>();
            var keys = new List<string>(); var meshes = new List<Mesh>();
            string kitPath = Dir + "/WallKit.asset";
            var kit = AssetDatabase.LoadAssetAtPath<WallKit>(kitPath);   // giữ asset cũ (GUID) để GameConfig không mất tham chiếu
            if (kit == null) { kit = ScriptableObject.CreateInstance<WallKit>(); AssetDatabase.CreateAsset(kit, kitPath); }
            else foreach (Object o in AssetDatabase.LoadAllAssetRepresentationsAtPath(kitPath)) Object.DestroyImmediate(o, true);
            foreach (var kv in variants)
            {
                var list = (List<object>)kv.Value;
                var v = (Dictionary<string, object>)list[0];
                var combine = new List<CombineInstance>();
                foreach (Dictionary<string, object> p in (List<object>)v["parts"])
                {
                    string name = (string)p["mesh"];
                    if (!cache.TryGetValue(name, out Mesh src)) cache[name] = src = AssetDatabase.LoadAssetAtPath<Mesh>(Dir + "/" + name);
                    var l = (List<object>)p["m"]; var m = new Matrix4x4();
                    for (int i = 0; i < 16; i++) m[i / 4, i % 4] = System.Convert.ToSingle(l[i]);
                    for (int s = 0; s < src.subMeshCount; s++) combine.Add(new CombineInstance { mesh = src, subMeshIndex = s, transform = m });
                }
                var mesh = new Mesh { name = "Wall_" + kv.Key };
                mesh.CombineMeshes(combine.ToArray(), true, true);
                if (kv.Key == "") FitToCell(mesh);   // 4xRounded trong prefab gốc có cỡ 2 ô, lệch tâm → đưa về 1 ô như các biến thể khác
                mesh.RecalculateBounds();
                AssetDatabase.AddObjectToAsset(mesh, kit);
                keys.Add(kv.Key); meshes.Add(mesh);
            }
            kit.keys = keys.ToArray(); kit.meshes = meshes.ToArray();
            EditorUtility.SetDirty(kit); AssetDatabase.SaveAssets();
            Debug.Log("WallKit: " + keys.Count + " bien the");
        }

        static void FitToCell(Mesh mesh)
        {
            Bounds b = mesh.bounds;
            float k = 1f / Mathf.Max(b.size.x, b.size.z);
            var v = mesh.vertices;
            for (int i = 0; i < v.Length; i++) v[i] = (v[i] - b.center) * k;
            mesh.vertices = v;
        }

        // JSON tối giản (object/array/number/string) – tránh phụ thuộc thư viện
        static object MiniJson(string s) { int i = 0; return Val(s, ref i); }
        static void Ws(string s, ref int i) { while (char.IsWhiteSpace(s[i])) i++; }
        static object Val(string s, ref int i)
        {
            Ws(s, ref i); char c = s[i];
            if (c == '{')
            {
                i++; var d = new Dictionary<string, object>();
                for (Ws(s, ref i); s[i] != '}'; Ws(s, ref i))
                {
                    string k = (string)Val(s, ref i); Ws(s, ref i); i++; d[k] = Val(s, ref i); Ws(s, ref i);
                    if (s[i] == ',') i++;
                }
                i++; return d;
            }
            if (c == '[')
            {
                i++; var l = new List<object>();
                for (Ws(s, ref i); s[i] != ']'; Ws(s, ref i)) { l.Add(Val(s, ref i)); Ws(s, ref i); if (s[i] == ',') i++; }
                i++; return l;
            }
            if (c == '"') { int e = s.IndexOf('"', i + 1); string r = s.Substring(i + 1, e - i - 1); i = e + 1; return r; }
            int st = i; while (i < s.Length && ",]} \n\r\t".IndexOf(s[i]) < 0) i++;
            return double.Parse(s.Substring(st, i - st), System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
