using UnityEngine;

namespace FusionContinent.Framework.Graph
{
    /// <summary>
    /// 节点基础接口
    /// </summary>
    public interface INode
    {
        string NodeId { get; }
        string NodeType { get; }
        Vector2 EditorPosition { get; set; }

        T GetParam<T>(string key, T defaultValue = default);
        float GetParamFloat(string key, float defaultValue = 0f);
        int GetParamInt(string key, int defaultValue = 0);
        string GetParamString(string key, string defaultValue = "");
        bool GetParamBool(string key, bool defaultValue = false);
        Vector3 GetParamVector3(string key, Vector3 defaultValue = default);
    }
}
