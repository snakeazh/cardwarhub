using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Config;
using DG.Tweening;
using FusionContinent.Skill;
using UnityEngine;
using QFramework;
using Random = UnityEngine.Random;

namespace FusionContinent
{
    public partial class TornadoProjectile : ViewController
    {
        private float _speed;
        private Tween _pTween;
        private Vector3 _direction;
        private bool _isDOMove;

        // 新增逻辑
        private HashSet<MonsterCard> _hitTargets = new HashSet<MonsterCard>(); // 用于防止对同一个目标造成多次伤害
        private bool isDestroyed = false;
        private bool isPendingDestroy = false;
        private Sequence damageSequence;
        private ISkill _skillCard;

        private void Awake()
        {
        }
        
        public void Launch(ISkill skillCard, Vector3 direction, float speed)
        {
            this._speed = speed;
            this._skillCard = skillCard;
            this._direction = direction;
            
            // 使用方向向量计算远处目标位置
            Vector3 targetPosition = transform.position + direction * 100f; // 100f是一个远距离，确保超出屏幕范围

            // 使用DOTween使子弹朝指定方向移动
            _pTween = transform
                    .DOMove(targetPosition, _speed)
                    .SetEase(Ease.Linear)
                    .SetSpeedBased();

            _isDOMove = true;

            // 特效创建单独的销毁序列
            DOTween.Sequence()
                .AppendInterval(skillCard.Attributes.GetFinal(AttributeType.SkillDuration))
                .AppendCallback(() =>
                {
                    _pTween = null;
                    _isDOMove = false;
                    Destroy(gameObject); // 销毁对象
                });
        }
       

        private void Update()
        {
            // 检测子弹是否超出屏幕范围
            if (!IsOnScreen(transform.position))
            {
                Destroy(gameObject); // 如果超出屏幕范围，销毁子弹
            }
        }

        // 检测物体是否在屏幕内的方法
        private bool IsOnScreen(Vector3 position)
        {
            Vector3 screenPoint = Camera.main.WorldToViewportPoint(position);
            return screenPoint.x > 0 && screenPoint.x < 1 && screenPoint.y > 0 && screenPoint.y < 1;
        }

        /**
         * 碰撞检测
         */
        private void OnTriggerStay2D(Collider2D collision)
        {
            if (collision.name != "HitBox")
            {
                return;
            }

            MonsterCard hitTarget = collision.transform.parent.GetComponent<MonsterCard>();
            if (hitTarget != null && _hitTargets.Add(hitTarget)) 
            {
                DOTween.Sequence().AppendInterval(0.3f) // 延迟 0.3 秒
                .AppendCallback(() =>
                {
                    if (hitTarget)
                    {
                        // LogTool.Log(">>>>> 龙卷风击中敌人 - monster"+ hitTarget._cardId);
                        hitTarget.ReceiveDamage(MonsterDamageRequest.ForSkillSumTypes(
                            hitTarget, _skillCard, collision.bounds.ClosestPoint(transform.position)));
                        
                        // 特效
                        SpawnHitEffect(hitTarget.transform.position);
                        
                        // 连续伤害逻辑
                        var continuousDamageNum = (int)_skillCard.Attributes.GetFinal(AttributeType.SkillContinuousDamageNum);
                        if (continuousDamageNum > 0)
                        {
                            ApplyContinuousDamage(hitTarget, continuousDamageNum);
                        }
                        
                        _hitTargets.Clear();
                    }
                });
                
                DOTween.Sequence().AppendInterval(0.1f) // 延迟 0.1 秒
                .AppendCallback(() =>
                {
                    if (_isDOMove)
                    {
                        if (_pTween != null)
                        {
                            // LogTool.Log(">>>>> 龙卷风击中敌人 - 停止");
                            _pTween.Kill(); // 停止移动
                            
                            // 旋转
                            StartMoving();
                        }
                        _isDOMove = false;
                    }
                });
                
            }
        }
        
        private void ApplyContinuousDamage(MonsterCard target, int continuousDamageNum)
        {
            // 使用 DOTween 的 Sequence 代替协程处理连续伤害
            damageSequence = DOTween.Sequence();

            for (int i = 0; i < continuousDamageNum; i++)
            {
                damageSequence.AppendCallback(() =>
                {
                    if (target != null)
                    {
                        target.ReceiveDamage(MonsterDamageRequest.ForSkillSumTypes(target, _skillCard, this.transform.position));
                    }
                });
                damageSequence.AppendInterval(0.1f); // 每次伤害间隔
            }
        }
        
        /**
         * moveRange 小范围移动的半径
         * moveDuration 每次移动的持续时间
         * waitDuration 每次移动后等待的时间
         */
        private void StartMoving(float moveRange = 5f, float moveDuration = 2f, float waitDuration = 1f)
        {
            Vector3 randomDir = Random.insideUnitSphere * moveRange;
            randomDir += transform.position;
            // randomDir.y = transform.position.y; // 保持y轴高度不变
            randomDir.z = transform.position.z; // 保持z轴高度不变

            // 移动
            transform.DOMove(randomDir, moveDuration)
                .SetEase(Ease.Linear)
                .OnComplete(() =>
                {
                    Invoke(nameof(StartMoving), waitDuration);
                });

            // 旋转
            transform.DORotate(new Vector3(0, 360, 0), 1f, RotateMode.FastBeyond360)
                .SetLoops(-1, LoopType.Restart)
                .SetEase(Ease.Linear)
                .SetSpeedBased(true)
                .SetRelative(true);
        }
        
        
        /**
         * 受击表现
         */
        private void SpawnHitEffect(Vector3 position)
        {
            string hitEffect = _skillCard.Config.SkillHitEffect ?? "youxia_jn1_06";
            GenericObjectPool pool = ObjectPoolManager.Instance.GetGameObjectPoolOrCreate(hitEffect);
            if (pool == null) return;

            var effect = pool.Get();
            if (effect == null) return;

            effect.transform.SetParent(GameLand.Default.DamageParent.transform);
            effect.transform.position = position;
            effect.transform.localScale = Vector3.one * 200;

            StartCoroutine(DisableAfterDelay(pool, effect, 0.5f));
        }
        
        private IEnumerator DisableAfterDelay(GenericObjectPool pool, GameObject effect, float delay)
        {
            yield return new WaitForSeconds(delay);

            if (effect != null)
            {
                pool.Return(effect);
            }
        }
        
        
        private void OnDestroy()
        {
            // 确保子弹销毁时，停止所有与其相关的 DOTween 动画
            DOTween.Kill(transform, true);
        }
    }
    
}
