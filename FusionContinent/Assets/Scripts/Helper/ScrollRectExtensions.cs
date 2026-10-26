namespace FusionContinent.Helper
{
    using UnityEngine;
    using UnityEngine.UI;
    using DG.Tweening;

    public static class ScrollRectExtensions
    {
        /// <summary>
        /// 将索引对应的元素滚动到视口顶部对齐（支持 GridLayoutGroup/VerticalLayoutGroup）
        /// </summary>
        /// <param name="scrollRect">目标 ScrollRect</param>
        /// <param name="index">Content 子物体索引</param>
        /// <param name="duration">平滑时间（0 为瞬移）</param>
        /// <param name="extraTopOffset">额外向下偏移（像素），比如保留顶部留白</param>
        public static void ScrollToIndex(this ScrollRect scrollRect, int index, float duration = 0.3f, float extraTopOffset = 0f)
        {
            var content = scrollRect.content;
            if (content == null) { LogTool.LogError("ScrollRect.content 未设置"); return; }
            if (index < 0 || index >= content.childCount) { LogTool.LogError($"索引越界: {index}"); return; }

            // 确保布局完成，拿到准确尺寸
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);

            float contentHeight  = content.rect.height;
            float viewportHeight = (scrollRect.viewport ? scrollRect.viewport : scrollRect.transform as RectTransform).rect.height;
            float scrollable     = Mathf.Max(0f, contentHeight - viewportHeight);

            float topOffset = 0f; // 目标元素顶边距离 Content 顶边的像素距离

            // 1) GridLayoutGroup：按“行”来算（不会把同一行内的同列项也累加）
            var grid = content.GetComponent<GridLayoutGroup>();
            if (grid != null)
            {
                // 估算一行有多少列（常见是 FixedColumnCount；否则按宽度推算）
                int columns = grid.constraint == GridLayoutGroup.Constraint.FixedColumnCount
                    ? Mathf.Max(1, grid.constraintCount)
                    : Mathf.Max(1, Mathf.FloorToInt(
                        (content.rect.width + grid.spacing.x + 0.0001f) / (grid.cellSize.x + grid.spacing.x)));

                // 默认 Axis.Horizontal（先填列再换行）最常见：row = index / columns
                int row;
                if (grid.startAxis == GridLayoutGroup.Axis.Horizontal)
                {
                    row = index / columns;
                }
                else
                {
                    // 先填行再换列的少见配置：用行数近似推算
                    int rows = Mathf.Max(1, Mathf.CeilToInt((float)content.childCount / columns));
                    row = index % rows;
                }

                float rowHeight = grid.cellSize.y + grid.spacing.y;
                topOffset = grid.padding.top + row * rowHeight + extraTopOffset;
            }
            else
            {
                // 2) VerticalLayoutGroup 或普通竖向列表：把目标项上方的高度、间距、边距累加
                var vlg = content.GetComponent<VerticalLayoutGroup>();
                if (vlg) topOffset += vlg.padding.top;

                for (int i = 0; i < index; i++)
                {
                    var rt = content.GetChild(i) as RectTransform;
                    if (rt == null || !rt.gameObject.activeInHierarchy) continue; // 跳过未激活项
                    topOffset += rt.rect.height;
                    if (vlg) topOffset += vlg.spacing;
                }

                topOffset += extraTopOffset;
            }

            // 转 normalized（1 顶部 / 0 底部）
            float normalizedY = scrollable <= 0f ? 1f : 1f - Mathf.Clamp01(topOffset / scrollable);

            if (duration > 0f)
            {
                DOTween.Kill(scrollRect); // 防止叠加旧 tween
                DOTween.To(
                    () => scrollRect.normalizedPosition,
                    v  => scrollRect.normalizedPosition = v,
                    new Vector2(scrollRect.normalizedPosition.x, normalizedY),
                    duration
                ).SetId(scrollRect);
            }
            else
            {
                scrollRect.normalizedPosition = new Vector2(scrollRect.normalizedPosition.x, normalizedY);
            }
        }
    }


}