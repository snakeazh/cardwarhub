using System;
using System.Collections.Generic;
using System.Linq;
using Config;
using FusionContinent;
using FusionContinent.Attribute;
using FusionContinent.Buff;
using FusionContinent.Model;
using FusionContinent.Utils;
using Game.Battle;
using Game.Services;
using UIFramework.Services;
using UnityEngine;
using Random = System.Random;
using ValueType = Config.ValueType;

namespace Manager
{
    public class AttributeManager
    {
        /// <summary>
        /// 本局战斗已学习的规则（允许同 RuleId 多次叠加）。
        /// </summary>
        private static readonly List<BattleLearnedRuleEntry> mBattleLearnedSkillRules = new List<BattleLearnedRuleEntry>();

        /// <summary>
        /// 本局战斗已学习规则（只读快照，含叠加实例）。
        /// </summary>
        public static IReadOnlyList<BattleLearnedRuleEntry> BattleLearnedSkillRules => mBattleLearnedSkillRules;

        /// <summary>
        /// 本局已学 RuleId 序列（含重复，供仅关心 Id 的遍历）。
        /// </summary>
        public static IEnumerable<int> BattleLearnedSkillRuleIds
        {
            get
            {
                for (int i = 0; i < mBattleLearnedSkillRules.Count; i++)
                {
                    yield return mBattleLearnedSkillRules[i].RuleId;
                }
            }
        }

        /// <summary>
        /// 添加本局规则（同 RuleId 可多次学习并叠加）。
        /// </summary>
        public static bool AddBattleSkillRule(
            int ruleId,
            string stackSourceKey = null,
            AttributeSourceType sourceType = AttributeSourceType.SchoolCard,
            int scopeHeroCardId = 0)
        {
            if (ruleId <= 0)
            {
                return false;
            }

            var entry = new BattleLearnedRuleEntry(ruleId, stackSourceKey, sourceType, scopeHeroCardId);
            mBattleLearnedSkillRules.Add(entry);
            LogTool.Log(
                $"[AttributeManager] 本局新增规则: RuleId={ruleId}, StackKey={entry.StackSourceKey}, Source={sourceType}, ScopeHero={scopeHeroCardId}");
            SyncBattleSkillRulesToService();
            TryTriggerNoConditionRuleOnAcquire(entry);
            TryApplyAttachedDamageRuleOnAcquire(ruleId);
            TrySyncHpChangeRuleOnAcquire(entry);

            return true;
        }

        /// <summary>
        /// 清空本局规则缓存（新开局时调用）。
        /// </summary>
        public static void ClearBattleSkillRules()
        {
            mBattleLearnedSkillRules.Clear();
            BattleRuleBattleEconomySupport.ResetBattleState();
            var ruleService = BattleServiceManager.Instance != null
                ? BattleServiceManager.Instance.GetService<BattleRuleService>()
                : null;
            if (ruleService != null)
            {
                ruleService.ClearComputerCounters();
            }

            SyncBattleSkillRulesToService();
        }

        /// <summary>
        /// 将本局规则同步到 BattleRuleService（战斗场景内有效）。
        /// </summary>
        private static void SyncBattleSkillRulesToService()
        {
            var ruleService = BattleServiceManager.Instance?.GetService<BattleRuleService>();
            if (ruleService == null)
            {
                return;
            }

            var ruleIds = new List<int>(mBattleLearnedSkillRules.Count);
            for (int i = 0; i < mBattleLearnedSkillRules.Count; i++)
            {
                ruleIds.Add(mBattleLearnedSkillRules[i].RuleId);
            }

            ruleService.RegisterOwnerRules(
                BattleRuleDamageSupport.GlobalBattleOwnerId,
                ruleIds,
                BattleRuleDamageValueProvider.Instance,
                BattleRuleDamageEffectExecutor.Instance,
                resetRuntimeState: false);
            ruleService.RefreshComputerGroupIndex();
        }

        /// <summary>
        /// 获得规则时：若为直接触发（NoCondition），条件通过则立即生效。
        /// 装备来源规则延后到技能挂载后由 <see cref="BattleRuleService.ApplyEquipLearnedRulesToSoldier"/> 统一施加，避免与补同步重复叠加。
        /// </summary>
        private static void TryTriggerNoConditionRuleOnAcquire(BattleLearnedRuleEntry entry)
        {
            if (entry.SourceType == AttributeSourceType.Equip
                || entry.SourceType == AttributeSourceType.FusionGem
                || IsDeferredSpeakerWeaponRule(entry))
            {
                return;
            }

            var ruleService = BattleServiceManager.Instance?.GetService<BattleRuleService>();
            ruleService?.TryTriggerNoConditionOnAcquire(entry);
        }

        private static bool IsDeferredSpeakerWeaponRule(BattleLearnedRuleEntry entry)
        {
            return IsDeferredSpeakerModuleRule(entry);
        }

        private static bool IsDeferredSpeakerModuleRule(BattleLearnedRuleEntry entry)
        {
            if (entry.SourceType != AttributeSourceType.SpeakerSkin
                || string.IsNullOrEmpty(entry.StackSourceKey))
            {
                return false;
            }

            return entry.StackSourceKey.StartsWith("SpeakerWeapon_")
                || entry.StackSourceKey.StartsWith("SpeakerSkin_");
        }

        /// <summary>
        /// 附加物理伤害规则：Trigger 为命中时机时，补持久写入 AddPhysicalDamage。
        /// AddMagicDamage 由 OnAttackBefore 在普攻物理命中前处理。
        /// </summary>
        private static void TryApplyAttachedDamageRuleOnAcquire(int ruleId)
        {
            BattleRuleAttachedDamageSupport.TryApplyAttachedPhysicalDamageRule(ruleId);
        }

        /// <summary>
        /// 获得 HpChange 规则时：对场上存活英灵按当前血量立即判条件（满血加 Buff 等）。
        /// </summary>
        private static void TrySyncHpChangeRuleOnAcquire(BattleLearnedRuleEntry entry)
        {
            var ruleService = BattleServiceManager.Instance?.GetService<BattleRuleService>();
            ruleService?.TryApplyHpChangeRuleToAllSoldiers(
                entry.RuleId,
                entry.StackSourceKey,
                entry.SourceType);
        }

        public static void ApplyLockAtkInterval()
        {
            GamePlayManager.Default.UseLockAttackInterval = true;
        }
        
        public static void ApplyLockAtkSpeed()
        {
            GamePlayManager.Default.UseLockAttackSpeed = true;
        }

        /**
         * 设置属性
         */
        public static void SetAttributesLogic(AttributeModifier attrModifier, List<AttributeStruct> attrs, bool isAdditive = false, int redouble = 1, int targetId = 0)
        {
            if (attrModifier == null || attrs.Count == 0) return;
            
            // 属性生效
            foreach (var attr in attrs)
            {
                SetAttributeLogic(attrModifier, new AttributeData(attr), isAdditive, redouble, targetId);
            }
        }
        public static void SetAttributesLogic(AttributeModifier attrModifier, List<AttributeData> attrs, bool isAdditive = false, int redouble = 1, int targetId = 0)
        {
            if (attrModifier == null || attrs.Count == 0) return;
            
            // 属性生效
            foreach (var attr in attrs)
            {
                SetAttributeLogic(attrModifier, attr, isAdditive, redouble, targetId);
            }
        }
        
        public static void SetAttributeLogic(AttributeModifier attrModifier, AttributeData attr, bool isAdditive = false, int redouble = 1, int targetId = 0)
        {
            if (attrModifier == null || attr == null) return;
            
            // 属性加成
            attrModifier.FlatAdd = attr.ValueType == ValueType.Number ? attr.Value * redouble : 0;
            attrModifier.PercentAdd = attr.ValueType == ValueType.Percentage ? attr.Value * redouble : 0;
            
            // 设置概率生效
            attrModifier.ProbabilityTrigger = attr.ProbabilityTrigger;
            
            // 设置生效参数
            attrModifier.EffectParam = attr.EffectParam;

            // 类别
            var attrCfg = DataConfigManager.Tables.TbAttributeConfig.Get(attr.Type);
            if (attrCfg == null)
            {
                return;
            }

            switch (attrCfg.Category)
            {
                case AttributeCategoryType.Skill:
                    if (GamePlayManager.Default == null)
                    {
                        return;
                    }

                    GamePlayManager.Default.TraverseVillageSoldier((v) =>
                    {
                        if (targetId != 0 && v.cardId != targetId) return;
                        v.AddSkillAttributeModifier(attr.Type, attrModifier, attr.EffectParam, isAdditive);
                    });
                    break;
                    
                case AttributeCategoryType.Soldier:
                    if (GamePlayManager.Default == null)
                    {
                        return;
                    }

                    GamePlayManager.Default.TraverseVillageSoldier((v) =>
                    {
                        if (targetId != 0 && v.cardId != targetId) return;
                        
                        var pos = v.transform.position;
                        switch (attr.Type)
                        {
                            case AttributeType.SoldierAttackPower:
                                // 获得攻击力效果加倍
                                var redoubleVal = v.BaseAttributes.GetFinal(AttributeType.GainAttackPowerEffectRedouble);
                                if (redoubleVal > 0f && attr.Value > 0f)
                                {
                                    attrModifier.FlatAdd *= redoubleVal;
                                    attrModifier.PercentAdd *= redoubleVal;
                                }
                                break;
                            
                            case AttributeType.SingleSoldierCriticalRateMax:
                                // 上阵英灵数量为1时，该英灵的暴击率提高至100%
                                if (GamePlayManager.Default.villageGameCards.Count == 1)
                                {
                                    v.BaseAttributes.GetAttr(AttributeType.SoldierCriticalRate).AddModifier(new AttributeModifier
                                    {
                                        SourceKey = attrModifier.SourceKey,
                                        SourceType = attrModifier.SourceType,
                                        FlatAdd = 1,
                                        PercentAdd = 1
                                    }, isAdditive);
                                }
                                break;
                            
                            case AttributeType.SoldierUnableToReleaseSkills:
                                GameLand.Default.LockSkillRelease();
                                break;
                            
                            case AttributeType.SoldierHealth:// 增加当前血量
                                v.SetSoldierHp((int)attrModifier.FlatAdd);
                                return;
                            case AttributeType.SoldierPerRestoreCurrentHealth:
                                v.SetSoldierHp((int)(attrModifier.FlatAdd *v.BaseAttributes.GetFinal(AttributeType.SoldierMaxHealth)));
                                return;
                            case AttributeType.SoldierSoulFlaws:// 英灵灵魂破绽
                                CommonItemPoolManager.Instance.PlayGameObjectPool(new GameObjectPoolData()
                                {
                                    PoolName = "BondDamageEffect01",
                                    ParentTran = v.transform,
                                    Position = new Vector3(pos.x, pos.y - 0.6f, pos.z),
                                    LocalScale = 200,
                                    DelayReturn = 0f
                                });
                                break;
                            
                            case AttributeType.SoldierFuriousState:// 英灵暴怒状态
                                CommonItemPoolManager.Instance.PlayGameObjectPool(new GameObjectPoolData()
                                {
                                    PoolName = "BondLifeEffect02",
                                    ParentTran = v.transform,
                                    Position = new Vector3(pos.x, pos.y - 1f, pos.z),
                                    LocalScale = 150,
                                    DelayReturn = 0f
                                });
                                break;
                            
                            case AttributeType.SoldierMirror:// 英灵镜像
                                if (v.RoleType != SoldierRoleType.Self) return;// 跳过复制体

                                var copySoldier = GamePlayManager.Default.AddSoldierCard(v.cardId, v.level.Value, -1, SoldierRoleType.Mirror);
                                if (copySoldier != null)
                                {
                                    v.CopyBodyList.Add(copySoldier);
                                    // 学习本体技能
                                    foreach (var skillCard in v.GetSkillAll())
                                    {
                                        var skillCopy = copySoldier.LearnSkill(skillCard.Key, skillCard.Value.Level);
                                        if (skillCopy != null) skillCopy.Attributes
                                            .SetAttr(AttributeType.SkillAttackInheritance, attrModifier.FlatAdd, ValueType.Number, false);
                                    }
                                }
                                return;
                        }
                        
                        v.BaseAttributes.GetAttr(attr.Type).AddModifier(attrModifier, isAdditive);
                    });
                    break;
                
                case AttributeCategoryType.Monster:
                    if (GamePlayManager.Default == null)
                    {
                        return;
                    }

                    GamePlayManager.Default.MonsterAttributes.GetAttr(attr.Type).AddModifier(attrModifier, isAdditive);
                    MonsterManager.Instance.AddNewAttr();
                    break;
                    
                case AttributeCategoryType.Module:
                    switch (attr.Type)
                    {
                        case AttributeType.EquipEntryEffectCorrection:
                            // 元属性：同件装备词条倍率在 Equip 聚合阶段处理，不写入 ModuleAttributes。
                            return;
                        case AttributeType.LearnSkillRule:
                            // 学习规则：优先读取 Value，其次读取 EffectParam。
                            // 规则会记录到本局缓存，供战斗流程后续统一注册/触发。
                            var ruleId = Mathf.RoundToInt(attr.Value);
                            if (ruleId <= 0)
                            {
                                ruleId = Mathf.RoundToInt(attr.EffectParam);
                            }

                            if (ruleId <= 0)
                            {
                                LogTool.LogWarning($"[AttributeManager] LearnSkillRule 未解析到有效 RuleId, Value={attr.Value}, EffectParam={attr.EffectParam}");
                                break;
                            }

                            AddBattleSkillRule(
                                ruleId,
                                attrModifier.SourceKey,
                                attrModifier.SourceType,
                                targetId > 0 ? targetId : 0);
                            break;
                        case AttributeType.UnlockSchoolSoldierAddition:
                            // 获取所有已经解锁的流派数量
                            var schoolCount = SchoolManager.Instance.GetSchoolUnlockCount();
                            if (schoolCount > 0)
                            {
                                var additionAttrCfg = DataConfigManager.Tables.TbAttributeConfig.Get(AttributeType.UnlockSchoolSoldierAddition);
                                SetAttributesLogic(new AttributeModifier
                                {
                                    SourceKey = attrModifier.SourceKey,
                                    SourceType = attrModifier.SourceType,
                                }, additionAttrCfg.AdditionAttributes, true, schoolCount);
                            }
                            break;
                        
                        case AttributeType.CurrentSkillCdTimeCorrection:
                            foreach (var skillId in GameLand.Default.skill.Keys)
                            {
                                GameLand.Default.AddSkillCurrentCooldownTime(skillId, attr.Value);
                            }
                            return;
                        
                        case AttributeType.OneManArmy:
                            GamePlayManager.Default.OneManArmyCreateMirrorTime = 30f;
                            SchoolManager.Instance.AddSchoolCount(SchoolType.OneMan, 1);
                            break;
                        
                        case AttributeType.SchoolAwakening:
                            SchoolManager.Instance.AddSchoolCount((int)attr.EffectParam, (int)attr.Value);
                            break;
                        case AttributeType.RandomlyObtainRareEnhancements:
                            var service = GameServiceManager.Get<SchoolChooseCardService>();
                            var cardIds = service.RandomSchoolChooseCards(RandomTool.RandomRangeTarget(1, (int)attr.Value));
                            for (int i = 0; i < cardIds.Count; i++)
                            {
                                SchoolManager.Instance.AddSchoolCard(cardIds[i]);
                            }
                            break;
                        case AttributeType.LockAtkInterval:
                            ApplyLockAtkInterval();
                            break;
                        case AttributeType.LockAtkSpeed:
                            ApplyLockAtkSpeed();
                            break;
                        case AttributeType.LearnBuffEffect:
                            var buffEffectConfig = DataConfigManager.Tables.TbBuffEffectsConfig.Get((int)attr.Value);
                            if (buffEffectConfig != null)
                            {
                                GamePlayManager.Default.TraverseVillageSoldier((v) =>
                                {
                                    var buffable = v as IBuffable;
                                    if (buffable != null)
                                    {
                                        var triggerBuff = new TriggerBuff(buffEffectConfig, buffEffectConfig.DurationTime);
                                        buffable.AddBuff(triggerBuff, v);
                                    }
                                    
                                    //满血
                                    if (v.CurrentHp >= v.MaxHp)
                                    {
                                        if (buffEffectConfig.ConditionType == ConditionsType.HeroFullBlood)
                                        {
                                            EventBus.Publish(new ConditionsTypeEvent
                                            {
                                                coditionsType = ConditionsType.HeroFullBlood,
                                                caster = v,
                                                target = null
                                            });
                                        }
                                    }
                                });

                                
                            }
                            break;
                        // 学习技能
                        case AttributeType.TestFullScreenHarm:
                        case AttributeType.LearnSkillWindDanceAttack:
                        case AttributeType.LearnCitySkillEnergyBurst:
                        case AttributeType.SoldierStartReleaseCometSkill:
                        case AttributeType.LearnSkillThunderFieldFullScreen:
                        case AttributeType.SoldierNormalAttackHeavyBlowSkill:
                        case AttributeType.SoldierNormalAttackBrokenArmySlashSkill:
                        case AttributeType.LearnSkillBrokenBlade:
                            LearnSkillLogic(attr.Type);
                            break;
                    }
                    
                    GamePlayManager.Default.ModuleAttributes.GetAttr(attr.Type).AddModifier(attrModifier, isAdditive);
                    break;
                    
                default:
                    LogTool.LogError($"未定义的属性类别: {attrCfg.Category}");
                    break;
            }
        }

        public static List<AttributeDataView> GeAttributeDataViews(AttributeViewParam param)
        {
            // 主属性加成
            var attributes = new AttributeSet();
            // if (param.MainAttrs is { Count: > 0 })
            // {
            //     var mainAttributes = new MainAttributeSet(param.MainAttrType, attributes);
            //     foreach (var attr in param.MainAttrs)
            //     {
            //         mainAttributes.Set(attr.Type, attr.Value);
            //     }
            // }

            // 基础属性加成
            var attrData = new List<AttributeDataView>();
            foreach (var v in param.Attrs)
            {
                if (param.ShowAttrTypes.Count > 0 && !param.ShowAttrTypes.Contains(v.Type)) 
                    continue;
                
                var symbol = v.Value > 0 ? "+" : "";
                var attrCfg = DataConfigManager.Tables.TbAttributeConfig.GetOrDefault(v.Type);
                
                // 主属性加成
                var mainVal = attributes.GetFinal(v.Type);
                
                // 下一等级提升
                float valDiff = 0;
                if (param.NextAttrs is { Count: > 0 })
                {
                    foreach (var attr in param.NextAttrs)
                    {
                        if (attr.Type != v.Type) continue;
                        valDiff = (float)Math.Round((attr.Value + mainVal) - (v.Value + mainVal), 2);
                        break;
                    }
                }
                
                var valDiffSymbol = valDiff > 0 ? "+" : "";
                attrData.Add(new AttributeDataView()
                {
                    Type = v.Type,
                    Name = attrCfg.Name,
                    Value = SumValuePercentage(v.Type, v.Value + mainVal, v.ValueType),
                    ValueDiff = SumValuePercentage(v.Type, valDiff, v.ValueType),
                    ValueSymbol = symbol,
                    SuffixSymbol = v.ValueType == ValueType.Percentage ? "%" : "",
                    ValueDiffSymbol = valDiffSymbol,
                });
            }
            
            return attrData;
        }

        public static float SumValuePercentage(AttributeType type, float value, ValueType valueType)
        {
            value = valueType == ValueType.Percentage ? value * 100 : value;
            if (type is AttributeType.SoldierCriticalRate or AttributeType.SoldierCriticalDamage) {
                value *= 100;
            }

            return value;
        }

        // 学习技能
        private static void LearnSkillLogic(AttributeType attrType)
        {
            var skillType = SpecialEffectType.WindDanceAttack;
            bool isUseSkill = false;// 使用技能
            bool isFirstSoldier = false;// 首个角色生效
            
            switch (attrType)
            {
                // 首个角色生效
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
                
            }
            
            var skillCfg = DataConfigManager.Tables.TbSpecialEffectsConfig.DataList.FirstOrDefault(v =>
                v.SpecialEffectType == skillType);
            if (skillCfg == null)
            {
                LogTool.LogError($"属性.学习新技能: {skillType} 类型配置未找到");
                return;
            }

            foreach (var soldierCard in GamePlayManager.Default.villageGameCards)
            {
                var skillCard = soldierCard.LearnSkill(skillCfg.Id, 1);
                if (skillType == SpecialEffectType.CdTimeEnergyBurst)// 能量迸发
                {
                    Global.CitySkills.TryAdd(skillCfg.Id, new CitySkill()
                    {
                        Id = skillCfg.Id,
                        BaseSkill = skillCard,
                    });
                    //注释掉主城技能列表
                  GameLandUI.Default.SetCitySkillCardScroll();
                }

                if (isUseSkill) soldierCard.UseSkill(skillCfg.Id, Vector3.zero);
                if (isFirstSoldier) break;
            }
        }
        
    }
    
    public class AttributeData
    {
        public AttributeType Type;
        public float Value;
        public ValueType ValueType;
        public float EffectParam;
        public float ProbabilityTrigger;

        public AttributeData()
        {
        }

        public AttributeData(AttributeStruct v)
        {
            Type = v.Type;
            Value = v.Value;
            ValueType = v.ValueType;
            EffectParam = v.EffectParam;
            ProbabilityTrigger = v.ProbabilityTrigger;
        }
        
        
    }
    
    public class AttributeViewParam
    {
        public List<AttributeStruct> Attrs = new List<AttributeStruct>();
        public List<AttributeStruct> NextAttrs = new List<AttributeStruct>();
        public List<AttributeType> ShowAttrTypes = new List<AttributeType>();
        
        // 主属性
        public MainAttributeType MainAttrType = MainAttributeType.Strength;
        public List<MainAttributeStruct> MainAttrs = new List<MainAttributeStruct>();

    }

    public class AttributeDataView
    {
        public AttributeType Type;
        public string Name;
        public float Value;
        public float ValueDiff;
        public string ValueSymbol;
        public string SuffixSymbol;
        public string ValueDiffSymbol;

    }
}