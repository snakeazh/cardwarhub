using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Helper
{
    public class CommonScalableButton: MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [Header("缩放设置")]
        [Tooltip("按下时的缩放比例 (0-1)")]
        public float pressedScale = 0.9f;
        [Tooltip("缩放动画时间")]
        public float animationDuration = 0.1f;

        [Header("按钮事件")]
        public UnityEvent onClick; // 按钮点击事件（回弹完成后触发）
        public UnityEvent onBounceComplete; // 回弹动画完成事件

        private Vector3 originalScale;
        private bool isPressed = false;
        private Coroutine scaleCoroutine;
        public Transform target;
        void Start()
        {
            if (target  == null)
            {
                return;
            }
            originalScale = target.localScale;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (target  == null)
            {
                return;
            }
            if (isPressed) return;
        
            isPressed = true;
            if (scaleCoroutine != null)
            {
                StopCoroutine(scaleCoroutine);
            }
            scaleCoroutine = StartCoroutine(ScaleTo(originalScale * pressedScale));
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (target  == null)
            {
                return;
            }
            if (!isPressed) return;
        
            isPressed = false;
            if (scaleCoroutine != null)
            {
                StopCoroutine(scaleCoroutine);
            }
            scaleCoroutine = StartCoroutine(ScaleTo(originalScale, true));
        }

        private IEnumerator ScaleTo(Vector3 targetScale, bool invokeClickEvent = false)
        {
            Vector3 startScale = target.localScale;
            float elapsedTime = 0f;

            while (elapsedTime < animationDuration)
            {
                target.localScale = Vector3.Lerp(startScale, targetScale, elapsedTime / animationDuration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            target.localScale = targetScale;

            // 如果是回弹动画完成
            if (invokeClickEvent)
            {
                // 先触发回弹完成事件
                onBounceComplete?.Invoke();
            
                // 再触发点击事件
                onClick?.Invoke();
            }
        }
    }
}