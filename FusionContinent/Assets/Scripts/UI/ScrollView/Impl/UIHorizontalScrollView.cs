using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace ScrollView
{
    public class UIHorizontalScrollView : UIScrollView
    {
        public ScrollItem ItemPrefab;
        
        private float _cellWidth;
        private float _cellHeight;
        private int _totalColumns;
        private int _lastStartCol = -1;
        private int _lastEndCol = -1;
        
        public float TailSpacing;

        protected override void OnStart()
        {
            SetScrollViewMode();

            if (ContentRect != null)
            {
                ContentRect.anchorMin = new Vector2(0, 1);
                ContentRect.anchorMax = new Vector2(0, 1);
                ContentRect.pivot = new Vector2(0, 1);
                ContentRect.anchoredPosition = Vector2.zero;
            }
            
            _cellWidth = ItemSize.x + ItemSpacing.x;
            _cellHeight = ItemSize.y + ItemSpacing.y;
            ItemsPerLine = ComputeRowsPerColumn();
        }

        protected override void SetScrollViewMode()
        {
            if (ScrollRect != null)
            {
                ScrollRect.horizontal = true;
                ScrollRect.vertical = false;
            }
        }

        protected override void SetListCount(int count)
        {
            DataCount = Mathf.Max(0, count);
            EnsureContentSize(DataCount);
            ForceRefreshVisible();
        }

        private void EnsureContentSize(int count)
        {
            ItemsPerLine = ComputeRowsPerColumn();
            _totalColumns = Mathf.CeilToInt((float)count / Mathf.Max(1, ItemsPerLine));
            var width = _totalColumns > 0
                ? _totalColumns * ItemSize.x + (_totalColumns - 1) * ItemSpacing.x
                : 0f;
            var height = ItemsPerLine > 0
                ? ItemsPerLine * ItemSize.y + (ItemsPerLine - 1) * ItemSpacing.y
                : 0f;

            var size = ContentRect.sizeDelta;
            size.x = width+TailSpacing;
            var viewportH = Viewport != null ? Viewport.rect.height : size.y;
            size.y = Mathf.Max(viewportH, height);
            ContentRect.sizeDelta = size;
        }

        protected virtual int ComputeRowsPerColumn()
        {
            var viewportH = Viewport != null ? Viewport.rect.height : ContentRect.sizeDelta.y;
            var denom = ItemSize.y + ItemSpacing.y;
            if (denom <= 0f) return 1;
            var rows = Mathf.FloorToInt((viewportH + ItemSpacing.y) / denom);
            return Mathf.Max(1, rows);
        }

        private void ForceRefreshVisible()
        {
            _lastStartCol = -1;
            _lastEndCol = -1;
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
                    t.localScale = Vector3.one;
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
                    rect.anchorMin = new Vector2(0, 1);
                    rect.anchorMax = new Vector2(0, 1);
                    rect.pivot = new Vector2(0, 1);
                    rect.sizeDelta = ItemSize;
                }
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
            var column = Mathf.FloorToInt(dataIndex / Mathf.Max(1, ItemsPerLine));
            var row = dataIndex % Mathf.Max(1, ItemsPerLine);

            var x = column * (ItemSize.x + ItemSpacing.x);
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
            // Compute horizontal offset: content shifted left => anchoredPosition.x is negative
            var scrollOffsetX = Mathf.Max(0f, -ContentRect.anchoredPosition.x);
            var viewportWidth = Viewport.rect.width;

            var startCol = Mathf.FloorToInt(scrollOffsetX / _cellWidth);
            var endCol = Mathf.FloorToInt((scrollOffsetX + viewportWidth - 1f) / _cellWidth);

            // Add 1-column buffer to avoid flicker
            startCol = Mathf.Max(0, startCol - 1);
            endCol = Mathf.Min(_totalColumns - 1, endCol + 1);

            if (!force && startCol == _lastStartCol && endCol == _lastEndCol) return;

            _lastStartCol = startCol;
            _lastEndCol = endCol;

            // Build needed indices for visible columns
            var needed = new HashSet<int>();
            for (int col = startCol; col <= endCol; col++)
            {
                for (int row = 0; row < ItemsPerLine; row++)
                {
                    int index = col * ItemsPerLine + row;
                    if (index >= DataCount) break;
                    needed.Add(index);
                }
            }

            // Recycle items not needed (do not call ReleaseRuntimeAction during normal virtualization)
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
                        tr.localScale = Vector3.one;
                    }
                    node.Value.Node.SetActive(false);
                    cachedItems.AddLast(node.Value);
                    _displayedIndexSet.Remove(node.Value.Index);
                    displayedItems.Remove(node);
                }
                else
                {
                    //强制刷新的时候调用一下
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

            // Add missing items
            foreach (var idx in needed)
            {
                if (_displayedIndexSet.Contains(idx)) continue;
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

            // Explicit release API to free resources
            // Only call ReleaseRuntimeAction inside ReleaseScrollView()
        }

        public override void ReleaseScrollView()
        {
            base.ReleaseScrollView();
            _displayedIndexSet.Clear();
            _lastStartCol = -1;
            _lastEndCol = -1;
            _totalColumns = 0;
        }
    }
}