using System;
using System.Collections.Generic;

namespace Game.RedDot
{
    public sealed class RedDotRuleRegistry
    {
        readonly Dictionary<RedNodeType, List<Func<IRedDotRule>>> _globalFactories = new Dictionary<RedNodeType, List<Func<IRedDotRule>>>();
        readonly Dictionary<string, List<Func<ILocalRedDotRule>>> _localFactories = new Dictionary<string, List<Func<ILocalRedDotRule>>>();

        public void RegisterGlobal(RedNodeType type, Func<IRedDotRule> factory)
        {
            if (!_globalFactories.TryGetValue(type, out List<Func<IRedDotRule>> list))
            {
                list = new List<Func<IRedDotRule>>();
                _globalFactories[type] = list;
            }

            list.Add(factory);
        }

        public void RegisterGlobal<T>(RedNodeType type) where T : IRedDotRule, new()
        {
            RegisterGlobal(type, () => new T());
        }

        public void RegisterLocal(string scope, Func<ILocalRedDotRule> factory)
        {
            if (!_localFactories.TryGetValue(scope, out List<Func<ILocalRedDotRule>> list))
            {
                list = new List<Func<ILocalRedDotRule>>();
                _localFactories[scope] = list;
            }

            list.Add(factory);
        }

        public List<IRedDotRule> CreateGlobalRules(RedNodeType type)
        {
            List<IRedDotRule> rules = new List<IRedDotRule>();
            if (!_globalFactories.TryGetValue(type, out List<Func<IRedDotRule>> factories))
            {
                return rules;
            }

            for (int i = 0; i < factories.Count; i++)
            {
                rules.Add(factories[i]());
            }

            return rules;
        }

        public List<ILocalRedDotRule> CreateLocalRules(string scope)
        {
            List<ILocalRedDotRule> rules = new List<ILocalRedDotRule>();
            if (!_localFactories.TryGetValue(scope, out List<Func<ILocalRedDotRule>> factories))
            {
                return rules;
            }

            for (int i = 0; i < factories.Count; i++)
            {
                rules.Add(factories[i]());
            }

            return rules;
        }

        public IEnumerable<RedNodeType> GetRegisteredGlobalTypes()
        {
            return _globalFactories.Keys;
        }
    }
}
