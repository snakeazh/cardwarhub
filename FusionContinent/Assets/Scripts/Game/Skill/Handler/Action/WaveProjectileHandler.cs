using System;
using System.Collections.Generic;
using Config;
using Cysharp.Threading.Tasks;
using FusionContinent.Skill.Config;
using UnityEngine;

namespace FusionContinent.Skill.Handler
{
    /// <summary>
    /// 波纹投射物节点处理器 - 支持沿方向向量多轮推进的投射物
    /// 类似爆炎斩效果：沿拖拽方向每隔一段时间发射一路火球，逐步向外扩散
    /// </summary>
    public class WaveProjectileHandler : AsyncSkillNodeHandler
    {
        public override string NodeType => "WaveProjectile";

        public override async UniTask Execute(SkillNodeConfig node, SkillContext context)
        {
            var prefab = node.GetParamString("prefab", "Prefabs/Skills/Projectile");
            var hitEffect = node.GetParamString("hit_effect", "");
            var endEffect = node.GetParamString("end_effect", "");

            // 波纹配置
            var forkCount = node.GetParamInt("fork_count", 1);           // 分叉数量：1=正中，2=+左，3=+右
            var totalRounds = node.GetParamInt("total_rounds", 5);       // 总共打几轮
            var forwardStep = node.GetParamFloat("forward_step", 2f);    // 每轮推进距离
            var forkAngle = node.GetParamFloat("fork_angle", 15f);       // 左右分叉角度
            var roundInterval = node.GetParamFloat("round_interval", 0.2f); // 每轮间隔
            var lifetime = node.GetParamFloat("lifetime", 1f);           // 特效存活时间

            // 命中检测配置
            var maxPenetrateCount = node.GetParamInt("max_penetrate", 0);
            var hitTargetDuration = node.GetParamFloat("hit_target_duration", 0f);

            Debug.Log($"[WaveProjectileHandler] prefab={prefab}, forkCount={forkCount}, totalRounds={totalRounds}, forwardStep={forwardStep}");
            Debug.Log($"[WaveProjectileHandler] TargetPosition={context.TargetPosition}");

            // context.TargetPosition 已经是方向向量（目标位置减过施法者位置）
            Vector3 direction = context.TargetPosition;
            if (direction.sqrMagnitude < 0.001f)
            {
                direction = Vector3.up;
            }
            else
            {
                direction = direction.normalized;
            }

            // basePos: 发射起点（施法者位置）
            Vector3 basePos = context.Caster?.transform.position ?? Vector3.zero;

            // 计算缩放
            float sizeCorrection = context.Caster != null
                ? context.Caster.GetComponent<SoldierCard>().BaseAttributes.GetFinal(AttributeType.SoldierBulletSizeCorrection)
                : 1f;
            float globalScale = 1f;
            if (context.Caster != null)
            {
                float skillRange = context.Caster.GetComponent<SoldierCard>().BaseAttributes.GetFinal(AttributeType.SkillRange);
                globalScale = (100f + skillRange) / 100f * sizeCorrection;
            }

            // 计算基础缩放
            var baseScale = node.GetParamFloat("scale", 100f) / 100f * globalScale;

            // 计算"右方向"用于分叉计算
            Vector3 rightDir = Vector3.Cross(direction, Vector3.forward).normalized;

            // 分叉角度转弧度
            float forkRad = forkAngle * Mathf.Deg2Rad;

            // 获取技能卡片
            ISkill skillCard = null;
            if (context.Caster != null)
            {
                var soldierCard = context.Caster.GetComponent<SoldierCard>();
                if (soldierCard != null)
                {
                    skillCard = soldierCard.GetSkillById(context.SkillId);
                }
            }

            // 多轮发射
            for (int round = 0; round < totalRounds; round++)
            {
                int currentRound = round;

                // 每轮延迟
                await UniTask.Delay((int)(roundInterval * 1000));

                // 这一轮同时生成所有分叉
                for (int fork = 0; fork < forkCount; fork++)
                {
                    // 计算这一轮的前进距离
                    float forwardOffset = forwardStep * currentRound;

                    // 计算分叉的侧向偏移（按角度展开，距离随前进距离放大）
                    float sideOffset = 0f;
                    Vector3 forkDirection = direction;

                    if (fork == 1) // 左
                    {
                        sideOffset = -Mathf.Tan(forkRad) * forwardOffset;
                        forkDirection = (direction + rightDir * (-Mathf.Tan(forkRad))).normalized;
                    }
                    else if (fork == 2) // 右
                    {
                        sideOffset = Mathf.Tan(forkRad) * forwardOffset;
                        forkDirection = (direction + rightDir * (Mathf.Tan(forkRad))).normalized;
                    }

                    // 计算生成位置
                    Vector3 forwardOffsetVec = direction * forwardOffset;
                    Vector3 sideOffsetVec = rightDir * sideOffset;
                    Vector3 spawnPos = basePos + forwardOffsetVec + sideOffsetVec;

                    // 计算朝向角度
                    float angle = Mathf.Atan2(forkDirection.y, forkDirection.x) * Mathf.Rad2Deg;

                    // 计算缩放
                    float scale = baseScale;

                    // 创建投射物
                    CreateProjectile(prefab, spawnPos, angle, scale,
                        maxPenetrateCount, hitTargetDuration, hitEffect, endEffect,
                        skillCard, context);

                    Debug.Log($"[WaveProjectileHandler] Round={currentRound}, Fork={fork}, pos={spawnPos}, dir={forkDirection}");
                }
            }
        }

        private void CreateProjectile(string prefab, Vector3 position, float angle,
            float scale,
            int maxPenetrate, float hitTargetDuration, string hitEffect, string endEffect,
            ISkill skillCard, SkillContext context)
        {
            var effect = EffectManager.Instance.PlayEffect(prefab, position,
                scale: new Vector3(scale, scale, scale),
                parent: GameLand.Default.Bullet, lifetime: 0.7f);

            if (effect != null)
            {
                context.TrackedEffects.Add(effect);
            }

            // 如果特效预制体上有 BoxCollider2D，附加 BulletDamageDetector 做伤害检测
            var detector = effect.GetComponentInChildren<BulletDamageDetector>();
            if (detector != null)
            {
                detector.Init(context, skillCard, maxPenetrate, hitTargetDuration, hitEffect);
            }

            Debug.Log($"[WaveProjectileHandler] 创建投射物 pos={position}, angle={angle}, scale={scale:F2}");
        }
    }
}
