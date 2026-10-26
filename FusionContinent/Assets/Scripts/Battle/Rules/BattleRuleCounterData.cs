using System.Collections.Generic;

namespace Game.Battle
{
    /// <summary>
    /// 单条 ComputerGroup 计数运行时数据。
    /// Count 为累计总数；TriggerTimes 用于 CounterTime 时间窗口统计。
    /// </summary>
    public class BattleRuleCounterData
    {
        public int Count;
        public float LastActivityTime;
        public readonly Queue<float> TriggerTimes = new Queue<float>();
    }
}
