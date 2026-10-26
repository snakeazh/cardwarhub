using System.Collections.Generic;
using Config;

namespace Game.UserTool
{
    /// <summary>
    /// UserTool 数据配置
    /// 每个全屏界面通过此配置定义顶部工具栏显示内容
    /// </summary>
    public class UserToolData
    {
        public PositionType Direction { get; set; } =  PositionType.Left;
        /// <summary>
        /// 是否显示头像
        /// </summary>
        public bool ShowAvatar { get; set; } = true;
        
        /// <summary>
        /// 道具ID列表（用于显示道具数量）
        /// </summary>
        public List<int> ItemIds { get; set; } = new List<int>();
        
        /// <summary>
        /// 创建默认配置（显示头像）
        /// </summary>
        public static UserToolData Default => new UserToolData
        {
            ShowAvatar = true,
            ItemIds = new List<int>()
        };
        
        /// <summary>
        /// 创建只显示头像的配置
        /// </summary>
        public static UserToolData OnlyAvatar => new UserToolData
        {
            ShowAvatar = true,
            ItemIds = new List<int>()
        };
        
        /// <summary>
        /// 创建只显示道具的配置
        /// </summary>
        public static UserToolData OnlyItems(List<int> itemIds) => new UserToolData
        {
            ShowAvatar = false,
            ItemIds = itemIds ?? new List<int>()
        };
        
        /// <summary>
        /// 创建空配置（不显示任何内容）
        /// </summary>
        public static UserToolData Empty => new UserToolData
        {
            ShowAvatar = false,
            ItemIds = new List<int>()
        };
        
        /// <summary>
        /// 添加道具ID
        /// </summary>
        public UserToolData AddItem(int itemId)
        {
            if (!ItemIds.Contains(itemId))
            {
                ItemIds.Add(itemId);
            }
            return this;
        }
        
        /// <summary>
        /// 添加多个道具ID
        /// </summary>
        public UserToolData AddItems(params int[] itemIds)
        {
            foreach (var itemId in itemIds)
            {
                if (!ItemIds.Contains(itemId))
                {
                    ItemIds.Add(itemId);
                }
            }
            return this;
        }
    }
}
