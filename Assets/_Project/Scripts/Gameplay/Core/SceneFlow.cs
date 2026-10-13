using UnityEngine;
using UnityEngine.SceneManagement;

namespace BounceBlast.Gameplay
{
    /// <summary>
    /// Luồng chuyển scene: MenuGame ⇄ Gameplay. Cả hai scene phải có trong Build Settings (MenuGame đứng đầu).
    /// </summary>
    public static class SceneFlow
    {
        public const string Menu = "MenuGame";
        public const string Gameplay = "Gameplay";

        /// <summary>
        /// true khi người chơi vào Gameplay từ Menu → Gameplay chơi tiếp level đã lưu. Bấm Play thẳng scene Gameplay
        /// trong Editor thì false → dùng LevelManager.startLevel để tiện test.
        /// </summary>
        public static bool FromMenu { get; private set; }

        public static void GoToGameplay()
        {
            FromMenu = true;
            Time.timeScale = 1f;
            SceneManager.LoadScene(Gameplay);
        }

        public static void GoToMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(Menu);
        }
    }
}
