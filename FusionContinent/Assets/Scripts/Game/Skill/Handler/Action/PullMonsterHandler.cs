using System.Collections.Generic;
using FusionContinent.Skill.Config;
using UnityEngine;

namespace FusionContinent.Skill.Handler
{
    /// <summary>
    /// 将怪物拉拽到释放点（或施法者位置）。
    /// target=Hit 时复用 AoeSelect / AoeDamage 写入的 HitMonsters，可接在二者之后。
    /// </summary>
    public class PullMonsterHandler : SyncSkillNodeHandler
    {
        public override string NodeType => "PullMonster";

        public override void ExecuteSync(SkillNodeConfig node, SkillContext context)
        {
            var monsters = ResolveMonsters(node, context);
            if (monsters.Count == 0)
            {
                LogTool.LogWarning("[PullMonsterHandler] 无拉拽目标，请在前置节点使用 AoeSelect/AoeDamage，或本节点 target 设为 Aoe");
                return;
            }

            var pullTo = node.GetParamString("pull_to", "ReleasePoint");
            var duration = node.GetParamFloat("duration", 0.3f);
            var pullRatio = Mathf.Clamp01(node.GetParamFloat("pull_ratio", 1f));
            var dragAmplitude = node.GetParamFloat("drag_amplitude", 0f);
            var scaleDragAmplitudeByDistance = node.GetParamBool("scale_drag_amplitude_by_distance", false);

            var dest = ComputePullDestination(context, pullTo, dragAmplitude, scaleDragAmplitudeByDistance);

            for (int i = 0; i < monsters.Count; i++)
            {
                var monster = monsters[i];
                if (monster == null)
                    continue;

                var monsterPos = (Vector2)monster.transform.position;
                var finalPos = pullRatio >= 0.999f
                    ? dest
                    : Vector2.Lerp(monsterPos, dest, pullRatio);

                monster.PullToPosition(finalPos, duration);
            }

            LogTool.Log($"[PullMonsterHandler] 拉拽 {monsters.Count} 个目标 → {dest}, duration={duration}s, pull_ratio={pullRatio}");
        }

        private static List<MonsterCard> ResolveMonsters(SkillNodeConfig node, SkillContext context)
        {
            var targetMode = node.GetParamString("target", "Hit");
            if (targetMode == "Aoe")
            {
                var hits = AoeHitUtility.QueryMonsters(node, context);
                var list = new List<MonsterCard>(hits.Count);
                for (int i = 0; i < hits.Count; i++)
                {
                    var monster = hits[i].Monster;
                    if (SkillHitTargetGuard.CanApplyBuffToMonster(monster, context))
                        list.Add(monster);
                }

                return list;
            }

            var hitMonsters = new List<MonsterCard>(context.HitMonsters.Count);
            for (int i = 0; i < context.HitMonsters.Count; i++)
            {
                var monster = context.HitMonsters[i];
                if (SkillHitTargetGuard.CanApplyBuffToMonster(monster, context))
                    hitMonsters.Add(monster);
            }

            return hitMonsters;
        }

        private static Vector2 ComputePullDestination(
            SkillContext context,
            string pullTo,
            float dragAmplitude,
            bool scaleDragAmplitudeByDistance)
        {
            Vector2 dest;
            if (pullTo == "Caster")
            {
                dest = context.Caster != null
                    ? (Vector2)context.Caster.transform.position
                    : (Vector2)context.TargetPosition;
            }
            else
            {
                dest = (Vector2)context.TargetPosition;
            }

            if (Mathf.Abs(dragAmplitude) < 0.0001f)
                return dest;

            Vector2 dragDir;
            float dragDistance;
            if (context.HasDragInfo)
            {
                dragDir = context.DragDirection;
                dragDistance = context.DragDistance;
            }
            else if (context.Caster != null)
            {
                var delta = dest - (Vector2)context.Caster.transform.position;
                dragDistance = delta.magnitude;
                dragDir = dragDistance > 0.001f ? delta / dragDistance : Vector2.up;
            }
            else
            {
                dragDir = Vector2.up;
                dragDistance = 0f;
            }

            float offset = scaleDragAmplitudeByDistance
                ? dragAmplitude * dragDistance
                : dragAmplitude;

            if (pullTo == "Caster")
                dest -= dragDir * offset;
            else
                dest += dragDir * offset;

            return dest;
        }
    }
}
