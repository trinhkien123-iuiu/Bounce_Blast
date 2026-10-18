using System.Collections.Generic;
using BounceBlast.Gameplay;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using BounceBlast.UI;

namespace BounceBlast.EditorTools
{
    /// <summary>
    /// Gắn <see cref="UiButtonFeedback"/> vào mọi <see cref="Button"/> của các prefab UI (Assets/_Project/Prefabs/UI).
    /// Chạy lại bao nhiêu lần cũng được (nút đã có thì bỏ qua). Nút "Hover Indicator"/nút ẩn vẫn được gắn – component tự ngủ khi object tắt.
    /// </summary>
    public static class UiButtonFeedbackInstaller
    {
        const string Folder = "Assets/_Project/Prefabs/UI";

        [MenuItem("Tools/Bounce Blast/UI/Thêm phản hồi cho mọi nút")]
        public static void InstallAll()
        {
            int total = 0, prefabs = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { Folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                int added = 0;
                foreach (Button b in root.GetComponentsInChildren<Button>(true))
                {
                    if (b.GetComponent<UiButtonFeedback>() != null) continue;
                    b.gameObject.AddComponent<UiButtonFeedback>();
                    added++;
                }
                if (added > 0) { PrefabUtility.SaveAsPrefabAsset(root, path); prefabs++; total += added; }
                PrefabUtility.UnloadPrefabContents(root);
            }
            Debug.Log($"[UiButtonFeedback] Đã gắn {total} nút trong {prefabs} prefab.");
        }
    }
}
