using System;
using System.Collections.Generic;
using Config;
using Game.UserTool;
using UIFramework;

namespace Game.Soldier
{
    /// <summary>
    /// 底栏容器面板 ViewModel，聚合合成/分解子面板。
    /// </summary>
    public class BottomBarViewModel : ViewModel, IUserToolConfigurable
    {
        /// <summary>
        /// 底栏 Tab 类型：合成 / 分解
        /// </summary>
        public enum TabType
        {
            Conbine = 0,
            Resolve = 1,
        }

        #region 命令
        public Command OnCloseCommand { get; private set; }
        public Command<TabType> OnTabSelectedCommand { get; private set; }
        #endregion

        #region 事件
        public Action OnClosePanel;
        public Action OnOpenConbinePanel;
        public Action OnOpenResolvePanel;
        #endregion

        /// <summary>
        /// 打开时默认显示的 Tab（由调用方在打开前设置）
        /// </summary>
        public TabType InitialTab { get; set; } = TabType.Conbine;

        public override void OnCreate()
        {
            base.OnCreate();

            OnCloseCommand = new Command(OnClose);
            OnTabSelectedCommand = new Command<TabType>(OnTabSelected);
        }

        private void OnClose()
        {
            OnClosePanel?.Invoke();
        }

        private void OnTabSelected(TabType type)
        {
            switch (type)
            {
                case TabType.Conbine:
                    OnOpenConbinePanel?.Invoke();
                    break;
                case TabType.Resolve:
                    OnOpenResolvePanel?.Invoke();
                    break;
            }
        }

        /// <summary>
        /// 顶部资源栏配置：固定显示奥术卡页（ArcCardPage）资源，
        /// 与聚合的合成/分解子面板保持一致。
        /// </summary>
        public UserToolData GetUserToolConfig()
        {
            return GlobalFunc.GetUserResourceConfig(PageType.ArcCardPage);
        }
    }
}
