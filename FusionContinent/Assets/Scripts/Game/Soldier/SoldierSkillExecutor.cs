using System;
using Config;
using FusionContinent.Model;
using FusionContinent.Target;
using Manager;
using UnityEngine;

namespace FusionContinent
{
    /// <summary>
    /// 角色技能执行器 - 管理技能释放条件、目标构建、状态切换与事件触发。
    /// </summary>
    public class SoldierSkillExecutor
    {
        private enum ReleaseEffectPhase
        {
            Comet,
            Health,
        }

        private readonly SoldierCard _soldier;
        private DateTime _cometSkillReleaseTime = DateTime.Now;

        public SoldierSkillExecutor(SoldierCard soldier)
        {
            _soldier = soldier;
        }

        public bool StartReleaseSkill(int specialId, Vector3 targetPosition = default, bool useNearestEnemy = false)
        {
            using (BattleSkillProfilerMarkers.Begin(BattleSkillProfilerMarkers.SkillRelease))
            {
                return StartReleaseSkillInternal(specialId, targetPosition, useNearestEnemy);
            }
        }

        private bool StartReleaseSkillInternal(int specialId, Vector3 targetPosition, bool useNearestEnemy)
        {
            if (_soldier.isDie || specialId == 0)
            {
                LogTool.LogWarning($"[SkillRelease-Start] {_soldier.cardId} 释放失败: isDie={_soldier.isDie}, specialId={specialId}");
                return false;
            }

            if (_soldier.BaseAttributes.GetFinal(AttributeType.SoldierUnableToReleaseSkills) > 0f)
            {
                LogTool.LogWarning($"[SkillRelease-Start] {_soldier.cardId} 释放失败: 属性 SoldierUnableToReleaseSkills 生效, value={_soldier.BaseAttributes.GetFinal(AttributeType.SoldierUnableToReleaseSkills)}");
                return false;
            }

            if (!BuildTarget(specialId, targetPosition, useNearestEnemy))
            {
                return false;
            }

            ApplyReleaseEffects(_soldier.nowTargetInfo, ReleaseEffectPhase.Comet);

            if (_soldier.CurrentStateType == SoldierCard.SoldierStateType.CastingSkill)
            {
                _soldier.soldierCastingSkillState.UseNewSkill(specialId, _soldier.nowTargetInfo);
                _soldier.releaseSpecialId = specialId;
            }
            else
            {
                _soldier.soldierCastingSkillState.SetSkillId(specialId, _soldier.nowTargetInfo);
                _soldier.releaseSpecialId = specialId;
                _soldier.SwitchState(SoldierCard.SoldierStateType.CastingSkill);
            }

            ApplyReleaseEffects(_soldier.nowTargetInfo, ReleaseEffectPhase.Health);

            TriggerReleaseEvents(specialId);

            LogTool.Log($"[SkillRelease-Start] {_soldier.cardId} 释放技能成功: specialId={specialId}, targetPos={_soldier.nowTargetInfo.targetPosition}, state={_soldier.CurrentStateType}");
            return true;
        }

        private bool BuildTarget(int specialId, Vector3 targetPosition, bool useNearestEnemy)
        {
            using (BattleSkillProfilerMarkers.Begin(BattleSkillProfilerMarkers.TargetSearch))
            {
                return BuildTargetInternal(specialId, targetPosition, useNearestEnemy);
            }
        }

        private bool BuildTargetInternal(int specialId, Vector3 targetPosition, bool useNearestEnemy)
        {
            if (targetPosition != default)
            {
                _soldier.nowTargetInfo = EnemyTargetFinder.FromPosition(targetPosition);
                return true;
            }

            MonsterCard targetEnemy = null;
            // 主动技能：人物为起点的扇形索敌（半径=攻击距离），不回退普攻最近锁定
            if (!_soldier.TrySelectSkillFanTarget(out targetEnemy))
            {
                if (Global.AutoReleaseSkillState.Value || useNearestEnemy)
                {
                    LogTool.LogWarning(
                        $"[SkillRelease-Start] {_soldier.cardId} 扇形索敌失败: specialId={specialId}, attackRange={_soldier.attackRange}");
                    return false;
                }

                // 手动释放且不强制索敌时允许无目标施法
                return true;
            }

            _soldier.nowTargetInfo = EnemyTargetFinder.FromMonster(targetEnemy);
            return true;
        }

        private void ApplyReleaseEffects(BattleTargetInfo targetInfo, ReleaseEffectPhase phase)
        {
            if (phase == ReleaseEffectPhase.Comet)
            {
                CheckStartReleaseCometSkill(targetInfo);
                return;
            }

            var releaseAddHealth = _soldier.BaseAttributes.GetFinal(AttributeType.ReleaseSkillProbabilityAddHealth);
            if (releaseAddHealth > 0f)
            {
                _soldier.SetSoldierHp((int)releaseAddHealth);
            }

            var releaseAddMaxHealth = _soldier.BaseAttributes.GetFinal(AttributeType.SoldierReleaseSkillRecoverMaxHealth);
            if (releaseAddMaxHealth > 0f)
            {
                _soldier.SetSoldierHp((int)_soldier.BaseAttributes.GetFinal(AttributeType.SoldierMaxHealth));
            }
        }

        private void TriggerReleaseEvents(int specialId)
        {
            using (BattleSkillProfilerMarkers.Begin(BattleSkillProfilerMarkers.CounterUpdate))
            {
                GamePlayManager.Default.TriggerGuide(GuideType.FirstReleaseSkill);
                Global.Publisher.Publish(MessagePublisher.CHANGE_GAME_CARD_RELEASE_COMPEPLETE, specialId);
                ConditionManager.Instance.OnTriggerProgress(ConditionType.SkillReleasesCount, 1);
                Game.Battle.BattleRuleComputerSupport.OnSkillCast(_soldier, specialId);

                if (specialId != _soldier.NormalAttackSkillId)
                {
                    // PVP：AI（对侧）放技能走独立连击计数，不再抬高玩家 combo
                    if (_soldier.BattleSide == BattleSide.Opponent)
                    {
                        ConsecutiveSkillReleaseCounter.Instance?.OnOpponentSkillReleased();
                    }
                    else
                    {
                        ConsecutiveSkillReleaseCounter.Instance?.OnSkillReleased();
                    }
                }
            }
        }

        private void CheckStartReleaseCometSkill(BattleTargetInfo targetInfo)
        {
            var direction = EnemyTargetFinder.ResolveAttackPosition(targetInfo);
            var cometBlow = GamePlayManager.Default.ModuleAttributes.GetFinal(AttributeType.SoldierStartReleaseCometSkill);
            if (cometBlow <= 0f) return;

            var skillCard = _soldier.GetSkillByType(SpecialEffectType.SecretCometTechnique);
            if (skillCard == null) return;

            var cdTime = skillCard.Attributes.GetFinal(AttributeType.SkillCdTime);
            var totalSeconds = (DateTime.Now - _cometSkillReleaseTime).TotalSeconds;
            if (totalSeconds < cdTime / Global.SpeedMultiplier.Value)
            {
                return;
            }

            skillCard.Cast(direction);
            _cometSkillReleaseTime = DateTime.Now;
        }
    }
}
