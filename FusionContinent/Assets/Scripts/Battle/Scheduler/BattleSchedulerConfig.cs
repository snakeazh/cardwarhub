namespace Game.Battle
{
    /// <summary>
    /// 战斗技能表现调度配置。
    /// </summary>
    public class BattleSchedulerConfig
    {
        /// <summary>是否启用帧预算调度。</summary>
        public bool Enabled = true;

        /// <summary>每帧最多允许开始的技能表现数量。</summary>
        public int MaxSkillPerFrame = 3;

        /// <summary>仅在自动战斗模式下启用调度。</summary>
        public bool OnlyInAutoMode = true;

        /// <summary>手动释放（高优先级）跳过队列立即执行。</summary>
        public bool ManualBypassQueue = true;

        /// <summary>等待队列上限，超出后拒绝入队。</summary>
        public int MaxQueueSize = 32;
    }
}
