using FusionContinent.Buff;

namespace FusionContinent
{
    /// <summary>
    /// 可被施加Buff的接口
    /// </summary>
    public interface IBuffable
    {
        void AddBuff(IBuff buff, object caster = null);
    }
}
