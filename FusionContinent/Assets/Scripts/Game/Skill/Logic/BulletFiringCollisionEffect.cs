using System;
using DG.Tweening;
using FusionContinent;
using UnityEngine;

namespace Game.Skill.Logic
{
    // 子弹发射碰撞效果
    public class BulletFiringCollisionEffect : MonoBehaviour
    {
        private Tween _moveTween;
        private bool _isCollision;
        
        // ===== 事件 =====
        public event Action OnComplete; // 完成事件
        public event Action<Vector3> OnHit; // 命中事件
        
        void OnDisable()
        {
            OnHit = null;
        }
        
        // 发射子弹
        public void Launch(Vector3 direction, float speed)
        {
            _moveTween?.Kill();
            _isCollision = false;
            
            // 计算子弹朝向
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle - 90f);

            // 计算目标位置并开始移动
            Vector3 targetPosition = transform.position + direction * 100f;
            _moveTween = transform.DOMove(targetPosition, speed).SetEase(Ease.Linear).SetSpeedBased();
        }
        
        private void OnTriggerEnter2D(Collider2D collision)
        {
            // 如果已经触发过，直接返回
            if (_isCollision || collision.name != "HitBox")
            {
                return;
            }

            MonsterCard hitTarget = collision.transform.parent.GetComponent<MonsterCard>();
            if (hitTarget == null || !hitTarget.enabled)
            {
                return;
            }
        
            _moveTween.Kill();
            _isCollision = true;
            
            // 命中瞬间
            OnHit?.Invoke(transform.position);
            OnComplete?.Invoke();
        }

        private void FixedUpdate()
        {
            if (_isCollision) return;
            
            // 检测子弹是否超出屏幕范围
            if (!IsOnScreen(transform.position))
            {
                _isCollision = true;
                OnComplete?.Invoke();
            }
        }
        
        // 检测物体是否在屏幕内的方法
        private bool IsOnScreen(Vector3 position)
        {
            Vector3 screenPoint = Camera.main.WorldToViewportPoint(position);
            return screenPoint.x > 0 && screenPoint.x < 1 && screenPoint.y > 0 && screenPoint.y < 1;
        }
        
    }
}