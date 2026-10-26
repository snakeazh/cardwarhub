using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FusionContinent.Skill.Config;
using UnityEngine;

namespace FusionContinent.Skill.Handler
{
    /// <summary>
    /// 技能节点执行器
    /// </summary>
    public class SkillNodeExecutor
    {
        private SkillContext _context;
        private SkillGraphConfig _config;
        private HashSet<string> _executedNodes = new HashSet<string>();

        /// <summary>
        /// 执行技能图
        /// </summary>
        public async UniTask Execute(SkillGraphConfig config, SkillContext context)
        {
            _config = config;
            _context = context;
            _executedNodes.Clear();

            _context.Config = config;

            if (config == null || config.Nodes.Count == 0)
            {
                LogTool.LogWarning("技能配置为空");
                return;
            }


            var entryNode = config.GetEntryNode();
            if (entryNode == null)
            {
                LogTool.LogWarning("未找到入口节点");
                foreach (var node in config.Nodes)
                {
                    await ExecuteFromNode(node.NodeId);
                }
                return;
            }

            await ExecuteFromNode(entryNode.NodeId);

        }

        private async UniTask ExecuteFromNode(string nodeId)
        {
            if (_context.CancellationToken.IsCancellationRequested)
            {
                LogTool.Log("[SkillNodeExecutor] 技能执行被取消");
                return;
            }

            var node = _config.GetNode(nodeId);
            if (node == null)
            {
                LogTool.LogWarning($"[SkillNodeExecutor] 未找到节点: {nodeId}");
                return;
            }

            // LoopSequential Handler 内部已标记执行的 body 节点，跳过（优先于 _executedNodes 判断，
            // 因为同一次技能执行中 body 节点会被循环多次，而 _executedNodes 只在 Handler 外层加了一次）
            var parentLoopId = FindParentLoopNode(nodeId);
            if (parentLoopId != null && _context.GetNodeData<bool>(parentLoopId, $"executed_{nodeId}"))
            {
                LogTool.Log($"[SkillNodeExecutor] 节点已被 LoopSequential 执行，跳过: {nodeId}");
                return;
            }

            if (_executedNodes.Contains(nodeId))
            {
                LogTool.Log($"[SkillNodeExecutor] 节点已执行过，跳过: {nodeId}");
                return;
            }

            _context.CurrentNodeId = nodeId;
            _executedNodes.Add(nodeId);

            var handler = SkillHandlerRegistry.Get(node.NodeType);
            if (handler == null)
            {
                LogTool.LogWarning($"[SkillNodeExecutor] 未找到节点处理器: {node.NodeType}");
            }
            else
            {
                try
                {
                    if (!handler.IsAsync)
                    {
                        using (BattleSkillProfilerMarkers.Begin(BattleSkillProfilerMarkers.SkillRelease))
                        {
                            await handler.Execute(node as SkillNodeConfig, _context);
                        }
                    }
                    else
                    {
                        await handler.Execute(node as SkillNodeConfig, _context);
                    }
                }
                catch (OperationCanceledException)
                {
                    LogTool.Log($"[SkillNodeExecutor] 节点执行被取消: {nodeId}");
                    return;
                }
                catch (Exception ex)
                {
                    LogTool.LogError($"[SkillNodeExecutor] 节点执行异常: {nodeId}, {ex}");
                }
            }

            await ExecuteNextNodes(nodeId);
        }

        private async UniTask ExecuteNextNodes(string fromNodeId)
        {
            if (_context.CancellationToken.IsCancellationRequested)
                return;

            var node = _config.GetNode(fromNodeId);
            var outputEdges = _config.GetOutputEdges(fromNodeId);

            if (node != null && node.NodeType == "Branch")
            {
                var resultObj = _context.GetExtraData<object>("branch_result");
                bool result = resultObj is bool b && b;

                var targetPortId = result ? "true" : "false";
                foreach (var e in outputEdges)
                {
                    LogTool.Log($"  → {e.FromPortId} → {e.ToNodeId}");
                }

                var selectedEdge = outputEdges.FirstOrDefault(e => e.FromPortId == targetPortId);
                if (selectedEdge != null)
                {
                    await ExecuteFromNode(selectedEdge.ToNodeId);
                }
                else
                {
                    LogTool.LogWarning($"[SkillNodeExecutor] Branch节点未找到端口 {targetPortId} 对应的连线");
                }
                return;
            }

            // Loop / LoopSequential / Fork：body、fork_n 已在 Handler 内执行，此处只沿 exec_out 继续主流程
            if (node != null && (node.NodeType == "Loop" || node.NodeType == "LoopSequential" || node.NodeType == "Fork"))
            {
                foreach (var edge in outputEdges.Where(e => e.FromPortId == "exec_out"))
                {
                    await ExecuteFromNode(edge.ToNodeId);
                }
                return;
            }

            foreach (var edge in outputEdges)
            {
                await ExecuteFromNode(edge.ToNodeId);
            }
        }

        /// <summary>
        /// 查找指定节点所属的父级 LoopSequential 节点 ID。
        /// </summary>
        private string FindParentLoopNode(string nodeId)
        {
            if (_config == null) return null;
            var edge = _config.Edges.FirstOrDefault(e =>
                e.ToNodeId == nodeId &&
                e.ToPortId == "body" &&
                _config.GetNode(e.FromNodeId)?.NodeType == "LoopSequential");
            return edge?.FromNodeId;
        }

        public void Cancel()
        {
            _context?.Cancel();
        }
    }
}
