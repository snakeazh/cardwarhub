using Config;
using FusionContinent;
using FusionContinent.Skill.Config;
using Manager;
using System.Collections.Generic;

namespace Game.Battle
{
    /// <summary>
    /// 战斗规则效果的目标与参数解析。
    /// 负责 Buff Id 解析、受击/施法英灵选取等。
    /// </summary>
    public static class BattleRuleEffectTargets
    {
        /// <summary>从旧版 AttributeStruct 解析 Buff Id（兼容保留）。</summary>
        public static int ResolveBuffId(AttributeStruct attr)
        {
            return ResolveBuffId((int)attr.Value);
        }

        /// <summary>
        /// 从 EffectParamStruct 解析 Buff Id。
        /// AddBuff / RemoveBuff 的 BaseValue 即为 SkillBuffTemplateLibrary.BuffId。
        /// </summary>
        public static int ResolveBuffId(EffectParamStruct effect)
        {
            return ResolveBuffId((int)effect.BaseValue);
        }

        /// <summary>从旧版 AttributeStruct 解析要移除的 Buff Id（兼容保留）。</summary>
        public static int ResolveRemoveBuffId(AttributeStruct attr)
        {
            return ResolveRemoveBuffId((int)attr.Value);
        }

        /// <summary>
        /// 从 EffectParamStruct 解析要移除的 Buff Id。
        /// RemoveBuff 的 BaseValue 直接为 SkillBuffTemplateLibrary.BuffId（与运行时 buff.BuffId 一致）。
        /// </summary>
        public static int ResolveRemoveBuffId(EffectParamStruct effect)
        {
            return ResolveRemoveBuffId((int)effect.BaseValue);
        }

        /// <summary>
        /// 解析要移除的 Buff Id：直接使用 BaseValue，不查 EffectsConfig，也不校验模板是否存在。
        /// </summary>
        public static int ResolveRemoveBuffId(int rawValue)
        {
            if (rawValue <= 0)
            {
                LogTool.LogError($"[BattleRuleEffectTargets] 无效的 RemoveBuff Id: {rawValue}");
            }

            return rawValue;
        }

        /// <summary>
        /// 解析 Buff Id：BaseValue 直接对应 SkillBuffTemplateLibrary 模板 Id，不经过 EffectsConfig。
        /// </summary>
        public static int ResolveBuffId(int rawValue)
        {
            if (rawValue <= 0)
            {
                return rawValue;
            }

            var effects = SkillBuffTemplateProvider.GetEffects(rawValue);
            if (effects.Count == 0)
            {
                LogTool.LogError(
                    $"[BattleRuleEffectTargets] SkillBuffTemplateLibrary 中未找到 Buff 模板: buffId={rawValue}");
                return 0;
            }

            return rawValue;
        }

        /// <summary>
        /// 解析效果作用的英灵列表，优先级：
        /// 1. ruleContext.Source（HpChange 等指定来源）
        /// 2. DamageContext.Soldier（命中后规则）
        /// 3. 全场存活英灵（NoCondition 获得规则）
        /// </summary>
        public static List<SoldierCard> ResolveTargetSoldiers(BattleRuleTriggerContext ruleContext)
        {
            var targets = new List<SoldierCard>();

            if (ruleContext?.Source is SoldierCard sourceSoldier && !sourceSoldier.isDie)
            {
                targets.Add(sourceSoldier);
                return targets;
            }

            var damageSoldier = ruleContext?.DamageContext?.Soldier;
            if (damageSoldier != null && !damageSoldier.isDie)
            {
                targets.Add(damageSoldier);
                return targets;
            }

            var gamePlay = GamePlayManager.Default;
            if (gamePlay == null)
            {
                return targets;
            }

            gamePlay.TraverseVillageSoldier(hero =>
            {
                if (!hero.isDie)
                {
                    targets.Add(hero);
                }
            });

            return targets;
        }
    }
}
