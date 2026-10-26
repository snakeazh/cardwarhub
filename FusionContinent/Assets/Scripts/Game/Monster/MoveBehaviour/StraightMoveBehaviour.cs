using UnityEngine;
using Random = UnityEngine.Random;

namespace FusionContinent.Monster.MoveBehaviour
{
    /// <summary>
    /// 直行移动（MoveType.ZhiXing / SXing）。
    ///
    /// 移动流程：
    ///   1. Enter → 沿所属侧前进方向行进（Downward 阶段名保留）
    ///   2. 到达 EngageDepthY 且有索敌目标 → 切换为朝目标移动（TowardTarget）
    ///   3. 到达攻击范围后由 MonsterAtkRange 切换为 engage 状态，本 Behaviour 停止
    /// </summary>
    public class StraightMoveBehaviour : MonsterMoveBehaviourBase
    {
        protected MonsterMovePhase _phase = MonsterMovePhase.None;

        /// <summary>索敌后朝目标移动的目标点（含随机 X 偏移）。</summary>
        protected Vector3 _moveTarget;

        protected MonsterCard _monster;

        public override void Enter(MonsterCard monster)
        {
            _monster = monster;
            ResetSpeedScale();
            StartDownwardMove();
            monster.monsterData.isMoveEngaged = false;
        }

        public override void Update(MonsterCard monster, float deltaTime)
        {
            TryEngageTargetMove(monster);
            ApplyMove(monster, deltaTime);
        }

        public override void StopMove()
        {
            _phase = MonsterMovePhase.None;
        }

        protected void StartDownwardMove()
        {
            _phase = MonsterMovePhase.Downward;
        }

        /// <summary>锁定索敌目标位置，带 ±0.3 随机 X 偏移避免重叠。</summary>
        protected void StartTowardTargetMove(MonsterCard monster)
        {
            float xRandom = Random.Range(-0.3f, 0.3f);
            Vector3 targetPos = monster.targetSolidierCard.PlayerspineObj != null
                ? monster.targetSolidierCard.PlayerspineObj.transform.position
                : monster.targetSolidierCard.transform.position;
            _moveTarget = targetPos + new Vector3(xRandom, 0, 0);
            _phase = MonsterMovePhase.TowardTarget;
        }

        /// <summary>
        /// 到达一定深度后开始朝士兵移动。
        /// 索敌本身在 MonsterMoveState.Update 中处理，此处只负责切换移动阶段。
        /// </summary>
        protected virtual void TryEngageTargetMove(MonsterCard monster)
        {
            if (monster.targetSolidierCard == null)
            {
                return;
            }

            float engageY = GetEngageDepthY();
            // 深度判定跟随所属侧移动方向：向下走（-1）→ y 到达阈值下方才索敌；向上走（+1）→ y 到达阈值上方才索敌
            var engageLane = GameLand.Default != null ? GameLand.Default.GetLane(monster.BattleSide) : null;
            bool movesDown = engageLane == null || engageLane.MoveDirectionY < 0f;
            bool reachedDepth = movesDown
                ? monster.transform.position.y <= engageY
                : monster.transform.position.y >= engageY;

            if (reachedDepth && monster.monsterData.isMoveEngaged == false)
            {
                StartTowardTargetMove(monster);
                monster.monsterData.isMoveEngaged = true;
            }
        }

        /// <summary>开始朝目标移动的 Y 轴阈值，默认按所属侧 Lane 配置。</summary>
        protected virtual float GetEngageDepthY()
        {
            if (_monster != null && GameLand.Default != null)
            {
                var lane = GameLand.Default.GetLane(_monster.BattleSide);
                if (lane != null)
                {
                    return lane.EngageDepthY;
                }
            }

            return -3f;
        }

        protected virtual void ApplyMove(MonsterCard monster, float deltaTime)
        {
            if (_phase == MonsterMovePhase.None || deltaTime <= 0f)
            {
                return;
            }

            float speed = CalcMoveSpeed(monster);
            if (speed <= 0f)
            {
                return;
            }

            if (_phase == MonsterMovePhase.Downward)
            {
                ApplyDownwardMove(monster, speed, deltaTime);
                return;
            }

            if (_phase == MonsterMovePhase.TowardTarget)
            {
                ApplyTowardTargetMove(monster, speed, deltaTime);
            }
        }

        /// <summary>沿所属侧前进方向位移。</summary>
        protected virtual void ApplyDownwardMove(MonsterCard monster, float speed, float deltaTime)
        {
            float dirY = -1f;
            if (GameLand.Default != null)
            {
                var lane = GameLand.Default.GetLane(monster.BattleSide);
                if (lane != null)
                {
                    dirY = lane.MoveDirectionY;
                }
            }

            Vector3 next = monster.transform.position + new Vector3(0f, dirY, 0f) * speed * deltaTime;
            monster.MovePhysicsPosition(next);
        }

        /// <summary>朝 _moveTarget 插值移动。</summary>
        protected virtual void ApplyTowardTargetMove(MonsterCard monster, float speed, float deltaTime)
        {
            Vector3 pos = monster.transform.position;
            Vector3 offset = _moveTarget - pos;
            float distance = offset.magnitude;
            float step = speed * deltaTime;
            if (distance <= step)
            {
                monster.MovePhysicsPosition(_moveTarget);
            }
            else
            {
                monster.MovePhysicsPosition(pos + offset / distance * step);
            }
        }
    }
}
