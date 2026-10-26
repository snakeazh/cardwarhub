using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace ScrollView
{
    public class UIVerticalScrollView : UIScrollView
    {
        public ScrollItem ItemPrefab;
        
        private float _cellWidth;
        private float _cellHeight;
        private int _totalRows;
        private int _lastStartRow = -1;
        private int _lastEndRow = -1;
       
        // 在 UIVerticalScrollView 里加一个开关（公开字段，可以在 Inspector 勾选）
        public bool AutoFitHorizontalSpacing = false;
        protected override void OnStart()
        {

            SetScrollViewMode();
            if (ContentRect != null)
            {
                ContentRect.anchorMin = anchorMin;
                ContentRect.anchorMax = anchorMax;
                ContentRect.pivot = contentPivot;
                ContentRect.anchoredPosition = Vector2.zero;
            }

            // Ensure viewport available; fallback to ScrollRect transform if viewport is null
            if (Viewport == null && ScrollRect != null)
            {
                Viewport = ScrollRect.transform as RectTransform;
            }
            if (AutoFitHorizontalSpacing && Viewport != null && ItemsPerLine > 1)
            {
                var viewportW = Viewport.rect.width;
                var totalItemWidth = ItemsPerLine * ItemSize.x;
                var availableSpacing = viewportW - totalItemWidth;
                var spacingX = Mathf.Max(0f, availableSpacing / (ItemsPerLine - 1));
                ItemSpacing = new Vector2(spacingX, ItemSpacing.y);
            }
            _cellWidth = ItemSize.x + ItemSpacing.x;
            _cellHeight = ItemSize.y + ItemSpacing.y;
            ItemsPerLine = ComputeColumnsPerRow();
        }

        protected override void SetScrollViewMode()
        {
            if (ScrollRect != null)
            {
                ScrollRect.vertical = true;
                ScrollRect.horizontal = false;
            }
        }

        protected override void SetListCount(int count)
        {
            DataCount = Mathf.Max(0, count);
            // _displayedIndexSet.Clear();
            EnsureContentSize(DataCount);
            ForceRefreshVisible();
        }

        private void EnsureContentSize(int count)
        {
            ItemsPerLine = ComputeColumnsPerRow();
            _totalRows = Mathf.CeilToInt((float)count / Mathf.Max(1, ItemsPerLine));

            var width = ItemsPerLine > 0
                ? ItemsPerLine * ItemSize.x + (ItemsPerLine - 1) * ItemSpacing.x
                : 0f;
            var height = _totalRows > 0
                ? _totalRows * ItemSize.y + (_totalRows - 1) * ItemSpacing.y
                : 0f;

            var size = ContentRect.sizeDelta;
            var viewportW = Viewport != null ? Viewport.rect.width : size.x;
            size.x = Mathf.Max(viewportW, width);
            size.y = height;
            ContentRect.sizeDelta = size;
        }

        protected virtual int ComputeColumnsPerRow()
        {
            var viewportW = Viewport != null ? Viewport.rect.width : ContentRect.sizeDelta.x;
            var denom = ItemSize.x + ItemSpacing.x;
            if (denom <= 0f) return 1;
            var cols = Mathf.FloorToInt((viewportW + ItemSpacing.x) / denom);
            return Mathf.Max(1, cols);
        }

        private void ForceRefreshVisible()
        {
            _lastStartRow = -1;
            _lastEndRow = -1;
            UpdateVisibleItems(true);
        }

        private Item AcquireItem()
        {
            // Always reuse from internal cache first, regardless of PrefabFactory/ReleaseRuntimeAction.
            Item item;
            if (cachedItems.Count > 0)
            {
                item = cachedItems.First.Value;
                cachedItems.RemoveFirst();
                item.Node.SetActive(true);
                var t = item.ScrollItem != null && item.ScrollItem.TweenTarget != null
                    ? item.ScrollItem.TweenTarget
                    : item.Node.GetComponent<RectTransform>();
                if (t != null)
                {
                    DOTween.Kill(t, false);
                    t.localScale = Vector3.one * ScaleRate;
                }
            }
            else
            {
                var go = PrefabFactory != null
                    ? PrefabFactory.Invoke(ContentRect)
                    : Object.Instantiate(ItemPrefab.gameObject, ContentRect);
                if (PrefabFactory== null)
                {
                    var itemx = go.GetComponent<ScrollItem>();
                    itemx.OnSpawn();
                }
                var rect = go.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchorMin = anchorMin;
                    rect.anchorMax = anchorMax;
                    rect.pivot = contentPivot;
                    rect.sizeDelta = ItemSize;
                }
                // Activate in case prefab is inactive
                go.SetActive(true);

                item = new Item
                {
                    ScrollItem = go.GetComponent<ScrollItem>(),
                    Node = go
                };
            }
            return item;
        }

        private void PositionItem(Item item, int dataIndex)
        {
            var rect = item.Node.GetComponent<RectTransform>();
            var row = Mathf.FloorToInt(dataIndex / Mathf.Max(1, ItemsPerLine));
            var col = dataIndex % Mathf.Max(1, ItemsPerLine);

            var x = col * (ItemSize.x + ItemSpacing.x);
            var y = -row * (ItemSize.y + ItemSpacing.y);
            rect.sizeDelta = ItemSize;
            rect.anchoredPosition = new Vector2(x, y);
        }

        public void Refresh(int count)
        {
            
            SetListCount(count);
        }

        private void LateUpdate()
        {
            if (DataCount <= 0 || Viewport == null) return;
            UpdateVisibleItems(false);
        }

        private void UpdateVisibleItems(bool force)
        {
            // content moved up => anchoredPosition.y increases; clamp to >=0
            var scrollOffsetY = Mathf.Max(0f, ContentRect.anchoredPosition.y);
            var viewportHeight = Viewport.rect.height;

            var startRow = Mathf.FloorToInt(scrollOffsetY / _cellHeight);
            var endRow = Mathf.FloorToInt((scrollOffsetY + viewportHeight - 1f) / _cellHeight);

            // buffer 1 row to reduce flicker
            startRow = Mathf.Max(0, startRow - 1);
            endRow = Mathf.Min(_totalRows - 1, endRow + 1);

            if (!force && startRow == _lastStartRow && endRow == _lastEndRow) return;

            _lastStartRow = startRow;
            _lastEndRow = endRow;

            var needed = new HashSet<int>();
            for (int row = startRow; row <= endRow; row++)
            {
                for (int col = 0; col < ItemsPerLine; col++)
                {
                    int index = row * ItemsPerLine + col;
                    if (index >= DataCount) break;
                    needed.Add(index);
                }
            }
            

            // recycle items not needed (do not call ReleaseRuntimeAction during normal virtualization)
            var node = displayedItems.First;
            while (node != null)
            {
                var next = node.Next;
                if (!needed.Contains(node.Value.Index))
                {
                    var tr = node.Value.ScrollItem != null && node.Value.ScrollItem.TweenTarget != null
                        ? node.Value.ScrollItem.TweenTarget
                        : node.Value.Node.GetComponent<RectTransform>();
                    if (tr != null)
                    {
                        DOTween.Kill(tr, false);
                        tr.localScale = Vector3.one * ScaleRate;
                    }
                    node.Value.Node.SetActive(false);
                    cachedItems.AddLast(node.Value);
                    _displayedIndexSet.Remove(node.Value.Index);
                    displayedItems.Remove(node);
                }
                else
                {
                    if (force)
                    {
                        if (OnUpdateItem != null && node.Value.ScrollItem != null)
                        {
                            OnUpdateItem.Invoke(node.Value.Index, node.Value.ScrollItem);
                        }
                    }
                }
                node = next;
            }
            
            // add missing items
            foreach (var idx in needed)
            {
                if ( _displayedIndexSet.Contains(idx)) continue;
                var item = AcquireItem();
                item.Index = idx;
                PositionItem(item, idx);
                displayedItems.AddLast(item);
                _displayedIndexSet.Add(idx);
                QueueScaleAnimationForItem(item);

                if (OnUpdateItem != null && item.ScrollItem != null)
                {
                    OnUpdateItem.Invoke(idx, item.ScrollItem);
                }
            }
        }

        public override void ReleaseScrollView()
        {
            base.ReleaseScrollView();
            _displayedIndexSet.Clear();
            _lastStartRow = -1;
            _lastEndRow = -1;
            _totalRows = 0;
        }
    }
}