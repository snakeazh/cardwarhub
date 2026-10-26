
namespace Game.RedDot.Business
{
    public static class RedDotBusinessSetup
    {
        public static void RegisterAll(RedDotService service)
        {
            RedDotRuleBootstrap.RegisterAll(service.Registry);
            service.InitGlobalTree(GlobalRedDotTreeSetup.Build);
        }
    }
}
