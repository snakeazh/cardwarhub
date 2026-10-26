using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ScrollView
{
    /// <summary>
    /// 支持底部上拉加载更多的竖向 ScrollView（排行榜场景）：
    /// 1. 列表滚到底部时继续上拉超过 PullRefreshThreshold 并松手，进入等待状态；
    ///    等弹性回弹结束后再触发 OnPullRefresh（在此处发送服务器请求），避免列表在回弹过程中重建；
    /// 2. 服务器数据返回后调用 AppendData 追加数据并刷新界面，滚动位置保持不变；
    /// 3. 请求失败或无需追加时调用 EndRefresh 解除等待状态。
    /// </summary>
    public class UIVerticalScrollViewLockRank : UIVerticalScrollViewLock, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Tooltip("底部上拉触发加载的距离（像素）")]
        public float PullRefreshThreshold = 100f;

        [Tooltip("等待回弹结束的最长时间（秒），超时强制触发，避免回调丢失")]
        public float ReboundTimeout = 1f;

        /// <summary>
        /// 底部上拉超过阈值、松手且回弹结束时触发（在这里发送服务器请求）
        /// </summary>
        public Action OnPullRefresh;

        private float _pullDistance;
        private bool _waitingResponse;
        private Coroutine _reboundCoroutine;

        /// <summary>
        /// 是否正在等待服务器返回（等待期间不会重复触发加载）
        /// </summary>
        public bool IsWaitingResponse => _waitingResponse;

        public virtual void SetPullRefreshCallback(Action onPullRefresh)
        {
            OnPullRefresh = onPullRefresh;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _pullDistance = 0f;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_waitingResponse || ContentRect == null) return;

            // 只有内容已滚到底部时，继续向上拖才累计上拉距离（兼容 Elastic/Clamped 两种 MovementType）
            if (IsAtBottom() && eventData.delta.y > 0f)
            {
                _pullDistance += eventData.delta.y;
            }
            else
            {
                _pullDistance = 0f;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            var shouldRefresh = !_waitingResponse && _pullDistance >= PullRefreshThreshold;
            _pullDistance = 0f;

            if (shouldRefresh)
            {
                _waitingResponse = true;
                if (_reboundCoroutine != null)
                {
                    StopCoroutine(_reboundCoroutine);
                }
                _reboundCoroutine = StartCoroutine(FireRefreshAfterRebound());
            }
        }

        /// <summary>
        /// 等弹性回弹结束（内容回到边界内且滑动速度归零）后再触发刷新回调，超时强制触发
        /// </summary>
        private IEnumerator FireRefreshAfterRebound()
        {
            var elapsed = 0f;
            while (elapsed < ReboundTimeout && !IsReboundFinished())
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            _reboundCoroutine = null;
            OnPullRefresh?.Invoke();
        }

        /// <summary>
        /// 回弹是否结束：内容已回到滚动边界内，且滑动速度基本归零
        /// </summary>
        private bool IsReboundFinished()
        {
            if (ScrollRect == null || ContentRect == null) return true;

            var viewportH = Viewport != null ? Viewport.rect.height : 0f;
            var maxScroll = Mathf.Max(0f, ContentRect.sizeDelta.y - viewportH);
            var y = ContentRect.anchoredPosition.y;
            var inBounds = y >= -0.5f && y <= maxScroll + 0.5f;
            var settled = ScrollRect.velocity.sqrMagnitude < 1f;
            return inBounds && settled;
        }

        /// <summary>
        /// 服务器数据返回后调用：追加 addedCount 条数据并刷新界面（滚动位置保持不变）
        /// </summary>
        /// <param name="addedCount">本次追加的数据条数，传 0 表示没有更多数据</param>
        public void AppendData(int addedCount)
        {
            _waitingResponse = false;
            if (addedCount <= 0) return;
            Refresh(DataCount + addedCount);
        }

        /// <summary>
        /// 请求失败或本次不需要追加数据时调用，解除等待状态
        /// </summary>
        public void EndRefresh()
        {
            _waitingResponse = false;
            _pullDistance = 0f;
        }

        public override void ReleaseScrollView()
        {
            base.ReleaseScrollView();
            StopReboundCoroutine();
            _waitingResponse = false;
            _pullDistance = 0f;
        }

        private void OnDisable()
        {
            StopReboundCoroutine();
        }

        private void StopReboundCoroutine()
        {
            if (_reboundCoroutine != null)
            {
                StopCoroutine(_reboundCoroutine);
                _reboundCoroutine = null;
            }
        }

        /// <summary>
        /// 内容是否已滚到底部（内容不足一屏时视为始终在底部，允许直接上拉加载）
        /// </summary>
        private bool IsAtBottom()
        {
            var viewportH = Viewport != null ? Viewport.rect.height : 0f;
            var maxScroll = Mathf.Max(0f, ContentRect.sizeDelta.y - viewportH);
            return ContentRect.anchoredPosition.y >= maxScroll - 0.01f;
        }
    }
}
