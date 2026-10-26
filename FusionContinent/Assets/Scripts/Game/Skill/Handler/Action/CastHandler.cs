using Cysharp.Threading.Tasks;
using FusionContinent.Skill.Config;
using UnityEngine;

namespace FusionContinent.Skill.Handler
{
    /// <summary>
    /// 施法节点处理器
    /// </summary>
    public class CastHandler : AsyncSkillNodeHandler
    {
        public override string NodeType => "Cast";
        public override int Priority => 100;

        public override async UniTask Execute(SkillNodeConfig node, SkillContext context)
        {
            LogTool.Log("现在没有cast  cast只是一个开始索引");
        }

        public override bool Validate(SkillNodeConfig node, out string error)
        {
            error = null;
            return true;
        }
    }
}
