namespace FusionContinent.Guide
{
using UnityEngine;
using UnityEngine.UI;

public static class UITransformUtil
{
    // 获取一个 Canvas 的相关摄像机；Overlay 模式下传 null
    public static Camera GetCanvasCamera(Canvas canvas)
    {
        if (!canvas) return null;
        return canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
    }

    // 将目标 RectTransform 的外接矩形，转换到引导根节点（guideRoot）本地坐标系，返回局部 Rect
    public static Rect GetLocalRectIn(RectTransform guideRoot, Canvas guideCanvas, RectTransform target)
    {
        if (guideRoot == null || target == null) return default;

        // 目标所在 Canvas（用于世界->屏幕转换）
        var targetCanvas = target.GetComponentInParent<Canvas>();
        var targetCam = GetCanvasCamera(targetCanvas);
        var guideCam = GetCanvasCamera(guideCanvas);

        // 世界角点 -> 屏幕点
        var world = new Vector3[4];
        var screen = new Vector2[4];
        target.GetWorldCorners(world);
        for (int i = 0; i < 4; i++)
        {
            screen[i] = RectTransformUtility.WorldToScreenPoint(targetCam, world[i]);
        }

        // 屏幕点 -> 引导根的本地点
        var local = new Vector2[4];
        for (int i = 0; i < 4; i++)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(guideRoot, screen[i], guideCam, out local[i]);
        }

        // 构造局部 Rect（min/max）
        var min = local[0];
        var max = local[0];
        for (int i = 1; i < 4; i++)
        {
            min = Vector2.Min(min, local[i]);
            max = Vector2.Max(max, local[i]);
        }
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    // 仅转换一个点（比如目标中心点）到引导根的局部坐标
    public static Vector2 GetLocalPointIn(RectTransform guideRoot, Canvas guideCanvas, RectTransform target, Vector2 targetLocalPoint)
    {
        var targetCanvas = target.GetComponentInParent<Canvas>();
        var targetCam = GetCanvasCamera(targetCanvas);
        var guideCam = GetCanvasCamera(guideCanvas);

        var worldPoint = target.TransformPoint(targetLocalPoint);
        var screenPoint = RectTransformUtility.WorldToScreenPoint(targetCam, worldPoint);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(guideRoot, screenPoint, guideCam, out var localPoint);
        return localPoint;
    }
}
}