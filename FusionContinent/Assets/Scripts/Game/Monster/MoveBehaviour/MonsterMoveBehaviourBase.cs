namespace FusionContinent.Monster.MoveBehaviour
{
    /// <summary>
    /// 移动行为抽象基类。
    ///
    /// 职责边界：
    ///   - 本类：移速倍率计算、抽象生命周期
    ///   - 子类：具体位移逻辑（向下走、跳跃、静止等）
    ///
    /// 移速公式（与子类共用）：
    ///   实际速度 = moveSpeed / 2 × (monsterspeed + _additiveSpeedBonus)
    ///   眩晕时倍率为 0，位移停止。
    /// </summary>
    public abstract class MonsterMoveBehaviourBase : IMonsterMoveBehaviour
    {
        /// <summary>叠加移速修正（首次受击加速、MonsterEffect 等），Enter 时重置。</summary>
        protected float _additiveSpeedBonus;

        public abstract void Enter(MonsterCard monster);
        public abstract void Update(MonsterCard monster, float deltaTime);

        public virtual void Exit(MonsterCard monster)
        {
            StopMove();
        }

        public abstract void StopMove();

        public float GetSpeedScale(MonsterCard monster)
        {
            if (monster.monsterData.monsterAbnormal.vertigo)
            {
                return 0f;
            }

            return monster.monsterData.monsterAbnormal.monsterspeed + _additiveSpeedBonus;
        }

        public void AddSpeedScale(float delta)
        {
            _additiveSpeedBonus += delta;
        }

        public void ResetSpeedScale()
        {
            _additiveSpeedBonus = 0f;
        }

        /// <summary>计算本帧实际移动速度（单位/秒），供子类 ApplyMove 使用。</summary>
        protected float CalcMoveSpeed(MonsterCard monster)
        {
            return monster.moveSpeed / 2f * GetSpeedScale(monster);
        }
    }
}
