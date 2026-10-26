using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class LoopCarousel : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("References (set in Inspector)")]
    public ScrollRect scrollRect;
    public RectTransform viewport;
    public RectTransform content;
    public List<RectTransform> items = new List<RectTransform>();

    [Header("Layout & motion")]
    public float spacing = 400f;        // item 间距（像素）
    public float snapSpeed = 10f;       // 吸附速度
    public float swipeThreshold = 50f;  // 拖动阈值

    [Header("Scale")]
    public float maxScale = 1.2f;
    public float minScale = 0.8f;
    public float scaleRange = 700f;

    // 状态
    bool isSnapping = false;
    bool isDraggingInViewport = false;
    Vector2 dragStartContentPos;
    Vector2 dragStartPointerPos;
    float targetX;
    int itemCount;
    float totalLength;

    void Start()
    {
        // 基础检查
        if (scrollRect == null || viewport == null || content == null || items == null || items.Count == 0)
        {
            LogTool.LogError("LoopCarouselRobust: references missing or items empty.");
            enabled = false;
            return;
        }

        itemCount = items.Count;
        totalLength = itemCount * spacing;

        // 强制初始化排布与纵向位置
        ArrangeItems();
        KeepVerticalCentered();
    }

    void Update()
    {
        // 强制 content 与 items 在 Y 轴居中，避免跑上面
        KeepVerticalCentered();

        // 缩放 & 循环逻辑（world-space 计算更稳定）
        UpdateScale();
        LoopItems();

        // 平滑吸附
        if (isSnapping)
            SmoothSnap();
    }

    // ----------------------------
    // 强制垂直居中：确保 y = 0，不会跑到上面
    // ----------------------------
    void KeepVerticalCentered()
    {
        // content 的 y 固定为 0
        Vector2 cpos = content.anchoredPosition;
        if (Mathf.Abs(cpos.y) > 0.001f)
            content.anchoredPosition = new Vector2(cpos.x, 0);

        // item 的 y 固定为 0（local anchoredPosition）
        foreach (var it in items)
        {
            Vector2 pos = it.anchoredPosition;
            if (Mathf.Abs(pos.y) > 0.001f)
                it.anchoredPosition = new Vector2(pos.x, 0);
        }
    }

    // ----------------------------
    // 均匀排列 items（X轴），并确保 Y=0
    // ----------------------------
    void ArrangeItems()
    {
        float start = 0f;
        for (int i = 0; i < items.Count; i++)
        {
            items[i].anchoredPosition = new Vector2(start + i * spacing, 0f);
            items[i].localScale = Vector3.one;
        }

        // 保证 content y 为 0
        content.anchoredPosition = new Vector2(content.anchoredPosition.x, 0f);
    }

    // ----------------------------
    // 缩放：基于世界坐标的中心距离
    // ----------------------------
    void UpdateScale()
    {
        Vector3 viewportCenterWorld = viewport.TransformPoint(viewport.rect.center);

        foreach (var item in items)
        {
            Vector3 itemCenterWorld = item.TransformPoint(item.rect.center);
            float dist = Vector3.Distance(itemCenterWorld, viewportCenterWorld);
            float t = Mathf.Clamp01(dist / scaleRange);
            float s = Mathf.Lerp(maxScale, minScale, t);
            item.localScale = new Vector3(s, s, 1f);
        }
    }

    // ----------------------------
    // 无限循环：使用 world x 边界判断
    // ----------------------------
    void LoopItems()
    {
        // 计算视口左右世界边界（扩展 spacing 保证平滑）
        Vector3 leftEdgeWorld = viewport.TransformPoint(new Vector3(-spacing, 0f, 0f));
        Vector3 rightEdgeWorld = viewport.TransformPoint(new Vector3(viewport.rect.width + spacing, 0f, 0f));

        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            Vector3 itemCenterWorld = item.TransformPoint(item.rect.center);

            if (itemCenterWorld.x < leftEdgeWorld.x)
            {
                item.anchoredPosition += new Vector2(items.Count * spacing, 0f);
            }
            else if (itemCenterWorld.x > rightEdgeWorld.x)
            {
                item.anchoredPosition -= new Vector2(items.Count * spacing, 0f);
            }
        }
    }

    // ----------------------------
    // 指针/拖动相关：仅在 Viewport 内开始才有效
    // ----------------------------
    public void OnBeginDrag(PointerEventData eventData)
    {
        // 只有当按下的位置在 viewport 内部时才视为有效拖动
        if (RectTransformUtility.RectangleContainsScreenPoint(viewport, eventData.position, eventData.pressEventCamera))
        {
            isDraggingInViewport = true;
            isSnapping = false;

            dragStartContentPos = content.anchoredPosition;
            dragStartPointerPos = eventData.position;

            // 在开始拖动时关闭吸附（避免中断）
            scrollRect.inertia = true;
        }
        else
        {
            isDraggingInViewport = false;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDraggingInViewport)
            return;

        // 如果在拖动过程中指针移出 Viewport，则认为拖动取消并吸附到最近 item
        if (!RectTransformUtility.RectangleContainsScreenPoint(viewport, eventData.position, eventData.pressEventCamera))
        {
            isDraggingInViewport = false;
            // 立即吸附到最近
            SnapToIndex(GetNearestIndex());
            return;
        }

        // 否则，正常由 ScrollRect 处理拖动行为（我们不在这里直接修改 content）
        // 但可以根据需要做额外约束（例如禁止纵向移动）
        // 强制 content.y = 0 保证不纵移
        Vector2 cpos = content.anchoredPosition;
        if (Mathf.Abs(cpos.y) > 0.001f)
            content.anchoredPosition = new Vector2(cpos.x, 0f);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDraggingInViewport)
        {
            // 如果拖动并非在 viewport 内开始，直接吸附最近并返回
            SnapToIndex(GetNearestIndex());
            isDraggingInViewport = false;
            return;
        }

        isDraggingInViewport = false;

        // 计算拖拽位移（在 content 坐标系）
        float dragDelta = content.anchoredPosition.x - dragStartContentPos.x;

        if (Mathf.Abs(dragDelta) > swipeThreshold)
        {
            int dir = dragDelta > 0 ? -1 : 1;
            SnapToIndex(GetNearestIndex() + dir);
        }
        else
        {
            SnapToIndex(GetNearestIndex());
        }
    }

    // ----------------------------
    // 找到最接近中心的 item index（基于世界坐标中心）
    // ----------------------------
    int GetNearestIndex()
    {
        Vector3 viewportCenterWorld = viewport.TransformPoint(viewport.rect.center);

        int best = 0;
        float bestDist = float.MaxValue;

        for (int i = 0; i < items.Count; i++)
        {
            Vector3 itemCenterWorld = items[i].TransformPoint(items[i].rect.center);
            float dist = Vector3.Distance(itemCenterWorld, viewportCenterWorld);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = i;
            }
        }

        return best;
    }

    // ----------------------------
    // 吸附到某 index（支持循环）
    // ----------------------------
    public void SnapToIndex(int index)
    {
        if (items == null || items.Count == 0) return;

        index = ((index % items.Count) + items.Count) % items.Count;

        // 目标 item 的本地 anchoredPosition.x
        float itemX = items[index].anchoredPosition.x;

        // 视口中心相对于 content 的目标 offset:
        // 我们要把 content.x 移动，使得 itemX 与 viewport 中心对齐
        float viewportCenterLocalX = -content.anchoredPosition.x + (viewport.rect.width * 0.5f);
        float diff = viewportCenterLocalX - itemX; // 正数表示 item 在左边，需要把 content 向左移（x-）

        targetX = content.anchoredPosition.x + diff;
        isSnapping = true;

        // 吸附时禁用 inertia，避免冲突
        scrollRect.inertia = false;
    }

    void SmoothSnap()
    {
        Vector2 newPos = Vector2.Lerp(content.anchoredPosition, new Vector2(targetX, 0f), Time.deltaTime * snapSpeed);
        content.anchoredPosition = newPos;

        if (Mathf.Abs(content.anchoredPosition.x - targetX) < 0.3f)
        {
            content.anchoredPosition = new Vector2(targetX, 0f);
            isSnapping = false;
            // 恢复 inertia
            scrollRect.inertia = true;
        }
    }

    // ----------------------------
    // 外部接口：点击 item 居中（可从 Button 调用）
    // ----------------------------
    public void CenterOnItem(RectTransform item)
    {
        int idx = items.IndexOf(item);
        if (idx >= 0) SnapToIndex(idx);
    }

    // ----------------------------
    // Debug 辅助：在 Editor 中手动刷新排布
    // ----------------------------
    #if UNITY_EDITOR
    [ContextMenu("Rearrange Items")]
    void EditorRearrange() { ArrangeItems(); }
    #endif
}
