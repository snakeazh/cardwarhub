namespace FusionContinent.Monster.MoveBehaviour
{
    /// <summary>
    /// 怪物移动行为策略接口（Strategy Pattern）。
    ///
    /// 调度链路：
    ///   MonsterManager.FixedUpdate → MonsterCard.FixedMoveTick → MoveBehaviour.Update（位移）
    ///   MonsterManager.Update（分批）→ MonsterCard.Tick → MonsterMoveState（动画/索敌/状态切换）
    ///
    /// 每种 MoveType 对应一个实现类，由 MonsterMoveBehaviourFactory 创建并挂到 MonsterCard.MoveBehaviour。
    /// </summary>
    public interface IMonsterMoveBehaviour
    {
        /// <summary>进入移动状态时初始化移动数据（由 MonsterMoveState.Enter 调用）。</summary>
        void Enter(MonsterCard monster);

        /// <summary>逐帧更新位移（由 MonsterCard.FixedMoveTick 调用，deltaTime 已含倍速）。</summary>
        void Update(MonsterCard monster, float deltaTime);

        /// <summary>离开移动状态时清理（由 MonsterMoveState.Exit 调用）。</summary>
        void Exit(MonsterCard monster);

        /// <summary>立即停止位移（进入攻击、击退、暂停等场景时调用）。</summary>
        void StopMove();

        /// <summary>获取当前移速倍率（异常状态减速/加速 + 首次受击修正等）。</summary>
        float GetSpeedScale(MonsterCard monster);

        /// <summary>叠加移速倍率修正（对应原 MonsterTweens.AddMoveSpeed / 首次受击修正）。</summary>
        void AddSpeedScale(float delta);

        /// <summary>重置移速倍率修正（重新 Enter 移动时调用）。</summary>
        void ResetSpeedScale();
    }
}
