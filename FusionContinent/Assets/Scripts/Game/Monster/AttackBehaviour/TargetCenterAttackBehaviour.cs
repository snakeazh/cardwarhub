using System;
using FusionContinent;
using QFramework;
using UnityEngine;

namespace FusionContinent.Monster.AttackBehaviour
{
    /// <summary>
    /// 中心点目标位置攻击：在配置 X 与当前索敌人物 Y 处直接生成 Bullet 特效并造成城墙伤害。
    /// </summary>
    public sealed class TargetCenterAttackBehaviour : SpineLoopAttackBehaviourBase
    {
        private const float EffectReturnDelay = 2f;

        protected override void ExecuteAttackHit(MonsterCard owner, string eventName)
        {
            var target = owner.targetSolidierCard;
            if (target == null || target.isDie)
            {
                owner.monsterData.monsterState = MonsterState.move;
                owner.Tick();
                return;
            }

            string effectName = owner._monsterConfig.Bullet;
            if (string.IsNullOrEmpty(effectName))
            {
                LogTool.LogError(
                    "[TargetCenterAttackBehaviour] Bullet 为空，无法生成攻击特效. MonsterId="
                    + owner._monsterConfig.Id);
                throw new InvalidOperationException(
                    "TargetCenter attack requires Bullet effect name. MonsterId="
                    + owner._monsterConfig.Id);
            }

            Vector3 hitPos = ResolveHitPosition(owner, target);
            SpawnAttackEffect(owner, effectName, hitPos);
            MonsterAttackPresentationHelper.ShakeCameraIfEliteOrBoss(owner);
            MonsterAttackPresentationHelper.DamageWall(owner);
        }

        private static Vector3 ResolveHitPosition(MonsterCard owner, SoldierCard target)
        {
            float soldierY = target.PlayerspineObj != null
                ? target.PlayerspineObj.transform.position.y
                : target.transform.position.y;

            var atkLocation = owner._monsterConfig.AtkLocation;
            return new Vector3(atkLocation.X, soldierY + atkLocation.Y, 0f);
        }

        private void SpawnAttackEffect(MonsterCard owner, string effectName, Vector3 hitPos)
        {
            GenericObjectPool pool = ObjectPoolManager.Instance.GetGameObjectPoolOrCreate(effectName);
            if (pool == null)
            {
                LogTool.LogError(
                    "[TargetCenterAttackBehaviour] 无法创建特效对象池. Effect="
                    + effectName
                    + ", MonsterId=" + owner._monsterConfig.Id);
                throw new InvalidOperationException(
                    "Failed to create attack effect pool. Effect=" + effectName);
            }

            var effect = pool.Get();
            effect.transform.SetParent(GameLand.Default.Bullet);
            effect.transform.position = hitPos;
            effect.transform.localScale = Vector3.one;
            MonsterAttackPresentationHelper.ReturnPooledAfterDelay(Runtime, pool, effect, EffectReturnDelay);
        }
    }
}
