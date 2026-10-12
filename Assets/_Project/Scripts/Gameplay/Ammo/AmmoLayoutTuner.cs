using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>
    /// KHÔNG CÒN DÙNG – giữ lại để scene cũ có gắn component này không báo "missing script".
    ///
    /// Trước đây component ghi tư thế hộp đạn vào biến static, nên thoát Play là mất số và phải
    /// Copy Component / Paste Component Values thủ công. Nay các thông số nằm trong GameConfig
    /// (ammoActivePose / ammoInactivePose / ammoSinkDepth): chọn GameConfig.asset rồi kéo thanh trượt,
    /// chỉnh được cả khi đang Play và thoát Play vẫn giữ nguyên.
    ///
    /// Xoá component này khỏi GameObject, rồi xoá luôn file nếu không cần nữa.
    /// </summary>
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    public class AmmoLayoutTuner : MonoBehaviour
    {
        [TextArea, SerializeField]
        string ghiChu = "Thông số đã chuyển sang GameConfig (ammoActivePose / ammoInactivePose / ammoSinkDepth). " +
                        "Component này không còn tác dụng, xoá đi được.";
    }
}
