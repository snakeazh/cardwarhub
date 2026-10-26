using System;
using FusionContinent;

namespace FusionContinent.Monster.AttackBehaviour
{
    public abstract class MonsterAttackBehaviourBase : IAttackBehaviour
    {
        protected AttackBehaviourRuntime Runtime { get; } = new();
        private bool _isCleaned;

        public virtual bool DeferStraightTravelInit => false;

        public virtual void OnInit(MonsterCard owner)
        {
            _isCleaned = false;
            Runtime.BindCoroutineHost(owner);
        }

        public virtual void OnCleanup(MonsterCard owner)
        {
            if (_isCleaned)
            {
                return;
            }

            _isCleaned = true;
            Runtime.StopAll();
        }

        public virtual void OnSpawnPresentation(MonsterCard owner)
        {
        }

        public virtual bool TryHandleStraightTravelArrival(MonsterCard owner, Action onReady)
        {
            return false;
        }

        public abstract void BeginEngage(MonsterCard owner);

        public virtual void PauseEngage(MonsterCard owner)
        {
        }

        public virtual void ResumeEngage(MonsterCard owner)
        {
        }

        public virtual void StopEngage(MonsterCard owner)
        {
        }

        public virtual void OnSpineEvent(MonsterCard owner, string eventName)
        {
        }
    }
}
