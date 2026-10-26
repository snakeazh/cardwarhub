using System;
using System.Collections.Generic;
using System.Linq;
using Config;
using Cysharp.Threading.Tasks;
using FusionContinent.Skill.Config;
using UnityEngine;

namespace FusionContinent.Skill.Handler
{
    /// <summary>
    /// 顺序循环执行节点处理器
    /// 循环执行 body 端口连接的子节点，每次循环按顺序逐个执行（等待前一个完成再执行下一个）
    /// 与 Loop 节点的区别：Loop 为并行执行，LoopSequential 为顺序执行
    /// 支持四种调度模式：
    /// 1. count + interval 直接指定
    /// 2. duration + times_per_second 固定总时长，按每秒次数均匀分配
    /// 3. duration + duration_loop_count 固定总时长与总次数，自动计算间隔
    /// 4. duration + interval 固定总时长，按间隔计算次数
    /// </summary>
    public class LoopSequentialHandler : AsyncSkillNodeHandler
    {
        public override string NodeType => "LoopSequential";

        public override async UniTask Execute(SkillNodeConfig node, SkillContext context)
        {
            var count = node.GetParamInt("count", 3);
            var interval = node.GetParamFloat("interval", 0.2f);
            var duration = node.GetParamFloat("duration", 0f);
            var timesPerSecond = node.GetParamFloat("times_per_second", 0f);
            var durationLoopCount = node.GetParamInt("duration_loop_count", 0);

            var loopStartTime = Time.time;

            // 获取技能卡片
            ISkill skillCard = null;
            if (context.Caster != null)
            {
                var soldierCard = context.Caster.GetComponent<SoldierCard>();
                if (soldierCard != null)
                {
                    skillCard = soldierCard.GetSkillById(context.SkillId);
                }
            }

            // 如果配置了属性名，从技能卡片属性读取
            var countAttrName = node.GetParamString("count_attr", "");
            var intervalAttrName = node.GetParamString("interval_attr", "");
            var durationAttrName = node.GetParamString("duration_attr", "");
            var timesPerSecondAttrName = node.GetParamString("times_per_second_attr", "");
            var durationLoopCountAttrName = node.GetParamString("duration_loop_count_attr", "");

            if (skillCard != null)
            {
                if (!string.IsNullOrEmpty(countAttrName) && Enum.TryParse<AttributeType>(countAttrName, out var countAttr))
                {
                    count = (int)skillCard.Attributes.GetFinal(countAttr);
                    LogTool.Log($"[LoopSequentialHandler] 从属性 {countAttrName} 读取 count={count}");
                }

                if (!string.IsNullOrEmpty(intervalAttrName) && Enum.TryParse<AttributeType>(intervalAttrName, out var intervalAttr))
                {
                    interval = skillCard.Attributes.GetFinal(intervalAttr);
                    LogTool.Log($"[LoopSequentialHandler] 从属性 {intervalAttrName} 读取 interval={interval}");
                }

                if (!string.IsNullOrEmpty(durationAttrName) && Enum.TryParse<AttributeType>(durationAttrName, out var durationAttr))
                {
                    duration = skillCard.Attributes.GetFinal(durationAttr);
                    LogTool.Log($"[LoopSequentialHandler] 从属性 {durationAttrName} 读取 duration={duration}");
                }

                if (!string.IsNullOrEmpty(timesPerSecondAttrName) && Enum.TryParse<AttributeType>(timesPerSecondAttrName, out var timesPerSecondAttr))
                {
                    timesPerSecond = skillCard.Attributes.GetFinal(timesPerSecondAttr);
                    LogTool.Log($"[LoopSequentialHandler] 从属性 {timesPerSecondAttrName} 读取 times_per_second={timesPerSecond}");
                }

                if (!string.IsNullOrEmpty(durationLoopCountAttrName) && Enum.TryParse<AttributeType>(durationLoopCountAttrName, out var durationLoopCountAttr))
                {
                    durationLoopCount = (int)skillCard.Attributes.GetFinal(durationLoopCountAttr);
                    LogTool.Log($"[LoopSequentialHandler] 从属性 {durationLoopCountAttrName} 读取 duration_loop_count={durationLoopCount}");
                }
            }

            var comboSide = BattleSide.Player;
            if (context.Caster != null)
            {
                var casterSoldier = context.Caster.GetComponent<SoldierCard>();
                if (casterSoldier != null)
                {
                    comboSide = casterSoldier.BattleSide;
                }
            }

            duration = LoopScheduleUtil.ApplyDurationComboScale(node, duration, "LoopSequentialHandler", comboSide);

            // 固定持续时间 + 每秒次数：自动分配循环次数与间隔
            if (duration > 0f && timesPerSecond > 0f)
            {
                count = Mathf.CeilToInt(duration * timesPerSecond);
                interval = 1f / timesPerSecond;
                LogTool.Log($"[LoopSequentialHandler] 通过持续时间+频率计算: duration={duration}, times_per_second={timesPerSecond}, count={count}, interval={interval}");
            }
            // 固定持续时间 + 固定总次数：自动计算间隔
            else if (duration > 0f && durationLoopCount > 0)
            {
                count = durationLoopCount;
                interval = duration / durationLoopCount;
                LogTool.Log($"[LoopSequentialHandler] 通过持续时间+总次数计算: duration={duration}, duration_loop_count={durationLoopCount}, interval={interval}");
            }
            // 固定持续时间 + 固定间隔：通过 duration / interval 计算循环次数
            else if (duration > 0f && interval > 0f)
            {
                count = Mathf.CeilToInt(duration / interval);
                LogTool.Log($"[LoopSequentialHandler] 通过持续时间计算循环次数: duration={duration}, interval={interval}, count={count}");
            }

            if (count <= 0) count = 1;
            LogTool.Log($"[LoopSequentialHandler] 开始顺序循环: count={count}, interval={interval}s");
            for (int i = 0; i < count; i++)
            {
                if (context.CancellationToken.IsCancellationRequested)
                {
                    LogTool.Log("[LoopSequentialHandler] 循环被取消");
                    break;
                }

                var targetTime = loopStartTime + interval * i;
                var now = Time.time;
                var sleepMs = (int)((targetTime - now) * 1000);
                if (sleepMs > 0)
                {
                    await UniTask.Delay(sleepMs, cancellationToken: context.CancellationToken.Token);
                }

                // 每次循环重置 EffectOffset，让循环内的 Effect/AnimatedEffect 节点各自计算新的随机偏移
                context.EffectOffset = Vector2.zero;
                context.HasEffectOffset = false;

                await ExecuteBodyNodesSequential(node, context);
            }

        }

        private async UniTask ExecuteBodyNodesSequential(SkillNodeConfig loopNode, SkillContext context)
        {
            var graphConfig = context.Config;
            if (graphConfig == null)
            {
                LogTool.LogWarning("[LoopSequentialHandler] SkillContext.Config 未设置，无法执行子节点");
                return;
            }

            var bodyEdges = graphConfig.Edges
                .Where(e => e.FromNodeId == loopNode.NodeId && e.FromPortId == "body")
                .ToList();

            if (bodyEdges.Count == 0)
            {
                LogTool.Log("[LoopSequentialHandler] body 端口没有连接子节点");
                return;
            }

            SkillTargetResolver.SortLoopBodyEdgesSelectTargetFirst(graphConfig, bodyEdges);

            foreach (var edge in bodyEdges)
            {
                // 先标记为已执行，防止 SkillNodeExecutor 走 exec_out 时重复触发
                context.SetNodeData(loopNode.NodeId, $"executed_{edge.ToNodeId}", true);
                LogTool.Log($"[LoopSequentialHandler] 开始执行子节点: {edge.ToNodeId}");
                await ExecuteNodeRecursive(edge.ToNodeId, context);
            }
        }

        /// <summary>
        /// 仅执行指定节点的 handler，不递归跟随 exec_out 边。
        /// 与 SkillNodeExecutor.ExecuteFromNode 的区别是：不走 ExecuteNextNodes，
        /// 避免 LoopSequentialHandler 的 body 节点链被提前全部触发。
        /// </summary>
        private async UniTask ExecuteSingleNode(string nodeId, SkillContext context)
        {
            var graphConfig = context.Config;
            if (graphConfig == null) return;

            var node = graphConfig.GetNode(nodeId);
            if (node == null) return;

            var handler = SkillHandlerRegistry.Get(node.NodeType);
            if (handler != null)
            {
                LogTool.Log($"[LoopSequentialHandler] 执行节点: {node.NodeType} ({node.NodeId})");
                await handler.Execute(node as SkillNodeConfig, context);
            }
        }

        /// <summary>
        /// 完整递归：执行 handler 并继续跟随 exec_out 边。
        /// 保留给将来需要深度递归子图的场景使用。
        /// </summary>
        private async UniTask ExecuteNodeRecursive(string nodeId, SkillContext context)
        {
            await ExecuteSingleNode(nodeId, context);

            var graphConfig = context.Config;
            if (graphConfig == null) return;

            var node = graphConfig.GetNode(nodeId);
            if (node == null) return;

            var outputEdges = graphConfig.Edges
                .Where(e => e.FromNodeId == nodeId)
                .ToList();

            // 分支节点特殊处理：根据条件结果选择 true/false 端口
            if (node.NodeType == "Branch")
            {
                var resultObj = context.GetExtraData<object>("branch_result");
                bool result = resultObj is bool b && b;
                var targetPortId = result ? "true" : "false";
                LogTool.Log($"[LoopSequentialHandler] Branch节点选择分支: {targetPortId}");
                var selectedEdge = outputEdges.FirstOrDefault(e => e.FromPortId == targetPortId);
                if (selectedEdge != null)
                {
                    await ExecuteNodeRecursive(selectedEdge.ToNodeId, context);
                }
                else
                {
                    LogTool.LogWarning($"[LoopSequentialHandler] Branch节点未找到端口 {targetPortId} 对应的连线");
                }
                return;
            }

            // 普通节点：查找 exec_out 端口
            var execOutEdges = outputEdges.Where(e => e.FromPortId == "exec_out").ToList();
            foreach (var edge in execOutEdges)
            {
                await ExecuteNodeRecursive(edge.ToNodeId, context);
            }
        }
    }
}
