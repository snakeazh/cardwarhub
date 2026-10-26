using System;
using System.Collections.Generic;
using System.Linq;
using Config;
using FusionContinent.Attribute;
using FusionContinent.Buff;
using Manager;
using UnityEngine;
using ValueType = Config.ValueType;

namespace FusionContinent
{
    /// <summary>
    /// 通用技能（奥数卡）管理器
    /// 参照 SchoolManager 的模式，通过 AttributeManager 统一处理属性生效
    /// </summary>
        public class GeneralSkillManager : MonoBehaviour
    {
        public static GeneralSkillManager Instance;

        /// <summary>
        /// 已激活的奥数卡字典（key: 卡牌ID）
        /// </summary>
        private readonly Dictionary<int, GeneralSkillCard> _generalSkillCards = new Dictionary<int, GeneralSkillCard>();

        /// <summary>
        /// 是否持有贪婪预支卡
        /// </summary>
        public bool HasGreedyAdvanceCard => _generalSkillCards.Values.Any(c =>
            c.SpecialCfg.SpecialEffectType == SpecialEffectType.GreedyAdvancePayment);

        /// <summary>
        /// 已透支的金额（正值表示欠款，初始为0）
        /// </summary>
        private int _overdraftAmount;

        /// <summary>
        /// 透支上限
        /// </summary>
        private int _overdraftLimit;

        /// <summary>
        /// 初始债务（开局时预支的金币，需要通过局内收益偿还）
        /// </summary>
        private int _initialDebt;

        /// <summary>
        /// 当前透支金额
        /// </summary>
        public int OverdraftAmount => _overdraftAmount;

        /// <summary>
        /// 透支上限
        /// </summary>
        public int OverdraftLimit => _overdraftLimit;

        /// <summary>
        /// 在 AddGeneralSkill 时调用，标记贪婪预支卡已激活并初始化透支系统
        /// </summary>
        public void OnGreedyAdvanceCardAdded()
        {
            _overdraftAmount = 0;
            _overdraftLimit = DataConfigManager.Tables.TbGameBattleConst.SpecialArcCardGoldNumTwo;
        }
        

        /// <summary>
        /// 当获得金币时的处理：先偿还债务（初始债务优先），债务还完后余额才增加
        /// </summary>
        /// <param name="gainedGold">获得的金币数量</param>
        /// <returns>实际增加到可见金币的数量</returns>
        public int OnGoldGained(int gainedGold)
        {
            gainedGold += (int)GamePlayManager.Default.ModuleAttributes.GetFinal(AttributeType.KillAnEnemyExtraBattleGoldCoin);
            if (gainedGold <= 0)
            {
                return gainedGold;
            }

            // 没有贪婪预支卡时，直接加钱
            if (!HasGreedyAdvanceCard)
            {
                Global.GameGold.Value += gainedGold;
                return gainedGold;
            }

            // 有贪婪预支卡时：优先偿还初始债务
            if (_initialDebt > 0)
            {
                int repayAmount = Mathf.Min(_initialDebt, gainedGold);
                _initialDebt -= repayAmount;
                int remainGold = gainedGold - repayAmount;

                if (remainGold > 0)
                {
                    Global.GameGold.Value += remainGold;
                }

                return remainGold;
            }

            // 初始债务已还完，有透支时继续偿还透支
            if (_overdraftAmount > 0)
            {
                int repayAmount = Mathf.Min(_overdraftAmount, gainedGold);
                _overdraftAmount -= repayAmount;
                int remainGold = gainedGold - repayAmount;

                if (remainGold > 0)
                {
                    Global.GameGold.Value += remainGold;
                }

                return remainGold;
            }

            // 没有债务时，直接加钱
            Global.GameGold.Value += gainedGold;
            return gainedGold;
        }

        /// <summary>
        /// 重置透支状态（游戏重置时调用）
        /// </summary>
        public void ResetOverdraft()
        {
            _overdraftAmount = 0;
            _overdraftLimit = 0;
            _initialDebt = 0;
        }

        /// <summary>
        /// 初始化透支系统（贪婪预支卡开局时调用）
        /// 初始债务为 SpecialArcCardGoldNumOne（开局预支的金币）
        /// 透支上限为 SpecialArcCardGoldNumTwo
        /// </summary>
        public void InitOverdraftSystem()
        {
            _initialDebt = DataConfigManager.Tables.TbGameBattleConst.SpecialArcCardGoldNumOne;
            _overdraftAmount = 0;
            _overdraftLimit = DataConfigManager.Tables.TbGameBattleConst.SpecialArcCardGoldNumTwo;
        }

        /// <summary>
        /// 尝试消费金币（支持透支，受透支上限限制）
        /// </summary>
        /// <param name="cost">消费金额</param>
        /// <returns>是否消费成功</returns>
        public bool TrySpendGold(int cost)
        {
            if (cost <= 0)
                return false;

            int balance = Global.GameGold.Value;
            if (balance >= cost)
            {
                Global.GameGold.Value -= cost;
                return true;
            }

            if (!HasGreedyAdvanceCard)
                return false;

            int deficit = cost - balance;
            int maxOverdraft = _overdraftLimit - _overdraftAmount;
            if (deficit > maxOverdraft)
            {
                LogTool.Log($"[GeneralSkillManager] 贪婪预支：消费失败，超出透支上限（可透支={maxOverdraft}，需={deficit}）");
                return false;
            }

            _overdraftAmount += deficit;
            Global.GameGold.Value = 0;
            return true;
        }

        /// <summary>
        /// 获取实际可用金币（扣除透支）
        /// </summary>
        public int GetAvailableGold()
        {
            return Mathf.Max(0, Global.GameGold.Value - _overdraftAmount);
        }

        /// <summary>
        /// 获取用于UI显示的金币（可显示为负数，表示欠款）
        /// </summary>
        public int GetDisplayGold()
        {
            return Global.GameGold.Value;
        }

        private TbSpecialEffectsConfig _specialEffectsConfig;
        private TbConditionsConfig _conditionsConfig;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }

            _specialEffectsConfig = DataConfigManager.Tables.TbSpecialEffectsConfig;
            _conditionsConfig = DataConfigManager.Tables.TbConditionsConfig;
        }

        public void Init()
        {
            _generalSkillCards.Clear();
            _overdraftAmount = 0;
            _overdraftLimit = 0;
            _initialDebt = 0;
        }

        /// <summary>
        /// 通过卡牌ID获取奥数卡数据
        /// </summary>
        public GeneralSkillCard GetGeneralSkillCard(int cardId)
        {
            return _generalSkillCards.GetValueOrDefault(cardId);
        }

        /// <summary>
        /// 获取所有已激活的奥数卡
        /// </summary>
        public Dictionary<int, GeneralSkillCard> GetAllGeneralSkillCards()
        {
            return _generalSkillCards;
        }

        /// <summary>
        /// 添加奥数卡（被动类型）— 在 InitCardTeams 中调用
        /// 走 AttributeManager 统一属性生效
        /// </summary>
        /// <param name="cardId">奥数卡ID</param>
        /// <param name="cardLevel">卡牌等级</param>
        /// <returns>是否添加成功</returns>
        public bool AddGeneralSkill(int cardId, int cardLevel)
        {
            if (_generalSkillCards.ContainsKey(cardId))
                return false;

            var specialCfg = _specialEffectsConfig.GetOrDefault(cardId);
            if (specialCfg == null)
            {
                LogTool.LogError($"[GeneralSkillManager] 添加奥数卡失败，配置不存在: {cardId}");
                return false;
            }

            // 获取等级属性配置
            if (cardLevel <= 0 || cardLevel > specialCfg.LevelPropertyList.Count)
            {
                LogTool.LogError($"[GeneralSkillManager] 奥数卡 {cardId} 等级 {cardLevel} 超出配置范围");
                return false;
            }

            var lvCfg = specialCfg.LevelPropertyList[cardLevel - 1];
            var generalSkillCard = new GeneralSkillCard(cardId, cardLevel, specialCfg, lvCfg);
            _generalSkillCards.Add(cardId, generalSkillCard);

            LogTool.L($"[GeneralSkillManager] 开局携带奥数卡: cardId={cardId}, level={cardLevel}, name={specialCfg.Id}");

            // 分离普通属性和条件Buff属性
            var normalAttrs = new List<AttributeStruct>();
            var conditionAttrs = new List<AttributeStruct>();
            foreach (var attr in lvCfg.Attributes)
            {
                if (attr.Type == AttributeType.LearnConditionBuff)
                    conditionAttrs.Add(attr);
                else
                    normalAttrs.Add(attr);
            }

            // 通过 AttributeManager 统一生效普通副属性
            if (normalAttrs.Count > 0)
            {
                var attrModifier = new AttributeModifier
                {
                    SourceKey = $"{cardId}",
                    SourceType = AttributeSourceType.GeneralSkill,
                };
                AttributeManager.SetAttributesLogic(attrModifier, normalAttrs, true);
            }

                // 处理条件Buff属性
            foreach (var condAttr in conditionAttrs)
            {
                var conditionId = (int)condAttr.Value;
                var condCfg = _conditionsConfig.GetOrDefault(conditionId);
                if (condCfg == null)
                {
                    LogTool.LogError($"[GeneralSkillManager] 条件配置不存在: {conditionId}");
                    continue;
                }

                switch (condCfg.EffectiveTiming)
                {
                    // 伤害时机条件已迁移至 LearnSkillRule(115) + BattleRuleDamageModifier，不再写入 atkCarryings
                    case EffectiveTimingType.Damage:
                        LogTool.LogWarning(
                            $"[GeneralSkillManager] 奥数卡 {cardId} 仍使用 LearnConditionBuff(Damage)，请改为 LearnSkillRule: conditionId={condCfg.Id}");
                        break;

                    // 开局计算时机：仍走旧 ConditionsConfig 分发（OpeningCalculation 不在 SkillRule 迁移范围）
                    case EffectiveTimingType.OpeningCalculation:
                        LogTool.L($"[GeneralSkillManager] 奥数卡 {cardId} 条件Buff(时机=OpeningCalculation): conditionId={condCfg.Id}, 开始判定...");
                        ApplyOpeningCalculationAttributes(cardId, condCfg);
                        break;
                }
            }

            // 通过 AttributeManager 统一生效主属性
            if (lvCfg.MainAttributes.Count > 0)
            {
                // 主属性直接应用到所有英灵
                foreach (var v in GamePlayManager.Default.villageGameCards)
                {
                    foreach (var attr in lvCfg.MainAttributes)
                    {
                        v.BaseAttributes.UpdateMainAttributeValue(attr.Type, attr.Value, true);
                    }
                }
            }

            // 贪婪预支卡的透支系统初始化在 SceneInitGreedyAdvance 中统一处理

            return true;
        }

        /// <summary>
        /// 将当前已激活的奥数卡直接属性补应用到指定英灵（用于战斗中新增英灵）。
        /// 开局 AddGeneralSkill 时场上通常只有首个英灵，后续上场需走此路径。
        /// </summary>
        public void ApplyExistingGeneralSkillsToSoldier(SoldierCard soldier)
        {
            if (soldier == null) return;

            foreach (var pair in _generalSkillCards)
            {
                var card = pair.Value;
                var lvCfg = card.LevelCfg;
                if (lvCfg == null) continue;

                var normalAttrs = new List<AttributeStruct>();
                foreach (var attr in lvCfg.Attributes)
                {
                    // 条件Buff（开局计算/伤害时机）不在此处补挂，避免重复注册或错误重算
                    if (attr.Type == AttributeType.LearnConditionBuff)
                        continue;

                    var attrCfg = DataConfigManager.Tables.TbAttributeConfig.GetOrDefault(attr.Type);
                    if (attrCfg == null) continue;
                    if (attrCfg.Category != AttributeCategoryType.Soldier &&
                        attrCfg.Category != AttributeCategoryType.Skill)
                    {
                        continue;
                    }

                    normalAttrs.Add(attr);
                }

                if (normalAttrs.Count > 0)
                {
                    var attrModifier = new AttributeModifier
                    {
                        SourceKey = $"{card.CardId}",
                        SourceType = AttributeSourceType.GeneralSkill,
                    };
                    AttributeManager.SetAttributesLogic(attrModifier, normalAttrs, true, 1, soldier.cardId);
                }

                if (lvCfg.MainAttributes.Count > 0)
                {
                    foreach (var attr in lvCfg.MainAttributes)
                    {
                        soldier.BaseAttributes.UpdateMainAttributeValue(attr.Type, attr.Value, true);
                    }
                }
            }
        }

        /// <summary>
        /// 应用被动技能属性到对应英灵的技能（PassiveActsOnCard）
        /// 在所有英灵和技能初始化完成后调用
        /// </summary>
        public void ApplyPassiveSkillAttributes()
        {
            foreach (var generalSkillCard in _generalSkillCards.Values)
            {
                var specialCfg = generalSkillCard.SpecialCfg;
                if (specialCfg.PassiveSkill == 0 || specialCfg.ActsOnCard == 0)
                    continue;

                // 获取被动技能所属的英灵
                var soldierCard = GamePlayManager.Default.GetVillageGameCard(specialCfg.ActsOnCard);
                if (soldierCard == null) continue;

                // 获取该英灵的被动技能卡
                var passiveSkillCard = soldierCard.GetSkillById(specialCfg.Id);
                if (passiveSkillCard == null) continue;

                // 将被动属性传递给 PassiveActsOnCard 中的技能
                foreach (var cardId in specialCfg.PassiveActsOnCard)
                {
                    var skillCard = soldierCard.GetSkillById(cardId);
                    if (skillCard == null) continue;
                    skillCard.Attributes.AddRangeBaseValue(passiveSkillCard.Attributes);
                }
            }
        }
        /// <summary>
        /// 开局计算时机：根据 SpecialType 统计对应类型英灵数量，满足条件后对符合类型的英灵施加属性
        /// WarriorNum 等职业类型：条件满足时对所有该职业英灵施加属性，并学习触发型Buff
        /// PowerHero/IntHero/AgHero：条件满足时对所有该主属性英灵施加属性
        /// </summary>
        private void ApplyOpeningCalculationAttributes(int cardId, ConditionsConfig condCfg)
        {
            if (condCfg.SpecialType == SpecialType.GoalAttribute)
            {
                LogTool.LogWarning($"[GeneralSkillManager] SpecialType.GoalAttribute 开局计算暂未支持，conditionId={condCfg.Id}");
                return;
            }

            // 判断是否为职业类型（战士/射手/法师/刺客/辅助）
            bool isCareerType = IsCareerSpecialType(condCfg.SpecialType);

            List<SoldierCard> eligibleHeroes;
            if (isCareerType)
            {
                // 职业类型：统计对应职业的英灵数量
                var targetCareerType = ToCareerType(condCfg.SpecialType);
                eligibleHeroes = GamePlayManager.Default.villageGameCards
                    .Where(v => GetCareerType(v) == targetCareerType)
                    .ToList();
            }
            else
            {
                // 主属性类型：统计对应主属性的英灵数量
                var targetMainAttrType = condCfg.SpecialType switch
                {
                    SpecialType.PowerHero => MainAttributeType.Strength,
                    SpecialType.IntHero => MainAttributeType.Intelligence,
                    SpecialType.AgHero => MainAttributeType.Dexterity,
                    _ => MainAttributeType.Strength
                };

                eligibleHeroes = GamePlayManager.Default.villageGameCards
                    .Where(v => v.MainAttrType == targetMainAttrType)
                    .ToList();
            }

            int count = eligibleHeroes.Count;

            LogTool.L($"[GeneralSkillManager] 条件判定: conditionId={condCfg.Id}, SpecialType={condCfg.SpecialType}, 当前符合条件的英灵数量={count}, 要求={condCfg.ComparisonType} {condCfg.ComparisonValue}");

            // 通用职业数量条件判断
            bool conditionMet = CheckCareerCondition(condCfg, count);

            if (!conditionMet)
            {
                LogTool.L($"[GeneralSkillManager] 条件判定失败: conditionId={condCfg.Id}, 奥数卡 {cardId} 的条件Buff 未生效");
                return;
            }

            LogTool.L($"[GeneralSkillManager] 条件判定成功: conditionId={condCfg.Id}, 奥数卡 {cardId} 的条件Buff 生效！");

            // 对所有符合条件的英灵施加属性（仅对符合条件的职业英灵生效，而非所有英灵）
            foreach (var hero in eligibleHeroes)
            {

                // 为每个符合条件的英灵应用属性
                foreach (var attr in condCfg.TakeAttributes)
                {
                    var attrCfg = DataConfigManager.Tables.TbAttributeConfig.Get(attr.Type);
                    // 创建属性修改器
                    var modifier = new AttributeModifier
                    {
                        SourceKey = $"{cardId}_Condition_{condCfg.Id}",
                        SourceType = AttributeSourceType.GeneralSkill,
                        FlatAdd = attr.ValueType == ValueType.Number ? attr.Value : 0,
                        PercentAdd = attr.ValueType == ValueType.Percentage ? attr.Value : 0
                    };
                    
                    switch (attrCfg.Category)
                    {
                        case AttributeCategoryType.Skill:
                            hero.AddSkillAttributeModifier(attr.Type, modifier, attr.EffectParam, true);
                            break;

                        case AttributeCategoryType.Soldier:
                            hero.BaseAttributes.GetAttr(attr.Type).AddModifier(modifier, true);
                            break;

                        case AttributeCategoryType.Module:
                            GamePlayManager.Default.ModuleAttributes.GetAttr(attr.Type).AddModifier(modifier, true);
                            break;
                    }

                    LogTool.L($"[GeneralSkillManager]   属性生效 -> hero={hero.cardId}, attr={attr.Type}({attrCfg?.Name}), value={attr.Value}, effectParam={attr.EffectParam}");
                }
            }

            // 职业类型额外逻辑：学习触发型Buff（BuffId 由 attr.EffectParam 指定）
            if (isCareerType)
            {
                LearnTriggerBuffForCareer(cardId, condCfg, eligibleHeroes);
            }
        }

        /// <summary>
        /// 职业类型英灵学习触发型Buff
        /// 从 TakeAttributes 的 EffectParam 读取 BuffId，为所有符合条件的英灵添加该触发型Buff
        /// </summary>
        private void LearnTriggerBuffForCareer(int cardId, ConditionsConfig condCfg, List<SoldierCard> eligibleHeroes)
        {
            // 从 TakeAttributes 中查找 EffectParam 作为 BuffId
            foreach (var attr in condCfg.TakeAttributes)
            {
                if (attr.EffectParam <= 0)
                    continue;

                int buffEffectId = (int)attr.EffectParam;
                var buffEffectConfig = DataConfigManager.Tables.TbBuffEffectsConfig.GetOrDefault(buffEffectId);
                if (buffEffectConfig == null)
                {
                    LogTool.LogWarning($"[GeneralSkillManager] BuffEffectsConfig 不存在: {buffEffectId}");
                    continue;
                }

                foreach (var hero in eligibleHeroes)
                {
                    var buffable = hero as IBuffable;
                    if (buffable == null) continue;

                    var triggerBuff = new TriggerBuff(buffEffectConfig, buffEffectConfig.DurationTime);
                    triggerBuff.SourceKey = $"{cardId}_CareerBuff_{condCfg.Id}";

                    // 若 Buff 支持叠层，设置最大层数
                    if (buffEffectConfig.DurationTime > 0)
                    {
                        triggerBuff.MaxStack = 5; // 受击增伤最多5层
                    }

                    buffable.AddBuff(triggerBuff, hero);
                    LogTool.L($"[GeneralSkillManager] 战士触发型Buff学习成功: hero={hero.cardId}, buffId={buffEffectId}");
                }
            }
        }

        /// <summary>
        /// 判断 SpecialType 是否为职业类型
        /// </summary>
        private static bool IsCareerSpecialType(SpecialType specialType)
        {
            return specialType switch
            {
                SpecialType.WarriorNum or
                SpecialType.ArcherNum or
                SpecialType.MageNum or
                SpecialType.AssassinNum or
                SpecialType.AssistantNum => true,
                _ => false
            };
        }

        /// <summary>
        /// 将职业类型的 SpecialType 转换为 CareerType
        /// </summary>
        private static CareerType ToCareerType(SpecialType specialType)
        {
            return specialType switch
            {
                SpecialType.WarriorNum => CareerType.Warrior,
                SpecialType.ArcherNum => CareerType.Archer,
                SpecialType.MageNum => CareerType.Wizard,
                SpecialType.AssassinNum => CareerType.Assassin,
                SpecialType.AssistantNum => CareerType.Assistant,
                _ => CareerType.Warrior
            };
        }

        /// <summary>
        /// 获取英灵的职业类型
        /// </summary>
        private static CareerType GetCareerType(SoldierCard soldier)
        {
            var soldierCfg = DataConfigManager.Tables.TbSoldierBattleConfig.GetOrDefault(soldier.cardId);
            return soldierCfg?.CarType ?? CareerType.Warrior;
        }

        /// <summary>
        /// 通用职业数量条件判断
        /// 根据 SpecialType 统计对应职业的英灵数量，判断是否满足条件
        /// </summary>
        /// <param name="condCfg">条件配置</param>
        /// <returns>条件是否满足</returns>
        public static bool CheckCareerCondition(ConditionsConfig condCfg)
        {
            if (!IsCareerSpecialType(condCfg.SpecialType))
                return false;

            var targetCareerType = ToCareerType(condCfg.SpecialType);
            int careerCount = GamePlayManager.Default.villageGameCards
                .Count(v => !v.isDie && GetCareerType(v) == targetCareerType);

            return CheckCareerCondition(condCfg, careerCount);
        }

        /// <summary>
        /// 通用职业数量条件判断
        /// </summary>
        /// <param name="condCfg">条件配置</param>
        /// <param name="count">已统计的数量</param>
        /// <returns>条件是否满足</returns>
        public static bool CheckCareerCondition(ConditionsConfig condCfg, int count)
        {
            return condCfg.ComparisonType switch
            {
                ComparisonType.GreaterThanOrEqualTo => count >= condCfg.ComparisonValue,
                ComparisonType.GreaterThan => count > condCfg.ComparisonValue,
                ComparisonType.LessThan => count < condCfg.ComparisonValue,
                ComparisonType.LessThanOrEqualTo => count <= condCfg.ComparisonValue,
                _ => false
            };
        }
    }

    /// <summary>
    /// 奥数卡数据类
    /// </summary>
    public class GeneralSkillCard
    {
        public readonly int CardId;
        public readonly int CardLevel;
        public readonly SpecialEffectsConfig SpecialCfg;
        public readonly SpecialLevelPropertyList LevelCfg;

        public GeneralSkillCard(int cardId, int cardLevel, SpecialEffectsConfig specialCfg, SpecialLevelPropertyList levelCfg)
        {
            CardId = cardId;
            CardLevel = cardLevel;
            SpecialCfg = specialCfg;
            LevelCfg = levelCfg;
        }
    }
}
