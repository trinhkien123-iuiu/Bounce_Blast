using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>KHÔNG CÒN DÙNG – giữ lại để scene cũ có gắn component này không báo "missing script"</summary>
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    public class AmmoLayoutTuner : MonoBehaviour
    {
        [TextArea, SerializeField]
        string ghiChu = "Thông số đã chuyển sang GameConfig (ammoActivePose / ammoInactivePose / ammoSinkDepth). " +
                        "Component này không còn tác dụng, xoá đi được.";
    }
}
