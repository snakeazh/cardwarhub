
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Localization;
using Config;
using Cysharp.Threading.Tasks;
using FusionContinent;
using FusionContinent.Model;
using Game.Team;
using UIFramework;
using UIFramework.Services;
using UnityEngine;
using ValueType = Config.ValueType;
using PlayerCard = FusionContinent.Model.PlayerCard;

namespace Game.Skill
{
    /// <summary>
    /// 技能详情面板ViewModel
    /// 负责技能详情界面的数据管理和业务逻辑
    /// 产品改动：技能升级已移除，技能等级跟随英灵等级同步提升，本面板只读展示 + 穿戴/卸下
    /// </summary>
    public class SkillUpGradePanelViewModel : ViewModel
    {
        #region 可绑定属性

        /// <summary>
        /// 技能名称
        /// </summary>
        public BindableProperty<string> SkillName { get; } = new BindableProperty<string>("");

        /// <summary>
        /// 卡牌等级文本
        /// </summary>
        public BindableProperty<string> CardLevelText { get; } = new BindableProperty<string>("");

        /// <summary>
        /// 技能描述
        /// </summary>
        public BindableProperty<string> SkillDesc { get; } = new BindableProperty<string>("");

        /// <summary>
        /// 基础攻击力值
        /// </summary>
        public BindableProperty<string> BaseAttackValue { get; } = new BindableProperty<string>("");

        /// <summary>
        /// 基础攻击力差值
        /// </summary>
        public BindableProperty<string> BaseAttackDiff { get; } = new BindableProperty<string>("");

        /// <summary>
        /// 是否显示攻击力差值
        /// </summary>
        public BindableProperty<bool> ShowBaseAttackDiff { get; } = new BindableProperty<bool>(false);

        /// <summary>
        /// 伤害类型名称
        /// </summary>
        public BindableProperty<string> DamageTypeName { get; } = new BindableProperty<string>("");

        /// <summary>
        /// 继承伤害值
        /// </summary>
        public BindableProperty<string> InheritDamageValue { get; } = new BindableProperty<string>("");

        /// <summary>
        /// 继承伤害颜色（根据伤害类型变化）
        /// </summary>
        public BindableProperty<Color> InheritDamageColor { get; } = new BindableProperty<Color>(Color.white);

        /// <summary>
        /// 继承伤害材质
        /// </summary>
        public BindableProperty<Material> InheritDamageMaterial { get; } = new BindableProperty<Material>(null);

        /// <summary>
        /// 继承伤害差值
        /// </summary>
        public BindableProperty<string> InheritDamageDiff { get; } = new BindableProperty<string>("");

        /// <summary>
        /// 是否显示继承伤害差值
        /// </summary>
        public BindableProperty<bool> ShowInheritDamageDiff { get; } = new BindableProperty<bool>(false);

        /// <summary>
        /// 冷却时间值
        /// </summary>
        public BindableProperty<string> CooldownValue { get; } = new BindableProperty<string>("");

        /// <summary>
        /// 冷却时间差值
        /// </summary>
        public BindableProperty<string> CooldownDiff { get; } = new BindableProperty<string>("");

        /// <summary>
        /// 是否显示冷却时间差值
        /// </summary>
        public BindableProperty<bool> ShowCooldownDiff { get; } = new BindableProperty<bool>(false);

        /// <summary>
        /// 升品描述列表（4个等级）
        /// </summary>
        public BindableProperty<List<LevelDescData>> LevelDescs { get; } = new BindableProperty<List<LevelDescData>>(new List<LevelDescData>());

        /// <summary>
        /// 卡牌槽位数据
        /// </summary>
        public BindableProperty<CardSlotData> CardSlotData { get; } = new BindableProperty<CardSlotData>(null);

        /// <summary>
        /// 当前技能是否已穿戴在所属英灵上
        /// </summary>
        public BindableProperty<bool> IsSkillWear { get; } = new BindableProperty<bool>(false);

        #endregion

        #region 命令

        /// <summary>
        /// 关闭命令
        /// </summary>
        public Command OnCloseCommand { get; private set; }

        /// <summary>
        /// 穿戴技能命令
        /// </summary>
        public Command OnWearSkillCommand { get; private set; }

        /// <summary>
        /// 卸下技能命令
        /// </summary>
        public Command OnUnWearSkillCommand { get; private set; }

        #endregion

        #region 内部数据

        /// <summary>
        /// 当前技能ID
        /// </summary>
        private int mSkillId;

        /// <summary>
        /// 当前技能卡牌数据
        /// </summary>
        private PlayerCard mPlayerSkill;

        /// <summary>
        /// 开发配置
        /// </summary>
        private CardDevelopConfig mDevCfg;

        /// <summary>
        /// 技能特效配置
        /// </summary>
        private SpecialEffectsConfig mSkillCfg;

        /// <summary>
        /// 配置表引用
        /// </summary>
        private TbCardBaseConfig mCardBaseConfig;
        private TbCardDevelopConfig mCardDevelopConfig;
        private TbSpecialEffectsConfig mSpecialEffectsConfig;

        private TeamService mTeamService;

        #endregion

        #region 生命周期

        public override void OnCreate()
        {
            base.OnCreate();

            // 获取配置表
            mCardBaseConfig = DataConfigManager.Tables.TbCardBaseConfig;
            mCardDevelopConfig = DataConfigManager.Tables.TbCardDevelopConfig;
            mSpecialEffectsConfig = DataConfigManager.Tables.TbSpecialEffectsConfig;

            // 获取Service
            mTeamService = GameServiceManager.Get<TeamService>();
            Global.Publisher.Subscribe(MessagePublisher.TEAM_HERO_SKILL_WEAR_CHANGED, OnTeamHeroSkillWearChanged);

            // 初始化命令
            OnCloseCommand = new Command(OnClose);
            OnWearSkillCommand = new Command(OnWearSkill);
            OnUnWearSkillCommand = new Command(OnUnWearSkill);
        }

        public override void OnDestroy()
        {
            Global.Publisher.Unsubscribe(MessagePublisher.TEAM_HERO_SKILL_WEAR_CHANGED, OnTeamHeroSkillWearChanged);

            base.OnDestroy();
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 设置技能数据
        /// </summary>
        /// <param name="skillId">技能ID</param>
        public void SetData(int skillId)
        {
            mSkillId = skillId;

            // 获取配置
            var cfg = mCardBaseConfig.GetOrDefault(skillId);
            mDevCfg = mCardDevelopConfig.GetOrDefault(skillId);
            mSkillCfg = mSpecialEffectsConfig.GetOrDefault(skillId);

            if (mDevCfg == null || mSkillCfg == null)
            {
                LogTool.LogError($"[SkillUpGradePanelViewModel] 配置为空 skillId={skillId}");
                return;
            }

            // 获取或创建玩家技能数据
            if (!Global.PlayerCards.TryGetValue(skillId, out mPlayerSkill))
            {
                mPlayerSkill = new PlayerCard()
                {
                    Lv = 1,
                    IsHave = false,
                    Config = cfg,
                    CardId = cfg.Id,
                    ObjData = new CardDataStruct(),
                };
            }

            // 刷新界面
            RefreshDetailPanel();
        }

        /// <summary>
        /// 刷新界面（当数据已在ViewModel中准备好时调用）
        /// </summary>
        public void RefreshUI()
        {
            if (mDevCfg == null || mSkillCfg == null || mPlayerSkill == null)
            {
                // 如果数据未初始化，尝试重新从mSkillId加载
                if (mSkillId > 0)
                {
                    SetData(mSkillId);
                    return;
                }

                LogTool.LogWarning("[SkillUpGradePanelViewModel] RefreshUI 时 skillId 为 0");
                return;
            }

            RefreshDetailPanel();
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 刷新详情面板
        /// </summary>
        private void RefreshDetailPanel()
        {
            if (mDevCfg == null || mSkillCfg == null || mPlayerSkill == null)
            {
                return;
            }

            // 获取等级配置
            var devLevelCfg = mDevCfg.LevelPropertyList[mPlayerSkill.Lv - 1];
            var skillLevelCfg = mSkillCfg.LevelPropertyList[mPlayerSkill.Lv - 1];

            // 设置技能名称
            SkillName.Value = mPlayerSkill.Config?.Name ?? "";

            // 设置卡牌等级
            CardLevelText.Value = $"{devLevelCfg.Level}级";

            // 设置技能描述
            SkillDesc.Value = GetLevelAttrText(skillLevelCfg.Desc, skillLevelCfg.Attributes);

            // 下一级预览（技能等级跟随英灵等级提升，差值仅作展示）
            bool isHaveNext = mPlayerSkill.Lv < mSkillCfg.LevelPropertyList.Count;

            // 刷新技能属性
            RefreshSkillAttributes(skillLevelCfg, isHaveNext ? mSkillCfg.LevelPropertyList[mPlayerSkill.Lv] : null);

            // 刷新升品描述
            RefreshLevelDetailDesc();

            // 刷新卡牌槽位
            RefreshCardSlot();

            // 刷新技能穿戴状态
            RefreshSkillWearState();
        }

        /// <summary>
        /// 刷新技能属性显示
        /// </summary>
        private void RefreshSkillAttributes(SpecialLevelPropertyList skillLevelCfg, SpecialLevelPropertyList skillNextLevelCfg)
        {
            var damageType = mSkillCfg.DamageType;
            var damageParams = mSkillCfg.DamageParam;

            // 0: 基础攻击力
            var baseAttackAttr = skillLevelCfg.Attributes.Find(a => a.Type == AttributeType.SkillBasicAttackPower);
            var nextBaseAttackAttr = skillNextLevelCfg?.Attributes.Find(a => a.Type == AttributeType.SkillBasicAttackPower);
            float currentBaseAttack = baseAttackAttr?.Value ?? 0;
            float nextBaseAttack = nextBaseAttackAttr?.Value ?? currentBaseAttack;

            BaseAttackValue.Value = currentBaseAttack.ToString("F0");
            float baseAttackDiff = nextBaseAttack - currentBaseAttack;
            bool showBaseAttackDiff = skillNextLevelCfg != null && Mathf.Abs(baseAttackDiff) > 0.001f;
            ShowBaseAttackDiff.Value = showBaseAttackDiff;
            BaseAttackDiff.Value = baseAttackDiff > 0 ? $"+{baseAttackDiff:F0}" : $"{baseAttackDiff:F0}";

            // 1: 伤害类型
            if (damageType != null && damageType.Count > 0)
            {
                var dmgType = damageType[0];
                DamageTypeName.Value = GetDamageTypeName(dmgType);
            }

            // 2: 继承伤害
            float inheritDamage = 0;
            if (damageParams != null && damageParams.Count > 0)
            {
                var inheritParam = damageParams.FirstOrDefault(p => p.ValueType == ValueType.Percentage);
                if (inheritParam != null)
                {
                    inheritDamage = inheritParam.EffectVal * 100;
                }
            }
            InheritDamageValue.Value = $"{inheritDamage:F0}%";

            // 继承伤害差值（DamageParam不随等级变化，所以差值为0，不显示）
            ShowInheritDamageDiff.Value = false;
            InheritDamageDiff.Value = "";

            // 设置继承伤害的颜色和材质
            if (damageType != null && damageType.Count > 0)
            {
                var dmgType = damageType[0];
                if (dmgType == TakeDamageType.PhysicalDamage)
                {
                    InheritDamageColor.Value = new Color(0.533f, 1f, 0.49f); // #88FF7D
                    InheritDamageMaterial.Value = GlobalFunc.LoadMaterial("ChironGoRoundTC-700BSDFStaticGreen");
                }
                else if (dmgType == TakeDamageType.MagicDamage)
                {
                    InheritDamageColor.Value = new Color(0.616f, 0.851f, 1f); // #9DD9FF
                    InheritDamageMaterial.Value = GlobalFunc.LoadMaterial("ChironGoRoundTC-700BSDFStaticIndigo");
                }
                else
                {
                    InheritDamageColor.Value = Color.white;
                    InheritDamageMaterial.Value = null;
                }
            }

            // 3: 冷却时间
            var cdAttr = skillLevelCfg.Attributes.Find(a => a.Type == AttributeType.SkillCdTime);
            var nextCdAttr = skillNextLevelCfg?.Attributes.Find(a => a.Type == AttributeType.SkillCdTime);
            float currentCd = cdAttr?.Value ?? 0;
            float nextCd = nextCdAttr?.Value ?? currentCd;

            CooldownValue.Value = currentCd.ToString("F1") + "s";
            float cdDiff = nextCd - currentCd;
            bool showCdDiff = skillNextLevelCfg != null && Mathf.Abs(cdDiff) > 0.001f;
            ShowCooldownDiff.Value = showCdDiff;
            CooldownDiff.Value = cdDiff > 0 ? $"+{cdDiff:F1}" : $"{cdDiff:F1}";
        }

        /// <summary>
        /// 刷新升品描述
        /// </summary>
        private void RefreshLevelDetailDesc()
        {
            var levelDescs = new List<LevelDescData>();

            foreach (var levelCfg in mDevCfg.LevelPropertyList)
            {
                if (levelCfg.UpQualityDesc != null && levelCfg.UpQualityDesc.Count > 0)
                {
                    foreach (var desc in levelCfg.UpQualityDesc.Where(d => !string.IsNullOrEmpty(d)))
                    {
                        levelDescs.Add(new LevelDescData
                        {
                            Level = levelCfg.Level,
                            Desc = desc,
                            IsLocked = mPlayerSkill.Lv < levelCfg.Level
                        });
                    }
                }
            }

            LevelDescs.Value = levelDescs;
        }

        /// <summary>
        /// 刷新卡牌槽位
        /// </summary>
        private void RefreshCardSlot()
        {
            if (mPlayerSkill == null) return;

            var skillCfg = mSpecialEffectsConfig.GetOrDefault(mPlayerSkill.CardId);
            var cardData = new CardSlotData()
            {
                CardId = mPlayerSkill.CardId,
                NameText = mPlayerSkill.Config.Name,
                LevelText = $"{mPlayerSkill.Lv}",
                Desc = mPlayerSkill.Config.Desc,
                State = mPlayerSkill.Config.State,
                IsUnlock = mPlayerSkill.IsHave,
                QualityType = skillCfg?.LevelPropertyList[mPlayerSkill.Lv - 1].Quality ?? 0,
            };

            switch (mPlayerSkill.Config.Type)
            {
                case CardType.GeneralSkill:
                    cardData.IconName = mPlayerSkill.Config.Icon;
                    cardData.IconScale = Vector3.one * 1.1f;
                    break;
                default:
                    cardData.Icon = Global.CardIconSpriteAtlas.GetSpriteExt(mPlayerSkill.Config.Icon);
                    break;
            }

            CardSlotData.Value = cardData;
        }

        /// <summary>
        /// 刷新穿戴/卸下按钮状态
        /// </summary>
        private void RefreshSkillWearState()
        {
            if (mTeamService == null || mSkillCfg == null)
            {
                LogTool.LogError($"[SkillUpGradePanelViewModel] 无法刷新技能穿戴状态 skillId={mSkillId}");
                return;
            }

            IsSkillWear.Value = mTeamService.CheckSkillWear(mSkillCfg.ActsOnCard, mSkillId);
        }

        /// <summary>
        /// 获取属性文本
        /// </summary>
        private string GetLevelAttrText(string descText, List<AttributeStruct> attributes)
        {
            for (int i = 1; i <= attributes.Count; i++)
            {
                var attr = attributes[i - 1];
                var val = attr.ValueType == ValueType.Percentage ? attr.Value * 100 : attr.Value;
                if (attr.Type is AttributeType.SoldierCriticalRate or AttributeType.SoldierCriticalDamage)
                {
                    val = attr.Value * 100;
                }
                descText = descText.Replace("{Attribute" + i + "}", $"{val}");
            }

            return descText;
        }

        /// <summary>
        /// 获取伤害类型名称
        /// </summary>
        private string GetDamageTypeName(TakeDamageType type)
        {
            switch (type)
            {
                case TakeDamageType.PhysicalDamage:
                    return LocalizationManager.Instance.GetText(LocalizationModule.UITexts, "UI_DAMAGE_TYPE_PHYSICAL");
                case TakeDamageType.MagicDamage:
                    return LocalizationManager.Instance.GetText(LocalizationModule.UITexts, "UI_DAMAGE_TYPE_MAGIC");
                case TakeDamageType.MagicDamageThunder:
                    return LocalizationManager.Instance.GetText(LocalizationModule.UITexts, "UI_DAMAGE_TYPE_THUNDER");
                case TakeDamageType.RealDamage:
                    return LocalizationManager.Instance.GetText(LocalizationModule.UITexts, "UI_DAMAGE_TYPE_REAL");
                case TakeDamageType.SchoolDamage:
                    return LocalizationManager.Instance.GetText(LocalizationModule.UITexts, "UI_DAMAGE_TYPE_SCHOOL");
                default:
                    return LocalizationManager.Instance.GetText(LocalizationModule.UITexts, "UI_DAMAGE_TYPE_GENERAL");
            }
        }

        #endregion

        #region 命令执行

        /// <summary>
        /// 关闭界面
        /// </summary>
        private void OnClose()
        {
            // 关闭命令，实际关闭操作在View中处理
        }

        /// <summary>
        /// 穿戴当前技能
        /// </summary>
        private async void OnWearSkill()
        {
            await SetSkillWearStateAsync(true);
        }

        /// <summary>
        /// 卸下当前技能
        /// </summary>
        private async void OnUnWearSkill()
        {
            await SetSkillWearStateAsync(false);
        }

        private async UniTask SetSkillWearStateAsync(bool isWear)
        {
            if (mTeamService == null || mSkillCfg == null)
            {
                LogTool.LogError($"[SkillUpGradePanelViewModel] 无法保存技能穿戴状态 skillId={mSkillId}");
                return;
            }

            var soldierId = mSkillCfg.ActsOnCard;
            if (soldierId <= 0)
            {
                LogTool.LogError($"[SkillUpGradePanelViewModel] 技能缺少所属英灵配置 skillId={mSkillId}");
                return;
            }

            var success = await mTeamService.SetSkillWearStateAsync(soldierId, mSkillId, isWear);
            if (!success)
            {
                LogTool.LogError($"[SkillUpGradePanelViewModel] 保存技能穿戴状态失败 skillId={mSkillId}");
                return;
            }

            IsSkillWear.Value = isWear;
            RefreshSkillWearState();
        }

        #endregion

        #region 事件回调

        /// <summary>
        /// 解锁引导回调
        /// </summary>
        public System.Action OnAnimationOpen;

        #endregion

        private void OnTeamHeroSkillWearChanged(object data)
        {
            RefreshSkillWearState();
        }

        #region 辅助数据类

        /// <summary>
        /// 等级描述数据
        /// </summary>
        public class LevelDescData
        {
            public int Level;
            public string Desc;
            public bool IsLocked;
        }

        #endregion
    }
}
