namespace FusionContinent.Helper
{
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.EventSystems;

    /// <summary>
    /// 通用的左右滑动监听组件
    /// 挂在任意 UI 节点上（需要有 GraphicRaycaster 且带 Image 组件）
    /// 支持触屏和鼠标事件
    /// </summary>
    [DisallowMultipleComponent]
    public class UISwipeListener : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [Header("滑动识别的最小距离 (像素)")]
        public float swipeThreshold = 80f;

        [Header("是否打印调试日志")]
        public bool debugLog = false;

        [Header("向左滑动事件")]
        public UnityEvent onSwipeLeft;

        [Header("向右滑动事件")]
        public UnityEvent onSwipeRight;

        private Vector2 _startPos;
        private Vector2 _endPos;

        public void OnPointerDown(PointerEventData eventData)
        {
            _startPos = eventData.position;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _endPos = eventData.position;
            DetectSwipe();
        }

        private void DetectSwipe()
        {
            Vector2 delta = _endPos - _startPos;

            // 必须是水平滑动且超过阈值
            if (Mathf.Abs(delta.x) > swipeThreshold && Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            {
                if (delta.x > 0)
                {
                    // 从左往右滑动 （即右滑）
                    if (debugLog) LogTool.Log("[UISwipeListener] 向右滑动");
                    onSwipeRight?.Invoke();
                }
                else
                {
                    // 从右往左滑动 （即左滑）
                    if (debugLog) LogTool.Log("[UISwipeListener] 向左滑动");
                    onSwipeLeft?.Invoke();
                }
            }
        }

        // 可供代码直接绑定的接口
        public void AddListener(UnityAction left, UnityAction right)
        {
            if (left != null) onSwipeLeft.AddListener(left);
            if (right != null) onSwipeRight.AddListener(right);
        }

        public void RemoveAllListeners()
        {
            onSwipeLeft.RemoveAllListeners();
            onSwipeRight.RemoveAllListeners();
        }
    }
    
}