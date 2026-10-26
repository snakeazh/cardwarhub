
namespace Game.RedDot
{
    public sealed class RedDotRuleContext
    {
        public RedDotService Service { get; }
        public RedNodeType NodeType { get; }

        public RedDotRuleContext(RedDotService service, RedNodeType nodeType)
        {
            Service = service;
            NodeType = nodeType;
        }

        public void MarkDirty()
        {
            Service.MarkDirty(NodeType);
        }
    }
}
