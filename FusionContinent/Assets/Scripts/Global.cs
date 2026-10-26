using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Config;
using FusionContinent.Base;
using FusionContinent.Extensions;
using FusionContinent.Model;
using FusionContinent.Pool;
using JetBrains.Annotations;
using QFramework;
using TMPro;
using UIFramework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.U2D;
using YooAsset;

namespace FusionContinent
{
    public partial class Global : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod]
        public static void AutoInit()
        {
            ResetProperties();
        }

        public static int UID;
        public static QFramework.BindableProperty<int> Debug = new QFramework.BindableProperty<int>(1); // Debug模式

        // 服务端逻辑单机化：本地服务端会话（登录时读档/建档）与本地 API 分发
        public static LocalServer.LocalServerContext LocalServer;
        public static LocalServer.LocalApi LocalApi = new LocalServer.LocalApi();

        public static MessagePublisher Publisher = new MessagePublisher();
        
        /// <summary>
        /// 士兵ID枚举
        /// </summary>
        public const int SpearmenCardId = 2010001; // 剑圣ID
        public const int ArcherCardId = 2010002; // 游侠ID
        public const int MagicianCardId = 2010003; // 魔法师ID
        public const int AssassinCardId = 2010004; // 刺客ID
        public static QFramework.BindableProperty<int> LastPlayStage = new QFramework.BindableProperty<int>(0); // 关卡Id

        // 游戏全局属性
        public static QFramework.BindableProperty<int> Difficulty = new QFramework.BindableProperty<int>(1); // 难度
        public static QFramework.BindableProperty<int> Date = new QFramework.BindableProperty<int>(0); // 日期
        public static QFramework.BindableProperty<Int64> TotalDamage = new QFramework.BindableProperty<Int64>(0); // 本局总伤害
        public static QFramework.BindableProperty<int> StageId = new QFramework.BindableProperty<int>(3001001); // 关卡Id
        public static QFramework.BindableProperty<int> CityExp = new QFramework.BindableProperty<int>(0);
        public static QFramework.BindableProperty<int> CityLevel = new QFramework.BindableProperty<int>(1);
        public static QFramework.BindableProperty<int> TreasureChestExp = new QFramework.BindableProperty<int>(0);
        public static QFramework.BindableProperty<int> TreasureChestLevel = new QFramework.BindableProperty<int>(1);
        public static QFramework.BindableProperty<int> RotaryTableOpenMaxNum = new QFramework.BindableProperty<int>(0);// 转盘开启最大次数
        public static QFramework.BindableProperty<int> CityPopulation = new QFramework.BindableProperty<int>(0); // 城市人口
        public static QFramework.BindableProperty<int> MaxCityPopulation = new QFramework.BindableProperty<int>(3); // 人口上限
        public static QFramework.BindableProperty<int> BuildUnLock = new QFramework.BindableProperty<int>(0); // 建筑解锁
        public static QFramework.BindableProperty<int> SpeedMultiplier = new QFramework.BindableProperty<int>(1); // 速度倍率
        public static QFramework.BindableProperty<int> BuyCardLevel = new QFramework.BindableProperty<int>(1); // 购买卡的等级
        public static QFramework.BindableProperty<int> GameDifficultyLevel = new QFramework.BindableProperty<int>(1); // 游戏难度
        public static QFramework.BindableProperty<float> SpearmanAtk = new QFramework.BindableProperty<float>(0); // 士兵攻击力
        public static QFramework.BindableProperty<float> SpearmanInterval = new QFramework.BindableProperty<float>(0); // 士兵攻击间隔
        public static QFramework.BindableProperty<float> ArcherAtk = new QFramework.BindableProperty<float>(0); // 弓箭手攻击力
        public static QFramework.BindableProperty<float> ArcherInterval = new QFramework.BindableProperty<float>(0); // 弓箭手攻击间隔
        public static QFramework.BindableProperty<float> MagicianAtk = new QFramework.BindableProperty<float>(0); // 魔法师攻击力
        public static QFramework.BindableProperty<float> MagicianInterval = new QFramework.BindableProperty<float>(0); // 魔法师攻击间隔
        public static QFramework.BindableProperty<float> Barracks = new QFramework.BindableProperty<float>(0); // 兵营产出时间
        public static QFramework.BindableProperty<float> ShooterCamp = new QFramework.BindableProperty<float>(0); // 射手营产出时间
        public static QFramework.BindableProperty<float> MagicAcademy = new QFramework.BindableProperty<float>(0); // 魔法学院产出时间
        public static QFramework.BindableProperty<int> MaxStamina = new QFramework.BindableProperty<int>(0); // 最大体力
        public static QFramework.BindableProperty<int> MaxCityHp = new QFramework.BindableProperty<int>(5000); // 最大生命值
        public static QFramework.BindableProperty<int> CityDefense = new QFramework.BindableProperty<int>(0); // 防御值
        public static QFramework.BindableProperty<int> Retaliation = new QFramework.BindableProperty<int>(0); // 反击伤害
        public static QFramework.BindableProperty<int> BuildingTime = new QFramework.BindableProperty<int>(0); // 产出时间
        public static QFramework.BindableProperty<int> strAtk = new QFramework.BindableProperty<int>(0); // 攻击力强化
        public static QFramework.BindableProperty<int> strShield = new QFramework.BindableProperty<int>(0); // 护盾值强化
        public static QFramework.BindableProperty<int> GameGold = new QFramework.BindableProperty<int>(0); // 游戏金币
        public static QFramework.BindableProperty<int> GameMana = new QFramework.BindableProperty<int>(0); // 最大游戏法力
        public static QFramework.BindableProperty<int> MaxGameMana = new QFramework.BindableProperty<int>(0); // 最大游戏法力
        public static QFramework.BindableProperty<int> GameManaFragment = new QFramework.BindableProperty<int>(0); // 游戏法力碎片
        public static QFramework.BindableProperty<int> GameReviveCount = new QFramework.BindableProperty<int>(0); // 游戏复活次数
        public static QFramework.BindableProperty<bool> Music = new QFramework.BindableProperty<bool>(true); // 游戏背景音乐
        public static QFramework.BindableProperty<bool> Sound = new QFramework.BindableProperty<bool>(true); // 游戏音效
        public static QFramework.BindableProperty<bool> moon = new QFramework.BindableProperty<bool>(false);//月卡

        public static Dictionary<int, CitySkill> CitySkills = new Dictionary<int, CitySkill>();// 主城技能
        public static QFramework.BindableProperty<bool> UnlimitedCdTime = new QFramework.BindableProperty<bool>(false); // 无限Cd时间 
        

        // 玩家数据
        public static UserData User = new UserData(); // 用户数据
        public static PlayerSpeaker UserPlayerSpeaker = new PlayerSpeaker();//轮语者数据
        public static Dictionary<int, PlayerWeapon> PlayerWeapons = new Dictionary<int, PlayerWeapon>(); // 玩家武器数据（key=流水id）
        public static Dictionary<int, PlayerAvatar> PlayerAvatars = new Dictionary<int, PlayerAvatar>(); // 玩家已拥有头像（key=流水id）
        public static Dictionary<int, PlayerAvatarFrame> PlayerAvatarFrames = new Dictionary<int, PlayerAvatarFrame>(); // 玩家已拥有头像框（key=流水id）
        public static Dictionary<int, PlayerCycleSpeakerSkin> PlayerCycleSpeakerSkins = new Dictionary<int, PlayerCycleSpeakerSkin>(); // 轮语者皮肤数据（key=流水id）
        public static UserPlayer Player = new UserPlayer(); // 玩家数据
        public static PlayerAtlas PlayerAtlas = new PlayerAtlas(); // 玩家图鉴数据
        public static PlayerSignin PlayerSignin = new PlayerSignin(); // 玩家签到数据
        public static PlayerBattle PlayerBattle = new PlayerBattle(); // 玩家战斗数据
        public static PlayerResource PlayerResource = new PlayerResource(); // 玩家资源数据
        public static PlayerActivityModel PlayerActivity = new PlayerActivityModel(); // 玩家活动数据
        public static PlayerChallenge PlayerChallenge = new PlayerChallenge(); // 玩家挑战数据
        public static PlayerExchangeGift PlayerExchangeGift = new PlayerExchangeGift(); // 玩家兑换礼包数据
        public static Dictionary<int, PlayerStage> PlayerStages = new Dictionary<int, PlayerStage>(); // 玩家关卡数据
        public static Dictionary<int, PlayerChapter> PlayerChapters = new Dictionary<int, PlayerChapter>(); // 玩家章节数据
        public static Dictionary<int, bool> LevelStage = new Dictionary<int, bool>();
        public static Dictionary<int, PlayerItemModel> PlayerItems = new Dictionary<int, PlayerItemModel>(); // 玩家道具数据
        public static Dictionary<int, PlayerCard> PlayerCards = new Dictionary<int, PlayerCard>(); // 玩家卡牌数据
        public static Dictionary<int, PlayerShop> PlayerShops = new Dictionary<int, PlayerShop>(); // 玩家商店数据
        public static Dictionary<int, PlayerTask> PlayerTasks = new Dictionary<int, PlayerTask>(); // 玩家任务数据
        public static Dictionary<TaskType, PlayerTaskActiveRewards> PlayerTaskActives = new Dictionary<TaskType, PlayerTaskActiveRewards>(); // 玩家任务活跃度数据
        public static Dictionary<int, PlayerGoods> PlayerGoods = new Dictionary<int, PlayerGoods>(); // 玩家商品数据
        public static Dictionary<int, ChapterGiftPack> ChapterGiftPacks = new Dictionary<int, ChapterGiftPack>(); // 关卡礼包购买状态（key=ChapterId）
        public static Dictionary<int, PlayerTeam> PlayerTeames = new Dictionary<int, PlayerTeam>(); // 玩家编队数据
        public static Dictionary<int, PlayerEquip> PlayerEquips = new Dictionary<int, PlayerEquip>(); // 玩家装备数据
        public static Dictionary<EquipPartType, PlayerEquipSlot> PlayerEquipSlots = new Dictionary<EquipPartType, PlayerEquipSlot>(); // 玩家装备槽位数据（key=EquipPartType）
        public static Dictionary<int, PlayerTalent> PlayerTalents = new Dictionary<int, PlayerTalent>(); // 玩家天赋数据
        public static Dictionary<int, PlayerModule> PlayerModules = new Dictionary<int, PlayerModule>(); // 玩家模块数据
        public static Dictionary<int, PlayerChestPool> PlayerChestPools = new Dictionary<int, PlayerChestPool>(); // 玩家抽卡池数据
        public static Dictionary<int, UserCreatePlayerActivityModel> CreatePlayerActivitys = new Dictionary<int, UserCreatePlayerActivityModel>(); // 玩家创角活动数据
        // [Obsolete]
        public static Dictionary<int, PlayerFusionGem> PlayerFusionGems = new Dictionary<int, PlayerFusionGem>(); // 玩家融合石数据
        public static Dictionary<int, PlayerArcResonance> PlayerArcResonances = new Dictionary<int, PlayerArcResonance>(); // 玩家奥数共鸣数据
        public static PlayerCityWallArchive PlayerCityWall = new PlayerCityWallArchive(); // 玩家城墙数据
        public static Dictionary<int, PlayerEndlessTower> PlayerEndlessTowers = new Dictionary<int, PlayerEndlessTower>(); // 玩家无尽塔数据
        public static Dictionary<int, PlayerChallengeBoss> PlayerChallengeBoss = new Dictionary<int, PlayerChallengeBoss>(); // 玩家挑战Boss数据
        public static Dictionary<int, PlayerHiddenRealmTower> PlayerHiddenRealmTowers = new Dictionary<int, PlayerHiddenRealmTower>(); // 玩家秘境塔数据
        public static Dictionary<int, PlayerTicket> PlayerTickets = new Dictionary<int, PlayerTicket>(); // 玩家门票数据
        
        
        
        // 临时状态
        public static int ConfirmDialogPanelStatus = 1; // 确认弹框状态
        

        // 道具配置
        public static Dictionary<ItemType, int> ItemTypeIds = new Dictionary<ItemType, int>();

        // 玩家资源
        public static QFramework.BindableProperty<int> Gold = new QFramework.BindableProperty<int>(0); // 金币
        public static QFramework.BindableProperty<int> Diamonds = new QFramework.BindableProperty<int>(0); // 钻石
        public static QFramework.BindableProperty<int> Stamina = new QFramework.BindableProperty<int>(0); // 体力
        public static QFramework.BindableProperty<int> CoreFragments = new QFramework.BindableProperty<int>(0); // 核心碎片

        public static int CitySkill = 500001; // 主城技能id
        public static int strengthenIndex = 0; // 强化节点
        public static QFramework.BindableProperty<bool> AutoReleaseSkillState = new QFramework.BindableProperty<bool>(false); // 自动释放技能状态

        /// <summary>
        /// 连续释放技能计数器（0-6层）
        /// </summary>
        public static QFramework.BindableProperty<int> ConsecutiveSkillReleaseCount = new QFramework.BindableProperty<int>(0);

        /// <summary>
        /// 连续释放技能伤害加成（连击数 * 0.2）
        /// </summary>
        public static QFramework.BindableProperty<float> ConsecutiveSkillReleaseDamageBonus = new QFramework.BindableProperty<float>(0);

        public static List<GemData> Gems = new List<GemData>(); // 宝石数据
        public static List<CityLoadData> CityLoads = new List<CityLoadData>(); //城主数据
        public static List<CardAtlas> CardAtlas = new List<CardAtlas>(); //图鉴数据
        public static QFramework.BindableProperty<int> CityEnhancementProgress = new QFramework.BindableProperty<int>(0); // 主城强化进度
        public static List<GemData> GemSlots = new List<GemData> { null, null, null, null, null, null }; //镶嵌数据

        public static List<BattleCardGroup> SoldierCardGroupList = new List<BattleCardGroup>(); // 战士卡组配置
        public static List<BattleCardGroup> SoldierSkillCardGroupList = new List<BattleCardGroup>(); // 战士技能卡组配置
        
        public static List<BattleCardGroup> SoldierTeams = new List<BattleCardGroup>(); // 战士卡组配置
        public static List<BattleCardGroup> SoldierSkillTeams = new List<BattleCardGroup>(); // 战士技能卡组配置
        
       
        public static Dictionary<QualityType, Color32> QualityTypeColors = new Dictionary<QualityType, Color32>();
        public static Dictionary<QualityType, Color32> QualityTypeFrameColors = new Dictionary<QualityType, Color32>();
        public static Dictionary<QualityType, Color32> QualityTypeFrameOutColors = new Dictionary<QualityType, Color32>();
        public static Dictionary<QualityType, Color32> ChooseCardQualityTypeColors = new Dictionary<QualityType, Color32>();
        public static Dictionary<MainAttributeType, Color32> MainAttributeTypeColors = new Dictionary<MainAttributeType, Color32>();

        public static UIPoolManager UIPool;
        public static TMP_SpriteAsset DamageAsset;
        /// <summary>InitGameConst 前由 GameStartMono 异步预载（WebGL 不支持同步加载）</summary>
        public static GameObject UIPoolRootPrefab;
        public static void InitAtlas()
        {
            var fields = typeof(Global).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            foreach (var field in fields)
            {
                var attr = field.GetCustomAttribute<SpriteAtlasTargetAttribute>();
                if (attr == null) continue;
                
                SpriteAtlas atlas = null;
                try
                {
                    atlas = ResLoader != null ? ResLoader.LoadSync<SpriteAtlas>("SpriteAtlas_"+attr.Name) : null;
                }
                catch
                {
                }
                if (atlas != null)
                {
                    field.SetValue(null, atlas);
                }
                else
                {
                    LogTool.LogWarning($"SpriteAtlas not found: {attr.Name}");
                }
            }
        }

        
        /// <summary>
        /// 注册图集替补链(fallback)。幂等;字段为 null 的链会被静默跳过,
        /// 由 AtlasManager.OnGlobalAtlasFieldsUpdated 在图集加载写回字段后重注册生效。
        /// </summary>
        public static void RegisterAtlasFallbacks()
        {
            GameSpriteAtlasExtensions.SetFallbackAtlases(ItemIconSpriteAtlas, EquipIconSpriteAtlas);
            GameSpriteAtlasExtensions.SetFallbackAtlases(SchoolChooseCardIconSpriteAtlas, ConnectionIconAtlas, SpecialSkillIconAtlas);
            GameSpriteAtlasExtensions.SetFallbackAtlases(CardIconSpriteAtlas, ConnectionIconAtlas, HeroHeadIconAtlas, HeroIconSpriteAtlas, SpecialSkillIconAtlas);
            GameSpriteAtlasExtensions.SetFallbackAtlases(ShopGoodsIconAtlas, ItemIconSpriteAtlas);
        }

        public static void InitGameConst()
        {
#if GAME_RELEASE || (!UNITY_EDITOR && !DEVELOPMENT_BUILD)
            Debug.Value = 0;
#else
            Debug.Value = 1; 
#endif
            if (Debug.Value == 0)
            {
                LogTool.Configure(false,LogLevel.Error,false);
            }
#if !UNITY_EDITOR
            // 真机/WASM 上 StackTrace 捕获开销大，错误堆栈由 Debug.LogError 自带兜底
            LogTool.SetIncludeStackTrace(false);
#endif
            LogTool.Log("加个日志"+Debug.Value);
            // 初始化图集
            ResLoader = new YooAssetLoader(YooAssets.GetPackage("DefaultPackage"));
            // EnableAutoPreload=true 时由 AtlasManager.PreloadCommonAtlasesAsync 加载并写回 Global 字段
            if (!AtlasManager.EnableAutoPreload)
            {
                InitAtlas();
            }
            // LoadAllAtlas("Assets/SpriteAtlas");
            // LoadingEffectFullScreen01

                // obj.Instantiate();
            
            RegisterAtlasFallbacks();
            // 面板级图集(如 SchoolChooseCardIcon)启动时未加载、字段为 null,fallback 链会被静默跳过;
            // 图集加载写回 Global 字段后重注册,保证后加载的图集也能挂上替补链(注册幂等)
            AtlasManager.OnGlobalAtlasFieldsUpdated += RegisterAtlasFallbacks;
            // 初始化稀有度颜色
            QualityTypeColors.Clear();
            var qualityTypeColorList = DataConfigManager.Tables.TbGameBattleConst.QualityTypeColors;
            foreach (var v in qualityTypeColorList)
            {
                QualityTypeColors.Add(v.Type, new Color(v.Color.X / 255.0f, v.Color.Y / 255.0f, v.Color.Z / 255.0f));
            }
            QualityTypeFrameColors.Clear();
            var qualityTypeFrameColors = DataConfigManager.Tables.TbGameBattleConst.QualityTypeFrameColors;
            foreach (var v in qualityTypeFrameColors)
            {
                QualityTypeFrameColors.Add(v.Type, new Color(v.Color.X / 255.0f, v.Color.Y / 255.0f, v.Color.Z / 255.0f));
            }
            QualityTypeFrameOutColors.Clear();
            var qualityTypeFrameOutColors = DataConfigManager.Tables.TbGameBattleConst.QualityTypeFrameOutColors;
            foreach (var v in qualityTypeFrameOutColors)
            {
                QualityTypeFrameOutColors.Add(v.Type, new Color(v.Color.X / 255.0f, v.Color.Y / 255.0f, v.Color.Z / 255.0f));
            }
            ChooseCardQualityTypeColors.Clear();
            var chooseCardQualityTypeColors = DataConfigManager.Tables.TbGameBattleConst.ChooseCardQualityTypeColors;
            foreach (var v in chooseCardQualityTypeColors)
            {
                ChooseCardQualityTypeColors.Add(v.Type, new Color(v.Color.X / 255.0f, v.Color.Y / 255.0f, v.Color.Z / 255.0f));
            }
            
            // 初始化所属派系颜色
            MainAttributeTypeColors.Clear();
            var mainAttributeTypeColors = DataConfigManager.Tables.TbGameBattleConst.MainAttributeTypeColors;
            foreach (var v in mainAttributeTypeColors)
            {
                MainAttributeTypeColors.Add(v.Type, new Color(v.Color.X / 255.0f, v.Color.Y / 255.0f, v.Color.Z / 255.0f));
            }
            UIKit.Config.PanelLoaderPool = new YooPanelLoaderPool();
            ///初始化UI对象池（UIPoolRoot 由 GameStartMono.PreloadGameConstAssets 异步预载）
            UIPool = Instantiate(UIPoolRootPrefab).GetComponent<UIPoolManager>();
            UIPool.gameObject.AddComponent<UIPoolWarmUpScheduler>();
        }

        /// <summary>
        /// 宝石影响数据
        /// </summary>
        public static int GemAttackPower = 0; // 宝石增加的攻击力

        public static float GemDamage = 0; // 宝石增加的伤害
        public static float GemAttackInterval = 0; // 宝石影响的攻击间隔
        public static float GemProjectileSpeed = 0; // 宝石影响的弹道速度
        public static int GemMainCityDefense = 0; // 宝石增加的主城防御
        public static float GemSoldierDamage = 0; // 宝石增加的士兵伤害
        public static float GemArcherDamage = 0; // 宝石增加的弓箭手伤害
        public static float GemMageDamage = 0; // 宝石增加的魔法师伤害
        public static float GemQuickSlashDamage = 0; // 宝石增加的快速挥砍伤害
        public static float GemSplitSlashDamage = 0; // 宝石增加的分裂斩次斩伤害
        public static float GemWhirlwindSlashDamage = 0; // 宝石增加的旋风斩伤害
        public static float GemCrossbowDamage = 0; // 宝石增加的连弩伤害
        public static float GemArrowRainDamage = 0; // 宝石增加的万箭齐发伤害
        public static float GemShooterFocusDamage = 0; // 宝石增加的射手专注伤害

        // 特殊效果
        public static float GemEveryDayRestoration = 0f; // 宝石增加的每天主城恢复
        public static int GemImmuneDamageCount = 0; // 宝石提供的免疫伤害次数
        public static float GemCounterattack = 0f; // 宝石增加的反伤
        public static float GemEveryDayDamageUp = 0f; // 宝石增加的每天伤害提高
        public static float GemLowHpDamageUp = 0f; // 宝石增加的低血量提升伤害
        public static float GemCloseRangeDamageUp = 0f; // 宝石增加的近距离提升伤害
        public static float GemMonstersHighHpDamageUp = 0f; // 宝石增加的怪物高血量提升伤害
        public static float GemKillEliteRegen = 0f; // 宝石增加的击杀精英回血
        public static float EveryDayDamageUp = 0f; // 每天伤害提高

        // 重置属性方法
        public static void ResetProperties()
        {
            Date.Value = 0;
            TotalDamage.Value = 0;
            CityExp.Value = 0;
            CityLevel.Value = 1;
            CityPopulation.Value = 0;
            RotaryTableOpenMaxNum.Value = 0;
            MaxCityPopulation.Value = 3;
            BuildUnLock.Value = 0;
            SpeedMultiplier.Value = 1;
            SpearmanAtk.Value = 0;
            SpearmanInterval.Value = 0;
            ArcherAtk.Value = 0;
            ArcherInterval.Value = 0;
            MagicianAtk.Value = 0;
            MagicianInterval.Value = 0;
            Barracks.Value = 0;
            ShooterCamp.Value = 0;
            MagicAcademy.Value = 0;
            MaxStamina.Value = 30;
            EveryDayDamageUp = 0;
            GameMana.Value = 0;// 清空法力
            GameManaFragment.Value = 0;// 清空法力碎片
            GameReviveCount.Value = 0;// 重置复活次数
            GameGold.Value = 0;
            ConsecutiveSkillReleaseCount.Value = 0;// 重置连击计数器
            ConsecutiveSkillReleaseDamageBonus.Value = 0;// 重置连击伤害加成
            UnlimitedCdTime.Value = false;// 重置能量迸发等无冷却状态
            // 重置贪婪预支透支
            if (GeneralSkillManager.Instance != null)
                GeneralSkillManager.Instance.ResetOverdraft();
        }

        public static void ResetGemData()
        {
            GemAttackPower = 0;
            GemDamage = 0;
            GemAttackInterval = 0;
            GemProjectileSpeed = 0;
            GemMainCityDefense = 0;
            GemSoldierDamage = 0;
            GemArcherDamage = 0;
            GemMageDamage = 0;
            GemQuickSlashDamage = 0;
            GemSplitSlashDamage = 0;
            GemWhirlwindSlashDamage = 0;
            GemCrossbowDamage = 0;
            GemArrowRainDamage = 0;
            GemShooterFocusDamage = 0;

            GemEveryDayRestoration = 0f;
            GemImmuneDamageCount = 0;
            GemCounterattack = 0;
            GemEveryDayDamageUp = 0f;
            GemLowHpDamageUp = 0f;
            GemCloseRangeDamageUp = 0f;
            GemMonstersHighHpDamageUp = 0f;
            GemKillEliteRegen = 0f;
            
            CitySkills.Clear();

            // 清除战内消息订阅
            Publisher.UnsubscribeKeyAll(MessagePublisher.CHANGE_SCHOOL_STAR);
        }
        
        /**
         * 显示通用确认框
         */
        public static void ShowConfirm(string title, string message, Action onConfirm, Action onCancel = null, bool noCancel = false)
        {
            var pool = CommonItemPoolManager.Instance.GetGameObjectPool("ConfirmDialogPanel");
            if (pool == null) return;
            
            var confirmDialog = pool.Get();
            // confirmDialog.transform.SetParent(transform);
            confirmDialog.transform.SetParent(UIFramework.UIRoot.Instance.Config.LevelConfigs.Find(ele =>
            {
                return ele.Level == UIFramework.UILevel.PopUI;
            }).RootTransform);
            
            // confirmDialog.transform.localScale = Vector3.one;
            // confirmDialog.transform.position = transform.position;
            ConfirmDialogPanel dialogScript = confirmDialog.GetComponent<ConfirmDialogPanel>();
            dialogScript.SetData(title, message, onConfirm, onCancel, noCancel);
        }
        
        // 获取道具数量
        public static long GetItemNumById(int itemId)
        {
            PlayerItemModel itemModel;
            if (!PlayerItems.TryGetValue(itemId, out itemModel)) return 0;
            
            return itemModel.ItemNum;
        }
        
        // 通过道具类型，获取道具信息
        public static PlayerItemModel GetItemByType(ItemType itemType)
        {
            var itemId = 0;
            foreach (var v in DataConfigManager.Tables.TbItemConfig.DataList)
            {
                if (v.Type == itemType)
                {
                    itemId = v.Id;
                    break;
                }
            }

            PlayerItemModel item;
            if (!PlayerItems.TryGetValue(itemId, out item)) return null;
            return item;
        }

        [CanBeNull]
        public static PlayerCard GetCardById(int cardId, PlayerCard defaultValue = null)
        {
            return PlayerCards.GetValueOrDefault(cardId, defaultValue);
        }
        
        public static PlayerGoods GetGoodsById(int goodsId)
        {
            PlayerGoods goods;
            if (!PlayerGoods.TryGetValue(goodsId, out goods))
            {
                goods = new PlayerGoods();
                goods.GoodsId = goodsId;
            }

            return goods;
        }

        // 获取最大通关名称
        public static string GetMaxClearStageName()
        {
            var clearStageId = Global.PlayerBattle.ClearStageId;
            if (clearStageId == 0) return "";

            var cfg = DataConfigManager.Tables.TbStageBattleConfig.GetOrDefault(clearStageId);
            return cfg.StageName;
        }
    }
 

    // 宝石数据结构
    public class GemData
    {
        public int GemUid; // 宝石ID
        public int GemId; // 宝石ID
        public int Quality; // 宝石品质（如：普通、稀有、史诗）
        public GemType GemType;
        public int Experience; // 宝石经验
        public int Level; // 宝石等级

        public GemData(int gemUid,int gemId, int quality,GemType gemType, int experience, int level)
        {
            GemUid = gemUid;
            GemId = gemId;
            Quality = quality;
            GemType = gemType;
            Experience = experience;
            Level = level;
        }
    }
    
    // 城主数据结构
    public class CityLoadData
    {
        public int Id; // 主城ID
        public bool IsGotoWar;

        public CityLoadData(int id, bool isGotoWar = false)
        {
            Id = id;
            IsGotoWar = isGotoWar;
        }
    }
    
    // 卡牌图鉴结构
    public class CardAtlas
    {
        public int CardUid; // 卡牌uid
        public int CardId; // 卡牌ID
        public bool UnlockStatus; // 解锁状态
        public int Level; // 卡牌等级
        public int Fragments; // 卡牌碎片

        public CardAtlas(int cardId, bool unlockStatus,int level, int fragments)
        {
            // CardUid = cardUid;
            CardId = cardId;
            UnlockStatus = unlockStatus;
            Level = level;
            Fragments = fragments;
        }
    }
    
    
}
