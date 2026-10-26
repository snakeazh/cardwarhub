using System;
using System.Collections.Generic;
using Config;
using FusionContinent.Attribute;
using Manager;
using UnityEngine;
using ValueType = Config.ValueType;

namespace FusionContinent.Skill
{
    public abstract class SkillBase : ISkill
    {
        public int Id { get; private set; }
        public int Level { get; private set; }
        public SpecialEffectType Type { get; private set; }
        public SoldierCard Parent { get; private set; }
        
        public AttributeSet Attributes { get; private set; }
        public SpecialEffectsConfig Config { get; private set; }

        private bool _isNormalAttack; // 是否为普攻
        private DateTime _lastCastTime = DateTime.Now; // 上次施放的时间

        public void Initialize(SoldierCard parent, int level, SpecialEffectsConfig cfg)
        {
            Config = cfg;
            Parent = parent;

            // 注入基础数据
            Id = cfg.Id;
            Level = level;
            Type = cfg.SpecialEffectType;

            // 是否为普攻
            _isNormalAttack = Parent.NormalAttackSkillId == cfg.Id;

            // 注入属性
            var lvCfg = cfg.LevelPropertyList[Level - 1];
            Attributes = new AttributeSet();
            foreach (var attr in lvCfg.Attributes)
            {
                // 类别
                var attrCfg = DataConfigManager.Tables.TbAttributeConfig.Get(attr.Type);
                switch (attrCfg.Category)
                {
                    case AttributeCategoryType.Skill:
                    case AttributeCategoryType.Add:
                        // Skill：技能自身属性；Add：规则参数（供 SourceAttrType / Buff *AttrName 读取）
                        Attributes.GetAttr(attr.Type).SetBaseValue(attr.Value, true);
                        break;

                    case AttributeCategoryType.Soldier:
                        Parent.BaseAttributes.GetAttr(attr.Type).AddModifier(new AttributeModifier
                        {
                            SourceKey = $"{Id}",
                            SourceType = AttributeSourceType.Skill,
                            FlatAdd = attr.ValueType == ValueType.Number ? attr.Value : 0,
                            PercentAdd = attr.ValueType == ValueType.Percentage ? attr.Value : 0
                        }, true);
                        break;

                    case AttributeCategoryType.Module:
                        if (attr.Type == AttributeType.LearnSkillRule)
                        {
                            var ruleId = Mathf.RoundToInt(attr.Value);
                            if (ruleId <= 0)
                            {
                                ruleId = Mathf.RoundToInt(attr.EffectParam);
                            }

                            if (ruleId > 0)
                            {
                                AttributeManager.AddBattleSkillRule(ruleId, $"{Id}", AttributeSourceType.Skill);
                            }
                            else
                            {
                                LogTool.LogWarning(
                                    $"[SkillBase] LearnSkillRule 未解析到有效 RuleId, skillId={Id}, Value={attr.Value}, EffectParam={attr.EffectParam}");
                            }
                            break;
                        }

                        GamePlayManager.Default.ModuleAttributes.GetAttr(attr.Type).AddModifier(new AttributeModifier
                        {
                            SourceKey = $"{Id}",
                            SourceType = AttributeSourceType.Skill,
                            FlatAdd = attr.ValueType == ValueType.Number ? attr.Value : 0,
                            PercentAdd = attr.ValueType == ValueType.Percentage ? attr.Value : 0
                        }, true);
                        break;

                    default:
                        LogTool.LogError($"未定义的属性类别: {attrCfg.Category}");
                        break;
                }
            }
        }

        public bool IsReady(bool isActiveRelease)
        {
            // 主动释放：技能卡 CD 已在点击时处理，此处仅用于动画/逻辑放行
            if (isActiveRelease) return true;

            // 计算冷却时间
            var totalSeconds = (DateTime.Now - _lastCastTime).TotalSeconds;
            if (totalSeconds >= GetCooldownTime())
            {
                return ReleaseSkill();
            }

            return false;
        }

        /// <summary>
        /// 点击确认释放时立即将技能卡置入冷却（早于动画命中帧）。
        /// </summary>
        public void BeginCooldownFromClick()
        {
            ApplyCooldownToSkillCard();
        }

        private bool ReleaseSkill()
        {
            ApplyCooldownToSkillCard();
            return true;
        }

        private void ApplyCooldownToSkillCard()
        {
            _lastCastTime = DateTime.Now;
            var cdTime = Global.UnlimitedCdTime.Value ? 0.5f : GetCooldownTime(false);

            // PVP 对侧：技能卡 UI/冷却字典属于玩家侧；对侧 AI 只记录自身冷却，不刷新玩家技能卡
            if (Parent != null && Parent.BattleSide == BattleSide.Opponent)
            {
                return;
            }

            switch (Config.CooldownTiming)
            {
                case SkillCooldownTimingType.AfterBuffEnd:
                    GameLand.Default.ReleaseSkillCoolingShow(Id);
                    break;

                default:
                    GameLand.Default.ReleaseSkillCallBack(Id, cdTime);
                    break;
            }
        }

        public float GetCooldownTime(bool isSpeedMultiplier = true)
        {
            var cdTime = Attributes.GetFinal(AttributeType.SkillCdTime);
            if (cdTime <= 0) return 0.1f;

            // 技能冷却时间，对二倍速生效
            if (isSpeedMultiplier) return cdTime / Global.SpeedMultiplier.Value;
            
            return cdTime;
        }


        public void Cast(object obj)
        {
            // 交给子类实现技能逻辑
            OnCast(obj); 
        }

        /// <summary>
        /// 停止老场景技能的运行时循环（重开战斗、切场景时调用）。
        /// </summary>
        public virtual void StopLegacyRuntime()
        {
        }

        protected abstract void OnCast(object obj);

        /**
         * 计算伤害类型
         */
        public Dictionary<TakeDamageType, int> SumDamageTypes()
        {
            var damageTypes = new Dictionary<TakeDamageType, int>()
            {
                { TakeDamageType.PhysicalDamage, 0 },
                { TakeDamageType.MagicDamage, 0 },
                { TakeDamageType.RealDamage, 0 }
            };

            var skillAttackPower = Attributes.GetFinal(AttributeType.SkillBasicAttackPower);
            for (int i = 0; i < Config.DamageType.Count; i++)
            {
                var type = Config.DamageType[i];
                var damageParam = Config.DamageParam[i];
                
                switch (type)
                {
                    case TakeDamageType.PhysicalDamage:
                        var attackInheritance = Attributes.GetFinal(AttributeType.SkillAttackInheritance);
                        damageTypes[type] = Mathf.CeilToInt(skillAttackPower + (Parent.BaseAttributes.GetFinal(AttributeType.SoldierAttackPower) * attackInheritance));
                        damageTypes[type] += (int)Parent.BaseAttributes.GetFinal(AttributeType.PhysicalDamage);
                        break;

                    case TakeDamageType.MagicDamage:
                        var spellInheritance = Attributes.GetFinal(AttributeType.SkillSpellInheritance);
                        var extraSpellPower = Parent.BaseAttributes.GetFinal(AttributeType.SoldierExtraSpellPower);
                        damageTypes[type] = Mathf.CeilToInt(skillAttackPower + ((Parent.BaseAttributes.GetFinal(AttributeType.SoldierSpellPower) + extraSpellPower) * spellInheritance));
                        damageTypes[type] += (int)Parent.BaseAttributes.GetFinal(AttributeType.MagicDamage);
                        break;

                    case TakeDamageType.RealDamage:
                        damageTypes[type] = Mathf.CeilToInt(damageParam.EffectVal);
                        damageTypes[type] += (int)Parent.BaseAttributes.GetFinal(AttributeType.RealDamage);
                        break;
                }
            }

            if (this.IsNormalAttack())
            {
                var normalAtkSpellPower = Parent.BaseAttributes.GetFinal(AttributeType.SoldierNormalAttackNumExtraSpellPower,
                    (atkNum) => Parent.NormalAttackCount % (int)atkNum == 0);
                if (normalAtkSpellPower > 0f)
                {
                    damageTypes[TakeDamageType.MagicDamage] += Mathf.CeilToInt(normalAtkSpellPower);
                }
                
                var normalAtkRealDamage = Parent.BaseAttributes.GetFinal(AttributeType.SoldierConsecutiveCritBonusDmg,
                    (atkNum) => Parent.CriticalContinueCount % (int)atkNum == 0);
                if (normalAtkRealDamage > 0f)
                {
                    var currentAttackPower = Parent.BaseAttributes.GetFinal(AttributeType.SoldierAttackPower);
                    damageTypes[TakeDamageType.RealDamage] += Mathf.CeilToInt(normalAtkRealDamage * currentAttackPower);
                    Parent.CriticalContinueCount = 0;
                }
                
                var normalAtkProbRealDamage = Parent.BaseAttributes.GetFinal(AttributeType.SoldierNormalAttackNumExtraRealDmg);
                if (normalAtkProbRealDamage > 0f)
                {
                    var mainAttrValue = Parent.BaseAttributes.GetMainAttributeValue();
                    damageTypes[TakeDamageType.RealDamage] += Mathf.CeilToInt(mainAttrValue * normalAtkProbRealDamage);
                }
                
                var tempAddVal = (int)Parent.BaseAttributes.GetFinal(AttributeType.TempRealDamage);
                if (tempAddVal != 0)
                {
                    damageTypes[TakeDamageType.RealDamage] += tempAddVal;
                    Parent.BaseAttributes.GetAttr(AttributeType.TempRealDamage).Clear();
                }

                // 普攻专属附加真伤（生命裁决/神圣打击等规则写入 AttackRealDamage）
                damageTypes[TakeDamageType.RealDamage] += (int)Parent.BaseAttributes.GetFinal(AttributeType.AttackRealDamage);
            }

            // 附加伤害（规则/Buff 持久写入 AddPhysicalDamage / AddRealDamage；AddMagicDamage 走 OnAttackBefore 管线）
            damageTypes[TakeDamageType.PhysicalDamage] += (int)Parent.BaseAttributes.GetFinal(AttributeType.AddPhysicalDamage);
            damageTypes[TakeDamageType.RealDamage] += (int)Parent.BaseAttributes.GetFinal(AttributeType.AddRealDamage);
            
            var pmToTrueRate = Parent.BaseAttributes.GetFinal(AttributeType.SoldierPMDamageToTureDamage);
            if (pmToTrueRate > 0f)
            {
                if (damageTypes.TryGetValue(TakeDamageType.PhysicalDamage, out var physicalDamage) && physicalDamage > 0)
                {
                    var convertPhysical = Mathf.CeilToInt(physicalDamage * pmToTrueRate);
                    damageTypes[TakeDamageType.PhysicalDamage] = physicalDamage - convertPhysical;
                    damageTypes[TakeDamageType.RealDamage] += convertPhysical;
                }

                if (damageTypes.TryGetValue(TakeDamageType.MagicDamage, out var magicDamage) && magicDamage > 0)
                {
                    var convertMagic = Mathf.CeilToInt(magicDamage * pmToTrueRate);
                    damageTypes[TakeDamageType.MagicDamage] = magicDamage - convertMagic;
                    damageTypes[TakeDamageType.RealDamage] += convertMagic;
                }
            }

            return damageTypes;
        }

        //获取暴击值
        public float GetCriticalValue()
        {
            var baseRate = Parent.BaseAttributes.GetFinal(AttributeType.SoldierNumOfCritical);
            // var maxRate = Parent.BaseAttributes.GetFinal(AttributeType.SoldierNumOfAttackSpeed);
            LogTool.Log(baseRate);
            return baseRate / (baseRate + DataConfigManager.Tables.TbGameBattleConst.CriticalNumBase);//暴击基础值
            

        }

        /**
         * 获取暴击伤害
         */
        public float GetCriticalDamage()
        {
            // 技能暴击状态
            if (!_isNormalAttack && Parent.BaseAttributes.GetFinal(AttributeType.SkillTriggersCriticalStrike) <= 0)
            {
                Parent.CriticalContinueCount = 0;
                return 0;
            }

           
            // 判断释放暴击（伪随机）
            var critical_RateAgo = Parent.GetClampedCriticalRate();
            var criticalValue = GetCriticalValue();
            float criticalRate = critical_RateAgo + criticalValue;
            if (!Parent.RollCriticalHit(criticalRate))
            {
                Parent.CriticalContinueCount = 0;
                return 0;
            }

            // 暴击率超过100%时
            float extraDamage = 0;
            if (criticalRate > 1 )
            {
                extraDamage = criticalRate - 1; // 超出的部分会转化为额外暴击伤害
            }
            
            // 英灵触发暴击概率恢复生命值
            var recoverHealth = Parent.BaseAttributes.GetFinal(AttributeType.SoldierTriggerCriticalStrikeRecoverHealth);
            if (recoverHealth > 0)
            {
                Parent.SetSoldierHp((int)recoverHealth);
            }
            
            // 英灵技能暴击技能当前冷却时间重置
            if (!_isNormalAttack && Parent.BaseAttributes.GetFinal(AttributeType.SoldierSkillCriticalCdTimeRestart) > 0f)
            {
                GameLand.Default.AddSkillCurrentCooldownTime(this.Id, -60);
            }

            // 监听条件 - 造成暴击次数
            // GamePlayManager.Default.AddListenerCondition(ConditionType.CausingCriticaCount, 1);
            ConditionManager.Instance.OnTriggerProgress(ConditionType.CausingCriticaCount, 1);
            this.Parent.CriticalContinueCount++;//计算连续触发暴击次数,一旦不暴击就清零
            return Parent.BaseAttributes.GetFinal(AttributeType.SoldierCriticalDamage) + extraDamage;
        }

        /**
         * 获取子弹射速
         */
        protected float GetLaunchSpeed()
        {
            var missileSpeed = Attributes.GetFinal(AttributeType.SkillMissileSpeed);
            return missileSpeed > 0  ? (missileSpeed / 100) : Parent.missileSpeed;
        }
        
        public bool IsNormalAttack() => _isNormalAttack;
    }
}