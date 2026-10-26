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
    /// 循环执行节点处理器
    /// 循环执行 body 端口连接的子节点，配合 Fork 可以实现多重斩、爆炎斩等效果
    /// 支持四种调度模式：
    /// 1. count + interval 直接指定
    /// 2. duration + times_per_second 固定总时长，按每秒次数均匀分配
    /// 3. duration + duration_loop_count 固定总时长与总次数，自动计算间隔
    /// 4. duration + interval 固定总时长，按间隔计算次数
    /// </summary>
    public class LoopHandler : AsyncSkillNodeHandler
    {
        public override string NodeType => "Loop";

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
                    LogTool.Log($"[LoopHandler] 从属性 {countAttrName} 读取 count={count}");
                }

                if (!string.IsNullOrEmpty(intervalAttrName) && Enum.TryParse<AttributeType>(intervalAttrName, out var intervalAttr))
                {
                    interval = skillCard.Attributes.GetFinal(intervalAttr);
                    LogTool.Log($"[LoopHandler] 从属性 {intervalAttrName} 读取 interval={interval}");
                }

                if (!string.IsNullOrEmpty(durationAttrName) && Enum.TryParse<AttributeType>(durationAttrName, out var durationAttr))
                {
                    duration = skillCard.Attributes.GetFinal(durationAttr);
                    LogTool.Log($"[LoopHandler] 从属性 {durationAttrName} 读取 duration={duration}");
                }

                if (!string.IsNullOrEmpty(timesPerSecondAttrName) && Enum.TryParse<AttributeType>(timesPerSecondAttrName, out var timesPerSecondAttr))
                {
                    timesPerSecond = skillCard.Attributes.GetFinal(timesPerSecondAttr);
                    LogTool.Log($"[LoopHandler] 从属性 {timesPerSecondAttrName} 读取 times_per_second={timesPerSecond}");
                }

                if (!string.IsNullOrEmpty(durationLoopCountAttrName) && Enum.TryParse<AttributeType>(durationLoopCountAttrName, out var durationLoopCountAttr))
                {
                    durationLoopCount = (int)skillCard.Attributes.GetFinal(durationLoopCountAttr);
                    LogTool.Log($"[LoopHandler] 从属性 {durationLoopCountAttrName} 读取 duration_loop_count={durationLoopCount}");
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

            duration = LoopScheduleUtil.ApplyDurationComboScale(node, duration, "LoopHandler", comboSide);

            // 固定持续时间 + 每秒次数：自动分配循环次数与间隔
            if (duration > 0f && timesPerSecond > 0f)
            {
                count = Mathf.CeilToInt(duration * timesPerSecond);
                interval = 1f / timesPerSecond;
                LogTool.Log($"[LoopHandler] 通过持续时间+频率计算: duration={duration}, times_per_second={timesPerSecond}, count={count}, interval={interval}");
            }
            // 固定持续时间 + 固定总次数：自动计算间隔
            else if (duration > 0f && durationLoopCount > 0)
            {
                count = durationLoopCount;
                interval = duration / durationLoopCount;
                LogTool.Log($"[LoopHandler] 通过持续时间+总次数计算: duration={duration}, duration_loop_count={durationLoopCount}, interval={interval}");
            }
            // 固定持续时间 + 固定间隔：通过 duration / interval 计算循环次数
            else if (duration > 0f && interval > 0f)
            {
                count = Mathf.CeilToInt(duration / interval);
                LogTool.Log($"[LoopHandler] 通过持续时间计算循环次数: duration={duration}, interval={interval}, count={count}");
            }

            if (count <= 0) count = 1;
            LogTool.Log($"[LoopHandler] 开始循环: count={count}, interval={interval}s");

            for (int i = 0; i < count; i++)
            {
                if (context.CancellationToken.IsCancellationRequested)
                {
                    LogTool.Log("[LoopHandler] 循环被取消");
                    break;
                }

                var targetTime = loopStartTime + interval * i;
                var now = Time.time;
                var sleepMs = (int)((targetTime - now) * 1000);
                if (sleepMs > 0)
                {
                    await UniTask.Delay(sleepMs, cancellationToken: context.CancellationToken.Token);
                }

                LogTool.Log($"[LoopHandler] 第 {i + 1}/{count} 次循环触发");

                context.EffectOffset = Vector2.zero;
                context.HasEffectOffset = false;

                await ExecuteBodyNodes(node, context);
            }
        }

        private async UniTask ExecuteBodyNodes(SkillNodeConfig loopNode, SkillContext context)
        {
            var graphConfig = context.Config;
            if (graphConfig == null)
            {
                LogTool.LogWarning("[LoopHandler] SkillContext.Config 未设置，无法执行子节点");
                return;
            }

            var bodyEdges = graphConfig.Edges
                .Where(e => e.FromNodeId == loopNode.NodeId && e.FromPortId == "body")
                .ToList();

            LogTool.Log($"[LoopHandler] body 端口连接了 {bodyEdges.Count} 条边");
            foreach (var edge in bodyEdges)
            {
                LogTool.Log($"[LoopHandler]   → body → {edge.ToNodeId}");
            }

            if (bodyEdges.Count == 0)
            {
                LogTool.Log("[LoopHandler] body 端口没有连接子节点");
                return;
            }

            SkillTargetResolver.SortLoopBodyEdgesSelectTargetFirst(graphConfig, bodyEdges);

            // 按顺序逐个执行，而非并行（避免多个节点同时修改 branch_result）
            foreach (var edge in bodyEdges)
            {
                await ExecuteNodeRecursive(edge.ToNodeId, context);
            }
        }

        private async UniTask ExecuteNodeRecursive(string nodeId, SkillContext context)
        {
            LogTool.Log($"[LoopHandler] 开始处理节点: {nodeId}");

            var graphConfig = context.Config;
            if (graphConfig == null) return;

            var node = graphConfig.GetNode(nodeId);
            if (node == null)
            {
                LogTool.LogWarning($"[LoopHandler] 节点不存在: {nodeId}");
                return;
            }

            LogTool.Log($"[LoopHandler] 节点类型: {node.NodeType}");

            var handler = SkillHandlerRegistry.Get(node.NodeType);
            if (handler == null)
            {
                LogTool.LogWarning($"[LoopHandler] 未找到 Handler: {node.NodeType}");
                return;
            }

            // 关键：必须先调用 Handler（如 BranchHandler 会存储 branch_result）
            await handler.Execute(node, context);

            // 查找所有输出边
            var outputEdges = graphConfig.Edges
                .Where(e => e.FromNodeId == nodeId)
                .ToList();

            LogTool.Log($"[LoopHandler] 节点 {nodeId} 有 {outputEdges.Count} 条输出边");
            foreach (var edge in outputEdges)
            {
                LogTool.Log($"[LoopHandler]   → {edge.FromPortId} → {edge.ToNodeId}");
            }

            // 分支节点：只走 true 或 false
            if (node.NodeType == "Branch")
            {
                var resultObj = context.GetExtraData<object>("branch_result");
                bool result = resultObj is bool b && b;
                var targetPortId = result ? "true" : "false";
                LogTool.Log($"[LoopHandler] Branch条件结果={result}，选择端口: {targetPortId}");
                var selectedEdge = outputEdges.FirstOrDefault(e => e.FromPortId == targetPortId);
                if (selectedEdge != null)
                {
                    LogTool.Log($"[LoopHandler] 递归到: {selectedEdge.ToNodeId}");
                    await ExecuteNodeRecursive(selectedEdge.ToNodeId, context);
                }
                else
                {
                    LogTool.LogWarning($"[LoopHandler] Branch节点未找到端口 {targetPortId} 对应的连线");
                }
                return;
            }

            // 普通节点：只走 exec_out
            var execOutEdges = outputEdges.Where(e => e.FromPortId == "exec_out").ToList();
            foreach (var edge in execOutEdges)
            {
                await ExecuteNodeRecursive(edge.ToNodeId, context);
            }
        }
    }
}
