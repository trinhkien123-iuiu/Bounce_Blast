using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BounceBlast.Gameplay;

namespace BounceBlast.UI
{
    /// <summary>
    /// Script quản lý của prefab MenuBottomBar – thanh 5 tab dưới đáy MenuGame (Shop, Journey, Home, Collection, Settings).
    /// Chỉ lo phần hình của tab (icon phóng to, hiện tên, dải sáng trượt theo) và báo <see cref="OnTabClicked"/>;
    /// mở trang nào là việc của <c>MenuManager</c>.
    /// </summary>
    [DisallowMultipleComponent]
    public class BottomBarView : MonoBehaviour
    {
        /// <summary>Một tab của thanh dưới đáy.</summary>
        [Serializable]
        public class Tab
        {
            [Tooltip("Nút bấm của tab")] public Button button;
            [Tooltip("Biểu tượng (phóng to khi được chọn)")] public RectTransform icon;
            [Tooltip("Tên tab – chỉ hiện khi được chọn")] public GameObject label;
            [Tooltip("Tên hiển thị trong thông báo 'sắp ra mắt'")] public string title;
        }

        [SerializeField] Tab[] tabs = new Tab[0];
        [Tooltip("Dải sáng nằm sau tab đang chọn")] [SerializeField] RectTransform hover;
        [SerializeField] float selectedIconScale = 1.2f;
        [SerializeField] float hoverSpeed = 14f;

        /// <summary>Người chơi bấm tab thứ i.</summary>
        public event Action<int> OnTabClicked;

        public int Count => tabs.Length;
        public string Title(int i) => i >= 0 && i < tabs.Length ? tabs[i].title : null;

        int _selected = -1;
        float _hoverX;
        bool _hoverSnap = true;

        void Awake()
        {
            for (int i = 0; i < tabs.Length; i++)
            {
                int index = i;
                if (tabs[i].button != null) tabs[i].button.onClick.AddListener(() => { AudioManager.Play(Sfx.Button); OnTabClicked?.Invoke(index); });
            }
        }

        /// <summary>Tô sáng tab đang chọn.</summary>
        public void Highlight(int index)
        {
            if (index < 0 || index >= tabs.Length) return;
            _selected = index;
            for (int i = 0; i < tabs.Length; i++)
            {
                bool on = i == index;
                if (tabs[i].icon != null) tabs[i].icon.localScale = Vector3.one * (on ? selectedIconScale : 1f);
                if (tabs[i].label != null) tabs[i].label.SetActive(on);
            }
        }

        void LateUpdate()
        {
            if (hover == null || _selected < 0 || tabs[_selected].button == null) return;
            // dải sáng trượt tới giữa tab đang chọn
            var parent = (RectTransform)hover.parent;
            float target = parent.InverseTransformPoint(tabs[_selected].button.transform.position).x;
            _hoverX = _hoverSnap ? target : Mathf.Lerp(_hoverX, target, 1f - Mathf.Exp(-hoverSpeed * Time.unscaledDeltaTime));
            _hoverSnap = false;
            Vector2 p = hover.anchoredPosition;
            p.x = _hoverX;
            hover.anchoredPosition = p;
        }

        /// <summary>Báo ô tham chiếu nào còn trống (null nếu đủ).</summary>
        public string Validate()
        {
            var miss = new List<string>();
            if (hover == null) miss.Add("hover");
            if (tabs.Length == 0) miss.Add("tabs");
            for (int i = 0; i < tabs.Length; i++)
                if (tabs[i].button == null || tabs[i].icon == null || tabs[i].label == null) miss.Add("tabs[" + i + "]");
            return miss.Count == 0 ? null : string.Join(", ", miss);
        }
    }
}
