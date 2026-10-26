using System.Collections;
using System.Collections.Generic;
using FusionContinent;
using FusionContinent.Base;
using QFramework;
using UnityEngine;
using YooAsset;

namespace HotFix
{
    public class GameEntry
    {
        // 这是热更代码的入口，我们将通过反射调用它
        public static void StartGame()
        {
             LogTool.Log("🎉 恭喜！你成功进入了热更逻辑！");
             LogTool.Log($"当前 Unity 版本: {Application.unityVersion}");
             new GameObject().AddComponent<GameStartMono>();
// // ResKit.Init();
//             var audioLoader = new YooAssetAudioLoaderPool();
//             AudioKit.Config.AudioLoaderPool = audioLoader;
//             // 在你的启动代码中注册
//             // ResKit.SetAssetProvider();
//             yield return DataConfigManager.LoadAllConfigs();
//             DataConfigManager.Initialize();
// 			
//             yield return InitInstance();
// 		
//             Global.InitGameConst();
//             var PreloadRoot =new GameObject("PreloadRoot");
//             DontDestroyOnLoad(PreloadRoot);
//             GameSoundPreload soundPreload = PreloadRoot.AddComponent<GameSoundPreload>();
//             soundPreload.OnLoaded = (key, clip) =>
//             {
//                 audioLoader.PreloadAdd(key,clip);
//                 // AudioKit.Config.AudioLoaderPool 
//             };
//             foreach (var soundConfig in DataConfigManager.Tables.TbGameSoundConfig.DataList)
//             {
//                 soundPreload.AddSound(soundConfig.SoundEffects);
//                 // DataConfigManager.Tables.TbGameSoundConfig.DataList
//             }
//
//             soundPreload.StartCoroutine(soundPreload.StartPreload());
//             var StartPreload = PreloadRoot.AddComponent<UIPanelPreload>();
//             // StartPreload.AddPanel("ChestCardsPanel01");ShopPanel01 CardGroupPanel01  MainInterfacePanel SoldierPanel
//             StartPreload.AddPanel("ChestCardsPanel01");
//             StartPreload.AddPanel("ShopPanel01");
//             StartPreload.AddPanel("CardGroupPanel01");
//             StartPreload.AddPanel("MainInterfacePanel");
//             StartPreload.AddPanel("SoldierPanel");
//             StartCoroutine(StartPreload.StartPreload());
//             //ResKit.InitAsync().ToAction().StartGlobal();
//             Application.targetFrameRate = 120; 
//             Screen.sleepTimeout = SleepTimeout.NeverSleep;// 设置屏幕永不休眠
//             //StartCoroutine( UIKit.OpenPanelAsync<LoginPanel>());
//             YooAssets.LoadSceneSync("GameStart");
//             // 在这里你可以实例化 UI、加载场景等
//             // GameObject.Instantiate(Resources.Load("Player"));
        }
    }
}