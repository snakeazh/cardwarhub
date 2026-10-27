using System;
using System.Collections.Generic;
using System.Linq;
using Config;
using DG.Tweening;
using UnityEngine;
using QFramework;
using UnityEditor;
using UnityEngine.U2D;
using Random = UnityEngine.Random;
using System.Collections;
using Cysharp.Threading.Tasks;
using FusionContinent.Attribute;
using FusionContinent.Base;
using FusionContinent.Guide;
using FusionContinent.Manager;
using FusionContinent.Model;
using FusionContinent.Module;
using FusionContinent.Skill;
using FusionContinent.Skill.Config;
using FusionContinent.Target;
using FusionContinent.Sounds;
using FusionContinent.Monster.Animation;
using FusionContinent.SpineBake;
using FusionContinent.Utils;
using Game.Battle;
using Game.Player;
using Game.Soldier;
using Game.Team;
using Manager;
using Newtonsoft.Json;
using UIFramework.Services;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using YooAsset;
using Object = UnityEngine.Object;
using Sequence = DG.Tweening.Sequence;
using ValueType = Config.ValueType;

namespace FusionContinent
{
    public partial class GamePlayManager : ViewController
    {
        // public const int annaBeiDongID = 2030004;
        
        
        
        public AsyncResLoader _resLoader;
        public static GamePlayManager Default;
        private TbPropertyConfig _propertyConfig;
        public TbMonsterGenConfig _monsterGenConfig;
        private TbMonsterBattleConfig _monsterBattleConfig;

        private TbBuildingBattleConfig _tbBuildingBattleConfig;

        public float gameTime = 0f;
        public int enemyCount = 0;
        private bool isDayInProgress = false;
        private ChooseCardType _choosePanelType = 0; // 三选一面板类型
        public List<BuildingCard> placedCards = new List<BuildingCard>(); // 存储当前已有的建筑卡牌ID列表

        // 在 GamePlay 类中添加一个 HashSet 来记录已经生成的波次
        private HashSet<string> spawnedWaves = new HashSet<string>();

        public List<SoldierCard> villageGameCards = new List<SoldierCard>();
        /// <summary>PVP 对侧英灵列表（挂在 GameLand/pvp FriendlySlot）。</summary>
        public List<SoldierCard> opponentGameCards = new List<SoldierCard>();

        // 追踪每个槽位状态的列表，初始化时假设所有槽位为空
        public Dictionary<int, BuildingCard> buildingDic = new Dictionary<int, BuildingCard>();
        public Dictionary<int, Transform> buildingLock = new Dictionary<int, Transform>();
        private Dictionary<string, int> spawnedMonsterCounts = new Dictionary<string, int>(); // 记录各个怪物生成数量
        private Dictionary<int, int> spawnedMonsterPositions = new Dictionary<int, int>(); // 记录各个怪物生成坐标

        private int totalMonsterCount; // 当前总怪物数量
        private int spawnedMonsterCount; // 已生成怪物数量
        private int deadCount; // 死亡怪物数量
        public List<Transform> ordinaryPositions = new List<Transform>();
        public List<Transform> elitePositions = new List<Transform>();
        public List<Transform> bossPositions = new List<Transform>();
        private int currentWaveIndex; // 当前生效的刷怪下标
        private int currentWaveSpawnDataIndex; // 当前生效的刷怪下标
        private float nextWaveTime = 0f; // 下一个刷怪配置的时间
        public Transform[] friendlyPositions;
        public bool[] friendlyPositionsLocked;
        public Transform[] opponentFriendlyPositions;
        public bool[] opponentFriendlyPositionsLocked;
        private Transform _opponentFriendlySlotRoot;
        private int _spawnedOpponentTeamSoldierCount;
        public event Action<bool> OnPauseChanged; // 暂停事件
        private bool isPaused = true;
        private const float TimeScaleEpsilon = 0.0001f;
        private float _timeScaleBeforePause = 1f;

        public ViolentConflicts violentConflicts;
        public CrackedAirBlade crackedAirBlade;
        public StormPuncture stormPuncture;
        public DroppingFireController droppingFireController;

        public bool IsPaused() => isPaused;


        public TbGameBattleConst GameBattleConst;

        public Dictionary<int, BuffCard> BuffCards = new Dictionary<int, BuffCard>(); // buff列表
        private int _spawnedTeamSoldierCount = 0; // 当前已从编队中上场的英雄数量

        // 法力
        public int GameManaFragmentToEntire;
        private int recoverGameManaNum;
        private float recoverGameManaTime;
        private float recoverGameManaTimeLimit;

        private static readonly object _lockObject = new object();

        public bool Isloodingover = false; //加载是否完毕
        private Sequence spawnTween;
        private float currentDateTime;
        // public GameObject bombProjectile;
        public float MoseterxLast; //上一只怪物的位置
        public MonsterGenConfig _monsterGenCfg;
        public MonsterGenStage _monsterGenDayList;
        public readonly Dictionary<int, int> _monsterGenDayUpperLimitMap = new Dictionary<int, int>(); // 刷怪天数刷怪上限
        public StageBattleConfig StageCfg;
        private bool _isStageGuide = false;

        // 解锁英雄详情界面
        public bool UnlockSoldierDetailPanel = false;
        public bool battleLog = false;

        // 卡能值伤害转换
        public bool CanGameMananToAtkDamage;

        // 攻击力加成翻倍
        public bool CanAttackAdditionDouble;

        // 英雄释放主动技能
        public bool CanSoldierSkillRelease = true;

        // 取舍有道
        public ChoosingWiselyModule ChoosingWiselyModule = new ChoosingWiselyModule();

        // 稀有度颜色
        public Dictionary<QualityType, Color32> QualityTypeColors = new Dictionary<QualityType, Color32>();

        public LoadingPanel loadingPanel; // 加载页面
        
        // 战斗天状态
        private BattleDayState _dayState;
        
        // 一人成军（创建镜像时间）
        private float _createMirrorTime;
        public float OneManArmyCreateMirrorTime;


        /**
         * 所有模块聚合属性加成
         */
        public AttributeSet ModuleAttributes;
        
        /**
         * 所有怪物属性加成
         */
        public AttributeSet MonsterAttributes;

        public Dictionary<MainAttributeType, float> ModuleMainAttributes;
        
        // 祈祷技能
        public PraySkill PraySkill = new();

        public SkillConfigManager _skillConfigManager = new();
        
        //把通用的计算基准值放在这里 - 使用字典存储不同类型的伤害计算管线
        private Dictionary<DamagePipelineType, DamagePipeline> _damagePipelines;

        /// <summary>
        /// 获取指定类型的伤害计算管线
        /// </summary>
        public DamagePipeline GetDamagePipeline(DamagePipelineType type)
        {
            if (_damagePipelines == null)
            {
                InitializeDamagePipelines();
            }
            return _damagePipelines.TryGetValue(type, out var pipeline) ? pipeline : _damagePipelines[DamagePipelineType.SoldierDamage];
        }

        /// <summary>
        /// 初始化所有伤害计算管线
        /// </summary>
        private void InitializeDamagePipelines()
        {
            _damagePipelines = new Dictionary<DamagePipelineType, DamagePipeline>
            {
                { DamagePipelineType.MonsterDamage, new DamagePipeline(DamagePipelineType.MonsterDamage) },
                { DamagePipelineType.SoldierDamage, new DamagePipeline(DamagePipelineType.SoldierDamage) },
                { DamagePipelineType.SceneSkillDamage, new DamagePipeline(DamagePipelineType.SceneSkillDamage) }
            };
            
            // 兼容旧代码：保留一个默认的 damagePipeline 引用
            damagePipeline = _damagePipelines[DamagePipelineType.SoldierDamage];
        }

        /// <summary>
        /// 兼容旧代码的属性（默认使用 SoldierDamage 管线）
        /// </summary>
        public DamagePipeline damagePipeline { get; private set; }

        public bool UseLockAttackInterval = false;

        public bool UseLockAttackSpeed = false;
        
        /// <summary>
        /// 局内怪物 Spine 网格预烘焙开关。开启后普通怪优先查表播帧，关闭则走完整骨骼链路。
        /// </summary>
        public bool UseSpineMeshBake =>
            MonsterAnimationSettings.GlobalRenderMode.Value == AnimationRenderMode.BakeMesh;

        public void SetSpineMeshBakeEnabled(bool enabled)
        {
            MonsterAnimationSettings.SetGlobalRenderMode(
                enabled ? AnimationRenderMode.BakeMesh : AnimationRenderMode.Spine);
        }

        public void SetSpineMeshBakePerfLogEnabled(bool enabled)
        {
            SpineMeshBakeSettings.SetPerfLogEnabled(enabled);
        }

        public void SetSpineMeshBakePerfOverlayVisible(bool visible)
        {
            SpineMeshBakeSettings.SetRuntimeOverlayVisible(visible);
        }

        public bool IsSpineMeshBakePerfLogEnabled =>
            SpineMeshBakeSettings.EnablePerfLog.Value;

        public bool IsSpineMeshBakePerfOverlayVisible =>
            SpineMeshBakeSettings.ShowRuntimeOverlay.Value;

        public void EnsureSpineMeshBakePerfMonitor()
        {
            if (GetComponent<SpineMeshBakePerfMonitor>() == null)
            {
                gameObject.AddComponent<SpineMeshBakePerfMonitor>();
            }
        }

        public void EnsureSpineMeshBakePerfDebugOverlay()
        {
            if (GetComponent<SpineMeshBakePerfDebugOverlay>() == null)
            {
                gameObject.AddComponent<SpineMeshBakePerfDebugOverlay>();
            }
        }

        public void EnsureSpineMeshBakeUpdater()
        {
            if (GetComponent<SpineMeshBakeUpdater>() == null)
            {
                gameObject.AddComponent<SpineMeshBakeUpdater>();
            }
        }

        /// <summary>
        /// 从本关刷怪配置收集所有会出现的普通怪 Spine 资源名（去重）。
        /// </summary>
        public HashSet<string> CollectStageMonsterSpineKeys()
        {
            var spineKeys = new HashSet<string>();
            if (_monsterGenCfg == null || _monsterBattleConfig == null)
            {
                return spineKeys;
            }

            foreach (MonsterGenStage dayCfg in _monsterGenCfg.StageList)
            {
                foreach (MonsterGenWave wave in dayCfg.WaveList)
                {
                    foreach (CardIdNumLv card in wave.GenCards)
                    {
                        CollectMonsterSpineKey(spineKeys, card.CardId);
                    }
                }
            }

            return spineKeys;
        }

        /// <summary>
        /// 收集本关所有怪物的 Spine 预制体名（MonsterCard.Init 必同步加载，不做 BakeMesh 普通怪过滤）。
        /// </summary>
        public HashSet<string> CollectStageMonsterSpinePrefabKeys()
        {
            var keys = new HashSet<string>();
            if (_monsterGenCfg == null || _monsterBattleConfig == null)
            {
                return keys;
            }

            foreach (MonsterGenStage dayCfg in _monsterGenCfg.StageList)
            {
                foreach (MonsterGenWave wave in dayCfg.WaveList)
                {
                    foreach (CardIdNumLv card in wave.GenCards)
                    {
                        MonsterBattleConfig battleCfg = _monsterBattleConfig.GetOrDefault(card.CardId);
                        if (battleCfg == null || string.IsNullOrEmpty(battleCfg.Spine))
                        {
                            continue;
                        }
                        keys.Add(battleCfg.Spine);
                    }
                }
            }

            return keys;
        }

        /// <summary>
        /// 收集当前出战队伍所有英灵的 Spine 预制体名（SoldierAnimationController.CreateSpineObject 同步读取 _resLoader）。
        /// 名单来源与 SpawnTeamSoldiers 一致：Global.SoldierTeams 过滤已解锁士兵卡。
        /// </summary>
        public HashSet<string> CollectTeamSoldierSpinePrefabKeys()
        {
            var keys = new HashSet<string>();
            var soldierBattleConfig = DataConfigManager.Tables.TbSoldierBattleConfig;
            foreach (var v in Global.SoldierTeams)
            {
                if (v.CardType != CardType.Soldier || v.CardId == 0 || v.IsUnlock.Value == false)
                {
                    continue;
                }

                var cfg = soldierBattleConfig.GetOrDefault(v.CardId);
                if (cfg == null || string.IsNullOrEmpty(cfg.Spine))
                {
                    continue;
                }
                keys.Add(cfg.Spine);
            }

            return keys;
        }

        /// <summary>
        /// 预加载本关所有普通怪的 Spine 网格烘焙帧到内存缓存。
        /// </summary>
        public void PreloadStageSpineBakeData()
        {
#if UNITY_WEBGL
            // WebGL 不支持同步加载，由 ScenePreloadBattleAssets 调 PreloadStageSpineBakeDataRoutine 异步预载
            return;
#else
            if (MonsterAnimationSettings.GlobalRenderMode.Value != AnimationRenderMode.BakeMesh)
            {
                return;
            }

            BakeMeshFrameLoader.ClearCache();
            HashSet<string> spineKeys = CollectStageMonsterSpineKeys();
            BakeMeshFrameLoader.Preload(spineKeys);
            LogTool.Log("[GamePlayManager] 预加载 BakeMesh 帧: " + spineKeys.Count + " 个");
#endif
        }

        /// <summary>
        /// WebGL：异步预载本关怪物 Spine 烘焙帧字节到 ResLoader 缓存，再同步填充 BakeMeshFrameLoader 缓存。
        /// 需在 _monsterGenCfg 就绪后调用（SceneInitMonsterConfig 之后）。
        /// </summary>
        public IEnumerator PreloadStageSpineBakeDataRoutine()
        {
            if (MonsterAnimationSettings.GlobalRenderMode.Value != AnimationRenderMode.BakeMesh)
            {
                yield break;
            }

            BakeMeshFrameLoader.ClearCache();
            HashSet<string> spineKeys = CollectStageMonsterSpineKeys();
            foreach (var skeletonName in spineKeys)
            {
                var frameKey = BakeMeshFrameLoader.GetFrameAssetKey(skeletonName);
                string[] assetKeys =
                {
                    frameKey,
                    BakeMeshFrameLoader.YooAssetGroupPrefix + frameKey,
                    BakeMeshFrameLoader.BakeMeshGroupPrefix + frameKey
                };
                for (int i = 0; i < assetKeys.Length; i++)
                {
                    if (!Global.ResLoader.CheckLocationValid(assetKeys[i]))
                    {
                        continue;
                    }
                    yield return Global.ResLoader.LoadAsync<TextAsset>(assetKeys[i], null);
                    break;
                }
            }

            // 字节已在 ResLoader 缓存，BakeMeshFrameLoader 的同步读全部命中
            BakeMeshFrameLoader.Preload(spineKeys);
            LogTool.Log("[GamePlayManager] 异步预载 BakeMesh 帧: " + spineKeys.Count + " 个");
        }

        /// <summary>
        /// 收集战斗入场需预载的特效：通用死亡/出场/连击特效 + TbSpecialEffectsConfig 全表的弹道/命中/Link 特效
        /// + TbSchoolConfig 门派星级达成特效 + TbGameBattleConst 战场互动物件受击特效。
        /// WebGL 下 EffectManager 与 GamePlayManager._resLoader 都只能读已预载资源，配置表即真值源，全量预载避免遗漏。
        /// </summary>
        public List<string> CollectBattlePreloadEffectNames()
        {
            var names = new HashSet<string>
            {
                "MonsterDeathEffect02",
                "BossDeathEffect01",
                "MonsterDeathEffectBoss",
                "MonsterDeathEffectElite",
                "HeroDeathEffect01",
                "ComboEffectSix01",
                "ComboEffectSix02",
                "ArcherProjectile",          // SplitHitModule 默认弹道
                "ArcherProjectileSplits",    // SplitHitModule 首次分裂
                "ArcherProjectileSplitsAgain", // SplitHitModule 再次分裂
                GlobalConst.BurnEffect,
                // 羁绊特效（GameLandUI / BattleSchoolsPanelNew 按名硬编码取池，池 miss 时同步读 ResLoader 缓存）
                "BondEffectGet01",
                "BondEffectGet02",
                "BondEffectGet03",
                "BondEffectGetColor01",
                "BondEffectGetColor02",
                "BondEffectGetColor03",
                "BondEffectGetGolden01",
                "BondEffectGetGolden02",
                "BondEffectGetSilvery01",
            };

            foreach (var specialCfg in DataConfigManager.Tables.TbSpecialEffectsConfig.DataList)
            {
                if (!string.IsNullOrEmpty(specialCfg.ProjectileName)) names.Add(specialCfg.ProjectileName);
                if (!string.IsNullOrEmpty(specialCfg.SkillHitEffect)) names.Add(specialCfg.SkillHitEffect);
                if (specialCfg.LinkEffects == null) continue;
                foreach (var linkEffect in specialCfg.LinkEffects)
                {
                    if (!string.IsNullOrEmpty(linkEffect)) names.Add(linkEffect);
                }
            }

            // 门派星级达成特效（SoldierCard.OnPublisherSchoolStarChanged 读取 _resLoader）
            foreach (var schoolCfg in DataConfigManager.Tables.TbSchoolConfig.DataList)
            {
                if (schoolCfg.CardCountList == null) continue;
                foreach (var countCfg in schoolCfg.CardCountList)
                {
                    if (!string.IsNullOrEmpty(countCfg.EffectName)) names.Add(countCfg.EffectName);
                }
            }

            // 战场互动物件受击特效（SceneInteractiveProp 读取 _resLoader）
            var battleConst = DataConfigManager.Tables.TbGameBattleConst;
            if (battleConst.BattleSceneBodyEffects != null)
            {
                foreach (var bodyEffect in battleConst.BattleSceneBodyEffects)
                {
                    if (!string.IsNullOrEmpty(bodyEffect.EffectName)) names.Add(bodyEffect.EffectName);
                }
            }

            return new List<string>(names);
        }

        private void CollectMonsterSpineKey(HashSet<string> spineKeys, int cardId)
        {
            MonsterBattleConfig battleCfg = _monsterBattleConfig.GetOrDefault(cardId);
            if (battleCfg == null || string.IsNullOrEmpty(battleCfg.Spine))
            {
                return;
            }

            if (MonsterAnimationSettings.OnlyOrdinaryMonsters.Value
                && battleCfg.MonsterQuality != MonsterQuality.Ordinary)
            {
                return;
            }

            spineKeys.Add(battleCfg.Spine);
        }

        private async void Start()
        {
            CardRuchang = 0;
            // LogTool.Log($">>>>>> 开始初始GamePlayManager <<<<<<");
            PauseGame();
            SoundManager.Instance.PlayBgm(SoundType.SoundBattleBGM);

            // SkillGraphConfig skillGraphConfig = _skillConfigManager.Get("1001");
            // LogTool.Log(skillGraphConfig);
            // LogTool.Log(skillGraphConfig.Nodes[0].NodeType);
            BattleAudioSystem.Init();
            // 显示加载页面
            //loadingPanel.Show();
            //loadingPanel.StartFakeLoading();
        }
        
        /// <summary>
        /// 初始化对象池调度器
        /// </summary>
        private void InitObjectPoolScheduler()
        {
            _objectPoolWarmUpScheduler = gameObject.AddComponent<ObjectPoolWarmUpScheduler>();
            _objectPoolWarmUpScheduler.SetMaintenance("Damage",100,50);
            _objectPoolWarmUpScheduler.SetMaintenance("AnnaSkillArrows01",100,50);
            _objectPoolWarmUpScheduler.SetMaintenance("Kapaiposui02",100,50);
            _objectPoolWarmUpScheduler.SetMaintenance("Wanjianprefect",20,20);
            _objectPoolWarmUpScheduler.SetMaintenance("Annanormalattackhit01" ,20,20);
            // _objectPoolWarmUpScheduler.SetMaintenance("MonsterCard1" ,20,20);
            
            _objectPoolWarmUpScheduler.StartGlobalWarmUp();
        }
        
        private void SetSkillEffect(SpecialEffectsConfig specialCfg)
        {
            if (specialCfg == null)
            {
                return;
            }

            var effectManager = EffectManager.Instance;
            if (effectManager == null)
            {
                LogTool.LogWarning("[GamePlayManager] SetSkillEffect 跳过：EffectManager.Instance is null");
                return;
            }

            // 弹道 / 命中 / Link 特效预热，避免命中帧 ObjectPool.Get → Instantiate + 读盘尖刺
            if (!string.IsNullOrEmpty(specialCfg.ProjectileName))
            {
                effectManager.PreloadEffectPool(specialCfg.ProjectileName, 12);
            }

            if (!string.IsNullOrEmpty(specialCfg.SkillHitEffect))
            {
                effectManager.PreloadEffectPool(specialCfg.SkillHitEffect, 20);
            }

            if (specialCfg.LinkEffects == null || specialCfg.LinkEffects.Count <= 0)
            {
                return;
            }

            foreach (var linkEffect in specialCfg.LinkEffects)
            {
                if (string.IsNullOrEmpty(linkEffect))
                {
                    continue;
                }

                effectManager.PreloadEffectPool(linkEffect, 10);
            }
        }

        private static void WarmBattleStaticConstructors()
        {
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(
                typeof(FusionContinent.Buff.BuffFactory).TypeHandle);
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(
                typeof(FusionContinent.Skill.Handler.SkillHandlerRegistry).TypeHandle);
        }

        /// <summary>
        /// 预热局内高频/首次易尖刺的通用特效（死亡等），不依赖出战技能配置。
        /// </summary>
        private static void PreloadCommonBattleEffects()
        {
            var effectManager = EffectManager.Instance;
            if (effectManager == null)
            {
                LogTool.LogWarning("[GamePlayManager] PreloadCommonBattleEffects 跳过：EffectManager.Instance is null");
                return;
            }

            // 普通怪死亡最频繁
            effectManager.PreloadEffectPool("MonsterDeathEffect02", 12);
            // Boss/精英共用世界特效 + Canvas 飘字类特效
            effectManager.PreloadEffectPool("BossDeathEffect01", 4);
            effectManager.PreloadEffectPool("MonsterDeathEffectBoss", 2);
            effectManager.PreloadEffectPool("MonsterDeathEffectElite", 4);
            // 英灵死亡
            effectManager.PreloadEffectPool("HeroDeathEffect01", 4);
        }

        private ObjectPoolWarmUpScheduler _objectPoolWarmUpScheduler;
        private void Awake()
        {
            SetSpineMeshBakeEnabled(true);
            SetSpineMeshBakePerfLogEnabled(false);   // 性能日志
            SetSpineMeshBakePerfOverlayVisible(false); // 局内浮层
            
            DOTween.SetTweensCapacity(5000, 1000); // 设置最大容量为 500 个 Tween，同时保持 100 个 Tween 的缓冲
            Default = this;
            _resLoader = new AsyncResLoader(YooAssets.GetPackage("DefaultPackage"));
            _monsterGenConfig = DataConfigManager.Tables.TbMonsterGenConfig;
            _monsterBattleConfig = DataConfigManager.Tables.TbMonsterBattleConfig;
            GameBattleConst = DataConfigManager.Tables.TbGameBattleConst;
            _propertyConfig = DataConfigManager.Tables.TbPropertyConfig;

            // 初始化稀有度颜色
            var qualityTypeColorList = DataConfigManager.Tables.TbGameBattleConst.QualityTypeColors;
            foreach (var v in qualityTypeColorList)
            {
                QualityTypeColors.Add(v.Type, new Color(v.Color.X / 255.0f, v.Color.Y / 255.0f, v.Color.Z / 255.0f));
            }


            // 预热对象池调度器（需要在InitCardTeams之前初始化）
            InitObjectPoolScheduler();
            
            // 初始化连击计数器
            gameObject.AddComponent<ConsecutiveSkillReleaseCounter>();

            if (GetComponent<SpineMeshBakePerfMonitor>() == null)
            {
                gameObject.AddComponent<SpineMeshBakePerfMonitor>();
            }

            EnsureSpineMeshBakePerfDebugOverlay();
            EnsureSpineMeshBakeUpdater();
            
            // 开始初始化流程
            StartInitWithLoadingManager();
            UseLockAttackSpeed = false;
            UseLockAttackInterval = false;
            //bombProjectile = yoo.Load<GameObject>("MagicianBombSkill");
            // DamagePool = ObjectPoolManager.Instance.GetGameObjectPool("Damage");
        }
        /// <summary>
        /// 使用SceneLoadingManager开始初始化
        /// </summary>
        private void StartInitWithLoadingManager()
        {
            var manager = SceneLoadingManager.Instance;
            if (manager == null)
            {
                LogTool.LogError("SceneLoadingManager.Instance is null, falling back to legacy init");
                return;
            }
            
            // 清空旧的模块（如果有）
            // 添加数据初始化模块
            var dataModules = new List<SceneMapBase>
            {
                gameObject.AddComponent<SceneInitObjectPool>(),
                gameObject.AddComponent<SceneInitAttributes>(),
                gameObject.AddComponent<SceneInitMonsterConfig>(),
                gameObject.AddComponent<ScenePreloadBattleAssets>(), // WebGL 异步预载战斗资源（卡牌预制体/Spine烘焙帧/特效），依赖怪物配置就绪
                gameObject.AddComponent<SceneInitStageSettings>(),
                gameObject.AddComponent<SceneInitAttributeAggregation>(),
            };
            
            // 添加显示初始化模块（注意：SceneInitStartSpawning 必须在最后）
            var displayModules = new List<SceneMapBase>
            {
                gameObject.AddComponent<SceneInitBuildingSlots>(),
                gameObject.AddComponent<SceneInitPositions>(),
                gameObject.AddComponent<SceneInitWaveConfig>(),
                gameObject.AddComponent<SceneInitManagers>(),
                gameObject.AddComponent<SceneInitCardTeams>(), // 初始化出战单位（技能卡牌）
                gameObject.AddComponent<SceneInitGreedyAdvance>(), // 贪婪预支卡开局金币（必须在奥数卡加载后）
                gameObject.AddComponent<SceneInitStartSpawning>(), // 刷怪必须在最后
            };
            
            manager.SetDataModules(dataModules);
            manager.SetDisplayModules(displayModules);
            
            // 注册回调
            manager.onAllComplete.AddListener(OnInitComplete);
            
            // 开始加载
            manager.StartLoading();
        }
        
        /// <summary>
        /// 初始化完成回调
        /// </summary>
        private void OnInitComplete()
        {
            var vm = new BattlePanelViewModel();
            vm.OnCreate();
            
            UIFramework.UIManager.OpenPanelAsync<BattlePanelNew, BattlePanelViewModel>(vm, UIFramework.UILevel.Common).Forget();

            // 加载结束再确保对侧上场（防止加载期暂停导致漏刷）
            if (BattleSession.IsPvpAi)
            {
                InitOpponentCardTeams();
            }

            DOVirtual.DelayedCall(0.2f, () =>
            {
                StartGame();
                BattleRuleRoundStartTrigger.Trigger();
                DOVirtual.DelayedCall(0.5f, () =>
                {
                    TriggerGuide(GuideType.GuideStageStory); // 引导 - 故事
                    DetectAdjacentSoldier(); // 检测相邻的角色
                });
            }, true);
            
        }


        public List<MonsterEntryLevelStruct> MonsterEntries = new List<MonsterEntryLevelStruct>();
        
        MonsterEntryLevelStruct StartRandomMonsterEntry(StageBattleConfig battleConfig)
        {
            MonsterEntryLevelStruct data = null;
            var entry = battleConfig.MonsterRandomEntry.FirstOrDefault();
            var dataList = DataConfigManager.Tables.TbMonsterEntryConfig.DataList;
            int minLv = entry.RandomLevelRange[0];
            int maxLv = entry.RandomLevelRange[1];
            List<MonsterEntryLevelStruct> RandomMonsterEntryList = new List<MonsterEntryLevelStruct>();
            foreach (var item in dataList)
            {
                if (entry.EntryType == item.MonsterEntryType || entry.EntryType == MonsterEntryType.ALL)
                {
                    foreach (var levelData in item.LevelList)
                    {
                        if (levelData.Level >= minLv && levelData.Level <= maxLv)
                        {
                            RandomMonsterEntryList.Add(levelData);
                        }
                    }
                }
            }
            
            int index =  Random.Range(0, RandomMonsterEntryList.Count);
            data = RandomMonsterEntryList[index];
            return data;
        }
        
        
        /// <summary>
        /// 设置是否为引导关卡
        /// </summary>
        public void SetIsStageGuide(bool isGuide)
        {
            _isStageGuide = isGuide;
        }
        
        /// <summary>
        /// 初始化第一天刷怪
        /// </summary>
        public void InitFirstDay()
        {
            EndDay();
        }

        


        /**
         * 初始化出战单位
         */
        public void InitCardTeams()
        {
            // 新局开战前清空规则缓存（须在奥数卡 LearnSkillRule 生效之前执行，避免被 BattleSceneLifecycle 二次清空）。
            AttributeManager.ClearBattleSkillRules();

            _skillConfigManager.InitBuffTemplateLibrary();  //初始化buff配置
            var currentTeam = Team.GetTeamByTeamId(Talent.GetUseTalent().TalentId);
            var cardsPosition = currentTeam?.CardsPosition;

            _spawnedTeamSoldierCount = 0;
            // 只初始化 1 个英雄，其余通过城市升级（IsAddHero）逐步添加。
            // 首个英雄走初始化流程，不走“新增英雄补技能/补被动”逻辑，避免被动重复叠加。
            SpawnTeamSoldiers(cardsPosition, 1, false, InitializeInitialTeamSkills);
            
            // ///将被动卡塞入
            // for (int i = 0; i < Global.SoldierTeams.Count; i++)
            // {
            //     SpecialEffectsConfig specialEffectsConfig = GameServiceManager.Get<SoldierService>().GetSoldierSkillConfig(Global.SoldierTeams[i].CardId,
            //         SoldierSkillSortType.PassiveSkills);
            //     bool hasPassive = Global.SoldierSkillTeams
            //         .Exists(x => x.CardId == specialEffectsConfig.Id);
            //     if (!hasPassive)
            //     {
            //         //模拟加被动 临时的
            //         var cardCfg = DataConfigManager.Tables.TbCardBaseConfig.GetOrDefault(specialEffectsConfig.Id);
            //         var cardInfo = Global.GetCardById(specialEffectsConfig.Id);
            //         if (cardInfo == null)
            //         {
            //             LogTool.E($"找不到被动卡 {specialEffectsConfig.Id}");
            //             continue;
            //         }
            //         var cardGroup = new BattleCardGroup(specialEffectsConfig.Id, cardInfo.Lv, cardCfg.Type, 0)
            //         {
            //             IsUnlock =
            //             {
            //                 Value = (cardCfg.UnLockLevel == 0)
            //             }
            //         };
            //         Global.SoldierSkillTeams.Add(cardGroup);
            //         cardGroup.ResetData();
            //     }
            //     
            // }
            // bool hasPassive = Global.SoldierSkillTeams
            //     .Exists(x => x.CardId == 2030004);
            // if (!hasPassive)
            // {
            //     //模拟加被动 临时的
            //     var cardCfg = DataConfigManager.Tables.TbCardBaseConfig.GetOrDefault(2030004);
            //     var cardInfo = Global.GetCardById(2030004);
            //     var cardGroup = new BattleCardGroup(2030004, cardInfo.Lv, cardCfg.Type, 0)
            //     {
            //         IsUnlock =
            //         {
            //             Value = (cardCfg.UnLockLevel == 0)
            //         }
            //     };
            //     Global.SoldierSkillTeams.Add(cardGroup);
            // }


            void InitializeInitialTeamSkills()
            {
                // 装备 Module 类属性（含 LearnSkillRule）与奥数卡同一时机走 AttributeManager 生效。
                ApplyEquipModuleAttributes();

                // 把首次技能/Buff 静态构造挪到加载阶段，避免命中帧 Mono.JIT / cctor 尖刺
                WarmBattleStaticConstructors();
                PreloadCommonBattleEffects();

                // 遍历添加出战技能
                foreach (var v in Global.SoldierSkillTeams)
                {
                    if (v.CardId == 0 || v.IsUnlock.Value == false) continue;

                    var specialCfg = DataConfigManager.Tables.TbSpecialEffectsConfig.GetOrDefault(v.CardId);
                    switch (v.CardType)
                    {
                        case CardType.Skill:
                        case CardType.SkillPassive:
                            AddSkillCard(v.CardId, specialCfg.ActsOnCard, v.CardLevel);
                            SetSkillEffect(specialCfg);
                            break;
                        
                        case CardType.GeneralSkill:
                            GeneralSkillManager.Instance.AddGeneralSkill(v.CardId, v.CardLevel);
                            break;
                    }
                    
                    // switch (specialCfg.SpecialEffectType)
                    // {
                    //     case SpecialEffectType.Pray:
                    //         PraySkill.OnEnable = true;
                    //         break;
                    // }
                }
                
                // 初始化被动技能（当前已上场英雄）
                foreach (var soldierCard in villageGameCards)
                {
                    if (soldierCard == null) continue;
                    InitializePassiveSkillsForSoldier(soldierCard);
                }
                
                // 应用被动技能属性（被动技能对自身普通攻击生效）
                GeneralSkillManager.Instance.ApplyPassiveSkillAttributes();

                // 装备/入局阵容规则在 ApplyEquipModuleAttributes 时可能早于技能挂载；
                // 技能向 ModifyAttr 与月影祝福叠加需在技能初始化完成后补同步。
                ApplyPostSkillInitBattleRulesToVillageSoldiers();

                // 武器 ExtraAttributes：战斗币消耗 / 每秒 ComputerGroup 监听
                BattleRuleBattleEconomySupport.StartBattleWatchers();
                
                // 所有流派初始层数
                var addLayers = (int)ModuleAttributes.GetFinal(AttributeType.AllSchoolInitialLayers);
                if (addLayers > 0)
                {
                    SchoolManager.Instance.AddAllSchoolLayers(addLayers);
                }

            }
        }

        /// <summary>
        /// 开局应用装备/武器 Module 类属性（含特殊规则），与奥数卡共用 AttributeManager 链路。
        /// 须在 ClearBattleSkillRules 之后、奥数卡 AddGeneralSkill 之前调用。
        /// </summary>
        private void ApplyEquipModuleAttributes()
        {
            var moduleEntries = Equip.GetModuleAttributeEntries();
            ApplyModuleAttributeEntries(moduleEntries, AttributeSourceType.Equip, "装备");

            var weaponModuleEntries = GameServiceManager.Get<WeaponService>().GetModuleAttributeEntries();
            ApplyModuleAttributeEntries(weaponModuleEntries, AttributeSourceType.SpeakerSkin, "轮语者武器");

            var skinModuleEntries = GameServiceManager.Get<SpeakerService>().GetModuleAttributeEntries();
            ApplyModuleAttributeEntries(skinModuleEntries, AttributeSourceType.SpeakerSkin, "轮语者皮肤");

            ApplyEquipBattleGoldBonus();
        }

        private void ApplyModuleAttributeEntries(
            List<EquipModuleAttributeEntry> moduleEntries,
            AttributeSourceType sourceType,
            string sourceLabel)
        {
            if (moduleEntries.Count == 0)
            {
                return;
            }

            for (int i = 0; i < moduleEntries.Count; i++)
            {
                var entry = moduleEntries[i];
                var entrySourceType = entry.SourceType;
                if (entrySourceType == AttributeSourceType.Base)
                {
                    entrySourceType = sourceType;
                }

                var attrModifier = new AttributeModifier
                {
                    SourceKey = entry.StackSourceKey,
                    SourceType = entrySourceType,
                };
                AttributeManager.SetAttributesLogic(
                    attrModifier,
                    new List<AttributeData> { entry.Attribute },
                    true);
            }

            LogTool.Log($"[GamePlayManager] {sourceLabel} Module 属性生效: count={moduleEntries.Count}");
        }

        /// <summary>
        /// 装备特效「战斗货币初始数量修正」(125)：在 Module 属性写入后叠加至局内金币。
        /// </summary>
        private void ApplyEquipBattleGoldBonus()
        {
            var bonusGold = (int)ModuleAttributes.GetFinal(AttributeType.BattleGoldInitialQuantity);
            if (bonusGold <= 0)
            {
                return;
            }

            Global.GameGold.Value += bonusGold;
            LogTool.Log(
                $"[GamePlayManager] 装备战斗货币初始修正: +{bonusGold}, 当前金币={Global.GameGold.Value}");
        }

        /// <summary>
        /// 按编队顺序生成指定数量英雄（忽略无效/未解锁卡）。
        /// </summary>
        private int SpawnTeamSoldiers(List<int> cardsPosition, int spawnCount, bool initializeAsNewSoldier = true, Action onAllSpawnCompleted = null)
        {
            if (spawnCount <= 0) return 0;

            // 出场顺序按 CardsPosition 决定；缺失映射时按 CardSlotIndex 兜底。
            var soldierList = Global.SoldierTeams
                .OrderBy(v => GetSoldierSpawnOrderByCardsPosition(v.CardSlotIndex, cardsPosition))
                .ThenBy(v => v.CardSlotIndex)
                .ToList();
            if (_spawnedTeamSoldierCount >= soldierList.Count) return 0;

            var spawnedCount = 0;
            var pendingSpawnVisualCount = 0;
            for (int index = _spawnedTeamSoldierCount; index < soldierList.Count && spawnedCount < spawnCount; index++)
            {
                var v = soldierList[index];

                if (v.CardType != CardType.Soldier || v.CardId == 0 || v.IsUnlock.Value == false)
                {
                    _spawnedTeamSoldierCount = index + 1;
                    continue;
                }

                int posIndex = ResolveSoldierSpawnPosition(cardsPosition, v.CardSlotIndex);
                if (posIndex < 0)
                {
                    // 当前位置暂时不可用时不消费该英雄，等待下次升级再尝试。
                    LogTool.LogWarning($"[GamePlayManager] 无可用站位，暂不生成: cardId={v.CardId}, slot={v.CardSlotIndex}");
                    break;
                }

                EffectManager.Instance.PlayEffect(GlobalConst.BurnEffect, friendlyPositions[posIndex].position);
                if (Global.CityPopulation.Value >= Global.MaxCityPopulation.Value)
                {
                    // Resolve 已预锁站位，取消生成时必须回滚，避免空锁占坑
                    if (friendlyPositionsLocked != null
                        && posIndex >= 0
                        && posIndex < friendlyPositionsLocked.Length)
                    {
                        friendlyPositionsLocked[posIndex] = false;
                    }
                    LogTool.LogWarning($"[GamePlayManager] 人口已满，取消生成: cardId={v.CardId}, slot={v.CardSlotIndex}");
                    break;
                }
                
                spawnedCount++;
                pendingSpawnVisualCount++;
                _spawnedTeamSoldierCount = index + 1;
                ActionKit.Delay(0.1f, () =>
                {
                    var soldier = AddSoldierCard(v.CardId, v.CardLevel, posIndex);
                    if (soldier == null)
                    {
                        if (friendlyPositionsLocked != null
                            && posIndex >= 0
                            && posIndex < friendlyPositionsLocked.Length)
                        {
                            friendlyPositionsLocked[posIndex] = false;
                        }
                        pendingSpawnVisualCount--;
                        if (pendingSpawnVisualCount <= 0)
                        {
                            onAllSpawnCompleted?.Invoke();
                        }
                        return;
                    }

                    var targetScale = soldier.transform.localScale;
                    soldier.transform.localScale = Vector3.zero;
                    soldier.transform.DOScale(targetScale, 0.1f).SetEase(Ease.Linear).OnComplete(() =>
                    {
                        if (initializeAsNewSoldier)
                        {
                            InitializeNewSoldierSkills(soldier);
                        }
                        SchoolManager.Instance?.ApplyExistingSchoolCardsToSoldier(soldier);
                        GeneralSkillManager.Instance?.ApplyExistingGeneralSkillsToSoldier(soldier);
                        GameLand.Default?.InitializeSkillCardReadyForSoldier(soldier.cardId);
                        pendingSpawnVisualCount--;
                        if (pendingSpawnVisualCount <= 0)
                        {
                            onAllSpawnCompleted?.Invoke();
                        }
                    });
                }).Start(this);
            }

            if (pendingSpawnVisualCount <= 0)
            {
                onAllSpawnCompleted?.Invoke();
            }

            return spawnedCount;
        }

        /// <summary>
        /// PVP：用 BattleSession.Opponent（opponentInfo）在对侧 FriendlySlot 生成初始英灵。
        /// </summary>
        public void InitOpponentCardTeams()
        {
            if (!BattleSession.IsPvpAi)
            {
                LogTool.Log("[GamePlayManager] InitOpponentCardTeams 跳过：非 PVP 模式");
                return;
            }

            var opponent = BattleSession.Opponent;
            if (opponent == null)
            {
                LogTool.LogError("[GamePlayManager] PVP 模式但 Opponent 上下文为空，跳过对侧上场");
                return;
            }

            if (opponent.SoldierTeams.Count == 0)
            {
                LogTool.LogError("[GamePlayManager] opponentInfo 无士兵，跳过对侧上场");
                return;
            }

            // 已上场则不重复刷
            if (opponentGameCards != null && opponentGameCards.Count > 0)
            {
                LogTool.Log($"[GamePlayManager] 对侧已有英灵 count={opponentGameCards.Count}，跳过重复上场");
                return;
            }

            InitOpponentFriendlyPositions();
            if (opponentFriendlyPositions == null || opponentFriendlyPositions.Length == 0)
            {
                return;
            }

            // PVP 滑动镜头：重置到玩家侧，并按 GameMap pve→pvp 计算相机滑动上限
            ResetPvpDrag();
            CollectPvpDragCamera();

            _spawnedOpponentTeamSoldierCount = 0;
            // PVP 战斗计时起点：对侧英灵上场时重置
            _pvpBattleTime = 0f;
            _pvpVictoryChecked = false;
            // 对侧 AI 初始金币与玩家一致（InitialCapitalReserves；携带贪婪预支奥数卡再额外增加）
            int opponentInitialGold = DataConfigManager.Tables.TbGameBattleConst.InitialCapitalReserves;
            if (opponent.HasGreedyAdvanceCard)
            {
                opponentInitialGold += DataConfigManager.Tables.TbGameBattleConst.SpecialArcCardGoldNumOne;
            }
            _opponentAiGold = opponentInitialGold;
            _opponentAiLevel = 1;
            _opponentAiExp = 0;
            _opponentAiChooseBuyCount = 0;
            _opponentAiBuyCooldown = 0f;
            _opponentAiSkillTick = 0f;
            _opponentAiSchoolCards.Clear();
            _opponentAiTeamData = null;
            var spawned = SpawnOpponentTeamSoldiers(opponent.ToTeamBuildResult(), 1);
            LogTool.Log($"[GamePlayManager] 对侧初始上场完成 spawned={spawned} teams={opponent.SoldierTeams.Count}");
        }

        private void InitOpponentFriendlyPositions()
        {
            _opponentFriendlySlotRoot = null;
            // 直接走 GameLand/pvp，禁止经 GetLane 回退到玩家 FriendlySlot
            var landPvp = GameLand.Default.transform.Find("pvp");
            if (landPvp == null)
            {
                LogTool.LogError("[GamePlayManager] 找不到 GameLand/pvp");
                opponentFriendlyPositions = Array.Empty<Transform>();
                opponentFriendlyPositionsLocked = Array.Empty<bool>();
                return;
            }

            if (!landPvp.gameObject.activeSelf)
            {
                landPvp.gameObject.SetActive(true);
            }

            // FriendlySlot 直接挂在 GameLand/pvp 下（场景已移除旧 Canvas 包装节点）
            var slot = landPvp.Find("FriendlySlot");
            if (slot == null)
            {
                slot = FindDeepChild(landPvp, "FriendlySlot");
            }

            if (slot == null)
            {
                LogTool.LogError("[GamePlayManager] 找不到 GameLand/pvp 下的 FriendlySlot");
                opponentFriendlyPositions = Array.Empty<Transform>();
                opponentFriendlyPositionsLocked = Array.Empty<bool>();
                return;
            }

            // 校验未误绑到玩家侧
            if (GameLand.Default.FriendlySlot != null
                && slot == GameLand.Default.FriendlySlot)
            {
                LogTool.LogError("[GamePlayManager] 对侧 FriendlySlot 与玩家侧为同一节点，中止对侧上场");
                opponentFriendlyPositions = Array.Empty<Transform>();
                opponentFriendlyPositionsLocked = Array.Empty<bool>();
                return;
            }

            _opponentFriendlySlotRoot = slot;
            int posNum = slot.childCount;
            opponentFriendlyPositions = new Transform[posNum];
            opponentFriendlyPositionsLocked = new bool[posNum];
            for (int i = 0; i < posNum; i++)
            {
                opponentFriendlyPositions[i] = slot.GetChild(i);
                opponentFriendlyPositionsLocked[i] = false;
            }

            LogTool.Log(
                $"[GamePlayManager] 对侧站位就绪 slot={slot.name} path={GetTransformPath(slot)} count={posNum} active={slot.gameObject.activeInHierarchy}");
        }

        private static string GetTransformPath(Transform t)
        {
            if (t == null)
            {
                return "";
            }

            var path = t.name;
            var cur = t.parent;
            while (cur != null)
            {
                path = cur.name + "/" + path;
                cur = cur.parent;
                if (cur != null && cur.name == "GameLand")
                {
                    path = "GameLand/" + path;
                    break;
                }
            }

            return path;
        }

        private static Transform FindDeepChild(Transform root, string name)
        {
            if (root == null)
            {
                return null;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                if (child.name == name)
                {
                    return child;
                }

                var found = FindDeepChild(child, name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private int SpawnOpponentTeamSoldiers(OpponentBattleTeamBuildResult teamData, int spawnCount)
        {
            if (spawnCount <= 0 || teamData == null || teamData.SoldierTeams.Count == 0)
            {
                return 0;
            }

            if (opponentFriendlyPositions == null || opponentFriendlyPositions.Length == 0)
            {
                LogTool.LogError("[GamePlayManager] 对侧站位为空，无法生成英灵");
                return 0;
            }

            var soldierList = teamData.SoldierTeams
                .OrderBy(v => GetSoldierSpawnOrderByCardsPosition(v.CardSlotIndex, teamData.CardsPosition))
                .ThenBy(v => v.CardSlotIndex)
                .ToList();

            if (_spawnedOpponentTeamSoldierCount >= soldierList.Count)
            {
                return 0;
            }

            var spawnedCount = 0;
            for (int index = _spawnedOpponentTeamSoldierCount;
                 index < soldierList.Count && spawnedCount < spawnCount;
                 index++)
            {
                var v = soldierList[index];
                if (v.CardType != CardType.Soldier || v.CardId == 0)
                {
                    _spawnedOpponentTeamSoldierCount = index + 1;
                    continue;
                }

                int posIndex = ResolveOpponentSoldierSpawnPosition(teamData.CardsPosition, v.CardSlotIndex);
                if (posIndex < 0)
                {
                    LogTool.LogWarning($"[GamePlayManager] 对侧无可用站位: cardId={v.CardId}");
                    break;
                }

                spawnedCount++;
                _spawnedOpponentTeamSoldierCount = index + 1;
                var cardId = v.CardId;
                var cardLevel = v.CardLevel;
                var spawnPosIndex = posIndex;

                // 同步生成：加载期 Time.timeScale=0 会导致 ActionKit.Delay 永不触发
                var soldier = AddSoldierCard(cardId, cardLevel, spawnPosIndex, 0, BattleSide.Opponent);
                if (soldier == null)
                {
                    if (spawnPosIndex >= 0
                        && spawnPosIndex < opponentFriendlyPositionsLocked.Length)
                    {
                        opponentFriendlyPositionsLocked[spawnPosIndex] = false;
                    }

                    LogTool.LogError($"[GamePlayManager] 对侧 AddSoldierCard 失败 cardId={cardId} pos={spawnPosIndex}");
                    continue;
                }

                var targetScale = soldier.transform.localScale;
                soldier.transform.localScale = Vector3.zero;
                soldier.transform.DOScale(targetScale, 0.1f)
                    .SetEase(Ease.Linear)
                    .SetUpdate(true);
            }

            return spawnedCount;
        }

        private int ResolveOpponentSoldierSpawnPosition(List<int> cardsPosition, int soldierSlotIndex)
        {
            var occupied = GetOccupiedOpponentPositionIndices();
            if (opponentFriendlyPositionsLocked == null
                || opponentFriendlyPositionsLocked.Length != opponentFriendlyPositions.Length)
            {
                opponentFriendlyPositionsLocked = new bool[opponentFriendlyPositions.Length];
            }

            for (int i = 0; i < opponentFriendlyPositionsLocked.Length; i++)
            {
                opponentFriendlyPositionsLocked[i] = occupied.Contains(i);
            }

            if (cardsPosition != null && cardsPosition.Count > 0)
            {
                var mappedIndex = cardsPosition.IndexOf(soldierSlotIndex);
                if (mappedIndex >= 0
                    && mappedIndex < opponentFriendlyPositions.Length
                    && !occupied.Contains(mappedIndex))
                {
                    opponentFriendlyPositionsLocked[mappedIndex] = true;
                    return mappedIndex;
                }
            }

            for (int i = 0; i < opponentFriendlyPositions.Length; i++)
            {
                if (occupied.Contains(i))
                {
                    continue;
                }

                opponentFriendlyPositionsLocked[i] = true;
                return i;
            }

            return -1;
        }

        private HashSet<int> GetOccupiedOpponentPositionIndices()
        {
            var occupied = new HashSet<int>();
            for (int i = 0; i < opponentGameCards.Count; i++)
            {
                var soldier = opponentGameCards[i];
                if (soldier == null)
                {
                    continue;
                }

                if (soldier.posIndex < 0 || soldier.posIndex >= opponentFriendlyPositions.Length)
                {
                    continue;
                }

                occupied.Add(soldier.posIndex);
            }

            return occupied;
        }

        private int FindClosestOpponentPosition()
        {
            if (opponentFriendlyPositions == null)
            {
                return -1;
            }

            for (int i = 0; i < opponentFriendlyPositions.Length; i++)
            {
                if (opponentFriendlyPositionsLocked != null
                    && i < opponentFriendlyPositionsLocked.Length
                    && opponentFriendlyPositionsLocked[i])
                {
                    continue;
                }

                if (opponentFriendlyPositionsLocked != null && i < opponentFriendlyPositionsLocked.Length)
                {
                    opponentFriendlyPositionsLocked[i] = true;
                }

                return i;
            }

            return -1;
        }

        /// <summary>
        /// 计算英雄出场顺序：优先按 CardsPosition 的槽位顺序，找不到时回退到槽位索引本身。
        /// </summary>
        private int GetSoldierSpawnOrderByCardsPosition(int soldierSlotIndex, List<int> cardsPosition)
        {
            if (cardsPosition == null || cardsPosition.Count == 0)
            {
                return soldierSlotIndex;
            }

            var order = cardsPosition.IndexOf(soldierSlotIndex);
            if (order >= 0)
            {
                return order;
            }

            return 1000 + soldierSlotIndex;
        }

        /// <summary>
        /// 新增英雄后补齐其可用技能，确保技能卡与CD状态可正常初始化。
        /// </summary>
        private void InitializeNewSoldierSkills(SoldierCard soldier)
        {
            if (soldier == null) return;
            var soldierCardId = soldier.cardId;

            foreach (var skillGroup in Global.SoldierSkillTeams)
            {
                if (skillGroup.CardId == 0 || skillGroup.IsUnlock.Value == false) continue;

                var specialCfg = DataConfigManager.Tables.TbSpecialEffectsConfig.GetOrDefault(skillGroup.CardId);
                if (specialCfg == null) continue;

                switch (skillGroup.CardType)
                {
                    case CardType.Skill:
                    case CardType.SkillPassive:
                        if (specialCfg.ActsOnCard == 0 || specialCfg.ActsOnCard == soldierCardId)
                        {
                            AddSkillCard(skillGroup.CardId, soldierCardId, skillGroup.CardLevel);
                        }
                        break;
                }
            }

            // 新增英雄后补做一次被动技能初始化（原 InitCardTeams 逻辑），保证新英雄也能正确挂载被动增益。
            InitializePassiveSkillsForSoldier(soldier);

            // 新增英雄后重算一次被动技能属性，使新英雄也能吃到全局/职业被动加成。
            GeneralSkillManager.Instance.ApplyPassiveSkillAttributes();

            // 技能初始化完成后再同步本局已学规则（含装备技能向属性、月影祝福叠加）。
            var ruleService = BattleRuleDamageSupport.GetRuleService();
            if (ruleService != null)
            {
                ruleService.ApplyEquipLearnedRulesToSoldier(soldier);
                ruleService.ApplyLearnedRulesToNewSoldier(soldier);
                ruleService.ApplyRosterCompositionRulesToSoldier(soldier);
            }
        }

        /// <summary>
        /// 技能初始化完成后，补同步装备已学规则与入局阵容规则。
        /// </summary>
        private void ApplyPostSkillInitBattleRulesToVillageSoldiers()
        {
            var ruleService = BattleRuleDamageSupport.GetRuleService();
            if (ruleService == null)
            {
                return;
            }

            for (int i = 0; i < villageGameCards.Count; i++)
            {
                var soldierCard = villageGameCards[i];
                if (soldierCard == null)
                {
                    continue;
                }

                ruleService.ApplyEquipLearnedRulesToSoldier(soldierCard);
                ruleService.ApplyRosterCompositionRulesToSoldier(soldierCard);
            }
        }

        /// <summary>
        /// 初始化指定英雄的被动技能加成（被动技能对自身普通攻击生效）。
        /// </summary>
        private void InitializePassiveSkillsForSoldier(SoldierCard soldierCard)
        {
            if (soldierCard == null) return;

            RebuildSoldierSkillAttributesWithPassives(soldierCard);
        }

        /// <summary>
        /// 学习被动技能并立即刷新对应英雄技能属性。
        /// </summary>
        public void LearnPassiveSkillCard(int passiveSkillId, int passiveLevel = 1, int targetSoldierId = 0)
        {
            var specialCfg = DataConfigManager.Tables.TbSpecialEffectsConfig.GetOrDefault(passiveSkillId);
            if (!IsPassiveSkillConfig(specialCfg))
            {
                LogTool.LogWarning($"[GamePlayManager] LearnPassiveSkillCard 跳过：skillId={passiveSkillId} 不是被动技能");
                return;
            }

            var actualTargetSoldierId = targetSoldierId != 0 ? targetSoldierId : specialCfg.ActsOnCard;

            var skillGroup = Global.SoldierSkillTeams.FirstOrDefault(v => v.CardId == passiveSkillId);
            if (skillGroup != null)
            {
                skillGroup.IsUnlock.Value = true;
                if (passiveLevel > 0)
                {
                    skillGroup.CardLevel = passiveLevel;
                }
            }

            if (targetSoldierId != 0)
            {
                LearnPassiveSkillDirectToSoldier(passiveSkillId, passiveLevel, actualTargetSoldierId);
                return;
            }

            AddSkillCard(passiveSkillId, actualTargetSoldierId, passiveLevel);
            RefreshPassiveSkillEffectsForSkill(passiveSkillId, actualTargetSoldierId);
        }

        /// <summary>
        /// 遗忘被动技能并立即回滚对应英雄技能属性。
        /// </summary>
        public void ForgetPassiveSkillCard(int passiveSkillId, int targetSoldierId = 0)
        {
            var specialCfg = DataConfigManager.Tables.TbSpecialEffectsConfig.GetOrDefault(passiveSkillId);
            if (!IsPassiveSkillConfig(specialCfg))
            {
                LogTool.LogWarning($"[GamePlayManager] ForgetPassiveSkillCard 跳过：skillId={passiveSkillId} 不是被动技能");
                return;
            }

            var actualTargetSoldierId = targetSoldierId != 0 ? targetSoldierId : specialCfg.ActsOnCard;

            var skillGroup = Global.SoldierSkillTeams.FirstOrDefault(v => v.CardId == passiveSkillId);
            if (skillGroup != null)
            {
                skillGroup.IsUnlock.Value = false;
            }

            if (targetSoldierId != 0)
            {
                ForgetPassiveSkillDirectFromSoldier(passiveSkillId, actualTargetSoldierId);
                return;
            }

            RefreshPassiveSkillEffectsForSkill(passiveSkillId, actualTargetSoldierId);
        }

        private void LearnPassiveSkillDirectToSoldier(int passiveSkillId, int passiveLevel, int targetSoldierId)
        {
            var specialCfg = DataConfigManager.Tables.TbSpecialEffectsConfig.GetOrDefault(passiveSkillId);
            if (!IsPassiveSkillConfig(specialCfg) || targetSoldierId == 0)
            {
                return;
            }

            var levelCfg = ResolvePassiveLevelConfig(specialCfg, passiveLevel);
            if (levelCfg == null)
            {
                LogTool.LogWarning($"[GamePlayManager] LearnPassiveSkillCard 失败：未找到被动等级配置 skillId={passiveSkillId}, level={passiveLevel}");
                return;
            }

            foreach (var soldierCard in villageGameCards)
            {
                if (soldierCard == null || soldierCard.cardId != targetSoldierId) continue;
                RemovePassiveModifiersFromSoldierSkills(soldierCard, specialCfg, passiveSkillId);
                ApplyPassiveLearnSkillRules(soldierCard, levelCfg, passiveSkillId, targetSoldierId);
                ApplyPassiveModifiersToSoldierSkills(soldierCard, specialCfg, levelCfg, passiveSkillId);
            }
        }

        private void ApplyPassiveLearnSkillRules(
            SoldierCard soldierCard,
            SpecialLevelPropertyList levelCfg,
            int passiveSkillId,
            int scopeHeroCardId)
        {
            if (soldierCard == null || levelCfg == null) return;

            string sourceKey = $"PassiveSkill_{passiveSkillId}";
            foreach (var attr in levelCfg.Attributes)
            {
                if (attr.Type != AttributeType.LearnSkillRule)
                {
                    continue;
                }

                var ruleId = Mathf.RoundToInt(attr.Value);
                if (ruleId <= 0)
                {
                    ruleId = Mathf.RoundToInt(attr.EffectParam);
                }

                if (ruleId <= 0)
                {
                    LogTool.LogWarning(
                        $"[GamePlayManager] 被动 LearnSkillRule 无效: passive={passiveSkillId}, hero={scopeHeroCardId}");
                    continue;
                }

                AttributeManager.AddBattleSkillRule(
                    ruleId,
                    sourceKey,
                    AttributeSourceType.GeneralSkill,
                    scopeHeroCardId);
            }
        }

        private void ForgetPassiveSkillDirectFromSoldier(int passiveSkillId, int targetSoldierId)
        {
            var specialCfg = DataConfigManager.Tables.TbSpecialEffectsConfig.GetOrDefault(passiveSkillId);
            if (!IsPassiveSkillConfig(specialCfg) || targetSoldierId == 0)
            {
                return;
            }

            foreach (var soldierCard in villageGameCards)
            {
                if (soldierCard == null || soldierCard.cardId != targetSoldierId) continue;
                RemovePassiveModifiersFromSoldierSkills(soldierCard, specialCfg, passiveSkillId);
            }
        }

        private SpecialLevelPropertyList ResolvePassiveLevelConfig(SpecialEffectsConfig specialCfg, int passiveLevel)
        {
            if (specialCfg?.LevelPropertyList == null || specialCfg.LevelPropertyList.Count == 0)
            {
                return null;
            }

            if (passiveLevel > 0)
            {
                var match = specialCfg.LevelPropertyList.FirstOrDefault(v => v.Level == passiveLevel);
                if (match != null)
                {
                    return match;
                }
            }

            var index = Mathf.Clamp(passiveLevel - 1, 0, specialCfg.LevelPropertyList.Count - 1);
            return specialCfg.LevelPropertyList[index];
        }

        private void ApplyPassiveModifiersToSoldierSkills(SoldierCard soldierCard, SpecialEffectsConfig passiveCfg, SpecialLevelPropertyList levelCfg, int passiveSkillId)
        {
            if (soldierCard == null || passiveCfg == null || levelCfg == null) return;

            string sourceKey = $"PassiveSkill_{passiveSkillId}";
            foreach (var skillId in passiveCfg.PassiveActsOnCard)
            {
                var targetSkill = soldierCard.GetSkillById(skillId);
                if (targetSkill == null) continue;

                foreach (var attr in levelCfg.Attributes)
                {
                    var attrCfg = DataConfigManager.Tables.TbAttributeConfig.GetOrDefault(attr.Type);
                    if (attrCfg == null || attrCfg.Category != AttributeCategoryType.Skill) continue;

                    var modifier = new AttributeModifier
                    {
                        SourceType = AttributeSourceType.GeneralSkill,
                        SourceKey = sourceKey,
                        FlatAdd = attr.ValueType == ValueType.Number ? attr.Value : 0f,
                        PercentAdd = attr.ValueType == ValueType.Percentage ? attr.Value : 0f,
                        EffectParam = attr.EffectParam,
                        ProbabilityTrigger = attr.ProbabilityTrigger
                    };
                    targetSkill.Attributes.GetAttr(attr.Type).AddModifier(modifier, false);
                }
            }
        }

        private void RemovePassiveModifiersFromSoldierSkills(SoldierCard soldierCard, SpecialEffectsConfig passiveCfg, int passiveSkillId)
        {
            if (soldierCard == null || passiveCfg == null) return;

            string sourceKey = $"PassiveSkill_{passiveSkillId}";
            foreach (var skillId in passiveCfg.PassiveActsOnCard)
            {
                var targetSkill = soldierCard.GetSkillById(skillId);
                if (targetSkill == null) continue;

                foreach (var attrPair in targetSkill.Attributes.GetAll())
                {
                    attrPair.Value.RemoveModifier(AttributeSourceType.GeneralSkill, sourceKey);
                }
            }
        }

        /// <summary>
        /// 刷新单个被动技能对应英雄的技能属性。
        /// </summary>
        private void RefreshPassiveSkillEffectsForSkill(int passiveSkillId, int fallbackSoldierId = 0)
        {
            var specialCfg = DataConfigManager.Tables.TbSpecialEffectsConfig.GetOrDefault(passiveSkillId);
            if (!IsPassiveSkillConfig(specialCfg)) return;

            var targetSoldierId = specialCfg.ActsOnCard != 0 ? specialCfg.ActsOnCard : fallbackSoldierId;
            foreach (var soldierCard in villageGameCards)
            {
                if (soldierCard == null) continue;
                if (targetSoldierId != 0 && soldierCard.cardId != targetSoldierId) continue;
                RebuildSoldierSkillAttributesWithPassives(soldierCard);
            }
        }

        /// <summary>
        /// 重建英雄技能属性（先还原技能基础属性，再按当前已解锁被动重新叠加）。
        /// </summary>
        private void RebuildSoldierSkillAttributesWithPassives(SoldierCard soldierCard)
        {
            if (soldierCard == null) return;
            ResetSoldierSkillBaseAttributes(soldierCard);

            foreach (var skillGroup in Global.SoldierSkillTeams)
            {
                if (skillGroup.CardId == 0 || skillGroup.IsUnlock.Value == false) continue;

                var specialCfg = DataConfigManager.Tables.TbSpecialEffectsConfig.GetOrDefault(skillGroup.CardId);
                if (!IsPassiveSkillConfig(specialCfg)) continue;
                if (specialCfg.ActsOnCard != 0 && specialCfg.ActsOnCard != soldierCard.cardId) continue;

                var passiveSkillCard = soldierCard.GetSkillById(specialCfg.Id);
                if (passiveSkillCard == null)
                {
                    LogTool.LogWarning($"[GamePlayManager] 被动技能未找到，跳过属性重建: soldier={soldierCard.cardId}, passiveSkill={specialCfg.Id}");
                    continue;
                }

                foreach (var cardId in specialCfg.PassiveActsOnCard)
                {
                    var skillCard = soldierCard.GetSkillById(cardId);
                    if (skillCard == null) continue;
                    skillCard.Attributes.AddRangeBaseValue(passiveSkillCard.Attributes);
                }
            }
        }

        /// <summary>
        /// 重置英雄所有技能的基础属性到配置值，清理旧的被动叠加痕迹。
        /// </summary>
        private void ResetSoldierSkillBaseAttributes(SoldierCard soldierCard)
        {
            foreach (var skill in soldierCard.GetSkillAll().Values)
            {
                if (skill == null || skill.Config == null || skill.Attributes == null) continue;

                foreach (var attr in skill.Attributes.GetAll().Values)
                {
                    attr.Clear();
                }

                var levelIndex = skill.Level - 1;
                if (levelIndex < 0 || levelIndex >= skill.Config.LevelPropertyList.Count)
                {
                    LogTool.LogWarning($"[GamePlayManager] 技能等级越界，跳过属性重置: soldier={soldierCard.cardId}, skill={skill.Id}, level={skill.Level}");
                    continue;
                }

                var levelCfg = skill.Config.LevelPropertyList[levelIndex];
                foreach (var attr in levelCfg.Attributes)
                {
                    var attrCfg = DataConfigManager.Tables.TbAttributeConfig.GetOrDefault(attr.Type);
                    if (attrCfg == null || attrCfg.Category != AttributeCategoryType.Skill) continue;
                    skill.Attributes.GetAttr(attr.Type).SetBaseValue(attr.Value, true);
                }
            }
        }

        private static bool IsPassiveSkillConfig(SpecialEffectsConfig specialCfg)
        {
            return specialCfg != null && specialCfg.PassiveSkill == 1;
        }

        /// <summary>
        /// 解析英雄出生位置：优先使用编队位置；失败时按固定顺序找空位。
        /// </summary>
        private int ResolveSoldierSpawnPosition(List<int> cardsPosition, int soldierSlotIndex)
        {
            var occupied = GetOccupiedFriendlyPositionIndices();
            SyncFriendlyPositionLocksByOccupied(occupied);

            if (cardsPosition != null && cardsPosition.Count > 0)
            {
                var mappedIndex = cardsPosition.IndexOf(soldierSlotIndex);
                if (mappedIndex >= 0 && mappedIndex < friendlyPositions.Length && !occupied.Contains(mappedIndex))
                {
                    friendlyPositionsLocked[mappedIndex] = true;
                    return mappedIndex;
                }
            }

            for (int i = 0; i < friendlyPositions.Length; i++)
            {
                if (occupied.Contains(i)) continue;

                friendlyPositionsLocked[i] = true;
                return i;
            }

            return -1;
        }

        /// <summary>
        /// 基于场上英灵的 posIndex 计算已占用站位。
        /// 死亡隐藏单位仍占编队槽，避免升级补人叠到死亡位。
        /// </summary>
        private HashSet<int> GetOccupiedFriendlyPositionIndices()
        {
            var occupied = new HashSet<int>();
            foreach (var soldier in villageGameCards)
            {
                if (soldier == null) continue;
                if (soldier.posIndex < 0 || soldier.posIndex >= friendlyPositions.Length) continue;
                occupied.Add(soldier.posIndex);
            }

            return occupied;
        }

        /// <summary>
        /// 将 friendlyPositionsLocked 与当前真实占位同步，避免历史脏锁导致误判。
        /// </summary>
        private void SyncFriendlyPositionLocksByOccupied(HashSet<int> occupied)
        {
            if (friendlyPositionsLocked == null || friendlyPositions == null) return;
            if (friendlyPositionsLocked.Length != friendlyPositions.Length)
            {
                friendlyPositionsLocked = new bool[friendlyPositions.Length];
            }

            for (int i = 0; i < friendlyPositionsLocked.Length; i++)
            {
                friendlyPositionsLocked[i] = occupied.Contains(i);
            }
        }

        /// <summary>
        /// 城市升级触发：尝试从编队新增 1 个英雄。
        /// </summary>
        public bool TryAddHeroFromTeamByLevelUp()
        {
            var currentTeam = Team.GetTeamByTeamId(Talent.GetUseTalent().TalentId);
            var cardsPosition = currentTeam?.CardsPosition;
            var spawned = SpawnTeamSoldiers(cardsPosition, 1, true, () =>
            {
                DetectAdjacentSoldier();
                TriggerGuide(GuideType.NewSoldierDebut);
            });
            if (spawned > 0)
            {
                return true;
            }

            return false;
        }

        bool IsEffect;
        bool IsEffectover;

        private void Update()
        {
            // 增加时间，如果游戏未暂停且当前天数在进行中
            if (!IsPaused())
            {
                var delta = Time.deltaTime * Global.SpeedMultiplier.Value;
                gameTime += delta;
                BattleRuleBattleEconomySupport.Tick(delta);

                // PVP：累计战斗时间并做胜负判定（英灵全灭 / 5 分钟限时）
                if (BattleSession.IsPvpAi)
                {
                    _pvpBattleTime += delta;
                    TickOpponentAi(delta);

                    // PVP：对侧英灵自动释放技能（每 0.5s 检查一轮）
                    _opponentAiSkillTick -= delta;
                    if (_opponentAiSkillTick <= 0f)
                    {
                        _opponentAiSkillTick = 0.5f;
                        TickOpponentAiSkillRelease();
                    }

                    TryCheckPvpEndByTeam();
                    if (_pvpBattleTime >= PvpBattleTimeLimit)
                    {
                        TrySettlePvpByRemainingHp();
                    }
                }
                CheckForSpawn();
                // CheckForRecoverGameMana();

                // 一人成军（战斗开始后每30秒过后自动生成一个相同镜像的英灵）
                if (OneManArmyCreateMirrorTime > 0f)
                {
                    _createMirrorTime += delta;
                    if (_createMirrorTime >= OneManArmyCreateMirrorTime)
                    {
                        _createMirrorTime = 0f;
                        var oneManArmyVal = ModuleAttributes.GetFinal(AttributeType.OneManArmy);
                        AttributeManager.SetAttributeLogic(new AttributeModifier
                        {
                            SourceKey = "OneManArmyCreateMirrorTime",
                            SourceType = AttributeSourceType.SchoolCard,
                        }, new AttributeData()
                        {
                            Type = AttributeType.SoldierMirror,
                            ValueType = ValueType.Number,
                            Value = oneManArmyVal
                        });
                    }
                }
            }

            // PVP：拖拽空白处滑动镜头查看对侧（AI 方）信息
            if (BattleSession.IsPvpAi && !_gameOver && !_pvpVictoryChecked)
            {
                UpdatePvpCameraDrag();
            }

            // 调试模式
            if (Global.Debug.Value == 1)
            {
                // 监听按键
                if (Input.GetKeyDown(KeyCode.G))
                {
                    Global.GameGold.Value += 9999;
                }

                if (Input.GetKeyDown(KeyCode.S))
                {
                    Global.SpeedMultiplier.Value++;
                    if (Global.SpeedMultiplier.Value > 5)
                    {
                        Global.SpeedMultiplier.Value = 1;
                    }
                }

                // 全场战士加血
                if (Input.GetKeyDown(KeyCode.H))
                {
                    TraverseVillageSoldier((soldierCard) =>
                    {
                        if (soldierCard.IsDie()) return;
                        soldierCard.SetSoldierMaxHp(1000, true);
                    });
                }

                if (Input.GetKeyDown(KeyCode.P))
                {
                    UnlockSoldierDetailPanel = true; 
                    battleLog = true;
                }
                
                if (Input.GetKeyDown(KeyCode.C))
                {
                    var vm = new Game.SchoolChooseCard.SchoolChooseCardViewModel();
                    vm.OnCreate();
                    vm.Initialize(0);
                    UIFramework.UIManager.OpenPanelAsync<SchoolChooseCardPanelNew, Game.SchoolChooseCard.SchoolChooseCardViewModel>(vm, UIFramework.UILevel.PopUI).Forget();
                }
                
                if (Input.GetKeyDown(KeyCode.R))
                {
                    foreach (var skillId in GameLand.Default.skill.Keys)
                    {
                        GameLand.Default.AddSkillCurrentCooldownTime(skillId, -100);
                    }
                }
                
                if (Input.GetKeyDown(KeyCode.J))
                {
                    DropSurpriseChooseCardPanel(new Vector3());
                    // UIKit.OpenPanel<SurpriseChooseCardPanel>().SetData();
                }

                if (Input.GetKeyDown(KeyCode.L))
                {
                    // SchoolManager.Instance.AddSchoolCard(14104);
                    //SchoolManager.Instance.AddSchoolCard(13109);

                    StartCoroutine(OpenToolPanelRoutine(panel => panel.SetSchoolPanel()));
                }

                if (Input.GetKeyDown(KeyCode.T))
                {
                    StartCoroutine(OpenToolPanelRoutine(panel => panel.SetGameScenes()));
                }
                
                if (Input.GetKeyDown(KeyCode.Y))
                {
                    
                }

                if (Input.GetKeyDown(KeyCode.K))
                {
                    AttributeManager.SetAttributeLogic(new AttributeModifier
                    {
                        SourceKey = "",
                        SourceType = AttributeSourceType.ToolPanel,
                    }, new AttributeData()
                    {
                        Type = AttributeType.TestFullScreenHarm,
                        ValueType = ValueType.Number,
                        Value = 1,
                    });
                }
            }

            if (isPaused && !IsEffect)
            {
                // MonsterCard[] monsterCards = GameObject.FindObjectsOfType<MonsterCard>();
                IsEffect = true;
                IsEffectover = false;
            }
            else if (!isPaused && !IsEffectover)
            {
                // MonsterCard[] monsterCards = GameObject.FindObjectsOfType<MonsterCard>();
                IsEffectover = true;
                IsEffect = false;
            }
        }

        // WebGL 不支持同步加载，ToolPanel 统一走异步打开（prefabName 必须显式传，OpenPanelAsync 不会默认取类名）
        private IEnumerator OpenToolPanelRoutine(System.Action<ToolPanel> setup)
        {
            yield return UIKit.OpenPanelAsync<ToolPanel>(UILevel.PopUI, prefabName: typeof(ToolPanel).Name);
            var panel = UIKit.GetPanel<ToolPanel>();
            if (panel != null)
            {
                setup(panel);
            }
        }

        private void LateUpdate()
        {
            BattleScheduler.Instance.ProcessLateUpdate();
        }

        #region 游戏定义事件

        /**
         * 游戏暂停继续
         */
        public void StartGame()
        {
            isPaused = false;
            isDayInProgress = true;
            RestoreTimeScaleAfterPause();
            OnPauseChanged?.Invoke(false); // 触发恢复事件（角色/子系统同步恢复）
            DOTween.PlayAll();
            // if (GameLand.Default?.Autotoggle.isOn == false)
            // {
            //     GameLand.Default._rotateTween.Pause();
            // }

            var children = GameObject.FindObjectsOfType<MonsterCard>(true);
            foreach (var VARIABLE in children)
            {
                VARIABLE.GetComponent<MonsterCard>().Resume();
            }
        }

        public void RestartGame(Action successCallback, Action finallyCallback = null)
        {
            // 结算成功回调
            void SettleCallback()
            {
                ClearBattleLegacySkillState();
                Global.ResetProperties();
                UIFramework.Services.GameServiceManager.GetOrCreate<Game.Team.TeamService>().BuildBattleTeamsForCurrentTalent();// 重新生成一遍队伍数据

                // 战斗开始
                BattleModel.Start(successCallback,
                    (err) => { FusionContinent.Toast.Show($"{err}"); },
                    finallyCallback);
            }

            // 检查是否开启战斗
            if (Global.PlayerBattle.Status != 0)
            {
                // 战斗结算
                BattleModel.Settle(0, SettleCallback);
            }
            else
            {
                // 已经结算
                SettleCallback();
            }
        }

        /**
         * 清除战内游戏数据
         */
        public void ClearBattleLegacySkillState()
        {
            BattleRuleBattleEconomySupport.StopBattleWatchers();
            BattleRuleBattleEconomySupport.ResetBattleState();

            var ruleService = BattleServiceManager.Instance != null
                ? BattleServiceManager.Instance.GetService<BattleRuleService>()
                : null;
            if (ruleService != null)
            {
                ruleService.Reset();
                ruleService.ClearComputerCounters();
            }

            foreach (var skillId in SkillConfigManager.GetLegacyCastOnlySkillIds())
            {
                for (int i = 0; i < villageGameCards.Count; i++)
                {
                    var soldierCard = villageGameCards[i];
                    if (soldierCard == null) continue;

                    var skill = soldierCard.GetSkillById(skillId);
                    if (skill is SkillBase skillBase)
                    {
                        skillBase.StopLegacyRuntime();
                    }
                }
            }

            StopAllBattleTweens();
            StopActiveLegacySceneEffects();
            villageGameCards.Clear();
            opponentGameCards.Clear();
        }

        private void StopAllBattleTweens()
        {
            for (int i = 0; i < villageGameCards.Count; i++)
            {
                var soldierCard = villageGameCards[i];
                if (soldierCard == null) continue;
                DOTween.Kill(soldierCard.gameObject);
            }

            if (MonsterManager.Instance != null)
            {
                var enemies = MonsterManager.Instance.enemies;
                for (int i = 0; i < enemies.Count; i++)
                {
                    var enemy = enemies[i];
                    if (enemy == null) continue;
                    if (enemy.monsterData != null && enemy.monsterData.monsterTweens != null)
                    {
                        enemy.monsterData.monsterTweens.KillAllTweens();
                    }
                    DOTween.Kill(enemy.gameObject);
                }
            }

            if (GameLand.Default != null)
            {
                GameLand.Default.StopAllSkillCooldownTweens();
                DOTween.Kill(GameLand.Default.gameObject);
                if (GameLand.Default.DamageParent != null)
                {
                    DOTween.Kill(GameLand.Default.DamageParent);
                }
            }

            if (DamageUIManager.Instance != null)
            {
                DamageUIManager.Instance.RecycleAllActiveDamageTexts();
            }

            DOTween.Kill(GlobalConst.MonsterTween);
            DOTween.Kill(GlobalConst.BattleTween);
        }

        private static void StopActiveLegacySceneEffects()
        {
            var thunderEffects = UnityEngine.Object.FindObjectsOfType<ThunderFieldFullScreenEffect>(true);
            for (int i = 0; i < thunderEffects.Length; i++)
            {
                thunderEffects[i].StopRuntime();
            }

            var windEffects = UnityEngine.Object.FindObjectsOfType<WindDanceAttackEffect>(true);
            for (int i = 0; i < windEffects.Length; i++)
            {
                windEffects[i].StopRuntime();
            }

            var shadowFormations = UnityEngine.Object.FindObjectsOfType<MagicianShadowFormation>(true);
            for (int i = 0; i < shadowFormations.Length; i++)
            {
                shadowFormations[i].StopRuntime();
            }
        }

        /**
         * 清除战内游戏数据
         */
        private void ClearGame()
        {
            PauseGame();
            _gameOver = true;
            ClearBattleLegacySkillState();

            Global.ResetGemData();
            Global.ResetProperties();
            BattleSession.ResetToPve();
            ResetPvpDrag();
            UIKit.ClosePanel<SchoolChooseCardPanel>();// 关闭三选一
            Destroy(GameMap.Default);
            Destroy(GameLand.Default);
            Destroy(GameLandUI.Default);
            // Destroy(CoinManager.Instance);
            // Destroy(this);
            _monsterGenCfg = null;
            if (GuideManager.Instance)
            {
                Object.Destroy(GuideManager.Instance.gameObject);
            }
        }

        /**
         * 返回主界面
         */
        public void BackHome()
        {
            _gameOver = true;
            
            // 结算成功回调
            Action settleCallback = () =>
            {
                UIRoot.Instance.transform.Find("Audio Source").GetComponent<AudioSource>().Play();
                // Gamebg.SetActive(true);
                ClearGame();
                Time.timeScale = 1f;
                _timeScaleBeforePause = 1f;

                // 切换主界面场景
                SceneLoader.Load("GameStart");
                // SceneManager.LoadScene("GameStart", LoadSceneMode.Single);
            };

            // PVP 未结算（有 match_id）或 PVE 战斗中：先结算再回城
            bool needSettle = BattleSession.IsPvpAi && BattleSession.CurrentMatchId > 0
                              || Global.PlayerBattle != null && Global.PlayerBattle.Status != 0;
            if (needSettle)
            {
                BattleModel.Settle(0, null, true);
            }
            else
            {
                settleCallback();
            }
        }

        // 暂停游戏
        public void PauseGame()
        {
            if (Time.timeScale > TimeScaleEpsilon)
            {
                _timeScaleBeforePause = Time.timeScale;
            }
            isPaused = true;
            OnPauseChanged?.Invoke(true); // 触发暂停事件
            Time.timeScale = 0;
            DOTween.Pause(GlobalConst.MonsterTween);
            DOTween.Pause(GlobalConst.BattleTween);
            var children = GameObject.FindObjectsOfType<MonsterCard>(true);
            foreach (var VARIABLE in children)
            {
                VARIABLE.GetComponent<MonsterCard>().Pause();
            }
        }

        public void ResumeGame()
        {
            isPaused = false;
            OnPauseChanged?.Invoke(false); // 触发恢复事件
            RestoreTimeScaleAfterPause();
            DOTween.Play(GlobalConst.MonsterTween);
            DOTween.Play(GlobalConst.BattleTween);

            var children = GameObject.FindObjectsOfType<MonsterCard>(true);
            foreach (var VARIABLE in children)
            {
                VARIABLE.GetComponent<MonsterCard>().Resume();
            }
        }

        private void RestoreTimeScaleAfterPause()
        {
            Time.timeScale = _timeScaleBeforePause > TimeScaleEpsilon ? _timeScaleBeforePause : 1f;
        }

        /**
         * 游戏结束
         */
        private bool _gameOver;

        /// <summary>PVP 胜负已结算标记（防止结算延迟期间重复触发）。</summary>
        private bool _pvpVictoryChecked;

        /// <summary>PVP 战斗计时（秒），满 5 分钟按剩余血量总和判定胜负。</summary>
        private float _pvpBattleTime;

        /// <summary>PVP 限时（秒）：5 分钟。</summary>
        private const float PvpBattleTimeLimit = 300f;

        /// <summary>PVP 对侧 AI 金币（击杀 + 被动收入，无 UI）。</summary>
        private float _opponentAiGold;


        /// <summary>PVP 对侧 AI 内部等级（购买人物费用递增，对齐 CityLevelConfig.NeedGold）。</summary>
        private int _opponentAiLevel = 1;

        /// <summary>PVP 对侧 AI 经验值（买经验升级，对齐 CityLevelConfig.CityExp）。</summary>
        private int _opponentAiExp;

        /// <summary>PVP 对侧 AI 技能自动释放节流（秒）。</summary>
        private float _opponentAiSkillTick;

        /// <summary>PVP 对侧 AI 已购买三选一次数（费用递增）。</summary>
        private int _opponentAiChooseBuyCount;

        /// <summary>PVP 对侧 AI 购买节流（秒）。</summary>
        private float _opponentAiBuyCooldown;

        /// <summary>PVP 对侧 AI 编队缓存。</summary>
        private OpponentBattleTeamBuildResult _opponentAiTeamData;

        /// <summary>PVP 对侧 AI 已购买的三选一卡（cardId → 星级），用于补到后续上场英灵。</summary>
        private readonly Dictionary<int, int> _opponentAiSchoolCards = new Dictionary<int, int>();

        /// <summary>对侧养成属性聚合（装备/奥数卡/奥数共鸣）缓存。</summary>
        private AttributeAggregation _opponentBattleAttributes;
        private bool _opponentBattleAttributesBuilt;

        /// <summary>PVP 滑动镜头：被移动的相机（GameMap canvas 的 worldCamera，通常为主相机）。</summary>
        private Camera _pvpDragCamera;

        /// <summary>PVP 滑动镜头：相机基准位置（玩家侧视角，收集时记录）。</summary>
        private Vector3 _pvpDragCameraBasePos;

        /// <summary>PVP 滑动镜头：当前相机上移量（世界单位，0=玩家侧，最大值=AI 侧）。</summary>
        private float _pvpDragOffsetY;

        /// <summary>PVP 滑动镜头：相机上移上限（世界单位），按 GameMap pve→pvp 实际偏移计算。</summary>
        private float _pvpDragMaxOffsetY;

        /// <summary>PVP 滑动镜头：屏幕像素 → 世界单位换算系数（正交相机下 = 2*orthoSize / Screen.height）。</summary>
        private float _pvpDragWorldPerScreenPixel = 1f;

        /// <summary>PVP 滑动镜头：上一次指针位置（屏幕坐标）。</summary>
        private Vector2 _pvpCameraDragLastPos;

        /// <summary>PVP 滑动镜头：是否正在拖动内容。</summary>
        private bool _pvpCameraDragActive;

        /// <summary>结算耗时排查：最后怪死亡 / 失败触发时的 realtimeSinceStartup。</summary>
        public static float SettleTimingT0;

        public static float SettleTimingElapsedMs()
        {
            return (Time.realtimeSinceStartup - SettleTimingT0) * 1000f;
        }

        public async void GameSuccess()
        {
            LogTool.Log($"[SettleTiming] 2.开始子弹时间(2s) +{SettleTimingElapsedMs():F0}ms");
            // 子弹时间（只影响 DOTween，不改 Time.timeScale）
            DOTween.timeScale = 0.1f;

            // 必须用真实时间延时：DOVirtual.DelayedCall 会受 DOTween.timeScale 影响；
            // 引导 PauseGame 时 Time.deltaTime=0 也会卡死 ActionKit.Delay。
            await UniTask.Delay(System.TimeSpan.FromSeconds(2f), ignoreTimeScale: true);

            LogTool.Log($"[SettleTiming] 3.子弹结束，准备请求结算 +{SettleTimingElapsedMs():F0}ms");
            DOTween.timeScale = Global.SpeedMultiplier.Value;

            PauseGame();
            BattleModel.Settle(Global.User.UserId, () =>
            {
                ClearGame();
            }, true);
        }

        // 战斗失败
        public async void GameFailure()
        {
            SettleTimingT0 = Time.realtimeSinceStartup;
            LogTool.Log($"[SettleTiming] 1.失败触发 t0={SettleTimingT0:F3}");
            DOTween.timeScale = 0.1f;
            await UniTask.Delay(System.TimeSpan.FromSeconds(1f), ignoreTimeScale: true);

            LogTool.Log($"[SettleTiming] 3.子弹结束，准备请求结算 +{SettleTimingElapsedMs():F0}ms");
            DOTween.timeScale = Global.SpeedMultiplier.Value;

            PauseGame();
            _gameOver = true;
            BattleModel.Settle(0, ClearGame, true);
        }

        // 入场动画
        public int CardRuchang;

        public void SoliderCardRuChnag()
        {
            SoldierCard[] solider = GameObject.FindObjectsOfType<SoldierCard>();
            List<Transform> pos = new List<Transform>();
            foreach (Transform child in GameLand.Default.FriendlySlot.transform)
            {
                if (child.name.Contains("Pos"))
                {
                    pos.Add(child);
                }
            }

            for (int i = 0; i < solider.Length; i++)
            {
                solider[i].transform.position = pos[i].position;
            }
        }

        #endregion


        #region 生成怪物

        private void CheckForSpawn()
        {
            if (_monsterGenDayList == null || Global.Date.Value <= 0) return;

            var monsterGenDayUpperLimit = _monsterGenDayUpperLimitMap[Global.Date.Value];

            // 怪物生成完毕，开始倒计时进入下一天
            if (spawnedMonsterCount >= monsterGenDayUpperLimit)
            {
                currentDateTime += Time.deltaTime;
                // LogTool.Log($"怪物生成完毕，开始倒计时: {currentDateTime}/s");
                if (_monsterGenDayList.Countdown > 0 && currentDateTime >= _monsterGenDayList.Countdown)
                {
                    // LogTool.Log($">>>>>> 倒计时: {currentDateTime}/s结束，开始进入下一波");
                    // 倒计时完毕，直接进入下一天
                    EndDay();
                }
            }

            // 检查是否需要更新当前的刷怪配置
            var currentWave = UpdateCurrentWave(_monsterGenDayList);

            // 检查是否有有效的刷怪配置
            if (currentWave == null) return;

            var aliveMonsterCount = totalMonsterCount;

            // 检查是否达到刷怪间隔时间，并且确保未达到同屏怪物数量上限
            if (gameTime >= nextWaveTime && aliveMonsterCount < currentWave.MaxMonster)
            {
                if (spawnedMonsterCount >= monsterGenDayUpperLimit) // 检查怪物总数是否达到上限
                {
                    return;
                }

                // testIndex++;
                // LogTool.Log("生成"+ testIndex);
                
                //还没随机过
                if (ShuffledGenCards == null)
                {
                    ShuffledGenCards = new List<CardIdNumLv>();
                    for (int i = 0; i < currentWave.GenCards.Count; i++)
                    {
                        var cfg = currentWave.GenCards[i];
                        for (int j = 0; j < cfg.Num; j++)
                        {
                            ShuffledGenCards.Add(cfg);
                        }
                    }
                    ShuffledGenCards = ShuffledGenCards.OrderBy(x => Random.value).ToList();
                }
                // 生成怪物并更新下次生成时间
                if (SpawnEnemies(ShuffledGenCards,currentWaveSpawnDataIndex))
                {
                    currentWaveSpawnDataIndex = 0;
                    currentWaveIndex++;
                    ShuffledGenCards = null;
                }
                else
                {
                    currentWaveSpawnDataIndex++;
                }

                

                nextWaveTime = gameTime + currentWave.Spawninterval; // 设置下次生成时间

                LogTool.Log(
                    $">>>>> 第{Global.Date.Value}天 - currentWaveIndex: {currentWaveIndex}, 总怪物数量: {totalMonsterCount}, 生成怪物数量: {spawnedMonsterCount}");
            }
        }

        // 更新当前的刷怪配置，根据当前时间选择最近的时间点
        private MonsterGenWave UpdateCurrentWave(MonsterGenStage monsterConfig)
        {
            if (currentWaveIndex >= monsterConfig.WaveList.Count)
            {
                return null;
            }

            return monsterConfig.WaveList[currentWaveIndex];
        }

        /// <summary>
        /// 随机的波次怪物
        /// </summary>
        private List<CardIdNumLv> ShuffledGenCards = null;
        private bool SpawnEnemies(List<CardIdNumLv> genCards,int dataIndex)
        {
            // 打乱 genCards 列表顺序，以随机顺序生成怪物
            List<CardIdNumLv> shuffledGenCards = genCards;
            // shuffledGenCards = shuffledGenCards.OrderBy(x => Random.value).ToList();
            var shuffledGenCount = 0;

            // 生成怪物数量修正
            var followUpMonsterNum = ModuleAttributes.GetFinal(AttributeType.FollowUpMonsterNum);
            for (int i = 0; i < shuffledGenCards.Count; i++)
            {
                if (i != dataIndex)
                {
                    continue;
                }
                var cardConfig = shuffledGenCards[i];
                 // var cardConfig = shuffledGenCards[dataIndex];
                int cardId = cardConfig.CardId; // 怪物ID
                int level = cardConfig.Lv; // 怪物等级
                int maxCount = cardConfig.Num <= 0 ? 1 : cardConfig.Num; // 怪物最大数量
                
                var monsterBattle = _monsterBattleConfig.GetOrDefault(cardId);
                List<Transform> availablePositions = new List<Transform>();
                switch (monsterBattle.MonsterQuality)
                {
                    case MonsterQuality.Ordinary:
                        availablePositions = ordinaryPositions;
                        maxCount += Mathf.FloorToInt(maxCount * followUpMonsterNum);// 最大数量修正加成
                        break;
                    case MonsterQuality.Elite:
                        availablePositions = elitePositions;
                        break;
                    case MonsterQuality.Boss:
                        availablePositions = bossPositions;
                        break;
                }
                
                // 检查是否已达到单种怪物的数量上限
                var uniqueKey = string.Join("_", cardId, level, maxCount);
                if (spawnedMonsterCounts.ContainsKey(uniqueKey) && spawnedMonsterCounts[uniqueKey] >= maxCount)
                {
                    shuffledGenCount++;
                    continue;
                }

                // 更新计数
                totalMonsterCount++;
                // LogTool.Log(">>>>> 当前计数增加"+totalMonsterCount);
                spawnedMonsterCount++;
                // LogTool.Log(">>>>> 总数计数增加"+spawnedMonsterCount);

                // 随机选择生成位置
                Vector3 spawnPosition = Vector3.zero;
                if (availablePositions.Count > 0)
                {
                    var randomIndex = Random.Range(0, availablePositions.Count);
                    spawnPosition = availablePositions[randomIndex].position;
                }

                spawnedMonsterCounts.TryAdd(uniqueKey, 0);
                spawnedMonsterCounts[uniqueKey]++;

                // 生成敌人并设置其位置
                // GetCard(cardId, level, Vector3.zero, spawnPosition);
                GetCard(cardId, level, Vector3.zero);

                // PVP：同一波怪镜像刷到对侧，双方城市同时被怪攻打
                if (BattleSession.IsPvpAi && GameLand.Default != null && GameLand.Default.OpponentLane != null)
                {
                    totalMonsterCount++;
                    AddMonsterCard(cardId, level, default, 1f, false, 0.5f, BattleSide.Opponent);
                    LogTool.Log($">>>>> PVP 对侧镜像刷怪 cardId={cardId} lv={level}");
                }
            }

            return dataIndex >= shuffledGenCards.Count - 1;
            // return shuffledGenCount >= shuffledGenCards.Count;
        }

        // 当敌人被消灭时调用此方法
        public void OnEnemyDefeated(int cardId)
        {
            OnEnemyDefeatedInternal(cardId, null);
        }

        public void OnEnemyDefeated(MonsterCard monster)
        {
            if (monster == null)
            {
                return;
            }

            // 先转移再结算清场，避免场上短暂为空误触发胜利
            MonsterTransferOnDeathRule.TryTransfer(monster);
            OnEnemyDefeatedInternal(monster._cardId, monster);
        }

        private void OnEnemyDefeatedInternal(int cardId, MonsterCard monster)
        {
            if (_gameOver) return;

            // PVP：对侧怪物被对侧英灵击杀时发放对侧 AI 金币（全自动经济）
            if (BattleSession.IsPvpAi && monster != null && monster.BattleSide == BattleSide.Opponent)
            {
                _opponentAiGold += GetOpponentAiKillReward(monster);
                LogTool.Log($">>>>> PVP 对侧AI击杀奖励 金币={_opponentAiGold}");
            }

            // 计数可能因 AOE 连杀/跨天与场上不一致，不能再因 totalMonsterCount<=0 直接丢弃，
            // 否则场上最后几只怪死亡时无法进入胜利判定（风舞疾袭等清场技易触发）。
            if (totalMonsterCount > 0)
            {
                totalMonsterCount--;
            }

            deadCount++;

            // 监听条件 - 击杀敌人数量
            ConditionManager.Instance.OnTriggerProgress(ConditionType.KillEnemyCount, 1);
            ScoreController.Instance.AddKillMonster(cardId);

            var wall = monster != null
                ? GameLand.Default.GetLane(monster.BattleSide)?.Wall
                : GameLand.Default.wall;
            if (wall == null)
            {
                wall = GameLand.Default.wall;
            }

            if (wall != null &&
                _monsterBattleConfig.GetOrDefault(cardId).MonsterQuality == MonsterQuality.Elite)
            {
                wall.KillEliteRegenFun();
            }

            LogTool.Log($">>>>> [{cardId}] 总死亡计数减少: {totalMonsterCount}, 死亡数量: {deadCount}");
            LogTool.Log(">>>>> [" + cardId + "]死亡数量" + deadCount);

            TryHandleDayClearedAfterEnemyDefeated();
        }

        /// <summary>
        /// 场上无存活怪物时，尝试胜利结算或重置当日击杀计数。
        /// </summary>
        private void TryHandleDayClearedAfterEnemyDefeated()
        {
            // PVP 胜负由英灵全灭 / 5 分钟限时判定，不走 PVE 清场结算
            if (BattleSession.IsPvpAi)
            {
                return;
            }

            if (MonsterManager.Instance == null || _monsterGenCfg == null || Global.Date.Value <= 0)
            {
                return;
            }

            if (MonsterManager.Instance.GetAliveEnemyCount() > 0)
            {
                return;
            }

            if (_monsterGenDayList == null
                || !_monsterGenDayUpperLimitMap.TryGetValue(Global.Date.Value, out var monsterGenDayUpperLimit))
            {
                return;
            }

            // 击杀达标，或当天已刷完且场上已空（避免 deadCount/totalMonsterCount 漂移导致永不结算）
            bool killQuotaMet = deadCount >= monsterGenDayUpperLimit;
            bool spawnDoneAndFieldClear = spawnedMonsterCount >= monsterGenDayUpperLimit;
            if (!killQuotaMet && !spawnDoneAndFieldClear)
            {
                return;
            }

            LogTool.Log(
                $">>>>> 清场检测: deadCount={deadCount}, spawned={spawnedMonsterCount}, limit={monsterGenDayUpperLimit}");

            var lastCfg = _monsterGenCfg.StageList.Last();
            if (Global.Date.Value == lastCfg.Day)
            {
                totalMonsterCount = 0;
                deadCount = 0;
                _gameOver = true;
                SettleTimingT0 = Time.realtimeSinceStartup;
                LogTool.Log($"[SettleTiming] 1.最后怪物死亡(清场胜利) t0={SettleTimingT0:F3}");
                GameSuccess();
                return;
            }

            totalMonsterCount = 0;
            deadCount = 0;
        }

        /// <summary>
        /// PVP：任意一方英灵全灭立即判定胜负（对侧全灭=胜利，我方全灭=失败）。
        /// </summary>
        private void TryCheckPvpEndByTeam()
        {
            if (_pvpVictoryChecked || _gameOver || !BattleSession.IsPvpAi)
            {
                return;
            }

            // 对侧英灵全灭 → 玩家胜利
            if (IsTeamAllDead(opponentGameCards))
            {
                _pvpVictoryChecked = true;
                LogTool.Log(">>>>> PVP 对侧英灵全灭，我方胜利");
                GameSuccess();
                return;
            }

            // 我方英灵全灭 → 玩家失败
            if (IsTeamAllDead(villageGameCards))
            {
                _pvpVictoryChecked = true;
                LogTool.Log(">>>>> PVP 我方英灵全灭，战斗失败");
                GameFailure();
            }
        }

        /// <summary>队伍是否全灭（空列表不视为全灭，避免开局尚未上场时误判）。</summary>
        private bool IsTeamAllDead(List<SoldierCard> cards)
        {
            if (cards == null || cards.Count == 0)
            {
                return false;
            }

            foreach (var card in cards)
            {
                if (card != null && !card.isDie)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// PVP：战斗满 5 分钟按双方剩余英灵血量总和判定，血量高者胜；打平视为我方胜。
        /// </summary>
        private void TrySettlePvpByRemainingHp()
        {
            if (_pvpVictoryChecked || _gameOver || !BattleSession.IsPvpAi)
            {
                return;
            }

            _pvpVictoryChecked = true;
            float playerHp = GetTeamRemainHpSum(villageGameCards);
            float opponentHp = GetTeamRemainHpSum(opponentGameCards);
            LogTool.Log($">>>>> PVP 5分钟限时结算: playerHp={playerHp} opponentHp={opponentHp}");

            if (opponentHp > playerHp)
            {
                GameFailure();
            }
            else
            {
                GameSuccess();
            }
        }

        /// <summary>队伍剩余血量总和（存活英灵 CurrentHp 之和）。</summary>
        private float GetTeamRemainHpSum(List<SoldierCard> cards)
        {
            if (cards == null || cards.Count == 0)
            {
                return 0f;
            }

            float sum = 0f;
            foreach (var card in cards)
            {
                if (card != null && !card.isDie)
                {
                    sum += card.CurrentHp;
                }
            }

            return sum;
        }

        /// <summary>
        /// PVP：对侧 AI 全自动购买 —— 与玩家一致，花金币买经验升级，IsAddHero 等级才上场英灵；
        /// 人物全部上场后购买三选一并随机选择（对侧无购买 UI，玩家侧有 Toast + 飘字提示）。
        /// </summary>
        private void TickOpponentAi(float delta)
        {
            if (_gameOver || _pvpVictoryChecked || !BattleSession.IsPvpAi)
            {
                return;
            }

            // 对侧金币仅来自击杀（与玩家一致，无被动收入；击杀发放见 OnEnemyDefeatedInternal）

            // 购买节流：避免同帧连续购买
            _opponentAiBuyCooldown -= delta;
            if (_opponentAiBuyCooldown > 0f)
            {
                return;
            }
            _opponentAiBuyCooldown = 0.5f;

            var opponent = BattleSession.Opponent;
            if (opponent == null)
            {
                return;
            }

            if (_opponentAiTeamData == null)
            {
                _opponentAiTeamData = opponent.ToTeamBuildResult();
            }

            var teamData = _opponentAiTeamData;
            if (teamData == null || teamData.SoldierTeams == null || teamData.SoldierTeams.Count == 0)
            {
                return;
            }

            // 优先购买人物（与玩家一致）：花金币买经验，升级到 IsAddHero 等级才上英魂
            if (_spawnedOpponentTeamSoldierCount < teamData.SoldierTeams.Count)
            {
                int expCost = GetOpponentAiHeroBuyCost();
                if (expCost <= 0 || _opponentAiGold < expCost)
                {
                    return;
                }

                _opponentAiGold -= expCost;
                _opponentAiExp += GetOpponentAiCityExpGain();
                LogTool.Log(
                    $">>>>> PVP 对侧AI购买经验: cost={expCost} gold={_opponentAiGold} exp={_opponentAiExp}/{GetOpponentAiRequiredExp(_opponentAiLevel)} lv={_opponentAiLevel}");

                int requiredExp = GetOpponentAiRequiredExp(_opponentAiLevel);
                if (requiredExp > 0 && _opponentAiExp >= requiredExp)
                {
                    _opponentAiExp -= requiredExp;
                    _opponentAiLevel++;
                    bool addHero = IsOpponentAiLevelAddHero(_opponentAiLevel);
                    if (addHero)
                    {
                        SpawnOpponentTeamSoldiers(teamData, 1);
                    }

                    LogTool.Log(
                        $">>>>> PVP 对侧AI升级: level={_opponentAiLevel} addHero={addHero} 已上场 {opponentGameCards.Count}/{teamData.SoldierTeams.Count}");
                }

                return;
            }

            // 人物全部购买完毕 → 购买三选一（随机选）
            int chooseCost = GetOpponentAiChooseCost();
            if (_opponentAiGold < chooseCost)
            {
                return;
            }

            if (TryOpponentAiBuySchoolCard(chooseCost))
            {
                _opponentAiGold -= chooseCost;
                _opponentAiChooseBuyCount++;
            }
        }

        /// <summary>对侧 AI 购买下一个英雄（买经验）的费用（对齐 CityLevelConfig.NeedGold，随内部等级递增）。</summary>
        private int GetOpponentAiHeroBuyCost()
        {
            var levelCfg = GetOpponentAiCityLevelCfg(_opponentAiLevel);
            if (levelCfg == null || levelCfg.NeedGold <= 0)
            {
                return 10;
            }

            return levelCfg.NeedGold;
        }

        /// <summary>对侧 AI 购买经验获得的经验值（对齐 CityLevelConfig.CityExp）。</summary>
        private int GetOpponentAiCityExpGain()
        {
            var levelCfg = GetOpponentAiCityLevelCfg(_opponentAiLevel);
            return levelCfg != null && levelCfg.CityExp > 0 ? levelCfg.CityExp : 10;
        }

        /// <summary>对侧 AI 当前等级升级所需经验（对齐 CityLevelConfig.CityExp）。</summary>
        private int GetOpponentAiRequiredExp(int level)
        {
            var levelCfg = GetOpponentAiCityLevelCfg(level);
            return levelCfg != null && levelCfg.CityExp > 0 ? levelCfg.CityExp : 10;
        }

        /// <summary>对侧 AI 指定等级升级是否上英魂（对齐 CityLevelConfig.IsAddHero）。</summary>
        private bool IsOpponentAiLevelAddHero(int level)
        {
            var levelCfg = GetOpponentAiCityLevelCfg(level);
            return levelCfg != null && levelCfg.IsAddHero == 1;
        }

        private CityLevelList GetOpponentAiCityLevelCfg(int level)
        {
            var difficultyCfg = DataConfigManager.Tables.TbCityLevelConfig.GetOrDefault(Global.GameDifficultyLevel.Value);
            return difficultyCfg?.LevelList?.FirstOrDefault(cfg => cfg.Level == level);
        }

        /// <summary>
        /// PVP：对侧英灵自动释放主动技能（每 0.5s 检查一轮，仅释放非被动且冷却完成的技能）。
        /// </summary>
        private void TickOpponentAiSkillRelease()
        {
            if (opponentGameCards == null || opponentGameCards.Count == 0)
            {
                return;
            }

            for (int i = 0; i < opponentGameCards.Count; i++)
            {
                var soldier = opponentGameCards[i];
                if (soldier == null || soldier.isDie)
                {
                    continue;
                }

                var skills = soldier.SkillComponent?.GetSkillAll();
                if (skills == null || skills.Count == 0)
                {
                    continue;
                }

                foreach (var pair in skills)
                {
                    var skill = pair.Value;
                    if (skill == null || skill.Id <= 0 || skill.Id == soldier.NormalAttackSkillId)
                    {
                        continue;
                    }

                    var specialCfg = DataConfigManager.Tables.TbSpecialEffectsConfig.GetOrDefault(skill.Id);
                    if (specialCfg != null && specialCfg.PassiveSkill == 1)
                    {
                        continue;
                    }

                    if (!skill.IsReady(false))
                    {
                        continue;
                    }

                    // 对侧英灵需要扇形索敌成功才放技能（与玩家主动技能同一套）
                    if (soldier.StartReleaseSkill(skill.Id, useNearestEnemy: true))
                    {
                        LogTool.Log($">>>>> PVP 对侧AI释放技能: soldier={soldier.cardId} skill={skill.Id}");
                        break;
                    }
                }
            }
        }

        /// <summary>对侧 3 选一购买提示：在对侧场上飘字 + 全局 Toast。</summary>
        private void ShowOpponentAiPurchaseHint(string cardName, int star)
        {
            FusionContinent.Toast.Show($"AI购买三选一：{cardName} Lv.{star}");

            var damageParent = GameLand.Default?.OpponentLane?.DamageParent;
            if (damageParent == null)
            {
                return;
            }

            DamageFun.Show(
                $"AI获得 {cardName}",
                GetOpponentAiDisplayPosition(),
                parentOverride: damageParent);
        }

        private Vector3 GetOpponentAiDisplayPosition()
        {
            if (opponentGameCards != null)
            {
                for (int i = 0; i < opponentGameCards.Count; i++)
                {
                    var soldier = opponentGameCards[i];
                    if (soldier != null && !soldier.isDie)
                    {
                        return soldier.transform.position;
                    }
                }
            }

            if (_opponentFriendlySlotRoot != null)
            {
                return _opponentFriendlySlotRoot.position;
            }

            return GameLand.Default != null ? GameLand.Default.transform.position : Vector3.zero;
        }

        private int GetOpponentAiChooseCost()
        {
            int baseCost = DataConfigManager.Tables.TbGameBattleConst.SkillInitBuyChooseCardGoldNum;
            if (baseCost <= 0)
            {
                baseCost = 20;
            }

            return baseCost * (_opponentAiChooseBuyCount + 1);
        }

        /// <summary>
        /// PVP：对侧 AI 购买三选一 —— 生成 3 张候选卡并随机选择一张，应用到对侧英灵。
        /// </summary>
        private bool TryOpponentAiBuySchoolCard(int cost)
        {
            var service = GameServiceManager.Get<Game.Services.SchoolChooseCardService>();
            if (service == null)
            {
                LogTool.Log(">>>>> PVP 对侧AI三选一失败：选卡服务不可用");
                return false;
            }

            service.InitializeSingleChoose(0);
            var cardIds = service.GenerateChooseCardIds(service.SingleChooseCount);
            if (cardIds == null || cardIds.Count == 0)
            {
                LogTool.Log(">>>>> PVP 对侧AI三选一失败：无可选卡牌");
                return false;
            }

            int picked = cardIds[Random.Range(0, cardIds.Count)];
            var cfg = service.GetSchoolCardConfig(picked);
            if (cfg == null || cfg.StarList == null || cfg.StarList.Count == 0)
            {
                LogTool.Log($">>>>> PVP 对侧AI三选一失败：配置缺失 cardId={picked}");
                return false;
            }

            int nextStar = _opponentAiSchoolCards.TryGetValue(picked, out var existStar) ? existStar + 1 : 1;
            nextStar = Mathf.Min(nextStar, cfg.StarList.Count);
            _opponentAiSchoolCards[picked] = nextStar;

            ApplyOpponentAiSchoolCardToTeam(picked, nextStar);
            // Module 类效果（学技能/经济/随机强化）仅购买时生效一次
            ApplyOpponentAiSchoolCardOnce(picked, nextStar);

            string options = string.Join(",", cardIds);
            LogTool.Log(
                $">>>>> PVP 对侧AI购买三选一: cost={cost} gold={_opponentAiGold} options=[{options}] picked={picked} star={nextStar}");

            // 显示对侧 AI 购买的 3 选 1（玩家侧可见）
            ShowOpponentAiPurchaseHint(
                string.IsNullOrEmpty(cfg.Name) ? $"#{picked}" : cfg.Name,
                nextStar);

            return true;
        }

        /// <summary>把对侧 AI 已购买的三选一卡属性应用到全体已上场英灵。</summary>
        private void ApplyOpponentAiSchoolCardToTeam(int cardId, int star)
        {
            if (opponentGameCards == null || opponentGameCards.Count == 0)
            {
                return;
            }

            for (int i = 0; i < opponentGameCards.Count; i++)
            {
                ApplyOpponentAiSchoolCardToSoldier(opponentGameCards[i], cardId, star);
            }
        }

        /// <summary>把对侧 AI 已购买的三选一卡属性应用到指定英灵（新增上场时补齐）。</summary>
        private void ApplyOpponentAiSchoolCardsToSoldier(SoldierCard soldier)
        {
            if (soldier == null || _opponentAiSchoolCards.Count == 0)
            {
                return;
            }

            foreach (var pair in _opponentAiSchoolCards)
            {
                ApplyOpponentAiSchoolCardToSoldier(soldier, pair.Key, pair.Value);
            }
        }

        /// <summary>把一张三选一卡的指定星级属性应用到单个英灵（Soldier/Skill 类属性 + 专属被动）。</summary>
        private void ApplyOpponentAiSchoolCardToSoldier(SoldierCard soldier, int cardId, int star)
        {
            if (soldier == null || soldier.isDie)
            {
                return;
            }

            var cfg = DataConfigManager.Tables.TbSchoolCardConfig.GetOrDefault(cardId);
            if (cfg == null || cfg.StarList == null || cfg.StarList.Count == 0)
            {
                return;
            }

            var starCfg = cfg.StarList[Mathf.Clamp(star, 1, cfg.StarList.Count) - 1];
            if (starCfg?.Attributes == null || starCfg.Attributes.Count == 0)
            {
                return;
            }

            bool isAdditive = cfg.IsAdditiveAttr > 0;
            foreach (var attr in starCfg.Attributes)
            {
                // 专属被动：仅对 IsAddHero 指定的英灵生效（对齐玩家 LearnPassiveSkillCard 英雄作用域）
                if (attr.Type == AttributeType.ExclusiveLearningEffects)
                {
                    ApplyOpponentAiExclusiveLearningEffect(soldier, attr, cfg.IsAddHero);
                    continue;
                }

                var attrCfg = DataConfigManager.Tables.TbAttributeConfig.GetOrDefault(attr.Type);
                if (attrCfg == null)
                {
                    continue;
                }

                switch (attrCfg.Category)
                {
                    case AttributeCategoryType.Soldier:
                        ApplyOpponentAiSoldierAttr(soldier, attr, cardId, star, isAdditive);
                        break;

                    case AttributeCategoryType.Skill:
                        ApplyOpponentAiSkillAttr(soldier, attr, cardId, star, isAdditive);
                        break;

                    default:
                        // Module 类（学技能/规则/经济等）在购买时统一生效一次，见 ApplyOpponentAiSchoolCardOnce
                        break;
                }
            }
        }

        /// <summary>对侧三选一 Module 类属性在购买时统一生效一次（学技能/经济/随机强化）。</summary>
        private void ApplyOpponentAiSchoolCardOnce(int cardId, int star, int bonusDepth = 0)
        {
            var cfg = DataConfigManager.Tables.TbSchoolCardConfig.GetOrDefault(cardId);
            if (cfg == null || cfg.StarList == null || cfg.StarList.Count == 0)
            {
                return;
            }

            var starCfg = cfg.StarList[Mathf.Clamp(star, 1, cfg.StarList.Count) - 1];
            if (starCfg?.Attributes == null || starCfg.Attributes.Count == 0)
            {
                return;
            }

            bool isAdditive = cfg.IsAdditiveAttr > 0;
            foreach (var attr in starCfg.Attributes)
            {
                var attrCfg = DataConfigManager.Tables.TbAttributeConfig.GetOrDefault(attr.Type);
                if (attrCfg == null || attrCfg.Category != AttributeCategoryType.Module)
                {
                    continue;
                }

                switch (attr.Type)
                {
                    case AttributeType.LearnSkillWindDanceAttack:
                    case AttributeType.LearnSkillThunderFieldFullScreen:
                    case AttributeType.LearnCitySkillEnergyBurst:
                    case AttributeType.TestFullScreenHarm:
                    case AttributeType.SoldierNormalAttackHeavyBlowSkill:
                    case AttributeType.SoldierNormalAttackBrokenArmySlashSkill:
                    case AttributeType.SoldierStartReleaseCometSkill:
                    case AttributeType.LearnSkillBrokenBlade:
                        // 学习全场/场地技能（对侧英灵自动释放，见 TickOpponentAiSkillRelease）
                        LearnOpponentAiSkillLogic(attr.Type);
                        break;

                    case AttributeType.KillAnEnemyExtraBattleGoldCoin:
                        // 金币收藏家：击杀敌人额外金币并入对侧养成属性（GetOpponentAiGoldBonusAttribute 读取）
                        var agg = GetOpponentBattleAttributes();
                        if (agg?.Attributes != null)
                        {
                            agg.Attributes.GetAttr(attr.Type).AddModifier(new AttributeModifier
                            {
                                SourceKey = $"OpponentAiSchool_{cardId}_{star}",
                                SourceType = AttributeSourceType.SchoolCard,
                                FlatAdd = attr.ValueType == ValueType.Number ? attr.Value : 0f,
                                PercentAdd = attr.ValueType == ValueType.Percentage ? attr.Value : 0f,
                            }, isAdditive);
                        }
                        break;

                    case AttributeType.RandomlyObtainRareEnhancements:
                        // 潘多拉的魔盒：随机获得稀有强化并应用到对侧（限制嵌套深度，避免魔盒套娃）
                        if (bonusDepth <= 0)
                        {
                            var service = GameServiceManager.Get<Game.Services.SchoolChooseCardService>();
                            if (service != null)
                            {
                                int count = RandomTool.RandomRangeTarget(1, Mathf.RoundToInt(attr.Value));
                                var bonusIds = service.RandomSchoolChooseCards(count);
                                if (bonusIds != null)
                                {
                                    for (int i = 0; i < bonusIds.Count; i++)
                                    {
                                        ApplyOpponentAiBonusSchoolCard(bonusIds[i]);
                                    }
                                }
                            }
                        }
                        break;

                    case AttributeType.LearnSkillRule:
                        // 规则引擎为玩家侧全局，注册会同时作用到玩家，暂不对对侧生效
                        LogTool.Log($">>>>> PVP 对侧AI三选一: LearnSkillRule 规则卡暂不对对侧生效 ruleId={attr.Value} cardId={cardId}");
                        break;

                    default:
                        LogTool.Log($">>>>> PVP 对侧AI三选一: Module 属性未实现对侧应用 type={attr.Type} cardId={cardId}");
                        break;
                }
            }
        }

        /// <summary>对侧英灵属性（Soldier 类）：写英灵基础属性，与玩家 SetAttributeLogic 默认路径一致。</summary>
        private void ApplyOpponentAiSoldierAttr(SoldierCard soldier, AttributeStruct attr, int cardId, int star, bool isAdditive)
        {
            var modifier = new AttributeModifier
            {
                SourceKey = $"OpponentAiSchool_{cardId}_{star}",
                SourceType = AttributeSourceType.SchoolCard,
                FlatAdd = attr.ValueType == ValueType.Number ? attr.Value : 0f,
                PercentAdd = attr.ValueType == ValueType.Percentage ? attr.Value : 0f,
                EffectParam = attr.EffectParam,
                ProbabilityTrigger = attr.ProbabilityTrigger,
            };
            soldier.BaseAttributes.GetAttr(attr.Type).AddModifier(modifier, isAdditive);
        }

        /// <summary>对侧技能属性（Skill 类）：写入已学技能的 Attributes（对齐玩家 AddSkillAttributeModifier）。</summary>
        private void ApplyOpponentAiSkillAttr(SoldierCard soldier, AttributeStruct attr, int cardId, int star, bool isAdditive)
        {
            var modifier = new AttributeModifier
            {
                SourceKey = $"OpponentAiSchool_{cardId}_{star}",
                SourceType = AttributeSourceType.SchoolCard,
                FlatAdd = attr.ValueType == ValueType.Number ? attr.Value : 0f,
                PercentAdd = attr.ValueType == ValueType.Percentage ? attr.Value : 0f,
                EffectParam = attr.EffectParam,
                ProbabilityTrigger = attr.ProbabilityTrigger,
            };
            soldier.AddSkillAttributeModifier(attr.Type, modifier, attr.EffectParam, isAdditive);
        }

        /// <summary>
        /// 对侧专属被动（ExclusiveLearningEffects）：与玩家 LearnPassiveSkillDirectToSoldier 一致，
        /// 将被动等级属性以 Modifier 叠加到 PassiveActsOnCard 指定的技能上；仅对 IsAddHero 对应英灵生效。
        /// </summary>
        private void ApplyOpponentAiExclusiveLearningEffect(SoldierCard soldier, AttributeStruct attr, int scopeHeroCardId)
        {
            int passiveSkillId = Mathf.RoundToInt(attr.Value);
            if (passiveSkillId <= 0)
            {
                return;
            }

            if (scopeHeroCardId != 0 && scopeHeroCardId != soldier.cardId)
            {
                return;
            }

            var specialCfg = DataConfigManager.Tables.TbSpecialEffectsConfig.GetOrDefault(passiveSkillId);
            if (!IsPassiveSkillConfig(specialCfg))
            {
                LogTool.Log($">>>>> PVP 对侧AI三选一: 专属效果不是被动技能, 跳过 passiveSkillId={passiveSkillId}");
                return;
            }

            var levelCfg = ResolvePassiveLevelConfig(specialCfg, 1);
            if (levelCfg == null)
            {
                return;
            }

            // 与玩家一致：先清旧 Modifier 再叠加新 Modifier（学习规则类属性跳过，规则引擎为玩家侧全局）
            RemovePassiveModifiersFromSoldierSkills(soldier, specialCfg, passiveSkillId);
            ApplyPassiveModifiersToSoldierSkills(soldier, specialCfg, levelCfg, passiveSkillId);
            LogTool.Log($">>>>> PVP 对侧AI三选一: 专属被动生效 soldier={soldier.cardId} passiveSkillId={passiveSkillId}");
        }

        /// <summary>对侧学习技能（对齐玩家 AttributeManager.LearnSkillLogic，遍历对侧英灵）。</summary>
        private void LearnOpponentAiSkillLogic(AttributeType attrType)
        {
            var skillType = SpecialEffectType.WindDanceAttack;
            bool isUseSkill = false;
            bool isFirstSoldier = false;

            switch (attrType)
            {
                case AttributeType.LearnSkillWindDanceAttack:
                    skillType = SpecialEffectType.WindDanceAttack; // 学习技能风舞疾袭
                    isUseSkill = true;
                    isFirstSoldier = true;
                    break;

                case AttributeType.LearnSkillThunderFieldFullScreen:
                    skillType = SpecialEffectType.ThunderFieldFullScreen; // 雷霆领域
                    isUseSkill = true;
                    isFirstSoldier = true;
                    break;

                case AttributeType.TestFullScreenHarm:
                    skillType = SpecialEffectType.TestFullScreenHarm; // 测试全屏伤害
                    isUseSkill = true;
                    isFirstSoldier = true;
                    break;

                case AttributeType.LearnCitySkillEnergyBurst:
                    skillType = SpecialEffectType.CdTimeEnergyBurst; // 能量迸发
                    isFirstSoldier = true;
                    break;

                case AttributeType.SoldierNormalAttackHeavyBlowSkill:
                    skillType = SpecialEffectType.HeavyBlow; // 重击斩
                    break;

                case AttributeType.SoldierNormalAttackBrokenArmySlashSkill:
                    skillType = SpecialEffectType.BrokenArmySlash; // 破军一击
                    break;

                case AttributeType.SoldierStartReleaseCometSkill:
                    skillType = SpecialEffectType.SecretCometTechnique; // 秘法彗星
                    break;

                case AttributeType.LearnSkillBrokenBlade:
                    skillType = SpecialEffectType.HeavyBlow; // 碎骨斩
                    isUseSkill = true;
                    isFirstSoldier = true;
                    break;

                default:
                    return;
            }

            var skillCfg = DataConfigManager.Tables.TbSpecialEffectsConfig.DataList.FirstOrDefault(v =>
                v.SpecialEffectType == skillType);
            if (skillCfg == null)
            {
                LogTool.LogError($">>>>> PVP 对侧AI三选一: 学习技能类型配置未找到: {skillType}");
                return;
            }

            for (int i = 0; i < opponentGameCards.Count; i++)
            {
                var soldierCard = opponentGameCards[i];
                if (soldierCard == null)
                {
                    continue;
                }

                var skillCard = soldierCard.LearnSkill(skillCfg.Id, 1);
                if (isUseSkill && skillCard != null)
                {
                    soldierCard.UseSkill(skillCfg.Id, Vector3.zero);
                }

                if (isFirstSoldier)
                {
                    break;
                }
            }

            LogTool.Log($">>>>> PVP 对侧AI三选一: 学习技能完成 type={attrType} skillId={skillCfg.Id}");
        }

        /// <summary>对侧额外获得一张三选一卡（潘多拉的魔盒）：记录星级并立即应用到场上英灵。</summary>
        private void ApplyOpponentAiBonusSchoolCard(int cardId)
        {
            var cfg = DataConfigManager.Tables.TbSchoolCardConfig.GetOrDefault(cardId);
            if (cfg == null || cfg.StarList == null || cfg.StarList.Count == 0)
            {
                return;
            }

            int nextStar = _opponentAiSchoolCards.TryGetValue(cardId, out var existStar) ? existStar + 1 : 1;
            nextStar = Mathf.Min(nextStar, cfg.StarList.Count);
            _opponentAiSchoolCards[cardId] = nextStar;

            ApplyOpponentAiSchoolCardToTeam(cardId, nextStar);
            ApplyOpponentAiSchoolCardOnce(cardId, nextStar, 1);
            LogTool.Log($">>>>> PVP 对侧AI三选一(魔盒): 额外获得 {cfg.Name} star={nextStar}");
        }

        /// <summary>PVP：对侧怪物被对侧英灵击杀时发放给对侧 AI 的金币（读怪物掉落 CombatCurrency 配置）。</summary>
        /// <summary>PVP：对侧怪物被对侧英灵击杀时发放给对侧 AI 的金币（与玩家击杀收益一致）。</summary>
        private int GetOpponentAiKillReward(MonsterCard monster)
        {
            if (monster == null || monster._monsterConfig == null)
            {
                return 0;
            }

            // 每个金币实体价值与玩家 CreateCoin 一致（DorpGoldBaseNumber）
            int goldBase = DataConfigManager.Tables.TbGameBattleConst.DorpGoldBaseNumber;
            int totalCoins = 0;
            var lvCfg = monster._monsterConfig.LevelPropertyList?.FirstOrDefault(v => v.Level == monster._level);
            if (lvCfg != null && lvCfg.DropRewards != null)
            {
                foreach (var rew in lvCfg.DropRewards)
                {
                    var itemCfg = DataConfigManager.Tables.TbItemConfig.GetOrDefault(rew.Id);
                    if (itemCfg == null || itemCfg.Type != ItemType.CombatCurrency)
                    {
                        continue;
                    }

                    totalCoins += rew.Num;
                }
            }

            // 对齐玩家 OnGoldGained：每个金币实体额外 + 击杀额外战斗币属性
            int extraPerCoin = GetOpponentAiGoldBonusAttribute(AttributeType.KillAnEnemyExtraBattleGoldCoin);
            int reward = totalCoins * (goldBase + extraPerCoin);

            // Boss/精英：对齐玩家 KillAnBossGoldCoin（同时再叠加一次击杀额外战斗币）
            if (monster._monsterConfig.MonsterQuality is MonsterQuality.Elite or MonsterQuality.Boss)
            {
                reward += GetOpponentAiGoldBonusAttribute(AttributeType.KillAnBossGoldCoin) + extraPerCoin;
            }

            return Mathf.Max(reward, 0);
        }

        /// <summary>读取对侧养成属性中的金币加成值（装备/奥数卡/共鸣聚合）。</summary>
        private int GetOpponentAiGoldBonusAttribute(AttributeType type)
        {
            var agg = GetOpponentBattleAttributes();
            if (agg == null || agg.Attributes == null)
            {
                return 0;
            }

            return Mathf.RoundToInt(agg.Attributes.GetFinal(type));
        }

        /// <summary>PVP 调试：对侧 AI 金币/等级/经验/英灵/技能实时显示（测试阶段用，后续可删）。</summary>
        private void OnGUI()
        {
            if (!BattleSession.IsPvpAi || _gameOver || _pvpVictoryChecked)
            {
                return;
            }

            // 调试字号调大，方便观察（测试阶段用）
            int originFontSize = GUI.skin.label.fontSize;
            FontStyle originFontStyle = GUI.skin.label.fontStyle;
            GUI.skin.label.fontSize = 30;
            GUI.skin.label.fontStyle = FontStyle.Bold;
            int totalTeam = _opponentAiTeamData != null && _opponentAiTeamData.SoldierTeams != null
                ? _opponentAiTeamData.SoldierTeams.Count
                : 0;
            int requiredExp = GetOpponentAiRequiredExp(_opponentAiLevel);
            int skillCount = 0;
            if (opponentGameCards != null)
            {
                for (int i = 0; i < opponentGameCards.Count; i++)
                {
                    var soldier = opponentGameCards[i];
                    if (soldier == null || soldier.isDie)
                    {
                        continue;
                    }

                    var skills = soldier.SkillComponent?.GetSkillAll();
                    if (skills != null)
                    {
                        skillCount += skills.Count;
                    }
                }
            }

            GUI.Label(
                new Rect(10f, Screen.height - 170f, 1000f, 42f),
                $"AI金币: {_opponentAiGold:F0}   等级: {_opponentAiLevel}   经验: {_opponentAiExp}/{requiredExp}   英灵: {_spawnedOpponentTeamSoldierCount}/{totalTeam}   技能数: {skillCount}");
            GUI.Label(
                new Rect(10f, Screen.height - 126f, 1000f, 42f),
                $"三选一购买: {_opponentAiChooseBuyCount} 次");
            GUI.Label(
                new Rect(10f, Screen.height - 82f, 1000f, 42f),
                $"拖拽空白处滑动镜头查看 AI 方（下滑看向对面）  进度: {(_pvpDragMaxOffsetY > 0.01f ? _pvpDragOffsetY / _pvpDragMaxOffsetY * 100f : 0f):F0}% 拖动中: {_pvpCameraDragActive}");

            GUI.skin.label.fontSize = originFontSize;
            GUI.skin.label.fontStyle = originFontStyle;
        }

        /// <summary>对侧养成属性聚合（懒加载，首次应用到英灵时构建）。</summary>
        private AttributeAggregation GetOpponentBattleAttributes()
        {
            if (_opponentBattleAttributesBuilt)
            {
                return _opponentBattleAttributes;
            }

            _opponentBattleAttributesBuilt = true;
            _opponentBattleAttributes = BattleSession.Opponent?.BuildBattleAttributes();
            return _opponentBattleAttributes;
        }

        /// <summary>
        /// 把对侧养成属性（装备/奥数卡/奥数共鸣）应用到单个英灵：
        /// 仅写 Soldier/Skill 类数值基础值与主属性，避免 Module/规则类被误写成基础值。
        /// </summary>
        private void ApplyOpponentBattleAttributesToSoldier(SoldierCard soldier)
        {
            if (soldier == null || soldier.isDie)
            {
                return;
            }

            var agg = GetOpponentBattleAttributes();
            if (agg == null)
            {
                return;
            }

            foreach (var kv in agg.Attributes.GetAll())
            {
                if (kv.Value.BaseValue == 0f)
                {
                    continue;
                }

                var attrCfg = DataConfigManager.Tables.TbAttributeConfig.GetOrDefault(kv.Key);
                if (attrCfg == null
                    || (attrCfg.Category != AttributeCategoryType.Soldier
                        && attrCfg.Category != AttributeCategoryType.Skill))
                {
                    continue;
                }

                soldier.BaseAttributes.GetAttr(kv.Key).SetBaseValue(kv.Value.BaseValue, true);
            }

            foreach (var ma in agg.MainAttributes)
            {
                soldier.BaseAttributes.UpdateMainAttributeValue(ma.Type, ma.Value, true);
            }
        }

        /// <summary>PVP 滑动镜头：重置相机到玩家侧视角。</summary>
        private void ResetPvpDrag()
        {
            _pvpCameraDragActive = false;
            _pvpDragOffsetY = 0f;
            _pvpDragMaxOffsetY = 0f;
            ApplyPvpDragOffset();
        }

        /// <summary>
        /// PVP 滑动镜头：记录相机基准位置，并按 GameMap pve→pvp 实际偏移计算滑动上限与像素换算。
        /// 只移动相机，场景物体（英灵/怪物/子弹/特效）保持不动，避免拖拽时特效乱飞。
        /// </summary>
        private void CollectPvpDragCamera()
        {
            _pvpDragCamera = null;
            if (GameMap.Default != null)
            {
                var mapCanvas = GameMap.Default.GetComponent<Canvas>();
                if (mapCanvas != null && mapCanvas.worldCamera != null)
                {
                    _pvpDragCamera = mapCanvas.worldCamera;
                }
            }

            if (_pvpDragCamera == null)
            {
                _pvpDragCamera = Camera.main;
            }

            if (_pvpDragCamera != null)
            {
                _pvpDragCameraBasePos = _pvpDragCamera.transform.position;
            }

            Transform playerSlot = GameLand.Default != null ? GameLand.Default.FriendlySlot : null;
            // 滑动上限按 GameMap pve→pvp 的实际偏移计算（对侧 pvp 相对玩家侧 pve 的位移）
            Transform mapPve = GameMap.Default != null ? GameMap.Default.transform.Find("pve") : null;
            Transform mapPvp = GameMap.Default != null ? GameMap.Default.transform.Find("pvp") : null;
            if (mapPve != null && mapPvp != null)
            {
                _pvpDragMaxOffsetY = Mathf.Abs(mapPvp.position.y - mapPve.position.y);
            }
            else if (playerSlot != null && _opponentFriendlySlotRoot != null)
            {
                _pvpDragMaxOffsetY = Mathf.Abs(_opponentFriendlySlotRoot.position.y - playerSlot.position.y);
            }
            else if (_opponentFriendlySlotRoot != null)
            {
                _pvpDragMaxOffsetY = Mathf.Abs(_opponentFriendlySlotRoot.position.y);
            }
            else
            {
                _pvpDragMaxOffsetY = 0f;
            }

            // 屏幕像素 → 世界单位：正交相机下 1 像素 = 视野高度(2*orthoSize) / 屏幕高度
            _pvpDragWorldPerScreenPixel = 1f;
            if (_pvpDragCamera != null && _pvpDragCamera.orthographic)
            {
                _pvpDragWorldPerScreenPixel = 2f * _pvpDragCamera.orthographicSize / Mathf.Max(1f, Screen.height);
            }
            else if (GameMap.Default != null)
            {
                var mapCanvas = GameMap.Default.GetComponent<Canvas>();
                if (mapCanvas != null)
                {
                    float worldPerCanvasUnit = Mathf.Abs(mapCanvas.transform.lossyScale.y);
                    float scaleFactor = Mathf.Max(0.0001f, mapCanvas.scaleFactor);
                    if (worldPerCanvasUnit > 0.0001f)
                    {
                        _pvpDragWorldPerScreenPixel = worldPerCanvasUnit / scaleFactor;
                    }
                }
            }

            _pvpDragOffsetY = Mathf.Clamp(_pvpDragOffsetY, 0f, _pvpDragMaxOffsetY);
            string playerSlotName = playerSlot != null ? playerSlot.name : "null";
            string opponentSlotName = _opponentFriendlySlotRoot != null ? _opponentFriendlySlotRoot.name : "null";
            LogTool.Log($" [PVP滑动镜头] 相机={(_pvpDragCamera != null ? _pvpDragCamera.name : "null")} 上限={_pvpDragMaxOffsetY:F2} 像素系数={_pvpDragWorldPerScreenPixel:F4} 玩家槽={playerSlotName} 对侧槽={opponentSlotName}");
        }

        /// <summary>PVP 滑动镜头：只移动相机，相机上移 _pvpDragOffsetY（下滑=看向 AI 方，物体不动）。</summary>
        private void ApplyPvpDragOffset()
        {
            if (_pvpDragCamera == null)
            {
                return;
            }

            _pvpDragCamera.transform.position = _pvpDragCameraBasePos + new Vector3(0f, _pvpDragOffsetY, 0f);
        }

        /// <summary>PVP 滑动镜头：拖拽屏幕移动相机（玩家侧 ↔ AI 侧）。</summary>
        private void UpdatePvpCameraDrag()
        {
            if (_pvpDragMaxOffsetY <= 0.01f)
            {
                // 进场时若相机/上限尚未就绪，这里补算一次
                CollectPvpDragCamera();
                if (_pvpDragMaxOffsetY <= 0.01f)
                {
                    return;
                }
            }

            Vector2 pointerPos;
            bool pointerDown = false;
            bool pointerUp = false;
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                pointerPos = touch.position;
                pointerDown = touch.phase == TouchPhase.Began;
                pointerUp = touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled;
                if (pointerDown)
                {
                    if (IsPvpCameraDragBlocked(touch.fingerId))
                    {
                        _pvpCameraDragActive = false;
                        return;
                    }

                    _pvpCameraDragActive = true;
                    _pvpCameraDragLastPos = pointerPos;
                    return;
                }
            }
            else
            {
                pointerPos = Input.mousePosition;
                pointerDown = Input.GetMouseButtonDown(0);
                pointerUp = Input.GetMouseButtonUp(0);
                if (pointerDown)
                {
                    if (IsPvpCameraDragBlocked(-1))
                    {
                        _pvpCameraDragActive = false;
                        return;
                    }

                    _pvpCameraDragActive = true;
                    _pvpCameraDragLastPos = pointerPos;
                    return;
                }
            }

            if (!_pvpCameraDragActive)
            {
                return;
            }

            if (pointerUp)
            {
                _pvpCameraDragActive = false;
                return;
            }

            Vector2 delta = pointerPos - _pvpCameraDragLastPos;
            _pvpCameraDragLastPos = pointerPos;
            if (Mathf.Abs(delta.y) < 0.01f)
            {
                return;
            }

            // 拖动跟随手指：下滑 = 相机上移 = 看向 AI 方；上滑 = 相机下移 = 回到玩家侧
            float worldDelta = delta.y * _pvpDragWorldPerScreenPixel;
            _pvpDragOffsetY = Mathf.Clamp(_pvpDragOffsetY - worldDelta, 0f, _pvpDragMaxOffsetY);
            ApplyPvpDragOffset();
        }

        /// <summary>PVP 滑动镜头：指针按下时判断是否应阻止开始拖动（只在英灵/技能卡/按钮上拦截）。</summary>
        private bool IsPvpCameraDragBlocked(int pointerId)
        {
            // 技能/卡牌拖拽中不抢镜头
            if (GameLandUI.Default != null)
            {
                var cardLongClicks = GameLandUI.Default.GetComponentsInChildren<CardSlotLongClick>(true);
                for (int i = 0; i < cardLongClicks.Length; i++)
                {
                    if (cardLongClicks[i].IsDragging)
                    {
                        return true;
                    }
                }
            }

            // 没有 EventSystem 时退回：屏幕下方技能栏区域不开始拖镜头
            var eventSystem = UnityEngine.EventSystems.EventSystem.current;
            if (eventSystem == null)
            {
                return Input.mousePosition.y < Screen.height * 0.2f;
            }

            // 精确判断按下点是否落在可交互对象上（英灵可拖动换位、技能卡拖拽释放、按钮点击）：
            // 只在这些对象上拦截，空白处 / 血条 / 透明全屏层等都不拦截，保证镜头能正常滑动。
            var pointerData = new UnityEngine.EventSystems.PointerEventData(eventSystem);
            pointerData.position = Input.mousePosition;
            var results = new List<UnityEngine.EventSystems.RaycastResult>();
            eventSystem.RaycastAll(pointerData, results);
            for (int i = 0; i < results.Count; i++)
            {
                var go = results[i].gameObject;
                if (go == null)
                {
                    continue;
                }

                if (go.GetComponentInParent<SoldierCard>() != null
                    || go.GetComponentInParent<CardSlotLongClick>() != null
                    || go.GetComponentInParent<Button>() != null)
                {
                    return true;
                }
            }

            return false;
        }

        // 惊喜三选一进行中状态
        private bool _surpriseChooseCardProgress = false;
        
        // 掉落惊喜三选一
        public void DropSurpriseChooseCardPanel(Vector3 pos)
        {
            _surpriseChooseCardProgress = true;

            float radius = 2f;
            float angle = Random.Range(0f, 2f * Mathf.PI); // 随机角度
            float distance = Random.Range(0f, radius); // 随机力度

            float x = pos.x + distance * Mathf.Cos(angle); // 随机x
            float y = pos.y + distance * Mathf.Sin(angle); // 随机y
            
            var pool = ObjectPoolManager.Instance.GetGameObjectPool("SurpriseBallEffect01");
            if (pool != null)
            {
                GameObject ballEffect = pool.Get();
                ballEffect.transform.position = pos;
                ballEffect.transform.localScale = Vector3.one * 1.5f;
                // ballEffect.transform.rotation= Quaternion.Euler(new Vector3(0,0, Random.Range(-180f, 180f)));
                ballEffect.transform.DOMove(new Vector3(x, y, 0f), 0.1f).SetEase(Ease.Linear).OnComplete(() =>
                {
                    DOTween.To(v => { }, 0, 0, 4f).SetEase(Ease.Linear).onComplete += () =>
                    {
                        pool.Return(ballEffect.gameObject);
                    };
                });
            }

            // 弹出三选一
            DOVirtual.DelayedCall(3f, () =>
            {
                try
                {
                    UIKit.OpenPanel<SurpriseChooseCardPanel>(UILevel.PopUI).SetData();
                }
                catch (Exception e)
                {
                    LogTool.LogError(e);
                }
            });
        }
        
        // 惊喜三选一结束
        public void SurpriseChooseCardOver()
        {
            StartGame();
            _surpriseChooseCardProgress = false;
        }

        // 开始下一波
        private void NextDay()
        {
            // 播放关闭宝箱特效
            TreasureChest.Default.CloseEffect();
            
            // 开始下一波
            EndDay();
            StartGame();
        }

        // 结束当前天数
        private void EndDay()
        {
            // 最后一天
            var lastCfg = _monsterGenCfg.StageList.Last();
            if (Global.Date.Value == lastCfg.Day) return;

            currentDateTime = 0;
            spawnedMonsterCounts.Clear();
            gameTime = 0f; // 重置时间
            spawnedWaves.Clear();
            spawnedMonsterPositions.Clear();
            nextWaveTime = 0;
            spawnedMonsterCount = 0; // 生成怪物数量
            deadCount = 0; // 换天重置击杀计数，避免跨天累加导致结算条件异常
            currentWaveIndex = 0; // 重置刷怪配置下标
            currentWaveSpawnDataIndex = 0;//重置刷怪第几个的下标
            Global.Date.Value++;
            _dayState = BattleDayState.Progress;
            _monsterGenDayList = _monsterGenCfg.StageList.FirstOrDefault(v => v.Day == Global.Date.Value);
            
            // 重置回合英雄受击次数
            foreach (var soldierCard in villageGameCards)
            {
                // 本回合没有受到攻击
                if (soldierCard.RoundBeHitCount == 0)
                {
                    // 英灵回合未受击加成主属性
                    var addAttr = soldierCard.BaseAttributes.GetFinal(AttributeType.SoldierRoundBeHitAddMainAttributes);
                    if (addAttr > 0f)
                    {
                        soldierCard.BaseAttributes.UpdateMainAttributeValue(MainAttributeType.Strength, addAttr,true);
                        soldierCard.BaseAttributes.UpdateMainAttributeValue(MainAttributeType.Dexterity, addAttr,true);
                        soldierCard.BaseAttributes.UpdateMainAttributeValue(MainAttributeType.Intelligence, addAttr,true);
                    }
                    continue;
                }

                soldierCard.RoundBeHitCount = 0;
            }

            // 监听条件 - 每生存天数
            AddListenerCondition(ConditionType.SurvivalDays, Global.Date.Value);
            LogTool.Log($"Day {Global.Date.Value - 1} ended. Next day: {Global.Date.Value}");
            if (Global.Date.Value == lastCfg.Day)
            {
                if (ChestRotaryTableLevel.Default && ChestRotaryTableLevel.Default.OpenCount > 0)
                {
                    return;
                }
                GamePlayManager.Default?.TriggerGuide(GuideType.FirstSpinPopUp); // 首次弹出转盘
            }
            
            // 祈祷被动：战中换波时触发 RoundStart（首日由 BattleRuleService 初始化补触发）
            if (isDayInProgress)
            {
                BattleRuleRoundStartTrigger.Trigger();
            }
        }

        #endregion


        #region 卡牌逻辑

        public void GetCard(int id, int level = 1, Vector3 initialPosition = default, Vector3 finialPosition = default)
        {
            var cardCfg = DataConfigManager.Tables.TbCardBaseConfig.GetOrDefault(id);
            if (cardCfg == null)
            {
                LogTool.LogError("ERROR: GetCard does not exist id: " + id);
                return;
            }

            // LogTool.Log(">>> GetCardId: " + id + ", level: " + level + ", cardType: " + cardCfg.Type);

            switch (cardCfg.Type)
            {
                case CardType.Soldier:
                {
                    var soldier = AddSoldierCard(id, level);
                    if (soldier != null)
                    {
                        InitializeNewSoldierSkills(soldier);
                    }

                    break;
                }
                case CardType.Monster:
                    AddMonsterCard(id, level, finialPosition);
                    break;
                default:
                    LogTool.LogError("GetCard is id " + id + ", undefined Type: " + cardCfg.Type);
                    break;
            }
        }

        /**
         * 添加角色卡
         */
        /// <summary>PVP：对侧新上场英灵补齐编队技能卡（主动/被动，ActsOnCard 匹配）。</summary>
        private void InitializeOpponentNewSoldierSkills(SoldierCard soldier)
        {
            if (soldier == null || BattleSession.Opponent == null)
            {
                return;
            }

            var skillTeams = BattleSession.Opponent.SkillTeams;
            if (skillTeams == null || skillTeams.Count == 0)
            {
                return;
            }

            foreach (var skillGroup in skillTeams)
            {
                if (skillGroup.CardId == 0)
                {
                    continue;
                }

                var specialCfg = DataConfigManager.Tables.TbSpecialEffectsConfig.GetOrDefault(skillGroup.CardId);
                if (specialCfg == null)
                {
                    continue;
                }

                if (specialCfg.ActsOnCard != 0 && specialCfg.ActsOnCard != soldier.cardId)
                {
                    continue;
                }

                soldier.LearnSkill(skillGroup.CardId, skillGroup.CardLevel);
                LogTool.Log($">>>>> PVP 对侧英灵学习技能: soldier={soldier.cardId} skill={skillGroup.CardId} lv={skillGroup.CardLevel}");
            }
        }

        public SoldierCard AddSoldierCard(int id, int level = 1, int posIndex = 0, SoldierRoleType roleType = 0, BattleSide side = BattleSide.Player)
        {
            bool isOpponent = side == BattleSide.Opponent;
            if (!isOpponent && Global.CityPopulation.Value >= Global.MaxCityPopulation.Value)
            {
                // LogTool.LogError("当前人口已达上限！");
                return null;
            }

            var positions = isOpponent ? opponentFriendlyPositions : friendlyPositions;
            if (positions == null || positions.Length == 0)
            {
                LogTool.LogError($"[GamePlayManager] 站位未就绪 side={side} cardId={id}");
                return null;
            }

            // 加载并实例化卡牌对象
            var soldier = _resLoader.Get<GameObject>("SoldierCard").Instantiate();
            var soldierTransform = soldier.transform;

            // 坐标位置小于0：按当前占位（含死亡单位）找空槽
            if (posIndex < 0)
            {
                posIndex = isOpponent
                    ? FindClosestOpponentPosition()
                    : FindClosestPosition2(friendlyPositions);
            }

            if (posIndex < 0 || posIndex >= positions.Length)
            {
                LogTool.LogWarning($"[GamePlayManager] 无可用站位，取消添加英灵: cardId={id} side={side}");
                Destroy(soldier);
                return null;
            }

            // 直接挂到对应 Pos 节点下，避免误用我方世界坐标
            var slotPos = positions[posIndex];
            soldierTransform.SetParent(slotPos, false);
            soldierTransform.localPosition = Vector3.zero;
            soldierTransform.localRotation = Quaternion.identity;
            soldier.LocalScale(new Vector3(0.65f, 0.65f, 0.65f));

            var soldierInit = soldier.GetComponent<SoldierCard>();
            soldierInit.posIndex = posIndex;
            soldierInit.BattleSide = side;

            if (!isOpponent)
            {
                Global.CityPopulation.Value++;
            }

            // 初始化卡牌属性
            soldierInit.Init(id, level, roleType);

            if (soldierInit.level.Value >= soldierInit.LevelScale.Length)
            {
                soldier.transform.localScale = new Vector3(0.65f, 0.65f, 0.65f) * soldierInit.LevelScale.Last();
                soldier.transform.Find("AttackRange").localScale = new Vector3(
                    1 / (soldier.transform.localScale.x * 1.5f), 1 / (soldier.transform.localScale.x * 1.5f),
                    1 / (soldier.transform.localScale.x * 1.5f));
            }
            else
            {
                soldier.transform.localScale = new Vector3(0.65f, 0.65f, 0.65f) *
                                               soldierInit.LevelScale[soldierInit.level.Value - 1];
                soldier.transform.Find("AttackRange").localScale = new Vector3(
                    1 / (soldier.transform.localScale.x * 1.5f), 1 / (soldier.transform.localScale.x * 1.5f),
                    1 / (soldier.transform.localScale.x * 1.5f));
            }

            soldier.GetComponent<Image>().color = new Color(0, 0, 0, 0);
            // 延时攻击
            ActionKit.Delay(2f, () => { soldierInit.UnlockAttackState(); }).Start(this);

            if (isOpponent)
            {
                opponentGameCards.Add(soldierInit);
                // PVP：新上场对侧英灵补上对侧 AI 已购买的三选一属性
                ApplyOpponentAiSchoolCardsToSoldier(soldierInit);
                // PVP：对侧养成属性（装备/奥数卡/奥数共鸣）生效
                ApplyOpponentBattleAttributesToSoldier(soldierInit);
                // PVP：对侧英灵补齐编队技能卡（否则无主动技能可用）
                InitializeOpponentNewSoldierSkills(soldierInit);
                LogTool.Log(
                    $"[GamePlayManager] 对侧英灵上场 cardId={id} pos={posIndex} parent={slotPos.name} path={GetTransformPath(slotPos)}");
            }
            else
            {
                villageGameCards.Add(soldierInit);
            }

            return soldierInit;
        }
        
        public void AddMonsterCard(int id, int level = 1, Vector3 finialPosition = default, float localScale = 1f, bool splitState = false, float splitHpScale = 0.5f, BattleSide side = BattleSide.Player, bool hasTransferred = false) 
        {
            // 加载并实例化卡牌对象
            var pool = ObjectPoolManager.Instance.GetGameObjectPoolOrCreate("MonsterCard1");
            if (pool != null)
            {
                var card = pool.Get();
                var cardTransform = card.transform;
                cardTransform.position = new Vector2(99999, 99999);;
                var monsterBattle = DataConfigManager.Tables.TbMonsterBattleConfig.GetOrDefault(id);
                card.GetComponent<MonsterCard>().pools = pool;

                var lane = GameLand.Default.GetLane(side);
                var parent = lane != null ? lane.GetMonsterParent(monsterBattle.MonsterQuality) : null;
                if (parent == null)
                {
                    switch (monsterBattle.MonsterQuality)
                    {
                        case MonsterQuality.Ordinary:
                            parent = GameLand.Default.OrdinaryMonster;
                            break;
                        case MonsterQuality.Elite:
                            parent = GameLand.Default.EliteMonster;
                            break;
                        case MonsterQuality.Boss:
                            parent = GameLand.Default.BossMonster;
                            break;
                    }
                }

                cardTransform.SetParent(parent);

                cardTransform.SetSiblingIndex(0);
                card.LocalScale(new Vector3(0.5f, 0.5f, 0.5f) * monsterBattle.SizeCorrection * localScale);
                // 对侧怪物出生点取该侧 MonsterPos，避免误用玩家侧坐标
                Vector3 spawnPosition;
                if (side == BattleSide.Opponent && lane != null && lane.MonsterPos != null)
                {
                    spawnPosition = lane.MonsterPos.position;
                }
                else
                {
                    spawnPosition = ordinaryPositions.Count > 0 ? ordinaryPositions.First().position : Vector3.zero;
                }

                // 设置怪物位置
                cardTransform.position = spawnPosition;
                // 初始化怪物的属性
                var monsterCard = card.GetComponent<MonsterCard>();
                monsterCard.BattleSide = side;
                monsterCard.HasTransferred = hasTransferred;
                monsterCard.Init(id, level, finialPosition, splitState, splitHpScale);

                if (hasTransferred)
                {
                    totalMonsterCount++;
                }
            }

        }

        /**
         * 添加技能卡
         */
        public void AddSkillCard(int id, int soldierId = 0, int specialLevel = 1, bool onceRelease = false)
        {
            var specialCfg = DataConfigManager.Tables.TbSpecialEffectsConfig.GetOrDefault(id);
            var isPassiveSkill = IsPassiveSkillConfig(specialCfg);
            foreach (var soldierCard in villageGameCards)
            {
                if (soldierId != 0 && soldierCard.cardId != soldierId) continue;
                soldierCard.LearnSkill(id, specialLevel);
            }

            if (isPassiveSkill)
            {
                RefreshPassiveSkillEffectsForSkill(id, soldierId);
            }
        }

        /**
         * 获取当前升级的士兵信息
         */
        public SoldierCard GetLevelUpSoldierCard()
        {
            return villageGameCards.FirstOrDefault(village => village.isLevelUp == true);
        }

        #endregion


        #region 建筑卡相关

        public BuildingCard FindCardById(int cardID)
        {
            return placedCards.FirstOrDefault(gameCard => gameCard.cardId == cardID);
        }

        #endregion

        #region 友方单位相关

        private int FindClosestPosition(Transform[] positions)
        {
            for (int i = 0; i < positions.Length; i++)
            {
                if (!IsPositionOccupied(positions[i].position, 0.5f) && friendlyPositionsLocked[i] == false) // 假设半径为 0.5
                {
                    friendlyPositionsLocked[i] = true;
                    return i;
                }
            }

            return Random.Range(0, positions.Length);
        }

        private int FindPositionByCardsPosition(List<int> cardsPosition, int soldierSlotIndex, Transform[] positions)
        {
            if (cardsPosition == null || cardsPosition.Count == 0 || positions == null || positions.Length == 0)
            {
                return -1;
            }

            int positionIndex = cardsPosition.IndexOf(soldierSlotIndex);
            if (positionIndex < 0 || positionIndex >= positions.Length)
            {
                return -1;
            }

            if (friendlyPositionsLocked[positionIndex] || IsPositionOccupied(positions[positionIndex].position, 0.5f))
            {
                return -1;
            }

            friendlyPositionsLocked[positionIndex] = true;
            return positionIndex;
        }
        
        private int FindClosestPosition2(Transform[] positions)
        {
            // 与升级补人共用占位规则（含死亡单位），避免镜像落到死亡槽或脏锁
            SyncFriendlyPositionLocksByOccupied(GetOccupiedFriendlyPositionIndices());

            for (int i = 0; i < positions.Length; i++)
            {
                if (friendlyPositionsLocked[i] == false)
                {
                    friendlyPositionsLocked[i] = true;
                    return i;
                }
            }

            return -1;
        }

        private bool IsPositionOccupied(Vector3 position, float radius)
        {
            // 使用物理检测方法查看指定位置是否有其他卡牌
            Collider2D[] colliders = Physics2D.OverlapCircleAll(position, radius);

            foreach (var collider in colliders)
            {
                // 检查碰撞体是否属于其他卡牌
                if (collider.name == "HitBox" && collider.GetComponentInParent<SoldierCard>() != null)
                {
                    return true; // 位置被占据
                }
            }

            return false; // 位置空闲
        }
        
        private SoldierCard GetPositionSoldier(Vector3 position, float radius)
        {
            // 使用物理检测方法查看指定位置是否有其他卡牌
            Collider2D[] colliders = Physics2D.OverlapCircleAll(position, radius);

            foreach (var collider in colliders)
            {
                // 检查碰撞体是否属于其他卡牌
                if (collider.name == "HitBox" && collider.GetComponentInParent<SoldierCard>() != null)
                {
                    return collider.GetComponentInParent<SoldierCard>();
                }
            }

            return null; 
        }

        #endregion


        private void OnDestroy()
        {
            ClearBattleLegacySkillState();
            ObjectPoolManager.Instance.DestroyGameObjectPool("DamagePool");
            ObjectPoolManager.Instance.DestroyGameObjectPool("pousui");
            ObjectPoolManager.Instance.DestroyGameObjectPool("pousui2");
            ObjectPoolManager.Instance.DestroyGameObjectPool("guaiwu_gongji_01");
            ObjectPoolManager.Instance.DestroyGameObjectPool("bullet");
            ObjectPoolManager.Instance.DestroyGameObjectPool("arrow");
            BattleAudioSystem.OnDisable();
            _skillConfigManager.OnDisable();
            _resLoader.ClearAll();
            _resLoader = null;
            Default = null;

        }


        #region 法力值

        /**
         * 更新法力值
         */
        public void UpdateGameMana(int manaNum, bool isCover = false)
        {
            // 使用 lock 语句确保线程安全
            lock (_lockObject)
            {
                Global.GameMana.Value = isCover ? manaNum : Global.GameMana.Value + manaNum;
                // 大于最大法力值直接溢出
                if (Global.GameMana.Value > Global.MaxGameMana.Value)
                {
                    Global.GameMana.Value = Global.MaxGameMana.Value;
                }

                ManaShowController.Instance.SetMana(Global.GameMana.Value);
                //LogTool.LogError(Global.GameMana.Value);
                LogTool.Log(">>>>>> 当前法力: " + $"{Global.GameMana.Value}, 操作数量: {manaNum}");
            }
        }

        /**
         * 更新法力碎片
         */
        public void UpdateGameManaFragment(int fragment)
        {
            // 使用 lock 语句确保线程安全
            lock (_lockObject)
            {
                // 取舍有道碎片加成
                fragment += ChoosingWiselyModule.GetExtraAddGameManaFragment();
                // 每秒卡能碎片恢复量
                fragment += (int)villageGameCards.First().BaseAttributes
                    .GetFinal(AttributeType.ManaFragmentRecoveryPerSecond);

                Global.GameManaFragment.Value += fragment;

                // 碎片合成
                var manaEntire = Mathf.FloorToInt((float)Global.GameManaFragment.Value / GameManaFragmentToEntire);
                if (manaEntire > 0)
                {
                    // LogTool.Log(">>>>>> 法力碎片合成数量: "+$"{manaEntire}");
                    UpdateGameMana(manaEntire);
                    Global.GameManaFragment.Value -= manaEntire * GameManaFragmentToEntire;
                }

                //ManaShowController.Instance.SetManaFragment(Global.GameManaFragment.Value);
                // LogTool.Log(">>>>>> 当前法力碎片: " + $"{Global.GameManaFragment.Value}");
                // 引导 - 首次恢复卡能值碎片
                TriggerGuide(GuideType.FirstRecoverManaFragment);
            }
        }

        /**
         * 时间恢复法力
         */
        private void CheckForRecoverGameMana()
        {
            recoverGameManaTime += Time.deltaTime * Global.SpeedMultiplier.Value;
            if (recoverGameManaTime >= recoverGameManaTimeLimit)
            {
                // LogTool.Log($">>>>>> 每隔: {recoverGameManaTimeLimit}/s, 恢复: {recoverGameManaNum}法力碎片");
                recoverGameManaTime = 0;
                UpdateGameManaFragment(recoverGameManaNum); // 恢复法力碎片
                ManaShowController.Instance.SetManaFragment(Global.GameManaFragment.Value);
            }
        }

        /**
         * 设置法力恢复速度
         */
        public void SetRecoverGameManaTimeLimit(float recoverTime)
        {
            recoverGameManaTimeLimit += recoverTime;
            if (recoverGameManaTimeLimit < 0.5f)
            {
                recoverGameManaTimeLimit = 0.5f;
            }
        }

        public int GetrecoverGameManaNum()
        {
            return recoverGameManaNum;
        }

        /**
         * 更新最大法力值
         */
        public void UpdateMaxGameMana(int maxManaNum, bool isCover = false)
        {
            if (maxManaNum == 0 && !isCover) return;

            // 使用 lock 语句确保线程安全
            lock (_lockObject)
            {
                Global.MaxGameMana.Value = isCover ? maxManaNum : Global.MaxGameMana.Value + maxManaNum;
                if (Global.MaxGameMana.Value < 0)
                {
                    Global.MaxGameMana.Value = 0;
                }

                // 取舍有道，法力限只有10
                if (ChoosingWiselyModule.Enabled)
                {
                    Global.MaxGameMana.Value = ChoosingWiselyModule.MaxGameMana;
                }

                // 当前法力值不能超过最大法力值
                if (Global.GameMana.Value > Global.MaxGameMana.Value)
                {
                    UpdateGameMana(Global.MaxGameMana.Value, true);
                }

                // ManaShowController.Instance.SetMaxMana(Global.MaxGameMana.Value);

                // 监听条件 - 拥有卡能值数量上限
                // AddListenerCondition(ConditionType.ManaNumMax, Global.MaxGameMana.Value);
                LogTool.Log(">>>>>> 当前最大法力: " + $"{Global.MaxGameMana.Value}, 操作数量: {maxManaNum}");
            }
        }

        #endregion


        #region 战内角色

        /**
         * 通过卡ID，获取游戏中的卡数据
         */
        public SoldierCard GetVillageGameCard(int cardId)
        {
            SoldierCard firstCard = null;
            foreach (var soldierCard in villageGameCards)
            {
                if (firstCard == null) firstCard = soldierCard;

                if (cardId != 0 && soldierCard.cardId != cardId)
                {
                    continue;
                }

                return soldierCard;
            }

            return firstCard;
        }

        /**
         * 遍历场上战士回调
         */
        public void TraverseVillageSoldier(Action<SoldierCard> callback)
        {
            for (int i = 0; i < villageGameCards.Count; i++)
            {
                callback(villageGameCards[i]);
            }
        }

        /**
         * 场上战士受击
         */
        public void VillageSoldierTaskDamage(int damageVal, MonsterCard monsterCard)
        {
            // 场上存活战士平分伤害
            var bisectDamage = Mathf.CeilToInt(damageVal / (float)villageGameCards.Count);

            // 角色受击 
            foreach (var v in villageGameCards)
            {
                if (bisectDamage <= 0 || v.IsDie()) continue;
                if (v.CanReceiveIncomingDamage(bisectDamage))
                {
                    var result = DamageContextBuilder.CalculateIncoming(
                        v, monsterCard, TakeDamageType.PhysicalDamage, bisectDamage);
                    bisectDamage = v.TakeDamage(result);
                }
            }
        }

        /**
         * 复活战士
         */
        public void ReviveSoldierCard()
        {
            foreach (var v in villageGameCards)
            {
                v.ReviveSoldier();
            }
        }

        #endregion


        #region 生效条件

        private Dictionary<int, ConditionProgress> conditionDictionary = new Dictionary<int, ConditionProgress>();

        /**
         * 添加监听条件
         */
        public void AddListenerCondition(ConditionType condType, int num, int condParam = 0)
        {
            var conditionConfig = DataConfigManager.Tables.TbConditionConfig.DataList;
            foreach (var cfg in conditionConfig)
            {
                if (condType != cfg.ConditionType) continue;
                conditionDictionary.TryAdd(cfg.Id, new ConditionProgress(cfg.Id, cfg));

                // 获取条件进度数据
                var conditionProgress = conditionDictionary[cfg.Id];

                // 添加进度 & 返回是否完成
                conditionProgress.AddProgress(num);
                if (conditionProgress.CompletedRound() > 0)
                {
                    CompleteCondition(cfg); // 完成条件调用函数
                }
            }
        }

        /**
         * 条件完成
         */
        private void CompleteCondition(ConditionConfig cfg)
        {
            // CompleteSchoolCardCondition(cfg.Id);
        }

        #endregion


        #region BUFF

        /**
         * 获取Buff生效属性
         */
        public Dictionary<int, float> GetBuffEffectProperty()
        {
            var property = new Dictionary<int, float>();
            if (BuffCards.Count > 0)
            {
                // 记录要移除的键
                List<int> keysToRemove = new List<int>();
                foreach (var v in BuffCards)
                {
                    var id = v.Key;
                    var buff = v.Value;
                    if (!buff.CheckExpireTime())
                    {
                        keysToRemove.Add(id);
                        continue;
                    }

                    var cfg = DataConfigManager.Tables.TbPropertyConfig.GetOrDefault(id);
                    property.Add(cfg.Id, cfg.CorrectionValue);
                }

                // 遍历结束后移除记录的键
                foreach (var key in keysToRemove)
                {
                    BuffCards.Remove(key);
                }
            }

            return property;
        }

        private void AddBuffEffectProperty(List<int> ids, int second)
        {
            foreach (var id in ids)
            {
                var cfg = DataConfigManager.Tables.TbPropertyConfig.GetOrDefault(id);
                BuffCards.TryAdd(id, new BuffCard());
                BuffCards[id].SetBuffEffect(id, cfg.CorrectionValue, second);
            }
        }

        #endregion

        public bool GuideIsRunning()
        {
        
            if (!_isStageGuide) return false;

            var cfg = DataConfigManager.Tables.TbGuideConfig.DataList.FirstOrDefault(
                v => v.Type == GuideType.GuideStage);
            if (cfg == null || Global.Player.CheckGuideStatus(cfg.Id)) return false;

            return true;
        }
        public void TriggerGuide(GuideType type)
        {
        
            if (!_isStageGuide) return;

            var cfg = DataConfigManager.Tables.TbGuideConfig.DataList.FirstOrDefault(
                v => v.Type == GuideType.GuideStage);
            if (cfg == null || Global.Player.CheckGuideStatus(cfg.Id)) return;

            var stepCfg = cfg.StepList.FirstOrDefault(v => v.Type == type);
            if (stepCfg == null) return;
            GuideManager.Instance.Trigger(type);
            Global.Publisher.Publish(MessagePublisher.TRIGGER_GUIDE_TIP, stepCfg.Step);
        }
        
        /**
         * 设置无CDTime限制
         *
         * @return
         * @author Zpp
         * @create 2026/1/6
         **/
        public void UnlimitedCdTime() 
        {
            Global.UnlimitedCdTime.Value = true;
            foreach (var skillId in GameLand.Default.skill.Keys)
            {
                GameLand.Default.AddSkillCurrentCooldownTime(skillId, -100);
            }
        }

        // 替换原来的4个私有字段，使用列表存储所有特效
        private List<(GenericObjectPool pool, GameObject obj)> _activeEffects = new();
        
        public void DetectAdjacentSoldier()
        {
            // 清理所有已存在的特效（而不是只清理最后两个）
            foreach (var (pool, obj) in _activeEffects)
            {
                if (obj != null && pool != null)
                    pool.Return(obj);
            }
            _activeEffects.Clear();
        
            var adjacentSoldierCard = new List<SoldierCard>();
            for (int i = 0; i < friendlyPositions.Length; i++)
            {
                var soldierCard = GetPositionSoldier(friendlyPositions[i].position, 0.2f);
                adjacentSoldierCard.Add(soldierCard);
                if (soldierCard != null)
                {
                    soldierCard.BaseAttributes.GetAttr(AttributeType.SoldierTakeDamage).AddModifier(
                        new AttributeModifier
                        {
                            SourceType = AttributeSourceType.GeneralSkill,
                            SourceKey = "AdjacentDamageReduction",
                            PercentAdd = 0
                        });
                    
                    // soldierCard.MainAttributes.GetAttr(soldierCard.MainAttrType).AddModifier(new AttributeModifier()
                    // {
                    //     SourceKey = "AdjacentMainStatBonus",
                    //     SourceType = AttributeSourceType.GeneralSkill,
                    //     FlatAdd = 0,
                    //     PercentAdd = 0,
                    // });

                }
            }

            // 当英灵站位相邻时，获得30%的伤害减免
            var adjacentDamageReduction = ModuleAttributes.GetFinal(AttributeType.AdjacentDamageReduction);
            // 当英灵站位相邻且相同主属性时,主属性增加25% 
            var adjacentMainStatBonus = ModuleAttributes.GetFinal(AttributeType.AdjacentMainStatBonus);
            // 当英灵周围无英灵时,主属性增加20%,受到伤害20%
            var soloStatBonus = ModuleAttributes.GetFinal(AttributeType.SoloStatBonus);
            var cfg = DataConfigManager.Tables.TbAttributeConfig.GetOrDefault(AttributeType.SoloStatBonus);
            

            for (int i = 0; i < adjacentSoldierCard.Count - 1; i++)
            {
                // 获取当前下标i的元素值
                var currentValue = adjacentSoldierCard[i];
                // 获取相邻的下一个下标i+1的元素值
                var nextValue = adjacentSoldierCard[i + 1];

                // 当英灵站位相邻时，获得30%的伤害减免
                if (adjacentDamageReduction > 0f && currentValue != null && nextValue != null)
                {
                    currentValue.BaseAttributes.GetAttr(AttributeType.SoldierTakeDamage).AddModifier( new AttributeModifier
                    {
                        SourceType = AttributeSourceType.GeneralSkill,
                        SourceKey = "AdjacentDamageReduction",
                        PercentAdd = adjacentDamageReduction
                    });
                
                    // 添加到列表而不是赋值给单个变量
                    var effect1 = ObjectPoolManager.Instance.PlayGameObjectPool(new GameObjectPoolData()
                    {
                        PoolName = $"EliaSkillMoonshadowWard01",
                        ParentTran = currentValue.transform,
                        Position = currentValue.transform.position,
                        LocalScale = 160f,
                        DelayReturn = 0f
                    });
                    _activeEffects.Add(effect1);
                
                    nextValue.BaseAttributes.GetAttr(AttributeType.SoldierTakeDamage).AddModifier( new AttributeModifier
                    {
                        SourceType = AttributeSourceType.GeneralSkill,
                        SourceKey = "AdjacentDamageReduction",
                        PercentAdd = adjacentDamageReduction
                    });
                
                    // 添加到列表而不是赋值给单个变量
                    var effect2 = ObjectPoolManager.Instance.PlayGameObjectPool(new GameObjectPoolData()
                    {
                        PoolName = $"EliaSkillMoonshadowWard01",
                        ParentTran = nextValue.transform,
                        Position = nextValue.transform.position,
                        LocalScale = 160f,
                        DelayReturn = 0f
                    });
                    _activeEffects.Add(effect2);
                }

                // 当英灵站位相邻且相同主属性时
                if (adjacentMainStatBonus > 0f && currentValue != null && nextValue != null && currentValue.MainAttrType == nextValue.MainAttrType)
                {
                    
                    // currentValue.MainAttributes.GetAttr(currentValue.MainAttrType).AddModifier(new AttributeModifier()
                    // {
                    //     SourceKey = "AdjacentMainStatBonus",
                    //     SourceType = AttributeSourceType.GeneralSkill,
                    //     FlatAdd = 0,
                    //     PercentAdd = adjacentMainStatBonus,
                    // });
                    // 特效1
                    var effect1 = ObjectPoolManager.Instance.PlayGameObjectPool(new GameObjectPoolData()
                    {
                        PoolName = $"EliaSkillMoonshadowWard01",
                        ParentTran = currentValue.transform,
                        Position = currentValue.transform.position,
                        LocalScale = 160f,
                        DelayReturn = 0f
                    });
                    _activeEffects.Add(effect1);
                    
                    // nextValue.MainAttributes.GetAttr(currentValue.MainAttrType).AddModifier(new AttributeModifier()
                    // {
                    //     SourceKey = "AdjacentMainStatBonus",
                    //     SourceType = AttributeSourceType.GeneralSkill,
                    //     FlatAdd = 0,
                    //     PercentAdd = adjacentMainStatBonus,
                    // });
                    // 特效2
                    var effect2 = ObjectPoolManager.Instance.PlayGameObjectPool(new GameObjectPoolData()
                    {
                        PoolName = $"EliaSkillMoonshadowWard01",
                        ParentTran = nextValue.transform,
                        Position = nextValue.transform.position,
                        LocalScale = 160f,
                        DelayReturn = 0f
                    });
                    _activeEffects.Add(effect2);
                }

                //奥术卡:孤身领域 激活
                if (soloStatBonus > 0f)
                {
                    // 检查左边是否有英灵（i>0时检查前一个元素）
                    bool hasLeftNeighbor = (i > 0) && (adjacentSoldierCard[i - 1] != null);
                    // 检查右边是否有英灵（nextValue即i+1位置）
                    bool hasRightNeighbor = (nextValue != null);
    
                    // 周围一格内无其它英灵（左右都没有）
                    if (!hasLeftNeighbor && !hasRightNeighbor)
                    {
                        // AttributeManager.SetAttributesLogic();
                    }
                    
                }
                
            }
        }

    }
}