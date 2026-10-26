using System.Collections.Generic;

namespace Game.Player
{
    /// <summary>
    /// 可升星道具的某一档星级信息（通用 DTO）。
    /// 用于把武器（WeaponStarListStruct）与皮肤形象（ImageStarListStruct）等
    /// 不同 Luban 生成结构统一为同一类型，供通用升星/特殊效果面板使用。
    /// </summary>
    public class StarLevelInfo
    {
        public int Level;
        public string Desc1;
        public string Desc2;
    }

    /// <summary>
    /// 升星消耗查询结果。
    /// </summary>
    public struct StarUpgradeConsume
    {
        public bool HasConsume;
        public int ItemId;
        public int NeedCount;
    }
}
