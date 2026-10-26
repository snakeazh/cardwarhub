using System;
using System.Collections.Generic;

namespace UIBridge
{
    /// <summary>
    /// UIKit 与 UIFramework 的桥接接口（避免程序集互相引用）。
    /// UIKit 侧只调用该接口；实现由其他程序集注入。
    /// </summary>
    public interface IUIKitAtlasPreloadHook
    {
        void OnBeforeCreatePanel(Type panelType);
        void OnAfterClosePanel(Type panelType);
        void OnAfterCloseAllPanels(IReadOnlyList<Type> panelTypes);
    }

    /// <summary>
    /// 全局桥接点：由启动代码注入 Hook。
    /// </summary>
    public static class UIKitAtlasPreloadBridge
    {
        public static IUIKitAtlasPreloadHook Hook { get; set; }
    }
}

