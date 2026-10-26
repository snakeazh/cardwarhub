namespace FusionContinent.Module
{
    /**
     * 取舍有道
     */
    public class ChoosingWiselyModule
    {
        public bool Enabled = false;
        readonly int maxEffectLevel = 40;
        readonly int maxEffectLevelAdd = 50;
        public int MaxGameMana = 10;
        public int EffectLevel = 0;
        public int EffectAdd = 0;


        /**
         * 加成法术强度
         */
        public int AdditionSpellPower()
        {
            var spellPower = 0;
            // 所有英雄的攻击会提供1层魔法强度+25的效果加成，最多提供40层
            if (Enabled && EffectLevel > 0 && EffectAdd > 0)
            {
                if (EffectLevel > maxEffectLevel)
                {
                    EffectLevel = maxEffectLevel;
                }

                spellPower = (EffectAdd * EffectLevel);
            }

            return spellPower;
        }

        /**
         * 获取额外加成法力碎片
         */
        public int GetExtraAddGameManaFragment()
        {
            var fragmentNum = 0;
            // 满层时会额外提供50点卡能碎片恢复量
            if (Enabled && EffectLevel >= maxEffectLevel)
            {
                fragmentNum = maxEffectLevelAdd;
            }

            return fragmentNum;
        }
    }
}