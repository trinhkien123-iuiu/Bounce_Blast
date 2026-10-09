using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneHierarchyCleaner
{
    [MenuItem("BounceBlast/Dọn dẹp Hierarchy (Clean Up Unused GameObjects)")]
    public static void CleanSceneHierarchy()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.isLoaded)
        {
            Debug.LogWarning("[SceneHierarchyCleaner] Không có Scene nào đang mở!");
            return;
        }

        int deletedCount = 0;

        // Quét toàn bộ tất cả GameObject trong Scene
        GameObject[] allObjects = activeScene.GetRootGameObjects();
        foreach (GameObject rootObj in allObjects)
        {
            deletedCount += CleanTransformRecursive(rootObj.transform);
        }

        // Xóa riêng quả bóng dummy nếu có ở root
        GameObject rootBall = GameObject.Find("/Ball");
        if (rootBall != null)
        {
            Undo.DestroyObjectImmediate(rootBall);
            deletedCount++;
        }

        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);

        Debug.Log($"<color=#00FF88><b>[BounceBlast] Đã dọn dẹp sạch sẽ {deletedCount} GameObject dư thừa trong Hierarchy và lưu Scene thành công!</b></color>");
    }

    private static int CleanTransformRecursive(Transform parent)
    {
        int count = 0;
        if (parent == null) return count;

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            string n = child.name;

            // Kiểm tra tên các GameObject rác cần xóa (không phân biệt chữ hoa thường)
            if (n.Equals("ConveyorManager", System.StringComparison.OrdinalIgnoreCase) ||
                n.Equals("CONVEYOR MANAGERS", System.StringComparison.OrdinalIgnoreCase) ||
                n.StartsWith("KPConveyorManager", System.StringComparison.OrdinalIgnoreCase) ||
                n.StartsWith("BOARD CUBE -", System.StringComparison.OrdinalIgnoreCase) ||
                n.StartsWith("BOARD WALL -", System.StringComparison.OrdinalIgnoreCase) ||
                n.Equals("Ball Spawner", System.StringComparison.OrdinalIgnoreCase) ||
                n.Equals("MASK HARD", System.StringComparison.OrdinalIgnoreCase) ||
                n.Equals("MASK SUPER HARD", System.StringComparison.OrdinalIgnoreCase) ||
                n.StartsWith("Cube (", System.StringComparison.OrdinalIgnoreCase) ||
                n.Equals("TutorialHand", System.StringComparison.OrdinalIgnoreCase))
            {
                Undo.DestroyObjectImmediate(child.gameObject);
                count++;
            }
            else
            {
                // Quét tiếp vào các cấp con
                count += CleanTransformRecursive(child);
            }
        }

        return count;
    }
}
