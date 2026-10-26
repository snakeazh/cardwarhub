using Game.StateMachine;
using Game.Battle;
using UnityEngine;

namespace FusionContinent
{
    /// <summary>
    /// 攻击状态 - 角色正在攻击目标
    /// </summary>
    public class SoldierAttackingState : State
    {
        private float skillTimeout = 3f; // 技能超时时间
        private float skillTimer = 0f;
        public SoldierAttackingState(StateMachine sm, SoldierCard soldier) : base(sm, soldier)
        {
        }

        public override void Enter()
        {
            base.Enter();

            skillTimer = 0f;

            var request = BattleCastRequestFactory.FromNormalAttack(soldier);
            if (!BattleScheduler.Instance.RequestCastSkill(request))
            {
                soldier.PlayAtkAnimation();
            }
        }

        public override void Update()
        {
            base.Update();
            // 超时保护
            skillTimer += Time.deltaTime * Global.SpeedMultiplier.Value;
            // 攻击状态逻辑由 Update 中的 Transition 条件控制
            if (skillTimer >= skillTimeout)
            {
                LogTool.LogWarning($"[SoldierCard {soldier.cardId}] 攻击释放超时，强制结束");
                soldier.SwitchState(SoldierCard.SoldierStateType.Idle);
            }
        }
    }
}
