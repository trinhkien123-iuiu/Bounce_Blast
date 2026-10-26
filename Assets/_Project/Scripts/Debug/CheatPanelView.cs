using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BounceBlast.Cheats
{
    /// <summary>View của prefab CheatPanel</summary>
    public class CheatPanelView : MonoBehaviour
    {
        [SerializeField] GameObject panelRoot;
        [SerializeField] Button closeButton;
        [Tooltip("Ba nút tab theo thứ tự CheatRegistry.Groups")] [SerializeField] Button[] tabButtons = new Button[0];
        [SerializeField] TMP_Text[] tabLabels = new TMP_Text[0];
        [SerializeField] RectTransform content;
        [SerializeField] ScrollRect scroll;
        [SerializeField] CheatButtonRow buttonTemplate;
        [SerializeField] CheatStepperRow stepperTemplate;

#if UNITY_EDITOR || DEVELOPMENT_BUILD || BB_CHEATS
        static readonly Color TabOn = new Color(0.98f, 0.74f, 0.1f), TabOff = new Color(0.25f, 0.27f, 0.45f);
        static readonly Color RowOn = new Color(0.2f, 0.7f, 0.3f), RowOff = new Color(0.3f, 0.32f, 0.5f);

        List<CheatItem> _items = new List<CheatItem>();
        readonly List<GameObject> _rows = new List<GameObject>();
        readonly List<Action> _refreshers = new List<Action>();
        int _tab;

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        void Awake()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Hide);
            for (int i = 0; i < tabButtons.Length; i++)
            {
                int idx = i;
                if (tabButtons[i] != null) tabButtons[i].onClick.AddListener(() => SelectTab(idx));
            }
            if (buttonTemplate != null) buttonTemplate.gameObject.SetActive(false);
            if (stepperTemplate != null) stepperTemplate.gameObject.SetActive(false);
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        /// <summary>Nhận danh sách mục cheat để hiển thị</summary>
        public void SetItems(List<CheatItem> items) => _items = items ?? new List<CheatItem>();

        /// <summary>Xử lý mở bảng cheat</summary>
        public void Show() { if (panelRoot != null) { panelRoot.SetActive(true); SelectTab(_tab); } }
        /// <summary>Xử lý đóng bảng cheat</summary>
        public void Hide() { if (panelRoot != null) panelRoot.SetActive(false); }
        /// <summary>Xử lý bật/tắt bảng cheat</summary>
        public void Toggle() { if (IsOpen) Hide(); else Show(); }

        void SelectTab(int index)
        {
            _tab = Mathf.Clamp(index, 0, CheatRegistry.Groups.Length - 1);
            for (int i = 0; i < tabButtons.Length; i++)
            {
                if (tabButtons[i] == null) continue;
                var img = tabButtons[i].GetComponent<Image>();
                if (img != null) img.color = i == _tab ? TabOn : TabOff;
                if (i < tabLabels.Length && tabLabels[i] != null)
                {
                    tabLabels[i].text = i < CheatRegistry.Groups.Length ? CheatRegistry.Groups[i] : "";
                    tabLabels[i].color = i == _tab ? Color.black : Color.white;
                }
            }
            Rebuild();
            if (scroll != null) scroll.verticalNormalizedPosition = 1f;
        }

        void Rebuild()
        {
            foreach (GameObject r in _rows) if (r != null) Destroy(r);
            _rows.Clear();
            _refreshers.Clear();
            string group = CheatRegistry.Groups[_tab];
            foreach (CheatItem item in _items)
            {
                if (item.group != group) continue;
                if (item.kind == CheatKind.Stepper) BuildStepper(item);
                else BuildButton(item);
            }
        }

        void RefreshAll() { foreach (Action a in _refreshers) a(); }

        void BuildButton(CheatItem item)
        {
            CheatButtonRow row = Instantiate(buttonTemplate, content);
            row.gameObject.SetActive(true);
            _rows.Add(row.gameObject);
            Action refresh = () =>
            {
                bool on = item.kind == CheatKind.Toggle && item.get != null && item.get();
                string text = item.Label;
                row.label.text = item.kind == CheatKind.Toggle ? (on ? "[ON]  " : "[off]  ") + text : text;
                if (row.background != null) row.background.color = item.kind == CheatKind.Toggle && on ? RowOn : RowOff;
            };
            _refreshers.Add(refresh);
            refresh();
            row.button.onClick.AddListener(() =>
            {
                try
                {
                    if (item.kind == CheatKind.Toggle) item.set?.Invoke(!(item.get != null && item.get()));
                    else item.onClick?.Invoke();
                }
                catch (Exception e) { Debug.LogError("[Cheat] " + item.label + ": " + e); }
                RefreshAll();
            });
        }

        void BuildStepper(CheatItem item)
        {
            CheatStepperRow row = Instantiate(stepperTemplate, content);
            row.gameObject.SetActive(true);
            _rows.Add(row.gameObject);
            int value = item.initial != null ? item.initial() : item.min;
            row.label.text = item.Label;
            row.goLabel.text = item.goLabel;
            Action show = () => row.value.text = value.ToString();
            void Step(int d) { value = Mathf.Clamp(value + d, item.min, item.max); show(); }
            row.minus10.onClick.AddListener(() => Step(-10));
            row.minus1.onClick.AddListener(() => Step(-1));
            row.plus1.onClick.AddListener(() => Step(1));
            row.plus10.onClick.AddListener(() => Step(10));
            row.go.onClick.AddListener(() =>
            {
                try { item.go?.Invoke(value); }
                catch (Exception e) { Debug.LogError("[Cheat] " + item.label + ": " + e); }
                RefreshAll();
            });
            show();
        }
#endif
    }
}
