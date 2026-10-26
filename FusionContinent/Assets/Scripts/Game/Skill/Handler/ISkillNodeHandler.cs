using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using FusionContinent.Skill.Config;

namespace FusionContinent.Skill.Handler
{
    /// <summary>
    /// 技能节点处理器接口
    /// </summary>
    public interface ISkillNodeHandler
    {
        /// <summary>
        /// 处理的节点类型
        /// </summary>
        string NodeType { get; }

        /// <summary>
        /// 优先级（用于排序）
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// 是否支持异步执行
        /// </summary>
        bool IsAsync { get; }

        /// <summary>
        /// 执行节点
        /// </summary>
        UniTask Execute(SkillNodeConfig node, SkillContext context);

        /// <summary>
        /// 验证节点参数
        /// </summary>
        bool Validate(SkillNodeConfig node, out string error);
    }

    /// <summary>
    /// 异步节点处理器基类
    /// </summary>
    public abstract class AsyncSkillNodeHandler : ISkillNodeHandler
    {
        public abstract string NodeType { get; }
        public virtual int Priority => 0;
        public virtual bool IsAsync => true;

        public abstract UniTask Execute(SkillNodeConfig node, SkillContext context);

        public virtual bool Validate(SkillNodeConfig node, out string error)
        {
            error = null;
            return true;
        }
    }

    /// <summary>
    /// 同步节点处理器基类
    /// </summary>
    public abstract class SyncSkillNodeHandler : ISkillNodeHandler
    {
        public abstract string NodeType { get; }
        public virtual int Priority => 0;
        public virtual bool IsAsync => false;

        public virtual UniTask Execute(SkillNodeConfig node, SkillContext context)
        {
            ExecuteSync(node, context);
            return UniTask.CompletedTask;
        }

        public abstract void ExecuteSync(SkillNodeConfig node, SkillContext context);

        public virtual bool Validate(SkillNodeConfig node, out string error)
        {
            error = null;
            return true;
        }
    }
}
