using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FusionContinent.UI
{
    [DisallowMultipleComponent]
    public class DropDownBase : Selectable, IPointerClickHandler
    {
        [Serializable]
        public class DropdownEvent : UnityEvent<int>
        {
        }

        private class RuntimeItem
        {
            public GameObject ItemObject;
            public Button ItemButton;
            public Toggle ItemToggle;
            public Components.UIPointerClickAndLongPress ItemClickAndLongPress;
            public UnityEngine.Events.UnityAction ClickHandler;
            public UnityEngine.Events.UnityAction<bool> ToggleHandler;
        }

        [Header("Reference")]
        [SerializeField] private Button mHeaderButton;
        [SerializeField] private TextMeshProUGUI mCaptionText;
        [SerializeField] private RectTransform mOptionsRoot;
        [SerializeField] private RectTransform mItemsRoot;
        [SerializeField] private GameObject mItemTemplate;

        [Header("Init Data")]
        [SerializeField] private int mDefaultIndex;

        private readonly List<int> mDataList = new List<int>();
        private readonly List<RuntimeItem> mRuntimeItems = new List<RuntimeItem>();
        private int mCurrentIndex = -1;
        private Func<int, string> mCaptionFormatter;
        private readonly Dictionary<int, string> mCaptionMap = new Dictionary<int, string>();

        public DropdownEvent onValueChanged = new DropdownEvent();

        public int value
        {
            get => mCurrentIndex;
            set => SetValue(value, true);
        }

        public int CurrentIndex => mCurrentIndex;
        public int CurrentData =>
            mCurrentIndex >= 0 && mCurrentIndex < mDataList.Count ? mDataList[mCurrentIndex] : 0;

        public event Action<int, int> OnSelectionChanged;
        public event Action<int, int, GameObject> OnBindItem;

        public void SetCaptionFormatter(Func<int, string> formatter, bool refreshCaption = true)
        {
            mCaptionFormatter = formatter;
            if (refreshCaption)
            {
                RefreshCaptionText();
            }
        }

        public void SetCaptionMap(IDictionary<int, string> captionMap, bool refreshCaption = true)
        {
            mCaptionMap.Clear();
            if (captionMap != null)
            {
                foreach (var kv in captionMap)
                {
                    mCaptionMap[kv.Key] = kv.Value;
                }
            }

            if (refreshCaption)
            {
                RefreshCaptionText();
            }
        }

        protected override void Awake()
        {
            base.Awake();

            if (mHeaderButton != null)
            {
                mHeaderButton.onClick.AddListener(ToggleOptions);
            }

            if (mItemTemplate != null)
            {
                mItemTemplate.SetActive(false);
            }

            SetOptionsVisible(false);
        }

        public void SetDataList(IList<int> dataList, int defaultIndex = 0)
        {
            mDataList.Clear();
            if (dataList != null)
            {
                for (int i = 0; i < dataList.Count; i++)
                {
                    mDataList.Add(dataList[i]);
                }
            }

            RebuildItems();

            if (mDataList.Count == 0)
            {
                mCurrentIndex = -1;
                if (mCaptionText != null)
                {
                    mCaptionText.text = string.Empty;
                }
                return;
            }

            mDefaultIndex = Mathf.Clamp(defaultIndex, 0, mDataList.Count - 1);
            SetValue(mDefaultIndex, false);
        }

        public void ClearOptions()
        {
            mDataList.Clear();
            ClearItems();
            mCurrentIndex = -1;

            if (mCaptionText != null)
            {
                mCaptionText.text = string.Empty;
            }

            SetOptionsVisible(false);
        }

        public void AddOptions(IList<int> dataList, bool keepCurrentSelection = true)
        {
            if (dataList == null || dataList.Count == 0)
            {
                return;
            }

            int previousIndex = mCurrentIndex;
            bool hadSelection = previousIndex >= 0 && previousIndex < mDataList.Count;

            for (int i = 0; i < dataList.Count; i++)
            {
                mDataList.Add(dataList[i]);
            }

            RebuildItems();

            if (mDataList.Count == 0)
            {
                mCurrentIndex = -1;
                if (mCaptionText != null)
                {
                    mCaptionText.text = string.Empty;
                }
                return;
            }

            if (keepCurrentSelection && hadSelection)
            {
                SetValue(previousIndex, false);
                return;
            }

            if (mCurrentIndex < 0 || mCurrentIndex >= mDataList.Count)
            {
                SetValue(0, false);
            }
            else if (mCaptionText != null)
            {
                mCaptionText.text = GetCaptionText(mDataList[mCurrentIndex]);
            }
        }

        public void SetValue(int index, bool notify = true, bool forceNotify = false)
        {
            if (mDataList.Count == 0)
            {
                if (mCaptionText != null)
                {
                    mCaptionText.text = string.Empty;
                }
                return;
            }

            int safeIndex = Mathf.Clamp(index, 0, mDataList.Count - 1);
            bool hasChanged = mCurrentIndex != safeIndex;
            mCurrentIndex = safeIndex;

            if (mCaptionText != null)
            {
                mCaptionText.text = GetCaptionText(mDataList[safeIndex]);
            }

            RefreshItemSelectionState();
            SetOptionsVisible(false);

            if (notify && (hasChanged || forceNotify))
            {
                onValueChanged?.Invoke(safeIndex);
                OnSelectionChanged?.Invoke(safeIndex, mDataList[safeIndex]);
            }
        }

        public void SetOptionsVisible(bool visible)
        {
            if (mOptionsRoot != null)
            {
                mOptionsRoot.gameObject.SetActive(visible);
            }
        }

        public void ToggleOptions()
        {
            if (mOptionsRoot == null)
            {
                return;
            }

            SetOptionsVisible(!mOptionsRoot.gameObject.activeSelf);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // When no header button is assigned, clicking this Selectable toggles the list.
            if (mHeaderButton == null && IsActive() && IsInteractable())
            {
                ToggleOptions();
            }
        }

        protected virtual string GetCaptionText(int data)
        {
            if (mCaptionFormatter != null)
            {
                return mCaptionFormatter(data) ?? string.Empty;
            }

            if (mCaptionMap.TryGetValue(data, out var mappedText))
            {
                return mappedText ?? string.Empty;
            }

            return data.ToString();
        }

        private void RefreshCaptionText()
        {
            if (mCaptionText == null)
            {
                return;
            }

            if (mCurrentIndex < 0 || mCurrentIndex >= mDataList.Count)
            {
                mCaptionText.text = string.Empty;
                return;
            }

            mCaptionText.text = GetCaptionText(mDataList[mCurrentIndex]);
        }

        private void RebuildItems()
        {
            ClearItems();

            if (mItemTemplate == null || mOptionsRoot == null)
            {
                return;
            }

            var runtimeParent = mItemsRoot != null ? mItemsRoot : mOptionsRoot;

            for (int i = 0; i < mDataList.Count; i++)
            {
                int itemIndex = i;
                var itemObject = Instantiate(mItemTemplate, runtimeParent);
                itemObject.SetActive(true);
                itemObject.name = "Item_" + i;

                var button = itemObject.GetComponent<Button>();
                if (button == null)
                {
                    button = itemObject.GetComponentInChildren<Button>(true);
                }

                var toggle = itemObject.GetComponent<Toggle>();
                if (toggle == null)
                {
                    toggle = itemObject.GetComponentInChildren<Toggle>(true);
                }

                // 兼容项目自定义的点击组件（如登录界面服务器下拉项）
                var clickAndLongPress = itemObject.GetComponent<Components.UIPointerClickAndLongPress>();
                if (clickAndLongPress == null)
                {
                    clickAndLongPress = itemObject.GetComponentInChildren<Components.UIPointerClickAndLongPress>(true);
                }

                UnityEngine.Events.UnityAction clickHandler = () => SetValue(itemIndex, true, forceNotify: true);
                UnityEngine.Events.UnityAction<bool> toggleHandler = isOn =>
                {
                    if (isOn)
                    {
                        SetValue(itemIndex, true, forceNotify: true);
                        return;
                    }

                    // 单选：不允许取消当前项，只能切换到另一项
                    if (mCurrentIndex == itemIndex && toggle != null)
                    {
                        toggle.SetIsOnWithoutNotify(true);
                    }
                };

                if (button != null)
                {
                    button.onClick.AddListener(clickHandler);
                }
                else if (toggle != null)
                {
                    toggle.onValueChanged.AddListener(toggleHandler);
                }
                else if (clickAndLongPress != null && clickAndLongPress.onClick != null)
                {
                    clickAndLongPress.onClick.AddListener(clickHandler);
                }

                mRuntimeItems.Add(new RuntimeItem
                {
                    ItemObject = itemObject,
                    ItemButton = button,
                    ItemToggle = toggle,
                    ItemClickAndLongPress = clickAndLongPress,
                    ClickHandler = clickHandler,
                    ToggleHandler = toggleHandler
                });

                OnBindItem?.Invoke(i, mDataList[i], itemObject);
            }

            RefreshItemSelectionState();
        }

        private void RefreshItemSelectionState()
        {
            for (int i = 0; i < mRuntimeItems.Count; i++)
            {
                var runtimeItem = mRuntimeItems[i];
                if (runtimeItem == null || runtimeItem.ItemObject == null)
                {
                    continue;
                }

                bool selected = i == mCurrentIndex;

                var itemView = runtimeItem.ItemObject.GetComponent<DropDownItemView>();
                if (itemView != null)
                {
                    itemView.SetSelected(selected);
                }

                if (runtimeItem.ItemToggle != null)
                {
                    runtimeItem.ItemToggle.SetIsOnWithoutNotify(selected);
                }
            }
        }

        private void ClearItems()
        {
            for (int i = 0; i < mRuntimeItems.Count; i++)
            {
                var item = mRuntimeItems[i];
                if (item == null)
                {
                    continue;
                }

                if (item.ItemButton != null && item.ClickHandler != null)
                {
                    item.ItemButton.onClick.RemoveListener(item.ClickHandler);
                }

                if (item.ItemToggle != null && item.ToggleHandler != null)
                {
                    item.ItemToggle.onValueChanged.RemoveListener(item.ToggleHandler);
                }

                if (item.ItemClickAndLongPress != null && item.ItemClickAndLongPress.onClick != null && item.ClickHandler != null)
                {
                    item.ItemClickAndLongPress.onClick.RemoveListener(item.ClickHandler);
                }

                if (item.ItemObject != null)
                {
                    Destroy(item.ItemObject);
                }
            }

            mRuntimeItems.Clear();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (mHeaderButton != null)
            {
                mHeaderButton.onClick.RemoveListener(ToggleOptions);
            }

            ClearItems();
        }
    }
}
