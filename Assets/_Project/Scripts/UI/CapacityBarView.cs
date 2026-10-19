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
    /// GameUI chỉ gọi <see cref="SetFill"/>. Tư thế (vị trí, xoay, cỡ), camera, font, màu chữ và độ bù dẹt của chữ
    /// (scale y 1.33 – camera game ép dọc cos20) đều lưu sẵn trong prefab/scene; code không đặt lại.
    /// </summary>
    [DisallowMultipleComponent]
    public class CapacityBarView : MonoBehaviour
    {
        [Tooltip("Canvas world-space bên trong prefab – camera (Main Camera) và thứ tự vẽ lưu sẵn trong scene")]
        [SerializeField] Canvas worldCanvas;
        [Tooltip("Thanh màu chạy theo % sức chứa")]
        [SerializeField] Image fill;
        [Tooltip("Chữ giữa thanh: \"CAPACITY\" khi chưa bắt đầu, \"n%\" khi đang chơi")]
        [SerializeField] TMP_Text label;
        [Tooltip("Icon vô cực – hiện ở level không giới hạn sức chứa")]
        [SerializeField] GameObject infiniteIcon;

        int _shownKey = int.MinValue;

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
