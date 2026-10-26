namespace Game.RedDot
{
    public sealed class LocalRedDotRuleContext
    {
        public RedDotService Service { get; }
        public RedDotTree Tree { get; }
        public string Scope { get; }

        public LocalRedDotRuleContext(RedDotService service, RedDotTree tree)
        {
            Service = service;
            Tree = tree;
            Scope = tree.Scope;
        }

        public void MarkDirty(LocalRedDotKey key)
        {
            Service.MarkLocalDirty(key);
        }
    }
}
