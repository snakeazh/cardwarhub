using Config;
using ValueType = Config.ValueType;

namespace FusionContinent.Buff
{
    /// <summary>
    /// 无效果标记 Buff（占位用，保留向后兼容）
    /// </summary>
    public class MarkerBuff : BuffBase
    {
        public MarkerBuff() : base(BuffType.Burn, -1, 0, ValueType.Number) { }
    }
}
