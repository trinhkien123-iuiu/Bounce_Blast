using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace BounceBlast.EditorTools
{
    /// <summary>Công cụ dọn prefab UI tách từ game gốc</summary>
    public static class UiTidy
    {
        /// <summary>Dùng cho thông tin một node UI</summary>
        class Info
        {
            public readonly Dictionary<Transform, List<Component>> refs = new Dictionary<Transform, List<Component>>();
            public readonly HashSet<string> external = new HashSet<string>();
            public readonly HashSet<Transform> animLocked = new HashSet<Transform>();
        }

        /// <summary>Dùng cho lấy đường dẫn tương đối của node</summary>
        public static string Rel(Transform root, Transform t)
        {
            if (t == root) return "";
            var parts = new List<string>();
            for (Transform q = t; q != null && q != root; q = q.parent) parts.Add(q.name);
            parts.Reverse();
            return string.Join("/", parts);
        }

        static Info Analyse(GameObject root, string prefabPath, Info sceneFrom = null)
        {
            var info = new Info();
            if (sceneFrom != null) foreach (string e in sceneFrom.external) info.external.Add(e);
            foreach (Component c in root.GetComponentsInChildren<Component>(true))
            {
                if (c == null || c is Transform) continue;
                var so = new SerializedObject(c);
                SerializedProperty it = so.GetIterator();
                bool enter = true;
                while (it.Next(enter))
                {
                    enter = true;
                    if (it.propertyType != SerializedPropertyType.ObjectReference || SkipProp(it.propertyPath)) continue;
                    Object o = it.objectReferenceValue;
                    Transform t = o is Component ? ((Component)o).transform : o is GameObject ? ((GameObject)o).transform : null;
                    if (t == null || !t.IsChildOf(root.transform) || t == c.transform) continue;
                    List<Component> list;
                    if (!info.refs.TryGetValue(t, out list)) info.refs[t] = list = new List<Component>();
                    list.Add(c);
                }
            }

            var scene = EditorSceneManager.GetActiveScene();
            if (sceneFrom == null)
            foreach (GameObject g in scene.GetRootGameObjects())
                foreach (Component c in g.GetComponentsInChildren<Component>(true))
                {
                    if (c == null || c is Transform) continue;
                    var so = new SerializedObject(c);
                    SerializedProperty it = so.GetIterator();
                    bool enter = true;
                    while (it.Next(enter))
                    {
                        enter = true;
                        if (it.propertyType != SerializedPropertyType.ObjectReference || SkipProp(it.propertyPath)) continue;
                        Object o = it.objectReferenceValue;
                        GameObject tg = o is Component ? ((Component)o).gameObject : o as GameObject;
                        if (tg == null || EditorUtility.IsPersistent(tg) || !PrefabUtility.IsPartOfPrefabInstance(tg)) continue;
                        GameObject instRoot = PrefabUtility.GetNearestPrefabInstanceRoot(tg);
                        if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(tg) != prefabPath) continue;
                        if (c.transform.IsChildOf(instRoot.transform) && PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(c.gameObject) == prefabPath) continue;
                        info.external.Add(Rel(instRoot.transform, tg.transform));
                    }
                }

            foreach (Animator a in root.GetComponentsInChildren<Animator>(true))
            {
                if (a.runtimeAnimatorController == null) continue;
                foreach (AnimationClip clip in a.runtimeAnimatorController.animationClips)
                {
                    var paths = AnimationUtility.GetCurveBindings(clip).Select(b => b.path)
                        .Concat(AnimationUtility.GetObjectReferenceCurveBindings(clip).Select(b => b.path));
                    foreach (string p in paths)
                    {
                        Transform t = string.IsNullOrEmpty(p) ? a.transform : a.transform.Find(p);
                        for (Transform q = t; q != null && q != a.transform; q = q.parent) info.animLocked.Add(q);
                    }
                }
            }
            return info;
        }

        /// <summary>Trường nội bộ của Unity (liên kết prefab, script…)</summary>
        static bool SkipProp(string path) =>
            path == "m_GameObject" || path == "m_Script" || path == "m_CorrespondingSourceObject" || path == "m_PrefabInstance" || path == "m_PrefabAsset";

        static bool ReferencedFromOutside(Info info, Transform root, Transform t, out string who)
        {
            foreach (var kv in info.refs)
            {
                if (kv.Key != t && !kv.Key.IsChildOf(t)) continue;
                foreach (Component c in kv.Value)
                    if (!c.transform.IsChildOf(t)) { who = c.GetType().Name + "@" + Rel(root, c.transform) + " → " + Rel(root, kv.Key); return true; }
            }
            string rel = Rel(root, t);
            foreach (string e in info.external)
                if (e == rel || e.StartsWith(rel + "/")) { who = "scene → " + e; return true; }
            who = null;
            return false;
        }

        /// <summary>In cây prefab ra file (Temp/UiTidy/&amp;lt;tên&amp;gt;.txt)</summary>
        public static string Dump(string prefabPath, int maxDepth = 99)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                Info info = Analyse(root, prefabPath);
                var sb = new StringBuilder();
                DumpRec(root.transform, root.transform, 0, maxDepth, info, sb);
                Directory.CreateDirectory("Temp/UiTidy");
                string file = "Temp/UiTidy/" + Path.GetFileNameWithoutExtension(prefabPath) + ".txt";
                File.WriteAllText(file, sb.ToString());
                return Path.GetFullPath(file);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        static void DumpRec(Transform root, Transform t, int d, int maxDepth, Info info, StringBuilder sb)
        {
            var comps = t.GetComponents<Component>().Where(c => c != null && !(c is Transform) && !(c is CanvasRenderer)).Select(Short).ToArray();
            string flags = "";
            if (info.refs.ContainsKey(t)) flags += "R";
            if (info.external.Contains(Rel(root, t))) flags += "X";
            if (info.animLocked.Contains(t)) flags += "A";
            if (t != root && IsWrapper(t)) flags += "W";
            var cv = t.GetComponent<Canvas>();
            if (cv != null && t != root && !cv.overrideSorting) flags += "C";
            int n = t.GetComponentsInChildren<Transform>(true).Length - 1;
            sb.Append(new string(' ', d * 2)).Append(t.name).Append(t.gameObject.activeSelf ? "" : " [off]")
              .Append(comps.Length > 0 ? " {" + string.Join(",", comps) + "}" : "")
              .Append(flags.Length > 0 ? " <" + flags + ">" : "")
              .Append(n > 0 ? " (" + n + ")" : "").Append('\n');
            if (d >= maxDepth) return;
            foreach (Transform c in t) DumpRec(root, c, d + 1, maxDepth, info, sb);
        }

        static string Short(Component c)
        {
            var g = c as Graphic;
            string n = c.GetType().Name.Replace("TextMeshProUGUI", "TMP");
            if (c is Behaviour && !((Behaviour)c).enabled) n += "-";
            if (g is Image && ((Image)g).sprite != null) n += ":" + ((Image)g).sprite.name;
            if (g is TMPro.TMP_Text) n += ":\"" + Trunc(((TMPro.TMP_Text)g).text) + "\"";
            return n;
        }

        static string Trunc(string s) { s = (s ?? "").Replace("\n", " "); return s.Length > 18 ? s.Substring(0, 18) + "…" : s; }

        static bool IsWrapper(Transform t)
        {
            var rt = t as RectTransform;
            if (rt == null || t.GetComponents<Component>().Any(c => c == null || !(c is RectTransform) && !(c is CanvasRenderer))) return false;
            return rt.anchorMin == Vector2.zero && rt.anchorMax == Vector2.one && rt.offsetMin == Vector2.zero && rt.offsetMax == Vector2.zero
                && rt.localScale == Vector3.one && rt.localRotation == Quaternion.identity && Mathf.Approximately(rt.localPosition.z, 0f);
        }

        delegate string Op(GameObject root, Info info, string rel);

        static string Run(string prefabPath, string[] rels, Op op)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            var sb = new StringBuilder();
            bool any = false;
            try
            {
                Info first = null;
                foreach (string rel in rels)
                {
                    Info info = Analyse(root, prefabPath, first);
                    if (first == null) first = info;
                    string r = op(root, info, rel);
                    sb.AppendLine(r);
                    if (r.StartsWith("OK")) any = true;
                }
                if (any) PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            return sb.ToString();
        }

        static Transform Find(GameObject root, string rel) => string.IsNullOrEmpty(rel) ? root.transform : root.transform.Find(rel);

        /// <summary>Xử lý xoá node trong prefab</summary>
        public static string Delete(string prefabPath, params string[] rels) => Run(prefabPath, rels, (root, info, rel) =>
        {
            Transform t = Find(root, rel);
            if (t == null) return "THIẾU " + rel;
            if (t == root.transform) return "BỎ QUA gốc";
            string who;
            if (ReferencedFromOutside(info, root.transform, t, out who)) return "CHẶN xoá " + rel + " – đang được tham chiếu: " + who;
            Transform locked = t.GetComponentsInChildren<Transform>(true).FirstOrDefault(info.animLocked.Contains);
            if (locked != null && !t.GetComponentsInChildren<Animator>(true).Any()) return "CHẶN xoá " + rel + " – nằm trên đường hoạt ảnh: " + Rel(root.transform, locked);
            int n = t.GetComponentsInChildren<Transform>(true).Length;
            Object.DestroyImmediate(t.gameObject);
            return "OK xoá " + rel + " (" + n + " object)";
        });

        /// <summary>Xử lý làm phẳng node trong prefab</summary>
        public static string Flatten(string prefabPath, params string[] rels) => Run(prefabPath, rels, (root, info, rel) =>
        {
            Transform w = Find(root, rel);
            if (w == null) return "THIẾU " + rel;
            if (w == root.transform || w.parent == null) return "BỎ QUA gốc";
            if (!w.gameObject.activeSelf) return "CHẶN làm phẳng " + rel + " – đang tắt (bỏ đi thì con sẽ hiện)";
            if (!IsWrapper(w)) return "CHẶN làm phẳng " + rel + " – không phải lớp bọc phủ kín / còn component: " + string.Join(",", w.GetComponents<Component>().Where(c => !(c is Transform)).Select(c => c.GetType().Name));
            if (info.refs.ContainsKey(w)) return "CHẶN làm phẳng " + rel + " – script tham chiếu: " + string.Join(",", info.refs[w].Select(c => c.GetType().Name));
            if (info.external.Contains(rel)) return "CHẶN làm phẳng " + rel + " – scene tham chiếu";
            if (info.animLocked.Contains(w)) return "CHẶN làm phẳng " + rel + " – nằm trên đường hoạt ảnh";
            Transform parent = w.parent;
            int idx = w.GetSiblingIndex();
            var kids = new List<Transform>();
            foreach (Transform c in w) kids.Add(c);
            foreach (Transform c in kids) { c.SetParent(parent, false); c.SetSiblingIndex(idx++); }
            Object.DestroyImmediate(w.gameObject);
            return "OK làm phẳng " + rel + " (" + kids.Count + " con lên " + (parent == root.transform ? "gốc" : parent.name) + ")";
        });

        /// <summary>Xử lý gỡ Canvas lồng nhau trong prefab</summary>
        public static string RemoveNestedCanvas(string prefabPath, params string[] rels) => Run(prefabPath, rels, (root, info, rel) =>
        {
            Transform t = Find(root, rel);
            if (t == null) return "THIẾU " + rel;
            var cv = t.GetComponent<Canvas>();
            if (cv == null || t == root.transform) return "BỎ QUA " + rel + " – không phải Canvas lồng";
            if (cv.overrideSorting) return "CHẶN " + rel + " – Canvas override thứ tự vẽ (cần giữ)";
            var gr = t.GetComponent<GraphicRaycaster>(); if (gr != null) Object.DestroyImmediate(gr);
            var cs = t.GetComponent<CanvasScaler>(); if (cs != null) Object.DestroyImmediate(cs);
            Object.DestroyImmediate(cv);
            return "OK bỏ Canvas lồng " + rel;
        });

        /// <summary>Đổi tên: mỗi phần tử dạng "đường/dẫn=&gt;TênMới"</summary>
        public static string Rename(string prefabPath, params string[] pairs) => Run(prefabPath, pairs, (root, info, pair) =>
        {
            int k = pair.IndexOf("=>");
            if (k < 0) return "SAI cú pháp " + pair;
            string rel = pair.Substring(0, k), name = pair.Substring(k + 2);
            Transform t = Find(root, rel);
            if (t == null) return "THIẾU " + rel;
            if (info.animLocked.Contains(t)) return "CHẶN đổi tên " + rel + " – nằm trên đường hoạt ảnh";
            if (t.parent != null) foreach (Transform s in t.parent) if (s != t && s.name == name) return "CHẶN đổi tên " + rel + " – trùng tên anh em " + name;
            t.name = name;
            return "OK đổi tên " + rel + " → " + name;
        });

        /// <summary>Đặt lại thứ tự / bật tắt / đổi cha thì làm trong Editor</summary>
        public static string Count(string prefabPath)
        {
            GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var all = root.GetComponentsInChildren<Transform>(true);
            int depth = 0;
            foreach (Transform t in all) { int d = 0; for (Transform q = t; q != root.transform; q = q.parent) d++; depth = Mathf.Max(depth, d); }
            return Path.GetFileNameWithoutExtension(prefabPath) + ": " + all.Length + " object, sâu " + depth + ", Canvas " + root.GetComponentsInChildren<Canvas>(true).Length;
        }

        /// <summary>Chụp các trạng thái MenuGame</summary>
        public static string ShotMenu(string tag, int w = 621, int h = 1344)
        {
            var scene = EditorSceneManager.GetActiveScene();
            GameObject ui = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "UI");
            if (ui == null) return "không thấy UI/";
            Transform home = ui.transform.Find("MenuHomeCanvas"), shop = ui.transform.Find("ShopCanvas"), set = ui.transform.Find("MenuSettingsCanvas"), prof = ui.transform.Find("ProfileCanvas");
            var pages = new[] { home, shop, set, prof };
            var saved = pages.Select(p => p.gameObject.activeSelf).ToArray();
            ScrollRect scroll = shop.GetComponentsInChildren<ScrollRect>(true).FirstOrDefault();
            float scrollPos = scroll != null ? scroll.verticalNormalizedPosition : 1f;
            Directory.CreateDirectory("Temp/UiTidy");
            var sb = new StringBuilder();
            try
            {
                Shot(tag, "home", w, h, pages, new[] { true, false, false, false }, null, 0f, sb);
                Shot(tag, "shop_top", w, h, pages, new[] { false, true, false, false }, scroll, 1f, sb);
                Shot(tag, "shop_bottom", w, h, pages, new[] { false, true, false, false }, scroll, 0f, sb);
                Shot(tag, "settings", w, h, pages, new[] { false, false, true, false }, null, 0f, sb);
                Shot(tag, "profile", w, h, pages, new[] { true, false, false, true }, null, 0f, sb);
            }
            finally
            {
                for (int i = 0; i < pages.Length; i++) pages[i].gameObject.SetActive(saved[i]);
                if (scroll != null) { scroll.verticalNormalizedPosition = scrollPos; }
                Canvas.ForceUpdateCanvases();
            }
            return sb.ToString();
        }

        /// <summary>Chụp từng popup của scene Gameplay</summary>
        public static string ShotGame(string tag, int w = 621, int h = 1344)
        {
            var scene = EditorSceneManager.GetActiveScene();
            GameObject ui = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "UI");
            Camera cam = Object.FindObjectsOfType<Camera>(true).FirstOrDefault(c => c.name == "UI Camera");
            if (ui == null || cam == null) return "không thấy UI/ hoặc UI Camera (mở scene Gameplay)";
            Transform popups = ui.transform.Find("Popups");
            Directory.CreateDirectory("Temp/UiTidy");
            var sb = new StringBuilder();
            var all = new List<Transform>();
            foreach (Transform p in popups) all.Add(p);
            var saved = all.ToDictionary(p => p, p => p.gameObject.activeSelf);
            var savedFlags = cam.clearFlags; var savedBg = cam.backgroundColor;
            var restore = new List<System.Action>();
            try
            {
                cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black;
                foreach (Transform p in all)
                {
                    foreach (Transform o in all) o.gameObject.SetActive(o == p);
                    var cg = p.GetComponent<CanvasGroup>();
                    if (cg != null) { float a = cg.alpha; restore.Add(() => cg.alpha = a); cg.alpha = 1f; }
                    if (p.name == "SuccessPopup")
                    {
                        var view = p.GetComponent<BounceBlast.UI.SuccessPopupView>();
                        var vso = new SerializedObject(view);
                        System.Func<string, Transform> Ref = f => { var o = vso.FindProperty(f).objectReferenceValue; return o is Component ? ((Component)o).transform : null; };
                        Transform container = Ref("container"), logoArea = Ref("logoArea"), logo = Ref("logo"), dim = Ref("dim");
                        var objs = new[] { container, logoArea, logo, dim }.Where(x => x != null).ToArray();
                        var st = objs.ToDictionary(x => x, x => x.gameObject.activeSelf);
                        restore.Add(() => { foreach (var kv in st) kv.Key.gameObject.SetActive(kv.Value); });
                        if (container) container.gameObject.SetActive(false); if (logoArea) logoArea.gameObject.SetActive(true); if (logo) logo.gameObject.SetActive(true); if (dim) dim.gameObject.SetActive(true);
                        Render(cam, tag + "_" + p.name + "_logo", w, h, sb);
                        if (container) container.gameObject.SetActive(true); if (logoArea) logoArea.gameObject.SetActive(false); if (dim) dim.gameObject.SetActive(false);
                        Render(cam, tag + "_" + p.name + "_panel", w, h, sb);
                    }
                    else Render(cam, tag + "_" + p.name, w, h, sb);
                }
            }
            finally
            {
                foreach (var r in restore) r();
                foreach (var kv in saved) kv.Key.gameObject.SetActive(kv.Value);
                cam.clearFlags = savedFlags; cam.backgroundColor = savedBg;
                Canvas.ForceUpdateCanvases();
            }
            return sb.ToString();
        }

        /// <summary>Đường dẫn (từ root) của object đầu tiên mang tên name</summary>
        static string FindPath(Transform root, string name)
        {
            Transform t = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == name);
            return t != null ? Rel(root, t) : "__không_có__";
        }

        static void Render(Camera cam, string name, int w, int h, StringBuilder sb)
        {
            Canvas.ForceUpdateCanvases();
            var rt = new RenderTexture(w, h, 24);
            var old = cam.targetTexture;
            cam.targetTexture = rt; cam.Render(); cam.targetTexture = old;
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
            RenderTexture.active = null;
            string file = "Temp/UiTidy/" + name + ".png";
            File.WriteAllBytes(file, tex.EncodeToPNG());
            Object.DestroyImmediate(tex); rt.Release(); Object.DestroyImmediate(rt);
            sb.AppendLine(Path.GetFullPath(file));
        }

        static void Shot(string tag, string state, int w, int h, Transform[] pages, bool[] on, ScrollRect scroll, float scrollPos, StringBuilder sb)
        {
            for (int i = 0; i < pages.Length; i++) pages[i].gameObject.SetActive(on[i]);
            Canvas.ForceUpdateCanvases();
            if (scroll != null) { scroll.verticalNormalizedPosition = scrollPos; Canvas.ForceUpdateCanvases(); }
            Camera cam = Camera.main;
            var rt = new RenderTexture(w, h, 24);
            cam.targetTexture = rt; cam.Render(); cam.targetTexture = null;
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
            RenderTexture.active = null;
            string file = "Temp/UiTidy/" + tag + "_" + state + ".png";
            File.WriteAllBytes(file, tex.EncodeToPNG());
            Object.DestroyImmediate(tex); rt.Release(); Object.DestroyImmediate(rt);
            sb.AppendLine(Path.GetFullPath(file));
        }
    }
}
