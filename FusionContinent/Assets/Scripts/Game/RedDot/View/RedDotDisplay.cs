using FusionContinent;
using TMPro;
using UnityEngine;

namespace Game.RedDot.View
{
    public static class RedDotDisplay
    {
        public const string RedPointPrefab = "RedPoint";
        public const string ArrowPointPrefab = "ArrowPoint";

        public static void Apply(
            Transform host,
            ref GameObject dot,
            TMP_Text countText,
            int count,
            Vector2 anchoredPosition = default,
            string prefabAddress = RedPointPrefab)
        {
            if (string.IsNullOrEmpty(prefabAddress))
            {
                prefabAddress = RedPointPrefab;
            }

            if (dot != null && !IsPrefabMatch(dot, prefabAddress))
            {
                Object.Destroy(dot);
                dot = null;
            }

            if (dot == null)
            {
                Transform existing = FindChildByName(host, prefabAddress);
                if (existing != null)
                {
                    dot = existing.gameObject;
                }
                else
                {
                    GameObject asset = Global.ResLoader.LoadSync<GameObject>(prefabAddress);
                    dot = Object.Instantiate(asset, host);
                    RectTransform dotRect = dot.GetComponent<RectTransform>();
                    dotRect.anchorMin = Vector2.one;
                    dotRect.anchorMax = Vector2.one;
                    dotRect.anchoredPosition = anchoredPosition;
                    dotRect.pivot = Vector2.one;
                }
            }

            // 同级另一套提示节点互斥，避免红点与箭头同时亮
            HideSiblingHint(host, dot, RedPointPrefab);
            HideSiblingHint(host, dot, ArrowPointPrefab);

            dot.layer = host.gameObject.layer;
            dot.SetActive(count > 0);
            if (countText != null)
            {
                countText.text = count > 0 ? count.ToString() : string.Empty;
            }
        }

        static bool IsPrefabMatch(GameObject dot, string prefabAddress)
        {
            string name = dot.name;
            return name == prefabAddress
                   || name == prefabAddress + "(Clone)"
                   || name.StartsWith(prefabAddress);
        }

        static Transform FindChildByName(Transform host, string childName)
        {
            for (int i = 0; i < host.childCount; i++)
            {
                Transform child = host.GetChild(i);
                if (child.name == childName || child.name == childName + "(Clone)")
                {
                    return child;
                }
            }

            return host.Find(childName);
        }

        static void HideSiblingHint(Transform host, GameObject activeDot, string childName)
        {
            for (int i = 0; i < host.childCount; i++)
            {
                Transform child = host.GetChild(i);
                if (child.gameObject == activeDot)
                {
                    continue;
                }

                if (child.name == childName || child.name == childName + "(Clone)")
                {
                    child.gameObject.SetActive(false);
                }
            }
        }
    }
}
