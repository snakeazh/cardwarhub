using DG.Tweening;
using FusionContinent;
using QFramework;
using UnityEngine;
using UnityEngine.UI;

namespace FusionContinent.Monster.AttackBehaviour
{
    public sealed class DragonKingAttackBehaviour : SpineLoopAttackBehaviourBase
    {
        public override bool DeferStraightTravelInit => true;

        public override void OnSpawnPresentation(MonsterCard owner)
        {
            DOTween.To(v => { }, 0, 0, 1f).OnComplete(() =>
            {
                owner.Play("roar", false);
                GenericObjectPool pool =
                    ObjectPoolManager.Instance.GetGameObjectPoolOrCreate("ChaosDragonKingRoar01");
                if (pool != null)
                {
                    var bullet = pool.Get();
                    bullet.transform.SetParent(GameLand.Default.Bullet);
                    bullet.transform.localPosition = owner.transform.position;
                    bullet.transform.localScale = Vector3.one;
                    MonsterAttackPresentationHelper.ReturnPooledAfterDelay(Runtime, pool, bullet.gameObject, 4f);
                }

                var readyTween = DOTween.To(v => { }, 0, 0, 1.2f).OnComplete(() =>
                {
                    owner.IsInit = true;
                    owner.Play("idle", true);
                });
                Runtime.RegisterTween(readyTween);
            });
        }

        protected override void ExecuteAttackHit(MonsterCard owner, string eventName)
        {
            GenericObjectPool pool = ObjectPoolManager.Instance.GetGameObjectPoolOrCreate("ChaosDragonKingAttack01");
            if (pool == null)
            {
                return;
            }

            var bullet = pool.Get();
            bullet.transform.SetParent(GameLand.Default.Bullet);
            if (owner._monsterConfig.Bullet != "")
            {
                bullet.transform.Find("bulleticon").GetComponent<Image>().sprite =
                    Global.CardIconSpriteAtlas.GetSpriteExt(owner._monsterConfig.Bullet);
                bullet.transform.Find("bulleticon").GetComponent<Image>().SetNativeSize();
            }

            bullet.transform.position = MonsterAttackPresentationHelper.GetAttackPoint(owner);
            bullet.transform.localScale = new Vector3(1f, 0f, 1f);
            var scriptBullet = bullet.GetComponent<FlameBulletController>();
            scriptBullet.monsterCard = owner;
            scriptBullet.atk = owner.atk;
            scriptBullet.pool = pool;

            GenericObjectPool pool2 =
                ObjectPoolManager.Instance.GetGameObjectPoolOrCreate("ChaosDragonKingAttack02");
            if (pool2 != null)
            {
                var bullet2 = pool2.Get();
                bullet2.transform.SetParent(GameLand.Default.Bullet);
                bullet2.transform.position = MonsterAttackPresentationHelper.GetAttackPoint(owner);
                bullet2.transform.localScale = Vector3.one;
                MonsterAttackPresentationHelper.ReturnPooledAfterDelay(Runtime, pool2, bullet2.gameObject, 4f);
            }
        }
    }
}
