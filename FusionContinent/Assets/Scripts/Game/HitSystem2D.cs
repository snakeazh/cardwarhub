using System.Collections.Generic;
using UnityEngine;

public static class HitSystem2D
{
    private static Collider2D[] results = new Collider2D[32];

    /// <summary>
    /// 圆形检测
    /// </summary>
    public static List<HitResult> Circle(
        Vector2 center,
        float radius,
        LayerMask layer)
    {
        List<HitResult> hits = new();

        int count = Physics2D.OverlapCircleNonAlloc(center, radius, results, layer);

        for (int i = 0; i < count; i++)
        {
            var col = results[i];

            hits.Add(new HitResult
            {
                target = col.gameObject,
                hitPoint = col.ClosestPoint(center),
            });
        }

        return hits;
    }

    /// <summary>
    /// 矩形（攻击盒）
    /// </summary>
    public static List<HitResult> Box(
        Vector2 center,
        Vector2 size,
        float angle,
        LayerMask layer)
    {
        List<HitResult> hits = new();

        int count = Physics2D.OverlapBoxNonAlloc(center, size, angle, results, layer);

        for (int i = 0; i < count; i++)
        {
            var col = results[i];

            hits.Add(new HitResult
            {
                target = col.gameObject,
                hitPoint = col.ClosestPoint(center),
            });
        }

        return hits;
    }

    /// <summary>
    /// 扇形（2D伪实现）
    /// </summary>
    public static List<HitResult> Sector(
        Vector2 center,
        Vector2 forward,
        float radius,
        float angle,
        LayerMask layer)
    {
        List<HitResult> hits = new();

        int count = Physics2D.OverlapCircleNonAlloc(center, radius, results, layer);

        for (int i = 0; i < count; i++)
        {
            var col = results[i];

            Vector2 dir = ((Vector2)col.transform.position - center).normalized;

            float dot = Vector2.Dot(forward.normalized, dir);
            float deg = Mathf.Acos(dot) * Mathf.Rad2Deg;

            if (deg <= angle * 0.5f)
            {
                hits.Add(new HitResult
                {
                    target = col.gameObject,
                    hitPoint = col.ClosestPoint(center),
                });
            }
        }

        return hits;
    }

    /// <summary>
    /// 射线检测（Physics2D.RaycastAll）
    /// </summary>
    public static List<HitResult> Ray(
        Vector2 origin,
        Vector2 direction,
        float distance,
        LayerMask layer)
    {
        List<HitResult> hits = new();

        RaycastHit2D[] rayResults = Physics2D.RaycastAll(origin, direction, distance, layer);

        foreach (var hit in rayResults)
        {
            hits.Add(new HitResult
            {
                target = hit.collider.gameObject,
                hitPoint = hit.point,
            });
        }

        return hits;
    }

    /// <summary>
    /// 调试绘制矩形（仅在 Editor 下可见）
    /// </summary>
    public static void DrawBoxGizmo(Vector2 center, Vector2 size, float angle, Color color)
    {
#if UNITY_EDITOR
        Vector3[] corners = GetRotatedRectPoints(center, size, angle);

        UnityEditor.Handles.color = color;
        UnityEditor.Handles.DrawLine(corners[0], corners[1]);
        UnityEditor.Handles.DrawLine(corners[1], corners[2]);
        UnityEditor.Handles.DrawLine(corners[2], corners[3]);
        UnityEditor.Handles.DrawLine(corners[3], corners[0]);
        // 对角线
        UnityEditor.Handles.DrawLine(corners[0], corners[2]);
        UnityEditor.Handles.DrawLine(corners[1], corners[3]);
        UnityEditor.Handles.color = Color.white;
#endif
    }

    /// <summary>
    /// 调试绘制扇形（仅在 Editor 下可见）
    /// </summary>
    public static void DrawSectorGizmo(Vector2 center, Vector2 forward, float radius, float angle, Color color)
    {
#if UNITY_EDITOR
        float halfAngle = angle * 0.5f * Mathf.Deg2Rad;
        float forwardAngle = Mathf.Atan2(forward.y, forward.x);
        float startAngle = forwardAngle - halfAngle;
        float endAngle = forwardAngle + halfAngle;

        // 画两条边线
        Vector2 p1 = center + new Vector2(Mathf.Cos(startAngle), Mathf.Sin(startAngle)) * radius;
        Vector2 p2 = center + new Vector2(Mathf.Cos(endAngle), Mathf.Sin(endAngle)) * radius;
        UnityEditor.Handles.color = color;
        UnityEditor.Handles.DrawLine(center, p1);
        UnityEditor.Handles.DrawLine(center, p2);

        // 画弧（多段线逼近）
        int segments = Mathf.CeilToInt(angle / 5f);
        Vector2 prev = p1;
        for (int i = 1; i <= segments; i++)
        {
            float a = Mathf.Lerp(startAngle, endAngle, (float)i / segments);
            Vector2 cur = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius;
            UnityEditor.Handles.DrawLine(prev, cur);
            prev = cur;
        }

        // 圆心
        UnityEditor.Handles.color = Color.yellow;
        UnityEditor.Handles.DrawWireDisc(center, Vector3.back, 0.2f);
        UnityEditor.Handles.color = Color.white;
#endif
    }

    private static Vector3[] GetRotatedRectPoints(Vector2 center, Vector2 size, float angleDeg)
    {
        float halfW = size.x * 0.5f;
        float halfH = size.y * 0.5f;
        float rad = angleDeg * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);

        Vector2 localTL = new Vector2(-halfW, halfH);
        Vector2 localTR = new Vector2(halfW, halfH);
        Vector2 localBL = new Vector2(-halfW, -halfH);
        Vector2 localBR = new Vector2(halfW, -halfH);

        Vector2 Rotate(Vector2 v) => new Vector2(
            v.x * cos - v.y * sin,
            v.x * sin + v.y * cos
        );

        return new Vector3[]
        {
            center + Rotate(localTL),
            center + Rotate(localTR),
            center + Rotate(localBR),
            center + Rotate(localBL),
        };
    }
}

public class HitResult
{
    public GameObject target;
    public Vector2 hitPoint;
}