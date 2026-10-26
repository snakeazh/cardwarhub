using Game.RedDot;
using Game.RedDot.View;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FusionContinent
{
    public class RedDotBinder : MonoBehaviour
    {
        public RedNodeType Key;
        public GameObject Dot;
        public TMP_Text CountText;
        public string DotPrefabAddress = RedDotDisplay.RedPointPrefab;

        public void SetDotPrefab(string prefabAddress)
        {
            if (string.IsNullOrEmpty(prefabAddress))
            {
                prefabAddress = RedDotDisplay.RedPointPrefab;
            }

            if (DotPrefabAddress == prefabAddress)
            {
                return;
            }

            DotPrefabAddress = prefabAddress;
            if (Dot != null)
            {
                Destroy(Dot);
                Dot = null;
            }
        }

        public void SetRedType(RedNodeType type)
        {
            if (Key == type)
            {
                return;
            }

            if (isActiveAndEnabled)
            {
                RedDotService.Instance.Unbind(Key, OnChanged);
            }

            Key = type;

            if (isActiveAndEnabled)
            {
                RedDotService.Instance.Bind(Key, OnChanged);
            }
        }

        public void SetRedType(RedNodeType type, string prefabAddress)
        {
            SetDotPrefab(prefabAddress);
            if (Key == type)
            {
                // 同节点仅切换预制体时，强制按当前 count 重绘
                if (isActiveAndEnabled)
                {
                    OnChanged(RedDotService.Instance.GetCount(type));
                }

                return;
            }

            SetRedType(type);
        }

        void OnEnable()
        {
            RedDotService.Instance.Bind(Key, OnChanged);
        }

        void OnDisable()
        {
            RedDotService.Instance.Unbind(Key, OnChanged);
        }

        void OnChanged(int count)
        {
            // 延迟刷新可能在对象销毁后的同一帧触发，避免访问已销毁的 Transform
            if (!this)
            {
                return;
            }

            RedDotDisplay.Apply(transform, ref Dot, CountText, count, default, DotPrefabAddress);
        }

        void OnDestroy()
        {
            if (Dot != null)
            {
                Object.Destroy(Dot);
                Dot = null;
            }
        }
    }
}
