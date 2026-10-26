using System;
using System.Collections.Generic;
using Config;
using DG.Tweening;
using FusionContinent;
using FusionContinent.Skill;
using UnityEngine;

namespace Game.Skill.Logic
{
    /**
     * 墙壁反弹子弹
     * 
     * @return 
     * @author Zpp
     * @create 2025/11/26
     **/
    public class WallReboundBullet : MonoBehaviour
    {
        [Header("子弹设置")]
        public float speed = 300f;
        public float lifeTime = 5f;
        public LayerMask wallLayer;
        public LayerMask enemyLayer; // 添加敌人层级

        private Vector2 _direction;
        private Tween _moveTween;
        private RectTransform _rectTransform;
        private bool _isWallRebound;
        
        private float _elapsedTime;
        private bool _hasPassedAngleWall = false; // 新增：标记是否已经通过anglewalldown
        private HashSet<string> hitTargets = new HashSet<string>(); // 用于防止对同一个目标造成多次伤害

        private ISkill _skillCard;

        public void SetElectricBall(ISkill skillCard, Vector3 direction, float missileSpeed)
        {
            this.speed = missileSpeed;
            this._skillCard = skillCard;
            if (this.speed < 1000f) this.speed = 1000f;

            this.lifeTime = skillCard.Attributes.GetFinal(AttributeType.SkillDuration);
            this._isWallRebound = skillCard.Attributes.GetFinal(AttributeType.SkillWallRebound) > 0f;
            
            Initialize(direction);
        }
        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        private void Initialize(Vector3 direction)
        {
            _direction = direction;
            
            _elapsedTime = 0f;
            _hasPassedAngleWall = false; // 重置标记
            this.hitTargets = new HashSet<string>();
            MoveBullet();
            DOTween.To(v => { }, 0, 0, lifeTime).SetEase(Ease.Linear).onComplete += () =>
            {
                Destroy(this.gameObject);
            };
        }

        private void FixedUpdate()
        {
            _elapsedTime += Time.deltaTime;
            
            // 检测是否已经通过anglewalldown
            if (!_hasPassedAngleWall && this.transform.position.y > GameLand.Default.anglewalldown.position.y)
            {
                _hasPassedAngleWall = true; // 标记为已通过
            }
            
            if(!_hasPassedAngleWall) return;
            
            // 碰撞检测
            Vector2 worldPos = _rectTransform.position;
            Vector2 rayDir = _direction;
            float rayDistance = 1f;

            // 墙壁碰撞检测
            if (_isWallRebound)
            {
                RaycastHit2D wallHit = Physics2D.Raycast(worldPos, rayDir, rayDistance, wallLayer);
                if (wallHit.collider != null)
                {
                    // 如果是anglewalldown且还没有通过，则不触发反弹
                    if (wallHit.collider.transform == GameLand.Default.anglewalldown && !_hasPassedAngleWall)
                    {
                        // 第一次碰撞到anglewalldown，不反弹，直接穿透
                        return;
                    }

                    // 获取反射方向
                    Vector2 normal = wallHit.normal;
                    Vector2 reflectDir = Vector2.Reflect(_direction, normal).normalized;
                    _direction = reflectDir;

                    // 修正旋转角度计算（子弹头朝上时的正确角度）
                    float newAngle = Mathf.Atan2(reflectDir.y, reflectDir.x) * Mathf.Rad2Deg;
                    _rectTransform.rotation = Quaternion.Euler(0, 0, newAngle - 90);

                    // 清空已命中目标列表
                    hitTargets.Clear();

                    // 重新移动
                    _moveTween.Kill();
                    MoveBullet();
                }
            }

            // 敌人伤害检测（使用射线检测）
            RaycastHit2D enemyHit = Physics2D.Raycast(worldPos, rayDir, rayDistance, enemyLayer);
            if (enemyHit.collider != null)
            {
                // 确保只检测到HitBox
                if (enemyHit.collider.name == "HitBox")
                {
                    MonsterCard hitTarget = enemyHit.collider.transform.parent.GetComponent<MonsterCard>();
                    if (hitTarget == null || !hitTarget.enabled)
                    {
                        return;
                    }
                    
                    if (hitTargets.Add(hitTarget.uniqueId))
                    {
                        // 受伤坐标位置
                        var damageHitPos = enemyHit.collider.bounds.ClosestPoint(worldPos);
                        try
                        {
                            hitTarget.ReceiveDamage(MonsterDamageRequest.ForSkillSumTypes(hitTarget, _skillCard, damageHitPos));
                        }
                        catch (Exception e)
                        {
                            LogTool.LogError($"伤害处理错误: {e.Message}");
                        }
                    }
                }
            }
        }

        private void MoveBullet()
        {
            Vector2 startPos = _rectTransform.anchoredPosition;
            float remainingTime = lifeTime - _elapsedTime;

            // 关键：根据当前旋转角度计算移动方向
            // float currentZRotation = _rectTransform.eulerAngles.z + 90;
            // _direction = Quaternion.Euler(0, 0, currentZRotation) * Vector2.right;
            
            // 计算目标位置
            Vector2 targetPos = startPos + (_direction * speed * remainingTime);

            _moveTween = _rectTransform.DOAnchorPos(targetPos, remainingTime)
                .SetEase(Ease.Linear)
                .OnUpdate(() =>
                {
                });
        }
        
        private void OnDestroy()
        {
            _moveTween?.Kill();
        }
    }
}