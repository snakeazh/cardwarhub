using System;
using FusionContinent;
using FusionContinent.UI.Components;
using Game.RedDot.View;
using QFramework;
using UnityEngine;
using UnityEngine.UI;

namespace Game.RedDot
{
    public static class RedDotExtensions
    {
        public static void BindRedDot(this Button button, RedNodeType type, string dotPrefabAddress = null)
        {
            BindRedDot(button.transform, type, dotPrefabAddress);
        }

        public static void BindRedDot(this UIPointerClickAndLongPress button, RedNodeType type, string dotPrefabAddress = null)
        {
            BindRedDot(button.transform, type, dotPrefabAddress);
        }

        public static void BindRedDot(this RectTransform target, RedNodeType type, string dotPrefabAddress = null)
        {
            BindRedDot((Transform)target, type, dotPrefabAddress);
        }

        public static void BindRedDot(this Transform target, RedNodeType type, string dotPrefabAddress = null)
        {
            RedDotView view = target.GetOrAddComponent<RedDotView>();
            view.SetDotPrefab(dotPrefabAddress);
            view.SetNodeType(type);
        }

        public static void BindLocalRedDot(this RectTransform target, string scope, string path, string dotPrefabAddress = null)
        {
            target.GetOrAddComponent<LocalRedDotView>().Configure(scope, path, dotPrefabAddress);
        }

        public static void BindLocalRedDot(this Transform target, string scope, string path, string dotPrefabAddress = null)
        {
            target.GetOrAddComponent<LocalRedDotView>().Configure(scope, path, dotPrefabAddress);
        }

        public static void SetLocalRedCount(this Transform target, int count, Vector2 anchoredPosition = default)
        {
            if (target is RectTransform rectTransform)
            {
                SetLocalRedCount(rectTransform, count, anchoredPosition);
                return;
            }

            LocalRedDot host = target.gameObject.GetOrAddComponent<LocalRedDot>();
            host.SetRed(count, anchoredPosition);
        }

        public static void SetLocalRedCount(this RectTransform target, int count, Vector2 anchoredPosition = default)
        {
            LocalRedDot host = target.GetOrAddComponent<LocalRedDot>();
            host.SetRed(count, anchoredPosition);
        }

        public static void SetLocalRedCount(this Button target, int count, Vector2 anchoredPosition = default)
        {
            LocalRedDot host = target.GetOrAddComponent<LocalRedDot>();
            host.SetRed(count, anchoredPosition);
        }

        public static void MarkRedDotDirty(RedNodeType type)
        {
            RedDotService.Instance.MarkDirty(type);
        }

        public static void BindLocalRedDot(
            this Button button,
            LocalRedDotScopeBase scope,
            string path,
            string dotPrefabAddress = null)
        {
            button.BindLocalRedDot(scope.Scope, path, dotPrefabAddress);
        }

        public static void BindLocalRedDot(
            this Button button,
            LocalRedDotScopeBase scope,
            RedNodeType type,
            string dotPrefabAddress = null)
        {
            button.BindLocalRedDot(scope.Scope, type, dotPrefabAddress);
        }

        public static void BindLocalRedDot(
            this RectTransform target,
            LocalRedDotScopeBase scope,
            string path,
            string dotPrefabAddress = null)
        {
            target.BindLocalRedDot(scope.Scope, path, dotPrefabAddress);
        }

        public static void BindLocalRedDot(
            this RectTransform target,
            LocalRedDotScopeBase scope,
            RedNodeType type,
            string dotPrefabAddress = null)
        {
            target.BindLocalRedDot(scope.Scope, type, dotPrefabAddress);
        }

        public static void BindLocalRedDot(
            this Transform target,
            LocalRedDotScopeBase scope,
            string path,
            string dotPrefabAddress = null)
        {
            target.BindLocalRedDot(scope.Scope, path, dotPrefabAddress);
        }

        public static void BindLocalRedDot(
            this Transform target,
            LocalRedDotScopeBase scope,
            RedNodeType type,
            string dotPrefabAddress = null)
        {
            target.BindLocalRedDot(scope.Scope, type, dotPrefabAddress);
        }

        public static void BindLocalRedDot(this Button button, string scope, string path, string dotPrefabAddress = null)
        {
            button.GetOrAddComponent<LocalRedDotView>().Configure(scope, path, dotPrefabAddress);
        }

        public static void BindLocalRedDot(this Button button, string scope, RedNodeType type, string dotPrefabAddress = null)
        {
            button.GetOrAddComponent<LocalRedDotView>().Configure(scope, type, dotPrefabAddress);
        }

        public static void BindLocalRedDot(this RectTransform target, string scope, RedNodeType type, string dotPrefabAddress = null)
        {
            target.GetOrAddComponent<LocalRedDotView>().Configure(scope, type, dotPrefabAddress);
        }

        public static void BindLocalRedDot(this Transform target, string scope, RedNodeType type, string dotPrefabAddress = null)
        {
            target.GetOrAddComponent<LocalRedDotView>().Configure(scope, type, dotPrefabAddress);
        }

        public static void MarkLocalRedDotDirty(string scope, RedNodeType type)
        {
            RedDotService.Instance.MarkLocalDirty(scope, type);
        }

        public static void MarkLocalRedDotDirty(LocalRedDotKey key)
        {
            RedDotService.Instance.MarkLocalDirty(key);
        }
    }
}
