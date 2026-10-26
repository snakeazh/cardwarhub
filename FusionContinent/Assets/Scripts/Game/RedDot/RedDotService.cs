using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.RedDot.Business;
using QFramework;
using UIFramework.Services;

namespace Game.RedDot
{
    public sealed class RedDotService : GameServiceBase
    {
        readonly RedDotTree _globalTree = new RedDotTree(RedDotTreeScopes.Global);
        readonly Dictionary<string, RedDotTree> _localTrees = new Dictionary<string, RedDotTree>();
        readonly Dictionary<string, List<ILocalRedDotRule>> _localRules = new Dictionary<string, List<ILocalRedDotRule>>();
        readonly Dictionary<string, HashSet<LocalRedDotKey>> _localDirtyKeys = new Dictionary<string, HashSet<LocalRedDotKey>>();
        readonly Dictionary<RedNodeType, List<IRedDotRule>> _globalRules = new Dictionary<RedNodeType, List<IRedDotRule>>();
        readonly HashSet<RedNodeType> _globalDirtyTypes = new HashSet<RedNodeType>();

        bool _globalInitialized;
        bool _flushScheduled;
        CancellationTokenSource _flushCts;

        public RedDotRuleRegistry Registry { get; } = new RedDotRuleRegistry();

        public override string ServiceName => "RedDotService";

        public static RedDotService Instance => GameServiceManager.Get<RedDotService>();

        public bool IsGlobalInitialized => _globalInitialized;

        public RedDotTree GlobalTree => _globalTree;

        public override async UniTask InitializeAsync()
        {
            await base.InitializeAsync();
        }

        public override void OnDestroy()
        {
            ShutdownGlobalTree();
            base.OnDestroy();
        }

        public override void Reset()
        {
            ShutdownGlobalTree();
            base.Reset();
        }

        public void InitGlobalTree(Action<RedDotTree> buildTree)
        {
            if (_globalInitialized)
            {
                ShutdownGlobalTree();
            }

            _globalTree.Clear();
            buildTree(_globalTree);
            BindGlobalRules();
            _globalInitialized = true;
            MarkAllGlobalDirty();
            FlushNow();
        }

        void BindGlobalRules()
        {
            foreach (RedNodeType type in Registry.GetRegisteredGlobalTypes())
            {
                List<IRedDotRule> rules = Registry.CreateGlobalRules(type);
                if (rules.Count == 0)
                {
                    continue;
                }

                _globalRules[type] = rules;
                RedDotRuleContext context = new RedDotRuleContext(this, type);
                for (int i = 0; i < rules.Count; i++)
                {
                    rules[i].Bind(context);
                }
            }
        }

        void ShutdownGlobalTree()
        {
            foreach (KeyValuePair<RedNodeType, List<IRedDotRule>> pair in _globalRules)
            {
                List<IRedDotRule> rules = pair.Value;
                for (int i = 0; i < rules.Count; i++)
                {
                    rules[i].Unbind();
                }
            }

            CancelScheduledFlush();
            _globalRules.Clear();
            _globalDirtyTypes.Clear();
            _globalTree.Clear();
            _globalInitialized = false;

            foreach (string scope in new List<string>(_localTrees.Keys))
            {
                DisposeLocalTree(scope);
            }
        }

        public int GetCount(RedNodeType type)
        {
            return _globalTree.GetCount(type);
        }

        public BindableProperty<int> GetCountProperty(RedNodeType type)
        {
            return _globalTree.GetCountProperty(type);
        }

        public void Bind(RedNodeType type, Action<int> listener)
        {
            _globalTree.Bind(type, listener);
        }

        public void Unbind(RedNodeType type, Action<int> listener)
        {
            _globalTree.Unbind(type, listener);
        }

        public void SetGlobalCount(RedNodeType type, int count)
        {
            _globalTree.SetLocal(type, count);
        }

        public void MarkDirty(RedNodeType type)
        {
            _globalDirtyTypes.Add(type);
            ScheduleFlush();
        }

        public void MarkAllGlobalDirty()
        {
            foreach (RedNodeType type in _globalRules.Keys)
            {
                _globalDirtyTypes.Add(type);
            }

            ScheduleFlush();
        }

        public void FlushNow()
        {
            CancelScheduledFlush();
            FlushGlobalDirty();
            FlushAllLocalDirty();
        }

        void ScheduleFlush()
        {
            if (_flushScheduled)
            {
                return;
            }

            _flushScheduled = true;
            _flushCts ??= new CancellationTokenSource();
            DeferredFlushAsync(_flushCts.Token).Forget();
        }

        async UniTaskVoid DeferredFlushAsync(CancellationToken token)
        {
            try
            {
                await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (token.IsCancellationRequested)
            {
                return;
            }

            _flushScheduled = false;
            FlushGlobalDirty();
            FlushAllLocalDirty();
        }

        void CancelScheduledFlush()
        {
            if (_flushCts != null)
            {
                _flushCts.Cancel();
                _flushCts.Dispose();
                _flushCts = null;
            }

            _flushScheduled = false;
        }

        void FlushGlobalDirty()
        {
            if (!_globalInitialized || _globalDirtyTypes.Count == 0)
            {
                return;
            }

            foreach (RedNodeType type in _globalDirtyTypes)
            {
                EvaluateGlobal(type);
            }

            _globalDirtyTypes.Clear();
        }

        void EvaluateGlobal(RedNodeType type)
        {
            if (_globalTree.GetNode(type) == null)
            {
                return;
            }

            if (!_globalRules.TryGetValue(type, out List<IRedDotRule> rules))
            {
                return;
            }

            int sum = 0;
            for (int i = 0; i < rules.Count; i++)
            {
                sum += rules[i].Evaluate();
            }

            _globalTree.SetLocal(type, sum);
        }

        public RedDotTree CreateLocalTree(string scope)
        {
            if (_localTrees.TryGetValue(scope, out RedDotTree existing))
            {
                DisposeLocalTree(scope);
            }

            RedDotTree tree = new RedDotTree(scope);
            _localTrees[scope] = tree;
            _localDirtyKeys[scope] = new HashSet<LocalRedDotKey>();
            BindLocalRules(scope, tree);
            return tree;
        }

        void BindLocalRules(string scope, RedDotTree tree)
        {
            List<ILocalRedDotRule> rules = Registry.CreateLocalRules(scope);
            _localRules[scope] = rules;
            LocalRedDotRuleContext context = new LocalRedDotRuleContext(this, tree);
            for (int i = 0; i < rules.Count; i++)
            {
                rules[i].Bind(context);
            }
        }

        public void DisposeLocalTree(string scope)
        {
            if (_localRules.TryGetValue(scope, out List<ILocalRedDotRule> rules))
            {
                for (int i = 0; i < rules.Count; i++)
                {
                    rules[i].Unbind();
                }

                _localRules.Remove(scope);
            }

            if (_localTrees.TryGetValue(scope, out RedDotTree tree))
            {
                tree.Clear();
                _localTrees.Remove(scope);
            }

            _localDirtyKeys.Remove(scope);
        }

        public RedDotTree GetLocalTree(string scope)
        {
            if (string.IsNullOrEmpty(scope))
            {
                return null;
            }

            _localTrees.TryGetValue(scope, out RedDotTree tree);
            return tree;
        }

        public int GetLocalCount(LocalRedDotKey key)
        {
            RedDotTree tree = GetLocalTree(key.Scope);
            return tree != null ? tree.GetCount(key) : 0;
        }

        public int GetLocalCount(string scope, RedNodeType type)
        {
            RedDotTree tree = GetLocalTree(scope);
            return tree != null ? tree.GetCount(type) : 0;
        }

        public BindableProperty<int> GetLocalProperty(LocalRedDotKey key)
        {
            RedDotTree tree = GetLocalTree(key.Scope);
            return tree.GetCountProperty(key);
        }

        public void WatchLocal(LocalRedDotKey key, Action<int> listener)
        {
            if (string.IsNullOrEmpty(key.Scope))
            {
                return;
            }

            RedDotTree tree = GetLocalTree(key.Scope);
            tree?.Bind(key, listener);
        }

        public void WatchLocal(string scope, RedNodeType type, Action<int> listener)
        {
            RedDotTree tree = GetLocalTree(scope);
            if (tree != null && tree.TryGetKey(type, out LocalRedDotKey key))
            {
                tree.Bind(key, listener);
            }
        }

        public void UnwatchLocal(LocalRedDotKey key, Action<int> listener)
        {
            RedDotTree tree = GetLocalTree(key.Scope);
            if (tree != null)
            {
                tree.Unbind(key, listener);
            }
        }

        public void UnwatchLocal(string scope, RedNodeType type, Action<int> listener)
        {
            RedDotTree tree = GetLocalTree(scope);
            if (tree != null && tree.TryGetKey(type, out LocalRedDotKey key))
            {
                tree.Unbind(key, listener);
            }
        }

        public void SetLocal(LocalRedDotKey key, int count)
        {
            RedDotTree tree = GetLocalTree(key.Scope);
            tree.SetLocal(key, count);
        }

        public void MarkLocalDirty(LocalRedDotKey key)
        {
            if (!_localDirtyKeys.TryGetValue(key.Scope, out HashSet<LocalRedDotKey> dirty))
            {
                return;
            }

            dirty.Add(key);
            ScheduleFlush();
        }

        public void MarkLocalDirty(string scope, RedNodeType type)
        {
            RedDotTree tree = GetLocalTree(scope);
            if (tree != null && tree.TryGetKey(type, out LocalRedDotKey key))
            {
                MarkLocalDirty(key);
            }
        }

        public void MarkLocalScopeDirty(string scope)
        {
            if (!_localDirtyKeys.TryGetValue(scope, out HashSet<LocalRedDotKey> dirty))
            {
                return;
            }

            RedDotTree tree = GetLocalTree(scope);
            if (tree == null)
            {
                return;
            }

            var paths = new List<string>(tree.Paths);
            for (int i = 0; i < paths.Count; i++)
            {
                dirty.Add(LocalRedDotKey.Of(scope, paths[i]));
            }

            ScheduleFlush();
        }

        void FlushAllLocalDirty()
        {
            foreach (string scope in new List<string>(_localDirtyKeys.Keys))
            {
                FlushLocalDirty(scope);
            }
        }

        void FlushLocalDirty(string scope)
        {
            if (!_localDirtyKeys.TryGetValue(scope, out HashSet<LocalRedDotKey> dirty) || dirty.Count == 0)
            {
                return;
            }

            RedDotTree tree = GetLocalTree(scope);
            if (!_localRules.TryGetValue(scope, out List<ILocalRedDotRule> rules))
            {
                dirty.Clear();
                return;
            }

            var dirtyKeys = new List<LocalRedDotKey>(dirty);
            for (int i = 0; i < dirtyKeys.Count; i++)
            {
                LocalRedDotKey key = dirtyKeys[i];
                int sum = 0;
                for (int j = 0; j < rules.Count; j++)
                {
                    sum += rules[j].Evaluate(key);
                }

                tree.SetLocal(key, sum);
            }

            dirty.Clear();
        }
    }
}
