using Config;
using FusionContinent;
using QFramework;
using UnityEngine;

namespace FusionContinent.Monster.AttackBehaviour
{
    internal static class MonsterAttackPresentationHelper
    {
        public static void ShakeCameraIfEliteOrBoss(MonsterCard owner)
        {
            if (owner._monsterConfig.MonsterQuality == MonsterQuality.Elite
                || owner._monsterConfig.MonsterQuality == MonsterQuality.Boss)
            {
                Camera.main.GetComponent<CameraShake>().ShakeCamera();
            }
        }

        public static void DamageWall(MonsterCard owner)
        {
            // PVP：对侧怪打对侧墙，不再误伤玩家墙
            var wall = GameLand.Default.GetLane(owner.BattleSide)?.Wall;
            if (wall == null)
            {
                wall = GameLand.Default.wall;
            }

            wall.TakeDamage(owner.atk, owner);
        }

        public static void ReturnPooledAfterDelay(
            AttackBehaviourRuntime runtime,
            GenericObjectPool pool,
            GameObject instance,
            float delay)
        {
            if (pool == null || instance == null)
            {
                return;
            }

            var tween = DG.Tweening.DOVirtual.DelayedCall(delay, () =>
            {
                if (instance != null)
                {
                    pool.Return(instance);
                }
            });
            runtime.RegisterTween(tween);
        }

        public static Vector3 GetAttackPoint(MonsterCard owner)
        {
            var point = owner.transform.Find("iconatk").transform.Find("Point");
            point.localPosition = Vector2.zero;
            return point.position;
        }
    }
}
