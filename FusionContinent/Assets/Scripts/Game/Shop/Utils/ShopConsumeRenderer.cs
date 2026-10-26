using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Config;
using FusionContinent;
using FusionContinent.Model;

namespace Game.Shop
{
    /// <summary>
    /// 商店消耗项数据
    /// </summary>
    public class ShopConsumeItem
    {
        /// <summary>
        /// 道具ID
        /// </summary>
        public int ItemId { get; set; }
        
        /// <summary>
        /// 单次消耗数量
        /// </summary>
        public int SingleCount { get; set; }
        
        /// <summary>
        /// 多抽次数（用于计算多抽总消耗）
        /// </summary>
        public int MultiCount { get; set; }
        
        /// <summary>
        /// 当前拥有的数量
        /// </summary>
        public long CurrentOwnCount => Global.GetItemNumById(ItemId);
        
        /// <summary>
        /// 是否足够
        /// </summary>
        public bool IsEnough(int times = 1)
        {
            return CurrentOwnCount >= GetTotalCount(times);
        }
        
        /// <summary>
        /// 获取总消耗数量
        /// </summary>
        public int GetTotalCount(int times)
        {
            return times == 1 ? SingleCount : MultiCount;
        }
    }

    /// <summary>
    /// 商店消耗渲染器 - 通用工具类
    /// 用于渲染商店按钮的消耗显示（图标+数量）
    /// </summary>
    public static class ShopConsumeRenderer
    {
        #region 颜色配置
        
        /// <summary>
        /// 足够时的颜色（白色）
        /// </summary>
        public const string COLOR_ENOUGH = "#FFFFFF";
        
        /// <summary>
        /// 不足时的颜色（红色）
        /// </summary>
        public const string COLOR_NOT_ENOUGH = "#FF0000";
        
        /// <summary>
        /// 免费时的颜色（绿色）
        /// </summary>
        public const string COLOR_FREE = "#00FF00";
        
        #endregion

        #region 核心渲染方法

        /// <summary>
        /// 渲染消耗按钮
        /// </summary>
        /// <param name="button">按钮Transform</param>
        /// <param name="primaryItem">默认消耗道具（如钻石）</param>
        /// <param name="priorityItem">优先消耗道具（如卡包券）</param>
        /// <param name="times">抽卡次数（1或5）</param>
        /// <param name="isFree">是否免费</param>
        /// <param name="iconPath">图标节点路径（默认："Layout/IconNode/DrawIcon"）</param>
        /// <param name="numPath">数量文本节点路径（默认："Layout/DrawNum"）</param>
        public static void Render(
            Transform button,
            RewardItem primaryItem,
            RewardItem priorityItem,
            int times = 1,
            bool isFree = false,
            string iconPath = "Layout/IconNode/DrawIcon",
            string numPath = "Layout/DrawNum")
        {
            if (button == null) return;

            var iconImg = button.Find(iconPath)?.GetComponent<Image>();
            var numText = button.Find(numPath)?.GetComponent<TextMeshProUGUI>();
            var layoutGroup = numText?.transform.parent?.GetComponent<HorizontalLayoutGroup>();

            RenderInternal(iconImg, numText, layoutGroup, primaryItem, priorityItem, times, isFree);
        }

        /// <summary>
        /// 渲染消耗按钮（重载，直接传入组件）
        /// </summary>
        public static void Render(
            Image iconImg,
            TextMeshProUGUI numText,
            HorizontalLayoutGroup layoutGroup,
            RewardItem primaryItem,
            RewardItem priorityItem,
            int times = 1,
            bool isFree = false)
        {
            RenderInternal(iconImg, numText, layoutGroup, primaryItem, priorityItem, times, isFree);
        }

        /// <summary>
        /// 渲染消耗按钮（使用 ShopConsumeItem）
        /// </summary>
        public static void Render(
            Transform button,
            ShopConsumeItem primaryItem,
            ShopConsumeItem priorityItem,
            int times = 1,
            bool isFree = false)
        {
            if (button == null) return;

            var iconImg = button.Find("Layout/IconNode/DrawIcon")?.GetComponent<Image>();
            var numText = button.Find("Layout/DrawNum")?.GetComponent<TextMeshProUGUI>();
            var layoutGroup = numText?.transform.parent?.GetComponent<HorizontalLayoutGroup>();

            // 转换为 RewardItem
            var primary = new RewardItem(primaryItem.ItemId, primaryItem.GetTotalCount(times));
            var priority = priorityItem != null 
                ? new RewardItem(priorityItem.ItemId, priorityItem.GetTotalCount(times)) 
                : null;

            RenderInternal(iconImg, numText, layoutGroup, primary, priority, times, isFree);
        }

        #endregion

        #region 内部实现

        private static void RenderInternal(
            Image iconImg,
            TextMeshProUGUI numText,
            HorizontalLayoutGroup layoutGroup,
            RewardItem primaryItem,
            RewardItem priorityItem,
            int times,
            bool isFree)
        {
            if (iconImg == null || numText == null) return;

            // 确定实际使用的消耗道具
            RewardItem useItem = null;
            bool hasEnough = false;

            if (isFree)
            {
                // 免费
                hasEnough = true;
            }
            else
            {
                // 检查优先道具
                if (priorityItem != null && priorityItem.Id > 0)
                {
                    long priorityOwn = Global.GetItemNumById(priorityItem.Id);
                    if (priorityOwn >= priorityItem.Num * times)
                    {
                        useItem = priorityItem;
                        hasEnough = true;
                    }
                }

                // 优先道具不足，使用默认道具
                if (!hasEnough && primaryItem != null && primaryItem.Id > 0)
                {
                    long primaryOwn = Global.GetItemNumById(primaryItem.Id);
                    if (primaryOwn >= primaryItem.Num *times)
                    {
                        useItem = primaryItem;
                        hasEnough = true;
                    }
                }
            }

            // 渲染显示
            if (isFree)
            {
                // 免费显示
                RenderFree(numText, layoutGroup);
                iconImg.transform.parent.gameObject.SetActive(false);
            }
            else if (useItem != null)
            {
                // 正常显示消耗
                RenderConsumeInternal(iconImg, numText, layoutGroup, useItem, hasEnough, times);
                iconImg.transform.parent.gameObject.SetActive(true);
            }
            else
            {
                // 道具不足，显示默认道具（红色）
                if (primaryItem != null)
                {
                    RenderConsumeInternal(iconImg, numText, layoutGroup, primaryItem, false, times);
                    iconImg.transform.parent.gameObject.SetActive(true);
                }
            }
        }

        private static void RenderFree(TextMeshProUGUI numText, HorizontalLayoutGroup layoutGroup)
        {
            numText.text = $"<color={COLOR_FREE}>免费</color>";
            numText.rectTransform.sizeDelta = new Vector2(numText.preferredWidth, numText.preferredHeight);
            
            if (layoutGroup != null)
            {
                layoutGroup.SetLayoutHorizontal();
            }
        }

        /// <summary>
        /// 渲染消耗图标与数量（不足时标红）
        /// </summary>
        public static void RenderConsume(
            Image iconImg,
            TextMeshProUGUI numText,
            HorizontalLayoutGroup layoutGroup,
            RewardItem item,
            bool hasEnough,
            int times = 1)
        {
            RenderConsumeInternal(iconImg, numText, layoutGroup, item, hasEnough, times);
        }

        private static void RenderConsumeInternal(
            Image iconImg,
            TextMeshProUGUI numText,
            HorizontalLayoutGroup layoutGroup,
            RewardItem item,
            bool hasEnough,
            int times)
        {
            if (iconImg == null || numText == null || item == null) return;

            // 设置颜色
            string colorHex = hasEnough ? COLOR_ENOUGH : COLOR_NOT_ENOUGH;
            long totalNum = item.Num * times;
            numText.text = $"<color={colorHex}>{totalNum}</color>";
            numText.rectTransform.sizeDelta = new Vector2(numText.preferredWidth, numText.preferredHeight);

            // 设置图标
            var itemCfg = DataConfigManager.Tables.TbItemConfig.Get(item.Id);
            if (itemCfg != null && Global.ItemIconSpriteAtlas != null)
            {
                iconImg.sprite = Global.ItemIconSpriteAtlas.GetSprite(itemCfg.Icon);
            }

            // 刷新布局
            if (layoutGroup != null)
            {
                layoutGroup.SetLayoutHorizontal();
            }
        }

        #endregion

        #region 工具方法

        /// <summary>
        /// 检查是否有足够道具
        /// </summary>
        public static bool CheckEnough(
            RewardItem primaryItem,
            RewardItem priorityItem,
            int times,
            out RewardItem useItem)
        {
            useItem = null;

            // 检查优先道具
            if (priorityItem != null && priorityItem.Id > 0)
            {
                long priorityOwn = Global.GetItemNumById(priorityItem.Id);
                long needCount = priorityItem.Num * times;
                if (priorityOwn >= needCount)
                {
                    useItem = new RewardItem(priorityItem.Id, needCount);
                    return true;
                }
            }

            // 检查默认道具
            if (primaryItem != null && primaryItem.Id > 0)
            {
                long primaryOwn = Global.GetItemNumById(primaryItem.Id);
                long needCount = primaryItem.Num * times;
                if (primaryOwn >= needCount)
                {
                    useItem = new RewardItem(primaryItem.Id, needCount);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 获取消耗道具字符串（用于提示）
        /// </summary>
        public static string GetConsumeString(RewardItem item,int rate =1)
        {
            if (item == null) return "";
            
            var itemCfg = DataConfigManager.Tables.TbItemConfig.Get(item.Id);
            if (itemCfg == null) return "";
            
            return $"{item.Num * rate} 个 {itemCfg.Name}";
        }

        /// <summary>
        /// 获取消耗道具字符串（多个道具）
        /// </summary>
        public static string GetConsumeString(List<RewardItem> items)
        {
            if (items == null || items.Count == 0) return "";
            
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < items.Count; i++)
            {
                if (i > 0) sb.Append("、");
                sb.Append(GetConsumeString(items[i]));
            }
            return sb.ToString();
        }

        #endregion
    }

    /// <summary>
    /// 商店消耗渲染器扩展方法
    /// </summary>
    public static class ShopConsumeRendererExtensions
    {
        /// <summary>
        /// 为 Button 扩展 RenderConsume 方法
        /// </summary>
        public static void RenderConsume(
            this Button button,
            RewardItem primaryItem,
            RewardItem priorityItem,
            int times = 1,
            bool isFree = false)
        {
            ShopConsumeRenderer.Render(button.transform, primaryItem, priorityItem, times, isFree);
        }

        /// <summary>
        /// 为 Button 扩展 RenderConsume 方法（使用配置）
        /// </summary>
        public static void RenderConsume(
            this Button button,
            ShopConsumeItem primaryItem,
            ShopConsumeItem priorityItem,
            int times = 1,
            bool isFree = false)
        {
            ShopConsumeRenderer.Render(button.transform, primaryItem, priorityItem, times, isFree);
        }
    }
}
