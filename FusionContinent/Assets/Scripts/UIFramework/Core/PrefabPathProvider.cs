using System;
using UnityEngine;

namespace UIFramework
{
    /// <summary>
    /// 预制体路径属性标签
    /// 用于在 Panel 类上标记预制体路径
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class PrefabPathAttribute : Attribute
    {
        public string Path { get; }
        
        public PrefabPathAttribute(string path)
        {
            Path = path;
        }
    }
    
    /// <summary>
    /// 使用反射读取 PrefabPathAttribute 的路径提供器
    /// </summary>
    public class ReflectionPrefabPathProvider : IPrefabPathProvider
    {
        private readonly string mDefaultBasePath;
        
        public ReflectionPrefabPathProvider(string defaultBasePath = "UI/Prefabs")
        {
            mDefaultBasePath = defaultBasePath;
        }
        
        public string GetPrefabPath(Type panelType)
        {
            // 尝试获取 PrefabPathAttribute
            var attr = (PrefabPathAttribute)Attribute.GetCustomAttribute(
                panelType, typeof(PrefabPathAttribute));
            
            if (attr != null)
            {
                // 使用了标签，直接返回标签中的路径
                return attr.Path;
            }
            
            // 没有标签，使用默认路径
            return $"{mDefaultBasePath}/{panelType.Name}";
        }
    }
}
