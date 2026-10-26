using System;
using System.Collections.Generic;
using Config;
using FusionContinent;
using FusionContinent.Attribute;
using FusionContinent.Skill;
using FusionContinent.Skill.Handler;
using Manager;
using UnityEngine;
using ValueType = Config.ValueType;

namespace Game.Battle
{
    /// <summary>
    /// 链式学习规则来源参数解析：装备词条、流派卡、轮语者武器/皮肤、技能/被动等。
    /// <para>
    /// 配表约定：在学习来源的 Attributes 中，除 LearnSkillRule 外再配置 Category=Add（加成属性）
    /// 或 Category=Skill 的属性作为规则参数；EffectGroup ModifyAttr / Buff *AttrName 通过 SourceAttrType 引用。
    /// </para>
    /// </summary>
    public static class BattleRuleSourceParamSupport
    {
        private const string EquipEntryPrefix = "Equip_";
        private const string EquipEffectPrefix = "EquipEffect_";
        private const string EntryEffectBonusMarker = "_EntryEffectBonus_";
        private const string SpeakerWeaponPrefix = "SpeakerWeapon_";
        private const string SpeakerSkinPrefix = "SpeakerSkin_";
        private const string PassiveSkillPrefix = "PassiveSkill_";

        /// <summary>该来源类型是否支持从学习配置读取规则参数（非技能实例路径）。</summary>
        public static bool UsesConfigSourceParams(AttributeSourceType sourceType)
        {
            return sourceType == AttributeSourceType.Equip
                || sourceType == AttributeSourceType.SchoolCard
                || sourceType == AttributeSourceType.SpeakerSkin
                || sourceType == AttributeSourceType.FusionGem;
        }

        /// <summary>
        /// 属性是否为规则来源参数（Category=Add / Skill，且非 LearnSkillRule 等功能属性）。
        /// 装备词条优先使用加成属性（Add）；技能等级参数仍可用 Skill。
        /// </summary>
        public static bool IsRuleSourceParamAttribute(AttributeType type)
        {
            if (type == AttributeType.LearnSkillRule)
            {
                return false;
            }

            var attrCfg = DataConfigManager.Tables.TbAttributeConfig.GetOrDefault(type);
            if (attrCfg == null)
            {
                return false;
            }

            return attrCfg.Category == AttributeCategoryType.Add
                || attrCfg.Category == AttributeCategoryType.Skill;
        }

        /// <summary>
        /// 从规则上下文解析单条来源参数值；技能/被动优先走技能实例（由调用方处理），此处处理配置型来源。
        /// </summary>
        public static float ResolveParamValue(BattleRuleTriggerContext ruleContext, AttributeType paramType)
        {
            if (ruleContext == null || !IsRuleSourceParamAttribute(paramType))
            {
                return 0f;
            }

            if (!TryResolveAttributeList(ruleContext, out var attributes))
            {
                return 0f;
            }

            return ReadParamValueFromList(attributes, paramType);
        }

        /// <summary>构建来源参数 AttributeSet，供 AddBuff *AttrName 代理技能读取。</summary>
        public static AttributeSet BuildParamAttributeSet(BattleRuleTriggerContext ruleContext)
        {
            if (ruleContext == null || !TryResolveAttributeList(ruleContext, out var attributes))
            {
                return null;
            }

            var paramSet = new AttributeSet();
            var hasParam = false;
            for (int i = 0; i < attributes.Count; i++)
            {
                var attr = attributes[i];
                if (!IsRuleSourceParamAttribute(attr.Type))
                {
                    continue;
                }

                hasParam = true;
                paramSet.GetAttr(attr.Type).SetBaseValue(ResolveAttributeStructValue(attr), true);
            }

            return hasParam ? paramSet : null;
        }

        /// <summary>将来源参数代理技能写入 SkillContext，供 BuffApplier 读取 *AttrName。</summary>
        public static void AttachParamSkillToContext(BattleRuleTriggerContext ruleContext, SkillContext skillContext)
        {
            if (ruleContext == null || skillContext == null)
            {
                return;
            }

            if (BattleRuleSkillSourceSupport.UsesSkillInstanceForValues(ruleContext.ModifierSourceType)
                && BattleRuleSkillSourceSupport.ResolveSourceSkillId(ruleContext) > 0)
            {
                return;
            }

            var paramSet = BuildParamAttributeSet(ruleContext);
            if (paramSet == null)
            {
                return;
            }

            skillContext.SetExtraData(
                SkillContextKeys.RuleSourceParamSkill,
                new BattleRuleSourceParamSkillProxy(paramSet));
        }

        private static bool TryResolveAttributeList(
            BattleRuleTriggerContext ruleContext,
            out List<AttributeStruct> attributes)
        {
            attributes = null;
            var stackSourceKey = ruleContext.StackSourceKey;
            if (string.IsNullOrEmpty(stackSourceKey))
            {
                return false;
            }

            switch (ruleContext.ModifierSourceType)
            {
                case AttributeSourceType.Equip:
                    return TryResolveEquipAttributes(stackSourceKey, out attributes);

                case AttributeSourceType.FusionGem:
                    return TryResolveFusionGemAttributes(stackSourceKey, out attributes);

                case AttributeSourceType.SchoolCard:
                    return TryResolveSchoolCardAttributes(stackSourceKey, out attributes);

                case AttributeSourceType.SpeakerSkin:
                    return TryResolveSpeakerSkinAttributes(stackSourceKey, out attributes);

                case AttributeSourceType.Skill:
                    return TryResolveSkillConfigAttributes(stackSourceKey, ruleContext, out attributes);

                case AttributeSourceType.GeneralSkill:
                    return TryResolvePassiveSkillConfigAttributes(stackSourceKey, ruleContext, out attributes);

                default:
                    return false;
            }
        }

        private static bool TryResolveEquipAttributes(string stackSourceKey, out List<AttributeStruct> attributes)
        {
            attributes = null;
            var normalizedKey = NormalizeEquipEntryStackSourceKey(stackSourceKey);

            if (normalizedKey.StartsWith(EquipEffectPrefix))
            {
                return TryParseEquipEffectStackSourceKey(normalizedKey, out _, out var effectId)
                    && TryGetEquipEffectAttributes(effectId, out attributes);
            }

            if (!FusionContinent.Model.Equip.TryParseEquipEntryStackSourceKey(normalizedKey, out _, out var entryId))
            {
                return false;
            }

            return TryGetEquipEntryAttributes(entryId, out attributes);
        }

        private static string NormalizeEquipEntryStackSourceKey(string stackSourceKey)
        {
            var bonusIndex = stackSourceKey.IndexOf(EntryEffectBonusMarker, StringComparison.Ordinal);
            if (bonusIndex < 0)
            {
                return stackSourceKey;
            }

            return stackSourceKey.Substring(0, bonusIndex);
        }

        private static bool TryParseEquipEffectStackSourceKey(
            string stackSourceKey,
            out int equipInstanceId,
            out int effectId)
        {
            equipInstanceId = 0;
            effectId = 0;
            if (string.IsNullOrEmpty(stackSourceKey) || !stackSourceKey.StartsWith(EquipEffectPrefix))
            {
                return false;
            }

            var prefixLength = EquipEffectPrefix.Length;
            var firstSep = stackSourceKey.IndexOf('_', prefixLength);
            if (firstSep < 0)
            {
                return false;
            }

            var equipPart = stackSourceKey.Substring(prefixLength, firstSep - prefixLength);
            var effectPart = stackSourceKey.Substring(firstSep + 1);
            return int.TryParse(equipPart, out equipInstanceId)
                && int.TryParse(effectPart, out effectId);
        }

        private static bool TryGetEquipEntryAttributes(int entryId, out List<AttributeStruct> attributes)
        {
            attributes = null;
            var entryCfg = DataConfigManager.Tables.TbEquipEntryConfig.GetOrDefault(entryId);
            if (entryCfg?.Attributes == null || entryCfg.Attributes.Count == 0)
            {
                return false;
            }

            attributes = entryCfg.Attributes;
            return true;
        }

        private static bool TryGetEquipEffectAttributes(int effectId, out List<AttributeStruct> attributes)
        {
            attributes = null;
            var effectCfg = DataConfigManager.Tables.TbEquipEffectsConfig.GetOrDefault(effectId);
            if (effectCfg?.Attributes == null || effectCfg.Attributes.Count == 0)
            {
                return false;
            }

            attributes = effectCfg.Attributes;
            return true;
        }

        private static bool TryResolveFusionGemAttributes(string stackSourceKey, out List<AttributeStruct> attributes)
        {
            attributes = null;
            if (!FusionContinent.Model.Equip.TryParseFusionGemStackSourceKey(stackSourceKey, out var gemInstanceId))
            {
                return false;
            }

            if (!Global.PlayerFusionGems.TryGetValue(gemInstanceId, out var gem) || gem == null)
            {
                return false;
            }

            var levelCfg = gem.GetLevelCfg();
            if (levelCfg?.Attributes == null || levelCfg.Attributes.Count == 0)
            {
                return false;
            }

            attributes = levelCfg.Attributes;
            return true;
        }

        private static bool TryResolveSchoolCardAttributes(string stackSourceKey, out List<AttributeStruct> attributes)
        {
            attributes = null;
            if (!int.TryParse(stackSourceKey, out var schoolCardId) || schoolCardId <= 0)
            {
                return false;
            }

            var schoolManager = SchoolManager.Instance;
            if (schoolManager == null)
            {
                return false;
            }

            var schoolCard = schoolManager.GetSchoolCard(schoolCardId);
            var star = Mathf.Clamp(schoolCard.CardStar, 1, int.MaxValue);
            var starCfg = SchoolManager.GetSchoolCardStarCfg(schoolCardId, star);
            if (starCfg?.Attributes == null || starCfg.Attributes.Count == 0)
            {
                return false;
            }

            attributes = starCfg.Attributes;
            return true;
        }

        private static bool TryResolveSpeakerSkinAttributes(string stackSourceKey, out List<AttributeStruct> attributes)
        {
            attributes = null;
            if (TryParseSpeakerModuleStackSourceKey(
                    stackSourceKey,
                    SpeakerWeaponPrefix,
                    out var weaponConfigId,
                    out var weaponLevel,
                    out _))
            {
                return TryGetWeaponLevelAttributes(weaponConfigId, weaponLevel, out attributes);
            }

            if (TryParseSpeakerModuleStackSourceKey(
                    stackSourceKey,
                    SpeakerSkinPrefix,
                    out var skinConfigId,
                    out var skinLevel,
                    out _))
            {
                return TryGetSkinLevelAttributes(skinConfigId, skinLevel, out attributes);
            }

            return false;
        }

        private static bool TryParseSpeakerModuleStackSourceKey(
            string stackSourceKey,
            string prefix,
            out int configId,
            out int level,
            out int ruleId)
        {
            configId = 0;
            level = 0;
            ruleId = 0;
            if (string.IsNullOrEmpty(stackSourceKey) || !stackSourceKey.StartsWith(prefix))
            {
                return false;
            }

            var parts = stackSourceKey.Substring(prefix.Length).Split('_');
            if (parts.Length < 3)
            {
                return false;
            }

            return int.TryParse(parts[0], out configId)
                && int.TryParse(parts[1], out level)
                && int.TryParse(parts[2], out ruleId);
        }

        private static bool TryGetWeaponLevelAttributes(int weaponConfigId, int level, out List<AttributeStruct> attributes)
        {
            attributes = null;
            var cfg = DataConfigManager.Tables.TbPlayerWeaponConfig.GetOrDefault(weaponConfigId);
            if (cfg?.WeaponStarList == null || cfg.WeaponStarList.Count == 0)
            {
                return false;
            }

            var levelCfg = FindWeaponStarLevel(cfg.WeaponStarList, level);
            return MergeSpeakerModuleAttributes(levelCfg?.BaseAttributes, levelCfg?.ExtraAttributes, out attributes);
        }

        private static bool TryGetSkinLevelAttributes(int skinConfigId, int level, out List<AttributeStruct> attributes)
        {
            attributes = null;
            var cfg = DataConfigManager.Tables.TbPlayerFigureConfig.GetOrDefault(skinConfigId);
            if (cfg?.ImageStarList == null || cfg.ImageStarList.Count == 0)
            {
                return false;
            }

            var levelCfg = FindImageStarLevel(cfg.ImageStarList, level);
            return MergeSpeakerModuleAttributes(levelCfg?.BaseAttributes, levelCfg?.ExtraAttributes, out attributes);
        }

        private static WeaponStarListStruct FindWeaponStarLevel(List<WeaponStarListStruct> starList, int level)
        {
            for (int i = 0; i < starList.Count; i++)
            {
                if (starList[i].Level == level)
                {
                    return starList[i];
                }
            }

            return starList[0];
        }

        private static ImageStarListStruct FindImageStarLevel(List<ImageStarListStruct> starList, int level)
        {
            for (int i = 0; i < starList.Count; i++)
            {
                if (starList[i].Level == level)
                {
                    return starList[i];
                }
            }

            return starList[0];
        }

        private static bool MergeSpeakerModuleAttributes(
            List<AttributeStruct> baseAttributes,
            List<AttributeStruct> extraAttributes,
            out List<AttributeStruct> merged)
        {
            merged = new List<AttributeStruct>();
            if (baseAttributes != null)
            {
                merged.AddRange(baseAttributes);
            }

            if (extraAttributes != null)
            {
                merged.AddRange(extraAttributes);
            }

            return merged.Count > 0;
        }

        private static bool TryResolveSkillConfigAttributes(
            string stackSourceKey,
            BattleRuleTriggerContext ruleContext,
            out List<AttributeStruct> attributes)
        {
            attributes = null;
            if (!int.TryParse(stackSourceKey, out var skillId) || skillId <= 0)
            {
                return false;
            }

            var sourceSkill = BattleRuleSkillSourceSupport.ResolveSourceSkill(ruleContext);
            if (sourceSkill != null)
            {
                return false;
            }

            return TryGetSpecialEffectLevelAttributes(skillId, ResolveSkillLevel(ruleContext, skillId), out attributes);
        }

        private static bool TryResolvePassiveSkillConfigAttributes(
            string stackSourceKey,
            BattleRuleTriggerContext ruleContext,
            out List<AttributeStruct> attributes)
        {
            attributes = null;
            if (!stackSourceKey.StartsWith(PassiveSkillPrefix))
            {
                return false;
            }

            var skillIdText = stackSourceKey.Substring(PassiveSkillPrefix.Length);
            if (!int.TryParse(skillIdText, out var passiveSkillId) || passiveSkillId <= 0)
            {
                return false;
            }

            var sourceSkill = BattleRuleSkillSourceSupport.ResolveSourceSkill(ruleContext);
            if (sourceSkill != null)
            {
                return false;
            }

            return TryGetSpecialEffectLevelAttributes(
                passiveSkillId,
                ResolvePassiveSkillLevel(ruleContext, passiveSkillId),
                out attributes);
        }

        private static int ResolveSkillLevel(BattleRuleTriggerContext ruleContext, int skillId)
        {
            var soldier = BattleRuleSkillSourceSupport.ResolveSourceSoldier(ruleContext, skillId);
            var skill = soldier != null ? soldier.GetSkillById(skillId) : null;
            if (skill != null)
            {
                return skill.Level;
            }

            var card = Global.GetCardById(skillId);
            return card != null && card.Lv > 0 ? card.Lv : 1;
        }

        private static int ResolvePassiveSkillLevel(BattleRuleTriggerContext ruleContext, int passiveSkillId)
        {
            var specialCfg = DataConfigManager.Tables.TbSpecialEffectsConfig.GetOrDefault(passiveSkillId);
            if (specialCfg == null)
            {
                return 1;
            }

            if (ruleContext.ScopeHeroCardId > 0)
            {
                var gamePlay = GamePlayManager.Default;
                if (gamePlay != null)
                {
                    for (int i = 0; i < gamePlay.villageGameCards.Count; i++)
                    {
                        var hero = gamePlay.villageGameCards[i];
                        if (hero != null && hero.cardId == ruleContext.ScopeHeroCardId)
                        {
                            var passiveSkill = hero.GetSkillById(passiveSkillId);
                            if (passiveSkill != null)
                            {
                                return passiveSkill.Level;
                            }
                        }
                    }
                }
            }

            if (specialCfg.ActsOnCard > 0)
            {
                var card = Global.GetCardById(specialCfg.ActsOnCard);
                if (card != null && card.Lv > 0)
                {
                    return card.Lv;
                }
            }

            return 1;
        }

        private static bool TryGetSpecialEffectLevelAttributes(
            int skillId,
            int level,
            out List<AttributeStruct> attributes)
        {
            attributes = null;
            var specialCfg = DataConfigManager.Tables.TbSpecialEffectsConfig.GetOrDefault(skillId);
            if (specialCfg?.LevelPropertyList == null || specialCfg.LevelPropertyList.Count == 0)
            {
                return false;
            }

            SpecialLevelPropertyList levelCfg = null;
            for (int i = 0; i < specialCfg.LevelPropertyList.Count; i++)
            {
                if (specialCfg.LevelPropertyList[i].Level == level)
                {
                    levelCfg = specialCfg.LevelPropertyList[i];
                    break;
                }
            }

            if (levelCfg == null)
            {
                var index = Mathf.Clamp(level - 1, 0, specialCfg.LevelPropertyList.Count - 1);
                levelCfg = specialCfg.LevelPropertyList[index];
            }

            if (levelCfg?.Attributes == null || levelCfg.Attributes.Count == 0)
            {
                return false;
            }

            attributes = levelCfg.Attributes;
            return true;
        }

        private static float ReadParamValueFromList(List<AttributeStruct> attributes, AttributeType paramType)
        {
            for (int i = 0; i < attributes.Count; i++)
            {
                var attr = attributes[i];
                if (attr.Type == paramType)
                {
                    return ResolveAttributeStructValue(attr);
                }
            }

            return 0f;
        }

        private static float ResolveAttributeStructValue(AttributeStruct attr)
        {
            return attr.ValueType == ValueType.Percentage ? attr.Value : attr.Value;
        }
    }
}
