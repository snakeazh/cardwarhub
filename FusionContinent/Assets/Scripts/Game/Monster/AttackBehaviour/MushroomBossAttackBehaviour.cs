using FusionContinent;

namespace FusionContinent.Monster.AttackBehaviour
{
    public sealed class MushroomBossAttackBehaviour : SpineLoopAttackBehaviourBase
    {
        protected override void ExecuteAttackHit(MonsterCard owner, string eventName)
        {
            MonsterAttackPresentationHelper.ShakeCameraIfEliteOrBoss(owner);
            MonsterAttackPresentationHelper.DamageWall(owner);
        }

        public override void OnSpineEvent(MonsterCard owner, string eventName)
        {
            if (eventName == nameof(MonsterCard.MoguBoss))
            {
                ExecuteAttackHit(owner, eventName);
                return;
            }

            base.OnSpineEvent(owner, eventName);
        }
    }
}
