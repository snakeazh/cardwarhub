using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CarouselScrollSnap : MonoBehaviour
{
    public ScrollRect scrollRect;
    public RectTransform content;
    public float snapSpeed = 12f;

    public float scaleMax = 1.2f;
    public float scaleMin = 0.8f;

    private List<RectTransform> items = new List<RectTransform>();
    private bool dragging = false;
    private int currentIndex = 0;

    float cardSpacing;
    float cardWidth;

    void Start()
    {
        foreach (Transform t in content)
            items.Add(t as RectTransform);

        scrollRect.inertia = false;
        scrollRect.movementType = ScrollRect.MovementType.Unrestricted;

        // 计算卡牌宽度
        if (items.Count >= 2)
        {
            cardWidth = ((RectTransform)items[0]).rect.width;
            float x1 = items[0].anchoredPosition.x;
            float x2 = items[1].anchoredPosition.x;
            cardSpacing = Mathf.Abs(x2 - x1);
        }
    }

    void Update()
    {
        UpdateScale();

        if (!dragging)
        {
            AutoSnap();
        }
    }

    public void OnBeginDrag()
    {
        dragging = true;
    }

    public void OnEndDrag()
    {
        dragging = false;
        UpdateNearestIndex();
    }

    private void UpdateScale()
    {
        float centerX = scrollRect.viewport.TransformPoint(new Vector3(scrollRect.viewport.rect.width / 2, 0)).x;

        foreach (var card in items)
        {
            float dist = Mathf.Abs(centerX - card.position.x);
            float t = Mathf.Clamp01(dist / 300f);
            float scale = Mathf.Lerp(scaleMax, scaleMin, t);

            card.localScale = Vector3.Lerp(card.localScale, Vector3.one * scale, Time.deltaTime * 10f);
        }
    }

    private void UpdateNearestIndex()
    {
        float centerX = scrollRect.viewport.TransformPoint(new Vector3(scrollRect.viewport.rect.width / 2, 0)).x;

        float nearest = float.MaxValue;
        int nearestIdx = currentIndex;

        for (int i = 0; i < items.Count; i++)
        {
            float d = Mathf.Abs(centerX - items[i].position.x);
            if (d < nearest)
            {
                nearest = d;
                nearestIdx = i;
            }
        }

        currentIndex = nearestIdx;
    }

    private void AutoSnap()
    {
        Vector2 targetPos = GetTargetAnchoredPos(currentIndex);
        content.anchoredPosition = Vector2.Lerp(content.anchoredPosition, targetPos, Time.deltaTime * snapSpeed);
    }

    private Vector2 GetTargetAnchoredPos(int index)
    {
        float offset = (cardSpacing * index);
        float centerOffset = scrollRect.viewport.rect.width / 2 - cardWidth / 2;
        return new Vector2(-offset + centerOffset, content.anchoredPosition.y);
    }

    // 给外部调用
    public void SetSelect(int index)
    {
        index = Mathf.Clamp(index, 0, items.Count - 1);
        currentIndex = index;
        dragging = false;
    }
}
