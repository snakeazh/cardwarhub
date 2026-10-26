using Config;
using FusionContinent;
using Game.StateMachine;
using UnityEngine;

namespace Game.StateMachine
{
    /// <summary>
    /// 怪物移动状态。
    ///
    /// 职责划分：
    ///   - 本状态：walk 动画、朝向、索敌（EngageSoldierCard）
    ///   - MoveBehaviour：实际位移（在 MonsterManager.FixedUpdate 中驱动）
    ///
    /// 状态切换：MonsterAtkRange 检测进入攻击范围 → engage 状态。
    /// </summary>
    public class MonsterMoveState : State
    {
        public MonsterMoveState(StateMachine sm, MonsterCard monster) : base(sm, monster)
        {
        }

        public override void Enter()
        {
            monster.Play("walk", true);
            // 初始化移动策略（开始向下行进等）
            monster.MoveBehaviour.Enter(monster);
        }

        public override void Update()
        {
            monster.IsSpine = true;

            // 索敌：目标死亡时重新选取，并重置"已朝目标移动"标记
            if (monster.targetSolidierCard == null || monster.targetSolidierCard.isDie)
            {
                monster.EngageSoldierCard(monster.transform.position);
                monster.monsterData.isMoveEngaged = false;
            }

            UpdateFacing();
        }

        public override void Exit()
        {
            monster.MoveBehaviour.Exit(monster);
        }

        /// <summary>更新行走动画速度和朝向（普通怪物按 X 轴翻转 Spine）。</summary>
        private void UpdateFacing()
        {
            if (monster.monsterData.spineTransform == null)
            {
                return;
            }

            if (monster.monsterData.skeletonAnim == null)
            {
                return;
            }

            monster.AnimationController.SetTimeScale(1 * Global.SpeedMultiplier.Value *
                                                                        monster.monsterData.monsterAbnormal.monsterspeed *
                                                                        (monster.monsterData.monsterAbnormal.vertigo ? 0 : 1)
                                                                        * monster._tbMonsterBattleConfig.GetOrDefault(monster._cardId).MoveAnimationSpeed);

            if (monster._monsterConfig.MonsterQuality == MonsterQuality.Ordinary)
            {
                if (monster.targetSolidierCard != null && !monster.targetSolidierCard.isDie)
                {
                    float targetX = monster.targetSolidierCard.PlayerspineObj.transform.position.x;
                    monster.AnimationController.SetSkeletonScaleX(targetX < monster.transform.position.x ? 1 : -1);
                }
                else
                {
                    monster.AnimationController.SetSkeletonScaleX(monster.transform.localPosition.x > 0 ? 1 : -1);
                }
            }
        }
    }
}
