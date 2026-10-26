using System.Collections.Generic;
using Config;

namespace FusionContinent.Attribute
{
    /// <summary>
    /// 属性提供者接口
    /// 统一兼容战斗内 AttributeSet 与战斗外静态查询。
    /// </summary>
    public interface IAttributeProvider
    {
        /// <summary>
        /// 获取最终属性值
        /// </summary>
        float GetFinal(AttributeType type);

        /// <summary>
        /// 获取所有属性
        /// </summary>
        Dictionary<AttributeType, AttributeValue> GetAll();

        /// <summary>
        /// 配置ID（静态查询、面板展示可用）
        /// </summary>
        int ConfigId { get; }

        /// <summary>
        /// 等级
        /// </summary>
        int Level { get; }

        /// <summary>
        /// 星级
        /// </summary>
        int Star { get; }

        /// <summary>
        /// 觉醒等级
        /// </summary>
        int Awaken { get; }

        /// <summary>
        /// 主属性类型
        /// </summary>
        MainAttributeType MainAttributeType { get; }
    }
}
