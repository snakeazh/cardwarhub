using DG.Tweening;
using FusionContinent;
using QFramework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FusionContinent.Monster.AttackBehaviour
{
    public sealed class AlienLaserAttackBehaviour : SpineLoopAttackBehaviourBase
    {
        private GameObject _effectRoot;

        public override void OnCleanup(MonsterCard owner)
        {
            StopEngage(owner);
            base.OnCleanup(owner);
        }

        public override void StopEngage(MonsterCard owner)
        {
            DestroyAlienEffect(owner);
            base.StopEngage(owner);
        }

        protected override void ExecuteAttackHit(MonsterCard owner, string eventName)
        {
            if (_effectRoot != null)
            {
                return;
            }

            GameObject waixingren_jn1_01 = GamePlayManager.Default._resLoader
                .Get<GameObject>("waixingren_jn1_01")
                .Instantiate();
            waixingren_jn1_01.transform.SetParent(owner.transform.Find("iconatk/外星人point"));
            waixingren_jn1_01.name = "waixingren_jn1_01";
            waixingren_jn1_01.transform.localPosition = Vector3.zero;
            waixingren_jn1_01.transform.Find("pos").position = owner.transform.position;
            waixingren_jn1_01.transform.localScale = Vector3.one * 2;
            var wallTransform = GameLand.Default.GetLane(owner.BattleSide)?.Wall?.transform;
            if (wallTransform == null)
            {
                wallTransform = GameLand.Default.wall.transform;
            }

            waixingren_jn1_01.transform.Find("pos").localScale = new Vector3(
                1,
                Mathf.Abs(owner.transform.position.y - wallTransform.position.y) / 32.0f,
                1);
            _effectRoot = waixingren_jn1_01;

            GameObject waixingren_jn1_02 = GamePlayManager.Default._resLoader
                .Get<GameObject>("waixingren_jn1_02")
                .Instantiate();
            waixingren_jn1_02.transform.SetParent(waixingren_jn1_01.transform.Find("pos"));
            waixingren_jn1_02.transform.localPosition = Vector3.zero;
            waixingren_jn1_02.transform.localScale = Vector3.one;

            GameObject waixingren_jn1_03 = GamePlayManager.Default._resLoader
                .Get<GameObject>("waixingren_jn1_03")
                .Instantiate();
            waixingren_jn1_03.transform.SetParent(
                waixingren_jn1_01.transform.Find("pos/eff_wxr_jn1_01/jiguang05"));
            waixingren_jn1_03.transform.localPosition = Vector3.zero;
            waixingren_jn1_03.transform.localScale = Vector3.one * 0.01f;

            var duration = owner.attackTime - 5f;
            var tween = DOTween.To(() => 0f, _ => TickAlienDamage(owner), 0, duration)
                .SetEase(Ease.Linear)
                .OnComplete(() => DestroyAlienEffect(owner));
            Runtime.RegisterTween(tween);
        }

        private void TickAlienDamage(MonsterCard owner)
        {
            if (GamePlayManager.Default.IsPaused())
            {
                return;
            }

            MonsterAttackPresentationHelper.ShakeCameraIfEliteOrBoss(owner);
            MonsterAttackPresentationHelper.DamageWall(owner);
        }

        private void DestroyAlienEffect(MonsterCard owner)
        {
            if (_effectRoot != null)
            {
                Object.Destroy(_effectRoot);
                _effectRoot = null;
                return;
            }

            var effect = owner.transform.Find("iconatk/外星人point/waixingren_jn1_01");
            if (effect != null)
            {
                Object.Destroy(effect.gameObject);
            }
        }
    }
}
