using System;
using System.Collections.Generic;
using UnityEngine;

namespace FusionContinent.Framework.Graph
{
    /// <summary>
    /// 节点配置基类
    /// </summary>
    [Serializable]
    public abstract class NodeConfig : INode
    {
        [SerializeField]
        protected string _nodeId;

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

        public string NodeId => _nodeId;
        public string NodeType => _nodeType;
        public Vector2 EditorPosition { get => _editorPosition; set => _editorPosition = value; }
        public IReadOnlyList<NodePort> InputPorts => _inputPorts;
        public IReadOnlyList<NodePort> OutputPorts => _outputPorts;

        public NodeConfig()
        {
            _nodeId = Guid.NewGuid().ToString();
        }

        public NodeConfig(string nodeType) : this()
        {
            _nodeType = nodeType;
        }

        public T GetParam<T>(string key, T defaultValue = default)
        {
            if (!_params.TryGetValue(key, out var value))
                return defaultValue;

            return ParseValue(value, defaultValue);
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

        private static T ParseValue<T>(string value, T defaultValue)
        {
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

        public abstract NodeConfig Clone();
    }
}
