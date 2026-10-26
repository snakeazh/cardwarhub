using UnityEngine;
using UnityEngine.EventSystems;

public class SwipePageController : MonoBehaviour, IBeginDragHandler, IEndDragHandler
{
    public System.Action OnSwipeLeft;
    public System.Action OnSwipeRight;

    private Vector2 startTouchPos;
    private float minSwipeDistance = 100f; // 最小滑动距离，防止误触

    public void OnBeginDrag(PointerEventData eventData)
    {
        startTouchPos = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Vector2 endTouchPos = eventData.position;
        float deltaX = endTouchPos.x - startTouchPos.x;

        if (Mathf.Abs(deltaX) > minSwipeDistance)
        {
            if (deltaX > 0)
            {
                OnSwipeRight?.Invoke(); // 向右滑（上一页）
            }
            else
            {
                OnSwipeLeft?.Invoke(); // 向左滑（下一页）
            }
        }
    }
}