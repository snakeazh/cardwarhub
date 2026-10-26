using Config;
using FusionContinent;
using FusionContinent.Skill.Handler;
using Manager;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Battle
{
    /// <summary>
    /// 施法时刻规则（CastSkill / OnSkillBefore 等触发的 ModifyAttr）临时属性修正会话。
    /// <para>
    /// 修正写入英灵 <see cref="FusionContinent.Attribute.AttributeSet"/>，由 GetFinal 统一结算
    /// （基础值 × (1 + PercentAdd) + FlatAdd），不再在每次伤害结算时对 DamageContext 叠乘百分比。
    /// </para>
    /// </summary>
    public static class BattleRuleCastDamageSession
    {
        private struct SessionState
        {
            public int SkillId;
            public AttributeType AttributeType;
            public string ModifierSourceKey;
            public int RemainingCasts;
            public bool IsActive => SkillId > 0 && RemainingCasts > 0 && !string.IsNullOrEmpty(ModifierSourceKey);
        }

        private static readonly Dictionary<int, SessionState> sSessions = new Dictionary<int, SessionState>();

        private static string BuildModifierSourceKey(int soldierInstanceId)
        {
            return "BattleRuleCastDamage_" + soldierInstanceId;
        }

        /// <summary>
        /// 注册施法时刻属性修正：立即写入 AttributeSet，施法次数用尽后移除。
        /// </summary>
        public static void Register(
            SoldierCard soldier,
            int skillId,
            EffectParamStruct effect,
            int totalCasts)
        {
            if (soldier == null || skillId <= 0 || totalCasts <= 0 || effect == null)
            {
                return;
            }

            var soldierKey = soldier.GetInstanceID();
            var sourceKey = BuildModifierSourceKey(soldierKey);

            RemoveSessionModifier(soldier, sSessions.TryGetValue(soldierKey, out var existing) ? existing : default);

            BattleRuleAcquireSoldierEffectApplier.ApplyEffectToSoldier(
                effect,
                sourceKey,
                AttributeSourceType.SchoolCard,
                soldier,
                false);

            sSessions[soldierKey] = new SessionState
            {
                SkillId = skillId,
                AttributeType = effect.AttributeType,
                ModifierSourceKey = sourceKey,
                RemainingCasts = totalCasts,
            };

            LogTool.Log(
                $"[BattleRuleCastDamageSession] Register skill={skillId}, hero={soldier.cardId}, casts={totalCasts}, attr={effect.AttributeType}, base={effect.BaseValue}, pre={effect.PreValue}");
        }

        /// <summary>技能图开始执行时标记 SkillContext（兼容延迟命中链路，不再写 DamageContext 百分比）。</summary>
        public static void TryBindToSkillContext(SkillContext skillContext, SoldierCard soldier, int skillId)
        {
            if (skillContext == null || soldier == null || skillId <= 0)
            {
                return;
            }

            if (!sSessions.TryGetValue(soldier.GetInstanceID(), out var session)
                || !session.IsActive
                || session.SkillId != skillId)
            {
                return;
            }

            skillContext.HasCastDamageModifier = true;
            skillContext.CastDamageModifierAttributeType = session.AttributeType;
            skillContext.CastDamageModifierBaseValue = 0f;
            skillContext.CastDamageModifierPreValue = 0f;
        }

        /// <summary>延迟命中前同步会话标记；属性值已由 AttributeSet 提供，不再拷贝到 DamageContext。</summary>
        public static void CopyToDamageContext(SkillContext skillContext, DamageContext damageContext)
        {
            if (skillContext == null || damageContext == null || !skillContext.HasCastDamageModifier)
            {
                return;
            }

            damageContext.HasCastDamageModifier = true;
            damageContext.CastDamageModifierAttributeType = skillContext.CastDamageModifierAttributeType;
            damageContext.CastDamageModifierBaseValue = 0f;
            damageContext.CastDamageModifierPreValue = 0f;
        }

        /// <summary>
        /// 保留调用点以维持伤害管线时序；修正已在 Register 时写入 AttributeSet，此处不再叠乘 DamageContext。
        /// </summary>
        public static void TryApplyToDamageContext(DamageContext ctx)
        {
        }

        /// <summary>单次技能施法流程结束后扣减 RemainingCasts；为 0 时移除 AttributeSet 修饰并清除会话。</summary>
        public static void OnSkillCastCompleted(SoldierCard soldier, int skillId)
        {
            if (soldier == null || skillId <= 0)
            {
                return;
            }

            var key = soldier.GetInstanceID();
            if (!sSessions.TryGetValue(key, out var session)
                || !session.IsActive
                || session.SkillId != skillId)
            {
                return;
            }

            session.RemainingCasts--;
            if (session.RemainingCasts <= 0)
            {
                RemoveSessionModifier(soldier, session);
                sSessions.Remove(key);
                LogTool.Log(
                    $"[BattleRuleCastDamageSession] Completed skill={skillId}, hero={soldier.cardId}");
                return;
            }

            sSessions[key] = session;
        }

        /// <summary>清空全部会话并移除仍挂载的 AttributeSet 修饰。</summary>
        public static void ClearAll()
        {
            if (sSessions.Count == 0)
            {
                return;
            }

            var gamePlay = GamePlayManager.Default;
            if (gamePlay != null)
            {
                gamePlay.TraverseVillageSoldier(hero =>
                {
                    if (hero == null)
                    {
                        return;
                    }

                    if (sSessions.TryGetValue(hero.GetInstanceID(), out var session) && session.IsActive)
                    {
                        RemoveSessionModifier(hero, session);
                    }
                });
            }

            sSessions.Clear();
        }

        private static void RemoveSessionModifier(SoldierCard soldier, SessionState session)
        {
            if (soldier == null || string.IsNullOrEmpty(session.ModifierSourceKey))
            {
                return;
            }

            BattleRuleAcquireSoldierEffectApplier.RemoveEffectFromAttributeSet(
                soldier.BaseAttributes,
                session.AttributeType,
                AttributeSourceType.SchoolCard,
                session.ModifierSourceKey);
        }
    }
}
