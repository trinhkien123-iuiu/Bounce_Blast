using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BounceBlast.Gameplay
{
    /// <summary>Mẫu dòng "chọn số + GO": nhãn, giá trị, 4 nút bước (−10 −1 +1 +10) và nút GO.</summary>
    public class CheatStepperRow : MonoBehaviour
    {
        public TMP_Text label;
        public TMP_Text value;
        public Button minus10, minus1, plus1, plus10, go;
        public TMP_Text goLabel;
    }
}
