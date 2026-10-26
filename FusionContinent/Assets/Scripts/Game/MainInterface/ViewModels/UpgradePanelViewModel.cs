using System.Collections.Generic;
using Config;
using FusionContinent;
using UIFramework;

namespace Game.MainInterface
{
    /// <summary>
    /// 升级解锁弹窗 ViewModel：携带本等级解锁的功能列表，由主界面每日弹出逻辑创建。
    /// </summary>
    public class UpgradePanelViewModel : ViewModel
    {
        public class UnlockItem
        {
            public ModuleFunctionType ModuleType;
            public string Icon;
            public string Name;
        }

        public int CurrentLevel { get; private set; }
        public List<UnlockItem> UnlockItems { get; } = new List<UnlockItem>();

        /// <summary>面板关闭后回调（用于继续每日弹出流程）。</summary>
        public System.Action OnClosed;

        /// <summary>
        /// 计算 (lastLevel, currentLevel] 区间内解锁且需要飞行展示的功能列表。
        /// </summary>
        public void Setup(int lastLevel, int currentLevel)
        {
            CurrentLevel = currentLevel;
            var moduleFunctionConfigs = DataConfigManager.Tables.TbModuleFunctionConfig.DataList;
            for (int lv = lastLevel + 1; lv <= currentLevel; lv++)
            {
                foreach (var config in moduleFunctionConfigs)
                {
                    if (config.UnlockUserLv == lv && config.FlyIcon)
                    {
                        UnlockItems.Add(new UnlockItem
                        {
                            ModuleType = config.ModuleType,
                            Icon = config.Icon,
                            Name = config.Name,
                        });
                    }
                }
            }
        }
    }
}
