using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FusionContinent.UI.Components
{
    /// <summary>
    /// 战斗技能卡横向 ScrollView：超过 5 张时，视口内连续 5 张正常大小，边缘卡牌随滑动缩小。
    /// 缩放窗口由 horizontalNormalizedPosition 推算起始 index；越界时固定首尾 5 张。
    /// </summary>
    [DisallowMultipleComponent]
    public class BattleSkillCardScrollView : ScrollRect
    {
        [SerializeField] private float mNormalScale = 1f;
        [SerializeField] private float mEdgeScale = 0.72f;
        [SerializeField] private int mNormalVisibleCount = 5;
        [SerializeField] private float mCardBaseWidth = 249f;
        [SerializeField] private float mEdgeCardLayoutWidth = 144f;
        [SerializeField] private int mCenterSortingOrder = 150;
        [SerializeField] private int mEdgeSortingOrder = 140;
        [SerializeField] private int mLayerSortingStep = 1;
        [SerializeField] private float mDisplayTransitionDuration = 0.2f;

        private bool mPendingLayoutRebuild;

        protected override void Awake()
        {
            base.Awake();
            onValueChanged.AddListener(OnScrollValueChanged);
        }

        protected override void Start()
        {
            base.Start();
            UpdateCardScales();
        }

        protected override void OnDestroy()
        {
            onValueChanged.RemoveListener(OnScrollValueChanged);
            base.OnDestroy();
        }

        private void MarkLayoutDirty()
        {
            mPendingLayoutRebuild = true;
        }

        protected override void LateUpdate()
        {
            base.LateUpdate();
            UpdateCardScales();
        }

        public void RefreshCardItems()
        {
            RefreshCardItems(horizontalNormalizedPosition);
        }

        public void RefreshCardItems(float preserveNormalizedPosition)
        {
            if (content != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            }

            var cardItems = CollectCardItems();
            foreach (var item in cardItems)
            {
                item.ResetDisplayStateCache();
                item.SnapDisplayState(true, mNormalScale, mCardBaseWidth);
            }

            mPendingLayoutRebuild = true;
            RebuildContentLayoutIfNeeded();
            RestoreHorizontalNormalizedPosition(preserveNormalizedPosition);
            UpdateCardScales();
        }

        /// <summary>
        /// 保留当前滚动位置，重新计算各卡牌缩放与布局。
        /// </summary>
        public void RefreshDisplayStates()
        {
            var cardItems = CollectCardItems();
            foreach (var item in cardItems)
            {
                item.ResetDisplayStateCache();
            }

            mPendingLayoutRebuild = true;
            UpdateCardScales();
        }

        public void RestoreHorizontalNormalizedPosition(float normalizedPosition)
        {
            horizontalNormalizedPosition = Mathf.Clamp01(normalizedPosition);
        }

        public void SetScrollEnabled(bool enabled)
        {
            horizontal = enabled;
            if (!enabled)
            {
                StopMovement();
            }
        }

        private void OnScrollValueChanged(Vector2 normalizedPosition)
        {
            UpdateCardScales();
        }

        private void UpdateCardScales()
        {
            if (viewport == null || content == null)
            {
                return;
            }

            var cardItems = CollectCardItems();
            if (cardItems.Count == 0)
            {
                return;
            }

            mPendingLayoutRebuild = false;

            if (cardItems.Count <= mNormalVisibleCount)
            {
                var windowStart = 0f;
                for (int i = 0; i < cardItems.Count; i++)
                {
                    ApplyCardDisplayState(cardItems[i], true, mNormalScale, mCardBaseWidth);
                    cardItems[i].SetScrollFocusAllowed(true);
                }

                RebuildContentLayoutIfNeeded();
                for (int i = 0; i < cardItems.Count; i++)
                {
                    cardItems[i].SetVisualLayerRank(CalcSortingOrder(i, windowStart, true));
                }

                return;
            }

            var maxStartIndex = cardItems.Count - mNormalVisibleCount;
            var scrollWindowStart = GetScrollWindowStart(maxStartIndex);

            for (int i = 0; i < cardItems.Count; i++)
            {
                var isNormalSize = IsIndexInNormalWindow(i, scrollWindowStart);
                var scale = CalcScaleForIndex(i, scrollWindowStart);
                var targetWidth = isNormalSize ? mCardBaseWidth : mEdgeCardLayoutWidth;
                ApplyCardDisplayState(cardItems[i], isNormalSize, scale, targetWidth);
                cardItems[i].SetScrollFocusAllowed(isNormalSize);
            }

            RebuildContentLayoutIfNeeded();
            for (int i = 0; i < cardItems.Count; i++)
            {
                var isNormalSize = IsIndexInNormalWindow(i, scrollWindowStart);
                cardItems[i].SetVisualLayerRank(CalcSortingOrder(i, scrollWindowStart, isNormalSize));
            }
        }

        /// <summary>
        /// 根据滑动值计算正常窗口起始 index；小于 0 固定首 5 张，大于 1 固定末 5 张。
        /// </summary>
        private float GetScrollWindowStart(int maxStartIndex)
        {
            if (maxStartIndex <= 0)
            {
                return 0f;
            }

            var normalized = horizontalNormalizedPosition;
            if (normalized <= 0f)
            {
                return 0f;
            }

            if (normalized >= 1f)
            {
                return maxStartIndex;
            }

            return maxStartIndex * normalized;
        }

        private void RebuildContentLayoutIfNeeded()
        {
            if (!mPendingLayoutRebuild || content == null)
            {
                return;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            mPendingLayoutRebuild = false;
        }

        private float CalcScaleForIndex(int index, float windowStart)
        {
            return IsIndexInNormalWindow(index, windowStart) ? mNormalScale : mEdgeScale;
        }

        /// <summary>
        /// 正常窗口内按距中心递减；窗口外缩小牌统一更低，保证被中间牌压住。
        /// </summary>
        private int CalcSortingOrder(int index, float windowStart, bool isNormalSize)
        {
            if (isNormalSize)
            {
                var cardCenter = index + 0.5f;
                var windowCenter = windowStart + (mNormalVisibleCount - 1) * 0.5f;
                var distance = Mathf.Abs(cardCenter - windowCenter);

                if (distance < 0.5f)
                {
                    return mCenterSortingOrder;
                }

                if (distance < 1.5f)
                {
                    return mCenterSortingOrder - mLayerSortingStep;
                }

                if (distance < 2.5f)
                {
                    return mCenterSortingOrder - mLayerSortingStep * 2;
                }

                if (distance < 3.5f)
                {
                    return mCenterSortingOrder - mLayerSortingStep * 3;
                }

                return mCenterSortingOrder - mLayerSortingStep * 4;
            }

            var edgeCenter = index + 0.5f;
            var windowEnd = windowStart + mNormalVisibleCount;
            var outsideDistance = edgeCenter < windowStart
                ? windowStart - edgeCenter
                : edgeCenter - windowEnd;
            var edgeTier = Mathf.Clamp(Mathf.CeilToInt(outsideDistance) - 1, 0, 3);
            return mEdgeSortingOrder - edgeTier * mLayerSortingStep;
        }

        /// <summary>
        /// 卡牌中心落在正常窗口内视为可点击。
        /// </summary>
        private bool IsIndexInNormalWindow(int index, float windowStart)
        {
            var cardCenter = index + 0.5f;
            return cardCenter >= windowStart && cardCenter < windowStart + mNormalVisibleCount;
        }

        private List<BattleSkillCardScrollItem> CollectCardItems()
        {
            var result = new List<BattleSkillCardScrollItem>();
            if (content == null)
            {
                return result;
            }

            for (int i = 0; i < content.childCount; i++)
            {
                var child = content.GetChild(i);
                if (child == null || !child.gameObject.activeSelf)
                {
                    continue;
                }

                var scrollItem = child.GetComponent<BattleSkillCardScrollItem>();
                if (scrollItem == null)
                {
                    continue;
                }

                result.Add(scrollItem);
            }

            return result;
        }

        private void ApplyCardDisplayState(BattleSkillCardScrollItem item, bool isNormalSize, float targetScale,
            float targetWidth)
        {
            mPendingLayoutRebuild |= item.TweenToDisplayState(
                isNormalSize,
                targetScale,
                targetWidth,
                mDisplayTransitionDuration,
                MarkLayoutDirty);
        }
    }
}
