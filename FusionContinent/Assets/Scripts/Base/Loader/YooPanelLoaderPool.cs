using YooAsset;

namespace FusionContinent.Base
{
    using System;
    using UnityEngine;
    using QFramework;

    public class YooPanelLoaderPool : AbstractPanelLoaderPool
    {
        class YooPanelLoader : IPanelLoader
        {
            private GameObject mPanelPrefab;

            public GameObject LoadPanelPrefab(PanelSearchKeys panelSearchKeys)
            {
                // var cachePanel= UIPanelPreload.Preload.GetPanel(panelSearchKeys.GameObjName);
                // if (cachePanel != null)
                // {
                //     return cachePanel;
                // }
#if UNITY_WEBGL
                // WebGL 不支持同步加载，只命中 Global.ResLoader 预载缓存；
                // 未命中由 LoadSync 报出面板名，需提前预载
                return Global.ResLoader.LoadSync<GameObject>(panelSearchKeys.GameObjName);
#else
                var handle = YooAssets.LoadAssetSync(panelSearchKeys.GameObjName, typeof(GameObject));
                mPanelPrefab = handle.AssetObject as GameObject;
                handle.Release();
                return mPanelPrefab;
#endif
            }

            public void LoadPanelPrefabAsync(PanelSearchKeys panelSearchKeys, Action<GameObject> onPanelPrefabLoad)
            {
                
                // var cachePanel= UIPanelPreload.Preload.GetPanel(panelSearchKeys.GameObjName);
                // if (cachePanel != null)
                // {
                //     onPanelPrefabLoad?.Invoke(cachePanel);
                //     return;
                // }
                var handle = YooAssets.LoadAssetAsync(panelSearchKeys.GameObjName, typeof(GameObject));

                void XXXLoaderComplete(AssetHandle assetHandle)
                {
                    onPanelPrefabLoad?.Invoke(assetHandle.AssetObject as GameObject);
                    handle.Completed -= XXXLoaderComplete;
                    handle.Release();
                }
                handle.Completed += XXXLoaderComplete;
            }
            

            public void Unload()
            {
                mPanelPrefab = null;
            }
        }

        protected override IPanelLoader CreatePanelLoader()
        {
            return new YooPanelLoader();
        }
    }
}