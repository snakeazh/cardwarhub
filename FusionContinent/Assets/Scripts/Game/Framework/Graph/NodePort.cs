namespace FusionContinent.Framework.Graph
{
    /// <summary>
    /// 端口类型
    /// </summary>
    public enum PortType
    {
        /// <summary>执行流</summary>
        Exec,
        /// <summary>控制流</summary>
        Flow,
        /// <summary>数据</summary>
        Data,
    }

    /// <summary>
    /// 端口方向
    /// </summary>
    public enum PortDirection
    {
        Input,
        Output,
    }

    /// <summary>
    /// 节点端口定义
    /// </summary>
    [System.Serializable]
    public class NodePort
    {
        public string PortId;
        public string Name;
        public PortType Type = PortType.Exec;
        public PortDirection Direction = PortDirection.Input;
        public bool Optional = false;

        public NodePort() { }

        public NodePort(string portId, string name, PortType type, PortDirection direction, bool optional = false)
        {
            PortId = portId;
            Name = name;
            Type = type;
            Direction = direction;
            Optional = optional;
        }

        /// <summary>
        /// 复制端口
        /// </summary>
        public NodePort Copy()
        {
            return new NodePort(PortId, Name, Type, Direction, Optional);
        }
    }
}
