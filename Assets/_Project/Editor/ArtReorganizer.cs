// ĐÃ CHẠY XONG (tái cấu trúc Art, Docs/lich-su/tai-cau-truc.md): 1324 asset đã được chuyển, 0 lỗi, 0 GUID mất.
// Giữ lại làm tài liệu cách phân loại (quy tắc FeatureForPrefab / ConfigFieldFeature) và để chạy lại phần PHÂN TÍCH nếu cần,
// nhưng các đường dẫn nguồn (_Ripped, Resources, _Project/Configs…) không còn tồn tại nên chạy lại sẽ cho kết quả sai.
// Tắt mặc định để không ai lỡ bấm "THỰC HIỆN". Muốn bật: thêm BB_ART_REORG vào Scripting Define Symbols.
#if BB_ART_REORG
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BounceBlast.Gameplay;
using UnityEditor;
using UnityEngine;
using BounceBlast.UI;

namespace BounceBlast.EditorTools
{
    /// <summary>
    /// CHẠY KHÔ phân tích tái cấu trúc Art (Docs/lich-su/tai-cau-truc.md, giai đoạn E).
    /// Menu: Tools/Bounce Blast/Art/Phân tích (chạy khô – chỉ đọc).
    ///
    /// Với mỗi asset "art" (mesh, material, texture, sprite, âm thanh, font, animation…) đang được game dùng, xác định TÍNH NĂNG
    /// sở hữu nó (Balls, Board, Ammo, Conveyor, Environment, VFX, UI/…): tính phụ thuộc đệ quy từ các "gốc" của từng tính năng.
    /// Asset thuộc đúng 1 tính năng → Art/&lt;Tính năng&gt;/&lt;Loại&gt;; dùng chung ≥ 2 tính năng → Art/Shared/&lt;Loại&gt;;
    /// không ai tham chiếu → _Archive. KHÔNG di chuyển, KHÔNG sửa file nào: chỉ ghi báo cáo vào Docs/lich-su/tai-cau-truc-art/.
    /// </summary>
    public static class ArtReorganizer
    {
        const string ReportDir = "Docs/lich-su/tai-cau-truc-art";

        // ---- các thư mục nguồn có thể chứa art cần sắp xếp ----
        static readonly string[] SourceRoots = { "Assets/_Ripped", "Assets/Resources/configs", "Assets/Resources/materials", "Assets/_Project/Configs", "Assets/_Project/UI/Sprites" };

        // (4 prefab trước đây tải theo CHUỖI nay đã đi qua GameConfig → được tính tự động qua phụ thuộc của GameConfig.)
        static readonly string[] HardCodedPrefabs = new string[0];

        class Root
        {
            public string Feature;
            public string Label;
            public HashSet<string> Deps = new HashSet<string>();
        }

        class Plan { public List<Row> Rows; public List<Row> Moves; public List<Root> Roots; public HashSet<string> Used; }

        [MenuItem("Tools/Bounce Blast/Art/Phân tích (chạy khô – chỉ đọc)")]
        public static void Analyze()
        {
            Plan plan = BuildPlan();
            WriteReport(plan.Rows, plan.Roots, plan.Used);
            WriteMovesCsv(plan.Moves);
            Debug.Log($"[ArtReorganizer] Xong. {plan.Rows.Count} asset art + {plan.Moves.Count} prefab/cấu hình – báo cáo ở {ReportDir}/ (CHƯA di chuyển file nào).");
        }

        static Plan BuildPlan()
        {
            var roots = new List<Root>();
            var used = new HashSet<string>();      // mọi thứ game thực sự dùng (kể cả prefab, SO)

            // ===== 1) Tập "đang dùng": scene + 3 ScriptableObject cấu hình + 4 prefab tải theo chuỗi =====
            string scenePath = "Assets/_Project/Scenes/Gameplay.unity";
            var entry = new List<string> { scenePath };
            string[] cfgs = { "Assets/_Project/Configs/GameConfig.asset", "Assets/_Project/Configs/UiSkin.asset", "Assets/_Project/Configs/ColorLibrary.asset" };
            foreach (string c in cfgs) if (File.Exists(c)) entry.Add(c);
            entry.AddRange(HardCodedPrefabs.Where(File.Exists));
            foreach (string d in AssetDatabase.GetDependencies(entry.ToArray(), true)) used.Add(d);

            // ===== 2) Gốc theo tính năng =====
            // 2a) mọi prefab đang dùng → tính năng theo tên/thư mục
            foreach (string p in used.Where(u => u.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)))
            {
                string f = FeatureForPrefab(p);
                if (f == null) continue;
                AddRoot(roots, f, p, AssetDatabase.GetDependencies(p, true));
            }

            // 2b) các nhánh của scene (cần mở scene): Conveyor / Environment / ...
            AddSceneRoots(roots, scenePath);

            // 2c) ScriptableObject cấu hình: gán theo TÊN TRƯỜNG ở cấp cao nhất
            AddConfigRoots(roots);

            // ===== 3) Asset KHÔNG dùng: tách "của tính năng chưa làm" (prefab chưa dùng còn trỏ tới) và "mồ côi thật" =====
            var unusedPrefabDeps = new Dictionary<string, HashSet<string>>();   // asset -> các prefab chưa dùng trỏ tới nó
            foreach (string pf in AssetDatabase.FindAssets("t:Prefab").Select(AssetDatabase.GUIDToAssetPath)
                         .Where(pf => pf.StartsWith("Assets/") && !used.Contains(pf)))
                foreach (string d in AssetDatabase.GetDependencies(pf, true))
                {
                    if (!unusedPrefabDeps.TryGetValue(d, out var set)) unusedPrefabDeps[d] = set = new HashSet<string>();
                    set.Add(Path.GetFileNameWithoutExtension(pf));
                }

            // ===== 4) Phân loại từng asset art =====
            var rows = new List<Row>();
            var allArt = AssetDatabase.FindAssets("", SourceRoots.Where(Directory.Exists).ToArray())
                .Select(AssetDatabase.GUIDToAssetPath).Where(p => !AssetDatabase.IsValidFolder(p)).Distinct().ToList();
            foreach (string path in allArt)
            {
                string type = ArtType(path);
                if (type == null) continue;                      // không phải art (TextAsset…)
                var owners = roots.Where(r => r.Deps.Contains(path)).Select(r => r.Feature).Distinct().OrderBy(x => x).ToList();
                var row = new Row { Path = path, Type = type, Owners = string.Join("+", owners) };
                if (!used.Contains(path))
                {
                    bool backlog = unusedPrefabDeps.TryGetValue(path, out var by);
                    row.Status = backlog ? "UnusedBacklog" : "UnusedOrphan";
                    row.Note = backlog ? string.Join(";", by.OrderBy(x => x).Take(4)) : "";
                    row.Target = "Assets/_Archive/" + (backlog ? "TinhNangChuaLam" : "MoCoi") + "/" + RelativeUnder(Path.GetDirectoryName(path).Replace('\\', '/'));
                }
                else
                {
                    row.Status = owners.Count > 1 ? "Shared" : owners.Count == 1 ? "Owned" : "UsedNoOwner";
                    row.Target = TargetFolder(owners, type);
                }
                rows.Add(row);
            }

            return new Plan { Rows = rows, Moves = BuildMoves(used), Roots = roots, Used = used };
        }

        // ------------------------------------------------------------------ prefab + cấu hình (giai đoạn C)
        static List<Row> BuildMoves(HashSet<string> used)
        {
            var moves = new List<Row>();
            // prefab gameplay (Resources/prefabs): đang dùng → Prefabs/Gameplay/<tính năng>; chưa dùng → _Archive
            foreach (string path in Folder("Assets/Resources/prefabs").Where(p => p.EndsWith(".prefab")))
            {
                bool u = used.Contains(path);
                moves.Add(new Row
                {
                    Path = path, Type = "Prefab", Status = u ? "PrefabGameplay" : "PrefabArchive",
                    Owners = FeatureForPrefab(path) ?? "Board",
                    Target = u ? "Assets/_Project/Prefabs/Gameplay/" + (FeatureForPrefab(path) ?? "Board") : "Assets/_Archive/TinhNangChuaLam/Prefabs"
                });
            }
            // prefab UI
            foreach (string path in Folder("Assets/_Project/UI/Prefabs").Where(p => p.EndsWith(".prefab")))
            {
                bool u = used.Contains(path);
                moves.Add(new Row { Path = path, Type = "Prefab", Status = u ? "PrefabUI" : "PrefabArchive", Owners = "UI",
                    Target = u ? "Assets/_Project/Prefabs/UI" : "Assets/_Archive/TinhNangChuaLam/Prefabs" });
            }
            // 3 ScriptableObject cấu hình → Data/
            foreach (string cfg in new[] { "GameConfig", "UiSkin", "ColorLibrary" })
            {
                string path = $"Assets/_Project/Configs/{cfg}.asset";
                if (File.Exists(path)) moves.Add(new Row { Path = path, Type = "Config", Status = "Config", Owners = "Data", Target = "Assets/_Project/Data" });
            }
            return moves;
        }

        static IEnumerable<string> Folder(string dir)
            => Directory.Exists(dir)
                ? AssetDatabase.FindAssets("", new[] { dir }).Select(AssetDatabase.GUIDToAssetPath).Where(p => !AssetDatabase.IsValidFolder(p)).Distinct()
                : Enumerable.Empty<string>();

        static void WriteMovesCsv(List<Row> moves)
        {
            Directory.CreateDirectory(ReportDir);
            var csv = new StringBuilder("status,owners,from,to" + Environment.NewLine);
            foreach (Row r in moves.OrderBy(x => x.Status).ThenBy(x => x.Path))
                csv.Append(r.Status).Append(',').Append(r.Owners).Append(',').Append(Q(r.Path)).Append(',').Append(Q(r.Target + "/" + Path.GetFileName(r.Path))).Append(Environment.NewLine);
            File.WriteAllText(Path.Combine(ReportDir, "prefab_move_plan.csv"), csv.ToString(), new UTF8Encoding(true));
        }

        // ------------------------------------------------------------------ THỰC HIỆN
        /// <summary>
        /// Di chuyển THẬT theo kế hoạch (prefab, cấu hình, art). Dùng AssetDatabase.MoveAsset nên GUID giữ nguyên, mọi tham chiếu không vỡ.
        /// KHÔNG có hộp thoại xác nhận (để chạy được không cần người): chỉ gọi khi đã có checkpoint git.
        /// Ghi nhật ký từng file vào Docs/lich-su/tai-cau-truc-art/apply-log.txt.
        /// </summary>
        [MenuItem("Tools/Bounce Blast/Art/THỰC HIỆN chuyển (cần checkpoint git)")]
        public static void Apply()
        {
            Plan plan = BuildPlan();
            var all = plan.Moves.Concat(plan.Rows).ToList();
            var log = new StringBuilder();
            int moved = 0, same = 0; var errors = new List<string>();
            foreach (Row r in all)
            {
                string dest = r.Target + "/" + Path.GetFileName(r.Path);
                if (string.Equals(r.Path, dest, StringComparison.Ordinal)) { same++; continue; }
                EnsureFolder(r.Target);
                string err = AssetDatabase.ValidateMoveAsset(r.Path, dest);
                if (!string.IsNullOrEmpty(err)) { errors.Add(r.Path + " -> " + err); log.AppendLine("LOI  " + r.Path + " : " + err); continue; }
                err = AssetDatabase.MoveAsset(r.Path, dest);
                if (!string.IsNullOrEmpty(err)) { errors.Add(r.Path + " -> " + err); log.AppendLine("LOI  " + r.Path + " : " + err); continue; }
                moved++;
                log.AppendLine("OK   " + r.Path + "  ->  " + dest);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Directory.CreateDirectory(ReportDir);
            File.WriteAllText(Path.Combine(ReportDir, "apply-log.txt"), log.ToString(), new UTF8Encoding(true));
            Debug.Log($"[ArtReorganizer] THỰC HIỆN xong: đã chuyển {moved}, giữ nguyên {same}, LỖI {errors.Count}. Nhật ký: {ReportDir}/apply-log.txt");
            foreach (string e in errors.Take(10)) Debug.LogWarning("[ArtReorganizer] " + e);
        }

        static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            string[] parts = folder.Split('/');
            string cur = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = cur + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(cur, parts[i]);
                cur = next;
            }
        }

        class Row { public string Path, Type, Status, Owners, Target, Note; }

        /// <summary>Thư mục đích cho asset ĐANG DÙNG. Một chủ → thư mục của tính năng; nhiều chủ → Shared, hoặc UI/Common nếu chủ đều là UI.</summary>
        static string TargetFolder(List<string> owners, string type)
        {
            string feat;
            if (owners.Count == 1) feat = owners[0];
            else if (owners.Count > 1 && owners.All(o => o.StartsWith("UI/"))) feat = "UI/Common";
            else feat = "Shared";
            if (feat == "Audio") return "Assets/_Project/Audio";                       // âm thanh không cần thư mục loại
            if (feat == "Shared/Colors") return "Assets/_Project/Art/Shared/Colors";   // bảng màu: gom 1 chỗ, không chia theo loại
            return $"Assets/_Project/Art/{feat}/{type}";
        }

        // ------------------------------------------------------------------ gốc
        static void AddRoot(List<Root> roots, string feature, string label, IEnumerable<string> deps)
        {
            var r = roots.FirstOrDefault(x => x.Feature == feature && x.Label == label);
            if (r == null) { r = new Root { Feature = feature, Label = label }; roots.Add(r); }
            foreach (string d in deps) r.Deps.Add(d);
        }

        /// <summary>Tính năng của một prefab theo tên / thư mục (quy tắc đơn giản, báo cáo sẽ cho duyệt lại).</summary>
        static string FeatureForPrefab(string p)
        {
            string n = Path.GetFileNameWithoutExtension(p);
            if (p.StartsWith("Assets/_Project/UI/Prefabs/"))
            {
                switch (n)
                {
                    case "OverlayCanvas": case "BoosterBar": return "UI/Booster";
                    case "GameCanvas": return "UI/Hud";
                    case "CapacityBar": return "UI/Capacity";
                    case "SuccessCanvas": return "UI/Win";
                    case "FailCanvas": case "SettingsCanvas": case "OutOfCapacityPopup": case "OutOfCapacityPanel": return "UI/Popup";
                    default: return "UI/Common";
                }
            }
            if (!p.StartsWith("Assets/Resources/prefabs/")) return null;
            if (n.StartsWith("Ball") || n == "CrushedBall" || n == "SlicedBall") return "Balls";
            if (n.StartsWith("Ammunation") || n.StartsWith("BlockerAmmo") || n.Contains("AmmoWall")) return "Ammo";
            if (n.Contains("Explosion") || n.Contains("Particle")) return "VFX";
            return "Board";
        }

        static void AddSceneRoots(List<Root> roots, string scenePath)
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != scenePath) { Debug.LogWarning("[ArtReorganizer] Hãy mở scene " + scenePath + " rồi chạy lại để tính các nhánh của scene."); return; }
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                {
                    string f = FeatureForSceneNode(PathOf(t));
                    if (f == null) continue;
                    var deps = EditorUtility.CollectDependencies(new UnityEngine.Object[] { t.gameObject })
                        .Select(AssetDatabase.GetAssetPath).Where(s => !string.IsNullOrEmpty(s));
                    AddRoot(roots, f, "scene:" + PathOf(t), deps);
                }
            }
        }

        /// <summary>Chỉ khớp ở các nút "cấp nhánh" để không gán trùng cho mọi con.</summary>
        static string FeatureForSceneNode(string path)
        {
            switch (path)
            {
                case "Level/Board - Default/Conveyor_Container": return "Conveyor";
                case "Level/Board - Default/Crushers": return "Conveyor";
                case "Level/Board - Default/Environment Base (1)": return "Environment";
                case "Level/Board - Default/Gradient_Back": return "Environment";
                default: return null;
            }
        }

        static string PathOf(Transform t)
        {
            string p = t.name;
            for (Transform q = t.parent; q != null; q = q.parent) p = q.name + "/" + p;
            return p;
        }

        // Tên trường cấp cao của GameConfig → tính năng. Trường không có trong bảng → bỏ qua (asset rơi vào "Shared" nếu vẫn được dùng).
        static readonly Dictionary<string, string> ConfigFieldFeature = new Dictionary<string, string>
        {
            { "cubePrefab", "Board" }, { "combinedCubePrefab", "Board" }, { "cratePrefab", "Board" }, { "wallMaterial", "Board" },
            { "crateMaterial", "Board" }, { "iceMaterial", "Board" }, { "qmarkMaterial", "Board" }, { "worldFont", "Board" },
            { "ammoPrefab", "Ammo" }, { "tunnelPrefab", "Ammo" }, { "ammoCellMaterial", "Ammo" }, { "ammoFrameMaterial", "Ammo" },
            { "ballPrefab", "Balls" }, { "trailMaterial", "Balls" },
            { "cubeBreakVfx", "VFX" }, { "crateBreakVfx", "VFX" }, { "ammoLaunchVfx", "VFX" }, { "confettiVfx", "VFX" },
            { "normalTheme", "Environment" }, { "hardTheme", "Environment" }, { "superHardTheme", "Environment" },
            { "sounds", "Audio" }, { "musicNormal", "Audio" }, { "musicHard", "Audio" }, { "musicSuperHard", "Audio" },
        };

        static void AddConfigRoots(List<Root> roots)
        {
            var cfg = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/_Project/Configs/GameConfig.asset");
            if (cfg != null)
            {
                var so = new SerializedObject(cfg);
                var it = so.GetIterator();
                bool enter = true;
                while (it.NextVisible(enter))
                {
                    enter = false;
                    if (!ConfigFieldFeature.TryGetValue(it.name, out string feat)) continue;
                    foreach (UnityEngine.Object o in ReferencesUnder(it))
                    {
                        string ap = AssetDatabase.GetAssetPath(o);
                        if (string.IsNullOrEmpty(ap)) continue;
                        AddRoot(roots, feat, "GameConfig." + it.name, AssetDatabase.GetDependencies(ap, true));
                    }
                }
            }
            // ColorLibrary: bảng material màu dùng cho gạch, hộp và bi → gom riêng vào Shared/Colors
            var colors = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/_Project/Configs/ColorLibrary.asset");
            if (colors != null)
                AddRoot(roots, "Shared/Colors", "ColorLibrary", AssetDatabase.GetDependencies("Assets/_Project/Configs/ColorLibrary.asset", true));
            // UiSkin: font + sprite thanh sức chứa gắn theo tên; còn lại đã được tính qua prefab UI
            var skin = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/_Project/Configs/UiSkin.asset");
            if (skin != null)
            {
                var so = new SerializedObject(skin);
                var it = so.GetIterator();
                bool enter = true;
                while (it.NextVisible(enter))
                {
                    enter = false;
                    string feat = it.name.StartsWith("capacity") ? "UI/Capacity"
                                : it.name.StartsWith("popup") || it.name.EndsWith("Button") || it.name == "sadFace" || it.name == "brokenHeart" ? "UI/Popup"
                                : it.name == "font" || it.name == "fontOutlineMaterial" ? "UI/Common" : null;
                    if (feat == null) continue;
                    foreach (UnityEngine.Object o in ReferencesUnder(it))
                    {
                        string ap = AssetDatabase.GetAssetPath(o);
                        if (!string.IsNullOrEmpty(ap)) AddRoot(roots, feat, "UiSkin." + it.name, AssetDatabase.GetDependencies(ap, true));
                    }
                }
            }
        }

        static IEnumerable<UnityEngine.Object> ReferencesUnder(SerializedProperty prop)
        {
            if (prop.propertyType == SerializedPropertyType.ObjectReference)
            {
                if (prop.objectReferenceValue != null) yield return prop.objectReferenceValue;
                yield break;
            }
            if (!prop.hasVisibleChildren) yield break;
            var copy = prop.Copy();
            var end = copy.GetEndProperty();
            bool enter = true;
            while (copy.NextVisible(enter) && !SerializedProperty.EqualContents(copy, end))
            {
                enter = true;
                if (copy.propertyType == SerializedPropertyType.ObjectReference && copy.objectReferenceValue != null)
                    yield return copy.objectReferenceValue;
            }
        }

        // ------------------------------------------------------------------ phân loại
        /// <summary>Loại asset art, null nếu không phải art.</summary>
        static string ArtType(string path)
        {
            if (path.EndsWith(".cs") || path.EndsWith(".asmdef") || path.EndsWith(".meta")) return null;
            Type t = AssetDatabase.GetMainAssetTypeAtPath(path);
            if (t == null) return null;
            if (typeof(Mesh).IsAssignableFrom(t)) return "Meshes";
            if (typeof(Material).IsAssignableFrom(t)) return "Materials";
            if (typeof(Shader).IsAssignableFrom(t)) return "Shaders";
            if (typeof(Sprite).IsAssignableFrom(t)) return "Sprites";
            if (typeof(Texture).IsAssignableFrom(t)) return "Textures";
            if (typeof(AudioClip).IsAssignableFrom(t)) return "Audio";
            if (typeof(Font).IsAssignableFrom(t) || t.Name.Contains("TMP_FontAsset")) return "Fonts";
            if (typeof(AnimationClip).IsAssignableFrom(t) || typeof(RuntimeAnimatorController).IsAssignableFrom(t)) return "Animation";
            if (t.Name.Contains("PhysicMaterial") || t.Name.Contains("PhysicsMaterial")) return "Physics";
            if (t.Name.Contains("AudioMixer")) return "Audio";
            return null;
        }

        static string RelativeUnder(string path)
        {
            string p = path.StartsWith("Assets/") ? path.Substring("Assets/".Length) : path;
            return p;
        }

        // ------------------------------------------------------------------ báo cáo
        static void WriteReport(List<Row> rows, List<Root> roots, HashSet<string> used)
        {
            Directory.CreateDirectory(ReportDir);

            // CSV đầy đủ để lọc/duyệt
            var csv = new StringBuilder("status,type,owners,note,from,to\n");
            foreach (Row r in rows.OrderBy(x => x.Status).ThenBy(x => x.Owners).ThenBy(x => x.Type).ThenBy(x => x.Path))
                csv.Append(r.Status).Append(',').Append(r.Type).Append(',').Append(r.Owners).Append(',').Append(Q(r.Note ?? "")).Append(',')
                   .Append(Q(r.Path)).Append(',').Append(Q(r.Target + "/" + Path.GetFileName(r.Path))).Append('\n');
            File.WriteAllText(Path.Combine(ReportDir, "art_move_plan.csv"), csv.ToString(), new UTF8Encoding(true));

            // Tóm tắt dễ đọc
            var sb = new StringBuilder();
            sb.AppendLine("# Báo cáo chạy khô – Art theo tính năng");
            sb.AppendLine();
            sb.AppendLine("> Sinh tự động bởi `Tools/Bounce Blast/Art/Phân tích (chạy khô – chỉ đọc)`. **Chưa di chuyển file nào.**");
            sb.AppendLine("> Danh sách đầy đủ từng file: `art_move_plan.csv` (cột `from` → `to`).");
            sb.AppendLine();
            sb.AppendLine($"Tổng asset art đã xét: **{rows.Count}**");
            sb.AppendLine();
            sb.AppendLine("## Theo trạng thái");
            sb.AppendLine("| Trạng thái | Số file | Ý nghĩa |");
            sb.AppendLine("|---|---:|---|");
            sb.AppendLine($"| Owned | {rows.Count(r => r.Status == "Owned")} | Thuộc đúng 1 tính năng → `Art/<Tính năng>/<Loại>` |");
            sb.AppendLine($"| Shared | {rows.Count(r => r.Status == "Shared")} | Dùng bởi ≥ 2 tính năng → `Art/Shared/<Loại>` |");
            sb.AppendLine($"| UsedNoOwner | {rows.Count(r => r.Status == "UsedNoOwner")} | Game dùng nhưng không gốc nào của tính năng chạm tới (vd chỉ qua config) → `Art/Shared/<Loại>` |");
            sb.AppendLine($"| UnusedBacklog | {rows.Count(r => r.Status == "UnusedBacklog")} | Chưa dùng nhưng prefab của TÍNH NĂNG CHƯA LÀM còn trỏ tới (bom, khoá, xích, keo…) → `_Archive/TinhNangChuaLam` |");
            sb.AppendLine($"| UnusedOrphan | {rows.Count(r => r.Status == "UnusedOrphan")} | Mồ côi thật sự, không prefab/asset nào trỏ tới → `_Archive/MoCoi` |");
            sb.AppendLine();
            sb.AppendLine("## Theo tính năng (chỉ asset đang dùng)");
            sb.AppendLine("| Tính năng | Tổng | " + string.Join(" | ", Types(rows)) + " |");
            sb.AppendLine("|---|---:|" + string.Join("", Types(rows).Select(_ => "---:|")));
            var feats = rows.Where(r => !r.Status.StartsWith("Unused"))
                .GroupBy(r => FeatureLabel(r.Target))
                .OrderBy(g => g.Key);
            foreach (var g in feats)
                sb.AppendLine($"| {g.Key} | {g.Count()} | " + string.Join(" | ", Types(rows).Select(t => g.Count(r => r.Type == t).ToString())) + " |");
            sb.AppendLine();
            sb.AppendLine("## Asset không dùng, theo thư mục nguồn");
            sb.AppendLine("| Thư mục | Số file |");
            sb.AppendLine("|---|---:|");
            foreach (var g in rows.Where(r => r.Status.StartsWith("Unused")).GroupBy(r => string.Join("/", r.Path.Split('/').Take(3))).OrderByDescending(g => g.Count()))
                sb.AppendLine($"| `{g.Key}` | {g.Count()} |");
            sb.AppendLine();
            sb.AppendLine("## Gốc đã dùng để phân loại");
            sb.AppendLine("| Tính năng | Gốc | Số phụ thuộc |");
            sb.AppendLine("|---|---|---:|");
            foreach (Root r in roots.OrderBy(x => x.Feature).ThenBy(x => x.Label)) sb.AppendLine($"| {r.Feature} | `{r.Label}` | {r.Deps.Count} |");
            File.WriteAllText(Path.Combine(ReportDir, "bao-cao-tom-tat.md"), sb.ToString(), new UTF8Encoding(true));
        }

        /// <summary>Nhãn tính năng để tóm tắt, suy ra từ thư mục đích.</summary>
        static string FeatureLabel(string target)
        {
            if (target.StartsWith("Assets/_Project/Audio")) return "Audio";
            string rest = target.Replace("Assets/_Project/Art/", "");
            string[] parts = rest.Split('/');
            return (parts[0] == "UI" || parts[0] == "Shared") && parts.Length > 1 && (parts[0] == "UI" || parts[1] == "Colors")
                ? parts[0] + "/" + parts[1] : parts[0];
        }

        static List<string> Types(List<Row> rows) => rows.Select(r => r.Type).Distinct().OrderBy(x => x).ToList();
        static string Q(string s) => "\"" + s.Replace("\"", "\"\"") + "\"";
    }
}
#endif
