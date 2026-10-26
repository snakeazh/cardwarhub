namespace Game.RedDot
{
    public interface ILocalRedDotRule
    {
        string Scope { get; }
        void Bind(LocalRedDotRuleContext context);
        void Unbind();
        int Evaluate(LocalRedDotKey key);
    }
}
