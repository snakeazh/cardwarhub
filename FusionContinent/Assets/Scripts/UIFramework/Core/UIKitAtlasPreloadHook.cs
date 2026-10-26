using System;
using System.Collections.Generic;
using UIBridge;

namespace UIFramework
{
    /// <summary>
    /// UIFramework 对 UIKit 的图集预加载/释放实现（通过接口注入给 UIKit 调用）。
    /// </summary>
    public sealed class UIKitAtlasPreloadHook : IUIKitAtlasPreloadHook
    {
        public void OnBeforeCreatePanel(Type panelType)
        {
            if (panelType == null)
            {
                return;
            }

            if (!AtlasManager.EnableAutoPreload)
            {
                return;
            }

            AtlasManager.Instance.PreloadForPanelSync(panelType);
        }

        public void OnAfterClosePanel(Type panelType)
        {
            if (panelType == null)
            {
                return;
            }

            AtlasManager.Instance.ReleaseForPanel(panelType);
        }

        public void OnAfterCloseAllPanels(IReadOnlyList<Type> panelTypes)
        {
            if (panelTypes == null || panelTypes.Count == 0)
            {
                return;
            }

            for (int i = 0; i < panelTypes.Count; i++)
            {
                var type = panelTypes[i];
                if (type != null)
                {
                    AtlasManager.Instance.ReleaseForPanel(type);
                }
            }
        }
    }
}

