using Cysharp.Threading.Tasks;
using FusionContinent;
using FusionContinent.Skill;
using Game.Battle;
using UnityEngine;

public class SkillManager : MonoBehaviour
{
    public static SkillManager instance;

    private void Awake()
    {
        instance = this;
    }

    /// <summary>
    /// 朝向指定方向释放技能图技能。
    /// 条件增伤/Buff 由 LearnSkillRule 在伤害管线触发；暴击预掷在 SkillRuntime 内完成。
    /// </summary>
    public async UniTask CastSkill(
        int skillID,
        Component caster,
        Vector3 point,
        bool skipBattleRuleBeforeTrigger = false,
        bool skipBuffOnCast = false)
    {
        var scheduler = BattleScheduler.Instance;
        if (scheduler.ShouldScheduleSkillGraph())
        {
            var request = BattleCastRequestFactory.FromSkillGraphPoint(
                skillID, caster, point, skipBattleRuleBeforeTrigger, skipBuffOnCast);
            if (scheduler.RequestCastSkill(request))
            {
                await request.WaitAsync();
                return;
            }
        }

        await CastSkillInternalToPoint(skillID, caster, point, skipBattleRuleBeforeTrigger, skipBuffOnCast);
    }

    /// <summary>
    /// 指定目标释放技能图技能。
    /// </summary>
    public async UniTask CastSkill(
        int skillID,
        Component caster,
        Component target,
        bool skipBattleRuleBeforeTrigger = false,
        bool skipBuffOnCast = false)
    {
        var scheduler = BattleScheduler.Instance;
        if (scheduler.ShouldScheduleSkillGraph())
        {
            var request = BattleCastRequestFactory.FromSkillGraphTarget(
                skillID, caster, target, skipBattleRuleBeforeTrigger, skipBuffOnCast);
            if (scheduler.RequestCastSkill(request))
            {
                await request.WaitAsync();
                return;
            }
        }

        await CastSkillInternalToTarget(skillID, caster, target, skipBattleRuleBeforeTrigger, skipBuffOnCast);
    }

    private static async UniTask CastSkillInternalToPoint(
        int skillID,
        Component caster,
        Vector3 point,
        bool skipBattleRuleBeforeTrigger,
        bool skipBuffOnCast)
    {
        SkillRuntime skillRuntime = new SkillRuntime();
        await skillRuntime.ExecuteToPoint(
            GamePlayManager.Default._skillConfigManager.Get(skillID),
            skillID,
            caster,
            point,
            skipBattleRuleBeforeTrigger,
            skipBuffOnCast);
    }

    private static async UniTask CastSkillInternalToTarget(
        int skillID,
        Component caster,
        Component target,
        bool skipBattleRuleBeforeTrigger,
        bool skipBuffOnCast)
    {
        SkillRuntime skillRuntime = new SkillRuntime();
        await skillRuntime.Execute(
            GamePlayManager.Default._skillConfigManager.Get(skillID),
            skillID,
            caster,
            target,
            skipBattleRuleBeforeTrigger,
            skipBuffOnCast);
    }
}
