using System;
using System.Collections.Generic;
using Config;
using DG.Tweening;
using UnityEngine;
using QFramework;
using UnityEngine.UI;
using Random = UnityEngine.Random;
using Sequence = DG.Tweening.Sequence;
using System.Collections;
using System.Linq;
using FusionContinent.Attribute;
using FusionContinent.Buff;
using FusionContinent.Guide;
using FusionContinent.Monster.AttackBehaviour;
using FusionContinent.Monster.MoveBehaviour;
using FusionContinent.Skill;
using FusionContinent.Skill.Handler;
using FusionContinent.Sounds;
using FusionContinent.Utils;
using Game.Battle;
using Game.StateMachine;
using Manager;
using Spine;
using Spine.Unity;
using TMPro;
using YooAsset;
using ValueType = Config.ValueType;

namespace FusionContinent
{
    public partial class MonsterCard : ViewController, IBuffable
    {
        public int _cardId;
        public int _level;
        public string uniqueId;
        private MonsterQuality selfQuality;
        public int atk;
        public int hp;
        public int maxHp;
        public float attackTime;
        private float attackRange;
        public float moveSpeed;
        private int[] provideExp;
        private float dropGold;
        private float growthCoins;
        public string HeadNameDefault = "MushroomHead";

        /// <summary>所属战场侧（pve=Player / pvp=Opponent）。</summary>
        public BattleSide BattleSide = BattleSide.Player;

        /// <summary>是否已由击杀转移生成；为 true 时死亡不再二次转移。</summary>
        public bool HasTransferred;

        public Transform SortTransform;
        public Renderer[] Renderers;
        
        private List<string> _handlingTags = new List<string>();

        [HideInInspector] public Vector3 enemyAtkPos = new(-1, -1f, -1); // 怪物攻击点
        private TbCardBaseConfig _tbCardBaseConfig;
        public Vector3 selfScale;
        private Vector3 originalPosition;
        private Tween damageTween;
        private Tween damageImgTween;

        public delegate void OnDestroyed(MonsterCard card);

        public GenericObjectPool pools;
        public static event OnDestroyed onDestroyed;

        public float damageTime;

        public Transform uiElement; // 需要移动的 UI 元素
        public float JumpmoveDistance = 1f; // 移动的距离
        public float JumpDownmoveDistance = 2f; // 移动的距离
        public float JumpDuration = 1f; // 移动的持续时间
        public float JumpDownDuration = 0.8f;
        public GameObject Healtheffect;
        private GameObject _prefabHealtheffect;
        private MonsterHealthBar _sceneHealthBar;
        private GameObject _prefabIconEffects;
        private GameObject _prefabDisarmIcon;
        private GameObject _prefabExecutionIcon;
        private GameObject _prefabShieldBreakIcon;
        private MonsterIconEffects _sceneIconEffects;
        public float DealyTime = 2;
        public Transform ShadowEnem;
        public bool Iswall;
        private List<Sprite> sprites = new List<Sprite>();
        List<Transform> burrowposs = new List<Transform>();
        public TbMonsterBattleConfig _tbMonsterBattleConfig;
        private TbItemConfig _tbItemConfig;
        public MonsterBattleConfig _monsterConfig;
        private Dictionary<string, GameObject> _spineObjs = new Dictionary<string, GameObject>();
        public bool IsBoos;
        
        // 怪物属性
        public AttributeSet Attributes = new();
        public MonsterAttribute MonsterAttribute = new();
        private MonsterAttribute _cachedMonsterAttribute = null;

        public MonsterData monsterData = new();

        public MonsterAnimationController AnimationController { get; private set; }

        public FusionContinent.Monster.Animation.IMonsterAnimationPlayer AnimationPlayer =>
            AnimationController.Player;

        // Buff控制器
        public BuffController BuffController { get; private set; }

        private RectTransform _staggerBarRect;
        private RectTransform _staggerBarParentRect;
        private Image _staggerBarImage;
        private Image _staggerBarEffectImage;
        private Tween _staggerBarTween;
        private Tween _staggerBarEffectTween;
        private Tween _healthBarTween;
        private Tween _healthBarEffectTween;
        private bool _hasReportedDefeat;

        /// <summary>
        /// 怪物根节点 Kinematic Rigidbody2D。
        /// HitBox 作为子碰撞体挂在其上，位移必须走 MovePosition / position，避免静态碰撞体每帧 teleport。
        /// </summary>
        private Rigidbody2D _rigidbody2D;

        public RectTransform StaggerBarRect => _staggerBarRect;
        public RectTransform StaggerBarParentRect => _staggerBarParentRect;
        public Image StaggerBarEffectImage => _staggerBarEffectImage;

        public DamageContext damageContext = new();  //伤害上下文

        
        #region 状态机信息
        // 添加状态机
        private StateMachine _stateMachine;
        
        // 预创建的状态实例（供CheckTransition使用）
        public MonsterIdleState monsterIdleState { get; private set; }
        public MonsterMoveState monsterMoveState { get; private set; }
        public MonsterEngageState monsterEngageState { get; private set; }
        public MonsterHurtState monsterHurtState { get; private set; }
        public MonsterDeadState monsterDeadState { get; private set; }
        public MonsterWeakState monsterWeakState {get; private set; }

        public MonsterSpecialState monsterSpecialState { get; private set; }
        
        private void InitializeStateMachine()
        {
            _stateMachine = new StateMachine();

            // 创建各个状态
            monsterIdleState = new MonsterIdleState(_stateMachine, this);
            monsterMoveState = new MonsterMoveState(_stateMachine, this);
            monsterEngageState = new MonsterEngageState(_stateMachine, this);
            monsterHurtState = new MonsterHurtState(_stateMachine, this);
            monsterDeadState = new MonsterDeadState(_stateMachine, this);
            monsterWeakState = new MonsterWeakState(_stateMachine,this);
            monsterSpecialState = new MonsterSpecialState(_stateMachine, this);
            
            // Idle 状态转换
            monsterIdleState.AddTransition(new Transition(() => monsterData.monsterState == MonsterState.move, monsterMoveState));
            monsterIdleState.AddTransition(new Transition(() => monsterData.monsterState == MonsterState.engage, monsterEngageState));
            monsterIdleState.AddTransition(new Transition(() => monsterData.monsterState == MonsterState.weak, monsterWeakState));
            monsterIdleState.AddTransition(new Transition(() => monsterData.monsterState == MonsterState.specialMove, monsterSpecialState));
            
            // Move 状态转换
            monsterMoveState.AddTransition(new Transition(() => monsterData.monsterState == MonsterState.engage, monsterEngageState));
            monsterMoveState.AddTransition(new Transition(() => monsterData.monsterState == MonsterState.die, monsterDeadState));
            monsterMoveState.AddTransition(new Transition(() => monsterData.monsterState == MonsterState.weak, monsterWeakState));
            monsterMoveState.AddTransition(new Transition(() => monsterData.monsterState == MonsterState.hurt, monsterHurtState));
            monsterMoveState.AddTransition(new Transition(() => monsterData.monsterState == MonsterState.specialMove, monsterSpecialState));
            
            // Engage 就是attack状态转换
            //攻击状态可以僵直么
            monsterEngageState.AddTransition(new Transition(() => monsterData.monsterState == MonsterState.move, monsterMoveState));
            monsterEngageState.AddTransition(new Transition(() => monsterData.monsterState == MonsterState.die, monsterDeadState));
            monsterEngageState.AddTransition(new Transition(() => monsterData.monsterState == MonsterState.weak, monsterWeakState));
            // monsterEngageState.AddTransition(new Transition(() => monsterData.monsterState == MonsterState.hurt, monsterHurtState));
            monsterEngageState.AddTransition(new Transition(() => monsterData.monsterState == MonsterState.specialMove, monsterSpecialState));
            
            // Hurt 状态转换
            monsterHurtState.AddTransition(new Transition(() => monsterData.monsterState == MonsterState.engage, monsterEngageState));
            monsterHurtState.AddTransition(new Transition(() => monsterData.monsterState == MonsterState.move, monsterMoveState));
            monsterHurtState.AddTransition(new Transition(() => monsterData.monsterState == MonsterState.die, monsterDeadState));
            monsterHurtState.AddTransition(new Transition(() => monsterData.monsterState == MonsterState.weak, monsterWeakState));
            monsterHurtState.AddTransition(new Transition(() => monsterData.monsterState == MonsterState.specialMove, monsterSpecialState));
            
            // Weak 状态转换
            monsterWeakState.AddTransition(new Transition(() => monsterData.monsterState == MonsterState.move, monsterMoveState));
            monsterWeakState.AddTransition(new Transition(() => monsterData.monsterState == MonsterState.die, monsterDeadState));

            monsterSpecialState.AddTransition(new Transition(() => monsterData.monsterState == MonsterState.die, monsterDeadState));
            monsterSpecialState.AddTransition(new Transition(() => monsterData.monsterState == MonsterState.move, monsterMoveState));
            monsterSpecialState.AddTransition(new Transition(() => monsterData.monsterState == MonsterState.engage, monsterEngageState));
            
            
            // 设置初始状态
            _stateMachine.CurrentState = monsterIdleState;
            monsterData.monsterState = MonsterState.move;
        }
        #endregion 状态机信息

        private void OnEnable()
        {
            _cachedMonsterAttribute = MonsterAttribute;
            _hasReportedDefeat = false;
            IsInit = false;
            Iswall = false;
            IsBoos = false;
            IsModerateState = false;
            IsSpine = false;
            colorTween = null;
            diecount = 0;
            _isSplitBody = false;
            _continuousDamageNum = 0;
            MonsterManager.Instance.Register(this);
            // hasEngagedTarget = false;
            MonsterSortManager.Instance.ResetSortGate();
            MonsterSortManager.Instance.RequestSort(0.05f, this);

            if (AnimationController.HasSkeletonAnimation)
            {
                originalColor = AnimationController.GetSkeletonColor();
                AnimationController.SetSkeletonColor(originalColor);
            }

            AnimationController.ResetSpineContentLocalPosition();
        }

        private void OnDisable()
        {
            if (AttackBehaviour != null)
            {
                AttackBehaviour.OnCleanup(this);
            }

            if (lvdoutween != null)
            {
                lvdoutween.Kill();
                lvdoutween = null;
            }

            if (damageTween != null)
            {
                damageTween.Kill();
                damageTween = null;
            }

            KillColorTween();
            DOTween.Kill(transform);
            StopAllCoroutines();
            CancelInvoke();
            ReportEnemyDefeatedIfNeeded();
            if (AnimationController.HasSkeletonAnimation)
            {
                AnimationController.SetSkeletonColor(Color.white);
            }

            AnimationController.UnregisterAllSpineCallbacks();
            AnimationController.ResetForPool();
            AnimationController.ReleasePlayer();

            if (_continuousDamageTween != null)
            {
                _continuousDamageTween.Kill();
                _continuousDamageTween = null;
            }

            _staggerBarTween?.Kill();
            _staggerBarEffectTween?.Kill();
            _healthBarTween?.Kill();
            _healthBarEffectTween?.Kill();

            BuffController?.ClearAll();
            CleanupMechanics();

            MonsterManager.Instance.Unregister(this);
            ReleaseSceneHealthBar();
            ReleaseSceneIconEffects();
            sprites.Clear();
            DOTween.Kill(transform, true);
            SetPhysicsPosition(new Vector3(99999, 99999, 20));
        }


        public void UpdateMove()
        {
            moveSpeed = Attributes.GetFinal(AttributeType.MonsterMoveSpeed) / 100;
            JumpDownmoveDistance *= moveSpeed;
            if (monsterData.monsterState == MonsterState.move)
            {
                Tick();
                if (_stateMachine != null && _stateMachine.CurrentState == monsterMoveState)
                {
                    MoveBehaviour.StopMove();
                    monsterMoveState.Enter();
                }
            }
        }

        private void Awake()
        {
            EnsurePhysicsBody();

            _tbItemConfig = DataConfigManager.Tables.TbItemConfig;
            _tbCardBaseConfig = DataConfigManager.Tables.TbCardBaseConfig;
            _tbMonsterBattleConfig = DataConfigManager.Tables.TbMonsterBattleConfig;

            AnimationController = new MonsterAnimationController();
            AnimationController.AwakeCache(this);

            // Healtheffect / IconEffects 已外移到 GameLandUI；预制体字段可能已解绑，仅关闭仍残留的旧节点
            _prefabHealtheffect = Healtheffect;
            if (_prefabHealtheffect == null)
            {
                Transform prefabHealtheffect = transform.Find("Canvas/Healtheffect");
                if (prefabHealtheffect != null)
                {
                    _prefabHealtheffect = prefabHealtheffect.gameObject;
                }
            }

            if (_prefabHealtheffect != null)
            {
                _prefabHealtheffect.SetActive(false);
            }

            _prefabDisarmIcon = DisarmIcon;
            _prefabExecutionIcon = ExecutionIcon;
            _prefabShieldBreakIcon = ShieldBreakIcon;
            if (DisarmIcon != null)
            {
                _prefabIconEffects = DisarmIcon.transform.parent.gameObject;
            }
            else
            {
                Transform prefabIconEffects = transform.Find("Canvas/IconEffects");
                if (prefabIconEffects != null)
                {
                    _prefabIconEffects = prefabIconEffects.gameObject;
                }
            }

            if (_prefabIconEffects != null)
            {
                _prefabIconEffects.SetActive(false);
            }

            transform.localScale = Vector3.one * 0.5f;
        }


        public RunStage runStage = new RunStage();
        Vector3 iconattack = Vector3.one;
        private QFramework.IUnRegister speedMultiplierRegistration; // 使用正确的类型

        public bool _isSplitBody = false;// 是否为分裂体
        private float _splitBodyVal = 0.5f;// 分裂体数值

        public SoldierCard targetSolidierCard;

        /// <summary>
        /// 移动行为策略，由 MonsterMoveBehaviourFactory 在 Init 时按 MoveType 创建。
        /// 位移在 FixedMoveTick 中更新，状态机在 Tick 中更新。
        /// </summary>
        public IMonsterMoveBehaviour MoveBehaviour;

        public IAttackBehaviour AttackBehaviour { get; private set; }
        
        
        /**
         * 初始化怪物
         */
        public void Init(int id, int cardLevel, Vector3 initPos = default, bool isSplitBody = false, float splitHpScale = 0.5f)
        {
            _cardId = id;
            _level = cardLevel;
            _isSplitBody = isSplitBody;
            _splitBodyVal = splitHpScale;
            _monsterConfig = _tbMonsterBattleConfig.GetOrDefault(_cardId);
            targetSolidierCard = null;

            // 对象池复用：清除上一局残留的 Buff（灼烧等 AbnormalBuff）
            if (BuffController == null)
                BuffController = new BuffController(this);
            else
                BuffController.ClearAll();

            GetComponent<MonsterAbnormal>()?.ResetAllAbnormalState();

            if (_monsterConfig == null)
            {
                throw new Exception($"怪物[{Global.StageId.Value}]，配置不存在");
            }

            uniqueId = Guid.NewGuid().ToString(); // 生成唯一Id
            monsterData.SuppressDeathLoot = false;
            selfScale = transform.localScale;
            _handlingTags.Clear();

            // Spine
            Transform spineContent = AnimationController.SpineContent;
            for (int i = spineContent.childCount - 1; i >= 0; i--)
            {
                spineContent.GetChild(i).gameObject.SetActive(false);
            }

            if (_spineObjs.TryGetValue(_monsterConfig.Spine, out GameObject spine))
            {
                spine.SetActive(true);
            }
            else
            {
                //这里去预加载一下，从对象池中取
                spine = Global.ResLoader.LoadSync<GameObject>(_monsterConfig.Spine).Instantiate();
                _spineObjs.Add(_monsterConfig.Spine, spine);
            }

            spine.name = _monsterConfig.Spine;
            spine.transform.SetParent(spineContent);
            spine.transform.localScale =
                _monsterConfig.SpineScale > 0 ? Vector3.one * _monsterConfig.SpineScale : Vector3.one;
            spine.transform.localPosition = new Vector3(0, 0, 0);
            spine.transform.GetChild(0).localPosition = new Vector3(0, 0, 0);
            AnimationController.RebindActiveSpine(spine.transform, _monsterConfig.Spine, _monsterConfig.MonsterQuality);

            AttackBehaviour = MonsterAttackBehaviourFactory.Create(_monsterConfig);
            AttackBehaviour.OnInit(this);

            // 怪物出场
            MonsterCreate(initPos);

            // 设置基础信息
            SetBaseInfo();
            // 等级配置
            var levelCfg = _monsterConfig.LevelPropertyList[cardLevel - 1];
            
             // 初始配置属性
            Attributes = new();
            foreach (var attr in levelCfg.Attributes)
            {
                var addVal = isSplitBody ? Mathf.FloorToInt(attr.Value * _splitBodyVal) : attr.Value;
                if (attr.EffectParam != 0f)
                {
                    // 带效果参数的属性，作为 Modifier 添加
                    Attributes.GetAttr(attr.Type).AddModifier(new AttributeModifier
                    {
                        SourceKey = "InitMonster",
                        SourceType = AttributeSourceType.Base,
                        FlatAdd = attr.ValueType == ValueType.Number ? addVal : 0,
                        PercentAdd = attr.ValueType == ValueType.Percentage ? addVal : 0,
                        EffectParam = attr.EffectParam,
                        ProbabilityTrigger = attr.ProbabilityTrigger
                    });
                }
                else
                {
                    // 基础值
                    Attributes.GetAttr(attr.Type).SetBaseValue(addVal);
                }
            }
            
            
            MonsterInit.Init(this); //初始话新的一些数据
            // 获取攻击相关配置
            attackTime = 1.0f / (_monsterConfig.AttackTime > 0 ? _monsterConfig.AttackTime : 1.0f);
            attackRange = _monsterConfig.AttackRange > 0 ? _monsterConfig.AttackRange / 100 : 1.0f;
            moveSpeed = _monsterConfig.MoveSpeed >= 0 ? _monsterConfig.MoveSpeed / 100 : 0;
            provideExp = _monsterConfig.ProvideExp;
            dropGold = _monsterConfig.DropGold;
            growthCoins = _monsterConfig.GrowthCoins;
            JumpDownmoveDistance =
                Random.Range(_monsterConfig.JumpDistance[0],
                    _monsterConfig.JumpDistance[1]);
            DealyTime = Random.Range(
                _monsterConfig.RetentionTime[0],
                _monsterConfig.RetentionTime[1]);
            selfQuality = _monsterConfig.MonsterQuality;
            if (selfQuality == MonsterQuality.Boss)
            {
                GameObject bossComingEffect = Instantiate(
                    YooAssets.LoadAssetSync<GameObject>("bossyujing_01").GetAssetObject<GameObject>(),
                    transform.position, Quaternion.identity);
                bossComingEffect.transform.SetParent(transform.parent.parent.parent);
                bossComingEffect.transform.localPosition = Vector3.zero;
                SoundManager.Instance.PlayVoice(SoundType.SoundBossComing);
                Destroy(bossComingEffect, 1.8f);
            }

            selfScale = transform.localScale;

            // 碰撞盒
            HitBox.gameObject.SetActive(true);
            HitBox.Initialize(this);
            HitBox.transform.localScale = new Vector3(_monsterConfig.HitBoxScale.X, _monsterConfig.HitBoxScale.Y, 1);
            // HitBox.transform.localPosition
            AttackRange.gameObject.SetActive(true);

           

            // 初始攻击力
            atk = levelCfg.Atk;

            //设置双抗数值
            var cfg = _tbMonsterBattleConfig.GetOrDefault(_cardId).LevelPropertyList[cardLevel - 1];
            MonsterAttribute.Physical_Resistance = cfg.Armor;
            MonsterAttribute.Mana_Resistance = cfg.MagicResistance;

           

            // 外部模块聚合属性加成
            foreach (var attr in GamePlayManager.Default.MonsterAttributes.GetAll())
            {
                foreach (var mod in attr.Value.GetModifiers().Values)
                {
                    Attributes.GetAttr(attr.Key).AddModifier(mod, true);
                }
            }

            atk = (int)Attributes.GetFinal(AttributeType.MonsterAttackPower);
            attackTime = 1.0f / Attributes.GetFinal(AttributeType.MonsterAttackSpeed);
            attackRange = Attributes.GetFinal(AttributeType.MonsterAttackRange) / 100;
            moveSpeed = Attributes.GetFinal(AttributeType.MonsterMoveSpeed) / 100;

            var monsterAttribute = MonsterAttribute;
            _cachedMonsterAttribute = monsterAttribute;
            monsterAttribute.Physical_Resistance = Attributes.GetFinal(AttributeType.MonsterPhysicalResistance); // 物抗
            monsterAttribute.Mana_Resistance = Attributes.GetFinal(AttributeType.MonsterMagicResistance); // 魔抗

            // 初始血量
            hp = levelCfg.Hp;
            hp = (int)Attributes.GetFinal(AttributeType.MonsterMaxHealth);
            maxHp = hp;
            uiElement = transform;

            var missNum = Attributes.GetFinal(AttributeType.MonsterMissNum);

            //RegisterEnemyAI();
            if (_monsterConfig.MonsterQuality is MonsterQuality.Elite or MonsterQuality.Boss)
            {
                IsBoos = true;
                BindSceneHealthBar();
                HealthInit();
            }

            BindSceneIconEffects();

            JumpDownmoveDistance *= moveSpeed;
            ShadowEnem.transform.localPosition = new Vector3(0, _monsterConfig.Shadow, 0);
            ShadowEnem.transform.localScale = Vector3.one *
                                              (_monsterConfig.ShadowSize == 0 ? 1 : _monsterConfig.ShadowSize);

            //SpineWalk();
            AttackRange.Initialize(this, attackRange);
            AttackBehaviour.OnSpawnPresentation(this);

            // 移动动画
            if (runStage != RunStage.Motionless)
            {
                Play("walk", true);
            }



            SortTransform = transform;
            Renderers = new Renderer[] { AnimationController.GetMeshRenderer() };

            speedMultiplierRegistration = Global.SpeedMultiplier.RegisterWithInitValue(speed =>
            {
                // 检查 MonsterCard 是否已被销毁
                if (this == null || gameObject == null) return;
                var monsterAbnormal = GetComponent<MonsterAbnormal>();
                if (monsterAbnormal == null) return;

                AnimationController.SetTimeScale(1 *
                                                                    speed *
                                                                    monsterAbnormal.monsterspeed *
                                                                    (monsterAbnormal.vertigo ? 0 : 1)
                                                                    * _tbMonsterBattleConfig.GetOrDefault(_cardId)
                                                                        .MoveAnimationSpeed);
            });

            MoveBehaviour = MonsterMoveBehaviourFactory.Create(_monsterConfig.MoveType);

            InitializeMechanics();

            // 初始化状态机
            InitializeStateMachine();
            if (_monsterConfig.MoveType == MoveType.Motionless)
            {
                // isTarget = true; // 静止不动的怪物，直接开始攻击
                monsterData.monsterState = MonsterState.engage;
                //
            }
            
        }

        //索敌 开局索敌一次，然后人物死亡重新索敌
        public void EngageSoldierCard(Vector3 initPos)
        {
            float distance = 9999;
            // PVP：按所属侧索敌，对侧怪锁定对侧英灵（opponentGameCards），避免回头打我方
            var candidates = BattleSide == BattleSide.Opponent
                ? GamePlayManager.Default.opponentGameCards
                : GamePlayManager.Default.villageGameCards;
            if (candidates == null)
            {
                return;
            }

            for (int i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                if (candidate == null || candidate.isDie)
                {
                    continue;
                }

                var candidatePos = candidate.PlayerspineObj != null
                    ? candidate.PlayerspineObj.transform.position
                    : candidate.transform.position;
                var newDistance = Vector2.Distance(candidatePos, initPos);
                if (newDistance < distance)
                {
                    distance = newDistance;
                    targetSolidierCard = candidate;
                }
            }

            if (targetSolidierCard == null)
            {
                //人物全部被消灭
                return;
            }
        }

        /// <summary>
        /// 怪物出场
        /// </summary>
        private void MonsterCreate(Vector3 initPos = default)
        {
            if (_monsterConfig.MoveType == MoveType.ZhiXing)
            {
                runStage = RunStage.StraightTravel;
            }
            else if (_monsterConfig.MoveType == MoveType.Flying)
            {
                runStage = RunStage.flymove;
            }
            else if (_monsterConfig.MoveType == MoveType.Jumping)
            {
                runStage = RunStage.StraightTravel;
            }
            else if (_monsterConfig.MoveType == MoveType.ZuanDi)
            {
                runStage = RunStage.Burrow;
            }
            else if (_monsterConfig.MoveType == MoveType.Motionless)
            {
                runStage = RunStage.Motionless;
            }
            else
            {
                runStage = RunStage.StraightTravel;
            }
            
            List<Transform> doorPoss = new List<Transform>();
            burrowposs.Clear();
            var monsterPos = GameLand.Default.GetLane(BattleSide)?.MonsterPos;
            if (monsterPos == null)
            {
                monsterPos = GameLand.Default.MonsterPos;
            }

            foreach (Transform child in monsterPos.transform)
            {
                if (child.name.Contains("doorpos"))
                {
                    doorPoss.Add(child);
                }

                if (child.name.Contains("Burrowpos"))
                {
                    burrowposs.Add(child);
                }
            }

            float moseterx = Random.Range(-3.2f, 3.2f);
            while (MathF.Abs(moseterx - GamePlayManager.Default.MoseterxLast) < 0.5f)
            {
                moseterx = Random.Range(-3.2f, 3.2f);
            }

            GamePlayManager.Default.MoseterxLast = moseterx;
            if (_monsterConfig.MonsterQuality ==
                MonsterQuality.Elite)
            {
                moseterx = 0;
            }

            if (_monsterConfig.MonsterQuality ==
                MonsterQuality.Boss)
            {
                moseterx = 0;
            }

            if (moseterx < 0)
            {
                iconattack = new Vector3(-1, 1, 1);
            }

            if (initPos == default)
            {
                // 对侧为横向镜像（x≈1153），出生 x 需带上该侧 doorpos 的 x，避免刷到玩家侧列
                float spawnX = BattleSide == BattleSide.Opponent
                    ? doorPoss[0].position.x + moseterx
                    : moseterx;
                initPos = new Vector3(spawnX, doorPoss[0].position.y, 0);
            }

            EnsurePhysicsBody();

            switch (runStage)
            {
                case RunStage.Burrow:
                {
                    BurrowCreate();
                    break;
                }
                case RunStage.jumpmove:
                {
                    SetPhysicsPosition(initPos);
                    IsInit = true;
                    break;
                }
                case RunStage.flymove:
                {
                    SetPhysicsPosition(initPos);
                    IsInit = true;
                    break;
                }
                case RunStage.diagonal:
                {
                    SetPhysicsPosition(initPos);
                    IsInit = true;
                    break;
                }
                case RunStage.StraightTravel:
                {
                    SetPhysicsPosition(initPos);
                    if (AttackBehaviour.TryHandleStraightTravelArrival(this, () => IsInit = true))
                    {
                        break;
                    }

                    if (AttackBehaviour.DeferStraightTravelInit)
                    {
                        break;
                    }

                    IsInit = true;
                    break;
                }
                case RunStage.Motionless:
                {
                    var laneMonsterPos = GameLand.Default.GetLane(BattleSide)?.MonsterPos;
                    var motionless = laneMonsterPos != null ? laneMonsterPos.Find("Motionless") : null;
                    if (motionless == null && GameLand.Default.MonsterPos != null)
                    {
                        motionless = GameLand.Default.MonsterPos.transform.Find("Motionless");
                    }

                    if (motionless != null)
                    {
                        SetPhysicsPosition(motionless.position);
                    }

                    // IsInit = true;
                    break;
                }
            }

            MonsterSortManager.Instance.RequestSort(Random.Range(0.2f, 5f));
            
        }

        /**
         * 快速移动到主城附近
         */
        public void FastCityMove()
        {
        }

        public bool IsSpine;

        public TrackEntry Play(string animName, bool loop = true, int track = 0)
        {
            return AnimationController.Play(track, animName, loop);
        }

        public void Pause()
        {
            AnimationController.Pause();
        }

        public void Resume(bool loop = true)
        {
            AnimationController.Resume(loop);
        }

        
        public int mySiblingIndex;
        
        internal bool IsInit { get; set; }
        public bool isInAttackCooldown = false; // 攻击冷却标记
        public bool isAttackAnimationPlaying = false; // 攻击动画播放状态

        #region AI

        /**
         * 设置移动速度
         */
        public void SetMoveSpeed(float speed)
        {
            moveSpeed += speed;
            JumpDownmoveDistance *= moveSpeed;
        }

        public enum RunStage
        {
            StraightTravel,
            diagonal,
            jumpmove,
            flymove,
            Burrow,
            Motionless
        }

        public void Tick()
        {
            if (!IsInit)
            {
                return;
            }

            if (GamePlayManager.Default.IsPaused())
            {
                return;
            }

            // 状态机更新：移动位移在 FixedMoveTick 中单独处理
            _stateMachine?.Update();
            BuffController?.Update(Time.deltaTime);
            if(_monsterConfig.MonsterQuality ==
               MonsterQuality.Boss)
                LogTool.Log($"[Tick] 当前状态: {(_stateMachine?.CurrentState?.GetType().Name ?? "Null")}, Iswall: {Iswall},nowstate {monsterData.monsterState}");
        }

        /// <summary>
        /// 怪物移动专用 FixedUpdate 入口，与状态机 Tick 解耦，保证位移平滑。
        /// 由 MonsterManager.FixedUpdate 调用，deltaTime = fixedDeltaTime × 游戏倍速。
        /// </summary>
        public void FixedMoveTick(float deltaTime)
        {
            if (!IsInit)
            {
                return;
            }

            if (GamePlayManager.Default.IsPaused())
            {
                return;
            }

            if (monsterData.monsterState != MonsterState.move)
            {
                return;
            }

            MoveBehaviour.Update(this, deltaTime);
        }

        /// <summary>
        /// 确保根节点有 Kinematic Rigidbody2D，子 HitBox 碰撞体才能作为动态刚体一部分移动。
        /// </summary>
        public void EnsurePhysicsBody()
        {
            if (_rigidbody2D == null)
            {
                _rigidbody2D = GetComponent<Rigidbody2D>();
            }

            if (_rigidbody2D == null)
            {
                _rigidbody2D = gameObject.AddComponent<Rigidbody2D>();
            }

            _rigidbody2D.bodyType = RigidbodyType2D.Kinematic;
            _rigidbody2D.simulated = true;
            _rigidbody2D.gravityScale = 0f;
            _rigidbody2D.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
            _rigidbody2D.sleepMode = RigidbodySleepMode2D.NeverSleep;
            _rigidbody2D.interpolation = RigidbodyInterpolation2D.None;
            _rigidbody2D.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        /// <summary>
        /// FixedUpdate 连续位移：使用 MovePosition，避免静态碰撞体 teleport 重建接触对。
        /// </summary>
        public void MovePhysicsPosition(Vector3 position)
        {
            if (_rigidbody2D == null)
            {
                EnsurePhysicsBody();
            }

            _rigidbody2D.MovePosition(position);
        }

        /// <summary>
        /// 瞬移/回收等立即改位：同步 Rigidbody2D 与 Transform（AutoSyncTransforms 关闭时两者需一起写）。
        /// </summary>
        public void SetPhysicsPosition(Vector3 position)
        {
            if (_rigidbody2D == null)
            {
                EnsurePhysicsBody();
            }

            _rigidbody2D.position = position;
            transform.position = position;
        }

        public Rigidbody2D PhysicsBody
        {
            get
            {
                if (_rigidbody2D == null)
                {
                    EnsurePhysicsBody();
                }

                return _rigidbody2D;
            }
        }


        public bool hasEngagedTarget = false; // 新增标志位，记录是否已执行

        public void ResetEngageTarget()
        {
            hasEngagedTarget = false;
        }

        public bool IsModerateState = false;
        

        public void PushPull(int actionid, float strenth, bool IsTarget)
        {
            if (_monsterConfig.MonsterQuality ==
                MonsterQuality.Boss || _cardId == 230003) return;
            
            //意义不明
            if (actionid == 0)
            {
                if (monsterData.monsterState == MonsterState.engage) return;
                if (IsTarget)
                {
                    SetPhysicsPosition(new Vector2(transform.position.x, strenth));
                }
                else
                {
                    SetPhysicsPosition(new Vector2(transform.position.x, transform.position.y - strenth / 500));
                }
            }

            if (actionid == 1)
            {
                if (IsTarget)
                {
                    SetPhysicsPosition(new Vector2(transform.position.x, strenth));
                }
                else
                {
                    SetPhysicsPosition(new Vector2(transform.position.x, transform.position.y + strenth / 500));
                }
            }
            
            monsterData.monsterState = MonsterState.move;
            hasEngagedTarget = false;
            Iswall = false;
            Tick();
        }

        //击退
        public void PullToPosition(Vector2 targetPos, float duration = 0.3f)
        {
            if (_monsterConfig.MonsterQuality is MonsterQuality.Boss or MonsterQuality.Elite)
                return;
            monsterData.pullinfo = (targetPos, duration);
            monsterData.monsterControlState = MonsterControlState.Pull;
            monsterData.monsterState = MonsterState.specialMove;
            Tick();
        }
        

        public void AttackState(bool atkStop)
        {
            if (_monsterConfig != null
                && _monsterConfig.MonsterQuality is MonsterQuality.Boss or MonsterQuality.Elite)
                return;

            if (AttackBehaviour == null)
                return;

            if (atkStop)
            {
                AttackBehaviour.PauseEngage(this);
            }
            else
            {
                AttackBehaviour.ResumeEngage(this);
            }
        }
        
        /// <summary>
        /// 让怪物击退
        /// </summary>
        /// <param name="repelVal">击退值</param>
        /// <param name="duration">击退时间</param>
        public void RepelMonster(float repelVal = 1000f, float duration = 0.8f)
        {
            if (repelVal == 0) return;
            var targetPos = new Vector2(transform.position.x, transform.position.y + repelVal / 500);
            PullToPosition(targetPos, duration);
        }

        /// <summary>
        /// 让怪物击飞
        /// </summary>
        /// <param name="height">击飞高度</param>
        /// <param name="durationUp">上升时间</param>
        /// <param name="durationDown">下落时间</param>
        /// <param name="airTime">空中停留时间</param>
        public void KnockUpMonster(float height = 2f, float durationUp = 0.5f, float airTime = 0.5f, float durationDown = 0.3f)
        {
            if (_monsterConfig.MonsterQuality is MonsterQuality.Boss or MonsterQuality.Elite) return;
            monsterData.knockupinfo = (height, durationUp, airTime, durationDown);
            monsterData.monsterControlState = MonsterControlState.KnockUp;
            monsterData.monsterState = MonsterState.specialMove;
            monsterData.isMoveEngaged = false;
            Tick();
        }
        

        public void BurrowCreate()
        {
            int burrowLength = 0;
            DOTween.To(v => { }, 0, 0, _monsterConfig.MoveSpeed / 40).onComplete += () =>
            {
                float randomX = burrowposs[burrowLength].transform.position.x -
                    burrowposs[burrowLength].GetComponent<Burrow>().spawnRangeSize.x / 2 + Random.Range(0,
                        burrowposs[burrowLength].GetComponent<Burrow>().spawnRangeSize.x);
                float randomY = burrowposs[burrowLength].transform.position.y -
                    burrowposs[burrowLength].GetComponent<Burrow>().spawnRangeSize.y / 2 + Random.Range(0,
                        burrowposs[burrowLength].GetComponent<Burrow>().spawnRangeSize.y);
                Vector3 spawnPosition = new Vector3(randomX, randomY, 0);
                SetPhysicsPosition(spawnPosition);
                IsInit = true;
            };
        }

        public void ModerateEnemy(float speed, bool IsModerate)
        {
            if (IsModerate)
            {
                if (IsModerateState == false)
                {
                    JumpmoveDistance = JumpmoveDistance / speed; // 移动的距离
                    JumpDownmoveDistance = JumpDownmoveDistance / speed; // 移动的距离
                    JumpDuration = JumpDuration * speed; // 移动的持续时间
                    JumpDownDuration = JumpDownDuration * speed;
                    DealyTime = DealyTime * speed;
                    IsModerateState = true;
                }
            }
            else
            {
                JumpmoveDistance = JumpmoveDistance * speed; // 移动的距离
                JumpDownmoveDistance = JumpDownmoveDistance * speed; // 移动的距离
                JumpDuration = JumpDuration / speed; // 移动的持续时间
                JumpDownDuration = JumpDownDuration / speed;
                DealyTime = DealyTime / speed;
                IsModerateState = false;
            }
        }
        #endregion

        #region 受到攻击

        private Tween lvdoutween;

        /**
         * 受击入口：构建上下文、跑管线、结算伤害。
         */
        public int ReceiveDamage(in MonsterDamageRequest request)
        {
            return MonsterDamageService.ReceiveDamage(in request);
        }

        private Color originalColor = Color.white;
        private Sequence colorTween;

        private bool IsSkeletonAlive => AnimationController.HasSkeletonAnimation;

        private Color GetSkeletonHitColor()
        {
            if (!IsSkeletonAlive)
            {
                return originalColor;
            }

            return AnimationController.GetSkeletonColor();
        }

        private void SetSkeletonHitColor(Color color)
        {
            if (!IsSkeletonAlive)
            {
                return;
            }

            AnimationController.SetSkeletonColor(color);
        }

        private void KillColorTween()
        {
            if (colorTween != null && colorTween.IsActive())
            {
                colorTween.Kill();
            }

            colorTween = null;
        }

        public void OnHit()
        {
            if (!IsSkeletonAlive)
            {
                LogTool.LogWarning("无法找到SkeletonAnimation组件");
                return;
            }
            if (colorTween != null)
            {
                KillColorTween();
            }
            // 保存原始颜色用于恢复
            originalColor = Color.white;

            // 创建新的闪烁序列
            colorTween = DOTween.Sequence();
            colorTween.SetTarget(transform);
            colorTween.SetId(GlobalConst.BattleTween);

            // 设置为可自动杀死，避免内存泄漏
            colorTween.SetAutoKill(true);

            // 1. 快速变红
            colorTween.Append(
                DOTween.To(
                    GetSkeletonHitColor,
                    SetSkeletonHitColor,
                    new Color(1f, 0.4f, 0.4f),
                    0.12f
                ).SetEase(Ease.OutExpo)
            );

            // 2. 轻微加深红色
            colorTween.Append(
                DOTween.To(
                    GetSkeletonHitColor,
                    SetSkeletonHitColor,
                    new Color(1f, 0.2f, 0.2f),
                    0.08f
                ).SetEase(Ease.OutQuad)
            );

            // 3. 完全恢复原色
            colorTween.Append(
                DOTween.To(
                    GetSkeletonHitColor,
                    SetSkeletonHitColor,
                    originalColor,
                    0.3f
                ).SetEase(Ease.InOutSine)
            );

            colorTween.OnComplete(KillColorTween);
        }

        /// <summary>
        /// 消费管线结算结果（不执行伤害计算）。
        /// </summary>
        public int TakeDamage(DamageResult result, in DamageApplyOptions options = default)
        {
            return DamageReceiver.ApplyDamage(result, in options);
        }

        #region DamageReceiver 内部钩子

        internal bool HandlingTagsContains(string tag)
        {
            return _handlingTags.Contains(tag);
        }

        internal void AddHandlingTag(string tag)
        {
            _handlingTags.Add(tag);
        }

        internal void ShowShieldBreakIconInternal(float delay)
        {
            ShowShieldBreakIcon(delay);
        }

        internal void TriggerHitEffectInternal()
        {
            TriggerHitEffect();
        }

        internal void ApplyToughnessDamageInternal(float value)
        {
            ApplyToughnessDamage(value);
        }

        internal void ContinuousDamageNumInternal(ISkill skillCard, float healthPercentage)
        {
            ContinuousDamageNum(skillCard, healthPercentage);
        }

        internal void PlayEffectCriticalThunderboltInternal(ISkill skillCard, Vector3 pos, float thunderDamageVal)
        {
            PlayEffectCriticalThunderbolt(skillCard, pos, thunderDamageVal);
        }

        #endregion

        /// <summary>
        /// 应用韧性伤害，扣减当前韧性值，归零时进入虚弱状态
        /// </summary>
        private void ApplyToughnessDamage(float value)
        {
            // 虚弱恢复保护期内：韧性条正在从0恢复，此时不接受新的韧性伤害，避免竞态导致永远卡在虚弱状态
            if (monsterData.isRecoveringFromWeak)
            {
                return;
            }

            if (value <= 0f || monsterData.MaxResilience <= 0f || monsterData.monsterState == MonsterState.weak)
            {
                return;
            }

            monsterData.CurrentResilience = Mathf.Clamp(monsterData.CurrentResilience - value, 0f, monsterData.MaxResilience);

            if (monsterData.CurrentResilience <= 0f )
            {
                monsterData.monsterState = MonsterState.weak;
                Tick();
            }
        }
        
        #region Spine 攻击事件转发（Prefab / Spine 绑定兼容）

        public void SenLingOneAttack()
        {
            AttackBehaviour.OnSpineEvent(this, nameof(SenLingOneAttack));
        }

        public void SenLingtwoAttack()
        {
            AttackBehaviour.OnSpineEvent(this, nameof(SenLingtwoAttack));
        }

        public void MoguBoss()
        {
            AttackBehaviour.OnSpineEvent(this, nameof(MoguBoss));
        }

        #endregion

        float LastrandomX;
        float LastrandomY;

        private int diecount = 0;

        private IEnumerator DisableAfterDelay(GenericObjectPool pool, GameObject damageObject, float delay)
        {
            // 等待指定的时间
            yield return new WaitForSeconds(delay);

            // 检查对象是否仍然有效
            if (damageObject != null)
            {
                pool.Return(damageObject);
            }
        }

        private void Jump(int uid)
        {
            if (_monsterConfig.Type == MonsterType.MushroomGiant)
            {
                transform.GetOrAddComponent<EnemyJump>();
            }
        }

        private void TriggerHitEffect()
        {
            damageTween?.Kill();
            damageTween = DOTween.Sequence()
                .Append(transform.DOScale(selfScale, 0.05f).SetEase(Ease.OutBack))
                .OnComplete(() => damageTween = null);
        }

        /// <summary>
        /// 死亡
        /// </summary>
        public void Die()
        {
            if ( monsterData.monsterState == MonsterState.die)
            {
                return;
            }
            // 先标记为死亡，避免回收/销毁先于后续逻辑时出现误判或漏记。
            monsterData.monsterState = MonsterState.die;
            // PVP：对侧怪由对侧 AI 击杀，金币/特效归 AI 侧，不再飞给玩家
            bool isPvpOpponentSide = BattleSession.IsPvpAi && BattleSide == BattleSide.Opponent;
            NotifyMechanicsDeath();
            Game.Battle.BattleRuleComputerSupport.OnMonsterRemoved(this);
            KillColorTween();
            BuffController.ClearAll();  //移除所有buff
            DOVirtual.DelayedCall(0.01f, () => { diecount++;});
            // 延时销毁敌人
            // StartCoroutine(DieWait());
            // 当对象被销毁时取消注册回调
            speedMultiplierRegistration?.UnRegister();
            hp = 0;
            StartCoroutine(DisableAfterDelay(ObjectPoolManager.Instance.GetGameObjectPool("MonsterCard1"),
                this.gameObject, 0.01f));
            // 通知所有监听者该卡牌即将被销毁
            onDestroyed?.Invoke(this);

            HitBox.gameObject.SetActive(false);
            if (selfQuality == MonsterQuality.Boss)
            {
                // AudioKit.PlayMusic("resources://Audio/常规战斗BGM随机" + Random.Range(1, 3).ToString());
            }
            SoundManager.Instance.PlayVoice(SoundType.SoundBattleDie);
            // AudioKit.PlaySound("resources://Audio/怪物死亡音效");

            var lvCfg = _monsterConfig.LevelPropertyList.FirstOrDefault(v => v.Level == _level);
            if (lvCfg == null)
            {
                LogTool.LogError("敌人经验值超出配置-_cardId: " + _cardId + ", _level: " + _level);
                return;
            }

            // Global.CityExp.Value += lvCfg.ProvideExp;

            // 掉落奖励
            if (lvCfg.DropRewards.Count > 0)
            {
                foreach (var rew in lvCfg.DropRewards)
                {
                    var rewNum = rew.Num;
                    var itemCfg = _tbItemConfig.GetOrDefault(rew.Id);
                    switch (itemCfg.Type)
                    {
                        // case ItemType.Mana:
                        //     GamePlayManager.Default.UpdateGameMana(rewNum);
                        //     ManaController.Instance.CreateMana(rewNum * 10, this.transform.position); // 法力
                        //     break;
                        // case ItemType.ManaFragment:
                        //     rewNum += (int)GamePlayManager.Default.
                        //         ModuleAttributes.GetFinal(AttributeType.MonsterDropManaFragmentIncrease);// 怪物掉落卡能碎片增加
                        //     GamePlayManager.Default.UpdateGameManaFragment(rewNum);
                        //     ManaController.Instance.CreateMana(rewNum, this.transform.position); // 法力碎片
                        //     break;

                        case ItemType.SurpriseLootBox:
                            if (isPvpOpponentSide) continue;
                            GamePlayManager.Default.DropSurpriseChooseCardPanel(this.transform.position);
                            break;
                        
                        case ItemType.CombatCurrency:
                            if (monsterData.SuppressDeathLoot || isPvpOpponentSide) continue;

                            // 击杀敌人额外战斗货币
                            // var extraGoldCoin = GamePlayManager.Default.ModuleAttributes.GetFinal(AttributeType.KillAnEnemyExtraBattleGoldCoin);
                            // if (extraGoldCoin > 0f)
                            // {
                            //     rewNum += (int)extraGoldCoin;
                            // }
                            
                            GoldCoinManager.Instance.CreateCoin(rewNum, this.transform.position); // 掉落金币
                            break;
                    }
                }
            }

            if (!isPvpOpponentSide && !monsterData.SuppressDeathLoot
                && _monsterConfig.MonsterQuality is MonsterQuality.Elite or MonsterQuality.Boss)
            {
                var bossExtraGold = (int)GamePlayManager.Default.ModuleAttributes
                    .GetFinal(AttributeType.KillAnBossGoldCoin);
                if (bossExtraGold > 0)
                {
                    // KillAnBossGoldCoin 是货币数值，不是金币实体个数。
                    // CreateCoin(n) 会生成 n 个实体，每个再加 DorpGoldBaseNumber，不能直接拿属性值当 count。
                    GeneralSkillManager.Instance.OnGoldGained(bossExtraGold);
                }
            }
            // Destroy(ShadowEnem.gameObject, 0.01f);
            
            // 通用怪物死亡效果
            switch (_monsterConfig.MonsterQuality)
            {
                case MonsterQuality.Boss:
                    EffectManager.Instance.PlayEffect("BossDeathEffect01", transform.position);
                    if (!isPvpOpponentSide)
                    {
                        PlayMonsterDeathCanvasEffect("MonsterDeathEffectBoss", transform.position);
                    }
                    break;
                case MonsterQuality.Elite:
                    EffectManager.Instance.PlayEffect("BossDeathEffect01", transform.position);
                    if (!isPvpOpponentSide)
                    {
                        PlayMonsterDeathCanvasEffect("MonsterDeathEffectElite", transform.position);
                    }
                    break;
                default:
                    EffectManager.Instance.PlayEffect("MonsterDeathEffect02", transform.position);
                    break;
            }
            GamePlayManager.Default?.TriggerGuide(GuideType.KillTheFirstMonster); // 引导 - 买流派
            monsterData.monsterTweens.KillAllTweens();
            if (AttackBehaviour != null)
            {
                AttackBehaviour.OnCleanup(this);
            }
            OnDispose();
        }

        /// <summary>
        /// Boss/精英死亡 Canvas 表现：设置 RenderCamera → 0.2s 渐显 → 停留 1s → 飞向 GoldBar → 回收。
        /// 坐标系与 MonsterHealthBar 一致（GameLandUI Canvas + MainCamera 换算）。
        /// </summary>
        private static void PlayMonsterDeathCanvasEffect(string effectName, Vector3 worldPos)
        {
            var effect = EffectManager.Instance.PlayEffect(effectName, worldPos);
            var canvas = effect.GetComponentInChildren<Canvas>(true);

            var lifecycle = effect.GetComponent<EffectLifecycle>();
            if (lifecycle != null)
            {
                lifecycle.SuspendAutoRecycle();
            }

            // 与精英/Boss 血条同一套 UI 坐标系，保证落点=怪物死亡点
            var hostCanvas = GameLandUI.Default.GetComponentInParent<Canvas>();
            var parentRt = hostCanvas.transform as RectTransform;
            // 与 MonsterHealthBar 相同：Overlay 传 null，Camera 模式用 host.worldCamera
            var uiCam = hostCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : hostCanvas.worldCamera;

            effect.transform.SetParent(parentRt, false);

            // 保持 Canvas 启用；相机/模式与宿主对齐，避免 UICamera 与 MainCamera 视差
            canvas.enabled = true;
            canvas.renderMode = hostCanvas.renderMode;
            canvas.worldCamera = hostCanvas.worldCamera;
            canvas.planeDistance = hostCanvas.planeDistance;
            canvas.overrideSorting = true;

            var canvasRt = canvas.transform as RectTransform;
            var goldRt = canvasRt.Find("gold") as RectTransform;

            // 铺满宿主 Canvas，才能用与血条相同的 Screen→Local 换算
            canvasRt.anchorMin = Vector2.zero;
            canvasRt.anchorMax = Vector2.one;
            canvasRt.pivot = new Vector2(0.5f, 0.5f);
            canvasRt.offsetMin = Vector2.zero;
            canvasRt.offsetMax = Vector2.zero;
            canvasRt.localScale = Vector3.one;
            canvasRt.localRotation = Quaternion.identity;
            canvasRt.localPosition = Vector3.zero;
            Canvas.ForceUpdateCanvases();

            var screenPoint = RectTransformUtility.WorldToScreenPoint(Camera.main, worldPos);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRt, screenPoint, uiCam, out var deathLocal))
            {
                LogTool.LogError("[MonsterCard] 死亡特效坐标换算失败");
            }

            const float fadeInDuration = 0.2f;
            const float showDuration = 1f;
            const float flyDuration = 0.5f;

            goldRt.anchoredPosition = deathLocal;
            goldRt.localScale = Vector3.one;

            var goldImage = goldRt.GetComponent<UnityEngine.UI.Image>();
            var color = goldImage.color;
            color.a = 0f;
            goldImage.color = color;

            var seq = DOTween.Sequence();
            seq.SetLink(effect);
            seq.Append(goldImage.DOFade(1f, fadeInDuration));
            seq.AppendInterval(showDuration);
            seq.AppendCallback(() =>
            {
                var goldBar = GoldCoinManager.Instance.target;
                var goldBarScreen = RectTransformUtility.WorldToScreenPoint(uiCam, goldBar.position);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRt, goldBarScreen, uiCam, out var goldBarLocal);
                goldRt.DOAnchorPos(goldBarLocal, flyDuration)
                    .SetEase(Ease.InQuad)
                    .SetLink(effect)
                    .OnComplete(() => EffectManager.Instance.RecycleEffect(effect));
            });
        }

        #endregion

        /// <summary>
        /// 血条初始化
        /// </summary>
        private Coroutine currentAnimation;

        private int displayedHpCount; // 记录当前显示的血量段数
        private float displayedFillAmount; // 记录当前显示的填充量

        private void BindSceneHealthBar()
        {
            if (MonsterHealthBarManager.Instance == null && GameLandUI.Default != null)
            {
                MonsterHealthBarManager.EnsureInitialized(GameLandUI.Default.transform);
            }

            if (MonsterHealthBarManager.Instance == null)
            {
                LogTool.LogError("[MonsterCard] MonsterHealthBarManager 未就绪，无法绑定场景血条");
                return;
            }

            ReleaseSceneHealthBar();
            _sceneHealthBar = MonsterHealthBarManager.Instance.Acquire(this);
            if (_sceneHealthBar == null)
            {
                LogTool.LogError("[MonsterCard] 获取场景血条失败");
                return;
            }

            Healtheffect = _sceneHealthBar.Root;
            ClearHealthBarUiCache();
        }

        private void ReleaseSceneHealthBar()
        {
            if (_sceneHealthBar != null && MonsterHealthBarManager.Instance != null)
            {
                MonsterHealthBarManager.Instance.Release(_sceneHealthBar);
                _sceneHealthBar = null;
            }

            ClearHealthBarUiCache();
            Healtheffect = _prefabHealtheffect;
            if (Healtheffect != null)
            {
                Healtheffect.SetActive(false);
            }
        }

        private void BindSceneIconEffects()
        {
            if (MonsterIconEffectsManager.Instance == null && GameLandUI.Default != null)
            {
                MonsterIconEffectsManager.EnsureInitialized(GameLandUI.Default.transform);
            }

            if (MonsterIconEffectsManager.Instance == null)
            {
                LogTool.LogError("[MonsterCard] MonsterIconEffectsManager 未就绪，无法绑定状态图标");
                return;
            }

            ReleaseSceneIconEffects();
            _sceneIconEffects = MonsterIconEffectsManager.Instance.Acquire(this);
            if (_sceneIconEffects == null)
            {
                LogTool.LogError("[MonsterCard] 获取场景状态图标失败");
                return;
            }

            DisarmIcon = _sceneIconEffects.DisarmIcon;
            ExecutionIcon = _sceneIconEffects.ExecutionIcon;
            ShieldBreakIcon = _sceneIconEffects.ShieldBreakIcon;
        }

        private void ReleaseSceneIconEffects()
        {
            if (_sceneIconEffects != null && MonsterIconEffectsManager.Instance != null)
            {
                MonsterIconEffectsManager.Instance.Release(_sceneIconEffects);
                _sceneIconEffects = null;
            }

            DisarmIcon = _prefabDisarmIcon;
            ExecutionIcon = _prefabExecutionIcon;
            ShieldBreakIcon = _prefabShieldBreakIcon;
            if (_prefabIconEffects != null)
            {
                _prefabIconEffects.SetActive(false);
            }
        }

        private void ClearHealthBarUiCache()
        {
            _staggerBarRect = null;
            _staggerBarParentRect = null;
            _staggerBarImage = null;
            _staggerBarEffectImage = null;
            displayedHpCount = 0;
            displayedFillAmount = 0f;
            currentAnimation = null;
        }

        private void SetBaseInfo()
        {
            if (!IsBoos || Healtheffect == null) return;
            
            var baseCfg = _tbCardBaseConfig.GetOrDefault(_cardId);
            if (baseCfg == null) return;
            
            TMP_Text monsterName = Healtheffect.transform.Find("name").GetComponent<TMP_Text>();
            Image healthIcon = Healtheffect.transform.Find("healthimage/MonsterIcon").GetComponent<Image>();
            monsterName.text = baseCfg.Name;

            healthIcon.sprite = Global.MonsterHeadIconSpriteAtlas.GetSprite(baseCfg.Icon + "Head");
            if (healthIcon.sprite == null)
            {
                healthIcon.sprite = Global.MonsterHeadIconSpriteAtlas.GetSprite(HeadNameDefault + "Head");
            }
        }

        public void SetHealth()
        {
            if (!IsBoos || Healtheffect == null) return;
            
            TMP_Text healthCount = Healtheffect.transform.Find("Healthcount").GetComponent<TMP_Text>();
            Image healthEffect = Healtheffect.transform.Find("health/healthEffect").GetComponent<Image>();
            Image healthDown = Healtheffect.transform.Find("health/healthBar").GetComponent<Image>();
            
            healthCount.text = Math.Ceiling((float)hp / maxHp * 100) + "%"; // 修正了百分比计算

            // 血条双层动画（与韧性条一致：healthBar 前景层，healthEffect 背景残影层）
            float healthFill = (float)hp / maxHp;
            RectTransform healthDownRect = healthDown.GetComponent<RectTransform>();
            RectTransform healthEffectRect = healthEffect.GetComponent<RectTransform>();
            RectTransform healthRect = healthDown.transform.parent.GetComponent<RectTransform>();
            float targetHealthWidth = healthRect.rect.width * healthFill;

            _healthBarTween?.Kill();
            _healthBarEffectTween?.Kill();

            // 前景层（healthBar）：几乎瞬时缩短，给玩家即时反馈
            healthDownRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetHealthWidth);

            // 背景残影层（healthEffect）：通过修改宽度缓慢追赶
            _healthBarEffectTween = DOTween.To(
                    () => healthEffectRect.rect.width,
                    x => healthEffectRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, x),
                    targetHealthWidth,
                    0.35f
                )
                .SetEase(Ease.OutQuad)//OutCubic 或者 OutQuad
                .SetDelay(0.15f);
            LogTool.Log($"怪物当前韧性值{monsterData.CurrentResilience}");

            // 韧性条（带平滑动画，虚弱/恢复保护期间禁用）
            if (monsterData.MaxResilience > 0f && _staggerBarRect != null && _staggerBarParentRect != null)
            {
                float fullWidth = _staggerBarParentRect.rect.width;
                float targetFill = monsterData.CurrentResilience / monsterData.MaxResilience;
                float targetWidth = fullWidth * targetFill;
                _staggerBarTween?.Kill();
                _staggerBarEffectTween?.Kill();

                // CurStaggerEffect 背景层
                if (_staggerBarEffectImage != null)
                {
                    RectTransform effectRect = _staggerBarEffectImage.rectTransform;
                    if (!monsterData.isRecoveringFromWeak)
                    {
                        _staggerBarEffectTween = DOTween.To(
                                () => effectRect.rect.width,
                                x => effectRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, x),
                                targetWidth,
                                0.35f
                            )
                            .SetEase(Ease.OutCubic);
                    }
                    else
                    {
                        effectRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetWidth);
                    }
                }

                // CurStaggerBar 前景层
                if (monsterData.monsterState != MonsterState.weak && !monsterData.isRecoveringFromWeak && _staggerBarImage != null)
                {
                    _staggerBarTween = DOTween.To(
                            () => _staggerBarRect.rect.width,
                            x => _staggerBarRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, x),
                            targetWidth,
                            0.1f
                        )
                        .SetEase(Ease.OutSine);
                }
                else
                {
                    _staggerBarRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetWidth);
                }
            }
        }

        public void HealthInit()
        {
            if (Healtheffect == null) return;

            int actualHpCount = Mathf.Max(0, (int)((hp / 1000) + 1)); // 确保不小于0
            float currentFill = (hp % 1000) / 1000f; // 当前血条的填充量
            currentFill = Mathf.Clamp01(currentFill); // 确保在0-1之间

            // 更新名字显示
            Healtheffect.transform.Find("name").GetComponent<TMP_Text>().text = _tbCardBaseConfig.GetOrDefault(_cardId).Name;

            // 实时更新血量数字显示
            Healtheffect.transform.Find("Healthcount").GetComponent<TMP_Text>().text = "X" + actualHpCount.ToString();

            // 血量段数发生变化
            if (displayedHpCount != actualHpCount)
            {
                // 停止当前动画
                if (currentAnimation != null)
                {
                    StopCoroutine(currentAnimation);
                }

                // 播放换血动画
                currentAnimation = StartCoroutine(PlayBarChangeAnimation(displayedHpCount, actualHpCount, currentFill));
            }
            // 血量段数没变但填充量变化（在当前段内受到伤害）
            else if (Mathf.Abs(displayedFillAmount - currentFill) > 0.01f)
            {
                // 播放血量减少动画
                PlayFillAmountAnimation(currentFill);
            }

            // 韧性条缓存（只在首次初始化）
            if (monsterData.MaxResilience > 0f && _staggerBarRect == null)
            {
                _staggerBarRect = Healtheffect.transform.Find("StaggerBar/CurStaggerBar").GetComponent<RectTransform>();
                _staggerBarParentRect = _staggerBarRect.transform.parent.GetComponent<RectTransform>();
                _staggerBarImage = _staggerBarRect.GetComponent<Image>();
                _staggerBarEffectImage = Healtheffect.transform.Find("StaggerBar/CurStaggerEffect")
                    .GetComponent<Image>();
            }

            // 更新显示状态
            displayedHpCount = actualHpCount;
            displayedFillAmount = currentFill;
            
            SetHealth();
        }

        // 播放换血动画（血量段数变化时）
        private IEnumerator PlayBarChangeAnimation(int startHpCount, int targetHpCount, float targetFill)
        {
            // 血量增加（治疗）
            if (targetHpCount > startHpCount)
            {
                UpdateHealthBarAppearance(targetHpCount);
                UpdateCurrentBarFillAmount(targetFill);
                yield break;
            }

            // 血量减少（受伤）
            int lostBars = startHpCount - targetHpCount;

            for (int i = 0; i < lostBars; i++)
            {
                int currentBar = startHpCount - i;
                bool isLastBar = (currentBar == 1 && targetHpCount == 0);

                // 更新血条外观
                UpdateHealthBarAppearance(currentBar);

                // 设置血条为满（准备播放减少动画）
                SetBarFillAmount(1f);

                // 播放血条减少动画
                float duration = isLastBar ? 0.3f : 0.15f;
                float startTime = Time.time;

                while (Time.time - startTime < duration)
                {
                    float progress = (Time.time - startTime) / duration;
                    float fillAmount = Mathf.Lerp(1f, 0f, progress);
                    SetBarFillAmount(fillAmount);
                    yield return null;
                }
            }

            // 更新到最终状态
            UpdateHealthBarAppearance(targetHpCount);
            UpdateCurrentBarFillAmount(targetFill);
        }

        // 播放血量填充量动画（当前段内血量变化）
        private void PlayFillAmountAnimation(float targetFill)
        {
            if (currentAnimation != null)
            {
                StopCoroutine(currentAnimation);
            }

            currentAnimation = StartCoroutine(AnimateFillAmount(displayedFillAmount, targetFill));
        }

        private IEnumerator AnimateFillAmount(float startFill, float targetFill)
        {
            float duration = 0.3f;
            float startTime = Time.time;

            while (Time.time - startTime < duration)
            {
                float progress = (Time.time - startTime) / duration;
                float currentFill = Mathf.Lerp(startFill, targetFill, progress);
                SetBarFillAmount(currentFill);
                displayedFillAmount = currentFill;
                yield return null;
            }

            SetBarFillAmount(targetFill);
            displayedFillAmount = targetFill;
        }

        private void UpdateHealthBarAppearance(int hpcount)
        {
            if (hpcount > 1)
            {
                int count = (hpcount - 1) % 4;
                int value1 = 2, value2 = 1, value3 = 3;

                switch (count)
                {
                    case 0:
                        value1 = 5;
                        value2 = 6;
                        value3 = 7;
                        break;
                    case 1:
                        value1 = 3;
                        value2 = 4;
                        value3 = 5;
                        break;
                    case 2:
                        value1 = 1;
                        value2 = 2;
                        value3 = 3;
                        break;
                    case 3:
                        value1 = 7;
                        value2 = 8;
                        value3 = 1;
                        break;
                }

                // Healtheffect.transform.Find("health/healthup").GetComponent<Image>().sprite =
                //     Global.HealthIconSpriteAtlas.GetSprite("MonsterHealth" + value3);
                // Healtheffect.transform.Find("health/healtheffect").GetComponent<Image>().sprite =
                //     Global.HealthIconSpriteAtlas.GetSprite("MonsterHealth" + value2);
                //
                // Healtheffect.transform.Find("health/healthup").GetComponent<Image>().color = Color.white;
            }
            else
            {
                // Healtheffect.transform.Find("health/healthup").GetComponent<Image>().color = new Color(1, 1, 1, 0);
                // Healtheffect.transform.Find("health/healtheffect").GetComponent<Image>().sprite =
                //     Global.HealthIconSpriteAtlas.GetSprite("MonsterHealth10");
            }
        }

        private void UpdateCurrentBarFillAmount(float fillAmount)
        {
            SetBarFillAmount(fillAmount);
            displayedFillAmount = fillAmount;
        }

        private void SetBarFillAmount(float fillAmount)
        {
            if (Healtheffect == null)
            {
                return;
            }

            Healtheffect.transform.Find("health/healthEffect").GetComponent<Image>().fillAmount = fillAmount;
            Image healthDown = Healtheffect.transform.Find("health/healthBar").GetComponent<Image>();
            RectTransform healthDownRect = healthDown.GetComponent<RectTransform>();
            RectTransform healthRect = healthDown.transform.parent.GetComponent<RectTransform>();
            healthDownRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, healthRect.rect.width * fillAmount);
        }

        private void OnDispose()
        {
            KillColorTween();
            monsterData.monsterTweens.KillAllTweens();
            if (AttackBehaviour != null)
            {
                AttackBehaviour.OnCleanup(this);
            }
            lvdoutween.Kill();
            BuffController?.ClearAll();
            ReportEnemyDefeatedIfNeeded();

            DOTween.Kill(transform, true);
        }

        private void ReportEnemyDefeatedIfNeeded()
        {
            if (_hasReportedDefeat || GamePlayManager.Default == null)
            {
                return;
            }

            if (monsterData.monsterState != MonsterState.die)
            {
                return;
            }

            _hasReportedDefeat = true;
            GamePlayManager.Default.OnEnemyDefeated(this);
        }

        public Transform Transform => transform;

        /// <summary>
        /// 添加Buff
        /// </summary>
        public void AddBuff(IBuff buff, object caster = null)
        {
            if (buff == null || BuffController == null)
                return;

            if (monsterData.monsterState == MonsterState.die || hp <= 0)
                return;

            if (caster != null) buff.Caster = caster;
            if (!NotifyMechanicsBuffAdd(buff))
            {
                return;
            }

            BuffController.AddBuff(buff);
        }

        private int GetDropGold()
        {
            /*
                var difficulty = Global.Difficulty;
                var dropGoldDifficileDouble = GamePlayManager.Default.GameBattleConst.GetOrDefault("DropGoldDifficileDouble").Value.Split("|");
                var difficileDouble = float.Parse(dropGoldDifficileDouble[difficulty.Value - 1]);
                var addDropGold = (dropGold * difficileDouble);
                if (growthCoins > 0)
                {
                    addDropGold += (growthCoins * _level - 1);// 成长金币
                }*/
            // LogTool.Log(">>>> GetDropGold - _cardId: " + _cardId + ", difficulty: " + difficulty.Value + ", monsterDropGold: " + dropGold + ", difficileDouble: " + difficileDouble + ", addDropGold: " + addDropGold);

            return (int)_monsterConfig.DropGold;
        }

        /**
         *播放特效暴击落雷效果
         * 
         * @return 
         * @author Zpp
         * @create 2025/12/30
         **/
        private void PlayEffectCriticalThunderbolt(ISkill skillCard, Vector3 pos, float thunderDamageVal = 0f)
        {
            SoundManager.Instance.PlayVoiceInGame(11003);// 落雷音效
            
            // 落雷大小
            var localScale = Mathf.Max(skillCard.Attributes.GetFinal(AttributeType.SkillCriticalThunderboltScale), 0.5f);
            var (_, bullet) = ObjectPoolManager.Instance.PlayGameObjectPool(new GameObjectPoolData()
            {
                PoolName = "AnnaSkillRandomShoot08",
                ParentTran = GameLand.Default.Bullet,
                Position = pos,
                LocalScale = localScale * 100f,
                DelayReturn = 3f
            });
            bullet.transform.Find("ani_yx_jn1_08/box").GetComponent<ArrowBullet>().
                StartAttackSet(skillCard, thunderDamageVal, 2.5f, true, TakeDamageType.SchoolDamage);
        }

        /**
         * 连续造成伤害次数
         *
         * @return
         * @author Zpp
         * @create 2026/1/7
         **/
        private int _continuousDamageNum = 0;
        private Tween _continuousDamageTween = null;
        private void ContinuousDamageNum(ISkill skillCard, float healthPercentage)
        {
            var continuousDamage = GamePlayManager.Default.ModuleAttributes.GetFinal(AttributeType
                    .MonsterContinuousDamage3CauseTrueDamage);
            if (continuousDamage <= 0f) return;
            
            // 停止之前计时
            _continuousDamageTween?.Kill();

            // 累加连续受击次数
            _continuousDamageNum++;

            // 判断是否达成条件
            if (_continuousDamageNum > 3)
            {
                _continuousDamageNum = 0;
                var position = transform.position;
                ObjectPoolManager.Instance.PlayGameObjectPool(new GameObjectPoolData()
                {
                    PoolName = "BondAttackEffect02",
                    ParentTran = GameLand.Default.Bullet,
                    Position = position,
                    LocalScale = 120f,
                    DelayReturn = 1.7f
                });
                
                // 最大生命值3%的真实伤害
                var realDamage = Mathf.CeilToInt(healthPercentage * continuousDamage);
                ReceiveDamage(MonsterDamageRequest.Single(
                    this,
                    TakeDamageType.SchoolDamage,
                    realDamage,
                    skillCard,
                    position,
                    pipelineType: DamagePipelineType.BuffSkillDamage,
                    isAgainDamage: true));
                return;
            }

            // 三秒后重置
            _continuousDamageTween = DOVirtual.DelayedCall(2f, () => _continuousDamageNum = 0);
        }

        private void ShowShieldBreakIcon(float delay = 0.5f)
        {
            ShieldBreakIcon.gameObject.SetActive(true);
            DOVirtual.DelayedCall(delay, () => ShieldBreakIcon.gameObject.SetActive(false));
        }
    }
}