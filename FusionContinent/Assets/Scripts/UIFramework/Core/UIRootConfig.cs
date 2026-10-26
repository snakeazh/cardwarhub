using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

namespace UIFramework
{
    /// <summary>
    /// UI层级配置项
    /// 用于在Inspector中配置每个层级
    /// </summary>
    [Serializable]
    public class UILevelConfig
    {
        [Tooltip("UI层级类型")]
        public UILevel Level;
        
        [Tooltip("层级根节点Transform")]
        public Transform RootTransform;
        
        [Tooltip("是否激活")]
        public bool IsActive = true;
    }
    
    /// <summary>
    /// UIRoot配置组件
    /// 挂载在场景中的UIRoot物体上
    /// </summary>
    public class UIRootConfig : MonoBehaviour
    {
        [Header("层级配置")]
        [Tooltip("拖拽配置各个UI层级，Sorting Order会自动根据UILevel枚举值设置")]
        public List<UILevelConfig> LevelConfigs = new List<UILevelConfig>();
        

        
        /// <summary>
        /// 获取指定层级的Transform
        /// </summary>
        public Transform GetLevelTransform(UILevel level)
        {
            var config = LevelConfigs.Find(c => c.Level == level);
            return config?.RootTransform;
        }
        
        /// <summary>
        /// 检查层级是否已配置
        /// </summary>
        public bool HasLevel(UILevel level)
        {
            return LevelConfigs.Exists(c => c.Level == level && c.RootTransform != null);
        }
    }
}
