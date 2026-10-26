using System;
using System.Collections.Generic;
using System.Linq;
using FusionContinent.Framework;
using FusionContinent.Framework.Graph;
using UnityEngine;

namespace FusionContinent.Skill.Config
{
    /// <summary>
    /// 技能节点配置
    /// </summary>
    [Serializable]
    [CreateAssetMenu(fileName = "NewSkillNode", menuName = "FusionContinent/Skill/Skill Node")]
    public class SkillNodeConfig : ScriptableObject
    {
        [SerializeField]
        protected string _nodeId = "";

        [SerializeField]
        protected string _nodeType;

        [SerializeField]
        protected Vector2 _editorPosition;

        [SerializeField]
        protected List<NodePort> _inputPorts = new List<NodePort>();

        [SerializeField]
        protected List<NodePort> _outputPorts = new List<NodePort>();

        [SerializeField]
        protected SerializableDictionary<string, string> _params = new SerializableDictionary<string, string>();

        public string NodeId
        {
            get => _nodeId;
            internal set => _nodeId = value;
        }
        public string NodeType => _nodeType;
        public Vector2 EditorPosition { get => _editorPosition; set => _editorPosition = value; }
        public IReadOnlyList<NodePort> InputPorts => _inputPorts;
        public IReadOnlyList<NodePort> OutputPorts => _outputPorts;

        public SkillNodeConfig()
        {
        }

        public SkillNodeConfig Clone()
        {
            var clone = Instantiate(this);
            clone.NodeId = Guid.NewGuid().ToString();
            return clone;
        }

        /// <summary>
        /// 复制节点配置（浅复制，但生成新的NodeId）
        /// </summary>
        public SkillNodeConfig Copy()
        {
            var copy = CreateInstance<SkillNodeConfig>();
            copy._nodeId = Guid.NewGuid().ToString();
            copy._nodeType = _nodeType;
            copy._editorPosition = _editorPosition;

            // 复制端口
            foreach (var port in _inputPorts)
            {
                copy._inputPorts.Add(port.Copy());
            }
            foreach (var port in _outputPorts)
            {
                copy._outputPorts.Add(port.Copy());
            }

            // 复制参数
            foreach (var kvp in _params)
            {
                copy._params[kvp.Key] = kvp.Value;
            }

            return copy;
        }

        /// <summary>
        /// 从另一个节点复制参数
        /// </summary>
        public void CopyParamsFrom(SkillNodeConfig other)
        {
            _params.Clear();
            foreach (var kvp in other._params)
            {
                _params[kvp.Key] = kvp.Value;
            }
        }

        public T GetParam<T>(string key, T defaultValue = default)
        {
            if (!_params.TryGetValue(key, out var value))
                return defaultValue;

            try
            {
                if (typeof(T) == typeof(float))
                    return (T)(object)float.Parse(value);
                if (typeof(T) == typeof(int))
                    return (T)(object)int.Parse(value);
                if (typeof(T) == typeof(bool))
                    return (T)(object)bool.Parse(value);
                if (typeof(T) == typeof(string))
                    return (T)(object)value;

                return defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }
        

        public float GetParamFloat(string key, float defaultValue = 0f)
        {
            if (!_params.TryGetValue(key, out var value))
                return defaultValue;

            if (float.TryParse(value, out var result))
                return result;

            return defaultValue;
        }

        public int GetParamInt(string key, int defaultValue = 0)
        {
            if (!_params.TryGetValue(key, out var value))
                return defaultValue;

            if (int.TryParse(value, out var result))
                return result;

            return defaultValue;
        }

        public string GetParamString(string key, string defaultValue = "")
        {
            if (!_params.TryGetValue(key, out var value))
                return defaultValue;

            return value;
        }

        public bool GetParamBool(string key, bool defaultValue = false)
        {
            if (!_params.TryGetValue(key, out var value))
                return defaultValue;

            return value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                   value.Equals("1");
        }

        public Vector3 GetParamVector3(string key, Vector3 defaultValue = default)
        {
            if (!_params.TryGetValue(key, out var value))
                return defaultValue;

            if (!string.IsNullOrEmpty(value))
            {
                var parts = value.Split(',');
                if (parts.Length >= 3 &&
                    float.TryParse(parts[0], out var x) &&
                    float.TryParse(parts[1], out var y) &&
                    float.TryParse(parts[2], out var z))
                {
                    return new Vector3(x, y, z);
                }
            }

            return defaultValue;
        }

        public void SetParam(string key, string value)
        {
            _params[key] = value;
        }

        public void SetParam(string key, object value)
        {
            _params[key] = value?.ToString() ?? "";
        }

        public static SkillNodeConfig Create(string nodeType, Vector2 position)
        {
            var node = CreateInstance<SkillNodeConfig>();
            node._nodeId = Guid.NewGuid().ToString();
            node._nodeType = nodeType;
            node._editorPosition = position;

            switch (nodeType)
            {
                case "Cast":
                    node._inputPorts.Add(new NodePort("exec_in", "Exec", PortType.Exec, PortDirection.Input));
                    node._outputPorts.Add(new NodePort("exec_out", "Exec", PortType.Exec, PortDirection.Output));
                    break;
                case "Loop":
                    node._inputPorts.Add(new NodePort("exec_in", "Exec", PortType.Exec, PortDirection.Input));
                    node._outputPorts.Add(new NodePort("exec_out", "Exec", PortType.Exec, PortDirection.Output));
                    node._outputPorts.Add(new NodePort("body", "Body", PortType.Exec, PortDirection.Output));
                    node.SetParam("count", "3");
                    node.SetParam("interval", "0.2");
                    node.SetParam("duration", "0");
                    node.SetParam("times_per_second", "0");
                    node.SetParam("duration_loop_count", "0");
                    node.SetParam("count_attr", "");
                    node.SetParam("interval_attr", "");
                    node.SetParam("duration_attr", "");
                    node.SetParam("times_per_second_attr", "");
                    node.SetParam("duration_loop_count_attr", "");
                    node.SetParam("duration_scale_by_combo", "false");
                    break;
                case "LoopSequential":
                    node._inputPorts.Add(new NodePort("exec_in", "Exec", PortType.Exec, PortDirection.Input));
                    node._outputPorts.Add(new NodePort("exec_out", "Exec", PortType.Exec, PortDirection.Output));
                    node._outputPorts.Add(new NodePort("body", "Body", PortType.Exec, PortDirection.Output));
                    node.SetParam("count", "3");
                    node.SetParam("interval", "0.2");
                    node.SetParam("duration", "0");
                    node.SetParam("times_per_second", "0");
                    node.SetParam("duration_loop_count", "0");
                    node.SetParam("count_attr", "");
                    node.SetParam("interval_attr", "");
                    node.SetParam("duration_attr", "");
                    node.SetParam("times_per_second_attr", "");
                    node.SetParam("duration_loop_count_attr", "");
                    node.SetParam("duration_scale_by_combo", "false");
                    break;
                case "Fork":
                    node._inputPorts.Add(new NodePort("exec_in", "Exec", PortType.Exec, PortDirection.Input));
                    node._outputPorts.Add(new NodePort("exec_out", "Exec", PortType.Exec, PortDirection.Output));
                    node._outputPorts.Add(new NodePort("fork_0", "Fork 0", PortType.Exec, PortDirection.Output));
                    node._outputPorts.Add(new NodePort("fork_1", "Fork 1", PortType.Exec, PortDirection.Output));
                    node._outputPorts.Add(new NodePort("fork_2", "Fork 2", PortType.Exec, PortDirection.Output));
                    node.SetParam("count", "3");
                    break;

                case "Projectile":
                    node._inputPorts.Add(new NodePort("exec_in", "Exec", PortType.Exec, PortDirection.Input));
                    node._outputPorts.Add(new NodePort("exec_out", "Exec", PortType.Exec, PortDirection.Output));
                    node.SetParam("prefab", "");
                    node.SetParam("hit_effect", "");
                    node.SetParam("end_effect", "");
                    node.SetParam("spawn_type", "caster");
                    node.SetParam("aim_target", "Target");
                    node.SetParam("projectile_direction", "Target");
                    node.SetParam("move_type", "Straight");
                    node.SetParam("hit_modules", "");
                    node.SetParam("speed", "10");
                    node.SetParam("max_time", "10");
                    node.SetParam("damage", "100");
                    node.SetParam("sweep_angle", "50");
                    node.SetParam("sweep_duration", "5");
                    node.SetParam("bounce_count", "3");
                    node.SetParam("bullet_length", "1");
                    node.SetParam("bullet_width", "1");
                    node.SetParam("fixed_rotation", "-1");
                    node.SetParam("fixed_start_x", "-999");
                    node.SetParam("fixed_start_y", "-999");
                    node.SetParam("spawn_offset_x", "0");
                    node.SetParam("spawn_offset_y", "0");
                    node.SetParam("spawn_burn_area_on_move", "false");
                    node.SetParam("burn_area_trigger_mode", "Distance");
                    node.SetParam("burn_area_time_interval", "0.2");
                    node.SetParam("burn_area_distance_interval", "1.5");
                    node.SetParam("burn_area_effect", "");
                    node.SetParam("burn_area_duration", "2");
                    node.SetParam("burn_area_radius", "2");
                    node.SetParam("burn_area_tick_interval", "1");
                    node.SetParam("burn_area_damage", "10");
                    node.SetParam("burn_area_use_skill_damage", "false");
                    node.SetParam("burn_area_buff_id", "0");
                    node.SetParam("burn_area_buff_stay_time", "0");
                    // 属性绑定参数
                    node.SetParam("speed_attr", "");
                    node.SetParam("max_time_attr", "");
                    node.SetParam("bullet_length_attr", "");
                    node.SetParam("bullet_width_attr", "");
                    node.SetParam("fixed_rotation_attr", "");
                    // MoveToPosition 专用参数
                    node.SetParam("arrive_distance", "0.05");
                    // Homing 专用参数
                    node.SetParam("search_speed", "3");
                    node.SetParam("search_duration", "5");
                    node.SetParam("homing_delay", "0");
                    node.SetParam("homing_rot_speed", "180");
                    node.SetParam("homing_strength", "0.5");
                    node.SetParam("wander_move_duration", "2");
                    node.SetParam("wander_wait_duration", "1");
                    node.SetParam("homing_move_duration", "2");
                    node.SetParam("homing_wait_duration", "1");
                    node.SetParam("wander_range", "5");
                    break;

                case "AoeDamage":
                    node._inputPorts.Add(new NodePort("exec_in", "Exec", PortType.Exec, PortDirection.Input));
                    node._outputPorts.Add(new NodePort("exec_out", "Exec", PortType.Exec, PortDirection.Output));
                    node.SetParam("shape", "Circle");
                    node.SetParam("range", "3");
                    node.SetParam("range_attr", "");
                    node.SetParam("rect_width", "10");
                    node.SetParam("rect_height", "2");
                    node.SetParam("fan_angle", "60");
                    node.SetParam("hit_effect", "");
                    node.SetParam("target_filter", "Enemy");
                    node.SetParam("center_source", "Caster");
                    break;

                case "AoeSelect":
                    node._inputPorts.Add(new NodePort("exec_in", "Exec", PortType.Exec, PortDirection.Input));
                    node._outputPorts.Add(new NodePort("exec_out", "Exec", PortType.Exec, PortDirection.Output));
                    node.SetParam("shape", "Circle");
                    node.SetParam("range", "3");
                    node.SetParam("range_attr", "");
                    node.SetParam("rect_width", "10");
                    node.SetParam("rect_height", "2");
                    node.SetParam("fan_angle", "60");
                    node.SetParam("center_source", "Caster");
                    node.SetParam("clear_hit_list", "true");
                    break;

                case "SelectTarget":
                    node._inputPorts.Add(new NodePort("exec_in", "Exec", PortType.Exec, PortDirection.Input));
                    node._outputPorts.Add(new NodePort("exec_out", "Exec", PortType.Exec, PortDirection.Output));
                    node.SetParam("target", "RandomAlly");
                    node.SetParam("clear_selected_list", "true");
                    node.SetParam("exclude_previous_selected", "true");
                    break;

                case "PullMonster":
                    node._inputPorts.Add(new NodePort("exec_in", "Exec", PortType.Exec, PortDirection.Input));
                    node._outputPorts.Add(new NodePort("exec_out", "Exec", PortType.Exec, PortDirection.Output));
                    node.SetParam("target", "Hit");
                    node.SetParam("pull_to", "ReleasePoint");
                    node.SetParam("duration", "0.3");
                    node.SetParam("pull_ratio", "1");
                    node.SetParam("drag_amplitude", "0");
                    node.SetParam("scale_drag_amplitude_by_distance", "false");
                    node.SetParam("shape", "Circle");
                    node.SetParam("range", "3");
                    node.SetParam("range_attr", "");
                    node.SetParam("rect_width", "10");
                    node.SetParam("rect_height", "2");
                    node.SetParam("fan_angle", "60");
                    node.SetParam("center_source", "Target");
                    break;

                case "Delay":
                    node._inputPorts.Add(new NodePort("exec_in", "Exec", PortType.Exec, PortDirection.Input));
                    node._outputPorts.Add(new NodePort("exec_out", "Exec", PortType.Exec, PortDirection.Output));
                    node.SetParam("duration", "1");
                    node.SetParam("duration_attr", "");
                    break;

                case "PlayMixAnimation":
                    node._inputPorts.Add(new NodePort("exec_in", "Exec", PortType.Exec, PortDirection.Input));
                    node._outputPorts.Add(new NodePort("exec_out", "Exec", PortType.Exec, PortDirection.Output));
                    node.SetParam("anim_name", "");
                    node.SetParam("track", "1");
                    node.SetParam("alpha", "1");
                    node.SetParam("loop", "true");
                    break;

                case "StopMixAnimation":
                    node._inputPorts.Add(new NodePort("exec_in", "Exec", PortType.Exec, PortDirection.Input));
                    node._outputPorts.Add(new NodePort("exec_out", "Exec", PortType.Exec, PortDirection.Output));
                    node.SetParam("anim_name", "");
                    node.SetParam("fade_duration", "0.2");
                    break;

                case "ReturnAnimation":
                    node._inputPorts.Add(new NodePort("exec_in", "Exec", PortType.Exec, PortDirection.Input));
                    node._outputPorts.Add(new NodePort("exec_out", "Exec", PortType.Exec, PortDirection.Output));
                    node.SetParam("anim_name", "return");
                    break;

                case "Effect":
                    node._inputPorts.Add(new NodePort("exec_in", "Exec", PortType.Exec, PortDirection.Input));
                    node._outputPorts.Add(new NodePort("exec_out", "Exec", PortType.Exec, PortDirection.Output));
                    node.SetParam("vfx", "");
                    node.SetParam("sfx", "");
                    node.SetParam("position", "Target");
                    node.SetParam("rotation", "None");
                    node.SetParam("random_offset_radius", "0");
                    node.SetParam("lock_x", "false");
                    node.SetParam("locked_x_value", "0");
                    node.SetParam("vfx_scale", "");
                    node.SetParam("vfx_scale_attr", "");
                    break;

                case "AnimatedEffect":
                    node._inputPorts.Add(new NodePort("exec_in", "Exec", PortType.Exec, PortDirection.Input));
                    node._outputPorts.Add(new NodePort("exec_out", "Exec", PortType.Exec, PortDirection.Output));
                    node.SetParam("vfx", "");
                    node.SetParam("crater_vfx", "");
                    node.SetParam("animation_type", "Fall");
                    node.SetParam("position", "Target");
                    node.SetParam("random_offset_radius", "0");
                    node.SetParam("vfx_scale", "");
                    node.SetParam("vfx_scale_attr", "");
                    break;

                case "ApplyBuff":
                    node._inputPorts.Add(new NodePort("exec_in", "Exec", PortType.Exec, PortDirection.Input));
                    node._outputPorts.Add(new NodePort("exec_out", "Exec", PortType.Exec, PortDirection.Output));
                    node.SetParam("buff_id", "1001");
                    node.SetParam("target", "Target");
                    node.SetParam("duration", "-1");
                    node.SetParam("duration_type", "Once");
                    node.SetParam("effect_value", "0");    // 属性变化值
                    node.SetParam("attr_type", "0");      // 属性类型（AttributeType enum）
                    node.SetParam("cooldown_time", "0");    // 冷却时间
                    node.SetParam("skill_id", "0");        // 技能ID（用于冷却回调）
                    node.SetParam("probability", "1");
                    node.SetParam("stack", "1");
                    node.SetParam("tick_interval", "0");   // 间隔触发秒数（0表示每帧触发）
                    break;

                case "Branch":
                    node._inputPorts.Add(new NodePort("exec_in", "Exec", PortType.Exec, PortDirection.Input));
                    node._outputPorts.Add(new NodePort("true", "True", PortType.Exec, PortDirection.Output));
                    node._outputPorts.Add(new NodePort("false", "False", PortType.Exec, PortDirection.Output));
                    node.SetParam("condition", "SkillLevelGreaterThan");
                    node.SetParam("compare_value", "1");
                    node.SetParam("compare_attr", "");
                    break;

                case "Sound":
                    node._inputPorts.Add(new NodePort("exec_in", "Exec", PortType.Exec, PortDirection.Input));
                    node._outputPorts.Add(new NodePort("exec_out", "Exec", PortType.Exec, PortDirection.Output));
                    node.SetParam("sound", "");
                    break;

                case "ResetOffset":
                    node._inputPorts.Add(new NodePort("exec_in", "Exec", PortType.Exec, PortDirection.Input));
                    node._outputPorts.Add(new NodePort("exec_out", "Exec", PortType.Exec, PortDirection.Output));
                    break;

                case "RecycleEffect":
                    node._inputPorts.Add(new NodePort("exec_in", "Exec", PortType.Exec, PortDirection.Input));
                    node._outputPorts.Add(new NodePort("exec_out", "Exec", PortType.Exec, PortDirection.Output));
                    node.SetParam("target", "All");
                    node.SetParam("effectName", "");
                    break;

                case "MultiProjectile":
                    node._inputPorts.Add(new NodePort("exec_in", "Exec", PortType.Exec, PortDirection.Input));
                    node._outputPorts.Add(new NodePort("exec_out", "Exec", PortType.Exec, PortDirection.Output));
                    node.SetParam("prefab", "");
                    node.SetParam("hit_effect", "");
                    node.SetParam("end_effect", "");
                    node.SetParam("offset_config_key", "");
                    node.SetParam("offset_list", "");
                    node.SetParam("loop_count", "1");
                    node.SetParam("interval", "0.1");
                    node.SetParam("spawn_type", "direction");
                    node.SetParam("fixed_start_x", "-999");
                    node.SetParam("fixed_start_y", "-999");
                    node.SetParam("max_penetrate", "0");
                    node.SetParam("hit_target_duration", "0");
                    break;

                case "MultiTargetProjectile":
                    node._inputPorts.Add(new NodePort("exec_in", "Exec", PortType.Exec, PortDirection.Input));
                    node._outputPorts.Add(new NodePort("exec_out", "Exec", PortType.Exec, PortDirection.Output));
                    node.SetParam("target_count", "3");
                    node.SetParam("target_count_attr", "");
                    node.SetParam("include_primary_target", "true");
                    node.SetParam("prefab", "");
                    node.SetParam("hit_effect", "");
                    node.SetParam("end_effect", "");
                    node.SetParam("spawn_type", "caster");
                    node.SetParam("move_type", "Straight");
                    node.SetParam("hit_modules", "");
                    node.SetParam("speed", "10");
                    node.SetParam("max_time", "10");
                    node.SetParam("sweep_angle", "50");
                    node.SetParam("sweep_duration", "5");
                    node.SetParam("bounce_count", "3");
                    node.SetParam("bullet_length", "1");
                    node.SetParam("bullet_width", "1");
                    node.SetParam("fixed_rotation", "-1");
                    node.SetParam("fixed_start_x", "-999");
                    node.SetParam("fixed_start_y", "-999");
                    node.SetParam("spawn_offset_x", "0");
                    node.SetParam("spawn_offset_y", "0");
                    node.SetParam("spawn_burn_area_on_move", "false");
                    node.SetParam("burn_area_trigger_mode", "Distance");
                    node.SetParam("burn_area_time_interval", "0.2");
                    node.SetParam("burn_area_distance_interval", "1.5");
                    node.SetParam("burn_area_effect", "");
                    node.SetParam("burn_area_duration", "2");
                    node.SetParam("burn_area_radius", "2");
                    node.SetParam("burn_area_tick_interval", "1");
                    node.SetParam("burn_area_damage", "10");
                    node.SetParam("burn_area_use_skill_damage", "false");
                    node.SetParam("burn_area_buff_id", "0");
                    node.SetParam("burn_area_buff_stay_time", "0");
                    node.SetParam("speed_attr", "");
                    node.SetParam("max_time_attr", "");
                    node.SetParam("bullet_length_attr", "");
                    node.SetParam("bullet_width_attr", "");
                    node.SetParam("fixed_rotation_attr", "");
                    node.SetParam("arrive_distance", "0.05");
                    node.SetParam("search_speed", "3");
                    node.SetParam("search_duration", "5");
                    node.SetParam("homing_delay", "0");
                    node.SetParam("homing_rot_speed", "180");
                    node.SetParam("homing_strength", "0.5");
                    node.SetParam("wander_move_duration", "2");
                    node.SetParam("wander_wait_duration", "1");
                    node.SetParam("homing_move_duration", "2");
                    node.SetParam("homing_wait_duration", "1");
                    node.SetParam("wander_range", "5");
                    break;

                case "WaveProjectile":
                    node._inputPorts.Add(new NodePort("exec_in", "Exec", PortType.Exec, PortDirection.Input));
                    node._outputPorts.Add(new NodePort("exec_out", "Exec", PortType.Exec, PortDirection.Output));
                    node.SetParam("prefab", "");
                    node.SetParam("hit_effect", "");
                    node.SetParam("end_effect", "");
                    node.SetParam("fork_count", "1");
                    node.SetParam("total_rounds", "5");
                    node.SetParam("forward_step", "2");
                    node.SetParam("fork_angle", "15");
                    node.SetParam("round_interval", "0.2");
                    node.SetParam("scale", "100");
                    node.SetParam("max_penetrate", "0");
                    node.SetParam("hit_target_duration", "0");
                    break;

                case "FanProjectile":
                    node._inputPorts.Add(new NodePort("exec_in", "Exec", PortType.Exec, PortDirection.Input));
                    node._outputPorts.Add(new NodePort("exec_out", "Exec", PortType.Exec, PortDirection.Output));
                    node.SetParam("prefab", "");
                    node.SetParam("hit_effect", "");
                    node.SetParam("end_effect", "");
                    node.SetParam("aim_target", "Target");
                    node.SetParam("fan_angle", "90");
                    node.SetParam("projectile_count", "5");
                    node.SetParam("forward_step", "2");
                    node.SetParam("total_rounds", "1");
                    node.SetParam("round_interval", "0.1");
                    node.SetParam("scale", "100");
                    node.SetParam("max_penetrate", "0");
                    node.SetParam("hit_target_duration", "0");
                    break;
            }

            node.EnsureDefaultPortsIfMissing();
            return node;
        }

        /// <summary>
        /// 为缺少端口的节点补全默认 exec 端口（例如新增节点类型后已保存的旧节点）。
        /// </summary>
        public void EnsureDefaultPortsIfMissing()
        {
            if (_inputPorts.Count > 0 && _outputPorts.Count > 0)
                return;

            if (_nodeType != "PullMonster")
                return;

            _inputPorts.Add(new NodePort("exec_in", "Exec", PortType.Exec, PortDirection.Input));
            _outputPorts.Add(new NodePort("exec_out", "Exec", PortType.Exec, PortDirection.Output));
        }
    }
}
