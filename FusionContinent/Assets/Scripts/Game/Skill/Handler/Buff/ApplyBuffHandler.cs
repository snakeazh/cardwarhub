using Config;

using FusionContinent.Attribute;

using FusionContinent.Buff;

using FusionContinent.Skill.Config;

using System.Collections.Generic;

using UnityEngine;
using Unity.Profiling;



namespace FusionContinent.Skill.Handler

{

    /// <summary>

    /// 施加Buff处理器

    /// </summary>

    public class ApplyBuffHandler : SyncSkillNodeHandler

    {

        public override string NodeType => "ApplyBuff";



        public override void ExecuteSync(SkillNodeConfig node, SkillContext context)

        {

            using (BattleSkillProfilerMarkers.Begin(BattleSkillProfilerMarkers.BuffTrigger))
            {

            if (context != null && context.SkipBuffOnCast)

            {

                return;

            }



            var buffId = node.GetParamInt("buff_id", 0);

            var defaultTarget = node.GetParamString("target", "Target");

            var effectConfigs = SkillBuffTemplateProvider.GetEffects(buffId);

            if (effectConfigs.Count == 0)

            {

                LogTool.LogWarning($"[ApplyBuffHandler] 未找到 Buff 模板: buffId={buffId}");

                return;

            }

            BuffApplier.RegisterHitExecuteBuff(context, buffId);



            if (TryApplyMoonlightShieldBundle(node, buffId, defaultTarget, context, effectConfigs))

            {

                return;

            }



            foreach (var effectConfig in effectConfigs)

            {

                List<IBuffable> effectTargets;
                using (BattleSkillProfilerMarkers.Begin(BattleSkillProfilerMarkers.BuffTriggerResolveTargets))
                {
                    effectTargets = SkillTargetResolver.Resolve(defaultTarget, context);
                }

                if (effectTargets.Count == 0) continue;



                using (BattleSkillProfilerMarkers.Begin(BattleSkillProfilerMarkers.BuffTriggerTriggerCondition))
                {
                    if (Random.value > Mathf.Clamp01(effectConfig.Probability)) continue;
                }



                foreach (var buffTarget in effectTargets)

                {

                    if (buffTarget == null) continue;
                    if (buffTarget is MonsterCard monster && !SkillHitTargetGuard.CanApplyBuffToMonster(monster, context))
                        continue;

                    using (BattleSkillProfilerMarkers.Begin(BattleSkillProfilerMarkers.BuffTriggerApplyBuff))
                    {
                        BuffApplier.ApplyBuff(buffId, context, buffTarget, effectConfig.Stack);
                    }

                }

            }

            }

        }



        /// <summary>

        /// 月影庇护：护盾层数 + 魔法强度加成合并为 MoonlightShieldBuff，按目标逐个施加。

        /// </summary>

        private bool TryApplyMoonlightShieldBundle(

            SkillNodeConfig node,

            int buffId,

            string defaultTarget,

            SkillContext context,

            List<SkillBuffEffectData> effectConfigs)

        {
            using (BattleSkillProfilerMarkers.Begin(BattleSkillProfilerMarkers.BuffTriggerMoonlightBundle))
            {
            return TryApplyMoonlightShieldBundleInternal(node, buffId, defaultTarget, context, effectConfigs);
            }
        }

        private bool TryApplyMoonlightShieldBundleInternal(

            SkillNodeConfig node,

            int buffId,

            string defaultTarget,

            SkillContext context,

            List<SkillBuffEffectData> effectConfigs)

        {

            SkillBuffEffectData moonlightConfig = null;

            SkillBuffEffectData spellPowerConfig = null;



            foreach (var effectConfig in effectConfigs)

            {

                if ((BuffType)effectConfig.BuffTypeId == BuffType.MoonlightShield)

                {

                    moonlightConfig = effectConfig;

                }

                else if (!string.IsNullOrEmpty(effectConfig.AttrTypeName))

                {

                    spellPowerConfig = effectConfig;

                }

            }



            if (moonlightConfig == null)

            {

                return false;

            }



            List<IBuffable> effectTargets;
            using (BattleSkillProfilerMarkers.Begin(BattleSkillProfilerMarkers.BuffTriggerResolveTargets))
            {
                effectTargets = SkillTargetResolver.Resolve(defaultTarget, context);
            }

            if (effectTargets.Count == 0)

            {

                return true;

            }



            if (Random.value > Mathf.Clamp01(moonlightConfig.Probability))

            {

                return true;

            }



            var layerFromNode = node.GetParamInt("effect_value", 0);

            var shieldLayers = layerFromNode > 0

                ? layerFromNode

                : Mathf.Max(1, Mathf.RoundToInt(moonlightConfig.EffectValue));



            var shieldVfx = node.GetParamString("shield_vfx", moonlightConfig.ShieldVfx);

            if (string.IsNullOrEmpty(shieldVfx))

            {

                shieldVfx = "EliaSkillMoonshadowWard01";

            }



            var breakVfx = node.GetParamString("break_vfx", moonlightConfig.BreakVfx);

            if (string.IsNullOrEmpty(breakVfx))

            {

                breakVfx = "EliaSkillMoonshadowWard02";

            }



            foreach (var buffTarget in effectTargets)

            {

                if (buffTarget is not SoldierCard soldier)

                {

                    continue;

                }



                RemoveStaleMoonlightBuffs(soldier, buffId);



                var spellPowerBonus = spellPowerConfig != null

                    ? BuffApplier.ResolveEffectFinalValue(spellPowerConfig, context, soldier)

                    : 0f;



                var skillCard = BuffApplier.GetSkillCard(context);
                var treatmentDamageType = BuffApplier.ResolveDamageType(
                    skillCard, moonlightConfig.DamageTypeAttrName, moonlightConfig.DamageType);

                var existing = FindMoonlightShield(soldier);

                if (existing != null)

                {

                    existing.AddLayers(shieldLayers, spellPowerBonus, soldier);
                    BuffApplier.NotifyTreatmentApplied(context, soldier, treatmentDamageType);
                    continue;

                }



                var buff = BuffFactory.CreateMoonlightShield(

                    moonlightConfig.Duration,

                    shieldLayers,

                    spellPowerBonus,

                    shieldVfx,

                    breakVfx,

                    caster: context?.Caster,

                    target: soldier,

                    context: context);



                if (buff == null)

                {

                    continue;

                }



                buff.BuffId = buffId > 0 ? buffId : moonlightConfig.BuffId;
                buff.DamageType = treatmentDamageType;

                soldier.AddBuff(buff);
                BuffApplier.NotifyTreatmentApplied(context, soldier, treatmentDamageType);

            }



            return true;

        }



        private static void RemoveStaleMoonlightBuffs(SoldierCard soldier, int buffId)

        {

            if (soldier.BuffController == null)

            {

                return;

            }



            var toRemove = new List<IBuff>();
            using (BattleSkillProfilerMarkers.Begin(BattleSkillProfilerMarkers.BuffTriggerTempList))
            {
            foreach (var buff in soldier.BuffController.GetAllBuffs())

            {

                if (buff.BuffId == buffId && buff is MultiEffectBuff)

                {

                    toRemove.Add(buff);

                }

            }



            foreach (var buff in toRemove)

            {

                soldier.BuffController.RemoveBuff(buff);

            }
            }

        }



        private MoonlightShieldBuff FindMoonlightShield(SoldierCard soldier)

        {

            if (soldier.BuffController == null) return null;

            foreach (var b in soldier.BuffController.GetBuffs(BuffType.MoonlightShield))

            {

                if (b is MoonlightShieldBuff msb) return msb;

            }

            return null;

        }

    }

}

