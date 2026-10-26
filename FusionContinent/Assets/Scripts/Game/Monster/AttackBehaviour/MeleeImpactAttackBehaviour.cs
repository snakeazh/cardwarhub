using FusionContinent;
using QFramework;
using UnityEngine;

namespace FusionContinent.Monster.AttackBehaviour
{
    public sealed class MeleeImpactAttackBehaviour : SpineLoopAttackBehaviourBase
    {
        protected override void ExecuteAttackHit(MonsterCard owner, string eventName)
        {
            MonsterAttackPresentationHelper.ShakeCameraIfEliteOrBoss(owner);

            GenericObjectPool pool = ObjectPoolManager.Instance.GetGameObjectPool("guaiwu_gongji_01");
            if (pool == null)
            {
                ObjectPoolManager.Instance.CreateGameObjectPool(
                    "guaiwu_gongji_01",
                    GamePlayManager.Default._resLoader.Get<GameObject>("guaiwu_gongji_01"));
                pool = ObjectPoolManager.Instance.GetGameObjectPool("guaiwu_gongji_01");
            }

            if (pool != null)
            {
                var damageObj = pool.Get();
                damageObj.transform.position = owner.Position() - new Vector3(-0.1f, owner.transform.localScale.y, 0);
                MonsterAttackPresentationHelper.ReturnPooledAfterDelay(Runtime, pool, damageObj, 0.2f);
            }

            MonsterAttackPresentationHelper.DamageWall(owner);
        }
    }
}
