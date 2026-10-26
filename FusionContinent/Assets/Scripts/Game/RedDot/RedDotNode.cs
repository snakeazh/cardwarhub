using System;
using System.Collections.Generic;
using QFramework;

namespace Game.RedDot
{
    public sealed class RedDotNode
    {
        readonly List<RedDotNode> _children = new List<RedDotNode>();
        RedDotAggregateMode _aggregateMode = RedDotAggregateMode.Sum;
        int _localCount;
        int _count;
        event Action<int> _onChanged;

        public RedDotNode(string path)
        {
            Path = path;
            CountProperty = new BindableProperty<int>(0);
        }

        public string Path { get; }
        public BindableProperty<int> CountProperty { get; }
        public RedDotNode Parent { get; private set; }
        public int LocalCount => _localCount;
        public int Count => _count;
        public IReadOnlyList<RedDotNode> Children => _children;

        public void SetAggregateMode(RedDotAggregateMode mode)
        {
            _aggregateMode = mode;
            RefreshUpward();
        }

        internal void AddChild(RedDotNode child)
        {
            _children.Add(child);
            child.Parent = this;
            RefreshUpward();
        }

        internal void RemoveChild(RedDotNode child)
        {
            if (_children.Remove(child))
            {
                child.Parent = null;
                RefreshUpward();
            }
        }

        public void SetLocalCount(int count)
        {
            if (_localCount == count)
            {
                return;
            }

            _localCount = count;
            RefreshUpward();
        }

        public void Bind(Action<int> listener)
        {
            _onChanged += listener;
            listener.Invoke(_count);
        }

        public void Unbind(Action<int> listener)
        {
            _onChanged -= listener;
        }

        internal void RefreshUpward()
        {
            int newCount = RedDotAggregate.Compute(_aggregateMode, _localCount, _children);
            if (newCount == _count)
            {
                return;
            }

            _count = newCount;
            CountProperty.Value = _count;
            _onChanged?.Invoke(_count);
            Parent?.RefreshUpward();
        }
    }
}
