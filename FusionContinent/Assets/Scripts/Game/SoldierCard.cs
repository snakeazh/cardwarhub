using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Config;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using FusionContinent;
using UnityEngine;
using QFramework;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Sequence = DG.Tweening.Sequence;
using FusionContinent.Assassin;
using FusionContinent.Attribute;
using FusionContinent.Buff;
using FusionContinent.Helper;
using FusionContinent.Manager;
using FusionContinent.Model;
using FusionContinent.Module;
using FusionContinent.Skill;
using FusionContinent.Utils;
using Game.Battle;
using Game.StateMachine;
using Helper;
using Manager;
using Spine.Unity;
using TMPro;
using Image = UnityEngine.UI.Image;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using FusionContinent.Target;
using ValueType = Config.ValueType;

namespace FusionContinent
{
    /// <summary>
    /// 英灵战场卡牌：聚合状态机、属性、技能、Buff、血条、拖拽与受击入口。
    /// <para>职责边界：</para>
    /// <list type="bullet">
    /// <item>编排战斗流程（Idle→攻击/施法→Idle、死亡/复活）</item>
    /// <item>委托子组件处理技能字典、主动释放、动画、Buff、血条</item>
    /// <item>受击仅做 <see cref="CanReceiveIncomingDamage"/> 门禁与 <see cref="TakeDamage"/> 委托，不含伤害公式</item>
    /// </list>
    /// <para>文档：<c>Soldier/SoldierCard_README.md</c>（总览）、<c>DamageCalculator/DamageSystem_README.md</c>（伤害）、<c>Soldier/SoldierCard_Refactor_Context.md</c>（技能组件）</para>
    /// </summary>
    public partial class SoldierCard : ViewController, IPointerDownHandler, IPointerUpHandler, IDragHandler, IBuffable
    {
        #region 状态机

        /// <summary>英灵行为状态（位标志，供 Transition 条件读取）</summary>
        public enum SoldierStateType
        {
            None = 0,
            Idle = 1 << 0,      // 待机
            Attacking = 1 << 1, // 攻击
            CastingSkill = 1 << 2, // 释放技能
            Dead = 1 << 3       // 死亡
        }


        private float NormalOverTime = 0f;

        private StateMachine _stateMachine;

        /// <summary>预创建的状态实例，由 AttackingState / CastingSkillState 等驱动动画与攻击</summary>
        public SoldierIdleState soldierIdleState { get; private set; }
        public SoldierAttackingState soldierAttackingState { get; private set; }
        public SoldierCastingSkillState soldierCastingSkillState { get; private set; }
        public SoldierDeadState soldierDeadState { get; private set; }

        /// <summary>当前状态类型，通过 <see cref="SwitchState"/> 写入，Transition 据此切换 State 实例</summary>
        private SoldierStateType _currentStateType = SoldierStateType.Idle;

        /// <summary>供 SkillExecutor 等外部组件读取当前状态，避免直接访问状态机</summary>
        public SoldierStateType CurrentStateType => _currentStateType;

        /// <summary>
        /// 初始化状态机
        /// </summary>
        private void InitializeStateMachine()
        {
            if (_stateMachine == null)
            {
                _stateMachine = new StateMachine();

                // 创建各个状态
                soldierIdleState = new SoldierIdleState(_stateMachine, this);
                soldierAttackingState = new SoldierAttackingState(_stateMachine, this);
                soldierCastingSkillState = new SoldierCastingSkillState(_stateMachine, this);
                soldierDeadState = new SoldierDeadState(_stateMachine, this);

                // Idle 状态转换
                soldierIdleState.AddTransition(new Transition(() => _currentStateType == SoldierStateType.Attacking,
                    soldierAttackingState));
                soldierIdleState.AddTransition(new Transition(() => _currentStateType == SoldierStateType.CastingSkill,
                    soldierCastingSkillState));
                soldierIdleState.AddTransition(new Transition(() => _currentStateType == SoldierStateType.Dead,
                    soldierDeadState));

                // Attacking 状态转换
                soldierAttackingState.AddTransition(new Transition(() => _currentStateType == SoldierStateType.Idle,
                    soldierIdleState));
                soldierAttackingState.AddTransition(
                    new Transition(() => _currentStateType == SoldierStateType.CastingSkill, soldierCastingSkillState));
                soldierAttackingState.AddTransition(new Transition(() => _currentStateType == SoldierStateType.Dead,
                    soldierDeadState));

                // CastingSkill 状态转换
                soldierCastingSkillState.AddTransition(new Transition(() => _currentStateType == SoldierStateType.Idle,
                    soldierIdleState));
                //soldierCastingSkillState.AddTransition(new Transition(() => _currentStateType == SoldierStateType.Attacking, soldierAttackingState));
                soldierCastingSkillState.AddTransition(new Transition(() => _currentStateType == SoldierStateType.Dead,
                    soldierDeadState));

                // Dead 状态转换
                soldierDeadState.AddTransition(new Transition(() => _currentStateType == SoldierStateType.Idle,
                    soldierIdleState));
            }

            // 设置初始状态
            _stateMachine.CurrentState = soldierIdleState;
            _currentStateType = SoldierStateType.Idle;
        }

        /// <summary>
        /// 切换状态（内部使用）
        /// </summary>
        public void SwitchState(SoldierStateType stateType)
        {
            _currentStateType = stateType;
        }

        #endregion

        #region 身份与基础字段

        /// <summary>卡牌配置 Id，对应 Luban TbCardBaseConfig / TbSoldierBattleConfig</summary>
        public int cardId;
        private Vector3 selfScale;
        public BindableProperty<int> level = new BindableProperty<int>(1);
        private BindableProperty<int> exp = new BindableProperty<int>(0);
        private int barExp;
        /// <summary>是否已死亡；Die 后置 true，ReviveSoldier 重置</summary>
        public bool isDie;
        public bool IsDie() => isDie;

        /// <summary>当前普攻/技能释放的目标快照，由 EnemyTargetFinder 构建</summary>
        public BattleTargetInfo nowTargetInfo;
        public MainAttributeType MainAttrType;

        /// <summary>战场角色类型：Self 本体 / Mirror 镜像（不受击）/ CopyBody 复制体</summary>
        public SoldierRoleType RoleType;

        public int CurrentHp
        {
            set { _hp.Value = value; }
            get { return _hp.Value; }
        }
        private BindableProperty<int> _hp = new BindableProperty<int>(0);
        private BindableProperty<int> _maxHp = new BindableProperty<int>(0);
        
        public int MaxHp
        {
            get
            {
                var maxHp = (int)BaseAttributes.GetFinal(AttributeType.SoldierMaxHealth);
                return maxHp ;
            }
        }
        public BindableProperty<int> attack = new BindableProperty<int>(0);
        public int spellPower;
        public float attackTime;
        /// <summary>普攻索敌与攻击范围检测半径（配置 + SoldierAttackRange 属性）</summary>
        public float attackRange;
        public float attackDuration;
        public float criticalRate;
        public float criticalDamage;
        public float missileSpeed;
        private float missRate;
        /// <summary>普通护盾剩余值，由 ShieldResolver / SoldierDamageReceiver 读写</summary>
        private int shield;

        /// <summary>距离下次普攻的剩余 CD（秒）；Update 中递减，PlayAtkAnimation 重置</summary>
        private float nextAttackTime = 0f;
        
        //血条线
        public RectTransform LineHpParent;
        public GameObject LineHpObj;
        
        // 总伤害
        public int TotalDamage;

        // 抗性属性
        private Dictionary<ResistanceType, int> resistanceProperty = new Dictionary<ResistanceType, int>();

        // 配置
        private TbCardBaseConfig _tbCardBaseConfig;
        private TbSoldierBattleConfig _tbSoldierConfig;
        private TbSpecialEffectsConfig _tbSpecialEffectsConfig;
        // private Sequence atkTween;
        private Tween damageTween;

        public float MonsterMoveSpeed;

        public Vector3 CardStartScale;
        public float[] LevelScale;
        private GameObject smoke;

        private Tweener atkActionDOPunch;
        private GameObject raiseHand;
        public Vector3 cardLevelScale;

        public GameObject PlayerspineObj;

        public SoldierBattleConfig soldierBattle;
        private SoldierLevelPropertyList soldierLvCfg;

        /// <summary>普攻锁定目标，由 SoldierAtkRange 通过 SetClosestTarget 更新</summary>
        [HideInInspector] public MonsterCard currentTarget;
        [HideInInspector] public SoldierCard CurrentFCard; // 当前锁定的融合目标
        [HideInInspector] public bool isDrag = false;
        [HideInInspector] public bool isCell = false; // 是否出售此卡
        public bool isLevelUp = false; // 是否升级
        [HideInInspector] public bool restrictLeft;
        [HideInInspector] public bool restrictRight;
        [HideInInspector] public bool restrictTop;
        [HideInInspector] public bool restrictBottom;
        [HideInInspector] public string raisehandName;
        [HideInInspector] public string projectileName; // 子弹名
        [HideInInspector] public float projectileScale; // 子弹大小
        GameObject projectile;

        public bool isAtkLock;

        // 生存时间
        private float survivalTime = 0f;
        private float currentSurvivalTime = 0f;

        /// <summary>战斗属性真值源，Init 时 InitFromConfig + 模块/皮肤 Modifier</summary>
        public AttributeSet BaseAttributes = new();

        public SoldierSkillComponent SkillComponent { get; private set; }
        /// <summary>来自 TbSoldierBattleConfig.NormalAttackSkillId</summary>
        public int NormalAttackSkillId;
        public int NormalAttackCount;
        public int CriticalContinueCount;

        /// <summary>攻击暴击伪随机状态（本局该英灵独立记账）。</summary>
        public readonly PseudoRandomProbability CritPseudoRandom = new PseudoRandomProbability();

        /// <summary>
        /// 伪随机暴击判定。
        /// </summary>
        /// <param name="critRate">暴击率；支持 0~1 或 0~100（与 GetFinal(SoldierCriticalRate) 一致时为 0~100）。</param>
        public bool RollCriticalHit(float critRate)
        {
            float rate01 = critRate > 1f ? critRate / 100f : critRate;
            rate01 = Mathf.Clamp01(rate01);
            return CritPseudoRandom.Roll(rate01, cardId, 0, "Crit");
        }

        /// <summary>六律·技等：施法计数达标后，下一招主动技能必暴（不含触发达标的那次 skillId 伤害）。</summary>
        public bool NextSkillMustCritical { get; private set; }

        public int NextSkillMustCriticalSkipSkillId { get; private set; }

        public void MarkNextSkillMustCritical(int castSkillId)
        {
            NextSkillMustCritical = true;
            NextSkillMustCriticalSkipSkillId = castSkillId;
        }

        public bool HasPendingNextSkillMustCritical(int damageSkillId)
        {
            return NextSkillMustCritical
                && damageSkillId > 0
                && damageSkillId != NormalAttackSkillId
                && damageSkillId != NextSkillMustCriticalSkipSkillId;
        }

        public bool ConsumeNextSkillMustCritical(int damageSkillId)
        {
            if (!HasPendingNextSkillMustCritical(damageSkillId))
            {
                return false;
            }

            NextSkillMustCritical = false;
            NextSkillMustCriticalSkipSkillId = 0;
            return true;
        }

        public bool CannotNormalAttack;
        public bool NoHealthBar;
        
        /// <summary>本回合受击次数，每次成功结算伤害后由 SoldierDamageReceiver 累加。</summary>
        public int RoundBeHitCount;

        private Tween _healthRegenPerSecondTween;
        private Tween _everyScePerMaxHpTween;
        private Tween _survivalMoveTween;
        private bool _runtimeSubscriptionsActive;
        
        /// <summary>主体持有的复制体列表；主体 Die 时联动全部 CopyBody Die</summary>
        public List<SoldierCard> CopyBodyList = new List<SoldierCard>();

        public SoldierBuffController BuffController { get; private set; }
        public SoldierHealthBarController HealthBarController { get; private set; }
        public SoldierAnimationController AnimationController { get; private set; }
        private Action<bool> _pauseChangedHandler;
        public SoldierSkillExecutor SkillExecutor { get; private set; }

        #endregion

        #region Unity 生命周期

        /// <summary>加载配置表、初始化状态机与子组件（不含卡牌数值，数值在 Init 中设置）</summary>
        private void Awake()
        {
            _tbCardBaseConfig = DataConfigManager.Tables.TbCardBaseConfig;
            _tbSoldierConfig = DataConfigManager.Tables.TbSoldierBattleConfig;
            _tbSpecialEffectsConfig = DataConfigManager.Tables.TbSpecialEffectsConfig;

            // 初始化状态机
            InitializeStateMachine();

            // 初始化控制器
            AnimationController = new SoldierAnimationController(this);
            SkillExecutor = new SoldierSkillExecutor(this);
            SkillComponent = new SoldierSkillComponent(this);
            BuffController = new SoldierBuffController(this);
            HealthBarController = new SoldierHealthBarController(this);
        }

        /// <summary>
        /// 战场实例化入口：重置运行时状态，从配置与模块属性构建 BaseAttributes，
        /// 创建 Spine、学习 InitSpecialIds 技能并注册属性订阅。
        /// </summary>
        /// <param name="id">卡牌配置 Id</param>
        /// <param name="cardLevel">英灵等级</param>
        /// <param name="roleType">Self / Mirror / CopyBody</param>
        public void Init(int id, int cardLevel, SoldierRoleType roleType = 0)
        {
            ClearRuntimeSubscriptions();
            ClearDotweenRuntime();

            SkillComponent.Reset();
            this.GetComponent<Animator>().enabled = false;
            // CannotNormalAttack = roleType == SoldierRoleType.Mirror;

            BaseAttributes.InitFromConfig(id,cardLevel);
            
            cardId = id;
            RoleType = roleType;
            level.Value = cardLevel;
            
            // 获取攻击相关配置
            soldierBattle = _tbSoldierConfig.GetOrDefault(cardId);
            soldierLvCfg = soldierBattle.LevelPropertyList[level.Value - 1];

            // selfType = type;
            exp.Value = 0;
            LevelScale = new float[] { 1.5f };

            MainAttrType = soldierBattle.MainAttrType;
            selfScale = transform.localScale;
            CardStartScale = transform.localScale;
            cardLevelScale = CardStartScale *
                             (level.Value > LevelScale.Length ? LevelScale.Last() : LevelScale[level.Value - 1]);
            if (level.Value > LevelScale.Length)
            {
                cardLevelScale *= LevelScale.Last();
            }
            
            // 普通攻击技能id
            NormalAttackSkillId = soldierBattle.NormalAttackSkillId;
            
            // 角色骨骼
            if (soldierBattle.Spine == "")
            {
                throw new Exception($"英灵没有配置Spine: {soldierBattle.Type}");
            }
            
            /*
            Icon.transform.localScale = Vector3.zero;
            PlayerspineObj = GamePlayManager.Default._resLoader.LoadSync<GameObject>(soldierBattle.Spine).Instantiate();
            PlayerspineObj.transform.SetParent(transform);
            PlayerspineObj.transform.localScale = soldierBattle.SpineScale > 0 ? Vector3.one * soldierBattle.SpineScale : Vector3.one;
            PlayerspineObj.transform.SetSiblingIndex(1);
            PlayerspineObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -135f);
            */
            
            // 改为:
            Icon.transform.localScale = Vector3.zero;
            AnimationController.CreateSpineObject(soldierBattle.Spine, soldierBattle.SpineScale);
            
            HealthBarController.SetHealthBarVisible(true);
            if (roleType == SoldierRoleType.Mirror)
            {
                // 镜像体不显示血条
                HealthBarController.SetupForMirror();
            }

            // 数值继承
            var valueInherit = 1f;
            
            // 英灵镜像（复制体）
            var soldierMirror = GamePlayManager.Default.ModuleAttributes.GetFinal(AttributeType.OneManArmy);
            if (soldierMirror > 0f) {
                valueInherit = soldierMirror;// 复制体继承本体伤害
            }
            
            // 初始配置属性
            // foreach (var attr in soldierBattle.BaseAttributes)
            // {
            //     BaseAttributes.GetAttr(attr.Type).SetBaseValue(attr.Value * valueInherit);
            // }
            
            // 初始配置主属性
            // MainAttributes = new(MainAttrType, BaseAttributes);
            // foreach (var attr in soldierLvCfg.MainAttributes)
            // {
            //     MainAttributes.Set(attr.Type, attr.Value * valueInherit);
            // }
            
            // 外部模块聚合属性加成
            foreach (var attr in GamePlayManager.Default.ModuleAttributes.GetAll())
            {
                BaseAttributes.GetAttr(attr.Key).SetBaseValue(attr.Value.FinalValue * valueInherit, true);
            }
            // foreach (var attr in GamePlayManager.Default.ModuleMainAttributes)
            // {
            //     MainAttributes.Set(attr.Key, attr.Value * valueInherit, true);
            // }
            
            // 皮肤属性（玩家读 Global；对侧读 opponentInfo 初始化的 BattleSession.Opponent）
            FusionContinent.Model.PlayerCard playerCard = null;
            if (BattleSide == BattleSide.Opponent)
            {
                if (BattleSession.Opponent != null)
                {
                    playerCard = BattleSession.Opponent.GetCardById(cardId);
                }
            }
            else
            {
                playerCard = Global.GetCardById(cardId);
            }

            if (playerCard != null && playerCard.ObjData != null && playerCard.ObjData.Skin > 0)
            {
                var skinCfg = DataConfigManager.Tables.TbSkinConfig.GetOrDefault(playerCard.ObjData.Skin);
                if (skinCfg != null && skinCfg.Attributes.Count > 0)
                {
                    foreach (var attr in skinCfg.Attributes)
                    {
                        BaseAttributes.GetAttr(attr.Type).AddModifier(new AttributeModifier
                        {
                            SourceKey = "SkinAttribute",
                            SourceType = AttributeSourceType.Skin,
                            FlatAdd = attr.ValueType == ValueType.Number ? attr.Value * valueInherit : 0,
                            PercentAdd = attr.ValueType == ValueType.Percentage ? attr.Value * valueInherit : 0
                        });
                    }
                }
            }

            // 初始角色血量
            ResetSoldierHp();
            
            // 监听 - 攻击距离
            attackRange = soldierBattle.AttackRange > 0 ? soldierBattle.AttackRange : 1.0f;
            attackRange += BaseAttributes.GetFinal(AttributeType.SoldierAttackRange);
            
            smoke = transform.Find("kapai_smoke01").gameObject;
            smoke.SetActive(false);
            // 开始禁止攻击
            isAtkLock = true;

            // 初始攻击力
            // attack.Value = soldierBattle.Attack;

            // 初始攻击速度
            
            attackDuration = soldierBattle.AttackDuration > 0 ? soldierBattle.AttackDuration : 2.0f;
            missileSpeed = soldierBattle.MissileSpeed >= 0
                ? soldierBattle.MissileSpeed * (1 + Global.GemProjectileSpeed) / 100
                : 0;
            projectileScale = soldierBattle.ProjectileScale > 0 ? soldierBattle.ProjectileScale : 1;

            // 初始抗性
            foreach (var resistance in soldierLvCfg.Resistances)
            {
                resistanceProperty.Add(resistance.Type, resistance.Value);
            }


            Frame.gameObject.SetActive(false);
            ProgressBar.gameObject.SetActive(false);
            ExpBar.gameObject.SetActive(false);
            Icon.sprite = Global.CardIconSpriteAtlas.GetSpriteExt(_tbCardBaseConfig.GetOrDefault(id).Icon);

            NameText.text = _tbCardBaseConfig.GetOrDefault(id).Name;
            HitBox.gameObject.SetActive(true);
            HitBox.Initialize(this);

            AttackRange.gameObject.SetActive(true);
            LevelText.text = $"{level}";
            string hexColor = "#78fdf4";
            UnityEngine.Color color;
            if (UnityEngine.ColorUtility.TryParseHtmlString(hexColor, out color))
            {
                LevelText.color = color;
                NameText.color = color;
            }

            if (level.Value >= 1 && level.Value <= 5)
            {
                //SelfShadow.GetComponent<Image>().sprite = GamePlayManager.Default._bgCardIcon.GetSprite("己方blue");
                string hexColor1 = "#78fdf4";
                UnityEngine.Color color1;
                if (UnityEngine.ColorUtility.TryParseHtmlString(hexColor1, out color1))
                {
                    LevelText.color = color1;
                    NameText.color = color1;
                }
            }

            if (level.Value >= 6 && level.Value <= 10)
            {
                //SelfShadow.GetComponent<Image>().sprite = GamePlayManager.Default._bgCardIcon.GetSprite("己方purple");
                string hexColor2 = "#e84dff";
                UnityEngine.Color color2;
                if (UnityEngine.ColorUtility.TryParseHtmlString(hexColor2, out color2))
                {
                    LevelText.color = color2;
                    NameText.color = color2;
                }
            }

            if (level.Value > 10)
            {
                //SelfShadow.GetComponent<Image>().sprite = GamePlayManager.Default._bgCardIcon.GetSprite("己方gold");
                string hexColor3 = "#fffe91";
                UnityEngine.Color color3;
                if (UnityEngine.ColorUtility.TryParseHtmlString(hexColor3, out color3))
                {
                    LevelText.color = color3;
                    NameText.color = color3;
                }
            }

            RegisterVillagerAI();

            // 角色类型
            switch (RoleType)
            {
                // 改为:
                case SoldierRoleType.Mirror:
                case SoldierRoleType.CopyBody:
                    AnimationController.SetTransparencyForCopyBody();
                    break;
            }
            
            // 初始技能
            foreach (var specialId in soldierBattle.InitSpecialIds)
            {
                LearnSkill(specialId, 1);
            }

            RegisterRuntimeSubscriptions();
            RefreshInitialHealthBarDisplay();
        }

        private void RefreshInitialHealthBarDisplay()
        {
            var maxHp = (int)BaseAttributes.GetFinal(AttributeType.SoldierMaxHealth);
            CurrentHp = (int)BaseAttributes.GetFinal(AttributeType.SoldierHealth);
            HealthBarController.UpdateHealthBar(CurrentHp, maxHp);
        }

        #endregion

        #region 运行时订阅

        /// <summary>注册属性变化、经验、全局事件与长按详情；Init 与 Revive 后调用，Die 前对称 Clear</summary>
        private void RegisterRuntimeSubscriptions()
        {
            if (_runtimeSubscriptionsActive) return;
            _runtimeSubscriptionsActive = true;

            BaseAttributes.RegisterListener(AttributeType.SoldierHealth, OnSoldierHealthChanged);
            BaseAttributes.RegisterListener(AttributeType.SoldierMaxHealth, OnSoldierMaxHealthChanged);
            BaseAttributes.RegisterListener(AttributeType.SoldierNumOfAttackSpeed, OnSoldierNumOfAttackSpeedChanged);
            BaseAttributes.RegisterListener(AttributeType.SoldierAttackSpeed, OnSoldierAttackSpeedChanged);
            BaseAttributes.RegisterListener(AttributeType.SoldierAttackRange, OnSoldierAttackRangeChanged);
            BaseAttributes.RegisterListener(AttributeType.SoldierAttackPower, OnSoldierAttackPowerChanged);
            BaseAttributes.RegisterListener(AttributeType.SoldierHealthRegenPerSecond, OnHealthRegenPerSecondChanged);
            BaseAttributes.RegisterListener(AttributeType.SoldierEveryScePerMaxHp, OnEveryScePerMaxHpChanged);

            OnExpChanged(exp.Value);
            exp.Register(OnExpChanged);

            OnConsecutiveSkillReleaseDamageBonusChanged(Global.ConsecutiveSkillReleaseDamageBonus.Value);
            Global.ConsecutiveSkillReleaseDamageBonus.Register(OnConsecutiveSkillReleaseDamageBonusChanged);

            Global.Publisher.Subscribe(MessagePublisher.CHANGE_SCHOOL_STAR, OnPublisherSchoolStarChanged);

            var longClick = GetComponent<CardSlotLongClick>();
            longClick.onLongPress.AddListener(OnHeroDetailsLongPress);
            longClick.onLongPressUp.AddListener(OnHeroDetailsLongPressUp);

            BindPauseState();
        }

        private void ClearRuntimeSubscriptions()
        {
            if (!_runtimeSubscriptionsActive) return;
            _runtimeSubscriptionsActive = false;

            BaseAttributes.GetAttr(AttributeType.SoldierHealth).OnFinalValueChanged -= OnSoldierHealthChanged;
            BaseAttributes.GetAttr(AttributeType.SoldierMaxHealth).OnFinalValueChanged -= OnSoldierMaxHealthChanged;
            BaseAttributes.GetAttr(AttributeType.SoldierNumOfAttackSpeed).OnFinalValueChanged -= OnSoldierNumOfAttackSpeedChanged;
            BaseAttributes.GetAttr(AttributeType.SoldierAttackSpeed).OnFinalValueChanged -= OnSoldierAttackSpeedChanged;
            BaseAttributes.GetAttr(AttributeType.SoldierAttackRange).OnFinalValueChanged -= OnSoldierAttackRangeChanged;
            BaseAttributes.GetAttr(AttributeType.SoldierAttackPower).OnFinalValueChanged -= OnSoldierAttackPowerChanged;
            BaseAttributes.GetAttr(AttributeType.SoldierHealthRegenPerSecond).OnFinalValueChanged -= OnHealthRegenPerSecondChanged;
            BaseAttributes.GetAttr(AttributeType.SoldierEveryScePerMaxHp).OnFinalValueChanged -= OnEveryScePerMaxHpChanged;

            exp.UnRegister(OnExpChanged);
            Global.ConsecutiveSkillReleaseDamageBonus.UnRegister(OnConsecutiveSkillReleaseDamageBonusChanged);

            Global.Publisher.Unsubscribe(MessagePublisher.CHANGE_SCHOOL_STAR, OnPublisherSchoolStarChanged);

            var longClick = GetComponent<CardSlotLongClick>();
            longClick.onLongPress.RemoveListener(OnHeroDetailsLongPress);
            longClick.onLongPressUp.RemoveListener(OnHeroDetailsLongPressUp);

            UnbindPauseState();
        }

        private void ClearDotweenRuntime()
        {
            if (damageTween != null)
            {
                damageTween.Kill();
                damageTween = null;
            }

            if (atkActionDOPunch != null)
            {
                atkActionDOPunch.Kill();
                atkActionDOPunch = null;
            }

            if (_healthRegenPerSecondTween != null)
            {
                _healthRegenPerSecondTween.Kill();
                _healthRegenPerSecondTween = null;
            }

            if (_everyScePerMaxHpTween != null)
            {
                _everyScePerMaxHpTween.Kill();
                _everyScePerMaxHpTween = null;
            }

            if (_survivalMoveTween != null)
            {
                _survivalMoveTween.Kill();
                _survivalMoveTween = null;
            }

            DOTween.Kill(transform, true);
        }

        private void OnSoldierHealthChanged(float currentF, float lastF)
        {
            if (isDie) return;

            CurrentHp = (int)currentF;
            var maxHp = (int)BaseAttributes.GetFinal(AttributeType.SoldierMaxHealth);
            if (CurrentHp > maxHp)
            {
                CurrentHp = maxHp;
                BaseAttributes.GetAttr(AttributeType.SoldierHealth).ResetFinalValue(maxHp);
            }

            var isFullHealth = CurrentHp >= maxHp;
            AddListenerFullHealth(isFullHealth);

            var residueHpPer = (float)CurrentHp / maxHp;
            TenaciousWillExtraAttribute(residueHpPer);
            HealthBelowPerExtraAttribute(residueHpPer);
            HealthBarController.UpdateHealthBar(CurrentHp, maxHp);

            BattleRuleHpChangeTrigger.Trigger(this);

            if (CurrentHp <= 0)
            {
                Die();
            }
        }

        private void OnSoldierMaxHealthChanged(float currentF, float lastF)
        {
            if (isDie) return;

            var diffV = (int)(currentF - lastF);
            if (diffV > 0)
            {
                BaseAttributes.SetAttr(AttributeType.SoldierHealth, diffV);
                DamageEffect(diffV);
                HealthBarController.SetLineHp((int)BaseAttributes.GetFinal(AttributeType.SoldierMaxHealth));
            }
        }

        private void OnSoldierNumOfAttackSpeedChanged(float currentF, float lastF)
        {
            if (isDie || lastF <= 0f)
            {
                return;
            }

            var speedRatio = currentF / lastF;
            if (speedRatio <= 0f || Mathf.Approximately(speedRatio, 1f))
            {
                return;
            }

            // 攻速值变化时缩放剩余普攻 CD，使满血 Buff 等加成即时生效
            if (nextAttackTime > 0f && nextAttackTime < 99f)
            {
                nextAttackTime /= speedRatio;
            }
        }

        private void OnSoldierAttackSpeedChanged(float currentF, float lastF)
        {
            if (isDie) return;

            var diffV = (int)(currentF - lastF);
            if (diffV > 0)
            {
                var modVals = BaseAttributes.GetAttr(AttributeType.SoldierAttackSpeedToNormalAttackDamage).SumModifierValues();
                if (modVals.Item1 > 0 || modVals.Item2 > 0)
                {
                    var addMod = new AttributeModifier
                    {
                        SourceKey = "SoldierAttackSpeedToNormalAttackDamage",
                        SourceType = AttributeSourceType.GeneralSkill,
                        FlatAdd = modVals.Item1,
                        PercentAdd = modVals.Item2,
                    };
                    GetSkillById(NormalAttackSkillId)?.Attributes.GetAttr(AttributeType.SkillBasicAttackPower).AddModifier(addMod);
                }
            }
        }

        private void OnSoldierAttackRangeChanged(float currentF, float lastF)
        {
            if (isDie) return;

            attackRange = soldierBattle.AttackRange + currentF;
            AttackRange.SetDetectionRange(attackRange);
        }

        private void OnSoldierAttackPowerChanged(float currentF, float lastF)
        {
            if (isDie) return;

            var extraSpellPowerRedouble = BaseAttributes.GetFinal(AttributeType.AttackPowerConvertExtraSpellPower);
            if (extraSpellPowerRedouble > 0f)
            {
                BaseAttributes.GetAttr(AttributeType.SoldierExtraSpellPower).AddModifier(new AttributeModifier
                {
                    SourceKey = "Base",
                    SourceType = AttributeSourceType.Base,
                    FlatAdd = currentF * extraSpellPowerRedouble
                });
            }

            ConditionManager.Instance.OnTriggerProgress(ConditionType.HeroAttackPower, (int)currentF, "", cardId);
        }

        private void OnHealthRegenPerSecondChanged(float recoverHp, float lastF)
        {
            if (isDie) return;

            if (recoverHp <= 0f) return;

            var mods = BaseAttributes.GetAttr(AttributeType.SoldierHealthRegenPerSecond).GetModifiers();
            if (mods.Count <= 0 || mods.First().Value.EffectParam <= 0f) return;

            if (_healthRegenPerSecondTween != null)
            {
                _healthRegenPerSecondTween.Kill();
                _healthRegenPerSecondTween = null;
            }

            _healthRegenPerSecondTween = DOVirtual.DelayedCall(mods.First().Value.EffectParam, () =>
                {
                    if (isDie) return;
                    SetSoldierHp((int)recoverHp);
                })
                .SetLoops(-1)
                .SetUpdate(false)
                .SetId(GlobalConst.BattleTween)
                .SetTarget(gameObject);
        }

        private void OnEveryScePerMaxHpChanged(float recoverMaxHpPercent, float lastF)
        {
            if (isDie) return;

            if (recoverMaxHpPercent <= 0f) return;

            var mods = BaseAttributes.GetAttr(AttributeType.SoldierEveryScePerMaxHp).GetModifiers();
            if (mods.Count <= 0 || mods.First().Value.EffectParam <= 0f) return;

            if (_everyScePerMaxHpTween != null)
            {
                _everyScePerMaxHpTween.Kill();
                _everyScePerMaxHpTween = null;
            }

            _everyScePerMaxHpTween = DOVirtual.DelayedCall(mods.First().Value.EffectParam, () =>
                {
                    if (isDie) return;
                    var maxHp = BaseAttributes.GetFinal(AttributeType.SoldierMaxHealth);
                    var recoverHp = Mathf.CeilToInt(maxHp * recoverMaxHpPercent);
                    if (recoverHp > 0)
                    {
                        SetSoldierHp(recoverHp);
                    }
                })
                .SetLoops(-1)
                .SetUpdate(false)
                .SetId(GlobalConst.BattleTween)
                .SetTarget(gameObject);
        }

        private void OnExpChanged(int expInt)
        {
            if (isDie) return;

            var levelUpNeedExp = GetLevelUpNeedExp(level.Value + 1);
            if (exp.Value >= levelUpNeedExp)
            {
                var expNum = exp.Value - levelUpNeedExp;
                level.Value++;
                exp.Value = expNum;
            }

            var progress = exp.Value / (float)GetLevelUpNeedExp(level.Value + 1);
            Exp.fillAmount = Mathf.Clamp01(progress);
        }

        private void OnConsecutiveSkillReleaseDamageBonusChanged(float bonus)
        {
            if (isDie) return;
        }

        private void OnHeroDetailsLongPress()
        {
            if (isDie) return;

            UIKit.OpenPanel<HeroDetails>(UILevel.PopUI).SetData(this);
            Global.Publisher.Publish(MessagePublisher.CHANGE_GUIDE_PROGRESS_SPE, NormalAttackSkillId);
        }

        private void OnHeroDetailsLongPressUp()
        {
            if (isDie) return;

            UIKit.ClosePanel<HeroDetails>();
        }

        private void OnPublisherSchoolStarChanged(object obj)
        {
            if (isDie || transform == null) return;

            var info = (ChangeSchoolStar)obj;
            if (info.SchoolId != NormalAttackSkillId) return;

            var schoolCfg = DataConfigManager.Tables.TbSchoolConfig.GetOrDefault(info.SchoolId);
            if (schoolCfg is not { SchoolType: SchoolType.Skill } || transform.Find("EffectPos") == null) return;

            foreach (var cfg in schoolCfg.CardCountList)
            {
                if (cfg.CardCount == info.StarLv && cfg.EffectName != "")
                {
                    var buffEffect = GamePlayManager.Default._resLoader.Get<GameObject>(cfg.EffectName)
                        .Instantiate();
                    buffEffect.transform.SetParent(transform.Find("EffectPos").transform);
                    buffEffect.transform.localPosition = Vector3.zero;
                    buffEffect.LocalScale(Vector3.one * 1.8f);
                    break;
                }
            }
        }

        #endregion

        #region 英雄血量

        // 主动改血接口；飘字走 DamageEffect，不走 SoldierDamageReceiver。

        /// <summary>计算指定等级升级所需经验：BaseProvideExp + GrowProvideExp * (lv-1)</summary>
        private int GetLevelUpNeedExp(int lv)
        {
            // 计算公式：10 * 2^（lv-1）
            var growProvideExp = Mathf.CeilToInt(soldierBattle.GrowProvideExp * (lv - 1));
            return (soldierBattle.BaseProvideExp + growProvideExp);
        }
        
        /// <summary>Init / Revive 时将当前血量设为最大生命并重置 isDie</summary>
        private void ResetSoldierHp()
        {
            // 初始血量
            var maxHp = BaseAttributes.GetFinal(AttributeType.SoldierMaxHealth);
            BaseAttributes.SetAttr(AttributeType.SoldierHealth, maxHp, ValueType.Number, false);
            isDie = false;
            HealthBarController.SetLineHp((int)maxHp);
        }

        /// <summary>增量改血（治疗/伤害）；受 SoldierCannotRestoreHealth 限制；发布满血/非满血条件事件</summary>
        public void SetSoldierHp(int v)
        {
            if (v == 0) return;
            
            // 英灵无法回复血量
            if (v > 0 && BaseAttributes.GetFinal(AttributeType.SoldierCannotRestoreHealth) > 0f)
            {
                return;
            }
        
            BaseAttributes.GetAttr(AttributeType.SoldierHealth).SetBaseValue(v, true);
            DamageEffect(v);
            var hp = (int)BaseAttributes.GetFinal(AttributeType.SoldierHealth);
            var maxHp = (int)BaseAttributes.GetFinal(AttributeType.SoldierMaxHealth);

            //满血
            if (hp >= maxHp)
            {
                EventBus.Publish(new ConditionsTypeEvent
                {
                    coditionsType = ConditionsType.HeroFullBlood,
                    caster = this,
                    target = null
                });
            }
            else
            {
                EventBus.Publish(new ConditionsTypeEvent
                {
                    coditionsType = ConditionsType.HeroNoFullBlood,
                    caster = this,
                    target = null
                });
            }
        }

        /// <summary>修改最大生命；可选 resetHp 将当前血量钳制到新的上限</summary>
        public void SetSoldierMaxHp(int v, bool resetHp = false)
        {
            // 英灵无法回复血量
            if (v > 0 && BaseAttributes.GetFinal(AttributeType.SoldierCannotRestoreHealth) > 0f)
            {
                return;
            }
            
            BaseAttributes.SetAttr(AttributeType.SoldierMaxHealth, v);
            var hp = (int)BaseAttributes.GetFinal(AttributeType.SoldierHealth);
            var maxHp = (int)BaseAttributes.GetFinal(AttributeType.SoldierMaxHealth);
            if (hp > maxHp || resetHp)
            {
                BaseAttributes.SetAttr(AttributeType.SoldierHealth, maxHp, ValueType.Number, false);
            }

            DamageEffect(maxHp);
        }

        #endregion

       
        #region 士兵AI

        private void RegisterVillagerAI()
        {
            AttackRange.Initialize(this, attackRange);
        }

        #endregion

        #region 攻击

        /// <summary>当前 CastingSkill 态正在释放的技能 Id，由 SkillExecutor 写入</summary>
        public int releaseSpecialId;

        public bool IsAtkLock()
        {
            return isAtkLock;
        }

        /// <summary>禁止普攻（放置初期、死亡、生存时间结束等）；可选清理抬手 Tween</summary>
        public void LockAttackState(bool clearAtkTween = false)
        {
            isAtkLock = true;
            // LogTool.Log(">>>>> LockAttackState");

            // 提前结束正在攻击动作

            // 清除抬手
            GameObject.Destroy(raiseHand); // 销毁抬手对象
            atkActionDOPunch.Kill();
            raiseHand = null;
        }

        /// <summary>Idle 态 Enter 时调用，允许进入普攻流程</summary>
        public void UnlockAttackState()
        {
            isAtkLock = false;
        }
        

        /// <summary>主动技能释放入口，委托 <see cref="SoldierSkillExecutor.StartReleaseSkill"/></summary>
        public bool StartReleaseSkill(int specialId, Vector3 targetPosition = default, bool useNearestEnemy = false)
        {
            return SkillExecutor.StartReleaseSkill(specialId, targetPosition, useNearestEnemy);
        }
        
        
        /// <summary>普攻入口：校验 CD/目标后设置 nextAttackTime 并进入 PlayAttackAnim</summary>
        public void PlayAtkAnimation()
        {
            //idle 状态才能攻击
            if (nextAttackTime > 0) return;
            if (isDie || GamePlayManager.Default.IsPaused()) return;
            ClearInvalidCurrentTarget();
            if (currentTarget == null) return;
            
            nowTargetInfo = EnemyTargetFinder.FromMonster(currentTarget);
            
            nextAttackTime = 1/BaseAttributes.GetFinal(AttributeType.SoldierAttackSpeed);
            
            AnimationController.SetAnimationTimeScale(BaseAttributes.GetFinal(AttributeType.SoldierAttackSpeed));
            PlayAttackAnim(NormalAttackSkillId, nowTargetInfo);
        }


        /// <summary>播放技能/普攻 Spine 动画，命中帧回调 ExecuteAttack；无动画则直接 ExecuteAttack</summary>
        public void PlayAttackAnim(int skillId, BattleTargetInfo targetInfo)
        {
            if (AnimationController != null && AnimationController.HasSkillAnimation(skillId.ToString()))
            {
                if (skillId != NormalAttackSkillId)
                {
                    nextAttackTime = 3;
                    //不为普攻的时候 动画不加速
                    AnimationController.SetAnimationTimeScale(1);
                }

                //释放技能后 释放成功会将攻击时间重置
                AnimationController.PlayAtkAnimation(skillId, () => { OnAttackHit(skillId, targetInfo); }, () =>
                {
                    // 主动技能播放完毕后，兜底恢复普攻计时，避免技能失败时 nextAttackTime 长时间停留在 99f
                    if (skillId != NormalAttackSkillId && nextAttackTime > 10f)
                    {
                        nextAttackTime = 1 / BaseAttributes.GetFinal(AttributeType.SoldierAttackSpeed);
                    }
                    // AnimationController.PlayIdle();
                    SwitchState(SoldierStateType.Idle);
                });
            }
            else
            {
                var skillCard = GetReadySkillById(skillId, isActiveRelease: true);
                if (skillCard == null)
                {
                    LogTool.LogWarning($"[SkillRelease-Anim] {cardId} 播放技能动画失败: GetReadySkillById({skillId}) 返回 null, releaseSpecialId={releaseSpecialId}, NormalAttackSkillId={NormalAttackSkillId}");
                    // 技能不可释放时立即回到待机，避免卡在 CastingSkill 导致后续普攻停滞
                    AnimationController.PlayIdle();
                    SwitchState(SoldierStateType.Idle);
                    return;
                }
                
                ExecuteAttack(skillCard, targetInfo);
                
                //部分技能会将人物的状态变为idle
                AnimationController.PlayIdle();
                SwitchState(SoldierStateType.Idle);
            }
        }
        
        private void OnAttackHit(int skillId, BattleTargetInfo targetInfo)
        {
            if (isDie || GamePlayManager.Default.IsPaused()) return;
            var skillCard = GetReadySkillById(skillId, isActiveRelease: true);
            if (skillCard == null)
            {
                LogTool.LogWarning($"[SkillRelease-Anim] {cardId} OnAttackHit 技能命中失败: GetReadySkillById({skillId}) 返回 null");
                // 命中阶段取不到技能时，恢复普攻计时，避免角色虽处于 Idle 但长期不出手
                if (skillId != NormalAttackSkillId)
                {
                    nextAttackTime = 1 / BaseAttributes.GetFinal(AttributeType.SoldierAttackSpeed);
                }
                return;
            }
            ExecuteAttack(skillCard, targetInfo);
        }
        
        /// <summary>命中帧：刷新目标、UseSkill、普攻时触发重击/破军</summary>
        private void ExecuteAttack(ISkill skillCard, BattleTargetInfo targetInfo)
        {
            //技能已经释放成功 下次攻击间隔变少
            if (skillCard == null)
            {
                LogTool.LogWarning($"[SkillRelease-Anim] {cardId} ExecuteAttack 失败: skillCard 为 null");
                return;
            }
            // #region 新的攻速逻辑
            // #endregion 新的攻速逻辑

            // 普攻攻击次数
            if (skillCard.Id == NormalAttackSkillId)
            {
                // 判断是否无法普攻
                if (CannotNormalAttack)
                {
                    LogTool.LogWarning($"[SkillRelease-Anim] {cardId} 普攻失败: CannotNormalAttack=true");
                    return;
                }
                
                NormalAttackCount++;
                // 每次普攻触发条件
                ConditionManager.Instance.OnTriggerProgress(ConditionType.EveryAtk, 1);
                // 重置普攻伤害继承
                skillCard.Attributes.GetAttr(AttributeType.SkillAttackInheritance).SetBaseValue(1);
            }
            else
            {
                nextAttackTime = 1/BaseAttributes.GetFinal(AttributeType.SoldierAttackSpeed);
                //开始倒计时普攻
            }
            
            if (!TryRefreshSkillCastDirection(skillCard.Id, targetInfo, out var skillTarget))
            {
                SetClosestTarget(null);
                skillTarget = Vector3.zero;
            }

            AnimationController.RotateToTarget(skillTarget);
            // 普通攻击或非Animator技能直接使用技能
            UseSkill(skillCard.Id, skillTarget);

            // 普攻攻击次数
            if (skillCard.Id == NormalAttackSkillId)
            {
                // 检查触发普攻重击
                CheckNormalAttackHeavyBlowSkill(skillTarget);
            }
        }

        /**
         * 计算攻击速度
         */
        public float SumAttackSpeedNum(float atkTweenInterval)
        {
            if (atkTweenInterval <= 0.01f) atkTweenInterval = 0.01f;
            return atkTweenInterval;
        }

        /**
         * 战士攻击动作
         */
        private void SoldierAttackAction(string directionName, float actionNum, float duration = 0.5f)
        {
            var pos = new Vector3(0, 0, 0);
            switch (directionName)
            {
                case "x":
                    pos.x += actionNum;
                    break;
                case "y":
                    pos.y += actionNum;
                    break;
            }

            atkActionDOPunch = this.transform.DOPunchPosition(pos, duration, 0, 0)
                .SetId(GlobalConst.BattleTween);
        }

        #endregion

        #region 卡牌拖拽

        /// <summary>友方战场站位索引，对应 GamePlayManager.friendlyPositions</summary>
        public int posIndex = 0;
        /// <summary>所属战场侧（pve=Player / pvp=Opponent）。</summary>
        public BattleSide BattleSide = BattleSide.Player;

        public GameObject effectxuanwo;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (RoleType == SoldierRoleType.Self)
            {
                Global.Publisher.Publish(MessagePublisher.BATTLE_SOLDIER_SELECTED, cardId);
            }

            DOTween.Kill(AttackRangeHintTweenId);
            ShowAttackRange(true);
            this.transform.Find("clone").localScale = Vector3.one;
            this.transform.Find("clone").position = transform.position;
            this.transform.Find("clone").GetComponent<Image>().sprite =
                Global.ConnectionIconAtlas.GetSprite(soldierBattle.BattleInIcon);
            // SpriteHelper.TryLoadTexture(soldierBattle.BattleInIcon, this.transform.Find("clone").GetComponent<Image>());
            
            if (GamePlayManager.Default.UnlockSoldierDetailPanel)
            {
                SoldierDetailsController.Default.InitSoldierDetails(this);
            }
            //selfScale = transform.localScale;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            //AudioKit.PlaySound("resources://Audio/卡牌放下音效");
            //smoke.SetActive(true);
            //smoke.transform.GetChild(0).GetComponent<ParticleSystem>().Play();

            //DOTween.To(v => { }, 0, 0, 0.2f).onComplete += () => { smoke.SetActive(false); };
            this.transform.Find("clone").localScale = Vector3.zero;
            DOTween.Kill(AttackRangeHintTweenId);
            ShowAttackRange(false);
            //transform.localScale = selfScale;
            //transform.rotation = Quaternion.identity;
           // SelfShadow.enabled = false;
            
            // 松手等待攻击速度
            //var atkSpeedTween = BaseAttributes.GetFinal(AttributeType.SoldierAttackSpeed);
           // DOTween.Sequence().AppendInterval(SumAttackSpeedNum(atkSpeedTween)).AppendCallback(() => isAtkLock = false);

            //isDrag = false;

            // 松手时隐藏经验条
            //ToggleExpBars(false);
           // SoldierDetailsController.Default.InitSoldierDetails(this);

            // 对侧英灵不参与玩家站位拖拽吸附
            if (BattleSide == BattleSide.Opponent)
            {
                return;
            }

            Vector3 newPos = GamePlayManager.Default.friendlyPositions[0].position;
            float distance = Vector3.Distance(this.transform.Find("clone").position,
                GamePlayManager.Default.friendlyPositions[0].position);
            int posThisIndex = 0;
            for (int i = 0; i < GamePlayManager.Default.friendlyPositions.Length; i++)
            {
                if (distance > Vector3.Distance(this.transform.Find("clone").position,
                        GamePlayManager.Default.friendlyPositions[i].position))
                {
                    posThisIndex = i;
                    distance = Vector3.Distance(this.transform.Find("clone").position,
                        GamePlayManager.Default.friendlyPositions[i].position);
                    newPos=GamePlayManager.Default.friendlyPositions[i].position;
                }
            }
            // 先关闭所有位置的高亮
            foreach (Transform friendlyPos in GamePlayManager.Default.friendlyPositions)
            {
                Transform floorShine = friendlyPos.Find("FloorShine");
                if (floorShine != null)
                {
                    floorShine.localScale = Vector3.zero; // 或者使用其他方式关闭高亮
                }
            }
            
             // 获取所有启用的
             var soldier = FindObjectsOfType<SoldierCard>(true)
                 .FirstOrDefault(card => card.gameObject.activeInHierarchy && card.posIndex == posThisIndex);
            if (soldier != null && GamePlayManager.Default.friendlyPositionsLocked[posThisIndex] && posThisIndex != posIndex)
            {
                newPos = GamePlayManager.Default.friendlyPositions[posThisIndex].position;
                soldier.transform.position = GamePlayManager.Default.friendlyPositions[posIndex].position;
                soldier.posIndex=posIndex;
                posIndex = posThisIndex;
                this.transform.position=newPos;
            }
            else
            {
                GamePlayManager.Default.friendlyPositionsLocked[posIndex] = false;
                GamePlayManager.Default.friendlyPositionsLocked[posThisIndex] = true;
                posIndex=posThisIndex;
                this.transform.position=newPos;
            }
            
            // 延时-检测相邻的角色
            ActionKit.Delay(0.3f, () => GamePlayManager.Default.DetectAdjacentSoldier()).Start(this);
        }

        /// <summary>拖动 clone 预览并高亮最近站位；受 CanDrag / SoldierCanDrag 限制</summary>
        public void OnDrag(PointerEventData eventData)
        {
            // 判断角色是否可以拖动
            if (soldierBattle.CanDrag == 0) return;

            //角色能否被拖动,通过属性系统配置.
            var canMove = BaseAttributes.GetFinal(AttributeType.SoldierCanDrag);
            if (canMove > 0f) return;
            
            // 攻击暂停
            //LockAttackState(true);

           // transform.SetAsLastSibling();
          //  GameLand.Default.AttackRangeCircle.transform.position = transform.position;

           // isDrag = true;
           // AttackRange._currentTarget = null;
            //currentTarget = null;
           // currentTargetPositions = Vector3.zero;

            // 放大浮动
            //transform.localScale = selfScale * 1.2f;
            //SelfShadow.enabled = true;
            if (Camera.main != null)
            {
                Vector3 newPosition = Camera.main.ScreenToWorldPoint(eventData.position);
                newPosition.z = 0;

                // 计算鼠标的偏移量
                Vector3 delta = newPosition - transform.position;

                // 设置物体的旋转角度
                float yAngle = Mathf.Clamp(delta.x * -1000f, -10f, 10f); // 根据 X 方向的移动设置 Y 轴旋转角度，限制在 -10 到 10 度之间
                float xAngle = Mathf.Clamp(delta.y * 1000f, -5f, 5f); // 根据 Y 方向的移动设置 X 轴旋转角度，限制在 -10 到 10 度之间
                this.transform.Find("clone").rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(xAngle, 0, yAngle * 0.4f), Time.deltaTime * 10);
                this.transform.Find("clone").position = newPosition;
                
                // 找到最近的友方位置并高亮其FloorShine
                Transform closestPos = null;
                float minDistance = Mathf.Infinity;

                foreach (Transform friendlyPos in GamePlayManager.Default.friendlyPositions)
                {
                    float currentDistance = Vector3.Distance(this.transform.Find("clone").position, friendlyPos.position);
                    if (currentDistance < minDistance)
                    {
                        minDistance = currentDistance;
                        closestPos = friendlyPos;
                    }
                }

                // 先关闭所有位置的高亮
                foreach (Transform friendlyPos in GamePlayManager.Default.friendlyPositions)
                {
                    Transform floorShine = friendlyPos.Find("FloorShine");
                    if (floorShine != null)
                    {
                        floorShine.localScale = Vector3.zero; // 或者使用其他方式关闭高亮
                    }
                }

                // 然后只高亮最近的位置
                if (closestPos != null)
                {
                    Transform closestFloorShine = closestPos.Find("FloorShine");
                    if (closestFloorShine != null)
                    {
                        closestFloorShine.localScale = Vector3.one;
                    }
                }
            }

            transform.SetSiblingIndex(GameLand.Default.FriendlySlot.childCount - 1);
            ToggleExpBars(true);
        }

        /// <summary>更新普攻锁定目标并转向（索敌结果由 SoldierAtkRange 传入）</summary>
        public void SetClosestTarget(MonsterCard target)
        {
            currentTarget = target;
            if (AttackRange != null)
            {
                AttackRange._currentTarget = target;
            }

            if (target == null) return;
            AnimationController.RotateToTarget(target.transform.position);
            // 把世界坐标转换到 UI 坐标系
            // Vector3 targetUIPos = Camera.main.WorldToScreenPoint(transform.position);
            // Vector3 selfUIPos = RectTransformUtility.WorldToScreenPoint(Camera.main, target.transform.position);
            // // 比较屏幕坐标的x，判断左右
            // if (targetUIPos.x < selfUIPos.x)
            // {
            //     // 左边 → rotation.x 为负
            //     PlayerspineObj.transform.rotation = Quaternion.Euler(
            //         PlayerspineObj.transform.rotation.eulerAngles.x,
            //         0,
            //         PlayerspineObj.transform.rotation.eulerAngles.z
            //     );
            //     
            // }
            // else
            // {
            //     // 右边 → rotation.x 为正
            //     PlayerspineObj.transform.rotation = Quaternion.Euler(
            //         PlayerspineObj.transform.rotation.eulerAngles.x,
            //         180,
            //         PlayerspineObj.transform.rotation.eulerAngles.z
            //     );
            // }
        }



        /// <summary>设置卡牌融合目标（英灵合成玩法）</summary>
        public void SetFusionTarget(SoldierCard target)
        {
            CurrentFCard = target;
            // CurrentFCard.Frame.gameObject.SetActive(true);
        }

        private const string AttackRangeHintTweenId = "SoldierAttackRangeHint";

        // 攻击范围显示（与普攻/主动技能索敌半径一致）
        private void ShowAttackRange(bool isShow)
        {
            GameLand.Default.AttackRangeObj.gameObject.SetActive(isShow);
            if (isShow)
            {
                var range = AttackRange != null ? AttackRange.CurrentAttackRange : attackRange;
                GameLand.Default.AttackRangeCircle.transform.position = transform.position;
                GameLand.Default.CircleFrame.rectTransform.sizeDelta = new Vector2(range * 2, range * 2);
                GameLand.Default.AttackRangeCircle.rectTransform.sizeDelta =
                    new Vector2(range * 2, range * 2);
            }
        }

        /// <summary>
        /// 点击手动释放技能时短暂显示攻击范围，提示玩家技能索敌半径。
        /// </summary>
        public void FlashAttackRangeHint(float duration = 1f)
        {
            DOTween.Kill(AttackRangeHintTweenId);
            ShowAttackRange(true);
            DOVirtual.DelayedCall(duration, () => ShowAttackRange(false))
                .SetId(AttackRangeHintTweenId)
                .SetUpdate(true);
        }


        private void ToggleExpBars(bool isVisible)
        {
            var allCards = FindObjectsOfType<SoldierCard>();
            foreach (var card in allCards)
            {
                card.ExpBar.gameObject.SetActive(isVisible);
            }
        }

        #endregion

        /// <summary>驱动状态机、普攻 CD、Idle→Attacking 转换、Buff 与生存倒计时</summary>
        private void Update()
        {
            if (!GamePlayManager.Default.IsPaused())
            {
                _stateMachine?.Update();

                ClearInvalidCurrentTarget();
                if (nextAttackTime <= 0
                    && _currentStateType == SoldierStateType.Idle
                    && currentTarget != null)
                {
                    SwitchState(SoldierStateType.Attacking);
                }
                if(_currentStateType == SoldierStateType.Idle)
                    nextAttackTime -= Time.deltaTime;

                // 如果正在攻击，且超过间隔没输入，就播放收手动画
                // if (isAttacking && Time.time - lastAttackTime > attackInterval)
                // {
                //     isAttacking = false;
                //     AnimationController.PlayIdle();
                // }


                // 卡牌生成时间
                if (survivalTime > 0f)
                {
                    currentSurvivalTime += Time.deltaTime * Global.SpeedMultiplier.Value;
                    if (currentSurvivalTime >= survivalTime)
                    {
                        LockAttackState(true);

                        if (_survivalMoveTween != null)
                        {
                            _survivalMoveTween.Kill();
                            _survivalMoveTween = null;
                        }

                        _survivalMoveTween = transform.DOMove(transform.position + new Vector3(8f, 0, 0), 0.1f)
                            .SetLoops(4, LoopType.Yoyo)
                            .SetEase(Ease.Linear)
                            .SetId(GlobalConst.BattleTween)
                            .SetTarget(gameObject)
                            .OnComplete(() =>
                            {
                                _survivalMoveTween = null;
                                DestroyOneself();
                            });

                        survivalTime = 0f;
                        currentSurvivalTime = 0f;
                    }
                }

                // 更新Buff
                BuffController.Update(Time.deltaTime);

            }
        }


        #region 受击

        // ── 伤害系统（计算与结算分离）────────────────────────────────────────────
        // 调用方（MainCity / GamePlayManager / BurnBuff 等）负责：
        //   1. CanReceiveIncomingDamage  前置校验
        //   2. DamageContextBuilder.CalculateIncoming  构建上下文 + 执行管线
        //   3. TakeDamage(result)  本区域仅消费 DamageResult
        // 结算细节（护盾、扣血、飘字、事件）在 SoldierDamageReceiver 中实现。

        /// <summary>最后一次伤害结算使用的调试记录器。</summary>
        private IDamageCalculationRecorder _lastDamageRecorder;

        /// <summary>是否记录伤害结算步骤（调试面板用）。</summary>
        public bool EnableDamageRecording = true;

        /// <summary>
        /// 是否允许进入受击流程（不含伤害公式，仅状态门禁）。
        /// 调用方应在 CalculateIncoming 之前调用，避免对已死亡/拖拽中/镜像单位做无意义计算。
        /// </summary>
        /// <param name="damageVal">原始伤害，&lt;= 0 时直接拒绝</param>
        public bool CanReceiveIncomingDamage(int damageVal)
        {
            // 新手引导期间低血量保护
            if (GamePlayManager.Default.GuideIsRunning() && _hp.Value <= 100)
            {
                return false;
            }

            if (isDie) return false;
            if (damageVal <= 0 || isDrag) return false;
            if (RoleType == SoldierRoleType.Mirror) return false;
            return true;
        }

        /// <summary>
        /// 消费 <see cref="DamageResult"/>，委托 <see cref="DamageReceiver"/> 完成结算。
        /// 本方法不构建 DamageContext、不执行 DamagePipeline。
        /// </summary>
        /// <param name="result">管线已计算完毕的结算结果</param>
        /// <param name="recorder">可选记录器；为空时按 <see cref="EnableDamageRecording"/> 自动创建</param>
        /// <returns>穿透护盾后实际扣血量；闪避时为 0</returns>
        public int TakeDamage(DamageResult result, IDamageCalculationRecorder recorder = null)
        {
            _lastDamageRecorder = recorder ?? CreateDamageRecorder();
            return DamageReceiver.ApplyDamage(result, new DamageApplyOptions
            {
                Recorder = _lastDamageRecorder
            });
        }

        /// <summary>供 SoldierDamageReceiver 回写记录器引用。</summary>
        internal void SetLastDamageRecorder(IDamageCalculationRecorder recorder)
        {
            _lastDamageRecorder = recorder;
        }

        /// <summary>获取最近一次伤害结算的调试报告。</summary>
        public string GetLastDamageReport()
        {
            return _lastDamageRecorder?.GetRecordReport() ?? "无伤害计算记录";
        }

        /// <summary>使用外部传入的记录器消费伤害结果（测试 / 调试专用）。</summary>
        public int TakeDamageWithRecorder(DamageResult result, IDamageCalculationRecorder recorder)
        {
            return TakeDamage(result, recorder ?? new NullDamageCalculationRecorder());
        }

        private IDamageCalculationRecorder CreateDamageRecorder()
        {
            if (EnableDamageRecording)
            {
                return new MemoryDamageCalculationRecorder();
            }

            return new NullDamageCalculationRecorder();
        }

        /// <summary>
        /// 治疗 / 属性变化飘字（非受击结算路径）。
        /// 怪物攻击造成的受击飘字由 <see cref="SoldierDamageReceiver"/> 处理，避免重复。
        /// </summary>
        private void DamageEffect(int damageVal, string strVal = "")
        {
            var damageObj = GamePlayManager.Default._resLoader.Get<GameObject>("CityDamage").Instantiate();
            damageObj.transform.position = HealthBarController.GetDamageTextOffset();
            damageObj.transform.SetParent(GameLand.Default.DamageParent);

            if (strVal != "")
            {
                damageObj.LocalScale(new Vector3(1f, 1f, 1f));
                damageObj.GetComponent<CityDamageFun>().SetData(strVal);
            }
            else
            {
                damageObj.LocalScale(new Vector3(0.6f, 0.6f, 0.6f));
                damageObj.GetComponent<CityDamageFun>().SetData(damageVal);
            }
        }

        #endregion


        #region 角色状态

        /// <summary>血量归零或外部调用：锁攻击、清订阅、人口-1、隐藏并联动复制体死亡</summary>
        public void Die()
        {
            if (isDie) return;

            isDie = true;
            Game.Battle.BattleScheduler.Instance.CancelByCaster(this);
            LockAttackState(true);
            ClearRuntimeSubscriptions();
            ClearDotweenRuntime();
            EffectManager.Instance.PlayEffect("HeroDeathEffect01", transform.position + new Vector3(0,-0.6f,0));
            // 切换到死亡状态
            SwitchState(SoldierStateType.Dead);

            // 归还人口
            Global.CityPopulation.Value--;
            if (Global.CityPopulation.Value < 0)
            {
                Global.CityPopulation.Value = 0;
            }
            
            BuffController.ClearAll();
            // HealthBarController.Hide();
            // 死亡锁定释放技能
            if (GameLand.Default != null)
                GameLand.Default.LockSkillRelease(cardId);

            // 破碎效果
            var pool = ObjectPoolManager.Instance.GetGameObjectPool("pousui2");
            if (pool != null)
            {
                var pousui = pool.Get();
                pousui.transform.localScale = Vector3.one * 100;
                pousui.transform.position = transform.position;
                StartCoroutine(DisableAfterDelay(pool, pousui, 0.2f));
            }

            transform.gameObject.SetActive(false);
            
            // 判断所属复制体
            if (CopyBodyList.Count > 0)
            {
                // 主体死亡复制体强行死亡
                foreach (var copy in CopyBodyList)
                {
                    copy.Die();
                }
            }
        }

        /// <summary>复活：恢复显示、血量、订阅与 Idle 状态</summary>
        public void ReviveSoldier()
        {
            // 增加人口
            Global.CityPopulation.Value++;
            transform.gameObject.SetActive(true);
            ResetSoldierHp();

            Game.Battle.BattleRuleComputerSupport.OnOwnerRevived(this);

            RegisterRuntimeSubscriptions();
            RefreshInitialHealthBarDisplay();

            // 复活后切换到待机状态
            SwitchState(SoldierStateType.Idle);
        }

        /// <summary>生存时间到期：人口-1 并 Destroy GameObject</summary>
        private void DestroyOneself()
        {
            // 归还人口
            Global.CityPopulation.Value--;
            if (Global.CityPopulation.Value < 0)
            {
                Global.CityPopulation.Value = 0;
            }

            isDie = true;
            ClearRuntimeSubscriptions();
            ClearDotweenRuntime();

            // 销毁自己
            Destroy(gameObject);
            if (effectxuanwo != null)
            {
                Destroy(effectxuanwo);
            }

            GamePlayManager.Default.villageGameCards.Remove(this);

            // 破碎效果
            var pool = ObjectPoolManager.Instance.GetGameObjectPool("pousui2");
            if (pool != null)
            {
                var pousui = pool.Get();
                pousui.transform.localScale = Vector3.one * 100;
                pousui.transform.position = transform.position;
                StartCoroutine(DisableAfterDelay(pool, pousui, 0.5f));
            }
        }

        private void OnDestroy()
        {
            ClearRuntimeSubscriptions();
            ClearDotweenRuntime();
            BuffController?.ClearAll();
            // SkillExecutor?.StopAttack();
            AnimationController?.StopAllAnimations();
        }

        private void BindPauseState()
        {
            if (_pauseChangedHandler != null || GamePlayManager.Default == null) return;

            _pauseChangedHandler = OnGamePauseChanged;
            GamePlayManager.Default.OnPauseChanged += _pauseChangedHandler;
            OnGamePauseChanged(GamePlayManager.Default.IsPaused());
        }

        private void UnbindPauseState()
        {
            if (_pauseChangedHandler == null || GamePlayManager.Default == null) return;

            GamePlayManager.Default.OnPauseChanged -= _pauseChangedHandler;
            _pauseChangedHandler = null;
        }

        private void OnGamePauseChanged(bool isPausedNow)
        {
            if (AnimationController == null) return;

            if (isPausedNow)
            {
                AnimationController.PauseAnimation();
            }
            else
            {
                AnimationController.ResumeAnimation();
            }
        }

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

        #endregion

        // 调用此方法播放攻击动画
        // public void PlayAttack()
        // {
        //     // 强制播放攻击动画（不循环）
        //     PlayerspineObj.GetComponent<SkeletonGraphic>().AnimationState.SetAnimation(0, "atk", false);
        //
        //     // 攻击动画播放完后自动切换回待机动画（循环）
        //     PlayerspineObj.GetComponent<SkeletonGraphic>().AnimationState.AddAnimation(0, "idle", true, 0);
        //
        // }
        
        // private int attackCallCount = 0;
        // private float lastAttackTime = 0f;
        // private float attackInterval = 0.6f;
        // private bool isAttacking = false; // 是否处于攻击状态
        //
        // // 当前攻击波次上下文（Spine "atk" 事件驱动命中时复用）
        // private AttackWaveContext _attackCtx;
        //
        // /// <summary>
        // /// 当前攻击波次的上下文数据，由 StartRangedAttack2 写入，OnSpineAttackHit 读取
        // /// </summary>
        // private struct AttackWaveContext
        // {
        //     public ISkill SkillCard;
        //     public int SalvoCount;
        //     public float SalvoAngleInterval;
        //     public Vector3 Direction;
        //     public Vector3 DirectionPos;
        // }

       /* private void PlayAttackAnnaAtk()
        {
            try
            {
                float currentTime = Time.time;
                var animationState = PlayerspineObj.GetComponent<SkeletonGraphic>().AnimationState;
            
                if (currentTime - lastAttackTime > attackInterval)
                {
                    // 超过间隔，说明是新一轮攻击
                    isAttacking = true;
                    switch (soldierBattle.Type)
                    {
                        case SoldierType.Spearmen:
                            animationState.SetAnimation(0, "atk_all", false);
                            break;
                    
                        case SoldierType.Archer:
                            animationState.SetAnimation(0, "atk_start", false);
                            break;
                    
                        case SoldierType.Magician:
                            animationState.SetAnimation(0, "atk", false);
                            break;
                    }
                }
                else
                {
                    // TODO 法师没有 
                    if (soldierBattle.Type != SoldierType.Magician)
                    {
                        // 连续攻击：刷新 atk_loop
                        animationState.AddAnimation(0, "atk_loop", true,0);
                    }
                }

                lastAttackTime = currentTime;
            }
            catch (Exception e)
            {
                LogTool.LogError(e);
            }
        }*/
        
        // public void PlayIdle()
        // {
        //     var state = PlayerspineObj.GetComponent<SkeletonGraphic>().AnimationState;
        //     state.AddAnimation(0, "idle", true, 0);
        // }
       
        #region 技能

        /// <summary>获取全部已学技能，委托 SkillComponent</summary>
        public Dictionary<int, ISkill> GetSkillAll()
        {
            return SkillComponent.GetSkillAll();
        }
        
        public ISkill GetSkillById(int skillId)
        {
            return SkillComponent.GetSkillById(skillId);
        }
        
        public ISkill GetSkillByType(SpecialEffectType skillType)
        {
            return SkillComponent.GetSkillByType(skillType);
        }
        
        /// <param name="isActiveRelease">true 时跳过冷却检查（动画命中帧 / 主动释放）</param>
        public ISkill GetReadySkillById(int skillId, bool isActiveRelease = false)
        {
            return SkillComponent.GetReadySkillById(skillId, isActiveRelease);
        }

        public ISkill LearnSkill(int skillId, int skillLevel,bool isPassSkill = false)
        {
            return SkillComponent.LearnSkill(skillId, skillLevel, isPassSkill);
        }

        /// <summary>覆盖已有技能等级（如破军/重击等级变化），必须用字典索引写入而非 Add</summary>
        public ISkill RelearnSkill(int skillId, int skillLevel)
        {
            return SkillComponent.RelearnSkill(skillId, skillLevel);
        }

        /// <summary>
        /// 释放技能图技能。
        /// 条件增伤/Buff 走 LearnSkillRule；暴击预掷在 SkillRuntime；命中增伤在 BattleRuleDamageModifier。
        /// </summary>
        public async UniTask UseSkill(
            int skillId,
            Vector3 _direction,
            bool skipBeforeTriggerEvent = false,
            bool skipBuffOnCast = false,
            bool useSnapshotCastTarget = false)
        {
            // 条件增伤/Buff 已迁移至 LearnSkillRule，不再组装 atkCarryings / SkillCarrying。
            // 暴击预掷、条件效果分别由 SkillRuntime 与 BattleRuleDamageModifier 处理。
            var skill = GetSkillById(skillId);
            if (skill == null)
            {
                LogTool.LogWarning($"[SkillRelease-UseSkill] {cardId} 尝试使用未学习的技能: {skillId}");
                return;
            }

            if (nowTargetInfo.isEnemy && !useSnapshotCastTarget)
            {
                if (!TryRefreshSkillCastDirection(skillId, nowTargetInfo, out _direction))
                {
                    _direction = Vector3.zero;
                }
            }

            if (!skipBeforeTriggerEvent && skillId == NormalAttackSkillId)
            {
                BattleRuleCastSupport.TriggerNormalAttackBeforeCast(this, skillId, _direction);
            }

            if (!skipBeforeTriggerEvent && skillId != NormalAttackSkillId)
            {
                BattleRuleCastSupport.TriggerSkillBeforeCast(this, skillId, _direction);
            }

            bool castExecuted = false;
            try
            {
                if (SkillConfigManager.IsLegacyCastOnlySkill(skillId))
                {
                    using (BattleSkillProfilerMarkers.Begin(BattleSkillProfilerMarkers.SkillRelease))
                    {
                        CastLegacySkill(skill, skillId, _direction);
                    }
                    castExecuted = true;
                    return;
                }

                ///如果没存技能信息 走老的逻辑
                var skillGraphConfig = GamePlayManager.Default._skillConfigManager.Get(skillId);
                if (skillGraphConfig == null)
                {
                    LogTool.LogWarning($"[SkillRelease-UseSkill] {cardId} 尝试使用技能 {skillId}: SkillGraphConfig=null (技能图未加载)");
                    using (BattleSkillProfilerMarkers.Begin(BattleSkillProfilerMarkers.SkillRelease))
                    {
                        CastLegacySkill(skill, skillId, _direction);
                    }
                    castExecuted = true;
                    return;
                }
                var data = _tbSpecialEffectsConfig.GetOrDefault(skillId);
                LogTool.Log($"[SkillRelease-UseSkill] {cardId} 释放技能 {skillId}: SkillGraphConfig=存在, ActsOnCard={data?.ActsOnCard}");
                if (data.AwakingSkill == 1)
                {
                    EventBus.Publish(new SoldierAwakeningSkillEvent()
                    {
                        SoldierId = data.ActsOnCard
                    });
                }

                if (!skipBeforeTriggerEvent)
                {
                    if (skill.Id!=NormalAttackSkillId)
                    {
                        EventBus.Publish(new ConditionsTypeEvent
                        {
                            coditionsType = ConditionsType.OnSkillBefore,
                            caster = this,
                            target = null,
                            direction = _direction
                        });
                    }
                    else
                    {
                        EventBus.Publish(new ConditionsTypeEvent
                        {
                            coditionsType = ConditionsType.OnAttackBefore,
                            caster = this,
                            target = null,
                            direction = _direction
                        });
                    }
                }

                await SkillManager.instance.CastSkill(skillId, this, _direction, skipBeforeTriggerEvent, skipBuffOnCast);
                castExecuted = true;
                LogTool.Log($"[SkillRelease-UseSkill] {cardId} 技能 {skillId} 执行完成");

                if (skill.Attributes.GetFinal(AttributeType.IsAddSkillNum) > 0)
                {
                    skill.Attributes.GetAttr(AttributeType.IsAddSkillNum).SetBaseValue(1, true);
                    LogTool.Log($"[SoldierCard] {cardId} 技能 {skillId} IsAddSkillNum 累加至 {skill.Attributes.GetFinal(AttributeType.IsAddSkillNum)}");
                }
            }
            finally
            {
                if (castExecuted)
                {
                    BattleRuleCastDamageSession.OnSkillCastCompleted(this, skillId);

                    // OnSkillUse：技能使用后触发一次（清空 CD 等），不跟每次命中走
                    if (!skipBeforeTriggerEvent && skillId != NormalAttackSkillId)
                    {
                        BattleRuleCastSupport.TriggerSkillAfterUse(this, skillId, _direction);
                    }
                }
            }
        }

        private void ClearInvalidCurrentTarget()
        {
            if (currentTarget != null && !EnemyTargetFinder.IsValidEnemy(currentTarget))
            {
                SetClosestTarget(null);
            }
        }

        /// <summary>
        /// 技能实际打出前刷新落点：锁定目标仍存活则沿用，否则按规则重新索敌。
        /// 无存活敌人时返回 false，避免向死亡快照坐标空打。
        /// </summary>
        private bool TryRefreshSkillCastDirection(int skillId, BattleTargetInfo castTargetInfo, out Vector3 skillTarget)
        {
            using (BattleSkillProfilerMarkers.Begin(BattleSkillProfilerMarkers.TargetSearch))
            {
                if (!castTargetInfo.isEnemy)
                {
                    skillTarget = castTargetInfo.targetPosition;
                    return true;
                }

                if (!TryResolveLiveSkillTarget(skillId, castTargetInfo, out var targetInfo))
                {
                    skillTarget = default;
                    return false;
                }

                nowTargetInfo = targetInfo;
                if (skillId == NormalAttackSkillId)
                {
                    SetClosestTarget(targetInfo.target);
                }

                skillTarget = EnemyTargetFinder.ResolveAttackPosition(nowTargetInfo);
                return true;
            }
        }

        /// <summary>
        /// 主动技能索敌：前方 180° + 15° 滑动密度窗口（有符号角）；
        /// 精英/Boss 仍优先，但优先更靠近扇形中轴。
        /// </summary>
        public bool TrySelectSkillFanTarget(out MonsterCard selected)
        {
            // 与普攻索敌同一起点，避免用 Spine 节点导致有效半径偏小
            var origin = AttackRange != null
                ? AttackRange.transform.position
                : transform.position;

            var moveDirectionY = -1f;
            var faceY = 1f;
            if (GameLand.Default != null)
            {
                var lane = GameLand.Default.GetLane(BattleSide);
                if (lane != null)
                {
                    moveDirectionY = lane.MoveDirectionY;
                    faceY = -lane.MoveDirectionY;
                }
            }

            var centerPoint = origin + new Vector3(0f, faceY, 0f);
            var range = AttackRange != null ? AttackRange.CurrentAttackRange : attackRange;
            var career = soldierBattle != null ? soldierBattle.CarType : CareerType.Archer;
            return EnemyTargetFinder.TrySelectCombatTarget(
                origin,
                range,
                career,
                moveDirectionY,
                CombatTargetFallback.FanCrowd,
                out selected,
                fanForwardCenter: centerPoint);
        }

        /// <summary>
        /// 解析本次施法可用的存活敌人。
        /// 普攻：快照 → currentTarget → 范围内最近；技能：快照 → 朝敌人最密集方向（受攻击距离限制）。
        /// 快照校验含 uniqueId，避免对象池复用后跟到出生点。
        /// </summary>
        private bool TryResolveLiveSkillTarget(int skillId, BattleTargetInfo castTargetInfo, out BattleTargetInfo targetInfo)
        {
            targetInfo = default;
            MonsterCard enemy = null;
            var isNormalAttack = skillId == NormalAttackSkillId;

            if (EnemyTargetFinder.IsLiveSnapshotEnemy(castTargetInfo))
            {
                enemy = castTargetInfo.target;
            }
            else if (isNormalAttack
                     && currentTarget != null
                     && EnemyTargetFinder.IsValidEnemy(currentTarget))
            {
                enemy = currentTarget;
            }
            else if (isNormalAttack)
            {
                var fromPos = AttackRange.transform.position;
                var moveDirectionY = -1f;
                if (GameLand.Default != null)
                {
                    var lane = GameLand.Default.GetLane(BattleSide);
                    if (lane != null)
                    {
                        moveDirectionY = lane.MoveDirectionY;
                    }
                }

                var career = soldierBattle != null ? soldierBattle.CarType : CareerType.Archer;
                if (!EnemyTargetFinder.TrySelectCombatTarget(
                        fromPos,
                        attackRange,
                        career,
                        moveDirectionY,
                        CombatTargetFallback.NearestInRange,
                        out enemy))
                {
                    return false;
                }
            }
            else if (!TrySelectSkillFanTarget(out enemy))
            {
                return false;
            }

            targetInfo = EnemyTargetFinder.FromMonster(enemy);
            return true;
        }

        private void CastLegacySkill(ISkill skill, int skillId, Vector3 direction)
        {
            skill.Cast(direction);

            if (skill.Attributes.GetFinal(AttributeType.IsAddSkillNum) > 0)
            {
                skill.Attributes.GetAttr(AttributeType.IsAddSkillNum).SetBaseValue(1, true);
            }

            LogTool.Log($"[SkillRelease-UseSkill] {cardId} 使用老技能逻辑: {skillId}");
        }
        
        
        /**
         * 使用技能
         */
        // public void UseSkill(int skillId, object obj)
        // {
        //     var skill = GetSkillById(skillId);
        //     if (skill == null)
        //     {
        //         LogTool.LogWarning($"{cardId} 尝试使用未学习的技能: {skillId}");
        //         return;
        //     }
        //
        //     // CD 检查在 SkillBase 里做
        //     skill.Cast(obj);
        //     
        //     //判断是不是觉醒技能
        //     var data = _tbSpecialEffectsConfig.GetOrDefault(skillId);
        //     if (data.AwakingSkill == 1)
        //     {
        //         EventBus.Publish(new SoldierAwakeningSkillEvent()
        //         {
        //             SoldierId = data.ActsOnCard
        //         });
        //     }
        // }

        /// <param name="skillId">大于 0 时仅作用于该技能；否则作用于全部已学技能</param>
        public void AddSkillAttributeModifier(AttributeType type, AttributeModifier mod, float skillId, bool isAdditive = false)
        {
            // 添加单个技能属性
            if (skillId > 0)
            {
                var skill = GetSkillById((int)skillId);
                if (skill == null) return;

                skill.Attributes.GetAttr(type).AddModifier(mod, isAdditive);
                return;
            }

            // 全部技能生效
            foreach (var skill in SkillComponent.GetSkillAll().Values)
            {
                skill.Attributes.GetAttr(type).AddModifier(mod, isAdditive);
            }
        }
        
        #endregion

        /// <summary>
        /// 获取受上限限制的暴击率
        /// </summary>
        /// <returns></returns>
        public float GetClampedCriticalRate()
        {
            if (BaseAttributes.GetFinal(AttributeType.CriticalNoTakeEffect) > 0f)
            {
                return 0f;
            }

            //基础暴击率
            var baseRate = BaseAttributes.GetFinal(AttributeType.SoldierCriticalRate);
            var maxRate = BaseAttributes.GetFinal(AttributeType.SoldierMaxCriticalRate);
            if (maxRate > 0f && baseRate > maxRate)
                return maxRate;
            return baseRate;
        }
        
        
        private bool _isMaxHealthAddAttr;
        private void AddListenerFullHealth(bool isFull)
        {
            if (isFull && !_isMaxHealthAddAttr) // 满血
            {
                _isMaxHealthAddAttr = true;
                
                // 条件 - 英雄满血
                ConditionManager.Instance.OnTriggerProgress(ConditionType.HeroFullHealth, 1, "", cardId);
            }
            else if (!isFull && _isMaxHealthAddAttr) 
            {
                _isMaxHealthAddAttr = false;
                
                // 条件 - 英雄满血(不满足)
                ConditionManager.Instance.OnTriggerProgress(ConditionType.HeroFullHealth, 0, "", cardId);
            }
        }

        private bool _isHealthBelowPerAddAttr;
        private void HealthBelowPerExtraAttribute(float residueHpPer)
        {
            // 英灵生命值低于百分之多少额外加成
            var lowHpExtraAttr = BaseAttributes.GetFinal(AttributeType.SoldierHealthBelowPerExtraAttribute, 
                (per) => residueHpPer <= per);
            if (!_isHealthBelowPerAddAttr && lowHpExtraAttr > 0f) // 生效 
            {
                // 加成
                _isHealthBelowPerAddAttr = true;
                var attrCfg = DataConfigManager.Tables.TbAttributeConfig.Get(AttributeType.SoldierHealthBelowPerExtraAttribute);
                AttributeManager.SetAttributesLogic(new AttributeModifier
                {
                    SourceKey = "SoldierHealthBelowPerExtraAttribute",
                    SourceType = AttributeSourceType.GeneralSkill,
                }, attrCfg.AdditionAttributes, false, (int)lowHpExtraAttr);
            }
            if (_isHealthBelowPerAddAttr && lowHpExtraAttr == 0f) // 未生效
            {
                // 去除加成
                _isHealthBelowPerAddAttr = false;
                var attrCfg = DataConfigManager.Tables.TbAttributeConfig.Get(AttributeType.SoldierHealthBelowPerExtraAttribute);
                AttributeManager.SetAttributesLogic(new AttributeModifier
                {
                    SourceKey = "SoldierHealthBelowPerExtraAttribute",
                    SourceType = AttributeSourceType.GeneralSkill,
                }, attrCfg.AdditionAttributes, false, 0);
            }
        }

        private bool _isTenaciousWillAddAttr;
        private GameObject _tenaciousWillAddAttrObject;
        private GenericObjectPool _tenaciousWillAddAttrPool;
        private void TenaciousWillExtraAttribute(float residueHpPer)
        {
            var extraAttr = BaseAttributes.GetFinal(AttributeType.SoldierTenaciousWill, 
                (per) => residueHpPer <= per);
            if (!_isTenaciousWillAddAttr && extraAttr > 0f) // 生效 
            {
                var pos = transform.position;
                (_tenaciousWillAddAttrPool, _tenaciousWillAddAttrObject) = ObjectPoolManager.Instance.PlayGameObjectPool(new GameObjectPoolData()
                {
                    PoolName = "BondLifeEffect01",
                    ParentTran = this.transform,
                    Position = new Vector3(pos.x, pos.y - 1f, pos.z),
                    LocalScale = 150,
                    DelayReturn = 0f
                });
                
                // 加成
                _isTenaciousWillAddAttr = true;
                var attrCfg = DataConfigManager.Tables.TbAttributeConfig.Get(AttributeType.SoldierTenaciousWill);
                AttributeManager.SetAttributesLogic(new AttributeModifier
                {
                    SourceKey = "SoldierTenaciousWill",
                    SourceType = AttributeSourceType.SchoolCard,
                }, attrCfg.AdditionAttributes, false, (int)extraAttr, cardId);
            }
            if (_isTenaciousWillAddAttr && extraAttr == 0f) // 未生效
            {
                _tenaciousWillAddAttrPool?.Return(_tenaciousWillAddAttrObject);
                
                // 去除加成
                _isTenaciousWillAddAttr = false;
                var attrCfg = DataConfigManager.Tables.TbAttributeConfig.Get(AttributeType.SoldierTenaciousWill);
                AttributeManager.SetAttributesLogic(new AttributeModifier
                {
                    SourceKey = "SoldierTenaciousWill",
                    SourceType = AttributeSourceType.SchoolCard,
                }, attrCfg.AdditionAttributes, false, 0, cardId);
            }
        }

        /// <summary>普攻后检测模块属性，触发破军一击或重击附加技能</summary>
        private void CheckNormalAttackHeavyBlowSkill(Vector3 direction)
        {
            Vector3 finalDirection = Quaternion.Euler(0, 0, 0) * direction;
            
            // 是否激活破军一击
            var brokenArmySlashLevel = (int)GamePlayManager.Default.ModuleAttributes.GetFinal(AttributeType.SoldierNormalAttackBrokenArmySlashSkill);
            if (brokenArmySlashLevel > 0)
            {
                // 释放技能
                var brokenArmySlashSkill = GetSkillByType(SpecialEffectType.BrokenArmySlash);
                if (brokenArmySlashSkill.Level != brokenArmySlashLevel)
                {
                    brokenArmySlashSkill = RelearnSkill(brokenArmySlashSkill.Id, brokenArmySlashLevel);
                }
                brokenArmySlashSkill.Cast(finalDirection);
                return;
            }

            // 是否激活重击
            var heavyBlowLevel = (int)GamePlayManager.Default.ModuleAttributes.GetFinal(AttributeType.SoldierNormalAttackHeavyBlowSkill);
            if (heavyBlowLevel <= 0) return;
           
            // 释放技能
            var heavyBlowSkill = GetSkillByType(SpecialEffectType.HeavyBlow);
            if (heavyBlowSkill.Level != heavyBlowLevel)
            {
                heavyBlowSkill = RelearnSkill(heavyBlowSkill.Id, heavyBlowLevel);
            }

            heavyBlowSkill.Cast(finalDirection);
        }

        public void AddBuff(IBuff buff, object caster = null)
        {
            if (caster != null) buff.Caster = caster;
            BuffController.AddBuff(buff);
        }


        public void AddMoonlightShield(float addVal)
        {
            BuffController.AddMoonlightShield(addVal);
        }
        
        #region 伤害结算访问器

        // 供 ShieldResolver / SoldierDamageReceiver 读写，避免直接访问私有字段。

        /// <summary>普通护盾剩余值。</summary>
        public int Shield
        {
            get => shield;
            set => shield = value;
        }

        /// <summary>是否已死亡。</summary>
        public bool IsSoldierDie => isDie;

        /// <summary>是否正在被拖拽（拖拽中视为无敌）。</summary>
        public bool IsDragging => isDrag;

        /// <summary>角色类型（镜像不受击等规则依赖此字段）。</summary>
        public SoldierRoleType SoldierRole => RoleType;

        #endregion

    }
}