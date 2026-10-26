using TMPro;
using UnityEngine;

namespace Game.RedDot.View
{
    public class RedDotView : MonoBehaviour
    {
        [SerializeField] RedNodeType _nodeType;
        [SerializeField] GameObject _dot;
        [SerializeField] TMP_Text _countText;
        [SerializeField] Vector2 _anchoredPosition;
        [SerializeField] string _dotPrefabAddress = RedDotDisplay.RedPointPrefab;

        void OnEnable()
        {
            RedDotService.Instance.Bind(_nodeType, OnCountChanged);
        }

        void OnDisable()
        {
            RedDotService.Instance.Unbind(_nodeType, OnCountChanged);
        }

        public void SetDotPrefab(string dotPrefabAddress)
        {
            if (string.IsNullOrEmpty(dotPrefabAddress))
            {
                dotPrefabAddress = RedDotDisplay.RedPointPrefab;
            }

            if (_dotPrefabAddress == dotPrefabAddress)
            {
                return;
            }

            _dotPrefabAddress = dotPrefabAddress;
            if (_dot != null)
            {
                Destroy(_dot);
                _dot = null;
            }
        }

        public void SetNodeType(RedNodeType nodeType)
        {
            if (isActiveAndEnabled)
            {
                RedDotService.Instance.Unbind(_nodeType, OnCountChanged);
            }

            _nodeType = nodeType;

            if (isActiveAndEnabled)
            {
                RedDotService.Instance.Bind(_nodeType, OnCountChanged);
            }
        }

        void OnCountChanged(int count)
        {
            RedDotDisplay.Apply(transform, ref _dot, _countText, count, _anchoredPosition, _dotPrefabAddress);
        }
    }
}
