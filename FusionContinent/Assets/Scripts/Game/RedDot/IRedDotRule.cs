
namespace Game.RedDot
{
    public interface IRedDotRule
    {
        RedNodeType NodeType { get; }
        void Bind(RedDotRuleContext context);
        void Unbind();
        int Evaluate();
    }
}
