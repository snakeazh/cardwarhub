#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using FusionContinent.Framework.Graph;
using FusionContinent.Skill.Config;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace FusionContinent.Skill.Editor
{
    /// <summary>
    /// 技能节点视图 - 显示单个节点
    /// </summary>
    public class SkillNodeView : Node
    {
        private SkillNodeConfig _nodeConfig;
        private SkillGraphView _graphView;

        private VisualElement _headerContainer;
        private VisualElement _portsContainer;
        private VisualElement _paramsContainer;

        public string NodeId => _nodeConfig.NodeId;
        public string NodeType => _nodeConfig.NodeType;

        private Dictionary<string, Port> _inputPorts = new Dictionary<string, Port>();
        private Dictionary<string, Port> _outputPorts = new Dictionary<string, Port>();

        public SkillNodeView(SkillNodeConfig nodeConfig, SkillGraphView graphView)
        {
            _nodeConfig = nodeConfig;
            _graphView = graphView;

            // 设置节点标题
            title = GetNodeTitle(nodeConfig.NodeType);

            // 设置节点颜色
            var headerColor = NodeGraphStyles.GetNodeColor(nodeConfig.NodeType);
            style.borderTopColor = headerColor;
            style.borderBottomColor = headerColor;
            style.borderLeftColor = headerColor;
            style.borderRightColor = headerColor;
            style.borderTopWidth = 2;
            style.borderBottomWidth = 2;
            style.borderLeftWidth = 2;
            style.borderRightWidth = 2;

            // 创建端口
            CreatePorts();

            // 创建参数显示
            CreateParamsDisplay();

            // 设置节点尺寸
            style.width = NodeGraphStyles.NodeWidth;

            // 注册回调
            RegisterCallback<MouseDownEvent>(OnMouseDown);
        }

        private string GetNodeTitle(string nodeType)
        {
            switch (nodeType)
            {
                case "Cast": return "Cast [施法]";
                case "Projectile": return "Projectile [投掷物]";
                case "FanProjectile": return "FanProjectile [扇形投射]";
                case "AoeDamage": return "AoeDamage [范围伤害]";
                case "AoeSelect": return "AoeSelect [范围选取]";
                case "SelectTarget": return "SelectTarget [目标选取]";
                case "PullMonster": return "PullMonster [拉拽怪物]";
                case "Delay": return "Delay [延迟]";
                case "PlayMixAnimation": return "PlayMixAnimation [播放混合动画]";
                case "StopMixAnimation": return "StopMixAnimation [结束混合动画]";
                case "ReturnAnimation": return "ReturnAnimation [Return动画]";
                case "Effect": return "Effect [特效]";
                case "Sound": return "Sound [音效]";
                case "ApplyBuff": return "ApplyBuff [应用Buff]";
                case "Branch": return "Branch [分支]";
                case "RecycleEffect": return "RecycleEffect [回收特效]";
                case "MultiProjectile": return "MultiProjectile [多重投射]";
                case "MultiTargetProjectile": return "MultiTargetProjectile [多目标投射]";
                case "WaveProjectile": return "WaveProjectile [波纹投射]";
                default: return nodeType;
            }
        }

        private string GetBuffTypeChineseName(string buffTypeName)
        {
            switch (buffTypeName)
            {
                case "Burn": return "灼烧";
                case "Vertigo": return "冻结";
                case "Vulnerable": return "易损";
                case "Moderate": return "减速";
                case "KnockBack": return "击退";
                case "Invincible": return "无敌";
                case "Accelerate": return "加速";
                case "RequiemChant": return "慰灵圣咏";
                case "Disarm": return "缴械";
                case "Health": return "血量";
                case "Weak": return "虚弱";
                default: return buffTypeName;
            }
        }

        private void CreatePorts()
        {
            // 输入端口
            foreach (var port in _nodeConfig.InputPorts)
            {
                var inputPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, GetPortType(port.Type));
                inputPort.portName = port.PortId;
                inputPort.tooltip = GetPortTooltip(port);
                inputPort.style.backgroundColor = NodeGraphStyles.GetPortColor(port.Type.ToString());
                inputContainer.Add(inputPort);
                _inputPorts[port.PortId] = inputPort;
            }

            // 输出端口
            foreach (var port in _nodeConfig.OutputPorts)
            {
                var outputPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, GetPortType(port.Type));
                outputPort.portName = port.PortId;
                outputPort.tooltip = GetPortTooltip(port);
                outputPort.style.backgroundColor = NodeGraphStyles.GetPortColor(port.Type.ToString());
                outputContainer.Add(outputPort);
                _outputPorts[port.PortId] = outputPort;
            }
        }

        private Type GetPortType(PortType portType)
        {
            switch (portType)
            {
                case PortType.Exec: return typeof(bool);
                case PortType.Flow: return typeof(bool);
                case PortType.Data: return typeof(object);
                default: return typeof(object);
            }
        }

        private string GetPortTooltip(NodePort port)
        {
            return $"{port.Name} ({port.Type})";
        }

        private void CreateParamsDisplay()
        {
            // 参数显示区域
            _paramsContainer = new VisualElement();
            _paramsContainer.style.paddingLeft = 8;
            _paramsContainer.style.paddingRight = 8;
            _paramsContainer.style.paddingTop = 4;
            _paramsContainer.style.paddingBottom = 4;

            // 获取主要参数显示
            var mainParams = GetMainParams(_nodeConfig.NodeType, _nodeConfig);

            foreach (var param in mainParams)
            {
                var paramRow = new VisualElement();
                paramRow.style.flexDirection = FlexDirection.Row;
                paramRow.style.marginBottom = 2;

                var label = new Label(param.Key);
                label.style.width = 60;
                label.style.unityTextAlign = TextAnchor.MiddleLeft;
                label.style.fontSize = 10;
                label.style.color = new Color(0.7f, 0.7f, 0.7f);

                var value = new Label(param.Value);
                value.style.flexGrow = 1;
                value.style.unityTextAlign = TextAnchor.MiddleLeft;
                value.style.fontSize = 10;
                value.style.color = Color.white;

                paramRow.Add(label);
                paramRow.Add(value);
                _paramsContainer.Add(paramRow);
            }

            extensionContainer.Add(_paramsContainer);
            RefreshExpandedState();
            RefreshPorts();
        }

        private Dictionary<string, string> ParseHitModulesForDisplay(string raw)
        {
            var result = new Dictionary<string, string>();
            if (string.Equals(raw, "None", StringComparison.OrdinalIgnoreCase)) return result;
            if (string.IsNullOrEmpty(raw)) return result;

            string[] entries = raw.Split(';', StringSplitOptions.RemoveEmptyEntries);
            foreach (var entry in entries)
            {
                var parts = entry.Split('|');
                var moduleType = parts[0];

                int intParam = 0;
                float floatParam1 = 0f, floatParam2 = 0f, floatParam3 = 0f;

                if (parts.Length > 1)
                {
                    var p = parts[1].Split(':', StringSplitOptions.RemoveEmptyEntries);
                    if (p.Length > 0) int.TryParse(p[0], out intParam);
                    if (p.Length > 1) float.TryParse(p[1], out floatParam1);
                    if (p.Length > 2) float.TryParse(p[2], out floatParam2);
                    if (p.Length > 3) float.TryParse(p[3], out floatParam3);
                }

                var key = $"hit_{result.Count}";
                var desc = moduleType switch
                {
                    "Penetrate" => $"穿透×{intParam}" + (floatParam1 > 0 ? $"/{floatParam1:F1}s" : ""),
                    "Explode" => $"爆炸{floatParam1:F1}m" + (floatParam2 > 0 ? $"@{floatParam2:F1}s" : ""),
                    "Chain" => $"链式×{intParam}/{floatParam1:F1}m",
                    "Pierce" => $"穿刺×{intParam}",
                    "DelayedHit" => $"延迟{floatParam2:F1}s" + (floatParam1 > 0 ? $"@{floatParam1:F1}m" : ""),
                    "AOE" => $"AOE{floatParam1:F1}m" + (floatParam2 > 0 ? $"/{floatParam2:F1}s" : ""),
                    "Rect" => $"矩形{intParam}击" + (floatParam3 > 0 ? $"/{floatParam3:F1}s" : ""),
                    "Catapult" => $"弹射×{intParam}" + (floatParam1 > 0 ? $"/{floatParam1:F1}m" : ""),
                    _ => moduleType
                };
                result[key] = desc;
            }

            return result;
        }

        private Dictionary<string, string> GetMainParams(string nodeType, SkillNodeConfig config)
        {
            var paramNames = new Dictionary<string, string>();

            switch (nodeType)
            {
                case "Cast":
                    paramNames["animation"] = config.GetParamString("animation", "Attack");
                    paramNames["cast_time"] = config.GetParamFloat("cast_time", 0f).ToString("F1") + "s";
                    break;

                case "Telegraph":
                    paramNames["shape"] = config.GetParamString("shape", "Circle");
                    paramNames["range"] = config.GetParamFloat("range", 3f).ToString("F1") + "m";
                    paramNames["duration"] = config.GetParamFloat("duration", 0.5f).ToString("F1") + "s";
                    break;

                case "Projectile":
                    paramNames["prefab"] = config.GetParamString("prefab", "");
                    paramNames["move"] = config.GetParamString("move_type", "Straight");
                    paramNames["speed"] = config.GetParamFloat("speed", 10f).ToString("F0") + "m/s";
                    paramNames["damage"] = config.GetParamFloat("damage", 100f).ToString("F0");
                    var bl = config.GetParamFloat("bullet_length", 1f);
                    if (Mathf.Abs(bl - 1f) > 0.01f)
                        paramNames["length"] = bl.ToString("F1") + "m";
                    var bw = config.GetParamFloat("bullet_width", 1f);
                    if (Mathf.Abs(bw - 1f) > 0.01f)
                        paramNames["width"] = bw.ToString("F1") + "m";
                    var fixedRot = config.GetParamFloat("fixed_rotation", -1f);
                    if (fixedRot >= 0)
                        paramNames["rot"] = fixedRot.ToString("F0") + "°";
                    var fixedStartX = config.GetParamFloat("fixed_start_x", -999f);
                    var fixedStartY = config.GetParamFloat("fixed_start_y", -999f);
                    if (fixedStartX > -900f || fixedStartY > -900f)
                    {
                        var sx = fixedStartX > -900f ? fixedStartX.ToString("F0") : "*";
                        var sy = fixedStartY > -900f ? fixedStartY.ToString("F0") : "*";
                        paramNames["start"] = $"({sx},{sy})";
                    }
                    var spawnOffsetX = config.GetParamFloat("spawn_offset_x", 0f);
                    var spawnOffsetY = config.GetParamFloat("spawn_offset_y", 0f);
                    if (Mathf.Abs(spawnOffsetX) > 0.001f || Mathf.Abs(spawnOffsetY) > 0.001f)
                    {
                        paramNames["spawnΔ"] = $"({spawnOffsetX:F1},{spawnOffsetY:F1})";
                    }
                    var moveType = config.GetParamString("move_type", "Straight");
                    if (moveType == "LaserSweep")
                    {
                        paramNames["sweep"] = $"±{config.GetParamFloat("sweep_angle", 50f):F0}°/{config.GetParamFloat("sweep_duration", 5f):F1}s";
                    }
                    else if (moveType == "Bounce")
                    {
                        paramNames["bounce"] = $"×{config.GetParamInt("bounce_count", 3)}";
                    }

                    // 直接显示命中模块参数（不点箭头也能看到）
                    var rawHitModules = config.GetParamString("hit_modules", "");
                    if (!string.IsNullOrEmpty(rawHitModules))
                    {
                        var hitModuleDescs = ParseHitModulesForDisplay(rawHitModules);
                        foreach (var desc in hitModuleDescs)
                        {
                            paramNames[desc.Key] = desc.Value;
                        }
                    }
                    break;

                case "AoeDamage":
                case "PullMonster":
                    paramNames["target"] = config.GetParamString("target", "Hit");
                    paramNames["pull_to"] = config.GetParamString("pull_to", "ReleasePoint");
                    paramNames["duration"] = config.GetParamFloat("duration", 0.3f).ToString("F1") + "s";
                    var pullRatioDisplay = config.GetParamFloat("pull_ratio", 1f);
                    if (pullRatioDisplay < 0.999f)
                        paramNames["ratio"] = pullRatioDisplay.ToString("F2");
                    var dragAmp = config.GetParamFloat("drag_amplitude", 0f);
                    if (Mathf.Abs(dragAmp) > 0.001f)
                    {
                        paramNames["drag"] = config.GetParamBool("scale_drag_amplitude_by_distance", false)
                            ? $"×dist {dragAmp:F2}"
                            : $"{dragAmp:F1}m";
                    }
                    break;

                case "AoeSelect":
                    var aoeShape = config.GetParamString("shape", "Circle");
                    paramNames["shape"] = aoeShape;
                    if (aoeShape == "Rectangle")
                    {
                        paramNames["size"] = $"{config.GetParamFloat("rect_width", 10f):F1}×{config.GetParamFloat("rect_height", 2f):F1}m";
                    }
                    else
                    {
                        var rangeVal = config.GetParamFloat("range", 3f);
                        var rangeAttr = config.GetParamString("range_attr", "");
                        if (!string.IsNullOrEmpty(rangeAttr))
                            paramNames["range"] = $"{rangeAttr}:{rangeVal:F1}m";
                        else
                            paramNames["range"] = rangeVal.ToString("F1") + "m";
                    }
                    if (nodeType == "AoeDamage")
                    {
                        paramNames["damage"] = config.GetParamFloat("damage", 100f).ToString("F0");
                        paramNames["type"] = config.GetParamString("damage_type", "Magic");
                        var aoeHitEffect = config.GetParamString("hit_effect", "");
                        if (!string.IsNullOrEmpty(aoeHitEffect))
                            paramNames["hitVFX"] = aoeHitEffect;
                    }
                    else
                    {
                        paramNames["clear"] = config.GetParamBool("clear_hit_list", true) ? "清空" : "追加";
                    }
                    break;

                case "SelectTarget":
                    paramNames["target"] = config.GetParamString("target", "Target");
                    paramNames["clear"] = config.GetParamBool("clear_selected_list", true) ? "清空" : "追加";
                    break;

                case "Delay":
                    var delayDuration = config.GetParamFloat("duration", 1f);
                    var delayDurationAttr = config.GetParamString("duration_attr", "");
                    paramNames["duration"] = string.IsNullOrEmpty(delayDurationAttr)
                        ? delayDuration.ToString("F1") + "s"
                        : $"{delayDurationAttr}({delayDuration:F1}s)";
                    break;

                case "PlayMixAnimation":
                    paramNames["anim"] = config.GetParamString("anim_name", "");
                    paramNames["track"] = config.GetParamInt("track", 1).ToString();
                    break;

                case "StopMixAnimation":
                    paramNames["anim"] = config.GetParamString("anim_name", "");
                    paramNames["fade"] = config.GetParamFloat("fade_duration", 0.2f).ToString("F1") + "s";
                    break;

                case "ReturnAnimation":
                    paramNames["anim"] = config.GetParamString("anim_name", "return");
                    break;

                case "Effect":
                    var vfx = config.GetParamString("vfx", "");
                    paramNames["vfx"] = string.IsNullOrEmpty(vfx) ? "(空)" : vfx;
                    var pos = config.GetParamString("position", "Target");
                    paramNames["pos"] = pos;
                    var rot = config.GetParamString("rotation", "None");
                    if (rot != "None")
                        paramNames["rot"] = rot;
                    if (config.GetParamBool("lock_x", false))
                        paramNames["lockX"] = config.GetParamFloat("locked_x_value", 0f).ToString("F1");
                    break;

                case "Sound":
                    var soundId = config.GetParamInt("sound", 0);
                    paramNames["sound"] = soundId > 0 ? $"ID:{soundId}" : "无";
                    break;

                case "ApplyBuff":
                    var buffId = config.GetParamInt("buff_id", 0);
                    var buffTypeName = Enum.GetName(typeof(global::Config.BuffType), buffId) ?? buffId.ToString();
                    var buffChineseName = GetBuffTypeChineseName(buffTypeName);
                    paramNames["buff"] = $"{buffChineseName}({buffTypeName})";
                    var buffTarget = config.GetParamString("target", "Target");
                    paramNames["target"] = buffTarget;
                    var buffDuration = config.GetParamFloat("duration", -1f);
                    paramNames["duration"] = buffDuration < 0 ? "永久" : $"{buffDuration:F1}s";
                    var buffDurationType = (global::Config.BuffDurationType)config.GetParamInt("duration_type", 0);
                    if (buffDurationType != global::Config.BuffDurationType.Once)
                        paramNames["dur_type"] = buffDurationType.ToString();
                    var buffVfxPreview = config.GetParamString("buff_vfx", "");
                    if (!string.IsNullOrEmpty(buffVfxPreview))
                        paramNames["vfx"] = $"⚡{buffVfxPreview}";
                    break;

                case "Branch":
                    paramNames["condition"] = config.GetParamString("condition", "TargetAlive");
                    break;

                case "RecycleEffect":
                    var recycleTarget = config.GetParamString("target", "All");
                    paramNames["target"] = recycleTarget;
                    if (recycleTarget == "ByName")
                    {
                        paramNames["effectName"] = config.GetParamString("effectName", "");
                    }
                    break;

                case "MultiProjectile":
                    var mpOffsets = config.GetParamString("offset_list", "");
                    var mpLoopCount = config.GetParamInt("loop_count", 1);
                    var mpInterval = config.GetParamFloat("interval", 0.1f);
                    if (!string.IsNullOrEmpty(mpOffsets))
                    {
                        var entries = mpOffsets.Split(';', StringSplitOptions.RemoveEmptyEntries);
                        paramNames["offsets"] = $"{entries.Length}点 × {mpLoopCount}次";
                    }
                    if (Mathf.Abs(mpInterval - 0.1f) > 0.01f)
                        paramNames["interval"] = $"{mpInterval:F2}s";
                    paramNames["speed"] = config.GetParamFloat("speed", 10f).ToString("F0") + "m/s";

                    // 命中检测模块显示
                    var mpHitModule = config.GetParamString("hit_module", "None");
                    if (mpHitModule != "None")
                    {
                        paramNames["hit"] = mpHitModule;
                        var mpRectWidth = config.GetParamFloat("rect_width", 2f);
                        var mpRectLength = config.GetParamFloat("rect_length", 3f);
                        paramNames["rect"] = $"{mpRectWidth:F1}×{mpRectLength:F1}m";
                        var mpPenetrate = config.GetParamInt("max_penetrate", 0);
                        if (mpPenetrate > 0)
                            paramNames["penetrate"] = $"×{mpPenetrate}";
                    }
                    break;

                case "MultiTargetProjectile":
                    var mtpCount = config.GetParamInt("target_count", 3);
                    var mtpCountAttr = config.GetParamString("target_count_attr", "");
                    paramNames["targets"] = string.IsNullOrEmpty(mtpCountAttr)
                        ? mtpCount.ToString()
                        : $"{mtpCountAttr}({mtpCount})";
                    paramNames["move"] = config.GetParamString("move_type", "Straight");
                    paramNames["speed"] = config.GetParamFloat("speed", 10f).ToString("F0") + "m/s";
                    break;

                case "WaveProjectile":
                    var wpPrefab = config.GetParamString("prefab", "");
                    paramNames["prefab"] = string.IsNullOrEmpty(wpPrefab) ? "(空)" : System.IO.Path.GetFileName(wpPrefab);
                    var wpForkCount = config.GetParamInt("fork_count", 1);
                    var wpTotalRounds = config.GetParamInt("total_rounds", 5);
                    var wpForwardStep = config.GetParamFloat("forward_step", 2f);
                    var wpForkAngle = config.GetParamFloat("fork_angle", 15f);
                    paramNames["rounds"] = $"{wpForkCount}叉 × {wpTotalRounds}轮";
                    paramNames["step"] = $"{wpForwardStep:F1}m × {wpForkAngle:F0}°";
                    break;
            }

            return paramNames;
        }

        private void OnMouseDown(MouseDownEvent evt)
        {
            if (evt.clickCount == 2)
            {
                // 双击打开节点编辑器
                OpenNodeEditor();
                evt.StopPropagation();
            }
        }

        /// <summary>
        /// 打开节点编辑器
        /// </summary>
        public void OpenNodeEditor()
        {
            SkillNodeEditorWindow.OpenWindow(_nodeConfig, () =>
            {
                // 刷新参数显示
                RefreshParamsDisplay();
                _graphView.NotifyGraphModified();
            });
        }

        private void RefreshParamsDisplay()
        {
            if (_paramsContainer != null)
            {
                _paramsContainer.Clear();

                var mainParams = GetMainParams(_nodeConfig.NodeType, _nodeConfig);

                foreach (var param in mainParams)
                {
                    var paramRow = new VisualElement();
                    paramRow.style.flexDirection = FlexDirection.Row;
                    paramRow.style.marginBottom = 2;

                    var label = new Label(param.Key);
                    label.style.width = 60;
                    label.style.unityTextAlign = TextAnchor.MiddleLeft;
                    label.style.fontSize = 10;
                    label.style.color = new Color(0.7f, 0.7f, 0.7f);

                    var value = new Label(param.Value);
                    value.style.flexGrow = 1;
                    value.style.unityTextAlign = TextAnchor.MiddleLeft;
                    value.style.fontSize = 10;
                    value.style.color = Color.white;

                    paramRow.Add(label);
                    paramRow.Add(value);
                    _paramsContainer.Add(paramRow);
                }
            }
        }

        /// <summary>
        /// 获取输入端口
        /// </summary>
        public Port GetInputPort(string portId)
        {
            return _inputPorts.TryGetValue(portId, out var port) ? port : null;
        }

        /// <summary>
        /// 获取输出端口
        /// </summary>
        public Port GetOutputPort(string portId)
        {
            return _outputPorts.TryGetValue(portId, out var port) ? port : null;
        }

        /// <summary>
        /// 获取节点配置
        /// </summary>
        public SkillNodeConfig GetNodeConfig()
        {
            return _nodeConfig;
        }

        public override void SetPosition(Rect newPos)
        {
            base.SetPosition(newPos);
            _nodeConfig.EditorPosition = new Vector2(newPos.x, newPos.y);
        }
    }
}
#endif
