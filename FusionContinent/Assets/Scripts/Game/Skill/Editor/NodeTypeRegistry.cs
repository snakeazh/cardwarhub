#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace FusionContinent.Skill.Editor
{
    /// <summary>
    /// 节点类型元数据注册表
    /// 集中管理所有节点类型的显示名称、分类、颜色等信息
    /// 消除编辑器代码中的硬编码节点类型列表
    /// </summary>
    public static class NodeTypeRegistry
    {
        /// <summary>
        /// 节点类型分类
        /// </summary>
        public enum NodeCategory
        {
            Cast,       // 施法类
            Action,     // 动作类
            Buff,       // Buff类
            Flow,       // 流程控制类
            Utility,    // 工具类
        }

        /// <summary>
        /// 节点类型元数据
        /// </summary>
        public struct NodeMetadata
        {
            public string NodeType;
            public string DisplayName;
            public NodeCategory Category;
            public string Description;

            public NodeMetadata(string nodeType, string displayName, NodeCategory category, string description = "")
            {
                NodeType = nodeType;
                DisplayName = displayName;
                Category = category;
                Description = description;
            }
        }

        private static readonly Dictionary<string, NodeMetadata> _metadataMap = new Dictionary<string, NodeMetadata>
        {
            // ========== 施法类节点 ==========
            {
                "Cast", new NodeMetadata("Cast", "施法节点", NodeCategory.Cast,
                    "播放施法动画和引导时间，是技能的入口节点")
            },

            // ========== 动作类节点 ==========
            {
                "Projectile", new NodeMetadata("Projectile", "投掷物节点", NodeCategory.Action,
                    "发射投射物，支持直线/追踪/穿透/弹跳")
            },
            {
                "FanProjectile", new NodeMetadata("FanProjectile", "扇形投射节点", NodeCategory.Action,
                    "从施法者位置向扇形范围内发射多个投射物，支持轮数和间隔")
            },
            {
                "MultiProjectile", new NodeMetadata("MultiProjectile", "多重投射节点", NodeCategory.Action,
                    "从配置列表发射多个投射物，支持循环次数和间隔，用于瞬袭斩等技能")
            },
            {
                "MultiTargetProjectile", new NodeMetadata("MultiTargetProjectile", "多目标投射节点", NodeCategory.Action,
                    "同时向多个最近敌人各发射一枚弹道，同一轮不重复目标")
            },
            {
                "WaveProjectile", new NodeMetadata("WaveProjectile", "波纹投射节点", NodeCategory.Action,
                    "沿方向向量多轮推进的投射物，类似爆炎斩效果，支持分叉角度和推进距离")
            },
            {
                "AoeDamage", new NodeMetadata("AoeDamage", "范围伤害节点", NodeCategory.Action,
                    "对范围内所有目标造成伤害")
            },
            {
                "AoeSelect", new NodeMetadata("AoeSelect", "范围选取节点", NodeCategory.Action,
                    "检测范围内目标并写入命中列表，不造成伤害；配合 ApplyBuff(target=Hit) 施加 Buff")
            },
            {
                "SelectTarget", new NodeMetadata("SelectTarget", "目标选取节点", NodeCategory.Utility,
                    "解析目标写入 SelectedTargets。后续 ApplyBuff / Effect / AnimatedEffect / Projectile / FanProjectile 可配置「已选取目标」复用同一目标")
            },
            {
                "PullMonster", new NodeMetadata("PullMonster", "拉拽怪物节点", NodeCategory.Action,
                    "将怪物拉向拖拽释放点；target=Hit 时可接在 AoeSelect 或 AoeDamage 之后")
            },
            {
                "Delay", new NodeMetadata("Delay", "延迟节点", NodeCategory.Action,
                    "等待指定时间后再继续执行")
            },
            {
                "PlayMixAnimation", new NodeMetadata("PlayMixAnimation", "播放混合动画", NodeCategory.Action,
                    "在指定 Spine 轨道上播放混合动画")
            },
            {
                "StopMixAnimation", new NodeMetadata("StopMixAnimation", "结束混合动画", NodeCategory.Action,
                    "按动画名关闭正在播放的混合动画")
            },
            {
                "ReturnAnimation", new NodeMetadata("ReturnAnimation", "Return动画", NodeCategory.Action,
                    "技能节点图末尾播放 return 表现动画；若期间有新技能动画抢占则跳过")
            },
            {
                "Effect", new NodeMetadata("Effect", "特效节点", NodeCategory.Action,
                    "播放视觉特效和音效")
            },
            {
                "AnimatedEffect", new NodeMetadata("AnimatedEffect", "动画特效节点", NodeCategory.Action,
                    "播放带动画过程的特效（陨石坠落、上升等），动画结束后可在落点播放陨石坑")
            },
            {
                "Sound", new NodeMetadata("Sound", "音效节点", NodeCategory.Action,
                    "播放音效")
            },

            // ========== Buff类节点 ==========
            {
                "ApplyBuff", new NodeMetadata("ApplyBuff", "应用Buff节点", NodeCategory.Buff,
                    "为目标施加Buff效果")
            },

            // ========== 流程控制类节点 ==========
            {
                "Branch", new NodeMetadata("Branch", "条件分支节点", NodeCategory.Flow,
                    "根据条件选择执行路径（true/false分支）")
            },
            {
                "Loop", new NodeMetadata("Loop", "循环节点(并行)", NodeCategory.Flow,
                    "循环执行子节点，配合Fork实现多重斩、爆炎斩（body端口内并行执行）")
            },
            {
                "LoopSequential", new NodeMetadata("LoopSequential", "循环节点(顺序)", NodeCategory.Flow,
                    "循环执行子节点，每次循环按顺序逐个执行（等待前一个完成再执行下一个）")
            },
            {
                "Fork", new NodeMetadata("Fork", "分叉节点", NodeCategory.Flow,
                    "同时执行多个并行分支")
            },

            // ========== 工具类节点 ==========
            {
                "ResetOffset", new NodeMetadata("ResetOffset", "重置偏移节点", NodeCategory.Utility,
                    "将位置偏移归零，后续节点以原始目标位置为基准计算偏移")
            },
            {
                "RecycleEffect", new NodeMetadata("RecycleEffect", "回收特效节点", NodeCategory.Action,
                    "回收当前技能执行中创建的特效实例，支持全部回收或按名称回收")
            },
        };

        /// <summary>
        /// 获取所有节点类型
        /// </summary>
        public static IReadOnlyDictionary<string, NodeMetadata> AllNodes => _metadataMap;

        /// <summary>
        /// 获取节点元数据
        /// </summary>
        public static NodeMetadata? GetMetadata(string nodeType)
        {
            if (_metadataMap.TryGetValue(nodeType, out var metadata))
                return metadata;
            return null;
        }

        /// <summary>
        /// 获取节点显示名称
        /// </summary>
        public static string GetDisplayName(string nodeType)
        {
            if (_metadataMap.TryGetValue(nodeType, out var meta))
                return meta.DisplayName;
            return nodeType;
        }

        /// <summary>
        /// 获取节点分类
        /// </summary>
        public static NodeCategory GetCategory(string nodeType)
        {
            if (_metadataMap.TryGetValue(nodeType, out var meta))
                return meta.Category;
            return NodeCategory.Action;
        }

        /// <summary>
        /// 获取节点分类对应的颜色
        /// </summary>
        public static Color GetCategoryColor(NodeCategory category)
        {
            return category switch
            {
                NodeCategory.Cast => new Color(0.2f, 0.6f, 0.9f, 1f),    // 蓝色
                NodeCategory.Action => new Color(0.9f, 0.5f, 0.2f, 1f),  // 橙色
                NodeCategory.Buff => new Color(0.6f, 0.2f, 0.8f, 1f),    // 紫色
                NodeCategory.Flow => new Color(0.2f, 0.8f, 0.4f, 1f),    // 绿色
                NodeCategory.Utility => new Color(0.5f, 0.5f, 0.5f, 1f), // 灰色
                _ => new Color(0.5f, 0.5f, 0.5f, 1f),
            };
        }

        /// <summary>
        /// 获取节点类型对应的颜色（兼容旧接口）
        /// </summary>
        public static Color GetNodeColor(string nodeType)
        {
            var category = GetCategory(nodeType);
            return GetCategoryColor(category);
        }

        /// <summary>
        /// 获取分类下的所有节点类型
        /// </summary>
        public static IEnumerable<string> GetNodesByCategory(NodeCategory category)
        {
            foreach (var kvp in _metadataMap)
            {
                if (kvp.Value.Category == category)
                    yield return kvp.Key;
            }
        }

        /// <summary>
        /// 获取所有节点类型列表（按分类排序）
        /// </summary>
        public static IEnumerable<(string nodeType, string displayName)> GetAllNodeTypes()
        {
            foreach (var category in System.Enum.GetValues(typeof(NodeCategory)))
            {
                foreach (var kvp in _metadataMap)
                {
                    if (kvp.Value.Category == (NodeCategory)category)
                        yield return (kvp.Key, kvp.Value.DisplayName);
                }
            }
        }
    }
}
#endif
