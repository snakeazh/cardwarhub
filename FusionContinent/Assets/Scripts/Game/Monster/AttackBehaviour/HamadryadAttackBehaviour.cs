using System;
using DG.Tweening;
using FusionContinent;
using QFramework;
using UnityEngine;

namespace FusionContinent.Monster.AttackBehaviour
{
    public sealed class HamadryadAttackBehaviour : MonsterAttackBehaviourBase
    {
        private Tween _engageTween;

        public override bool TryHandleStraightTravelArrival(MonsterCard owner, Action onReady)
        {
            owner.SetPhysicsPosition(new Vector3(999999, 999999, 0));
            var spawnTween = DOTween.To(v => { }, 0, 0, 1.5f).OnComplete(() =>
            {
                GameObject shujingCc = GamePlayManager.Default._resLoader
                    .Get<GameObject>("shujing_cc_01")
                    .Instantiate();
                shujingCc.transform.position = Vector3.zero + new Vector3(0, 5, 0);
                shujingCc.transform.SetParent(owner.transform);
                var moveTween = DOTween.To(v => { }, 0, 0, 0.7f).OnComplete(() =>
                {
                    owner.SetPhysicsPosition(shujingCc.transform.position);
                    UnityEngine.Object.Destroy(shujingCc);
                    onReady();
                });
                Runtime.RegisterTween(moveTween);
            });
            Runtime.RegisterTween(spawnTween);
            return true;
        }

        public override void OnCleanup(MonsterCard owner)
        {
            StopEngage(owner);
            base.OnCleanup(owner);
        }

        public override void BeginEngage(MonsterCard owner)
        {
            StopEngage(owner);

            owner.transform.Find("shujing_jn2_01").localScale = Vector3.one * 100;
            _engageTween = DOTween.Sequence()
                .Append(DOTween.To(v => { }, 0, 0, 0f).SetEase(Ease.InOutQuad))
                .AppendInterval(owner.attackTime)
                .SetLoops(-1, LoopType.Restart)
                .OnStepComplete(() =>
                {
                    owner.transform.Find("shujing_jn2_01").GetChild(0).GetComponent<ParticleSystem>().Play();
                    var delayTween = DOTween.To(v => { }, 0, 0, 0.5f).OnComplete(() =>
                    {
                        MonsterAttackPresentationHelper.DamageWall(owner);
                    });
                    Runtime.RegisterTween(delayTween);
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
        }
    }
}
