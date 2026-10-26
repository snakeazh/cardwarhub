using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Helper
{
    public static class UIContentSorter
    {
        public static void Sort(Transform content, System.Func<SorterItemData, object> keySelector, bool ascending = true)
        {
            var items = content
                .GetComponentsInChildren<SorterItemData>(true)
                .Select(item => new
                {
                    item,
                    rt = item.GetComponent<RectTransform>(),
                    key = keySelector(item)
                });

            var ordered = ascending
                ? items.OrderBy(x => x.key)
                : items.OrderByDescending(x => x.key);

            int index = 0;
            foreach (var o in ordered)
            {
                o.rt.SetSiblingIndex(index++);
            }
        }
        
        public static void SortWithTween(
            RectTransform content,
            System.Func<Transform, object> keySelector,
            float duration = 0.3f,
            bool ascending = true)
        {
            // 获取子项
            List<RectTransform> items = new List<RectTransform>();
            foreach (Transform child in content)
                items.Add(child as RectTransform);

            // 记录排序前的坐标
            Dictionary<RectTransform, Vector3> oldPos = items.ToDictionary(i => i, i => i.anchoredPosition3D);

            // 暂时禁用 LayoutGroup（非常关键）
            var layout = content.GetComponent<HorizontalOrVerticalLayoutGroup>();
            var fitter = content.GetComponent<ContentSizeFitter>();

            bool layoutWasEnabled = layout && layout.enabled;
            bool fitterWasEnabled = fitter && fitter.enabled;

            if (layout) layout.enabled = false;
            if (fitter) fitter.enabled = false;

            // 执行排序（改变 siblingIndex）
            var ordered = ascending ?
                items.OrderBy(i => keySelector(i)) :
                items.OrderByDescending(i => keySelector(i));

            int idx = 0;
            foreach (var item in ordered)
                item.SetSiblingIndex(idx++);

            // 强制刷新位置（布局禁用后 siblingIndex 可立即生效）
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);

            // 记录排序后的新坐标
            Dictionary<RectTransform, Vector3> newPos = items.ToDictionary(i => i, i => i.anchoredPosition3D);

            // 播放缓动动画
            foreach (var item in items)
            {
                item.anchoredPosition3D = oldPos[item];   // 先还原到旧位置
                item.DOAnchorPos3D(newPos[item], duration).SetEase(Ease.OutQuad);
            }

            // 动画结束恢复 LayoutGroup
            DOVirtual.DelayedCall(duration, () =>
            {
                if (layout) layout.enabled = layoutWasEnabled;
                if (fitter) fitter.enabled = fitterWasEnabled;
                LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            });
        }
    }
}