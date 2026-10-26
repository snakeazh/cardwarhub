using System;
using Game.StateMachine;
using UnityEngine;

namespace FusionContinent
{
    /// <summary>
    /// 待机状态 - 角色默认状态，搜索目标、移动等
    /// </summary>
    public class SoldierIdleState : State
    {
        public SoldierIdleState(StateMachine sm, SoldierCard soldier) : base(sm, soldier)
        {
        }

        public override void Enter()
        {
            base.Enter();

            // 播放待机动画
            soldier.AnimationController?.PlayIdle();

            // 解锁攻击状态
            soldier.UnlockAttackState();
        }

        public override void Update()
        {
            base.Update();

            // 待机状态逻辑由 Update 中的 Transition 条件控制
            // 通过外部状态变量控制转换
        }
    }
}
