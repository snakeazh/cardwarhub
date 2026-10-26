using System.Collections.Generic;
using UnityEngine;

namespace FusionContinent.Skill.Handler
{
    /// <summary>
    /// 技能节点处理器注册表
    /// </summary>
    public static class SkillHandlerRegistry
    {
        private static Dictionary<string, ISkillNodeHandler> _handlers = new Dictionary<string, ISkillNodeHandler>();
        private static bool _isInitialized = false;

        /// <summary>
        /// 注册处理器
        /// </summary>
        public static void Register(ISkillNodeHandler handler)
        {
            if (handler == null)
            {
                LogTool.LogError("注册的 Handler 不能为空");
                return;
            }

            var nodeType = handler.NodeType;
            if (string.IsNullOrEmpty(nodeType))
            {
                LogTool.LogError("Handler 类型不能为空");
                return;
            }

            _handlers[nodeType] = handler;
        }

        /// <summary>
        /// 批量注册处理器
        /// </summary>
        public static void Register(params ISkillNodeHandler[] handlers)
        {
            foreach (var handler in handlers)
            {
                Register(handler);
            }
        }

        /// <summary>
        /// 获取处理器
        /// </summary>
        public static ISkillNodeHandler Get(string nodeType)
        {
            if (!_isInitialized)
                Initialize();

            if (_handlers.TryGetValue(nodeType, out var handler))
            {
                return handler;
            }

            LogTool.LogWarning($"未找到节点处理器: {nodeType}");
            return null;
        }

        /// <summary>
        /// 获取所有处理器
        /// </summary>
        public static IReadOnlyDictionary<string, ISkillNodeHandler> GetAll()
        {
            if (!_isInitialized)
                Initialize();

            return _handlers;
        }

        /// <summary>
        /// 是否有指定类型的处理器
        /// </summary>
        public static bool HasHandler(string nodeType)
        {
            if (!_isInitialized)
                Initialize();

            return _handlers.ContainsKey(nodeType);
        }

        /// <summary>
        /// 初始化所有内置处理器
        /// </summary>
        public static void Initialize()
        {
            if (_isInitialized)
                return;

            // Action 节点
            Register(new CastHandler());
            Register(new ProjectileHandler());
            Register(new MultiProjectileHandler());
            Register(new MultiTargetProjectileHandler());
            Register(new WaveProjectileHandler());
            Register(new FanProjectileHandler());
            Register(new AoeDamageHandler());
            Register(new AoeSelectHandler());
            Register(new SelectTargetHandler());
            Register(new PullMonsterHandler());
            Register(new DelayHandler());
            Register(new PlayMixAnimationHandler());
            Register(new StopMixAnimationHandler());
            Register(new ReturnAnimationHandler());
            Register(new EffectHandler());
            Register(new AnimatedEffectHandler());
            Register(new RecycleEffectHandler());
            Register(new SoundHandler());
            Register(new ResetOffsetHandler());

            // Buff 节点
            Register(new ApplyBuffHandler());

            // Flow 控制节点
            Register(new BranchHandler());
            Register(new LoopHandler());
            Register(new LoopSequentialHandler());
            Register(new ForkHandler());

            _isInitialized = true;
            LogTool.Log($"技能节点处理器注册完成，共 {_handlers.Count} 个");
        }

        /// <summary>
        /// 清空所有处理器（用于测试）
        /// </summary>
        public static void Clear()
        {
            _handlers.Clear();
            _isInitialized = false;
        }
    }
}
