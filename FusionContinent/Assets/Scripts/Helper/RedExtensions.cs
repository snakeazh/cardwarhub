using Config;
using FusionContinent;
using FusionContinent.UI.Components;
using Game.RedDot;
using Game.RedDot.View;
using QFramework;
using UnityEngine;
using UnityEngine.UI;

public static class RedExtensions
{
    public static void SetRedType(this Button button, RedNodeType type, string prefabAddress = null)
    {
        RedDotBinder binder = button.GetOrAddComponent<RedDotBinder>();
        if (!string.IsNullOrEmpty(prefabAddress))
        {
            binder.SetDotPrefab(prefabAddress);
        }

        binder.SetRedType(type);
    }

    public static void SetRedType(this UIPointerClickAndLongPress button, RedNodeType type, string prefabAddress = null)
    {
        RedDotBinder binder = button.GetOrAddComponent<RedDotBinder>();
        if (!string.IsNullOrEmpty(prefabAddress))
        {
            binder.SetDotPrefab(prefabAddress);
        }

        binder.SetRedType(type);
    }

    public static void SetRedType(this RectTransform button, RedNodeType type, string prefabAddress = null)
    {
        RedDotBinder binder = button.GetOrAddComponent<RedDotBinder>();
        if (!string.IsNullOrEmpty(prefabAddress))
        {
            binder.SetDotPrefab(prefabAddress);
        }

        binder.SetRedType(type);
    }

    public static void SetRedType(this Transform button, RedNodeType type, string prefabAddress = null)
    {
        RedDotBinder binder = button.GetOrAddComponent<RedDotBinder>();
        if (!string.IsNullOrEmpty(prefabAddress))
        {
            binder.SetDotPrefab(prefabAddress);
        }

        binder.SetRedType(type);
    }

    public static void SetLocalRedType(this RectTransform button, int redCount, string prefabAddress = null)
    {
        LocalRedDot host = button.GetOrAddComponent<LocalRedDot>();
        if (!string.IsNullOrEmpty(prefabAddress))
        {
            host.SetDotPrefab(prefabAddress);
        }

        host.SetRed(redCount);
    }

    public static void SetLocalRedType(this Button button, int redCount, string prefabAddress = null)
    {
        LocalRedDot host = button.GetOrAddComponent<LocalRedDot>();
        if (!string.IsNullOrEmpty(prefabAddress))
        {
            host.SetDotPrefab(prefabAddress);
        }

        host.SetRed(redCount);
    }
}
