using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FusionContinent.Skill.Config;
using UnityEngine;

namespace FusionContinent.Skill.Handler
{
    /// <summary>
    /// 特效回收节点处理器
    /// 触发时立即回收当前技能执行中创建的特效，替代固定超时等待
    /// </summary>
    public class RecycleEffectHandler : AsyncSkillNodeHandler
    {
        public override string NodeType => "RecycleEffect";

        public override async UniTask Execute(SkillNodeConfig node, SkillContext context)
        {
            var target = node.GetParamString("target", "All");
            var effectName = node.GetParamString("effectName", "");

            if (context.TrackedEffects.Count == 0)
            {
                LogTool.Log("[RecycleEffectHandler] 追踪列表为空，无需回收");
                return;
            }

            List<GameObject> toRecycle = new List<GameObject>();

            if (target == "ByName" && !string.IsNullOrEmpty(effectName))
            {
                foreach (var effect in context.TrackedEffects)
                {
                    if (effect != null && effect.activeInHierarchy && effect.name.Contains(effectName))
                    {
                        toRecycle.Add(effect);
                    }
                }
                LogTool.Log($"[RecycleEffectHandler] 按名称回收: {effectName}, 命中 {toRecycle.Count} 个");
            }
            else
            {
                foreach (var effect in context.TrackedEffects)
                {
                    if (effect != null && effect.activeInHierarchy)
                    {
                        toRecycle.Add(effect);
                    }
                }
                Debug.Log($"[RecycleEffectHandler] 全部回收: {toRecycle.Count} 个（跳过已回收的）");
            }

            foreach (var effect in toRecycle)
            {
                if (effect != null)
                {
                    EffectManager.Instance.RecycleEffect(effect);
                }
                context.TrackedEffects.Remove(effect);
            }
        }
    }
}
