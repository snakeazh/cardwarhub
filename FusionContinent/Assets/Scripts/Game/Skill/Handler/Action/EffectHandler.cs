using FusionContinent.Skill.Config;
using System;
using Config;
using UnityEngine;
using AttributeType = Config.AttributeType;

namespace FusionContinent.Skill.Handler
{
    /// <summary>
    /// 特效/音效节点处理器
    /// </summary>
    public class EffectHandler : SyncSkillNodeHandler
    {
        public override string NodeType => "Effect";
        
        private float _continuousAtkSpeed = 1.2f;

        public override void ExecuteSync(SkillNodeConfig node, SkillContext context)
        {
            var vfxPath = node.GetParamString("vfx", "");
            var sfxPath = node.GetParamString("sfx", "");
            var position = node.GetParamString("position", "Target");
            var rotation = node.GetParamString("rotation", "None");
            var randomOffsetRadius = node.GetParamFloat("random_offset_radius", 0f);
            var lockX = node.GetParamBool("lock_x", false);
            var lockedXValue = node.GetParamFloat("locked_x_value", 0f);

            // 清理已回收的特效引用，防止列表膨胀
            CleanupInvalidEffects(context);

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
                    LogTool.Log($"[EffectHandler] 从属性 {scaleAttrName} 读取 vfx_scale={scaleValue}");
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

            LogTool.Log($"[EffectHandler] 播放特效: vfx={vfxPath}, sfx={sfxPath}, position={position}, rotation={rotation}, scale={vfxScale}");

            // 播放音效
            if (!string.IsNullOrEmpty(sfxPath))
            {
                LogTool.Log($"[EffectHandler] 播放音效: {sfxPath}");
                // AudioManager.Play(sfxPath);
            }

            // 播放特效
            if (!string.IsNullOrEmpty(vfxPath))
            {
                if (!SkillTargetResolver.TryResolveEffectPosition(
                        context, position, randomOffsetRadius, "EffectHandler", out var targetPos))
                {
                    return;
                }

                if (lockX)
                {
                    targetPos.x = lockedXValue;
                    // 将锁定后的坐标写回上下文，确保后续节点基于新坐标继续计算
                    context.TargetPosition = targetPos;
                }

                var effect = EffectManager.Instance.PlayEffect(vfxPath, targetPos, scale: vfxScale);
                if (effect != null)
                {
                    // 朝向设置
                    ApplyRotation(effect.transform, rotation, context);
                    context.TrackedEffects.Add(effect);
                    LogTool.Log($"[EffectHandler] 特效已加入追踪列表，当前追踪数量: {context.TrackedEffects.Count}");
                }
            }
        }

    private void ApplyRotation(Transform effectTransform, string rotation, SkillContext context)
    {
        switch (rotation)
        {
            case "FaceCaster":
                // 朝向施法者
                if (context.Caster != null && context.TargetPosition != Vector3.zero)
                {
                    var dir = (context.Caster.transform.position - context.TargetPosition).normalized;
                    if (dir.sqrMagnitude > 0.001f)
                    {
                        effectTransform.eulerAngles = new Vector3(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
                    }
                }

                
                break;
            case "FaceTarget":
                // 朝向目标
                if (context.Caster != null)
                {
                    var dir = (context.TargetPosition - context.Caster.transform.position).normalized;
                    if (dir.sqrMagnitude > 0.001f)
                    {
                        effectTransform.eulerAngles = new Vector3(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
                        
                    }
                    
                    effectTransform.up = dir;
                }
                LogTool.Log("特效朝向目标");
                break;
            case "Up":
                effectTransform.eulerAngles = new Vector3(0, 0, -90f);
                break;
            case "Down":
                effectTransform.eulerAngles = new Vector3(0, 0, 90f);
                break;
            case "Left":
                effectTransform.eulerAngles = new Vector3(0, 0, 180f);
                break;
            case "Right":
                effectTransform.eulerAngles = new Vector3(0, 0, 0);
                break;
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
            LogTool.Log($"[EffectHandler] 清理了 {beforeCount - context.TrackedEffects.Count} 个已回收特效");
        }
    }
}
}
