#if UNITY_EDITOR
using UnityEngine;

namespace FusionContinent.Skill.Editor
{
    /// <summary>
    /// 节点图编辑器样式定义
    /// </summary>
    public static class NodeGraphStyles
    {
        // 节点尺寸
        public const float NodeWidth = 200f;
        public const float NodeHeaderHeight = 24f;
        public const float NodeFooterHeight = 8f;
        public const float PortHeight = 20f;
        public const float PortSpacing = 4f;

        // 端口尺寸
        public const float PortRadius = 6f;
        public const float PortConnectorWidth = 12f;

        // 颜色定义
        public static readonly Color NodeBackgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        public static readonly Color NodeHeaderColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        public static readonly Color NodeSelectedColor = new Color(0.3f, 0.5f, 0.8f, 1f);
        public static readonly Color GridBackgroundColor = new Color(0.15f, 0.15f, 0.15f, 1f);
        public static readonly Color GridLineColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);

        // 端口颜色
        public static readonly Color ExecPortColor = new Color(0.8f, 0.6f, 0.2f, 1f);      // 橙色 - 执行流
        public static readonly Color FlowPortColor = new Color(0.2f, 0.6f, 0.8f, 1f);     // 蓝色 - 控制流
        public static readonly Color DataPortColor = new Color(0.4f, 0.8f, 0.4f, 1f);    // 绿色 - 数据

        // 节点类型颜色
        public static readonly Color CastNodeColor = new Color(0.2f, 0.6f, 0.9f, 1f);    // 蓝色
        public static readonly Color ActionNodeColor = new Color(0.9f, 0.5f, 0.2f, 1f); // 橙色
        public static readonly Color BuffNodeColor = new Color(0.6f, 0.2f, 0.8f, 1f);    // 紫色
        public static readonly Color FlowNodeColor = new Color(0.2f, 0.8f, 0.4f, 1f);   // 绿色

        // 贝塞尔曲线控制点偏移
        public const float BezierTangent = 50f;

        // 样式缓存
        private static GUIStyle _nodeHeaderStyle;
        private static GUIStyle _nodeBodyStyle;
        private static GUIStyle _portStyle;
        private static GUIStyle _portLabelStyle;

        public static GUIStyle NodeHeaderStyle
        {
            get
            {
                if (_nodeHeaderStyle == null)
                {
                    _nodeHeaderStyle = new GUIStyle(UnityEditor.EditorStyles.helpBox)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontStyle = FontStyle.Bold,
                        fontSize = 12,
                        normal = { textColor = Color.white }
                    };
                }
                return _nodeHeaderStyle;
            }
        }

        public static GUIStyle NodeBodyStyle
        {
            get
            {
                if (_nodeBodyStyle == null)
                {
                    _nodeBodyStyle = new GUIStyle(UnityEditor.EditorStyles.helpBox)
                    {
                        padding = new RectOffset(8, 8, 4, 4)
                    };
                }
                return _nodeBodyStyle;
            }
        }

        public static GUIStyle PortStyle
        {
            get
            {
                if (_portStyle == null)
                {
                    _portStyle = new GUIStyle()
                    {
                        alignment = TextAnchor.MiddleLeft,
                        fontSize = 10,
                        normal = { textColor = Color.white }
                    };
                }
                return _portStyle;
            }
        }

        public static GUIStyle PortLabelStyle
        {
            get
            {
                if (_portLabelStyle == null)
                {
                    _portLabelStyle = new GUIStyle(UnityEditor.EditorStyles.label)
                    {
                        fontSize = 10,
                        alignment = TextAnchor.MiddleLeft,
                        normal = { textColor = Color.white }
                    };
                }
                return _portLabelStyle;
            }
        }

        /// <summary>
        /// 根据节点类型获取颜色
        /// </summary>
        public static Color GetNodeColor(string nodeType)
        {
            switch (nodeType)
            {
                case "Cast":
                case "Telegraph":
                    return CastNodeColor;
                case "Projectile":
                case "AoeDamage":
                case "AoeSelect":
                case "SelectTarget":
                case "PullMonster":
                case "Delay":
                case "Effect":
                case "AnimatedEffect":
                case "Sound":
                case "Slash":
                case "RecycleEffect":
                    return ActionNodeColor;
                case "ApplyBuff":
                    return BuffNodeColor;
                case "Branch":
                case "Loop":
                case "Fork":
                    return FlowNodeColor;
                case "Dash":
                    return new Color(0.2f, 0.8f, 0.6f, 1f);  // 青色 - 位移类
                default:
                    return new Color(0.5f, 0.5f, 0.5f, 1f);
            }
        }

        /// <summary>
        /// 根据端口类型获取颜色
        /// </summary>
        public static Color GetPortColor(string portType)
        {
            switch (portType)
            {
                case "Exec":
                    return ExecPortColor;
                case "Flow":
                    return FlowPortColor;
                case "Data":
                    return DataPortColor;
                default:
                    return Color.gray;
            }
        }
    }
}
#endif
