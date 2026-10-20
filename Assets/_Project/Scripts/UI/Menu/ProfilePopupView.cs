using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BounceBlast.Gameplay;

namespace BounceBlast.UI
{
    /// <summary>
    /// Script quản lý của prefab ProfileCanvas – popup "Profile" mở khi bấm avatar ở Home (tách từ
    /// <c>ProfileSystem/SelectProfileCanvas</c> của ManagerScene gốc). Đổi tên (tối đa 14 ký tự như gốc) và chọn 1 trong 20 avatar;
    /// bấm Save thì báo <see cref="OnSave"/> – việc lưu là của <c>MenuManager</c>. Popup đặt sẵn, lưu ở trạng thái TẮT.
    /// Ảnh 20 ô, ô avatar vàng (tắt) lưu sẵn trong prefab; lúc chạy chỉ đổi: ô đang chọn (khung + dấu tích + phóng to), ảnh lớn, ô tên.
    /// </summary>
    [DisallowMultipleComponent]
    public class ProfilePopupView : MonoBehaviour
    {
        /// <summary>Một ô avatar trong lưới.</summary>
        [Serializable]
        public class AvatarSlot
        {
            public Button button;
            [Tooltip("Ảnh avatar của ô – ảnh lưu sẵn trong prefab, cũng là ảnh dùng cho avatar ở Home")] public Image image;
            [Tooltip("Khung + dấu tích khi đang chọn")] public GameObject selected;
        }

        public const int MaxNameLength = 14;

        [SerializeField] AvatarSlot[] slots = new AvatarSlot[0];
        [Tooltip("Ảnh avatar lớn cạnh ô tên")] [SerializeField] Image currentImage;
        [SerializeField] TMP_InputField nameInput;
        [SerializeField] Button saveButton;
        [SerializeField] Button closeButton;
        [SerializeField] ScrollRect scroll;
        [Tooltip("Ô đang chọn to gấp mấy lần cỡ trong prefab (ảnh gốc ≈ 1.1)")] [SerializeField] float selectedScale = 1.1f;

        /// <summary>Bấm Save: (tên, chỉ số avatar).</summary>
        public event Action<string, int> OnSave;
        public event Action OnClose;

        public int AvatarCount => slots.Length;
        int _avatar;
        Vector3[] _slotScale;   // cỡ từng ô trong prefab

        void Awake()
        {
            if (nameInput != null) nameInput.characterLimit = MaxNameLength;
            for (int i = 0; i < slots.Length; i++)
            {
                int index = i;
                if (slots[i].button != null) slots[i].button.onClick.AddListener(() => { AudioManager.Play(Sfx.Button); Select(index); });
            }
            if (saveButton != null) saveButton.onClick.AddListener(() =>
            {
                AudioManager.Play(Sfx.Button);
                string n = nameInput != null ? nameInput.text.Trim() : "";
                OnSave?.Invoke(n, _avatar);
            });
            if (closeButton != null) closeButton.onClick.AddListener(() => { AudioManager.Play(Sfx.Button); OnClose?.Invoke(); });
        }

        /// <summary>Ảnh avatar theo chỉ số (dùng cho nút avatar ở Home).</summary>
        public Sprite Avatar(int index)
        {
            if (slots.Length == 0) return null;
            Image img = slots[Mathf.Clamp(index, 0, slots.Length - 1)].image;
            return img != null ? img.sprite : null;
        }

        public void Show(string playerName, int avatar)
        {
            gameObject.SetActive(true);
            if (nameInput != null) nameInput.text = playerName;
            if (scroll != null) scroll.verticalNormalizedPosition = 1f;
            Select(avatar);
        }

        public void Hide() => gameObject.SetActive(false);

        void Select(int index)
        {
            _avatar = Mathf.Clamp(index, 0, Mathf.Max(0, slots.Length - 1));
            if (_slotScale == null)
            {
                _slotScale = new Vector3[slots.Length];
                for (int i = 0; i < slots.Length; i++) _slotScale[i] = slots[i].button != null ? slots[i].button.transform.localScale : Vector3.one;
            }
            for (int i = 0; i < slots.Length; i++)
            {
                bool on = i == _avatar;
                if (slots[i].selected != null) slots[i].selected.SetActive(on);
                if (slots[i].button != null) slots[i].button.transform.localScale = _slotScale[i] * (on ? selectedScale : 1f);
            }
            if (currentImage != null) currentImage.sprite = Avatar(_avatar);
        }

        /// <summary>Báo ô tham chiếu nào còn trống (null nếu đủ).</summary>
        public string Validate()
        {
            var miss = new List<string>();
            if (slots.Length == 0) miss.Add("slots");
            for (int i = 0; i < slots.Length; i++)
                if (slots[i].button == null || slots[i].image == null || slots[i].selected == null) miss.Add("slots[" + i + "]");
            if (currentImage == null) miss.Add("currentImage");
            if (nameInput == null) miss.Add("nameInput");
            if (saveButton == null) miss.Add("saveButton");
            if (closeButton == null) miss.Add("closeButton");
            return miss.Count == 0 ? null : string.Join(", ", miss);
        }
    }
}
