using FusionContinent;
using QFramework;
using UnityEngine;
using UnityEngine.UI;

namespace FusionContinent.Monster.AttackBehaviour
{
    public sealed class LvDouAttackBehaviour : SpineLoopAttackBehaviourBase
    {
        protected override void ExecuteAttackHit(MonsterCard owner, string eventName)
        {
            var target = owner.targetSolidierCard;
            if (target == null || target.isDie)
            {
                owner.monsterData.monsterState = MonsterState.move;
                owner.Tick();
                return;
            }

            FaceTarget(owner, target);

            GenericObjectPool pool = ObjectPoolManager.Instance.GetGameObjectPool("Bullet");
            if (pool == null)
            {
                ObjectPoolManager.Instance.CreateGameObjectPool(
                    "Bullet",
                    GamePlayManager.Default._resLoader.Get<GameObject>("Bullet"));
                pool = ObjectPoolManager.Instance.GetGameObjectPool("Bullet");
            }

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
            bullet.transform.localScale = Vector3.one;

            var controller = bullet.GetComponent<BulletController>();
            controller.monsterCard = owner;
            controller.atk = owner.atk;
            controller.pool = pool;
            controller.moveDuration = owner._monsterConfig.BulletSpeed / 100.0f;
            controller.LaunchToSoldier(target);
        }

        private static void FaceTarget(MonsterCard owner, SoldierCard target)
        {
            if (target.PlayerspineObj == null)
            {
                return;
            }

            float targetX = target.PlayerspineObj.transform.position.x;
            owner.AnimationController.SetSkeletonScaleX(targetX < owner.transform.position.x ? 1 : -1);
        }
    }
}
