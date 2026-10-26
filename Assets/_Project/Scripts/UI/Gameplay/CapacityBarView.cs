using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BounceBlast.Gameplay;

namespace BounceBlast.UI
{
    /// <summary>Script quản lý của prefab CapacityBar</summary>
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

        /// <summary>Xử lý cập nhật thanh sức chứa và chữ %</summary>
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

        /// <summary>Báo ô tham chiếu nào còn trống</summary>
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
