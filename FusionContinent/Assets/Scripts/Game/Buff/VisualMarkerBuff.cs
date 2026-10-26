using Config;
using ValueType = Config.ValueType;

namespace FusionContinent.Buff
{
    /// <summary>
    /// 仅播放 BuffVfx、不修改属性的标记 Buff。
    /// 用于 Attrbute 类型但无有效属性名或数值为 0 的配置（如暗影刀锋标记）。
    /// </summary>
    public class VisualMarkerBuff : BuffBase
    {
        public VisualMarkerBuff(BuffType type, float duration)
            : base(type, duration, 0, ValueType.Number)
        {
        }
    }
}
