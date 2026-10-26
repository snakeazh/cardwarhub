using Config;
using Cysharp.Threading.Tasks;
using FusionContinent.Skill.Config;
using FusionContinent.Skill.Handler;
using FusionContinent.Utils;
using Game.Battle;
using QFramework;
using UnityEngine;

namespace FusionContinent.Skill
{
    /// <summary>
    /// 技能图运行时：构建 SkillContext 并驱动 SkillNodeExecutor 执行节点链。
    ///
    /// 迁移说明（替代旧 SkillCarrying 链路）：
    /// - 条件增伤/Buff：不再经 UseSkill 组装 attributeStructs，改由 LearnSkillRule 在 BattleRuleDamageModifier 触发。
    /// - 暴击：普通暴击在命中时按目标独立掷骰；「下次技能必暴」仍经 ApplyCritPreRoll 预写到 SkillContext。
    ///
    /// 数据流：
    /// SoldierCard.UseSkill → SkillManager.CastSkill → SkillRuntime → SkillContext
    /// →（投射物命中）MonsterManager → DamageContextBuilder 判定 IsCrit → MonsterCard.TakeDamage
    /// </summary>
    public class SkillRuntime
    {
        private SkillNodeExecutor _executor = new SkillNodeExecutor();

        /// <summary>
        /// 对指定 Component 目标释放技能图。
        /// 创建 SkillContext 后依次写入暴击预掷与拖拽信息，再执行技能图。
        /// </summary>
        public async UniTask Execute(
            SkillGraphConfig config,
            int skillID,
            Component caster,
            Component target,
            bool skipBattleRuleBeforeTrigger = false,
            bool skipBuffOnCast = false)
        {
            if (config == null)
            {
                LogTool.LogError("[SkillRuntime] 技能配置为空");
                return;
            }

            var targetPos = target?.transform.position ?? Vector3.zero;
            var context = new SkillContext
            {
                SkillId = skillID,
                Level = Global.GetCardById(skillID) !=null?Global.GetCardById(skillID).Lv:1,
                Caster = caster,
                Target = target,
                TargetPosition = targetPos,
                Config = config,
                SkipBattleRuleBeforeTrigger = skipBattleRuleBeforeTrigger,
                SkipBuffOnCast = skipBuffOnCast,
            };
            if (target is MonsterCard primaryTarget)
            {
                SkillHitTargetGuard.SnapshotMonster(context, primaryTarget);
            }
            BattleRuleCastDamageSession.TryBindToSkillContext(context, caster as SoldierCard, skillID);
            // 施法瞬间预掷暴击，保证投射物延迟命中与释放时使用同一结果
            ApplyCritPreRoll(context, caster, skillID);
            ApplyDragInfo(context, caster, targetPos);
            ApplySkillAnimationPlayId(context, caster);

            try
            {
                await _executor.Execute(config, context);
                LogTool.Log($"[SkillRuntime] 技能执行完成");
            }
            catch (System.Exception ex)
            {
                LogTool.LogError($"[SkillRuntime] 技能执行异常: {ex}");
            }
        }

        /// <summary>
        /// 对指定世界坐标落点释放技能图（SoldierCard.UseSkill 主路径）。
        /// </summary>
        public async UniTask ExecuteToPoint(
            SkillGraphConfig config,
            int skillID,
            Component caster,
            Vector3 point,
            bool skipBattleRuleBeforeTrigger = false,
            bool skipBuffOnCast = false)
        {
            if (config == null)
            {
                LogTool.LogError($"[SkillRuntime] {skillID} 技能配置为空");
                return;
            }

            LogTool.Log($"[SkillRuntime] 开始执行技能: {config.name}");
            LogTool.Log($"[SkillRuntime] 目标位置: {point}");

            var context = new SkillContext
            {
                SkillId = skillID,
                Level = Global.GetCardById(skillID) !=null?Global.GetCardById(skillID).Lv:1,
                Caster = caster,
                TargetPosition = point,
                Config = config,
                SkipBattleRuleBeforeTrigger = skipBattleRuleBeforeTrigger,
                SkipBuffOnCast = skipBuffOnCast,
            };
            BattleRuleCastDamageSession.TryBindToSkillContext(context, caster as SoldierCard, skillID);
            ApplyCritPreRoll(context, caster, skillID);
            ApplyDragInfo(context, caster, point);
            ApplySkillAnimationPlayId(context, caster);

            try
            {
                await _executor.Execute(config, context);
                LogTool.Log($"[SkillRuntime] 技能执行完成");
            }
            catch (System.Exception ex)
            {
                LogTool.LogError($"[SkillRuntime] 技能执行异常: {ex}");
            }
        }

        /// <summary>
        /// 记录技能图开始执行时施法者的动画 playId（atk 回调链进入 UseSkill 后、节点遍历前快照）。
        /// 与 <see cref="SoldierAnimationController.CanPlayReturn"/> 配合，避免被抢占的旧技能播放 Return。
        /// </summary>
        private static void ApplySkillAnimationPlayId(SkillContext context, Component caster)
        {
            if (caster is not SoldierCard soldier || soldier.AnimationController == null)
            {
                return;
            }

            context.SkillAnimationPlayId = soldier.AnimationController.CurrentPlayId;
        }

        /// <summary>
        /// 施法时仅处理「下次技能必暴」预掷；普通暴击改由命中侧按目标独立掷骰。
        ///
        /// 规则：
        /// - 仅 SoldierCard 可写入 HasPreRolledCrit / PreRolledCrit（必暴标记）；
        /// - 普通暴击率判定在 DamageContextBuilder 命中时按目标独立执行；
        /// - 避免 AOE 共用一次预掷导致全暴或全不暴。
        /// </summary>
        private static void ApplyCritPreRoll(SkillContext context, Component caster, int skillId)
        {
            if (caster is not SoldierCard soldier)
            {
                return;
            }

            BattleRuleMustCriticalSupport.ApplyPendingNextSkillMustCriticalPreRoll(soldier, skillId, context);
        }

        /// <summary>
        /// 写入施法拖拽距离与方向，供技能图节点（如扇形、位移）读取。
        /// </summary>
        private static void ApplyDragInfo(SkillContext context, Component caster, Vector3 targetPoint)
        {
            if (caster == null)
            {
                context.HasDragInfo = false;
                context.DragDistance = 0f;
                context.DragDirection = Vector2.up;
                return;
            }

            var delta = targetPoint - caster.transform.position;
            context.DragDistance = delta.magnitude;
            context.DragDirection = context.DragDistance > 0.001f
                ? new Vector2(delta.x, delta.y) / context.DragDistance
                : Vector2.up;
            context.HasDragInfo = true;
        }
    }
}
