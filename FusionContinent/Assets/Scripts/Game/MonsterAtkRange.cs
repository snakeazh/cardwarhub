using DG.Tweening;
using UnityEngine;

namespace FusionContinent
{
    public class MonsterAtkRange : MonoBehaviour
    {
        private MonsterCard _monsterCard;
        public float _range;
        private Rigidbody2D _rb;

        [Header("墙体图层")]
        public LayerMask wallLayer;

        [Header("路径分段检测数（越高越精确）")]
        public int interpolationSteps = 8;

        private Vector2 _lastPosition;

        public void Initialize(MonsterCard monsterCard, float range)
        {
            _monsterCard = monsterCard;
            _range = range;
            _rb = _monsterCard.PhysicsBody;
            _lastPosition = _monsterCard.transform.position;
        }

        private void FixedUpdate()
        {
            if(_monsterCard == null) return;
            if (_monsterCard.monsterData.monsterState == MonsterState.engage)
            {
                //攻击状态 查看敌人死亡否
                if (_monsterCard.targetSolidierCard == null || _monsterCard.targetSolidierCard.isDie)
                {
                    _monsterCard.monsterData.monsterState = MonsterState.move;
                    _monsterCard.targetSolidierCard = null;
                    _monsterCard.Tick();
                }
            }
            
            if (!CanEnterEngage()) return;

            var targetPos = GetSoldierEngagePosition(_monsterCard.targetSolidierCard);
            if (Vector2.Distance(targetPos, _monsterCard.transform.position) <= _range)
            {
                _monsterCard.enemyAtkPos = _monsterCard.transform.position;
                _monsterCard.monsterData.monsterState  = MonsterState.engage;   //索敌成功
                _monsterCard.Tick();
            }

            // Vector2 currentPos = _monsterCard.transform.position;
            // Vector2 movement = currentPos - _lastPosition;
            //
            // Vector2 stepOffset = movement / interpolationSteps;
            // Vector2 direction = movement.normalized;
            //
            // for (int i = 1; i <= interpolationSteps; i++)
            // {
            //     Vector2 interpPos = _lastPosition + stepOffset * i;
            //
            //     // 使用直线射线检测代替盒型检测
            //     RaycastHit2D hit = Physics2D.Raycast(
            //         interpPos,
            //         direction,
            //         _range,
            //         wallLayer
            //     );
            //
            //     Debug.DrawRay(interpPos, direction * _range, Color.red, 0.05f);
            //
            //     if (hit.collider != null)
            //     {
            //         _monsterCard.enemyAtkPos = hit.point;
            //         _monsterCard.monsterData.monsterState  = MonsterState.engage;   //索敌成功
            //         // 停止DOTween移动
            //         break;
            //     }
            // }

            // _lastPosition = currentPos;
        }

        private static Vector2 GetSoldierEngagePosition(SoldierCard soldier)
        {
            if (soldier.PlayerspineObj != null)
            {
                return soldier.PlayerspineObj.transform.position;
            }

            return soldier.transform.position;
        }

        private bool CanEnterEngage()
        {
            if (_monsterCard == null || _monsterCard.targetSolidierCard == null)
            {
                return false;
            }

            var state = _monsterCard.monsterData.monsterState;
            if (state == MonsterState.engage
                || state == MonsterState.weak
                || state == MonsterState.die
                || state == MonsterState.specialMove
                || state == MonsterState.hurt)
            {
                return false;
            }

            return true;
        }

        private void OnDrawGizmosSelected()
        {
            if (_monsterCard == null) return;

            Vector2 pos = Application.isPlaying ? _monsterCard.transform.position : transform.position;
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(pos, 0.1f);
        }
    }
}