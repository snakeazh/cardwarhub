using Config;
using Game.RedDot;
using FusionContinent;
using FusionContinent.Model;

namespace Game.RedDot.Business.Rules
{
    public abstract class RedDotRuleBase : IRedDotRule
    {
        protected RedDotRuleContext Context;

        protected virtual bool CanWatchItemAllChange => true;
        protected virtual bool CanWatchLevelUpdate => true;

        public abstract RedNodeType NodeType { get; }

        public abstract int Evaluate();

        public virtual void Bind(RedDotRuleContext context)
        {
            Context = context;
            OnBind();
            if (CanWatchItemAllChange)
            {
                Global.Publisher.Subscribe(MessagePublisher.CHANGE_ITEM_CHANGE, OnRedDotItemChange);
            }

            if (CanWatchLevelUpdate)
            {
                Global.Publisher.Subscribe(MessagePublisher.CHANGE_USER_LEVEL_INFO, OnLevelChange);
            }
        }

        public virtual void Unbind()
        {
            OnUnbind();
            if (CanWatchItemAllChange)
            {
                Global.Publisher.Unsubscribe(MessagePublisher.CHANGE_ITEM_CHANGE, OnRedDotItemChange);
            }

            if (CanWatchLevelUpdate)
            {
                Global.Publisher.Unsubscribe(MessagePublisher.CHANGE_USER_LEVEL_INFO, OnLevelChange);
            }

            Context = null;
        }

        protected virtual void OnBind()
        {
        }

        protected virtual void OnUnbind()
        {
        }

        void OnLevelChange(object obj)
        {
            Context.MarkDirty();
        }

        void OnRedDotItemChange(object obj)
        {
            Context.MarkDirty();
        }
    }
}
