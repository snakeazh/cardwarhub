using System.Collections.Generic;
using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using FusionContinent.Skill.Config;
using UnityEngine;
using AttributeType = Config.AttributeType;

namespace FusionContinent.Skill.Handler
{
    /// <summary>
    /// 动画特效节点处理器
    /// 支持带动画过程的特效（如陨石坠落、上升等）
    /// </summary>
    public class AnimatedEffectHandler : AsyncSkillNodeHandler
    {
        public override string NodeType => "AnimatedEffect";

        public override async UniTask Execute(SkillNodeConfig node, SkillContext context)
        {
            // 清理已回收的特效引用
            CleanupInvalidEffects(context);

            var vfxPath = node.GetParamString("vfx", "");
            var craterVfxPath = node.GetParamString("crater_vfx", "");
            var animationType = node.GetParamString("animation_type", "Fall");
            var position = node.GetParamString("position", "Target");
            var randomOffsetRadius = node.GetParamFloat("random_offset_radius", 0f);

            // 解析 vfx_scale 参数，支持 "x,y" 格式或从技能属性读取
            Vector3? vfxScale = null;
            var scaleAttrName = node.GetParamString("vfx_scale_attr", "");
            var scaleRaw = node.GetParamString("vfx_scale", "");

            // 优先从技能属性读取缩放值
            if (!string.IsNullOrEmpty(scaleAttrName))
            {
                ISkill skillCard = null;
                if (context.Caster != null)
                {
                    var soldierCard = context.Caster.GetComponent<SoldierCard>();
                    if (soldierCard != null)
                    {
                        skillCard = soldierCard.GetSkillById(context.SkillId);
                    }
                }

                if (skillCard != null && Enum.TryParse<AttributeType>(scaleAttrName, out var scaleAttr))
                {
                    var scaleValue = skillCard.Attributes.GetFinal(scaleAttr);
                    vfxScale = new Vector3(scaleValue, scaleValue, 1f);
                    LogTool.Log($"[AnimatedEffectHandler] 从属性 {scaleAttrName} 读取 vfx_scale={scaleValue}");
                }
            }
            else if (!string.IsNullOrEmpty(scaleRaw))
            {
                var parts = scaleRaw.Split(',');
                if (parts.Length == 2 &&
                    float.TryParse(parts[0], out float sx) &&
                    float.TryParse(parts[1], out float sy))
                {
                    vfxScale = new Vector3(sx, sy, 1f);
                }
                else if (float.TryParse(parts[0], out float s))
                {
                    vfxScale = new Vector3(s, s, 1f);
                }
            }

            LogTool.Log($"[AnimatedEffectHandler] 播放动画特效: vfx={vfxPath}, type={animationType}, position={position}, offset={randomOffsetRadius}, scale={vfxScale}");

            if (string.IsNullOrEmpty(vfxPath))
            {
                LogTool.LogWarning("[AnimatedEffectHandler] vfx 为空，跳过");
                return;
            }

            if (!SkillTargetResolver.TryResolveEffectPosition(
                    context, position, randomOffsetRadius, "AnimatedEffectHandler", out var targetPos))
            {
                return;
            }

            switch (animationType)
            {
                case "Fall":
                    await ExecuteFall(vfxPath, craterVfxPath, targetPos, context, vfxScale);
                    break;
                case "Rise":
                    await ExecuteRise(vfxPath, craterVfxPath, targetPos, context, vfxScale);
                    break;
                case "RequiemChant":
                    await ExecuteRequiemChant(vfxPath, craterVfxPath, targetPos, position, context, vfxScale);
                    break;
                case "zhanji":
                    await ExecuteZhanJi(vfxPath, craterVfxPath, targetPos, context, vfxScale);
                    break;
                default:
                    await ExecuteFall(vfxPath, craterVfxPath, targetPos, context, vfxScale);
                    break;
            }
        }

        /// <summary>
        /// 陨石动画
        /// </summary>
        /// <param name="vfxPath"></param>
        /// <param name="craterVfxPath"></param>
        /// <param name="targetPos"></param>
        /// <param name="context"></param>
        private async UniTask ExecuteFall(string vfxPath, string craterVfxPath, Vector3 targetPos, SkillContext context, Vector3? vfxScale = null)
        {
            var meteor = EffectManager.Instance.PlayEffect(vfxPath, targetPos, scale: vfxScale);
            if (meteor == null)
            {
                LogTool.LogWarning($"[AnimatedEffectHandler] Fall 特效创建失败: {vfxPath}");
                return;
            }

            context.TrackedEffects.Add(meteor);

            float dropAngle = 45f;
            float height = 6f;
            float speed = 18f;

            float rad = dropAngle * Mathf.Deg2Rad;
            float horizontalOffset = height / Mathf.Tan(rad);

            Vector3 startPos = targetPos
                               + Vector3.right * horizontalOffset
                               + Vector3.up * height;

            meteor.transform.position = startPos;
            meteor.transform.rotation = Quaternion.Euler(0, 0, -dropAngle);

            float distance = Vector3.Distance(startPos, targetPos);
            float duration = distance / speed;
            bool recycled = false;

            void RecycleMeteor()
            {
                if (recycled || meteor == null || !meteor.activeInHierarchy) return;
                recycled = true;
                EffectManager.Instance.RecycleEffect(meteor);
            }

            var tween = meteor.transform.DOMove(targetPos, duration)
                .SetEase(Ease.Linear)
                .OnComplete(() =>
                {
                    RecycleMeteor();
                    PlayCrater(craterVfxPath, targetPos);
                })
                .OnKill(() =>
                {
                    // 结算时 DOTween.KillAll 会触发 OnKill，确保特效不会残留到下一局
                    RecycleMeteor();
                });
            try
            {
                await UniTask.Delay((int)(duration * 1000), cancellationToken: context.CancellationToken.Token);
            }
            catch (OperationCanceledException)
            {
                if (tween != null && tween.IsActive()) tween.Kill();
                throw;
            }
        }

        private async UniTask ExecuteRise(string vfxPath, string craterVfxPath, Vector3 targetPos, SkillContext context, Vector3? vfxScale = null)
        {
            var effect = EffectManager.Instance.PlayEffect(vfxPath, targetPos, scale: vfxScale);
            if (effect == null)
            {
                LogTool.LogWarning($"[AnimatedEffectHandler] Rise 特效创建失败: {vfxPath}");
                return;
            }
            context.TrackedEffects.Add(effect);

            float height = 8f;
            float speed = 10f;
            float duration = height / speed;

            Vector3 startPos = targetPos;
            Vector3 endPos = targetPos + Vector3.up * height;

            effect.transform.position = startPos;
            bool recycled = false;

            void RecycleEffect()
            {
                if (recycled || effect == null || !effect.activeInHierarchy) return;
                recycled = true;
                EffectManager.Instance.RecycleEffect(effect);
            }

            var tween = effect.transform.DOMove(endPos, duration)
                .SetEase(Ease.Linear)
                .OnComplete(() =>
                {
                    RecycleEffect();
                })
                .OnKill(() =>
                {
                    RecycleEffect();
                });
            try
            {
                await UniTask.Delay((int)(duration * 1000), cancellationToken: context.CancellationToken.Token);
            }
            catch (OperationCanceledException)
            {
                if (tween != null && tween.IsActive()) tween.Kill();
                throw;
            }
        }

        /// <summary>
        /// 死神挥砍动画
        /// </summary>
        private async UniTask ExecuteZhanJi(string vfxPath, string craterVfxPath, Vector3 targetPos, SkillContext context, Vector3? vfxScale = null)
        {
            var effect = EffectManager.Instance.PlayEffect(vfxPath, targetPos, scale: vfxScale);
            if (effect == null)
            {
                LogTool.LogWarning($"[AnimatedEffectHandler] zhanji 特效创建失败: {vfxPath}");
                return;
            }
            context.TrackedEffects.Add(effect);

            // zhanji 默认没有位移动画，使用一个可配置的兜底时长，避免战斗切局后特效常驻
            float duration = 1.2f;
            bool recycled = false;
            void RecycleEffect()
            {
                if (recycled || effect == null || !effect.activeInHierarchy) return;
                recycled = true;
                EffectManager.Instance.RecycleEffect(effect);
            }

            try
            {
                await UniTask.Delay((int)(duration * 1000), cancellationToken: context.CancellationToken.Token);
            }
            catch (OperationCanceledException)
            {
                RecycleEffect();
                throw;
            }

            RecycleEffect();
        }

        /// <summary>
        /// 慰灵圣咏动画
        /// 步骤：
        /// 1. 在施法者头顶生成珠子
        /// 2. 等待1秒后，向 position 解析出的每个目标位置发射抛物线飞行的珠子
        /// position=Selected 时向 SelectedTargets 中每个目标各飞一颗；单目标时仅一颗
        /// 整体动画持续约 1.8 秒（与 Delay 节点配合）
        /// </summary>
        private async UniTask ExecuteRequiemChant(
            string vfxPath,
            string craterVfxPath,
            Vector3 targetPos,
            string position,
            SkillContext context,
            Vector3? vfxScale = null)
        {
            if (context.Caster == null) return;

            var targetPositions = ResolveRequiemChantTargetPositions(context, position, targetPos);
            if (targetPositions.Count == 0) return;

            var caster = context.Caster.transform;
            var casterPos = caster.position;
            var ball1Name = $"{vfxPath}";
            var ball2Name = $"{craterVfxPath}";
            var effectScale = vfxScale ?? new Vector3(1, 1, 1);
            var spawnPos = new Vector3(casterPos.x, casterPos.y + 1f, 0);

            var ball1 = EffectManager.Instance.PlayEffect(
                ball1Name,
                spawnPos,
                lifetime: 1.9f,
                scale: effectScale,
                parent: caster);
            if (ball1 == null)
            {
                LogTool.LogWarning($"[AnimatedEffectHandler] RequiemChant ball1 特效创建失败: {ball1Name}");
                return;
            }
            ball1.transform.localScale = effectScale;
            context.TrackedEffects.Add(ball1);

            await UniTask.Delay(1000, cancellationToken: context.CancellationToken.Token);

            foreach (var endPos in targetPositions)
            {
                LaunchRequiemChantBall(ball2Name, spawnPos, endPos, effectScale, context);
            }
        }

        /// <summary>
        /// position=Selected 时取 SelectedTargets 全部位置；否则使用单点 targetPos。
        /// </summary>
        private static List<Vector3> ResolveRequiemChantTargetPositions(
            SkillContext context,
            string position,
            Vector3 targetPos)
        {
            var positions = new List<Vector3>();
            var offset = new Vector3(context.EffectOffset.x, context.EffectOffset.y, 0);

            if (position == SkillTargetResolver.ModeSelected)
            {
                foreach (var component in context.SelectedTargets)
                {
                    positions.Add(component.transform.position + offset);
                }
            }

            if (positions.Count == 0)
                positions.Add(targetPos);

            return positions;
        }

        private void LaunchRequiemChantBall(string ball2Name, Vector3 spawnPos, Vector3 endPos, Vector3 effectScale, SkillContext context)
        {
            var ball2 = EffectManager.Instance.PlayEffect(
                ball2Name,
                spawnPos,
                scale: effectScale);
            if (ball2 == null) return;

            ball2.transform.localScale = effectScale;
            context.TrackedEffects.Add(ball2);

            bool recycled = false;
            void RecycleBall2()
            {
                if (recycled || ball2 == null || !ball2.activeInHierarchy) return;
                recycled = true;
                EffectManager.Instance.RecycleEffect(ball2);
            }

            ball2.transform
                .DOJump(new Vector3(endPos.x, endPos.y, 0), 2f, 1, 0.8f)
                .SetEase(Ease.OutQuad)
                .OnComplete(RecycleBall2)
                .OnKill(RecycleBall2);
        }

        private void PlayCrater(string craterVfxPath, Vector3 targetPos)
        {
            if (!string.IsNullOrEmpty(craterVfxPath))
            {
                EffectManager.Instance.PlayEffect(craterVfxPath, targetPos);
            }
        }

        /// <summary>
        /// 清理已回收的特效引用（null 或非活跃状态）
        /// </summary>
        private void CleanupInvalidEffects(SkillContext context)
        {
            if (context.TrackedEffects.Count == 0) return;

            int beforeCount = context.TrackedEffects.Count;
            context.TrackedEffects.RemoveAll(e => e == null || !e.activeInHierarchy);

            if (context.TrackedEffects.Count < beforeCount)
            {
                LogTool.Log($"[AnimatedEffectHandler] 清理了 {beforeCount - context.TrackedEffects.Count} 个已回收特效");
            }
        }
    }
}
