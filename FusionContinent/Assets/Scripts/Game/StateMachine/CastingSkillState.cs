using System;
using FusionContinent.Target;
using Game.StateMachine;
using UnityEngine;

namespace FusionContinent
{
    /// <summary>
    /// 释放技能状态 - 角色正在释放技能
    /// </summary>
    public class SoldierCastingSkillState : State
    {
        private float skillTimeout = 3f; // 技能超时时间
        private float skillTimer = 0f;
        private int currentSkillId = 0;
        private bool useSkill = false;
        /// <summary>进入施法状态时的目标快照，与 SoldierCard.nowTargetInfo 对应</summary>
        private BattleTargetInfo currentTargetInfo;
        
        public SoldierCastingSkillState(StateMachine sm, SoldierCard soldier) : base(sm, soldier)
        {
        }

        /// <summary>
        /// 设置要释放的技能 ID 与目标快照（在进入状态前调用）
        /// </summary>
        /// <param name="targetInfo">由 EnemyTargetFinder 构建的目标信息</param>
        public void SetSkillId(int skillId, BattleTargetInfo targetInfo)
        {
            currentSkillId = skillId;
            useSkill = false;
            currentTargetInfo = targetInfo;
        }

        /// <summary>施法过程中切换技能时更新目标快照</summary>
        public void UseNewSkill(int skillId, BattleTargetInfo targetInfo)
        {
            skillTimer = 0f;

            // 播放技能动画
            if (currentSkillId > 0 && !useSkill)
            {
                soldier.PlayAttackAnim(currentSkillId, currentTargetInfo);
                useSkill = true;
            }

            SetSkillId(skillId, targetInfo);
        }

        public override void Enter()
        {
            base.Enter();

            // 重置计时器
            skillTimer = 0f;

            // 播放技能动画
            if (currentSkillId > 0)
            {
                soldier.PlayAttackAnim(currentSkillId, currentTargetInfo);
                useSkill = true;
            }
        }

        public override void Update()
        {
            base.Update();

            if (currentSkillId > 0 && !useSkill)
            {
                soldier.PlayAttackAnim(currentSkillId, currentTargetInfo);
                useSkill = true;
            }
            
            // 超时保护
            skillTimer += Time.deltaTime * Global.SpeedMultiplier.Value;
            if (skillTimer >= skillTimeout)
            {
                LogTool.LogWarning($"[SoldierCard {soldier.cardId}] 技能释放超时，强制结束");
                soldier.SwitchState(SoldierCard.SoldierStateType.Idle);
            }
        }

        public override void Exit()
        {
            base.Exit();
            // 确保解锁攻击状态
            soldier.UnlockAttackState();
            currentSkillId = 0;
        }
    }
}
