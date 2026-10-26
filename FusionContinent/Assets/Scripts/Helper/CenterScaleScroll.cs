using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CenterScaleScroll : MonoBehaviour
{
    public ScrollRect scrollRect;
    public RectTransform content;
    public List<RectTransform> items = new List<RectTransform>();

    [Header("缩放参数")]
    public float maxScale = 1.0f;     // 中心最大缩放
    public float minScale = 0.7f;     // 最小缩放
    public float scaleRange = 300f;   // 中心影响范围（像素）

    void Update()
    {
        ScaleItems();
    }

    void ScaleItems()
    {
        // 中心点位置（Content 局部坐标）
        float center = -content.anchoredPosition.x + scrollRect.viewport.rect.width / 2f;

        foreach (var item in items)
        {
            // item 中心点
            float itemCenter = item.anchoredPosition.x + item.rect.width / 2f;

            // 距离中心的绝对距离
            float distance = Mathf.Abs(center - itemCenter);

            // 根据距离计算缩放（距离越大，越接近 minScale）
            float t = Mathf.Clamp01(distance / scaleRange);
            float scale = Mathf.Lerp(maxScale, minScale, t);

            item.localScale = new Vector3(scale, scale, 1);
        }
    }
}