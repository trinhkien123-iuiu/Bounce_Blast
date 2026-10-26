using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BounceBlast.Cheats
{
    /// <summary>Mẫu dòng "chọn số + GO": nhãn, giá trị, 4 nút bước</summary>
    public class CheatStepperRow : MonoBehaviour
    {
        public TMP_Text label;
        public TMP_Text value;
        public Button minus10, minus1, plus1, plus10, go;
        public TMP_Text goLabel;
    }
}
