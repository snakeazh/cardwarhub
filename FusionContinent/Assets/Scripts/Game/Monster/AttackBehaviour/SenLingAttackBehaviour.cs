using FusionContinent;
using QFramework;
using UnityEngine;

namespace FusionContinent.Monster.AttackBehaviour
{
    public sealed class SenLingAttackBehaviour : SpineLoopAttackBehaviourBase
    {
        protected override void ExecuteAttackHit(MonsterCard owner, string eventName)
        {
            if (eventName == nameof(MonsterCard.SenLingtwoAttack))
            {
                PlaySecondHit(owner);
                return;
            }

            PlayFirstHit(owner);
        }

        public override void OnSpineEvent(MonsterCard owner, string eventName)
        {
            if (eventName == nameof(MonsterCard.SenLingOneAttack))
            {
                PlayFirstHit(owner);
                return;
            }

            if (eventName == nameof(MonsterCard.SenLingtwoAttack))
            {
                PlaySecondHit(owner);
                return;
            }

            base.OnSpineEvent(owner, eventName);
        }

        private void PlayFirstHit(MonsterCard owner)
        {
            MonsterAttackPresentationHelper.ShakeCameraIfEliteOrBoss(owner);
            GameObject effect = GamePlayManager.Default._resLoader
                .Get<GameObject>("conglinqishi_jn1_02")
                .Instantiate();
            effect.transform.SetParent(owner.transform);
            effect.transform.localPosition = Vector3.zero;
            effect.transform.localScale = Vector3.one;
            UnityEngine.Object.Destroy(effect, 0.2f);
            MonsterAttackPresentationHelper.DamageWall(owner);
        }

        private void PlaySecondHit(MonsterCard owner)
        {
            MonsterAttackPresentationHelper.ShakeCameraIfEliteOrBoss(owner);
            GameObject effect = GamePlayManager.Default._resLoader
                .Get<GameObject>("conglinqishi_jn1_03")
                .Instantiate();
            effect.transform.SetParent(owner.transform);
            effect.transform.localPosition = Vector3.zero;
            effect.transform.localScale = Vector3.one;
            UnityEngine.Object.Destroy(effect, 0.2f);
            MonsterAttackPresentationHelper.DamageWall(owner);
        }
    }
}
