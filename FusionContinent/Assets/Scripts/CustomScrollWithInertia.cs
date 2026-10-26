using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class CustomScrollWithInertia : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    public RectTransform content;
    public float dragSensitivity = 1f;
    public float decelerationRate = 0.95f;  // 减速系数
    public float snapThreshold = 20f;  // 自动吸附阈值
    
    private Vector2 velocity;
    private Vector2 lastPosition;
    private float timeSinceLastDrag;
    public bool IsLeft;
    

    private void Start()
    {
        //content=transform.parent.parent.parent.GetComponent<RectTransform>();

        if (this.name != "CardGroupSlot(Clone)")
        {
            Destroy(this);
        }
    }

    private void Update()
    {
        if (timeSinceLastDrag > 0.1f)  // 拖拽结束后0.1秒开始惯性滑动
        {
            // 应用惯性
            content.anchoredPosition += velocity * Time.deltaTime;
            if (!IsLeft)
            {
                content.anchoredPosition = new Vector2(0,content.anchoredPosition.y);
            }
            else
            {
                content.anchoredPosition = new Vector2(content.anchoredPosition.x,0);
            }
            // 减速
            velocity *= decelerationRate;
            
            // 边界检查
            CheckBounds();
            
            // 速度过小时停止
            if (velocity.magnitude < 10f)
            {
                velocity = Vector2.zero;
                SnapToNearest();  // 自动吸附到最近位置
            }
        }
        timeSinceLastDrag += Time.deltaTime;
    }
    
    public void OnBeginDrag(PointerEventData eventData)
    {
        velocity = Vector2.zero;
        lastPosition = eventData.position;
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        Vector2 delta = eventData.position - lastPosition;
        if (!IsLeft)
        {
            content.anchoredPosition += new Vector2(0, delta.y * dragSensitivity);
        }
        else
        {
            content.anchoredPosition += new Vector2(delta.x * dragSensitivity, 0);
        }

        
        // 计算速度（用于惯性）
        velocity = delta / Time.deltaTime;
        lastPosition = eventData.position;
        timeSinceLastDrag = 0f;
        
        CheckBounds();
    }
    
    public void OnEndDrag(PointerEventData eventData)
    {
        // 可以在这里添加释放后的逻辑
    }
    
    private void CheckBounds()
    {
        // 实现边界检查逻辑
        // 例如：if(content.anchoredPosition.x < minX) ...
    }
    
    private void SnapToNearest()
    {
        // 实现自动吸附到最近位置的逻辑
    }
}