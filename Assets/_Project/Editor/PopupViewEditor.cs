using BounceBlast.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using BounceBlast.UI;

namespace BounceBlast.EditorTools
{
    /// <summary>Giúp XEM và SỬA popup ngay trong Editor</summary>
    [CustomEditor(typeof(PopupViewBase), true)]
    public class PopupViewEditor : Editor
    {
        /// <summary>Xử lý hiện Inspector của popup</summary>
        public override void OnInspectorGUI()
        {
            var view = (PopupViewBase)target;
            if (!Application.isPlaying)
            {
                bool on = view.gameObject.activeSelf;
                EditorGUILayout.HelpBox(
                    on ? "ĐANG BẬT trong scene (xem trước). Sửa xong bấm \"Tắt xem trước\" – lưu scene cũng sẽ tự tắt."
                       : "Popup đang TẮT nên Scene view không vẽ gì (đúng thiết kế: chỉ hiện khi game gọi Show). Bấm \"Xem trước\" để bật tạm.",
                    on ? MessageType.Warning : MessageType.Info);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Xem trước", GUILayout.Height(26))) Preview(view, true);
                    if (GUILayout.Button("Tắt xem trước", GUILayout.Height(26))) Preview(view, false);
                }
                EditorGUILayout.Space(4);
            }
            DrawDefaultInspector();
            string missing = view.Validate();
            if (missing != null) EditorGUILayout.HelpBox("Chưa nối đủ tham chiếu: " + missing, MessageType.Error);
        }

        static void Preview(PopupViewBase view, bool show)
        {
            Undo.RecordObject(view.gameObject, show ? "Xem trước popup" : "Tắt xem trước popup");
            view.gameObject.SetActive(show);
            EditorUtility.SetDirty(view.gameObject);
            if (!show) return;
            Selection.activeGameObject = view.gameObject;
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.in2DMode = true;
                SceneView.lastActiveSceneView.FrameSelected();
            }
        }
    }

    /// <summary>Lưu scene thì tắt mọi popup đang bật</summary>
    [InitializeOnLoad]
    public static class PopupSaveGuard
    {
        static PopupSaveGuard() => EditorSceneManager.sceneSaving += OnSceneSaving;

        static void OnSceneSaving(UnityEngine.SceneManagement.Scene scene, string path)
        {
            if (Application.isPlaying) return;
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (PopupViewBase v in root.GetComponentsInChildren<PopupViewBase>(true))
                {
                    if (!v.gameObject.activeSelf) continue;
                    v.gameObject.SetActive(false);
                    Debug.Log("[PopupSaveGuard] Đã tắt popup '" + v.name + "' trước khi lưu scene (đang bật xem trước).", v);
                }
        }
    }
}
