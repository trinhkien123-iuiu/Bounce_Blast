using UnityEngine;
using UnityEngine.SceneManagement;

namespace BounceBlast.Gameplay
{
    /// <summary>
    /// Luồng chuyển scene: Loading → MenuGame ⇄ Gameplay. Build Settings: Loading đứng đầu (mở app), rồi MenuGame, Gameplay.
    /// Vào màn chơi đi qua scene Loading (màn loading gốc); về Menu nạp thẳng.
    /// </summary>
    public static class SceneFlow
    {
        public const string Loading = "Loading";
        public const string Menu = "MenuGame";
        public const string Gameplay = "Gameplay";

        static string _pending;

        /// <summary>Scene mà màn Loading cần nạp (null = vừa mở app → MenuGame). Đọc một lần rồi xoá.</summary>
        public static string TakePending()
        {
            string s = _pending;
            _pending = null;
            return s;
        }

        /// <summary>
        /// true khi người chơi vào Gameplay từ Menu → Gameplay chơi tiếp level đã lưu. Bấm Play thẳng scene Gameplay
        /// trong Editor thì false → dùng LevelManager.startLevel để tiện test.
        /// </summary>
        public static bool FromMenu { get; private set; }

        /// <summary>
        /// true khi Gameplay đã dựng xong level đầu tiên (LevelManager báo). Màn Loading giữ lại tới lúc này rồi mới mờ đi –
        /// không lộ khung bàn chơi trống / cú khựng lúc dựng level.
        /// </summary>
        public static bool LevelReady { get; private set; }

        public static void MarkLevelReady() => LevelReady = true;

        public static void GoToGameplay()
        {
            FromMenu = true;
            LevelReady = false;
            Time.timeScale = 1f;
            _pending = Gameplay;
            SceneManager.LoadScene(Loading);
        }

        public static void GoToMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(Menu);
        }
    }
}
