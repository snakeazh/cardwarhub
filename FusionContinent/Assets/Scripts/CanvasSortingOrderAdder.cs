using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CanvasSortingOrderAdder : MonoBehaviour
{
    public int AddOrder;

    private void Start()
    {
        Canvas canvas = FindParentCanvas(transform.parent);
        if (canvas == null)
        {
            LogTool.LogWarning($"transform: {transform} 未找到 Canvas，操作终止。");
            return;
        }

        Canvas selfCanvas = GetComponent<Canvas>();
        if (selfCanvas != null)
        {
            selfCanvas.sortingOrder =canvas.sortingOrder+ AddOrder;
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
