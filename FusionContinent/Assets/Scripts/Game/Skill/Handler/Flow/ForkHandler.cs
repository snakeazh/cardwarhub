using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FusionContinent.Skill.Config;
using UnityEngine;

namespace FusionContinent.Skill.Handler
{
    /// <summary>
    /// 分叉执行节点处理器
    /// 同时执行多个并行分支，如爆炎斩从1个分裂为3个斩击
    /// </summary>  
    public class ForkHandler : AsyncSkillNodeHandler
    {
        public override string NodeType => "Fork";

        public override async UniTask Execute(SkillNodeConfig node, SkillContext context)
        {
            var count = node.GetParamInt("count", 2);

            LogTool.Log($"[ForkHandler] 开始分叉: count={count}");

            var graphConfig = context.Config;
            if (graphConfig == null)
            {
                LogTool.LogWarning("[ForkHandler] SkillContext.Config 未设置");
                return;
            }

            var tasks = new List<UniTask>();
            for (int i = 0; i < count; i++)
            {
                var portId = $"fork_{i}";
                var portEdges = graphConfig.Edges
                    .Where(e => e.FromNodeId == node.NodeId && e.FromPortId == portId)
                    .ToList();

                if (portEdges.Count == 0)
                    continue;

                foreach (var edge in portEdges)
                {
                    tasks.Add(ExecuteNodeRecursive(edge.ToNodeId, context));
                }
            }

            if (tasks.Count == 0)
            {
                LogTool.Log("[ForkHandler] 没有分叉连接");
                return;
            }

            await UniTask.WhenAll(tasks);
            LogTool.Log("[ForkHandler] 分叉执行完成");
        }

        private async UniTask ExecuteNodeRecursive(string nodeId, SkillContext context)
        {
            var graphConfig = context.Config;
            if (graphConfig == null) return;

            var node = graphConfig.GetNode(nodeId);
            if (node == null) return;

            var handler = SkillHandlerRegistry.Get(node.NodeType);
            if (handler != null)
            {
                LogTool.Log($"[ForkHandler] 执行分叉节点: {node.NodeType}");
                await handler.Execute(node as SkillNodeConfig, context);
            }

            var outputEdges = graphConfig.Edges
                .Where(e => e.FromNodeId == nodeId)
                .ToList();

            // 分支节点特殊处理：根据条件结果选择 true/false 端口
            if (node.NodeType == "Branch")
            {
                var resultObj = context.GetExtraData<object>("branch_result");
                bool result = resultObj is bool b && b;
                var targetPortId = result ? "true" : "false";
                LogTool.Log($"[ForkHandler] Branch节点选择分支: {targetPortId}");
                var selectedEdge = outputEdges.FirstOrDefault(e => e.FromPortId == targetPortId);
                if (selectedEdge != null)
                {
                    await ExecuteNodeRecursive(selectedEdge.ToNodeId, context);
                }
                else
                {
                    LogTool.LogWarning($"[ForkHandler] Branch节点未找到端口 {targetPortId} 对应的连线");
                }
                return;
            }

            var subTasks = new List<UniTask>();
            foreach (var edge in outputEdges.Where(e => e.FromPortId == "exec_out"))
            {
                subTasks.Add(ExecuteNodeRecursive(edge.ToNodeId, context));
            }
            if (subTasks.Count > 0)
                await UniTask.WhenAll(subTasks);
        }
    }
}
