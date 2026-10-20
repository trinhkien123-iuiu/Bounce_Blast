using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BounceBlast.UI
{
    /// <summary>
    /// Vuốt ngang để chuyển trang ở MenuGame (như game gốc): trang đi theo ngón tay, trang kế bên lộ dần ra;
    /// thả quá <see cref="switchFraction"/> bề ngang hoặc vuốt nhanh thì sang trang kế, không thì trượt về.
    /// Tab chưa có trang (Journey, Collection) bị bỏ qua khi vuốt. Bấm tab cũng trượt sang trang đó.
    ///
    /// Đặt sẵn ở <c>UI/MenuPager</c> (scene MenuGame). Mỗi trang là một prefab riêng (Canvas gốc không dời được),
    /// nên thứ được trượt là <see cref="Page.content"/> – con phủ kín màn hình của trang. Lúc đứng yên mọi content ở x = 0
    /// (đúng như trong prefab); chỉ khi đang vuốt / đang trượt content mới lệch, xong lại về 0.
    /// </summary>
    [DisallowMultipleComponent]
    public class MenuPager : MonoBehaviour
    {
        [Serializable]
        public class Page
        {
            [Tooltip("Gốc trang (Canvas) – bật/tắt cả trang. Để TRỐNG nếu tab chưa có trang")] public GameObject root;
            [Tooltip("Con phủ kín màn hình được trượt khi vuốt (Home: MenuCanvas, Shop: IN APP PURCHASE, Settings: Content)")] public RectTransform content;
            [Tooltip("Chặn bấm nút trong trang khi đang vuốt – CanvasGroup trên gốc trang")] public CanvasGroup group;
        }

        [Tooltip("Cùng thứ tự với tab dưới đáy: Shop, Journey, Home, Collection, Settings")] [SerializeField] Page[] pages = new Page[0];
        [Tooltip("Ngón tay đi bao nhiêu pixel màn hình mới tính là vuốt ngang (nhỏ hơn thì vẫn là bấm)")] [SerializeField] float dragThreshold = 25f;
        [Tooltip("Thả quá phần này của bề ngang thì sang trang")] [SerializeField, Range(0.1f, 0.9f)] float switchFraction = 0.25f;
        [Tooltip("Vuốt nhanh hơn (bề ngang/giây) thì sang trang dù chưa quá ngưỡng")] [SerializeField] float flickSpeed = 1.2f;
        [Tooltip("Giây trượt nốt sau khi thả")] [SerializeField] float settleTime = 0.22f;
        [Tooltip("Vuốt về phía không còn trang thì chỉ kéo được phần này rồi bật lại")] [SerializeField, Range(0f, 0.5f)] float edgeResistance = 0.15f;

        /// <summary>Trang hiện tại đổi (do vuốt hoặc do <see cref="Show"/>) – nhận chỉ số tab.</summary>
        public event Action<int> OnPageChanged;

        public int Current => _current;
        public int Count => pages.Length;
        public bool HasPage(int i) => i >= 0 && i < pages.Length && pages[i].root != null;

        int _current = -1;
        // đang vuốt
        bool _pressed, _dragging, _blocked;
        Vector2 _pressPos;
        float _offset;          // độ lệch hiện tại, đơn vị = bề ngang trang (−1..1)
        float _lastX, _velocity;
        // đang trượt nốt
        bool _settling;
        float _settleFrom, _settleTo, _settleT;
        int _target = -1;
        int _restoreFrame = -1;
        readonly List<ScrollRect> _lockedScrolls = new List<ScrollRect>();

        /// <summary>Có popup che (VD Profile) thì tạm khoá vuốt.</summary>
        public bool Locked { get; set; }

        /// <summary>Hiện trang <paramref name="index"/>; <paramref name="animate"/> = trượt sang như vuốt.</summary>
        public void Show(int index, bool animate)
        {
            if (!HasPage(index)) return;
            if (!animate || _current < 0 || index == _current)
            {
                SnapTo(index);
                return;
            }
            // trượt: đặt trang đích sát bên rồi cho chạy offset về phía nó
            int dir = index > _current ? 1 : -1;
            _target = index;
            _settleFrom = 0f;
            _settleTo = -dir;
            _settleT = 0f;
            _settling = true;
            Layout(_current, _target, 0f, dir);
        }

        void SnapTo(int index)
        {
            _settling = false;
            _dragging = false;
            _offset = 0f;
            for (int i = 0; i < pages.Length; i++)
            {
                if (pages[i].root == null) continue;
                pages[i].root.SetActive(i == index);
                if (pages[i].content != null) SetX(pages[i].content, 0f);
            }
            bool changed = _current != index;
            _current = index;
            if (changed) OnPageChanged?.Invoke(index);
        }

        int Neighbour(int from, int dir)
        {
            for (int i = from + dir; i >= 0 && i < pages.Length; i += dir)
                if (HasPage(i)) return i;
            return -1;
        }

        float Width(int i)
        {
            Page p = pages[i];
            var rt = p.root != null ? p.root.transform as RectTransform : null;
            float w = rt != null ? rt.rect.width : 0f;
            return w > 1f ? w : 1242f;
        }

        static void SetX(RectTransform rt, float x)
        {
            Vector2 p = rt.anchoredPosition;
            p.x = x;
            rt.anchoredPosition = p;
        }

        /// <summary>Đặt trang hiện tại lệch <paramref name="offset"/> bề ngang, trang kế (phía <paramref name="dir"/>) nằm sát bên.</summary>
        void Layout(int cur, int next, float offset, int dir)
        {
            for (int i = 0; i < pages.Length; i++)
            {
                Page p = pages[i];
                if (p.root == null) continue;
                bool show = i == cur || i == next;
                if (p.root.activeSelf != show) p.root.SetActive(show);
                if (!show || p.content == null) continue;
                float w = Width(i);
                float x = i == cur ? offset : offset + dir;
                SetX(p.content, x * w);
            }
        }

        void Update()
        {
            if (_restoreFrame >= 0 && Time.frameCount > _restoreFrame) { SetInteractable(true); _restoreFrame = -1; }

            if (_settling)
            {
                _settleT += Time.unscaledDeltaTime / Mathf.Max(0.01f, settleTime);
                float k = Mathf.Clamp01(_settleT);
                float e = 1f - (1f - k) * (1f - k) * (1f - k);   // ease-out cubic
                _offset = Mathf.Lerp(_settleFrom, _settleTo, e);
                bool moving = _target >= 0 && _target != _current;
                int dir = moving ? (_target > _current ? 1 : -1) : (_offset < 0f ? 1 : -1);
                Layout(_current, moving ? _target : Neighbour(_current, dir), _offset, dir);   // trượt về: trang kế vẫn lộ tới khi khuất
                if (k >= 1f) SnapTo(_target >= 0 ? _target : _current);
                return;
            }

            if (Locked || _current < 0) { _pressed = false; return; }

            bool down, held, up;
            Vector2 pos;
            ReadPointer(out down, out held, out up, out pos);

            if (down) { _pressed = true; _dragging = false; _blocked = false; _pressPos = pos; _lastX = pos.x; _velocity = 0f; }
            if (!_pressed) return;

            float screenW = Mathf.Max(1f, Screen.width);
            if (held && !_dragging && !_blocked)
            {
                Vector2 d = pos - _pressPos;
                if (Mathf.Abs(d.y) > dragThreshold && Mathf.Abs(d.y) > Mathf.Abs(d.x)) _blocked = true;      // cuộn dọc (Shop) – không vuốt
                else if (Mathf.Abs(d.x) > dragThreshold) BeginDrag();
            }

            if (_dragging && held)
            {
                float dt = Mathf.Max(Time.unscaledDeltaTime, 1e-4f);
                _velocity = Mathf.Lerp(_velocity, (pos.x - _lastX) / screenW / dt, 0.5f);
                _lastX = pos.x;
                float raw = (pos.x - _pressPos.x) / screenW;
                int dir = raw < 0f ? 1 : -1;                       // kéo sang trái → lộ trang bên phải
                int next = Neighbour(_current, dir);
                _offset = next < 0 ? raw * edgeResistance : Mathf.Clamp(raw, -1f, 1f);
                Layout(_current, next, _offset, dir);
            }

            if (up)
            {
                _pressed = false;
                if (_dragging) EndDrag();
            }
        }

        void BeginDrag()
        {
            _dragging = true;
            SetInteractable(false);                               // không để cú vuốt kết thúc thành cú bấm nút
            _lockedScrolls.Clear();
            Page p = pages[_current];
            if (p.content != null)
                foreach (ScrollRect s in p.content.GetComponentsInChildren<ScrollRect>())
                    if (s.vertical) { s.vertical = false; _lockedScrolls.Add(s); }
        }

        void EndDrag()
        {
            _dragging = false;
            foreach (ScrollRect s in _lockedScrolls) if (s != null) s.vertical = true;
            _lockedScrolls.Clear();
            _restoreFrame = Time.frameCount;                      // bật lại nút ở frame sau (cú thả đã qua EventSystem)

            int dir = _offset < 0f ? 1 : -1;
            int next = Neighbour(_current, dir);
            bool go = next >= 0 && (Mathf.Abs(_offset) > switchFraction || Mathf.Abs(_velocity) > flickSpeed && Mathf.Sign(_velocity) == -dir);
            _target = go ? next : _current;
            _settleFrom = _offset;
            _settleTo = go ? -dir : 0f;
            _settleT = 0f;
            _settling = true;
        }

        void SetInteractable(bool on)
        {
            foreach (Page p in pages) if (p.group != null) p.group.interactable = on;
        }

        static void ReadPointer(out bool down, out bool held, out bool up, out Vector2 pos)
        {
            if (Input.touchCount > 0)
            {
                Touch t = Input.GetTouch(0);
                pos = t.position;
                down = t.phase == TouchPhase.Began;
                up = t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled;
                held = !up;
                return;
            }
            pos = Input.mousePosition;
            down = Input.GetMouseButtonDown(0);
            held = Input.GetMouseButton(0);
            up = Input.GetMouseButtonUp(0);
        }

        /// <summary>Báo ô tham chiếu nào còn trống (null nếu đủ).</summary>
        public string Validate()
        {
            var miss = new List<string>();
            if (pages.Length == 0) miss.Add("pages");
            for (int i = 0; i < pages.Length; i++)
                if (pages[i].root != null && (pages[i].content == null || pages[i].group == null)) miss.Add("pages[" + i + "] (content/group)");
            return miss.Count == 0 ? null : string.Join(", ", miss);
        }
    }
}
