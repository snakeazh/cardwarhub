/****************************************************************************
 * Copyright (c) 2017 xiaojun
 * Copyright (c) 2017 imagicbell
 * Copyright (c) 2017 ~ 2023 liangxiegame UNDER THE MIT LICENSE
 * 
 * https://qframework.cn
 * https://github.com/liangxiegame/QFramework
 * https://gitee.com/liangxiegame/QFramework
 * 
 ****************************************************************************/

using System;
using System.Linq;
using UIBridge;
using UnityEngine;

namespace QFramework
{
    [MonoSingletonPath("UIRoot/Manager")]
    public partial class UIManager : QMgrBehaviour, ISingleton
    {
        void ISingleton.OnSingletonInit()
        {
        }

        private static UIManager mInstance;

        public static UIManager Instance
        {
            get
            {
                if (!mInstance)
                {
                    var uiRoot = UIRoot.Instance;
                    Debug.Log("currentUIRoot:" + uiRoot);
                    mInstance = MonoSingletonProperty<UIManager>.Instance;
                }

                return mInstance;
            }
        }


        public void OpenUIAsync(PanelSearchKeys panelSearchKeys,Action<IPanel> onLoad)
        {
            if (panelSearchKeys.OpenType == PanelOpenType.Single)
            {
                var retPanel = UIKit.Table.GetPanelsByPanelSearchKeys(panelSearchKeys).FirstOrDefault();

                if (retPanel == null)
                {
                    CreateUIAsync(panelSearchKeys, (panel) =>
                    {
                        retPanel = panel;
                        retPanel.Open(panelSearchKeys.UIData);
                        retPanel.Show();
                        onLoad?.Invoke(retPanel);
                    });
                }
                else
                {
                    if (retPanel.Info != null && retPanel.Info.Level != panelSearchKeys.Level)
                    {
                        UIKit.Root.SetLevelOfPanel(panelSearchKeys.Level,retPanel);
                    }
                    retPanel.Open(panelSearchKeys.UIData);
                    retPanel.Show();
                    onLoad?.Invoke(retPanel);
                }
            }
            else
            {
                CreateUIAsync(panelSearchKeys, (panel) =>
                {
                    panel.Open(panelSearchKeys.UIData);
                    panel.Show();
                    onLoad?.Invoke(panel);
                });
            }
        }

        public IPanel OpenUI(PanelSearchKeys panelSearchKeys)
        {
            if (panelSearchKeys.OpenType == PanelOpenType.Single)
            {
                var retPanel = UIKit.Table.GetPanelsByPanelSearchKeys(panelSearchKeys).FirstOrDefault();

                if (retPanel == null)
                {
                    retPanel = CreateUI(panelSearchKeys);
                }

                if (retPanel.Info != null && retPanel.Info.Level != panelSearchKeys.Level)
                {
                    UIKit.Root.SetLevelOfPanel(panelSearchKeys.Level,retPanel);
                }
                
                retPanel.Open(panelSearchKeys.UIData);
                retPanel.Show();
                return retPanel;
            }
            else
            {
                var retPanel = CreateUI(panelSearchKeys);
                retPanel.Open(panelSearchKeys.UIData);
                retPanel.Show();
                return retPanel;
            }
        }

        /// <summary>
        /// 显示UIBehaiviour
        /// </summary>
        /// <param name="uiBehaviourName"></param>
        public void ShowUI(PanelSearchKeys panelSearchKeys)
        {
            var retPanel = UIKit.Table.GetPanelsByPanelSearchKeys(panelSearchKeys).FirstOrDefault();

            if (retPanel != null)
            {
                retPanel.Show();
            }
        }

        /// <summary>
        /// 隐藏UI
        /// </summary>
        /// <param name="uiBehaviourName"></param>
        public void HideUI(PanelSearchKeys panelSearchKeys)
        {
            var retPanel = UIKit.Table.GetPanelsByPanelSearchKeys(panelSearchKeys).FirstOrDefault();

            if (retPanel != null)
            {
                retPanel.Hide();
            }
        }

        /// <summary>
        /// 删除所有UI层
        /// </summary>
        public void CloseAllUI(bool delLoading = true)
        {
            Type[] closedTypesSnapshot = null;
// #if GAME_UIATLAS
            if (UIKitAtlasPreloadBridge.Hook != null)
            {
                closedTypesSnapshot = UIKit.Table.Select(p => p != null ? p.GetType() : null).Where(t => t != null).ToArray();
            }
// #endif

            foreach (var layer in UIKit.Table)
            {
                if (!delLoading)
                {
                    if (layer.Info.Level == UILevel.Loading)
                    {
                        continue;
                    }
                }
                layer.Close();
                layer.Info.Recycle2Cache();
                layer.Info = null;
            }

            UIKit.Table.Clear();

// #if GAME_UIATLAS
            if (closedTypesSnapshot != null && closedTypesSnapshot.Length > 0)
            {
                UIKitAtlasPreloadBridge.Hook.OnAfterCloseAllPanels(closedTypesSnapshot);
            }
// #endif
        }

        /// <summary>
        /// 隐藏所有 UI
        /// </summary>
        public void HideAllUI(params Type[] cullingPanelTypes)
        {
            foreach (var panel in UIKit.Table)
            {
                bool haveCullingPanel = false;
                foreach (var type in cullingPanelTypes)
                {
                    if (panel.GetType()  == type)
                    {
                        haveCullingPanel = true;
                        break;
                    }
                }

                if (!haveCullingPanel)
                {
                    panel.Hide();
                    
                }
            }
        }

        /// <summary>
        /// 关闭并卸载UI
        /// </summary>
        /// <param name="behaviourName"></param>
        public void CloseUI(PanelSearchKeys panelSearchKeys)
        {
            var panel = UIKit.Table.GetPanelsByPanelSearchKeys(panelSearchKeys).LastOrDefault();

            if (panel as UIPanel)
            {
                panel.Close();
                UIKit.Table.Remove(panel);
                panel.Info.Recycle2Cache();
                panel.Info = null;

// #if GAME_UIATLAS
                UIKitAtlasPreloadBridge.Hook?.OnAfterClosePanel(panel.GetType());
// #endif
            }
        }

        public void RemoveUI(PanelSearchKeys panelSearchKeys)
        {
            var panel = UIKit.Table.GetPanelsByPanelSearchKeys(panelSearchKeys).FirstOrDefault();

            if (panel != null)
            {
                UIKit.Table.Remove(panel);
            }
        }

        /// <summary>
        /// 获取UIBehaviour
        /// </summary>
        /// <param name="uiBehaviourName"></param>
        /// <returns></returns>
        public UIPanel GetUI(PanelSearchKeys panelSearchKeys)
        {
            return UIKit.Table.GetPanelsByPanelSearchKeys(panelSearchKeys).FirstOrDefault() as UIPanel;
        }

        public override int ManagerId
        {
            get { return QMgrID.UI; }
        }

        public IPanel CreateUI(PanelSearchKeys panelSearchKeys)
        {
// #if GAME_UIATLAS
            if (panelSearchKeys?.PanelType != null)
            {
                UIKitAtlasPreloadBridge.Hook?.OnBeforeCreatePanel(panelSearchKeys.PanelType);
            }
// #endif
            var panel = UIKit.Config.LoadPanel(panelSearchKeys);

            UIKit.Root.SetLevelOfPanel(panelSearchKeys.Level, panel);

            UIKit.Config.SetDefaultSizeOfPanel(panel);

            panel.Transform.gameObject.name = panelSearchKeys.GameObjName ?? panelSearchKeys.PanelType.Name;

            panel.Info = PanelInfo.Allocate(panelSearchKeys.GameObjName, panelSearchKeys.Level, panelSearchKeys.UIData,
                panelSearchKeys.PanelType, panelSearchKeys.AssetBundleName);

            UIKit.Table.Add(panel);

            panel.Init(panelSearchKeys.UIData);

            return panel;
        }

        public void CreateUIAsync(PanelSearchKeys panelSearchKeys, Action<IPanel> onPanelCreate)
        {
// #if GAME_UIATLAS
            if (panelSearchKeys?.PanelType != null)
            {
                UIKitAtlasPreloadBridge.Hook?.OnBeforeCreatePanel(panelSearchKeys.PanelType);
            }
// #endif
            UIKit.Config.LoadPanelAsync(panelSearchKeys, panel =>
            {
                UIKit.Root.SetLevelOfPanel(panelSearchKeys.Level, panel);

                UIKit.Config.SetDefaultSizeOfPanel(panel);

                panel.Transform.gameObject.name = panelSearchKeys.GameObjName ?? panelSearchKeys.PanelType.Name;

                panel.Info = PanelInfo.Allocate(panelSearchKeys.GameObjName, panelSearchKeys.Level,
                    panelSearchKeys.UIData,
                    panelSearchKeys.PanelType, panelSearchKeys.AssetBundleName);

                UIKit.Table.Add(panel);

                panel.Init(panelSearchKeys.UIData);

                onPanelCreate(panel);
            });
        }

        private void OnApplicationQuit()
        {
            UIKit.Table.Clear();
        }
    }
}