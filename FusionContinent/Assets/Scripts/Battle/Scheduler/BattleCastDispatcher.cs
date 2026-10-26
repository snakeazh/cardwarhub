using Cysharp.Threading.Tasks;
using FusionContinent;
using FusionContinent.Skill;
using UnityEngine;

namespace Game.Battle
{
    /// <summary>
    /// 将调度请求分发到既有技能释放路径。
    /// </summary>
    public class BattleCastDispatcher
    {
        public void Dispatch(BattleCastRequest request)
        {
            DispatchAsync(request).Forget();
        }

        private async UniTaskVoid DispatchAsync(BattleCastRequest request)
        {
            await DispatchAsyncInternal(request);
        }

        private async UniTask DispatchAsyncInternal(BattleCastRequest request)
        {
            BattleScheduler.Instance.EnterBypassScope();
            try
            {
                switch (request.Kind)
                {
                    case BattleCastKind.FullActiveSkill:
                        request.Complete(DispatchFullActiveSkill(request));
                        break;
                    case BattleCastKind.NormalAttack:
                        using (BattleSkillProfilerMarkers.Begin(BattleSkillProfilerMarkers.SkillRelease))
                        {
                            DispatchNormalAttack(request);
                            request.Complete(true);
                        }
                        break;
                    case BattleCastKind.SkillGraphToPoint:
                        await DispatchSkillGraphToPoint(request);
                        request.Complete(true);
                        break;
                    case BattleCastKind.SkillGraphToTarget:
                        await DispatchSkillGraphToTarget(request);
                        request.Complete(true);
                        break;
                    default:
                        LogTool.LogWarning($"[BattleCastDispatcher] 未知请求类型: {request.Kind}");
                        request.Complete(false);
                        break;
                }
            }
            catch (System.Exception ex)
            {
                LogTool.LogException(ex);
                request.Complete(false);
            }
            finally
            {
                BattleScheduler.Instance.ExitBypassScope();
            }
        }

        private static bool DispatchFullActiveSkill(BattleCastRequest request)
        {
            if (request.DeferSkillCardCooldown)
            {
                var gameLand = GameLand.Default;
                if (gameLand == null)
                {
                    return false;
                }

                return gameLand.TryExecuteDeferredSkillCardRelease(
                    request.SkillId,
                    request.Caster,
                    request.UseNearestEnemy,
                    request.HasTargetPosition ? request.TargetPosition : default);
            }

            var soldier = request.Caster;
            if (request.HasTargetPosition)
            {
                return soldier.StartReleaseSkill(request.SkillId, request.TargetPosition);
            }

            return soldier.StartReleaseSkill(request.SkillId, useNearestEnemy: request.UseNearestEnemy);
        }

        private static void DispatchNormalAttack(BattleCastRequest request)
        {
            request.Caster.PlayAtkAnimation();
        }

        private static Component ResolveGraphCaster(BattleCastRequest request)
        {
            if (request.GraphCaster != null)
            {
                return request.GraphCaster;
            }

            return request.Caster;
        }

        private static async UniTask DispatchSkillGraphToPoint(BattleCastRequest request)
        {
            var skillRuntime = new SkillRuntime();
            var config = GamePlayManager.Default._skillConfigManager.Get(request.SkillId);
            await skillRuntime.ExecuteToPoint(
                config,
                request.SkillId,
                ResolveGraphCaster(request),
                request.TargetPosition,
                request.SkipBattleRuleBeforeTrigger,
                request.SkipBuffOnCast);
        }

        private static async UniTask DispatchSkillGraphToTarget(BattleCastRequest request)
        {
            var skillRuntime = new SkillRuntime();
            var config = GamePlayManager.Default._skillConfigManager.Get(request.SkillId);
            await skillRuntime.Execute(
                config,
                request.SkillId,
                ResolveGraphCaster(request),
                request.SkillGraphTarget,
                request.SkipBattleRuleBeforeTrigger,
                request.SkipBuffOnCast);
        }
    }
}
