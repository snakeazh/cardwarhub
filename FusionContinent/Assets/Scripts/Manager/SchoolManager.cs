using System;
using System.Collections.Generic;
using System.Linq;
using Config;
using FusionContinent.Attribute;
using FusionContinent.Buff;
using FusionContinent.Model;
using Manager;
using UnityEngine;
using ValueType = Config.ValueType;

namespace FusionContinent
{
    public class SchoolManager : MonoBehaviour
    {
        public static SchoolManager Instance;
        
        public Dictionary<(QualityType, SchoolCategoryType), List<int>> _qualityPoolSchoolCards =
            new Dictionary<(QualityType, SchoolCategoryType), List<int>>();
        
        private readonly Dictionary<int, SchoolCard> _schoolCards = new Dictionary<int, SchoolCard>(); // 流派卡牌
        private readonly Dictionary<int, int> _schoolCardCounts = new Dictionary<int, int>(); // 流派卡牌数量（key: SchoolCardConfig.Id）
        private readonly Dictionary<int, int> _schoolCardApplyTimes = new Dictionary<int, int>(); // 属性生效次数（key: SchoolCardConfig.Id）
        private readonly Dictionary<int, List<int>> _schoolCardExclusivePassiveSkills = new Dictionary<int, List<int>>(); // 专属被动记录（key: SchoolCardConfig.Id）

        private int _schoolCardMaxStar;
        // private TbSchoolConfig _schoolConfig; // 已不再作为 SchoolManager 的主数据入口
        private TbSchoolCardConfig _schoolCardConfig;
        public TbSchoolCardConfig SchoolCardConfig { get  => _schoolCardConfig; set => _schoolCardConfig = value; }

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

            // _schoolConfig = DataConfigManager.Tables.TbSchoolConfig;
            _schoolCardConfig = DataConfigManager.Tables.TbSchoolCardConfig;
            _schoolCardMaxStar = DataConfigManager.Tables.TbGameBattleConst.SchoolCardMaxStar;
            RefreshQualityPoolSchoolCards();
        }
        
        /// <summary>
        /// 刷新卡池
        /// </summary>
        public void RefreshQualityPoolSchoolCards()
        {
            
            SchoolManager.Instance._qualityPoolSchoolCards.Clear();
            var team = Team.GetTeamByTeamId(Talent.GetUseTalent().TalentId);
            foreach (var cfg in _schoolCardConfig.DataList)
            {
                if (cfg.Category == SchoolCategoryType.Skill && !team.Cards.Contains(cfg.Param)) continue;
                var qualityPoolKey = (cfg.Quality, cfg.Category);
                SchoolManager.Instance._qualityPoolSchoolCards.TryAdd(qualityPoolKey, new List<int>());
                SchoolManager.Instance._qualityPoolSchoolCards[qualityPoolKey].Add(cfg.Id);
            }
        }

        public void Init()
        {
            _schoolCards.Clear();
            _schoolCardCounts.Clear();
            _schoolCardApplyTimes.Clear();
            _schoolCardExclusivePassiveSkills.Clear();
        }
        
        public int GetSchoolCardCount(int schoolCardId)
        {
            return _schoolCardCounts.GetValueOrDefault(schoolCardId, 0);
        }

        public Dictionary<int, int> GetSchoolCardCountAll()
        {
            return _schoolCardCounts;
        }

        /**
         * 获取已解锁流派卡数量（非技能类别）
         */
        public int GetSchoolUnlockCount()
        {
            var count = 0;
            foreach (var kv in _schoolCardCounts)
            {
                if (kv.Value <= 0) continue;
                var cfg = _schoolCardConfig.GetOrDefault(kv.Key);
                if (cfg == null || cfg.Category == SchoolCategoryType.Skill) continue;
                count++;
            }
            return count;
        }
        
        /**
         * 通过流派卡ID，获取流派卡数据
         */
        public SchoolCard GetSchoolCard(int cardId)
        {
            return _schoolCards.GetValueOrDefault(cardId, new SchoolCard(cardId, 0));
        }
        
        /**
         * 取流派卡全部数据
         */
        public Dictionary<int, SchoolCard> GetSchoolCardAll()
        {
            return _schoolCards;
        }

        /**
         * 添加流派卡
         */
        public SchoolCard AddSchoolCard(int cardId)
        {
            var schoolCardCfg = _schoolCardConfig.GetOrDefault(cardId);
            if (schoolCardCfg == null)
            {
                LogTool.E($"添加流派卡失败，未配置的流派ID[{cardId}]");
            }
            else
            {
                LogTool.L($"添加流派卡    [{cardId}]");
            }

            // 获取流派卡信息
            if (!_schoolCards.TryGetValue(cardId, out var schoolCard))
            {
                schoolCard = new SchoolCard(cardId, 0);
                _schoolCards.Add(cardId, schoolCard);

                // 引导 - 首次获得流派卡
                GamePlayManager.Default.TriggerGuide(GuideType.FirstAddSchoolCard);
            }

            // 当前获取到的目标星级配置（显示为下一星，达到上限后停在最大星）
            var previousStarLevel = Mathf.Clamp(schoolCard.CardStar, 0, schoolCardCfg.StarList.Count);
            var previousStarCfg = previousStarLevel > 0 ? GetStarConfigByLevel(schoolCardCfg, previousStarLevel) : null;
            var nextStarLevel = GetNextCardStarLevel(schoolCard, schoolCardCfg);
            var starCfg = GetStarConfigByLevel(schoolCardCfg, nextStarLevel);
            if (starCfg == null)
            {
                LogTool.LogError($"[SchoolManager] AddSchoolCard 失败，星级配置缺失: cardId={cardId}, targetStar={nextStarLevel}");
                return schoolCard;
            }
            schoolCard.CardStar = nextStarLevel;
            HandleExclusiveLearningEffects(schoolCardCfg, previousStarCfg, starCfg);

            // 更新所属流派卡数量（入参使用 SchoolCardConfig.Id）
            var addSchoolCount = Math.Max(starCfg.AddStar, 1);
            AddSchoolCount(cardId, addSchoolCount);
            var isAdditiveAttr = schoolCardCfg.IsAdditiveAttr > 0;
            var effectiveAttributes = GetEffectiveAttributes(starCfg);

            // 流派条件触发型效果已迁移至 LearnSkillRule，不再写入 SoldierCard.atkCarryings
            if (starCfg.ConditionType != ConditionsType.none)
            {
                LogTool.LogWarning(
                    $"[SchoolManager] 流派卡 {cardId} 仍使用 ConditionType 条件触发，请改为 LearnSkillRule: conditionType={starCfg.ConditionType}");
                return schoolCard;
            }

            // 属性生效
            void AddAttr(bool isAdditive = true, int redouble = 1, int targetId = 0)
            {
                _schoolCardApplyTimes.TryAdd(schoolCardCfg.Id, 0);
                _schoolCardApplyTimes[schoolCardCfg.Id]++;
                var applyTime = _schoolCardApplyTimes[schoolCardCfg.Id];
                var currentCount = GetSchoolCardCount(schoolCardCfg.Id);
                LogTool.Log($"[SchoolManager] School属性生效开始: cardId={schoolCardCfg.Id}, applyTime={applyTime}, count={currentCount}, isAdditive={isAdditive}, redouble={redouble}, targetId={targetId}");

                var attrModifier = new AttributeModifier { SourceKey = $"{schoolCardCfg.Id}", SourceType = AttributeSourceType.SchoolCard, };
                AttributeManager.SetAttributesLogic(attrModifier, effectiveAttributes, isAdditive, redouble, targetId);

                LogTool.Log($"[SchoolManager] School属性生效完成: cardId={schoolCardCfg.Id}, applyTime={applyTime}, attrNum={effectiveAttributes.Count}");
            }

            // 属性生效条件
            // if (starCfg.Condition.Count > 0)
            // {
            //     ConditionManager.Instance.AddListener(starCfg.Condition.First(), $"{schoolCardCfg.Id}", (completeRound, target) =>
            //         {
            //             if (gameObject == null) return;
            //
            //             var targetId = 0;
            //             if (target != null)
            //             {
            //                 targetId = (int)target;
            //             }
            //
            //             // 未达成条件，移除属性加成
            //             if (completeRound <= 0)
            //             {
            //                 AddAttr(false, 0, targetId);
            //                 return;
            //             }
            //             
            //             // 达成条件，生效次数
            //             if (starCfg.MaxTakeNum > 0f && schoolCard.EffectNum >= starCfg.MaxTakeNum)
            //             {
            //                 ConditionManager.Instance.RemoveListener(starCfg.Condition.First(), $"{schoolCardCfg.Id}");
            //                 return;
            //             }
            //             
            //             AddAttr(isAdditiveAttr, completeRound, targetId);
            //             schoolCard.EffectNum++;
            //         });
            // }
            // else
            // {
                var scopeHeroId = schoolCardCfg.IsAddHero > 0 ? schoolCardCfg.IsAddHero : 0;
                AddAttr(isAdditiveAttr, 1, scopeHeroId);
            // }

            return schoolCard;
        }

        private List<AttributeStruct> GetEffectiveAttributes(SchoolCardStarListStruct starCfg)
        {
            if (starCfg?.Attributes == null || starCfg.Attributes.Count == 0)
            {
                return new List<AttributeStruct>();
            }

            return starCfg.Attributes
                .Where(attr => attr.Type != AttributeType.ExclusiveLearningEffects)
                .ToList();
        }

        private void HandleExclusiveLearningEffects(SchoolCardConfig schoolCardCfg, SchoolCardStarListStruct previousStarCfg, SchoolCardStarListStruct currentStarCfg)
        {
            if (schoolCardCfg == null || schoolCardCfg.IsAddHero <= 0)
            {
                return;
            }

            var gamePlayManager = GamePlayManager.Default;
            if (gamePlayManager == null)
            {
                return;
            }

            var previousSkills = _schoolCardExclusivePassiveSkills.GetValueOrDefault(schoolCardCfg.Id);
            if (previousSkills == null || previousSkills.Count == 0)
            {
                previousSkills = ExtractExclusivePassiveSkillIds(previousStarCfg);
            }

            foreach (var passiveSkillId in previousSkills.Distinct())
            {
                gamePlayManager.ForgetPassiveSkillCard(passiveSkillId, schoolCardCfg.IsAddHero);
            }

            var currentSkills = ExtractExclusivePassiveSkillIds(currentStarCfg);
            foreach (var passiveSkillId in currentSkills.Distinct())
            {
                gamePlayManager.LearnPassiveSkillCard(passiveSkillId, 1, schoolCardCfg.IsAddHero);
            }

            _schoolCardExclusivePassiveSkills[schoolCardCfg.Id] = currentSkills;
        }

        private List<int> ExtractExclusivePassiveSkillIds(SchoolCardStarListStruct starCfg)
        {
            var result = new List<int>();
            if (starCfg?.Attributes == null || starCfg.Attributes.Count == 0)
            {
                return result;
            }

            foreach (var attr in starCfg.Attributes)
            {
                if (attr.Type != AttributeType.ExclusiveLearningEffects || attr.Value <= 0)
                {
                    continue;
                }

                result.Add(Mathf.RoundToInt(attr.Value));
            }

            return result;
        }

        private int GetNextCardStarLevel(SchoolCard schoolCard, SchoolCardConfig schoolCardCfg)
        {
            var maxStar = schoolCardCfg?.StarList?.Count ?? 0;
            if (maxStar <= 0)
            {
                return 0;
            }

            var currentStar = Mathf.Clamp(schoolCard.CardStar, 0, maxStar);
            return Mathf.Min(currentStar + 1, maxStar);
        }

        private SchoolCardStarListStruct GetStarConfigByLevel(SchoolCardConfig schoolCardCfg, int starLevel)
        {
            if (schoolCardCfg?.StarList == null || schoolCardCfg.StarList.Count == 0)
            {
                return null;
            }

            var clampedStar = Mathf.Clamp(starLevel, 1, schoolCardCfg.StarList.Count);
            return schoolCardCfg.StarList[clampedStar - 1];
        }

        /**
         * 添加流派数量
         * 
         * @return 
         * @author Zpp
         * @create 2026/1/14
         **/
        public void AddSchoolCount(int schoolCardId, int addCount)
        {
            var schoolCardCfg = _schoolCardConfig.GetOrDefault(schoolCardId);
            if (schoolCardCfg != null)
            {
                _schoolCardCounts.TryAdd(schoolCardId, 0);
                var before = _schoolCardCounts[schoolCardId];
                _schoolCardCounts[schoolCardId] = before + addCount;

                // 通知刷新（当前统一用 cardId 作为事件id）
                Global.Publisher.Publish(MessagePublisher.CHANGE_SCHOOL_STAR,
                    new ChangeSchoolStar(schoolCardId, _schoolCardCounts[schoolCardId]));

                if (before <= 0)
                {
                    Global.Publisher.Publish(MessagePublisher.UNLOCK_SCHOOL, schoolCardId);
                }
                return;
            }

            LogTool.LogWarning($"[SchoolManager] AddSchoolCount 失败，未找到 SchoolCardConfig: schoolCardId={schoolCardId}");
        }

        public void AddSchoolCount(SchoolType schoolType, int addCount)
        {
            var schoolCfg = DataConfigManager.Tables.TbSchoolConfig.DataList.FirstOrDefault(v => v.SchoolType == schoolType);
            if (schoolCfg == null)
            {
                return;
            }

            var schoolCardCfg = _schoolCardConfig.DataList.FirstOrDefault(v => v.Schools != null && v.Schools.Contains(schoolCfg.Id));
            if (schoolCardCfg == null)
            {
                LogTool.LogWarning($"[SchoolManager] SchoolType 映射 SchoolCard 失败: {schoolType}");
                return;
            }
            AddSchoolCount(schoolCardCfg.Id, addCount);
        }
        

        // 获取流派卡最大星级
        public int GetSchoolCardMaxStar() => _schoolCardMaxStar;
        
        // 获取流派卡星级配置
        public static SchoolCardStarListStruct GetSchoolCardStarCfg(int schoolId, int star, SchoolCardTakeType takeType = default)
        {
            var schoolCardCfg = DataConfigManager.Tables.TbSchoolCardConfig.GetOrDefault(schoolId);
            if (schoolCardCfg == null || (takeType != default && schoolCardCfg.TakeType != takeType))
            {
                return null;
            }

            return schoolCardCfg.StarList.FirstOrDefault(v => v.Star == star);
        }
        
        
        /**
         * 设置BUFF（新版：通过 BuffFactory 直接添加）
         */
        public void SetBuffLogic(List<BuffStruct> buffs, bool isAdditive = false)
        {
            if (buffs.Count == 0) return;

            foreach (var v in buffs)
            {
                var buffCfg = DataConfigManager.Tables.TbBuffConfig.Get(v.Type);
                if (buffCfg == null) continue;

                switch (buffCfg.Category)
                {
                    case AttributeCategoryType.Skill:
                        // 通过工厂创建并直接添加到士兵的 BuffController
                        GamePlayManager.Default.TraverseVillageSoldier((soldierCard) =>
                        {
                            var buff = BuffFactory.Create(v.Type, v.Duration, v.Value,
                                caster: null, target: soldierCard);
                            if (buff != null)
                                soldierCard.BuffController.AddBuff(buff);
                        });
                        break;

                    default:
                        LogTool.LogError($"未定义的BUFF类别: {buffCfg.Category}");
                        break;
                }
            }
        }

        public bool CheckSchoolMaxLevel()
        {
            var maxCount = 10;
            return _schoolCardCounts.Values.Any(v => v >= maxCount);
        }

        /**
         * 添加所有流派层数
         */
        public void AddAllSchoolLayers(int addLayers)
        {
            if (addLayers <= 0) return;
            
            foreach (var cfg in _schoolCardConfig.DataList)
            {
                AddSchoolCount(cfg.Id, addLayers);
            }
        }

        /// <summary>
        /// 将当前已选中的流派卡效果补应用到指定英灵（用于战斗中新增英灵）。
        /// </summary>
        public void ApplyExistingSchoolCardsToSoldier(SoldierCard soldier)
        {
            if (soldier == null) return;

            foreach (var schoolCardPair in _schoolCards)
            {
                var schoolCardCfg = _schoolCardConfig.GetOrDefault(schoolCardPair.Key);
                if (schoolCardCfg == null || schoolCardCfg.StarList == null || schoolCardCfg.StarList.Count == 0)
                {
                    continue;
                }

                var schoolCard = schoolCardPair.Value;
                var currentStar = Mathf.Clamp(schoolCard.CardStar, 1, schoolCardCfg.StarList.Count);
                var starCfg = schoolCardCfg.StarList[currentStar - 1];

                // 条件触发型流派：已迁移至 LearnSkillRule，不再补挂 atkCarryings
                if (starCfg.ConditionType != ConditionsType.none)
                {
                    LogTool.LogWarning(
                        $"[SchoolManager] 流派卡 {schoolCardPair.Key} 仍使用 ConditionType 条件触发，请改为 LearnSkillRule: conditionType={starCfg.ConditionType}");
                    continue;
                }

                // 带 Condition 监听的流派效果由全局监听驱动，这里不重复注册监听，避免重复回调。
                if (starCfg.Condition.Count > 0)
                {
                    continue;
                }

                // 立即生效型流派：仅对新增英灵补应用 Soldier/Skill 类属性。
                var isAdditiveAttr = schoolCardCfg.IsAdditiveAttr > 0;
                foreach (var attr in starCfg.Attributes)
                {
                    var attrCfg = DataConfigManager.Tables.TbAttributeConfig.GetOrDefault(attr.Type);
                    if (attrCfg == null) continue;
                    if (attrCfg.Category != AttributeCategoryType.Soldier &&
                        attrCfg.Category != AttributeCategoryType.Skill)
                    {
                        continue;
                    }

                    var attrModifier = new AttributeModifier
                    {
                        SourceKey = $"{schoolCardCfg.Id}",
                        SourceType = AttributeSourceType.SchoolCard
                    };
                    AttributeManager.SetAttributeLogic(attrModifier, new AttributeData(attr), isAdditiveAttr, 1, soldier.cardId);
                }
            }
        }
    }
    
    /**
     * 流派卡类
     */
    public class SchoolCard
    {
        public readonly int CardId;
        public int CardLevel;
        public int CardStar;
        public int EffectNum;// 生效次数

        public SchoolCardConfig SchoolCardCfg;

        public SchoolCard(int cardId, int cardLevel = 1)
        {
            SchoolCardCfg = DataConfigManager.Tables.TbSchoolCardConfig.GetOrDefault(cardId);
            if (SchoolCardCfg == null)
            {
                LogTool.LogError("Get TbSchoolCardConfig does not exist Id: " + cardId);
                return;
            }

            CardId = cardId;
            CardStar = 0;
            CardLevel = cardLevel;
        }
        
    }
    
    
}