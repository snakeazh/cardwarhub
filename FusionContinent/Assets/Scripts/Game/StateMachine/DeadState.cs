using DG.Tweening;
using Game.StateMachine;
using UnityEngine;

namespace FusionContinent
{
    /// <summary>
    /// 死亡状态 - 角色已死亡
    /// </summary>
    public class SoldierDeadState : State
    {
        public SoldierDeadState(StateMachine stateMachine, SoldierCard soldier) : base(stateMachine, soldier)
        {
        }

        public override void Enter()
        {
            base.Enter();

            // 播放死亡动画
            // soldier.AnimationController?.PlayDie();

            // 停止所有攻击
            soldier.LockAttackState();
            
        }
        

        public override void Update()
        {
            base.Update();

            // 死亡状态下不进行任何更新
            // 除非复活，否则保持在死亡状态
        }

        public override void Exit()
        {
            base.Exit();
        }
    }
}