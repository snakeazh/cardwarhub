using System;

namespace Game.Skill.Logic
{
    using UnityEngine;
    using DG.Tweening;

    public class FireballDropEffect : MonoBehaviour
    {
        [Tooltip("起点到落点的高度差")]
        public float height = 6f;

        // ===== 事件 =====
        // public event Action<Vector3> OnHit;       // 命中事件
        
        private Tween _moveTween;
        private Vector3 _targetPos;

        void OnDisable()
        {
            _moveTween?.Kill();
            _moveTween = null;
            // OnHit = null;
        }

        /// <summary>
        /// 播放火球
        /// </summary>
        public void Play(Vector3 targetPos, float dropSpeed = 18f, float dropAngle = 89f)
        {
            gameObject.SetActive(true);
            
            _targetPos = targetPos;

            // 角度 → 计算水平偏移（固定 X 方向）
            dropAngle = Mathf.Max(dropAngle, 1f);
            float rad = dropAngle * Mathf.Deg2Rad;
            float horizontalOffset = height / Mathf.Tan(rad);

            // 起点（默认向 X 轴正方向偏移）
            Vector3 startPos = targetPos
                               + Vector3.right * horizontalOffset
                               + Vector3.up * height;

            transform.position = startPos;
            transform.rotation = Quaternion.Euler(0, 0, -dropAngle);

            float distance = Vector3.Distance(startPos, targetPos);
            float duration = distance / dropSpeed;

            _moveTween?.Kill();
            _moveTween = transform.DOMove(targetPos, duration);
        }

        
    }

}