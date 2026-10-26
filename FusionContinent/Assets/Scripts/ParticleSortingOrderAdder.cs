using UnityEngine;

public class ParticleSortingOrderAdder : MonoBehaviour
{
    public int addOrder = 1;
    void Start()
    {
        // 找到最近的 Canvas 向上
        Canvas canvas = FindParentCanvas(transform);
        if (canvas == null)
        {
            LogTool.LogWarning($"transform: {transform} 未找到 Canvas，操作终止。 直接修改自己的值");
        }

        int canvasSortingOrder = canvas?canvas.sortingOrder + addOrder:addOrder;

        // 处理所有子物体中的 ParticleSystemRenderer
        ParticleSystemRenderer[] particleRenderers = GetComponentsInChildren<ParticleSystemRenderer>(includeInactive: true);
        foreach (var renderer in particleRenderers)
        {
            renderer.sortingOrder += canvasSortingOrder;
            // 可选调试
           // LogTool.Log($"粒子 {renderer.gameObject.name} 的 sortingOrder 已加到 {renderer.sortingOrder}");
        }

        // 处理所有子物体中的 TrailRenderer
        TrailRenderer[] trailRenderers = GetComponentsInChildren<TrailRenderer>(includeInactive: true);
        foreach (var trailRenderer in trailRenderers)
        {
            trailRenderer.sortingOrder += canvasSortingOrder;
            // 可选调试
           // LogTool.Log($"拖尾 {trailRenderer.gameObject.name} 的 sortingOrder 已加到 {trailRenderer.sortingOrder}");
        }
    }

    /// <summary>
    /// 向上查找最近的 Canvas 组件
    /// </summary>
    Canvas FindParentCanvas(Transform current)
    {
        while (current != null)
        {
            var canvas = current.GetComponent<Canvas>();
            if (canvas != null)
                return canvas;

            current = current.parent;
        }

        return null;
    }
}