using DG.Tweening;
using FusionContinent;
using Spine;
using UnityEngine;

namespace FusionContinent.Monster.AttackBehaviour
{
    /// <summary>
    /// Spine 接战循环：按 attackTime 周期播放 atk，由 OnSpineEvent 触发命中。
    /// </summary>
    public abstract class SpineLoopAttackBehaviourBase : MonsterAttackBehaviourBase
    {
        private Tween _engageTween;
        private MonsterCard _owner;

        public override void OnCleanup(MonsterCard owner)
        {
            StopEngage(owner);
            base.OnCleanup(owner);
        }

        public override void BeginEngage(MonsterCard owner)
        {
            StopEngage(owner);
            _owner = owner;

            owner.isInAttackCooldown = true;
            owner.Play("idle", true);

            _engageTween = DOTween.Sequence()
                .AppendInterval(owner.attackTime)
                .SetLoops(-1)
                .OnStepComplete(PlayAttackAnimation)
                .OnKill(() =>
                {
                    if (owner != null)
                    {
                        owner.isInAttackCooldown = false;
                    }
                });
            _engageTween.SetId(GlobalConst.MonsterTween);
            Runtime.RegisterTween(_engageTween);
        }

        public override void PauseEngage(MonsterCard owner)
        {
            _engageTween?.Pause();
        }

        public override void ResumeEngage(MonsterCard owner)
        {
            _engageTween?.Play();
        }

        public override void StopEngage(MonsterCard owner)
        {
            _engageTween?.Kill();
            _engageTween = null;
            _owner = null;

            if (owner == null)
            {
                return;
            }

            owner.isInAttackCooldown = false;
            owner.isAttackAnimationPlaying = false;
            owner.AnimationController.UnregisterAllSpineCallbacks();
        }

        public override void OnSpineEvent(MonsterCard owner, string eventName)
        {
            if (eventName == "atk" || eventName == nameof(MonsterCard.SenLingOneAttack)
                || eventName == nameof(MonsterCard.SenLingtwoAttack)
                || eventName == nameof(MonsterCard.MoguBoss))
            {
                ExecuteAttackHit(owner, eventName);
            }
        }

        protected abstract void ExecuteAttackHit(MonsterCard owner, string eventName);

        private void PlayAttackAnimation()
        {
            if (_owner == null)
            {
                return;
            }

            _owner.isAttackAnimationPlaying = false;
            _owner.AnimationController.SetEmptyAnimation(0, 0.1f);
            _owner.AnimationController.PlayAttack(OnSpineEventReceived, OnAttackAnimationComplete);
            _owner.isAttackAnimationPlaying = true;
        }

        private void OnSpineEventReceived(TrackEntry trackEntry, Spine.Event e)
        {
            if (_owner == null)
            {
                return;
            }

            OnSpineEvent(_owner, e.Data.Name);
        }

        private void OnAttackAnimationComplete(TrackEntry trackEntry)
        {
            if (_owner == null)
            {
                return;
            }

            if (trackEntry != null && trackEntry.Animation.Name != "atk")
            {
                return;
            }

            _owner.AnimationController.UnregisterAllSpineCallbacks();
            _owner.Play("idle", true);
            _owner.isAttackAnimationPlaying = false;
            _owner.isInAttackCooldown = false;
        }
    }
}
