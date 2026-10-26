using System;
using FusionContinent;

namespace FusionContinent.Monster.AttackBehaviour
{
    public interface IAttackBehaviour
    {
        /// <summary>
        /// 为 true 时 StraightTravel 到达后不立即 IsInit，由 OnSpawnPresentation 控制。
        /// </summary>
        bool DeferStraightTravelInit { get; }

        void OnInit(MonsterCard owner);

        void OnCleanup(MonsterCard owner);

        void OnSpawnPresentation(MonsterCard owner);

        /// <summary>
        /// StraightTravel 到达后是否由行为自行控制 IsInit 时机。
        /// </summary>
        bool TryHandleStraightTravelArrival(MonsterCard owner, Action onReady);

        void BeginEngage(MonsterCard owner);

        void PauseEngage(MonsterCard owner);

        void ResumeEngage(MonsterCard owner);

        /// <summary>
        /// 退出接战或进入虚弱等不可攻击状态时，停止攻击循环并清理 Spine 回调。
        /// </summary>
        void StopEngage(MonsterCard owner);

        void OnSpineEvent(MonsterCard owner, string eventName);
    }
}
