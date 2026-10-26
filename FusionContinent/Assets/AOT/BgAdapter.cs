using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class BgAdapter : MonoBehaviour
{
    private static readonly Vector2 DesignSize = new Vector2(1125, 2436);

    private RectTransform _rectTransform;
    private float _designRatio;

    private void Awake()
    {
        _rectTransform = (RectTransform)transform;
        _designRatio = DesignSize.x / DesignSize.y;
    }

    private void OnEnable()
    {
        Adapt();
    }

    private void OnRectTransformDimensionsChange()
    {
        Adapt();
    }

    private void Adapt()
    {
        if (_rectTransform == null)
        {
            _rectTransform = (RectTransform)transform;
            _designRatio = DesignSize.x / DesignSize.y;
        }

        var parent = (RectTransform)_rectTransform.parent;
        float parentWidth = parent.rect.width;
        float parentHeight = parent.rect.height;
        float currentRatio = parentWidth / parentHeight;

        if (currentRatio < _designRatio)
        {
            ApplyVerticalStretch(parentWidth, parentHeight);
        }
        else
        {
            ApplyHorizontalStretch(parentWidth, parentHeight);
        }
    }

    /// <summary>
    /// 水平居中 + 竖向拉伸（Center-Stretch）
    /// </summary>
    private void ApplyVerticalStretch(float parentWidth, float parentHeight)
    {
        _rectTransform.anchorMin = new Vector2(0.5f, 0f);
        _rectTransform.anchorMax = new Vector2(0.5f, 1f);
        _rectTransform.pivot = new Vector2(0.5f, 0.5f);
        _rectTransform.anchoredPosition = Vector2.zero;

        float width = parentHeight * _designRatio;
        if (width < parentWidth)
        {
            width = parentWidth;
        }

        _rectTransform.sizeDelta = new Vector2(width, 0f);
    }

    /// <summary>
    /// 横向拉伸 + 垂直居中（Stretch-Middle）
    /// </summary>
    private void ApplyHorizontalStretch(float parentWidth, float parentHeight)
    {
        _rectTransform.anchorMin = new Vector2(0f, 0.5f);
        _rectTransform.anchorMax = new Vector2(1f, 0.5f);
        _rectTransform.pivot = new Vector2(0.5f, 0.5f);
        _rectTransform.anchoredPosition = Vector2.zero;

        float height = parentWidth / _designRatio;
        if (height < parentHeight)
        {
            height = parentHeight;
        }

        _rectTransform.sizeDelta = new Vector2(0f, height);
    }
}
