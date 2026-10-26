using System;
using System.Collections.Generic;

namespace Game.RedDot
{
    public abstract class LocalRedDotScopeBase
    {
        static readonly Dictionary<string, LocalRedDotScopeBase> s_ActiveScopes = new Dictionary<string, LocalRedDotScopeBase>();

        protected RedDotTree Tree { get; private set; }

        protected RedDotService Service => RedDotService.Instance;

        public abstract string Scope { get; }

        public bool IsOpen => Tree != null;

        public static T GetActive<T>(string scope) where T : LocalRedDotScopeBase
        {
            return s_ActiveScopes.TryGetValue(scope, out LocalRedDotScopeBase active) ? active as T : null;
        }

        public static void RegisterRule(RedDotRuleRegistry registry, string scope, Func<ILocalRedDotRule> factory)
        {
            registry.RegisterLocal(scope, factory);
        }

        protected LocalRedDotKey Key(string path)
        {
            return LocalRedDotKey.Of(Scope, path);
        }

        protected LocalRedDotKey Key(RedNodeType type)
        {
            return Tree.KeyOfType(type);
        }

        protected void EnsureNode(string path)
        {
            Tree?.EnsureNode(Key(path));
        }

        protected void EnsureNode(RedDotTree tree, string path)
        {
            tree.EnsureNode(Key(path));
        }

        protected void RegisterNodeType(RedDotTree tree, RedNodeType type, string path)
        {
            tree.RegisterNodeType(type, path);
        }

        protected void RegisterNodeType(RedNodeType type, string path)
        {
            Tree?.RegisterNodeType(type, path);
        }

        public bool TryGetNodeType(string path, out RedNodeType type)
        {
            if (Tree != null && Tree.TryGetNodeType(path, out type))
            {
                return true;
            }

            type = default;
            return false;
        }

        public void Open()
        {
            RedDotTree existing = Service.GetLocalTree(Scope);
            if (existing != null)
            {
                Tree = existing;
                s_ActiveScopes[Scope] = this;
                MarkAllDirty();
                Service.FlushNow();
                return;
            }

            s_ActiveScopes[Scope] = this;
            Tree = Service.CreateLocalTree(Scope);
            BuildNodes(Tree);
            OnOpened();
            MarkAllDirty();
            Service.FlushNow();
        }

        public void Close()
        {
            if (Service.GetLocalTree(Scope) == null)
            {
                return;
            }

            OnClosing();
            Service.DisposeLocalTree(Scope);
            Tree = null;

            if (s_ActiveScopes.TryGetValue(Scope, out LocalRedDotScopeBase active) && active == this)
            {
                s_ActiveScopes.Remove(Scope);
            }

            ResetState();
        }

        protected abstract void BuildNodes(RedDotTree tree);

        protected virtual void OnOpened()
        {
        }

        protected virtual void OnClosing()
        {
        }

        protected virtual void ResetState()
        {
        }

        public void MarkDirty(string path)
        {
            Service.MarkLocalDirty(Key(path));
        }

        public void MarkDirty(RedNodeType type)
        {
            Service.MarkLocalDirty(Key(type));
        }

        public void MarkDirty(params string[] paths)
        {
            for (int i = 0; i < paths.Length; i++)
            {
                MarkDirty(paths[i]);
            }
        }

        public void MarkDirty(params RedNodeType[] types)
        {
            for (int i = 0; i < types.Length; i++)
            {
                MarkDirty(types[i]);
            }
        }

        public void MarkAllDirty()
        {
            Service.MarkLocalScopeDirty(Scope);
        }
    }
}
