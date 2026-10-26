using UnityEngine;
using UnityEngine.SceneManagement;

namespace BounceBlast.Gameplay
{
    /// <summary>Luồng chuyển scene: Loading → MenuGame ⇄ Gameplay</summary>
    public static class SceneFlow
    {
        public const string Loading = "Loading";
        public const string Menu = "MenuGame";
        public const string Gameplay = "Gameplay";

        static string _pending;

        /// <summary>Scene mà màn Loading cần nạp</summary>
        public static string TakePending()
        {
            string s = _pending;
            _pending = null;
            return s;
        }

        /// <summary>true khi người chơi vào Gameplay từ Menu → Gameplay chơi tiếp level đã lưu</summary>
        public static bool FromMenu { get; private set; }

        /// <summary>true khi Gameplay đã dựng xong level đầu tiên</summary>
        public static bool LevelReady { get; private set; }

        /// <summary>Xử lý báo level đã nạp xong</summary>
        public static void MarkLevelReady() => LevelReady = true;

        /// <summary>Xử lý chuyển sang scene chơi</summary>
        public static void GoToGameplay()
        {
            FromMenu = true;
            LevelReady = false;
            Time.timeScale = 1f;
            _pending = Gameplay;
            SceneManager.LoadScene(Loading);
        }

        /// <summary>Xử lý chuyển về scene menu</summary>
        public static void GoToMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(Menu);
        }
    }
}
