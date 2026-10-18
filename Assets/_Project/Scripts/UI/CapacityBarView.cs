using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BounceBlast.Gameplay;

namespace BounceBlast.UI
{
    /// <summary>
    /// Script quản lý của prefab CapacityBar – gắn ngay trên gốc prefab.
    ///
    /// Giữ tham chiếu tới thanh fill / chữ % / icon vô hạn bằng Inspector, và tự lo phần hiển thị.
    /// GameUI chỉ gọi <see cref="SetFill"/>. Tư thế (vị trí, xoay, cỡ) đã nung sẵn trong prefab –
    /// xem Docs 5.9; code không đặt lại nữa.
    /// </summary>
    [DisallowMultipleComponent]
    public class CapacityBarView : MonoBehaviour
    {
        [Tooltip("Canvas world-space bên trong prefab – cần gán camera lúc chạy")]
        [SerializeField] Canvas worldCanvas;
        [Tooltip("Thanh màu chạy theo % sức chứa")]
        [SerializeField] Image fill;
        [Tooltip("Chữ giữa thanh: \"CAPACITY\" khi chưa bắt đầu, \"n%\" khi đang chơi")]
        [SerializeField] TMP_Text label;
        [Tooltip("Icon vô cực – hiện ở level không giới hạn sức chứa")]
        [SerializeField] GameObject infiniteIcon;

        int _shownKey = int.MinValue;

        /// <summary>GameUI gán camera (không serialize được) và thứ tự vẽ.</summary>
        public void Bind(Camera worldCam, int sortingOrder, TMP_FontAsset font, Material fontMaterial)
        {
            if (worldCanvas != null)
            {
                worldCanvas.worldCamera = worldCam;
                worldCanvas.sortingOrder = sortingOrder;
            }
            if (label != null)
            {
                UnsquashLabel();
                label.color = Color.white;
                if (fontMaterial != null && font != null) { label.font = font; label.fontSharedMaterial = fontMaterial; }
            }
            if (infiniteIcon != null) infiniteIcon.SetActive(false);
        }

        /// <summary>
        /// Canvas của prefab ép dọc (scale y = 0.8 × x để thanh có độ dày đúng như gốc) và camera game lại ép dọc thêm
        /// cos20 (xem CameraFitter) → chữ bị bẹp ~25%. Bù riêng cho chữ: scale y của nó = scale x / cos20 trong hệ thế giới,
        /// để lên màn hình chữ có tỉ lệ 1:1. Đặt giá trị tuyệt đối (không nhân dồn) nên gọi nhiều lần vẫn đúng.
        /// </summary>
        void UnsquashLabel()
        {
            Transform t = label.transform, parent = t.parent;
            if (parent == null) return;
            Vector3 ps = parent.lossyScale;
            if (ps.y <= 1e-6f) return;
            Vector3 ls = t.localScale;
            ls.y = ls.x * (ps.x / ps.y) / ViewSpace.LegacyCos;
            t.localScale = ls;
        }

        /// <param name="fill01">0..1 phần sức chứa đã dùng</param>
        /// <param name="started">đã thả quả bi đầu tiên chưa</param>
        /// <param name="infinite">level không giới hạn sức chứa</param>
        public void SetFill(float fill01, bool started, bool infinite)
        {
            float shown = Mathf.Clamp01(fill01);
            if (fill != null)
            {
                fill.fillAmount = Mathf.MoveTowards(fill.fillAmount, started ? shown : 0f, Time.deltaTime * 1.5f);
                fill.color = CapacitySystem.FillColor(shown);
            }
            if (label != null)
            {
                // chỉ dựng chuỗi khi số % đổi (trước đây cấp phát chuỗi mới mỗi frame)
                int key = infinite ? -2 : !started ? -1 : Mathf.RoundToInt(fill01 * 100f);
                if (key != _shownKey)
                {
                    _shownKey = key;
                    if (key >= 0) label.SetText("{0}%", key);
                    else label.text = key == -2 ? "" : "CAPACITY";
                }
            }
            if (infiniteIcon != null) infiniteIcon.SetActive(infinite);
        }

        /// <summary>Báo ô tham chiếu nào còn trống (gọi khi dựng để lỗi hiện sớm, rõ ràng).</summary>
        public string Validate()
        {
            var miss = new System.Collections.Generic.List<string>();
            if (worldCanvas == null) miss.Add("worldCanvas");
            if (fill == null) miss.Add("fill");
            if (label == null) miss.Add("label");
            return miss.Count == 0 ? null : string.Join(", ", miss);
        }
    }
}
