using Game.RedDot;
using TMPro;
using UnityEngine;

namespace Game.RedDot.View
{
    public class LocalRedDotView : MonoBehaviour
    {
        [SerializeField] string _scope;
        [SerializeField] string _path;
        [SerializeField] RedNodeType _nodeType;
        [SerializeField] bool _useNodeType;
        [SerializeField] GameObject _dot;
        [SerializeField] TMP_Text _countText;
        [SerializeField] Vector2 _anchoredPosition;
        [SerializeField] string _dotPrefabAddress = RedDotDisplay.RedPointPrefab;

        LocalRedDotKey _key;
        bool _isWatching;

        void Awake()
        {
            ResolveKey();
        }

        void OnEnable()
        {
            ResolveKey();
            TryWatch();
        }

        void OnDisable()
        {
            TryUnwatch();
        }

        public void Configure(string scope, string path, string dotPrefabAddress = null)
        {
            TryUnwatch();

            _useNodeType = false;
            _scope = scope;
            _path = path;
            SetDotPrefab(dotPrefabAddress);
            ResolveKey();
            TryWatch();
        }

        public void Configure(string scope, RedNodeType type, string dotPrefabAddress = null)
        {
            TryUnwatch();

            _useNodeType = true;
            _scope = scope;
            _nodeType = type;
            SetDotPrefab(dotPrefabAddress);
            ResolveKey();
            TryWatch();
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

        public void RefreshWatch()
        {
            TryUnwatch();
            ResolveKey();
            TryWatch();
        }

        void ResolveKey()
        {
            if (string.IsNullOrEmpty(_scope))
            {
                _key = default;
                return;
            }

            if (_useNodeType)
            {
                RedDotTree tree = RedDotService.Instance.GetLocalTree(_scope);
                if (tree != null && tree.TryGetKey(_nodeType, out LocalRedDotKey typedKey))
                {
                    _key = typedKey;
                    return;
                }

                _key = default;
                return;
            }

            _key = LocalRedDotKey.Of(_scope, _path);
        }

        void TryWatch()
        {
            if (_isWatching || string.IsNullOrEmpty(_scope))
            {
                return;
            }

            if (!_useNodeType && string.IsNullOrEmpty(_path))
            {
                return;
            }

            if (RedDotService.Instance.GetLocalTree(_scope) == null)
            {
                return;
            }

            RedDotService.Instance.WatchLocal(_key, OnCountChanged);
            _isWatching = true;
            // Watch 时新建节点 count 默认为 0；MarkDirty 让帧末 Flush Evaluate，
            // 覆盖「ScrollView LateUpdate 才创建 item、错过同步 Flush」的首开场景。
            if (!string.IsNullOrEmpty(_key.Path))
            {
                RedDotService.Instance.MarkLocalDirty(_key);
            }
        }

        void TryUnwatch()
        {
            if (!_isWatching || string.IsNullOrEmpty(_scope))
            {
                return;
            }

            RedDotService.Instance.UnwatchLocal(_key, OnCountChanged);
            _isWatching = false;
        }

        void OnCountChanged(int count)
        {
            if (!this)
            {
                return;
            }

            RedDotDisplay.Apply(transform, ref _dot, _countText, count, _anchoredPosition, _dotPrefabAddress);
        }
    }
}
