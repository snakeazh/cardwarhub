using Config;
using UIFramework.Services;
using Cysharp.Threading.Tasks;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Game.CombatPower;
using FusionContinent;
using FusionContinent.Model;
using Newtonsoft.Json;
using Cardwar;
using FusionContinent.Utils;
using PlayerCard = FusionContinent.Model.PlayerCard;

namespace Game.Soldier
{
    /// <summary>
    /// 士兵服务 - 管理士兵数据和升级逻辑
    /// </summary>
    public class SoldierService : GameServiceBase
    {
        #region 属性
        public bool IsInitialized { get; private set; }
        #endregion

        #region 配置数据
        private TbCardBaseConfig mCardBaseConfig;
        private TbCardDevelopConfig mCardDevelopConfig;
        private TbItemConfig mItemConfig;
        private TbSpecialEffectsConfig mSpecialEffectsConfig;
        private readonly Dictionary<int, List<SpecialEffectsConfig>> mSoldierSkillIds = new Dictionary<int, List<SpecialEffectsConfig>>();
        #endregion

        public override async UniTask InitializeAsync()
        {
            await base.InitializeAsync();
            
            mCardBaseConfig = DataConfigManager.Tables.TbCardBaseConfig;
            mCardDevelopConfig = DataConfigManager.Tables.TbCardDevelopConfig;
            mItemConfig = DataConfigManager.Tables.TbItemConfig;
            mSpecialEffectsConfig = DataConfigManager.Tables.TbSpecialEffectsConfig;

            BuildSoldierSkillCache();
            
            IsInitialized = true;
            LogTool.Log($"[SoldierService] 初始化完成，已缓存 {mSoldierSkillIds.Count} 个角色的技能映射");
        }

        private void BuildSoldierSkillCache()
        {
            mSoldierSkillIds.Clear();

            foreach (var cfg in mCardBaseConfig.DataList)
            {
                if (cfg.Type != CardType.Skill && cfg.Type != CardType.SkillPassive)
                {
                    continue;
                }

                var skillCfg = mSpecialEffectsConfig.GetOrDefault(cfg.Id);
                if (skillCfg == null || skillCfg.ActsOnCard <= 0)
                {
                    continue;
                }

                if (!mSoldierSkillIds.TryGetValue(skillCfg.ActsOnCard, out var skillConfigs))
                {
                    skillConfigs = new List<SpecialEffectsConfig>();
                    mSoldierSkillIds[skillCfg.ActsOnCard] = skillConfigs;
                }

                skillConfigs.Add(skillCfg);
            }

            foreach (var skillConfigs in mSoldierSkillIds.Values)
            {
                skillConfigs.Sort(CompareSkillPriority);
            }
        }

        public PlayerCard GetSoldierSkillData(int skillId)
        {
            if (Global.PlayerCards.TryGetValue(skillId, out var skillData))
            {
                return skillData;
            }

            return null;
        }
        
        public SkinConfig GetSoldierSkinConfig(int soldierId)
        {
            List<SkinConfig> soldierConfigs = DataConfigManager.Tables.TbSkinConfig.DataList.FindAll(ele => ele.ActsOnCard == soldierId);
            int skinId = 0;
            if (Global.PlayerCards.TryGetValue(soldierId,out var playerCard))
            {
                if (playerCard.ObjData.Skin > 0)
                {
                    skinId = playerCard.ObjData.Skin;
                }
            }
            else
            {
                return null;
            }
            foreach (var skinConfig in soldierConfigs)
            {
                if (skinId == 0 && skinConfig.UnlockItemId == 0)
                {
                    return skinConfig;
                }
                else if (skinId != 0 && skinId == skinConfig.Id)
                {
                    return skinConfig;
                }
            }

            return null;
        }

        public SpecialEffectsConfig GetSoldierSkillConfig(int skillId,SoldierSkillSortType sortType)
        {
            if (sortType == SoldierSkillSortType.PassiveSkills)
            {
                return GetSkillConfigsBySoldierId(skillId)[0];
            }
            else if (sortType == SoldierSkillSortType.AwakingSkills)
            {
                return GetSkillConfigsBySoldierId(skillId).Last();
            }
            else
            {
                return GetSkillConfigsBySoldierId(skillId)[(int)sortType];

            }
        }

        private int CompareSkillPriority(SpecialEffectsConfig left, SpecialEffectsConfig right)
        {
            int leftPriority = GetSkillPriority(left);
            int rightPriority = GetSkillPriority(right);

            int compareResult = leftPriority.CompareTo(rightPriority);
            if (compareResult != 0)
            {
                return compareResult;
            }

            return left.Id.CompareTo(right.Id);
        }

        private int GetSkillPriority(SpecialEffectsConfig skillConfig)
        {
            var cardConfig = mCardBaseConfig.GetOrDefault(skillConfig.Id);
            if (cardConfig == null)
            {
                return int.MaxValue;
            }

            if (cardConfig.Type == CardType.SkillPassive)
            {
                return 0;
            }

            if (skillConfig.AwakingSkill > 0)
            {
                return 2;
            }

            return 1;
        }

        public List<SpecialEffectsConfig> GetSkillConfigsBySoldierId(int soldierId)
        {
            if (mSoldierSkillIds.TryGetValue(soldierId, out var skillConfigs))
            {
                return new List<SpecialEffectsConfig>(skillConfigs);
            }

            return new List<SpecialEffectsConfig>();
        }

        public List<PlayerCard> GetAllSkillsBySoldierId(int soldierId,bool checkHave =true)
        {
            var result = new List<PlayerCard>();
            if (!mSoldierSkillIds.TryGetValue(soldierId, out var skillConfigs))
            {
                return result;
            }

            foreach (var skillCfg in skillConfigs)
            {
                var cfg = mCardBaseConfig.GetOrDefault(skillCfg.Id);
                if (cfg == null)
                {
                    continue;
                }

                var cardInfo = Global.GetCardById(skillCfg.Id, new PlayerCard()
                {
                    Lv = 1,
                    IsHave = false,
                    Config = cfg,
                    CardId = skillCfg.Id,
                    ObjData = new CardDataStruct(),
                });
                if (checkHave)
                {
                    if (cardInfo.IsHave == false)
                    {
                        continue;
                    }
                }
                result.Add(cardInfo);
            }

            return result;
        }

        #region 升级方法

        /// <summary>
        /// 升级士兵
        /// </summary>
        public async UniTask<UpgradeResult> UpgradeSoldierAsync(int cardId, Dictionary<int, int> consumeItems)
        {
            try
            {
                var cardInfo = Global.GetCardById(cardId);
                if (cardInfo == null)
                {
                    return new UpgradeResult { Success = false, ErrorMessage = "英雄信息未初始化" };
                }

                CombatPowerChangeHelper.CaptureBaseline();

                var request = new LevelUpCardRequest { CardId = cardId, ConsumeItems = JsonConvert.SerializeObject(consumeItems) };
                var proto = await WebDataFun.SendProtoPostRequest<LevelUpCardResponse>(ApiKeys.ApiCardLevelUp, request);
                if (proto != null && proto.Card != null)
                {
                    var localCard = ProtoMappers.MapPlayerCard(proto.Card);
                    Global.PlayerCards[localCard.CardId] = localCard;

                    // 同步奖励
                    Items.Update(proto.Items);

                    CombatPowerChangeHelper.RecalculateAndShowTipIfIncreased();

                    Global.Publisher.Publish(MessagePublisher.CARD_UPGRADED, cardId);

                    // 升级到觉醒档（如40级）且尚未觉醒时，主动提示玩家可觉醒
                    if (!localCard.ObjData.IsAwaked
                        && CanAwakening(localCard, out var awakenLv)
                        && localCard.Lv == awakenLv)
                    {
                        Toast.Show("已达觉醒条件，可前往觉醒");
                    }

                    return new UpgradeResult
                    {
                        Success = true,
                        CardId = cardId,
                        NewLevel = localCard.Lv,
                        NewExp = localCard.Exp
                    };
                }
                
                return new UpgradeResult { Success = false, ErrorMessage = "升级失败" };
            }
            catch (System.Exception ex)
            {
                LogTool.LogError($"[SoldierService] 升级失败：{ex.Message} { ex.StackTrace}");
                return new UpgradeResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        #endregion

        #region 觉醒方法

        /// <summary>
        /// 觉醒士兵
        /// </summary>
        public async UniTask<AwakeningResult> AwakeningSoldierAsync(int cardId, int cardLv)
        {
            try
            {
                CombatPowerChangeHelper.CaptureBaseline();

                var request = new LevelUpCardRequest { CardId = cardId, CardLv = cardLv + 1 };
                var proto = await WebDataFun.SendProtoPostRequest<LevelUpCardResponse>(ApiKeys.ApiCardLevelUp, request);
                if (proto != null && proto.Card != null)
                {
                    var localCard = ProtoMappers.MapPlayerCard(proto.Card);
                    Global.PlayerCards[localCard.CardId] = localCard;

                    // 同步奖励
                    Items.Update(proto.Items);

                    CombatPowerChangeHelper.RecalculateAndShowTipIfIncreased();
                    Global.Publisher.Publish(MessagePublisher.CHANGE_CARD_CHANGE, cardId);
                    return new AwakeningResult
                    {
                        Success = true,
                        CardId = cardId,
                        NewLevel = localCard.Lv,
                        SkinId = Global.PlayerCards[localCard.CardId].ObjData.Skin
                    };
                }

                return new AwakeningResult { Success = false, ErrorMessage = "觉醒失败" };
            }
            catch (System.Exception ex)
            {
                LogTool.LogError($"[SoldierService] 觉醒失败：{ex.Message}");
                return new AwakeningResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        #endregion

        #region 养成奖励方法

        /// <summary>
        /// 领取养成奖励
        /// </summary>
        public async UniTask<ReceiveRewardResult> ReceiveDevelopRewardAsync(int cardId, int cardLv)
        {
            try
            {
                var request = new ReceiveDevelopRewardRequest { CardId = cardId, CardLv = cardLv };
                var proto = await WebDataFun.SendProtoPostRequest<ReceiveDevelopRewardResponse>(ApiKeys.ApiCardReceiveDevelopReward, request);
                if (proto != null && proto.Card != null)
                {
                    var localCard = ProtoMappers.MapPlayerCard(proto.Card);
                    Global.PlayerCards[localCard.CardId] = localCard;

                    // 同步奖励
                    Items.Update(proto.Items);

                    return new ReceiveRewardResult
                    {
                        Success = true,
                        CardId = cardId,
                        Level = cardLv
                    };
                }

                return new ReceiveRewardResult { Success = false, ErrorMessage = "领取奖励失败" };
            }
            catch (System.Exception ex)
            {
                LogTool.LogError($"[SoldierService] 领取奖励失败：{ex.Message}");
                return new ReceiveRewardResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        #endregion

        #region 英灵激活

        /// <summary>
        /// 英灵激活（消耗碎片解锁士兵卡）
        /// </summary>
        public async UniTask<bool> ActivateSoldierCardAsync(int soldierCardId)
        {
            try
            {
                var cardInfo = Global.GetCardById(soldierCardId);
                if (cardInfo != null && cardInfo.IsHave)
                {
                    Toast.Show("已激活该英灵");
                    return false;
                }

                var request = new SoliderCardActiveRequest { SoldierCardId = soldierCardId };
                var proto = await WebDataFun.SendProtoPostRequest<SoliderCardActiveResponse>(
                    ApiKeys.ApiCardSoliderCardActive, request);
                if (proto == null)
                {
                    Toast.Show("激活失败");
                    return false;
                }

                if (proto.Items != null)
                {
                    Items.Update(proto.Items);
                }

                return true;
            }
            catch (System.Exception ex)
            {
                LogTool.LogError($"[SoldierService] 英灵激活失败：{ex.Message}");
                Toast.Show(ex.Message);
                return false;
            }
        }

        #endregion

        #region 英灵技能卡解锁

        /// <summary>
        /// 英灵解锁技能卡（消耗材料解锁指定技能卡）
        /// </summary>
        public async UniTask<bool> UnlockSkillCardAsync(int skillCardId)
        {
            try
            {
                var request = new UnlockSkillCardRequest { SkillCardId = skillCardId };
                var proto = await WebDataFun.SendProtoPostRequest<UnlockSkillCardResponse>(
                    ApiKeys.ApiCardUnlockSkill, request);
                if (proto == null)
                {
                    Toast.Show("解锁失败");
                    return false;
                }

                if (proto.Items != null)
                {
                    Items.Update(proto.Items);
                }

                return true;
            }
            catch (System.Exception ex)
            {
                LogTool.LogError($"[SoldierService] 英灵技能卡解锁失败：{ex.Message}");
                Toast.Show(ex.Message);
                return false;
            }
        }

        #endregion

        #region 奥术卡分解合成

        /// <summary>
        /// 分解奥术卡
        /// </summary>
        public async UniTask<DecompositionCardResult> DecompositionGeneralSkillCard(List<int> cardIds)
        {
            try
            {
                var request = new AsCardFragmentRequest();
                request.CardIds.AddRange(cardIds);

                var proto = await WebDataFun.SendProtoPostRequest<AsCardFragmentResponse>(ApiKeys.ApiCardDecompositionCard, request);
                if (proto != null)
                {
                    // 同步奖励
                    Items.Update(proto.Items);

                    var rewardsDict = new Dictionary<string, int>();
                    if (proto.Rewards != null)
                    {
                        foreach (var kv in proto.Rewards)
                        {
                            rewardsDict[kv.Key.ToString()] = (int)kv.Value;
                        }
                    }

                    return new DecompositionCardResult { Success = true, Rewards = rewardsDict, ErrorMessage = "分解奥术卡成功" };
                }

                return new DecompositionCardResult { Success = false, ErrorMessage = "分解奥术卡失败" };
            }
            catch (System.Exception ex)
            {
                LogTool.LogError($"[SoldierService] 分解奥术卡失败：{ex.Message}");
                return new DecompositionCardResult { Success = false, ErrorMessage = ex.Message };
            }
        }
        
        /// <summary>
        /// 合成奥术卡
        /// </summary>
        public async UniTask<CompositionCardResult> CompositionGeneralSkillCard(QualityType  qualityType)
        {
            try
            {
                var request = new AsCardFusionRequest { Quality = (int)qualityType };
                var proto = await WebDataFun.SendProtoPostRequest<AsCardFusionResponse>(ApiKeys.ApiCardCompositionCard, request);
                if (proto != null)
                {
                    // 同步奖励
                    Items.Update(proto.Items);
                    return new CompositionCardResult { Success = true, ErrorMessage = "合成奥术卡成功", CardId = proto.CardId };
                }

                return new CompositionCardResult { Success = false, ErrorMessage = "合成奥术卡失败" };
            }
            catch (System.Exception ex)
            {
                LogTool.LogError($"[SoldierService] 合成奥术卡失败：{ex.Message}");
                Toast.Show($"合成奥术卡失败：{ex.Message}");
                return new CompositionCardResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        #endregion

        #region 检查方法

        /// <summary>
        /// 检查卡牌消耗红点
        /// </summary>
        public bool CheckCardConsumeRedDot(PlayerCard card)
        {
            var devCfg = mCardDevelopConfig.GetOrDefault(card.CardId);
            if (devCfg == null || !card.IsHave || card.Lv >= devCfg.LevelPropertyList.Count)
            {
                return false;
            }

            var devLvCfg = devCfg.LevelPropertyList[card.Lv - 1];
            return Items.CheckEnough(devLvCfg.ConsumeItems);
        }

        /// <summary>
        /// 获取卡牌进度数据
        /// </summary>
        public long GetCardProgress(PlayerCard card)
        {
            var devCfg = mCardDevelopConfig.GetOrDefault(card.CardId);
            if (devCfg == null) return 0;

            var devLvCfg = devCfg.LevelPropertyList[card.Lv - 1];
            
            if (devLvCfg.ConsumeItems.Count > 0)
            {
                foreach (var v in devLvCfg.ConsumeItems)
                {
                    var itemCfg = mItemConfig.GetOrDefault(v.Id);
                    if (itemCfg.Type is ItemType.AttributeCardFragment or ItemType.SkillCardFragment)
                    {
                        return Global.GetItemNumById(v.Id);
                    }
                }
            }
            
            return -1; // 最大
        }

        /// <summary>
        /// 检查是否可以觉醒
        /// </summary>
        public bool CanAwakening(PlayerCard card, out int awakeningLevel)
        {
            awakeningLevel = 0;
            // 已觉醒的卡不可再觉醒（避免已觉醒卡的 canAwakening 恒为 true，导致升级按钮被错误隐藏）
            if (card.ObjData == null || card.ObjData.IsAwaked) return false;

            var devCfg = mCardDevelopConfig.GetOrDefault(card.CardId);
            if (devCfg == null) return false;

            var levelPropertyList = devCfg.LevelPropertyList;
            // 觉醒档以 AwakeningConsume 是否配置为准（与 AwakeningPanelViewModel 判定及配表一致）
            var hasAwakening = levelPropertyList?
                .Where(x => x.Level >= card.Lv)
                .OrderBy(x => x.Level)
                .FirstOrDefault(x => x.AwakeningConsume.Any()) != null;

            if (hasAwakening)
            {
                var awakeningLevelConfig = levelPropertyList
                    .Where(x => x.Level >= card.Lv)
                    .OrderBy(x => x.Level)
                    .First(x => x.AwakeningConsume.Any());
                awakeningLevel = awakeningLevelConfig.Level;
            }

            return card.Lv >= awakeningLevel;
        }

        /// <summary>
        /// 检查觉醒材料是否足够（持有量 ≥ 觉醒档配置的 AwakeningConsume）
        /// </summary>
        public bool HasEnoughAwakeningMaterials(PlayerCard card)
        {
            var devCfg = mCardDevelopConfig.GetOrDefault(card.CardId);
            if (devCfg == null) return false;

            var levelPropertyList = devCfg.LevelPropertyList;
            // 取 >= 当前等级 中等级最小的觉醒档（与 CanAwakening 判定一致）
            var awakeningLevelConfig = levelPropertyList?
                .Where(x => x.Level >= card.Lv)
                .OrderBy(x => x.Level)
                .FirstOrDefault(x => x.AwakeningConsume.Any());

            if (awakeningLevelConfig == null || awakeningLevelConfig.AwakeningConsume.Count == 0)
            {
                return false;
            }

            return Items.CheckEnough(awakeningLevelConfig.AwakeningConsume);
        }

        #endregion
    }

    #region 数据类

    public class UpgradeResult
    {
        public bool Success;
        public string ErrorMessage;
        public int CardId;
        public int NewLevel;
        public int NewExp;
    }

    public class AwakeningResult
    {
        public bool Success;
        public string ErrorMessage;
        public int CardId;
        public int NewLevel;
        public int SkinId;
    }

    public class ReceiveRewardResult
    {
        public bool Success;
        public string ErrorMessage;
        public int CardId;
        public int Level;
    }
    public class DecompositionCardResult
    {
        public bool Success;
        public string ErrorMessage;
        public Dictionary<string, int> Rewards { get; set; }
    }
    
    public class CompositionCardResult
    {
        public bool Success;
        public string ErrorMessage;
        public int CardId;
    }
    #endregion
}
