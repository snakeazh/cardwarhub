using Config;
using FusionContinent.Target;
using UnityEngine;

namespace FusionContinent
{
    /// <summary>
    /// 英灵攻击范围检测：定时索敌并更新 <see cref="SoldierCard.currentTarget"/>。
    /// 选敌算法委托 <see cref="EnemyTargetFinder"/>，本组件只负责轮询与生命周期。
    /// </summary>
    public class SoldierAtkRange : MonoBehaviour
    {
        private SoldierCard _soldierCard;

        /// <summary>当前检测到的攻击目标（与 SoldierCard.currentTarget 同步）</summary>
        public MonsterCard _currentTarget;

        /// <summary>配置表攻击距离，传给 EnemyTargetFinder 做 range/250 换算</summary>
        private float _configAttackRange;

        /// <summary>当前普攻索敌使用的配置攻击距离（主动技能索敌复用此值，保证与普攻一致）</summary>
        public float CurrentAttackRange => _configAttackRange;

        /// <summary>索敌检测间隔（秒），避免每帧全量遍历</summary>
        private float _detectionInterval = 0.2f;
        private float _lastDetectionTime;

        public void Initialize(SoldierCard soldierCard, float range)
        {
            _soldierCard = soldierCard;
            _configAttackRange = range;
            // 索敌已改为距离计算，不再依赖碰撞体触发
            Destroy(GetComponent<CircleCollider2D>());
        }

        public void SetDetectionRange(float range)
        {
            _configAttackRange = range;
        }

        private void OnEnable()
        {
            MonsterCard.onDestroyed += OnTargetDestroyed;
        }

        private void OnDisable()
        {
            MonsterCard.onDestroyed -= OnTargetDestroyed;
        }

        private void Update()
        {
            if (Time.time - _lastDetectionTime >= _detectionInterval)
            {
                DetectTargets();
                _lastDetectionTime = Time.time;
            }
        }

        /// <summary>
        /// 索敌：精英/Boss 优先；战士取最前方；其它职业取范围内最近。
        /// </summary>
        private void DetectTargets()
        {
            if (_soldierCard == null || _soldierCard.isDrag) return;

            var career = _soldierCard.soldierBattle != null
                ? _soldierCard.soldierBattle.CarType
                : CareerType.Archer;
            var moveDirectionY = -1f;
            if (GameLand.Default != null)
            {
                var lane = GameLand.Default.GetLane(_soldierCard.BattleSide);
                if (lane != null)
                {
                    moveDirectionY = lane.MoveDirectionY;
                }
            }

            EnemyTargetFinder.TrySelectCombatTarget(
                transform.position,
                _configAttackRange,
                career,
                moveDirectionY,
                CombatTargetFallback.NearestInRange,
                out var closestTarget);

            if (_currentTarget != closestTarget)
            {
                _currentTarget = closestTarget;
                _soldierCard.SetClosestTarget(_currentTarget);
            }
        }

        /// <summary>锁定目标被销毁时清空，等待下一轮 DetectTargets 重新索敌</summary>
        private void OnTargetDestroyed(MonsterCard destroyedCard)
        {
            if (_currentTarget == destroyedCard)
            {
                _currentTarget = null;
                _soldierCard.SetClosestTarget(_currentTarget);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (_soldierCard != null && !_soldierCard.isDrag)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, EnemyTargetFinder.ToWorldRange(_configAttackRange));
            }
        }
    }
}
