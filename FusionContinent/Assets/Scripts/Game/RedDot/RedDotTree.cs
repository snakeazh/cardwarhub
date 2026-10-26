using System;
using System.Collections.Generic;
using QFramework;

namespace Game.RedDot
{
    public sealed class RedDotTree
    {
        readonly string _scope;
        readonly Dictionary<string, RedDotNode> _nodesByPath = new Dictionary<string, RedDotNode>();
        readonly Dictionary<RedNodeType, string> _pathByType = new Dictionary<RedNodeType, string>();
        readonly Dictionary<string, RedNodeType> _typeByPath = new Dictionary<string, RedNodeType>();
        readonly List<RedDotNode> _rootNodes = new List<RedDotNode>();

        public RedDotTree(string scope)
        {
            _scope = scope;
        }

        public string Scope => _scope;

        public bool IsGlobal => _scope == RedDotTreeScopes.Global;

        public IReadOnlyCollection<string> Paths => _nodesByPath.Keys;

        public LocalRedDotKey Key(string path)
        {
            return LocalRedDotKey.Of(_scope, path);
        }

        public LocalRedDotKey Key(RedNodeType type)
        {
            return KeyOfType(type);
        }

        public RedDotNode EnsureNode(LocalRedDotKey key)
        {
            if (key.Scope != _scope)
            {
                throw new InvalidOperationException($"Red dot key scope mismatch: {key.Scope} != {_scope}");
            }

            if (_nodesByPath.TryGetValue(key.Path, out RedDotNode existing))
            {
                if (key.HasNodeType && !_pathByType.ContainsKey(key.NodeType))
                {
                    RegisterTypeMapping(key.NodeType, key.Path);
                }

                return existing;
            }

            RedDotNode node = new RedDotNode(key.Path);
            _nodesByPath[key.Path] = node;
            _rootNodes.Add(node);
            if (key.HasNodeType)
            {
                RegisterTypeMapping(key.NodeType, key.Path);
            }

            return node;
        }

        public void RegisterNodeType(RedNodeType type, string path)
        {
            EnsureNode(LocalRedDotKey.OfType(_scope, type, path));
        }

        public bool TryGetPath(RedNodeType type, out string path)
        {
            return _pathByType.TryGetValue(type, out path);
        }

        public bool TryGetNodeType(string path, out RedNodeType type)
        {
            return _typeByPath.TryGetValue(path, out type);
        }

        public bool TryGetKey(RedNodeType type, out LocalRedDotKey key)
        {
            if (_pathByType.TryGetValue(type, out string path))
            {
                key = LocalRedDotKey.OfType(_scope, type, path);
                return true;
            }

            key = default;
            return false;
        }

        public LocalRedDotKey KeyOfType(RedNodeType type)
        {
            if (!_pathByType.TryGetValue(type, out string path))
            {
                throw new KeyNotFoundException($"Red dot type not registered: {_scope}/{type}");
            }

            return LocalRedDotKey.OfType(_scope, type, path);
        }

        public RedDotNode GetNode(RedNodeType type)
        {
            return _pathByType.TryGetValue(type, out string path) ? GetNodeByPath(path) : null;
        }

        public RedDotNode GetNodeByPath(string path)
        {
            _nodesByPath.TryGetValue(path, out RedDotNode node);
            return node;
        }

        public RedDotNode GetNode(LocalRedDotKey key)
        {
            return GetNodeByPath(key.Path);
        }

        public void SetParent(string childPath, string parentPath)
        {
            SetParent(Key(childPath), Key(parentPath));
        }

        public void SetParent(LocalRedDotKey childKey, LocalRedDotKey parentKey)
        {
            RedDotNode child = EnsureNode(childKey);
            RedDotNode parent = EnsureNode(parentKey);
            if (child.Parent == parent)
            {
                return;
            }

            if (child.Parent != null)
            {
                child.Parent.RemoveChild(child);
            }
            else
            {
                _rootNodes.Remove(child);
            }

            parent.AddChild(child);
        }

        public void SetAggregateMode(LocalRedDotKey key, RedDotAggregateMode mode)
        {
            EnsureNode(key).SetAggregateMode(mode);
        }

        public void SetAggregateMode(RedNodeType type, RedDotAggregateMode mode)
        {
            if (TryGetKey(type, out LocalRedDotKey key))
            {
                SetAggregateMode(key, mode);
            }
        }

        public void RemoveNode(LocalRedDotKey key)
        {
            if (!_nodesByPath.TryGetValue(key.Path, out RedDotNode node))
            {
                return;
            }

            if (node.Parent != null)
            {
                node.Parent.RemoveChild(node);
            }
            else
            {
                _rootNodes.Remove(node);
            }

            RemoveSubtree(node);
        }

        void RemoveSubtree(RedDotNode node)
        {
            for (int i = node.Children.Count - 1; i >= 0; i--)
            {
                RemoveSubtree(node.Children[i]);
            }

            if (_typeByPath.TryGetValue(node.Path, out RedNodeType type))
            {
                _typeByPath.Remove(node.Path);
                _pathByType.Remove(type);
            }

            _nodesByPath.Remove(node.Path);
        }

        public int GetCount(LocalRedDotKey key)
        {
            RedDotNode node = GetNode(key);
            return node != null ? node.Count : 0;
        }

        public int GetCount(RedNodeType type)
        {
            RedDotNode node = GetNode(type);
            return node != null ? node.Count : 0;
        }

        public void SetLocal(LocalRedDotKey key, int count)
        {
            EnsureNode(key).SetLocalCount(count);
        }

        public void SetLocal(RedNodeType type, int count)
        {
            RedDotNode node = GetNode(type);
            node?.SetLocalCount(count);
        }

        public void Bind(LocalRedDotKey key, Action<int> listener)
        {
            EnsureNode(key).Bind(listener);
        }

        public void Bind(RedNodeType type, Action<int> listener)
        {
            RedDotNode node = GetNode(type);
            node?.Bind(listener);
        }

        public void Unbind(LocalRedDotKey key, Action<int> listener)
        {
            RedDotNode node = GetNode(key);
            if (node != null)
            {
                node.Unbind(listener);
            }
        }

        public void Unbind(RedNodeType type, Action<int> listener)
        {
            RedDotNode node = GetNode(type);
            if (node != null)
            {
                node.Unbind(listener);
            }
        }

        public BindableProperty<int> GetCountProperty(RedNodeType type)
        {
            RedDotNode node = GetNode(type);
            return node != null ? node.CountProperty : null;
        }

        public BindableProperty<int> GetCountProperty(LocalRedDotKey key)
        {
            return EnsureNode(key).CountProperty;
        }

        public void Clear()
        {
            _nodesByPath.Clear();
            _pathByType.Clear();
            _typeByPath.Clear();
            _rootNodes.Clear();
        }

        void RegisterTypeMapping(RedNodeType type, string path)
        {
            _pathByType[type] = path;
            _typeByPath[path] = type;
        }
    }
}
