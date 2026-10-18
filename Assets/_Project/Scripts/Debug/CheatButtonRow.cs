using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BounceBlast.Gameplay
{
    /// <summary>Mẫu dòng "nút / công tắc" của panel cheat (đặt sẵn, tắt, trong prefab CheatPanel; CheatPanelView nhân bản).</summary>
    public class CheatButtonRow : MonoBehaviour
    {
        public Button button;
        public TMP_Text label;
        public Image background;
    }
}
