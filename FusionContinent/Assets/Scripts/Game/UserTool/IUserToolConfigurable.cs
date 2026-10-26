using System.Collections.Generic;

namespace Game.UserTool
{
    /// <summary>
    /// 支持 UserTool 配置的接口
    /// 全屏界面实现此接口以自定义顶部工具栏显示
    /// </summary>
    public interface IUserToolConfigurable
    {
        /// <summary>
        /// 获取 UserTool 配置
        /// </summary>
        UserToolData GetUserToolConfig();
    }
}
