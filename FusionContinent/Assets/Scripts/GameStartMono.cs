using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FusionContinent;
using FusionContinent.Base;
using Game.Challenge;
using Game.Login;
using Game.Loading;
using Game.Equip;
using Game.GameMain;
using Game.Shop;
using Game.Soldier;
using Game.Team;
using Game.UI;
using QFramework;
using TMPro;
using UIBridge;
using UIFramework;
using UnityEngine;
using YooAsset;

public class GameStartMono : MonoBehaviour
{
    		
    static List<string> managerStrings = new List<string>()
    {
        "CombatTipController",
        "LocalizationManager",
        "ObjectPoolManager",
        "ServerTimeManager",
        "UIRoot",
        "PlayerOnclickEffect",
        "AppLifeCycleManager",
        "TimerSystem",
        // "EffectManager",
        
        // "CommonItemPoolManager",
        "BattleItemPoolManager",
        "UIPanelItemPoolManager",

    };
    static IEnumerator InitInstance()
    {
        List<AssetHandle> assetHandles = new List<AssetHandle>();
        foreach (var managerName in managerStrings)
        {
            var assetHandle = YooAssets.LoadAssetAsync<GameObject>("Instance_"+managerName);
            assetHandles.Add(assetHandle);
            assetHandle.Completed += handle =>
            {
                var obj = handle.GetAssetObject<GameObject>().Instantiate();
                obj.DontDestroyOnLoad();
                obj.name = managerName;
            };
        }

        yield return new WaitUntil(() =>
        {
            bool allDone = true;
            foreach (var xhandle in assetHandles)
            {
                if (!xhandle.IsDone)
                {
                    allDone = false;
                    break;
                }
            }

            return allDone;
        });
    }
    private IEnumerator Start()
    {

        // 在你的启动代码中注册
        // ResKit.SetAssetProvider();
        yield return DataConfigManager.LoadAllConfigs();
        DataConfigManager.Initialize();
		
        yield return InitInstance();
    

        var spriteAssetHandle = YooAssets.LoadAssetAsync<TMP_SpriteAsset>("Fonts_" + "DamageFont");
        yield return spriteAssetHandle;
        Global.DamageAsset = spriteAssetHandle.GetAssetObject<TMP_SpriteAsset>();
        // 步骤1: 初始化UIFramework
        LogTool.Log("[1/4] 初始化 UIFramework...");
	    UIFramework.UIManager.Initialize();

        // 注入 UIKit -> UIFramework 的图集预加载桥接（避免程序集互相引用）
        UIKitAtlasPreloadBridge.Hook = new UIFramework.UIKitAtlasPreloadHook();

        LogTool.Log("[GameStartMono] 开始执行 PreloadCommonAtlasesAsync");
        yield return AtlasManager.Instance.PreloadCommonAtlasesAsync().ToCoroutine();
        LogTool.Log("[GameStartMono] PreloadCommonAtlasesAsync 执行结束");

        // 步骤2: 初始化Service系统
        LogTool.Log("[2/4] 初始化 GameServiceManager...");
        yield return UIFramework.Services.GameServiceManager.InitializeAsync().ToCoroutine();
            
        // 步骤3: 预加载关键Service（并行加载）
        LogTool.Log("[3/4] 加载核心Service...");
        yield return  LoadCoreServices().ToCoroutine();
        // 异步预载 InitGameConst 需要的资源（WebGL 不支持同步加载，统一走异步预载）
        yield return PreloadGameConstAssets();
        Global.InitGameConst();
        // GoldCoinManager 在 GamePlay 场景 Awake 时同步建池（早于战斗加载管线），这里预载进 ResLoader 缓存供 WebGL 命中
        yield return Global.ResLoader.LoadAsync<GameObject>("GoldCoin", null);
        // 技能升级面板伤害数字材质（SkillUpGradePanelViewModel 经 GlobalFunc.LoadMaterial 同步取用），预载供 WebGL 命中
        yield return Global.ResLoader.LoadAsync<Material>("Fonts_ChironGoRoundTC-700BSDFStaticGreen", null);
        yield return Global.ResLoader.LoadAsync<Material>("Fonts_ChironGoRoundTC-700BSDFStaticIndigo", null);
        // 红点预制体由 RedDotDisplay.Apply 同步取用，同样预载供 WebGL 命中
        yield return Global.ResLoader.LoadAsync<GameObject>(Game.RedDot.View.RedDotDisplay.RedPointPrefab, null);
        yield return Global.ResLoader.LoadAsync<GameObject>(Game.RedDot.View.RedDotDisplay.ArrowPointPrefab, null);
        // 全屏面板实例级预载已挪到登录成功后（LoginPanelNew.OnLoginSuccess）后台进行，
        // 启动到登录界面之间不再预载与登录无关的资源
        //ResKit.InitAsync().ToAction().StartGlobal();
        Application.targetFrameRate = 120; 
        Screen.sleepTimeout = SleepTimeout.NeverSleep;// 设置屏幕永不休眠

        yield return PlayerEffect.Instance.InitializeAsync();
        yield return PreloadPoolPrefabsAsync().ToCoroutine();
        //StartCoroutine( UIKit.OpenPanelAsync<LoginPanel>());
#if UNITY_WEBGL
        // WebGL 不支持同步加载场景，bundle 异步加载完成后再激活
        var sceneHandle = YooAssets.LoadSceneAsync("GameStart");
        yield return sceneHandle;
        if (sceneHandle.Status != EOperationStatus.Succeed)
            LogTool.LogError($"[GameStartMono] 加载 GameStart 场景失败: {sceneHandle.LastError}");
#else
        YooAssets.LoadSceneSync("GameStart");
#endif
    }
    

    private IEnumerator PreloadGameConstAssets()
    {
        var poolRootHandle = YooAssets.LoadAssetAsync<GameObject>("UIPoolRoot");
        var grayHandle = YooAssets.LoadAssetAsync<Material>("UIGray");
        yield return poolRootHandle;
        yield return grayHandle;
        Global.UIPoolRootPrefab = poolRootHandle.GetAssetObject<GameObject>();
        GrayTools.GrayMaterial = grayHandle.GetAssetObject<Material>();
    }

    private async UniTask PreloadPoolPrefabsAsync()
    {
        var tasks = new List<UniTask>();

        if (UIPanelItemPoolManager.Instance != null)
        {
            tasks.Add(UIPanelItemPoolManager.Instance.PreloadPredefinedPrefabsAsync());
        }

        if (CommonItemPoolManager.Instance != null)
        {
            tasks.Add(CommonItemPoolManager.Instance.PreloadPredefinedPrefabsAsync());
        }

        if (BattleItemPoolManager.Instance != null)
        {
            tasks.Add(BattleItemPoolManager.Instance.PreloadPredefinedPrefabsAsync());
        }

        if (tasks.Count > 0)
        {
            await UniTask.WhenAll(tasks);
        }
    }

    async UniTask LoadCoreServices()
    {
        LogTool.Log("[LoadCoreServices] 注册核心Service...");
        
        // 1. 注册登录相关Service
        UIFramework.Services.GameServiceManager.Register(new LoginService());
        UIFramework.Services.GameServiceManager.Register(new LocalizationService());
        // UIFramework.Services.GameServiceManager.Register(new GameMainService());

        
        // 2. 注册加载相关Service
        UIFramework.Services.GameServiceManager.Register(new LoadingService());
        
        UIFramework.Services.GameServiceManager.Register(new RewardTipsService());
        UIFramework.Services.GameServiceManager.Register(new CombatPowerService());
        UIFramework.Services.GameServiceManager.Register(new EquipService());
        UIFramework.Services.GameServiceManager.Register(new Game.Equip.FusionGemService());
        UIFramework.Services.GameServiceManager.Register(new Game.HeroSeat.SeatService());
        // 3. 注册UI相关Service
        UIFramework.Services.GameServiceManager.Register(new ToastService());
        UIFramework.Services.GameServiceManager.Register(new SoldierService());
        UIFramework.Services.GameServiceManager.Register(new Game.Soldier.DamageBonusService());
        UIFramework.Services.GameServiceManager.Register(new TeamService());
        // 4. 注册商店相关Service
        UIFramework.Services.GameServiceManager.Register(new ShopService());
        UIFramework.Services.GameServiceManager.Register(new BigWorldService());
        UIFramework.Services.GameServiceManager.Register(new Game.Station.StationService());
       UIFramework.Services.GameServiceManager.Register(new AtlasService());
       UIFramework.Services.GameServiceManager.Register(new RankService());
        UIFramework.Services.GameServiceManager.Register(new Game.Task.TaskService());
        UIFramework.Services.GameServiceManager.Register(new AchievementService());
        UIFramework.Services.GameServiceManager.Register(new Game.Realm.RealmService());
        UIFramework.Services.GameServiceManager.Register(new Game.Ticket.TicketService());
        // 5. 技能升级已移除（技能等级跟随英灵等级），原 SkillService 随之删除
        // 6. 注册流派选卡Service
        UIFramework.Services.GameServiceManager.Register(new Game.Services.SchoolChooseCardService());
        // 7. 注册轮语者Service
        UIFramework.Services.GameServiceManager.Register(new Game.Player.SpeakerService());
        UIFramework.Services.GameServiceManager.Register(new Game.Player.WeaponService());
        UIFramework.Services.GameServiceManager.Register(new Game.User.UserService());
        UIFramework.Services.GameServiceManager.Register(new Game.RedDot.RedDotService());
        // 7. 注册其他核心Service（后续添加）
        // UIFramework.Services.GameServiceManager.Register(new PlayerService());
        // UIFramework.Services.GameServiceManager.Register(new InventoryService());
        // UIFramework.Services.GameServiceManager.Register(new TaskService());
        
        LogTool.Log("[LoadCoreServices] 开始并行初始化所有Service...");
        
        // 6. 等待所有Service初始化完成
        await UIFramework.Services.GameServiceManager.InitializeAllAsync();
        
        LogTool.Log("[LoadCoreServices] 所有核心Service初始化完成！");
        
        // 打印所有Service状态
        var serviceInfos = UIFramework.Services.GameServiceManager.GetAllServiceInfo();
        foreach (var info in serviceInfos)
        {
            LogTool.Log($"- {info.Name}: {info.IsInitialized}");
        }
    }
    
    async UniTask WaitForService(UIFramework.Services.IGameService service)
    {
        while (!service.IsInitialized)
        {
            await UniTask.Delay(100);
        }
    }
    
}
