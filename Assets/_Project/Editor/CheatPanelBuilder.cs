using BounceBlast.Gameplay;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using BounceBlast.Cheats;

namespace BounceBlast.EditorTools
{
    /// <summary>Dựng prefab CheatPanel (UI công cụ cheat) bằng code Editor</summary>
    public static class CheatPanelBuilder
    {
        const string PrefabPath = "Assets/_Project/Prefabs/UI/CheatPanel.prefab";
        static readonly Color Dark = new Color(0.08f, 0.09f, 0.17f, 0.96f), Mid = new Color(0.3f, 0.32f, 0.5f), Accent = new Color(0.98f, 0.74f, 0.1f);

        /// <summary>Dựng prefab bảng cheat</summary>
        [MenuItem("Tools/Bounce Blast/Cheat/Build panel prefab")]
        public static void BuildPrefab()
        {
            var root = new GameObject("CheatPanel", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            TMP_Text fps = Text(root.transform, "FpsOverlay", "", 28, TextAlignmentOptions.TopLeft, Color.yellow);
            Stretch(fps.rectTransform, new Vector2(0f, 1f), new Vector2(0.75f, 1f), new Vector2(24f, -200f), new Vector2(0f, -24f));
            fps.raycastTarget = false;
            fps.gameObject.SetActive(false);

            var panel = NewRect(root.transform, "Panel");
            Stretch(panel, Vector2.zero, Vector2.one, new Vector2(40f, 120f), new Vector2(-40f, -120f));
            var bg = panel.gameObject.AddComponent<Image>(); bg.color = Dark;
            var blocker = panel.gameObject.AddComponent<Button>();
            blocker.transition = Selectable.Transition.None;

            TMP_Text title = Text(panel, "Title", "CHEATS  (dev only)", 44, TextAlignmentOptions.Left, Accent);
            Stretch(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(30f, -110f), new Vector2(-170f, -10f));
            Button close = ButtonRow(panel, "CloseButton", "X", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-150f, -100f), new Vector2(-20f, -20f), out _, 44);

            var tabBar = NewRect(panel, "Tabs");
            Stretch(tabBar, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -230f), new Vector2(-20f, -120f));
            var tabLayout = tabBar.gameObject.AddComponent<HorizontalLayoutGroup>();
            tabLayout.spacing = 12f; tabLayout.childForceExpandWidth = true; tabLayout.childForceExpandHeight = true; tabLayout.childControlWidth = true; tabLayout.childControlHeight = true;
            var tabs = new Button[CheatRegistry.Groups.Length];
            var tabLabels = new TMP_Text[CheatRegistry.Groups.Length];
            for (int i = 0; i < tabs.Length; i++)
                tabs[i] = TabButton(tabBar, "Tab" + i, CheatRegistry.Groups[i], out tabLabels[i]);

            var scrollGo = NewRect(panel, "Scroll");
            Stretch(scrollGo, Vector2.zero, Vector2.one, new Vector2(20f, 20f), new Vector2(-20f, -250f));
            var scroll = scrollGo.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Elastic; scroll.scrollSensitivity = 40f;
            var viewport = NewRect(scrollGo, "Viewport");
            Stretch(viewport, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            viewport.gameObject.AddComponent<RectMask2D>();
            var vpImg = viewport.gameObject.AddComponent<Image>(); vpImg.color = new Color(0f, 0f, 0f, 0.01f);
            var content = NewRect(viewport, "Content");
            content.anchorMin = new Vector2(0f, 1f); content.anchorMax = new Vector2(1f, 1f); content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = new Vector2(0f, 0f); content.offsetMax = new Vector2(0f, 0f);
            var vl = content.gameObject.AddComponent<VerticalLayoutGroup>();
            vl.spacing = 16f; vl.padding = new RectOffset(0, 0, 10, 10); vl.childControlWidth = true; vl.childControlHeight = true;
            vl.childForceExpandWidth = true; vl.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport; scroll.content = content;

            CheatButtonRow btnRow = ButtonTemplate(panel);
            CheatStepperRow stepRow = StepperTemplate(panel);

            var overlay = root.AddComponent<CheatPerfOverlay>();
            Set(overlay, "label", fps);
            var view = root.AddComponent<CheatPanelView>();
            Set(view, "panelRoot", panel.gameObject);
            Set(view, "closeButton", close);
            SetArray(view, "tabButtons", tabs);
            SetArray(view, "tabLabels", tabLabels);
            Set(view, "content", content);
            Set(view, "scroll", scroll);
            Set(view, "buttonTemplate", btnRow);
            Set(view, "stepperTemplate", stepRow);
            var host = root.AddComponent<CheatHost>();
            Set(host, "panel", view);
            Set(host, "overlay", overlay);

            System.IO.Directory.CreateDirectory("Assets/_Project/Prefabs/UI");
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.Refresh();
            Debug.Log("[Cheat] Đã dựng " + PrefabPath);
        }

        /// <summary>Xử lý đặt bảng cheat vào scene</summary>
        [MenuItem("Tools/Bounce Blast/Cheat/Place panel in open scene")]
        public static void PlaceInScene()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null) { Debug.LogError("[Cheat] Chưa có prefab – chạy 'Build panel prefab' trước."); return; }
            foreach (var h in Object.FindObjectsOfType<CheatHost>(true)) { Debug.Log("[Cheat] Scene đã có CheatPanel – bỏ qua."); return; }
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            inst.name = "CheatPanel";
            EditorSceneManager.MarkSceneDirty(inst.scene);
            Debug.Log("[Cheat] Đã đặt CheatPanel vào scene " + inst.scene.name + " – nhớ lưu scene.");
        }

        static RectTransform NewRect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        static void Stretch(RectTransform rt, Vector2 amin, Vector2 amax, Vector2 offMin, Vector2 offMax)
        {
            rt.anchorMin = amin; rt.anchorMax = amax; rt.offsetMin = offMin; rt.offsetMax = offMax;
        }

        static TMP_Text Text(Transform parent, string name, string text, float size, TextAlignmentOptions align, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<TextMeshProUGUI>();
            t.text = text; t.fontSize = size; t.alignment = align; t.color = color; t.enableWordWrapping = false; t.raycastTarget = false;
            return t;
        }

        static Button ButtonRow(Transform parent, string name, string label, Vector2 amin, Vector2 amax, Vector2 offMin, Vector2 offMax, out TMP_Text labelText, float fontSize)
        {
            RectTransform rt = NewRect(parent, name);
            Stretch(rt, amin, amax, offMin, offMax);
            var img = rt.gameObject.AddComponent<Image>(); img.color = Mid;
            var btn = rt.gameObject.AddComponent<Button>(); btn.targetGraphic = img;
            labelText = Text(rt, "Label", label, fontSize, TextAlignmentOptions.Center, Color.white);
            Stretch(labelText.rectTransform, Vector2.zero, Vector2.one, new Vector2(8f, 4f), new Vector2(-8f, -4f));
            return btn;
        }

        static Button TabButton(Transform parent, string name, string label, out TMP_Text labelText)
        {
            RectTransform rt = NewRect(parent, name);
            var img = rt.gameObject.AddComponent<Image>(); img.color = Mid;
            var btn = rt.gameObject.AddComponent<Button>(); btn.targetGraphic = img;
            labelText = Text(rt, "Label", label, 38, TextAlignmentOptions.Center, Color.white);
            Stretch(labelText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return btn;
        }

        static CheatButtonRow ButtonTemplate(Transform parent)
        {
            RectTransform rt = NewRect(parent, "ButtonRowTemplate");
            var img = rt.gameObject.AddComponent<Image>(); img.color = Mid;
            var btn = rt.gameObject.AddComponent<Button>(); btn.targetGraphic = img;
            var le = rt.gameObject.AddComponent<LayoutElement>(); le.preferredHeight = 110f; le.minHeight = 110f;
            TMP_Text label = Text(rt, "Label", "Button", 36, TextAlignmentOptions.Left, Color.white);
            Stretch(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(30f, 4f), new Vector2(-20f, -4f));
            var row = rt.gameObject.AddComponent<CheatButtonRow>();
            row.button = btn; row.label = label; row.background = img;
            rt.gameObject.SetActive(false);
            return row;
        }

        static CheatStepperRow StepperTemplate(Transform parent)
        {
            RectTransform rt = NewRect(parent, "StepperRowTemplate");
            var img = rt.gameObject.AddComponent<Image>(); img.color = new Color(0.2f, 0.22f, 0.38f);
            var le = rt.gameObject.AddComponent<LayoutElement>(); le.preferredHeight = 200f; le.minHeight = 200f;
            var row = rt.gameObject.AddComponent<CheatStepperRow>();
            row.label = Text(rt, "Label", "Stepper", 36, TextAlignmentOptions.Left, Color.white);
            Stretch(row.label.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(30f, -70f), new Vector2(-20f, -8f));

            var bar = NewRect(rt, "Bar");
            Stretch(bar, Vector2.zero, new Vector2(1f, 0f), new Vector2(16f, 12f), new Vector2(-16f, 118f));
            var hl = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 10f; hl.childForceExpandWidth = true; hl.childForceExpandHeight = true; hl.childControlWidth = true; hl.childControlHeight = true;
            TMP_Text dummy;
            row.minus10 = Small(bar, "-10", 1f, out dummy);
            row.minus1 = Small(bar, "-1", 1f, out dummy);
            var valueRect = NewRect(bar, "Value");
            var vle = valueRect.gameObject.AddComponent<LayoutElement>(); vle.flexibleWidth = 1.6f;
            row.value = Text(valueRect, "Text", "1", 46, TextAlignmentOptions.Center, Accent);
            Stretch(row.value.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            row.plus1 = Small(bar, "+1", 1f, out dummy);
            row.plus10 = Small(bar, "+10", 1f, out dummy);
            row.go = Small(bar, "GO", 1.6f, out row.goLabel);
            row.go.GetComponent<Image>().color = new Color(0.2f, 0.7f, 0.3f);
            rt.gameObject.SetActive(false);
            return row;
        }

        static Button Small(Transform parent, string label, float flex, out TMP_Text labelText)
        {
            RectTransform rt = NewRect(parent, "Btn " + label);
            var img = rt.gameObject.AddComponent<Image>(); img.color = Mid;
            var btn = rt.gameObject.AddComponent<Button>(); btn.targetGraphic = img;
            var le = rt.gameObject.AddComponent<LayoutElement>(); le.flexibleWidth = flex;
            labelText = Text(rt, "Label", label, 34, TextAlignmentOptions.Center, Color.white);
            Stretch(labelText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return btn;
        }

        static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetArray(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(field);
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
