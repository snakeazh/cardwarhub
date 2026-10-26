
namespace Game.RedDot
{
    public abstract class LocalRedDotRuleBase : ILocalRedDotRule
    {
        protected LocalRedDotRuleContext Context { get; private set; }

        public abstract string Scope { get; }

        protected TScope GetScope<TScope>() where TScope : LocalRedDotScopeBase
        {
            return LocalRedDotScopeBase.GetActive<TScope>(Scope);
        }

        protected bool TryGetNodeType(LocalRedDotKey key, out RedNodeType type)
        {
            if (Context?.Tree == null)
            {
                type = default;
                return false;
            }

            return Context.Tree.TryGetNodeType(key.Path, out type);
        }

        protected void MarkDirty(RedNodeType type)
        {
            Context?.Service.MarkLocalDirty(Scope, type);
        }

        public void Bind(LocalRedDotRuleContext context)
        {
            Context = context;
            OnBind();
        }

        public void Unbind()
        {
            OnUnbind();
            Context = null;
        }

        protected virtual void OnBind()
        {
        }

        protected virtual void OnUnbind()
        {
        }

        public abstract int Evaluate(LocalRedDotKey key);

        protected void MarkAllDirty()
        {
            Context?.Service.MarkLocalScopeDirty(Scope);
        }

        protected void MarkDirty(string path)
        {
            Context?.MarkDirty(LocalRedDotKey.Of(Scope, path));
        }
    }
}
