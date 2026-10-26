using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace ScrollView
{
    [RequireComponent(typeof(ScrollRect))]
    public class UIScrollView : MonoBehaviour
    {
        #region 更新Item的接口回调
        /// <summary>
        /// 刷新显示信息
        /// </summary>
        public delegate void UpdateItem(int dataIndex,ScrollItem scrollItem);
        public UpdateItem OnUpdateItem;
        public virtual void SetUpdateFunc(UpdateItem onUpdate)
        {
            OnUpdateItem = onUpdate;
        }
        
        public Func<RectTransform,GameObject> PrefabFactory;

        public virtual void SetPrefabFactory(Func<RectTransform,GameObject> prefabFactory)
        {
            PrefabFactory = prefabFactory;
        }

        public Action<GameObject> ReleaseRuntimeAction;

        public virtual void SetReleaseAction(Action<GameObject> releaseAction)
        {
            ReleaseRuntimeAction = releaseAction;
        }
        
        /// <summary>
        /// 刷新选中
        /// </summary>
        public UpdateItem OnUpdateSelectItem;
        
        public virtual void SetUpdateSelectFunc(UpdateItem onUpdate)
        {
            OnUpdateSelectItem = onUpdate;
        }
        #endregion

        #region 私有组件

        protected ScrollRect ScrollRect;
        protected RectTransform ContentRect;
        protected RectTransform Viewport;
        #endregion

        #region 数据结构

        public class Item
        {
            public int Index;
            public ScrollItem ScrollItem;
            public GameObject Node;
        }

        /// <summary>
        /// display items
        /// </summary>
        protected LinkedList<Item> displayedItems = new LinkedList<Item>();
        
        /// <summary>
        /// hide items
        /// </summary>
        protected LinkedList<Item> cachedItems = new LinkedList<Item>();
        
        
        #endregion

        public Vector2 contentPivot = new Vector2(0, 1);
        public Vector2 anchorMin = new  Vector2(0, 1);
        public Vector2 anchorMax = new  Vector2(0, 1);
        protected readonly HashSet<int> _displayedIndexSet = new HashSet<int>();
        public ScrollViewMode ScrollViewMode = ScrollViewMode.Horizontal;
        public Vector2 ItemSize;
        public Vector2 ItemSpacing;
        // Items per visual line (columns for vertical scrolling, rows for horizontal)
        public int ItemsPerLine = 1;
        public float duration;
        private Coroutine _scaleCoroutine;
        public float ScaleRate = 1f;
        private void Awake()
        {
            ScrollRect = GetComponent<ScrollRect>();
            ContentRect = ScrollRect.content;
            Viewport =  ScrollRect.viewport;
            var layout = ContentRect.GetComponent<LayoutGroup>();
            if (layout)
            {
                layout.enabled = false;
            }
        }

        private void Start()
        {
            OnStart();
        }

        protected virtual void OnStart()
        {
        
        }

        protected virtual void SetScrollViewMode()
        {
            
        }
        
        protected virtual void SetListCount(int count)
        {
            
        }

        protected int DataCount;
        public void ShowList(int count)
        {

            DataCount = count;
            // ReleaseScrollView();
            // _displayedIndexSet.Clear();
            // _animatedIndexSet.Clear();
            // _animatedIndexCompleteSet.Clear();
            // _initialBatchSet.Clear();
            SetScrollViewMode();
            SetListCount(count);
        }

        // Release all runtime-created nodes via external release or destruction.
        // Only invoked when caller explicitly requests releasing resources.
        public virtual void ReleaseScrollView()
        {
            // Release displayed items
            foreach (var item in displayedItems)
            {
                if (item?.Node == null) continue;
                if (ReleaseRuntimeAction != null)
                {
                    ReleaseRuntimeAction.Invoke(item.Node);
                }
                else
                {
                    UnityEngine.Object.Destroy(item.Node);
                }
            }

            // Release cached items
            foreach (var item in cachedItems)
            {
                if (item?.Node == null) continue;
                if (ReleaseRuntimeAction != null)
                {
                    ReleaseRuntimeAction.Invoke(item.Node);
                }
                else
                {
                    UnityEngine.Object.Destroy(item.Node);
                }
            }

            displayedItems.Clear();
            cachedItems.Clear();
            DataCount = 0;
            if (_seqCoroutine != null)
            {
                StopCoroutine(_seqCoroutine);
                _seqCoroutine = null;
            }
            _animatedIndexSet.Clear();
            _initialBatchOrder.Clear();
            _initialBatchSet.Clear();
            _initialBatchCompleted = false;
            if (_reenableDragCoroutine != null)
            {
                StopCoroutine(_reenableDragCoroutine);
                _reenableDragCoroutine = null;
            }
            if (ScrollRect != null)
            {
                ScrollRect.horizontal = _prevHorizontal;
                ScrollRect.vertical = _prevVertical;
            }
        }

        // Clear only cached items while keeping currently displayed items intact.
        // Uses ReleaseRuntimeAction if provided, otherwise destroys cached nodes.
        public virtual void ClearCacheKeepDisplayed()
        {
            foreach (var item in cachedItems)
            {
                if (item?.Node == null) continue;
                if (ReleaseRuntimeAction != null)
                {
                    ReleaseRuntimeAction.Invoke(item.Node);
                }
                else
                {
                    UnityEngine.Object.Destroy(item.Node);
                }
            }
            cachedItems.Clear();
        }
        

        public virtual void RefreshSelect()
        {
            if (OnUpdateSelectItem == null) return;
            var node = displayedItems.First;
            while (node != null)
            {
                var item = node.Value;
                if (item != null && item.ScrollItem != null)
                {
                    OnUpdateSelectItem.Invoke(item.Index, item.ScrollItem);
                }
                node = node.Next;
            }
        }
        public Ease ease = Ease.OutBack;
        public float stagger = 0f;
        public bool SequentialScale = true;
        private Coroutine _seqCoroutine;
        protected readonly HashSet<int> _animatedIndexSet = new HashSet<int>();
        protected readonly HashSet<int> _animatedIndexCompleteSet = new HashSet<int>();
        private bool _prevHorizontal;
        private bool _prevVertical;
        private Coroutine _reenableDragCoroutine;
        
        
        private readonly List<int> _initialBatchOrder = new List<int>();
        private readonly HashSet<int> _initialBatchSet = new HashSet<int>();
        private bool _initialBatchCompleted = false;
        protected void QueueScaleAnimationForItem(Item item)
        {
            if (!SequentialScale || item == null || item.Node == null) return;
            var rect = item.ScrollItem != null && item.ScrollItem.TweenTarget != null
                ? item.ScrollItem.TweenTarget
                : item.Node.GetComponent<RectTransform>();
            if (rect == null) return;
            DOTween.Kill(rect, false);
            if (_initialBatchCompleted)
            {
                rect.localScale = Vector3.one * ScaleRate;
                return;
            }
            if (_initialBatchSet.Count == 0)
            {
                rect.localScale = Vector3.one * ScaleRate;
                return;
            }
            if (_initialBatchSet.Contains(item.Index))
            {
                rect.localScale = _animatedIndexSet.Contains(item.Index) ? Vector3.one*ScaleRate : Vector3.zero;
            }
            else
            {
                if (_initialBatchCompleted)
                {
                    rect.localScale = Vector3.one*ScaleRate;
                }
                else
                {
                    // 动画窗口期间新进入可视的索引也加入初始批次，并按未播设为0
                    _initialBatchSet.Add(item.Index);
                    if (!_initialBatchOrder.Contains(item.Index))
                    {
                        _initialBatchOrder.Add(item.Index);
                    }
                    rect.localScale = _animatedIndexSet.Contains(item.Index) ? Vector3.one*ScaleRate : Vector3.zero;
                }
            }
        }
        public virtual void PlayScaleAnimationDOTween()
        {
            if (!this.gameObject.activeInHierarchy)
            {
                return;
            }
            if (ScrollRect != null)
            {
                _prevHorizontal = ScrollRect.horizontal;
                _prevVertical = ScrollRect.vertical;
                ScrollRect.horizontal = false;
                ScrollRect.vertical = false;
            }
            if (_reenableDragCoroutine != null)
            {
                StopCoroutine(_reenableDragCoroutine);
                _reenableDragCoroutine = null;
            }
            if (_seqCoroutine != null)
            {
                StopCoroutine(_seqCoroutine);
                _seqCoroutine = null;
            }
            _animatedIndexCompleteSet.Clear();
            _animatedIndexSet.Clear();
            _initialBatchOrder.Clear();
            _initialBatchSet.Clear();
            _initialBatchCompleted = false;
            var node = displayedItems.First;
            while (node != null)
            {
                var currentCollect = node.Value;
                node = node.Next;
                if (currentCollect != null)
                {
                    _initialBatchOrder.Add(currentCollect.Index);
                }
            }
            _initialBatchOrder.Sort();
            foreach (var idx in _initialBatchOrder)
            {
                _initialBatchSet.Add(idx);
            }
            var nodePreset = displayedItems.First;
            while (nodePreset != null)
            {
                var currentPreset = nodePreset.Value;
                nodePreset = nodePreset.Next;
                QueueScaleAnimationForItem(currentPreset);
            }
            if (SequentialScale)
            {
                _seqCoroutine = StartCoroutine(SequentialAnimateInitialBatch());
            }
            else
            {
                var preset = displayedItems.First;
                while (preset != null)
                {
                    var presetItem = preset.Value;
                    preset = preset.Next;
                    if (presetItem == null || presetItem.Node == null) continue;
                    var presetRect = presetItem.ScrollItem != null && presetItem.ScrollItem.TweenTarget != null
                        ? presetItem.ScrollItem.TweenTarget
                        : presetItem.Node.GetComponent<RectTransform>();
                    if (presetRect == null) continue;
                    presetRect.localScale = Vector3.zero;
                }

                var node2 = displayedItems.First;
                int i = 0;
                while (node2 != null)
                {
                    var current = node2.Value;
                    node2 = node2.Next;
                    if (current == null || current.Node == null) continue;

                    var rect = current.ScrollItem != null && current.ScrollItem.TweenTarget != null
                        ? current.ScrollItem.TweenTarget
                        : current.Node.GetComponent<RectTransform>();
                    if (rect == null) continue;

                    DOTween.Kill(rect, complete: false);
                    var delay = stagger > 0f ? i * stagger : 0f;
                    rect.DOScale(Vector3.one, Mathf.Max(0.0001f, duration))
                        .SetEase(ease)
                        .SetDelay(delay);
                    i++;
                }
                int visibleCount = 0;
                var calc = displayedItems.First;
                while (calc != null)
                {
                    visibleCount++;
                    calc = calc.Next;
                }
                var total = Mathf.Max(0.0001f, duration) + (stagger > 0f ? Mathf.Max(0, visibleCount - 1) * stagger : 0f);
                if (ScrollRect != null)
                {
                    _reenableDragCoroutine = StartCoroutine(ReenableDragAfter(total));
                }
            }
        }

        private IEnumerator SequentialAnimateInitialBatch()
        {
            for (int k = 0; k < _initialBatchOrder.Count; k++)
            {
                var i = _initialBatchOrder[k];
                if (_animatedIndexSet.Contains(i)) continue;
                Item targetItem = null;
                var node = displayedItems.First;
                while (node != null)
                {
                    if (node.Value != null && node.Value.Index == i)
                    {
                        targetItem = node.Value;
                        break;
                    }
                    node = node.Next;
                }
                if (targetItem == null)
                {
                    _animatedIndexSet.Add(i);
                    continue;
                }
                var rect = targetItem.ScrollItem != null && targetItem.ScrollItem.TweenTarget != null
                    ? targetItem.ScrollItem.TweenTarget
                    : targetItem.Node.GetComponent<RectTransform>();
                if (rect == null)
                {
                    _animatedIndexSet.Add(i);
                    continue;
                }
                DOTween.Kill(rect, false);
                rect.localScale = Vector3.zero;
                rect.DOScale(Vector3.one*ScaleRate, Mathf.Max(0.0001f, duration)).SetEase(ease).OnComplete(() =>
                {
                    _animatedIndexCompleteSet.Add(i);
                });
                yield return new WaitForSeconds(stagger);
                _animatedIndexSet.Add(i);
            }

            // yield return new WaitUntil(() => CheckAnimationComplete());
            
            _initialBatchCompleted = true;
            if (ScrollRect != null)
            {
                ScrollRect.horizontal = _prevHorizontal;
                ScrollRect.vertical = _prevVertical;
            }
            _seqCoroutine = null;
        }

        private IEnumerator ReenableDragAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            if (ScrollRect != null)
            {
                ScrollRect.horizontal = _prevHorizontal;
                ScrollRect.vertical = _prevVertical;
            }
            _reenableDragCoroutine = null;
        }


        bool CheckAnimationComplete()
        {
            foreach (var idx in _animatedIndexSet)
            {
                if (!_animatedIndexCompleteSet.Contains(idx))
                {
                    return false;
                }
            }

            return true;
        }

        // Scroll to a specific data index with optional alignment and tween
        public void ScrollToIndex(int index, ScrollAlignment alignment = ScrollAlignment.Start, bool animated = true, float tweenDuration = 0.25f)
        {
            if (ScrollRect == null || ContentRect == null || Viewport == null) return;
            if (DataCount <= 0) return;

            // Clamp index
            index = Mathf.Clamp(index, 0, Mathf.Max(0, DataCount - 1));

            // Grid support: items per line (use at least 1)
            int perLine = Mathf.Max(1, ItemsPerLine);
            int lineIndex = index / perLine; // which visual line contains this index

            // Sizes along scrolling axis
            float itemSize = ScrollViewMode == ScrollViewMode.Horizontal ? ItemSize.x : ItemSize.y;
            float spacing = ScrollViewMode == ScrollViewMode.Horizontal ? ItemSpacing.x : ItemSpacing.y;
            int lineCount = Mathf.CeilToInt((float)DataCount / perLine);
            float contentSize = lineCount * itemSize + Mathf.Max(0, lineCount - 1) * spacing;
            float viewportSize = ScrollViewMode == ScrollViewMode.Horizontal ? Viewport.rect.width : Viewport.rect.height;
            float scrollable = Mathf.Max(0, contentSize - viewportSize);

            // Start position of the requested item
            float itemStart = lineIndex * (itemSize + spacing);
            // LogTool.Log(itemStart);
            // Alignment offset
            float alignedStart = itemStart;
            switch (alignment)
            {
                case ScrollAlignment.Center:
                    alignedStart = itemStart - (viewportSize - itemSize) * 0.5f;
                    break;
                case ScrollAlignment.End:
                    alignedStart = itemStart - (viewportSize - itemSize);
                    break;
                case ScrollAlignment.Start:
                default:
                    break;
            }

            // Clamp to scrollable range
            alignedStart = Mathf.Clamp(alignedStart, 0f, scrollable);

            // Convert to normalized position
            if (ScrollViewMode == ScrollViewMode.Horizontal)
            {
                float target = scrollable <= 0.0001f ? 0f : alignedStart / scrollable;
                if (animated)
                {
                    DOTween.Kill(ScrollRect, false);
                    DOTween.To(() => ScrollRect.horizontalNormalizedPosition,
                               v => ScrollRect.horizontalNormalizedPosition = v,
                               target, tweenDuration)
                           .SetEase(ease)
                           .SetTarget(ScrollRect);
                }
                else
                {
                    ScrollRect.horizontalNormalizedPosition = target;
                }
            }
            else
            {
                // Unity verticalNormalizedPosition: 1 = top (offset 0), 0 = bottom (offset scrollable)
                float target = scrollable <= 0.0001f ? 1f : 1f - (alignedStart / scrollable);
                if (animated)
                {
                    DOTween.Kill(ScrollRect, false);
                    DOTween.To(() => ScrollRect.verticalNormalizedPosition,
                               v => ScrollRect.verticalNormalizedPosition = v,
                               target, tweenDuration)
                           .SetEase(ease)
                           .SetTarget(ScrollRect);
                }
                else
                {
                    ScrollRect.verticalNormalizedPosition = target;
                }
            }
        }
    }


    public enum ScrollAlignment
    {
        Center,
        Start,
        End,
    }
    public enum ScrollViewMode
    {
        Horizontal,
        Vertical,
    }
}


