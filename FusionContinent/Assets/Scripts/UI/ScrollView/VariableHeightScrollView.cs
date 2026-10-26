using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace ScrollView
{
    [RequireComponent(typeof(ScrollRect))]
    public class VariableHeightScrollView : MonoBehaviour
    {
        public delegate void UpdateItem(int dataIndex, ScrollItem scrollItem);
        public delegate int GetItemPrefabIndex(int dataIndex);

        /// <summary>消息 Prefab 列表，下标与 OnGetItemPrefabIndex 返回值对应。</summary>
        public ScrollItem[] ItemPrefabs;

        public float ItemSpacing = 8f;
        public float PaddingTop;
        public float PaddingBottom;
        public float DefaultItemHeight = 80f;
        public float NearBottomThreshold = 0.05f;
        public Ease ScrollEase = Ease.OutQuad;

        public UpdateItem OnUpdateItem;
        public GetItemPrefabIndex OnGetItemPrefabIndex;

        public Func<RectTransform, GameObject> PrefabFactory;
        public Func<int, RectTransform, GameObject> PrefabFactoryWithIndex;
        public Action<GameObject> ReleaseRuntimeAction;

        private ScrollRect _scrollRect;
        private RectTransform _content;
        private RectTransform _viewport;

        private int _dataCount;
        private readonly List<float> _heights = new List<float>();
        private readonly List<float> _posY = new List<float>();

        private readonly LinkedList<RuntimeItem> _displayedItems = new LinkedList<RuntimeItem>();
        private readonly Dictionary<int, LinkedList<RuntimeItem>> _cachedItemsByType = new Dictionary<int, LinkedList<RuntimeItem>>();
        private readonly HashSet<int> _displayedIndexSet = new HashSet<int>();
        private readonly Dictionary<int, ScrollItem> _measureItemsByType = new Dictionary<int, ScrollItem>();

        private ScrollItem[] _resolvedPrefabs = Array.Empty<ScrollItem>();
        private int _lastStartIndex = -1;
        private int _lastEndIndex = -1;

        private class RuntimeItem
        {
            public int Index;
            public int PrefabTypeIndex;
            public GameObject Node;
            public ScrollItem ScrollItem;
        }

        private void Awake()
        {
            _scrollRect = GetComponent<ScrollRect>();
            _content = _scrollRect.content;
            _viewport = _scrollRect.viewport != null ? _scrollRect.viewport : _scrollRect.transform as RectTransform;

            if (_content != null)
            {
                var layout = _content.GetComponent<LayoutGroup>();
                if (layout != null)
                {
                    layout.enabled = false;
                }

                _content.anchorMin = new Vector2(0f, 1f);
                _content.anchorMax = new Vector2(1f, 1f);
                _content.pivot = new Vector2(0.5f, 1f);
                _content.anchoredPosition = Vector2.zero;
            }

            if (_scrollRect != null)
            {
                _scrollRect.vertical = true;
                _scrollRect.horizontal = false;
            }

            RefreshResolvedPrefabs();
        }

        private void OnValidate()
        {
            RefreshResolvedPrefabs();
        }

        private void RefreshResolvedPrefabs()
        {
            _resolvedPrefabs = ItemPrefabs != null && ItemPrefabs.Length > 0
                ? ItemPrefabs
                : Array.Empty<ScrollItem>();
        }

        private void LateUpdate()
        {
            if (_dataCount > 0)
            {
                UpdateVisibleItems(false);
            }
        }

        public void SetUpdateFunc(UpdateItem onUpdate)
        {
            OnUpdateItem = onUpdate;
        }

        public void SetPrefabFactory(Func<RectTransform, GameObject> prefabFactory)
        {
            PrefabFactory = prefabFactory;
        }

        public void SetPrefabFactoryWithIndex(Func<int, RectTransform, GameObject> prefabFactory)
        {
            PrefabFactoryWithIndex = prefabFactory;
        }

        public void SetGetItemPrefabIndex(GetItemPrefabIndex getItemPrefabIndex)
        {
            OnGetItemPrefabIndex = getItemPrefabIndex;
        }

        public void SetReleaseAction(Action<GameObject> releaseAction)
        {
            ReleaseRuntimeAction = releaseAction;
        }

        public void SetHeights(IReadOnlyList<float> heights)
        {
            _dataCount = heights != null ? heights.Count : 0;
            _heights.Clear();
            for (int i = 0; i < _dataCount; i++)
            {
                _heights.Add(heights[i] > 0f ? heights[i] : DefaultItemHeight);
            }

            RebuildLayout(0);
            if (_dataCount <= 0)
            {
                RecycleAllDisplayedItems();
                return;
            }

            ForceRefreshVisible();
        }

        public void ShowList(int count, bool refreshVisible = true)
        {
            _dataCount = Mathf.Max(0, count);
            while (_heights.Count < _dataCount)
            {
                _heights.Add(DefaultItemHeight);
            }

            while (_heights.Count > _dataCount)
            {
                _heights.RemoveAt(_heights.Count - 1);
            }

            RebuildLayout(0);
            if (_dataCount <= 0)
            {
                RecycleAllDisplayedItems();
                return;
            }

            if (refreshVisible)
            {
                ForceRefreshVisible();
            }
        }

        public void SetItemHeight(int index, float height, bool refreshVisible = true)
        {
            if (index < 0 || index >= _dataCount)
            {
                return;
            }

            _heights[index] = height > 0f ? height : DefaultItemHeight;
            RebuildLayout(index);
            RepositionDisplayedFrom(index);
            if (refreshVisible)
            {
                ForceRefreshVisible();
            }
        }

        public float MeasureAndSetItemHeight(int index, bool refreshVisible = true)
        {
            if (index < 0 || index >= _dataCount || OnUpdateItem == null)
            {
                return DefaultItemHeight;
            }

            var scrollItem = AcquireMeasureItem(index);
            scrollItem.gameObject.SetActive(true);
            OnUpdateItem.Invoke(index, scrollItem);
            var height = GetPreferredHeight(scrollItem);
            scrollItem.gameObject.SetActive(false);
            SetItemHeight(index, height, refreshVisible);
            return height;
        }

        public void MeasureAllHeights()
        {
            if (OnUpdateItem == null)
            {
                return;
            }

            for (int i = 0; i < _dataCount; i++)
            {
                MeasureAndSetItemHeight(i, false);
            }

            ForceRefreshVisible();
        }

        public float GetItemHeight(int index)
        {
            if (index < 0 || index >= _heights.Count)
            {
                return DefaultItemHeight;
            }

            return _heights[index];
        }

        public void RefreshVisibleItems()
        {
            ForceRefreshVisible();
        }

        public bool IsNearBottom()
        {
            if (_scrollRect == null)
            {
                return true;
            }

            return _scrollRect.verticalNormalizedPosition <= NearBottomThreshold;
        }

        public void ScrollToBottom(bool animated = false, float duration = 0.2f)
        {
            ScrollToIndex(_dataCount - 1, ScrollAlignment.End, animated, duration);
        }

        public void ScrollToIndex(int index, ScrollAlignment alignment = ScrollAlignment.Start, bool animated = false, float duration = 0.25f)
        {
            if (_scrollRect == null || _content == null || _viewport == null || _dataCount <= 0)
            {
                return;
            }

            index = Mathf.Clamp(index, 0, _dataCount - 1);
            var itemTop = _posY[index];
            var itemBottom = itemTop + _heights[index];
            var viewportHeight = _viewport.rect.height;
            var contentHeight = _content.sizeDelta.y;
            var scrollable = Mathf.Max(0f, contentHeight - viewportHeight);

            float alignedStart;
            switch (alignment)
            {
                case ScrollAlignment.Center:
                    alignedStart = itemTop - (viewportHeight - _heights[index]) * 0.5f;
                    break;
                case ScrollAlignment.End:
                    alignedStart = itemBottom - viewportHeight;
                    break;
                default:
                    alignedStart = itemTop;
                    break;
            }

            alignedStart = Mathf.Clamp(alignedStart, 0f, scrollable);
            var target = scrollable <= 0.0001f ? 1f : 1f - alignedStart / scrollable;

            if (animated)
            {
                DOTween.Kill(_scrollRect, false);
                DOTween.To(() => _scrollRect.verticalNormalizedPosition,
                        v => _scrollRect.verticalNormalizedPosition = v,
                        target,
                        duration)
                    .SetEase(ScrollEase)
                    .SetTarget(_scrollRect);
            }
            else
            {
                _scrollRect.verticalNormalizedPosition = target;
            }
        }

        public void ReleaseScrollView()
        {
            var node = _displayedItems.First;
            while (node != null)
            {
                ReleaseRuntimeNode(node.Value);
                node = node.Next;
            }

            _displayedItems.Clear();
            _displayedIndexSet.Clear();

            foreach (var cacheEntry in _cachedItemsByType)
            {
                var cacheNode = cacheEntry.Value.First;
                while (cacheNode != null)
                {
                    ReleaseRuntimeNode(cacheNode.Value);
                    cacheNode = cacheNode.Next;
                }
            }

            _cachedItemsByType.Clear();

            foreach (var measureEntry in _measureItemsByType)
            {
                if (measureEntry.Value == null)
                {
                    continue;
                }

                ReleaseRuntimeNode(new RuntimeItem
                {
                    Node = measureEntry.Value.gameObject,
                    ScrollItem = measureEntry.Value
                });
            }

            _measureItemsByType.Clear();

            _dataCount = 0;
            _heights.Clear();
            _posY.Clear();
            _lastStartIndex = -1;
            _lastEndIndex = -1;

            if (_content != null)
            {
                _content.sizeDelta = new Vector2(_content.sizeDelta.x, 0f);
            }
        }

        private void RebuildLayout(int fromIndex)
        {
            EnsurePosCapacity(_dataCount);

            var startIndex = Mathf.Clamp(fromIndex, 0, _dataCount);
            for (int i = startIndex; i < _dataCount; i++)
            {
                if (i == 0)
                {
                    _posY[i] = PaddingTop;
                }
                else
                {
                    _posY[i] = _posY[i - 1] + _heights[i - 1] + ItemSpacing;
                }
            }

            var totalHeight = PaddingBottom;
            if (_dataCount > 0)
            {
                totalHeight = _posY[_dataCount - 1] + _heights[_dataCount - 1] + PaddingBottom;
            }

            if (_content != null)
            {
                _content.sizeDelta = new Vector2(_content.sizeDelta.x, totalHeight);
            }
        }

        private void EnsurePosCapacity(int count)
        {
            while (_posY.Count < count)
            {
                _posY.Add(0f);
            }

            while (_posY.Count > count)
            {
                _posY.RemoveAt(_posY.Count - 1);
            }
        }

        private void RepositionDisplayedFrom(int fromIndex)
        {
            var node = _displayedItems.First;
            while (node != null)
            {
                var item = node.Value;
                if (item.Index >= fromIndex)
                {
                    PositionItem(item);
                }

                node = node.Next;
            }
        }

        private void ForceRefreshVisible()
        {
            _lastStartIndex = -1;
            _lastEndIndex = -1;
            UpdateVisibleItems(true);
        }

        private void UpdateVisibleItems(bool force)
        {
            if (_content == null || _viewport == null || _dataCount <= 0)
            {
                return;
            }

            var scrollOffsetY = Mathf.Max(0f, _content.anchoredPosition.y);
            var viewportHeight = _viewport.rect.height;
            var startY = Mathf.Max(0f, scrollOffsetY - DefaultItemHeight);
            var endY = scrollOffsetY + viewportHeight + DefaultItemHeight;

            var startIndex = FindStartIndex(startY);
            var endIndex = FindEndIndex(endY);
            if (startIndex < 0)
            {
                startIndex = 0;
            }

            if (endIndex < startIndex)
            {
                endIndex = startIndex;
            }

            if (!force && startIndex == _lastStartIndex && endIndex == _lastEndIndex)
            {
                return;
            }

            _lastStartIndex = startIndex;
            _lastEndIndex = endIndex;

            var needed = new HashSet<int>();
            for (int i = startIndex; i <= endIndex && i < _dataCount; i++)
            {
                needed.Add(i);
            }

            var displayNode = _displayedItems.First;
            while (displayNode != null)
            {
                var next = displayNode.Next;
                var runtimeItem = displayNode.Value;
                if (!needed.Contains(runtimeItem.Index))
                {
                    RecycleItem(runtimeItem);
                }
                else
                {
                    var expectedType = ResolvePrefabTypeIndex(runtimeItem.Index);
                    if (runtimeItem.PrefabTypeIndex != expectedType)
                    {
                        RecycleItem(runtimeItem);
                    }
                    else if (force && runtimeItem.ScrollItem != null)
                    {
                        OnUpdateItem.Invoke(runtimeItem.Index, runtimeItem.ScrollItem);
                        PositionItem(runtimeItem);
                    }
                }

                displayNode = next;
            }

            foreach (var index in needed)
            {
                if (_displayedIndexSet.Contains(index))
                {
                    continue;
                }

                var runtimeItem = AcquireItem(index);
                if (runtimeItem == null)
                {
                    continue;
                }

                runtimeItem.Index = index;
                PositionItem(runtimeItem);
                _displayedItems.AddLast(runtimeItem);
                _displayedIndexSet.Add(index);
                OnUpdateItem.Invoke(index, runtimeItem.ScrollItem);
            }
        }

        private int FindStartIndex(float y)
        {
            if (_dataCount <= 0)
            {
                return -1;
            }

            var low = 0;
            var high = _dataCount - 1;
            var result = -1;

            while (low <= high)
            {
                var mid = low + (high - low) / 2;
                var top = _posY[mid];
                var bottom = top + _heights[mid];

                if (bottom < y)
                {
                    low = mid + 1;
                }
                else if (top > y)
                {
                    high = mid - 1;
                }
                else
                {
                    result = mid;
                    high = mid - 1;
                }
            }

            return result >= 0 ? result : Mathf.Clamp(low, 0, _dataCount - 1);
        }

        private int FindEndIndex(float y)
        {
            if (_dataCount <= 0)
            {
                return -1;
            }

            var low = 0;
            var high = _dataCount - 1;
            var result = -1;

            while (low <= high)
            {
                var mid = low + (high - low) / 2;
                var top = _posY[mid];

                if (top > y)
                {
                    high = mid - 1;
                }
                else
                {
                    result = mid;
                    low = mid + 1;
                }
            }

            return result >= 0 ? result : Mathf.Clamp(high, 0, _dataCount - 1);
        }

        private int ResolvePrefabTypeIndex(int dataIndex)
        {
            if (OnGetItemPrefabIndex == null)
            {
                return 0;
            }

            var typeIndex = OnGetItemPrefabIndex.Invoke(dataIndex);
            if (_resolvedPrefabs.Length == 0)
            {
                return 0;
            }

            return Mathf.Clamp(typeIndex, 0, _resolvedPrefabs.Length - 1);
        }

        private LinkedList<RuntimeItem> GetOrCreateCache(int prefabTypeIndex)
        {
            if (!_cachedItemsByType.TryGetValue(prefabTypeIndex, out var cache))
            {
                cache = new LinkedList<RuntimeItem>();
                _cachedItemsByType[prefabTypeIndex] = cache;
            }

            return cache;
        }

        private RuntimeItem CreateRuntimeItem(int prefabTypeIndex)
        {
            GameObject go;
            if (PrefabFactoryWithIndex != null)
            {
                go = PrefabFactoryWithIndex.Invoke(prefabTypeIndex, _content);
            }
            else if (PrefabFactory != null)
            {
                go = PrefabFactory.Invoke(_content);
            }
            else
            {
                if (_resolvedPrefabs.Length == 0)
                {
                    LogTool.LogError("[VariableHeightScrollView] 未配置 ItemPrefabs");
                    return null;
                }

                var prefab = _resolvedPrefabs[Mathf.Clamp(prefabTypeIndex, 0, _resolvedPrefabs.Length - 1)];
                go = Instantiate(prefab.gameObject, _content);
                go.GetComponent<ScrollItem>().OnSpawn();
            }

            return new RuntimeItem
            {
                PrefabTypeIndex = prefabTypeIndex,
                Node = go,
                ScrollItem = go.GetComponent<ScrollItem>()
            };
        }

        private RuntimeItem AcquireItem(int dataIndex)
        {
            var prefabTypeIndex = ResolvePrefabTypeIndex(dataIndex);
            var cache = GetOrCreateCache(prefabTypeIndex);
            RuntimeItem runtimeItem;
            if (cache.Count > 0)
            {
                runtimeItem = cache.First.Value;
                cache.RemoveFirst();
            }
            else
            {
                runtimeItem = CreateRuntimeItem(prefabTypeIndex);
                if (runtimeItem == null)
                {
                    return null;
                }
            }

            runtimeItem.Node.SetActive(true);
            SetupItemRect(runtimeItem.Node);
            return runtimeItem;
        }

        private ScrollItem AcquireMeasureItem(int dataIndex)
        {
            var prefabTypeIndex = ResolvePrefabTypeIndex(dataIndex);
            if (!_measureItemsByType.TryGetValue(prefabTypeIndex, out var measureItem) || measureItem == null)
            {
                var runtimeItem = CreateRuntimeItem(prefabTypeIndex);
                measureItem = runtimeItem.ScrollItem;
                measureItem.gameObject.SetActive(false);
                _measureItemsByType[prefabTypeIndex] = measureItem;
            }

            return measureItem;
        }

        private void SetupItemRect(GameObject go)
        {
            var rect = go.GetComponent<RectTransform>();
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(rect.anchorMin.x, 1f);
            rect.anchorMax = new Vector2(rect.anchorMax.x, 1f);
            rect.pivot = new Vector2(rect.pivot.x, 1f);
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, 0f);
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, DefaultItemHeight);
        }

        private void PositionItem(RuntimeItem runtimeItem)
        {
            var rect = runtimeItem.Node.GetComponent<RectTransform>();
            if (rect == null)
            {
                return;
            }

            var index = runtimeItem.Index;
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, -_posY[index]);
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, _heights[index]);
        }

        private void RecycleItem(RuntimeItem runtimeItem)
        {
            runtimeItem.Node.SetActive(false);
            _displayedItems.Remove(runtimeItem);
            _displayedIndexSet.Remove(runtimeItem.Index);
            GetOrCreateCache(runtimeItem.PrefabTypeIndex).AddLast(runtimeItem);
        }

        private void RecycleAllDisplayedItems()
        {
            var node = _displayedItems.First;
            while (node != null)
            {
                var next = node.Next;
                var runtimeItem = node.Value;
                runtimeItem.Node.SetActive(false);
                GetOrCreateCache(runtimeItem.PrefabTypeIndex).AddLast(runtimeItem);
                node = next;
            }

            _displayedItems.Clear();
            _displayedIndexSet.Clear();
            _lastStartIndex = -1;
            _lastEndIndex = -1;
        }

        private void ReleaseRuntimeNode(RuntimeItem runtimeItem)
        {
            if (runtimeItem == null || runtimeItem.Node == null)
            {
                return;
            }

            runtimeItem.ScrollItem?.OnRelease();

            if (ReleaseRuntimeAction != null)
            {
                ReleaseRuntimeAction.Invoke(runtimeItem.Node);
            }
            else
            {
                Destroy(runtimeItem.Node);
            }
        }

        private static float GetPreferredHeight(ScrollItem scrollItem)
        {
            if (scrollItem is IVariableHeightScrollItem variableHeightItem)
            {
                return variableHeightItem.GetPreferredHeight();
            }

            var rect = scrollItem.TweenTarget != null
                ? scrollItem.TweenTarget
                : scrollItem.GetComponent<RectTransform>();
            return rect.rect.height;
        }
    }
}
