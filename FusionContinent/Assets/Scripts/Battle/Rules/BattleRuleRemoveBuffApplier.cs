using Config;
using FusionContinent;
using FusionContinent.Buff;
using System.Collections.Generic;

namespace Game.Battle
{
    /// <summary>
    /// EffectGroupsType.RemoveBuff 执行器。
    /// 仅当目标英灵身上存在对应 BuffId 时才移除。
    /// </summary>
    public static class BattleRuleRemoveBuffApplier
    {
        /// <summary>旧版 AttributeStruct 入口（RemoveBuff 类型，兼容保留）。</summary>
        public static bool TryApply(AttributeStruct attr, BattleRuleTriggerContext ruleContext)
        {
            if (attr.Type != AttributeType.RemoveBuff)
            {
                return false;
            }

            return ApplyRemoveBuff(BattleRuleEffectTargets.ResolveRemoveBuffId(attr), ruleContext);
        }

        /// <summary>
        /// EffectParamStruct 入口。
        /// BaseValue = SkillBuffTemplateLibrary.BuffId；EffectType 必须为 RemoveBuff。
        /// </summary>
        public static bool TryApply(EffectParamStruct effect, BattleRuleTriggerContext ruleContext)
        {
            if (effect.EffectType != EffectGroupsType.RemoveBuff)
            {
                return false;
            }

            return ApplyRemoveBuff(BattleRuleEffectTargets.ResolveRemoveBuffId(effect), ruleContext);
        }

        private static bool ApplyRemoveBuff(int buffId, BattleRuleTriggerContext ruleContext)
        {
            if (buffId <= 0)
            {
                LogTool.LogError($"[BattleRuleRemoveBuffApplier] 无效的 Buff Id: {buffId}");
                return true;
            }

            var targets = BattleRuleEffectTargets.ResolveTargetSoldiers(ruleContext);
            if (targets.Count == 0)
            {
                LogTool.LogWarning("[BattleRuleRemoveBuffApplier] 未找到可移除 Buff 的英灵");
                return true;
            }

            for (int i = 0; i < targets.Count; i++)
            {
                TryRemoveBuffFromHero(targets[i], buffId);
            }

            return true;
        }

        /// <summary>从单个英灵移除所有匹配 buffId 的 Buff 实例。</summary>
        private static void TryRemoveBuffFromHero(SoldierCard hero, int buffId)
        {
            if (hero == null || hero.isDie)
            {
                return;
            }

            var buffController = hero.BuffController;
            if (buffController == null)
            {
                return;
            }

            var allBuffs = buffController.GetAllBuffs();
            var removedCount = 0;

            for (int i = allBuffs.Count - 1; i >= 0; i--)
            {
                var buff = allBuffs[i];
                if (buff == null || buff.BuffId != buffId)
                {
                    continue;
                }

                if (buffController.RemoveBuff(buff))
                {
                    removedCount++;
                }
            }

            if (removedCount > 0)
            {
                LogTool.Log(
                    $"[BattleRuleRemoveBuffApplier] 已移除 Buff {buffId} x{removedCount}, hero={hero.cardId}, hp={hero.CurrentHp}/{hero.MaxHp}");
            }
            else
            {
                var activeBuffIds = new System.Text.StringBuilder();
                for (int i = 0; i < allBuffs.Count; i++)
                {
                    var activeBuff = allBuffs[i];
                    if (activeBuff == null)
                    {
                        continue;
                    }

                    if (activeBuffIds.Length > 0)
                    {
                        activeBuffIds.Append(',');
                    }

                    activeBuffIds.Append(activeBuff.BuffId);
                }

                LogTool.Log(
                    $"[BattleRuleRemoveBuffApplier] 未找到可移除 Buff {buffId}, hero={hero.cardId}, hp={hero.CurrentHp}/{hero.MaxHp}, activeBuffIds=[{activeBuffIds}]");
            }
        }
    }
}
