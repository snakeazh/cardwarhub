using System;
using System.Collections.Generic;
using Config;
using Cysharp.Threading.Tasks;
using FusionContinent.Skill.Config;
using UnityEngine;
using SimpleJSON;

namespace FusionContinent.Skill.Handler
{
    /// <summary>
    /// 多重投射物节点处理器 - 支持从配置列表发射多个投射物
    /// 用于瞬袭斩等技能：多个固定偏移点 + 循环次数
    /// 支持两种数据源：
    /// 1. offset_config_key: 从配表读取（如 "SkillSwiftAssaultOffset"）
    /// 2. offset_list: 直接填写字符串格式
    /// </summary>
    public class MultiProjectileHandler : AsyncSkillNodeHandler
    {
        public override string NodeType => "MultiProjectile";

        public override async UniTask Execute(SkillNodeConfig node, SkillContext context)
        {
            var prefab = node.GetParamString("prefab", "Prefabs/Skills/Projectile");
            var hitEffect = node.GetParamString("hit_effect", "");
            var endEffect = node.GetParamString("end_effect", "");
            var fixedStartX = node.GetParamFloat("fixed_start_x", -999f);
            var fixedStartY = node.GetParamFloat("fixed_start_y", -999f);
            var offsetConfigKey = node.GetParamString("offset_config_key", "");
            var offsetListRaw = node.GetParamString("offset_list", "");
            var loopCount = node.GetParamInt("loop_count", 1);
            var interval = node.GetParamFloat("interval", 0.1f);

            // 命中检测配置
            var maxPenetrateCount = node.GetParamInt("max_penetrate", 0);
            var hitTargetDuration = node.GetParamFloat("hit_target_duration", 0f);

            // 生成位置类型：caster(施法者)、target(目标位置)、fixed(固定位置)、direction(点击/拖拽方向位置)
            var spawnType = node.GetParamString("spawn_type", "direction");

            Debug.Log($"[MultiProjectileHandler] prefab={prefab}, loopCount={loopCount}, interval={interval}s, spawnType={spawnType}");

            // 解析偏移列表（优先从配表读取）
            var offsets = new List<OffsetData>();

            if (!string.IsNullOrEmpty(offsetConfigKey))
            {
                offsets = ReadFromConfigTable(offsetConfigKey);
                Debug.Log($"[MultiProjectileHandler] 从配表 {offsetConfigKey} 读取了 {offsets.Count} 个偏移点");
            }

            if (offsets.Count == 0)
            {
                offsets = ParseOffsetList(offsetListRaw);
            }

            if (offsets.Count == 0)
            {
                Debug.LogWarning("[MultiProjectileHandler] 偏移列表为空!");
                return;
            }

            var casterPos = context.Caster?.transform.position ?? Vector3.zero;
            var targetPos = context.Target?.transform.position ?? context.TargetPosition;
            // directionPos: 拖拽释放位置，与旧版 SwiftAssaultSkill.OnCast(obj) 一致
            var directionPos = context.TargetPosition;

            // 计算缩放（支持 Reverse）
            float sizeCorrection = context.Caster != null
                ? context.Caster.GetComponent<SoldierCard>().BaseAttributes.GetFinal(AttributeType.SoldierBulletSizeCorrection)
                : 1f;
            // 与旧版 _scale = 100 + SkillRange 对应，缩放到百分比（1.0 = 原始大小）
            float globalScale = 1f;
            if (context.Caster != null)
            {
                float skillRange = context.Caster.GetComponent<SoldierCard>().BaseAttributes.GetFinal(AttributeType.SkillRange);
                globalScale = (100f + skillRange) / 100f * sizeCorrection;
            }

            // 与旧版一致：用 % offsets.Count 循环，支持配置列表长度 != 实际发射次数
            var totalCount = loopCount * offsets.Count;
            for (int i = 0; i < totalCount; i++)
            {
                var offset = offsets[i % offsets.Count];

                // 计算发射位置
                Vector3 basePos;
                switch (spawnType)
                {
                    case "caster":
                        basePos = casterPos;
                        break;
                    case "target":
                        basePos = targetPos;
                        break;
                    case "direction":
                        basePos = directionPos;
                        break;
                    case "fixed":
                        basePos = new Vector3(
                            fixedStartX > -900f ? fixedStartX : targetPos.x,
                            fixedStartY > -900f ? fixedStartY : targetPos.y,
                            targetPos.z
                        );
                        break;
                    default:
                        basePos = directionPos;
                        break;
                }

                Vector3 spawnPos = basePos + new Vector3(offset.x, offset.y, 0);

                // 计算缩放（与旧版 localScale = new Vector3(val.Reverse * _scale, _scale, _scale) 一致）
                float scaleX = offset.reverse * 100f * globalScale;
                float scaleY = 100f * globalScale;
                float scaleZ = 100f * globalScale;

                // 旋转：与旧版一致，直接用配表的 angle 作为 Z 轴旋转
                CreateProjectile(prefab, spawnPos, offset.angle,
                    scaleX, scaleY, scaleZ,
                    maxPenetrateCount, hitTargetDuration, hitEffect, endEffect,
                    context);

                // 等待间隔（最后一个不需要等待）
                if (i < totalCount - 1)
                {
                    await UniTask.Delay((int)(interval * 1000));
                }
            }
        }

        /// <summary>
        /// 从配表读取偏移列表
        /// 支持 TbGameBattleConst 中的 SkillSwiftAssaultOffset 等配置
        /// </summary>
        private List<OffsetData> ReadFromConfigTable(string configKey)
        {
            var offsets = new List<OffsetData>();
            
            try
            {
                // 根据配表键名获取数据
                // 目前支持 TbGameBattleConst.SkillSwiftAssaultOffset
                if (configKey == "SkillSwiftAssaultOffset")
                {
                    var configData = DataConfigManager.Tables.TbGameBattleConst.SkillSwiftAssaultOffset;
                    if (configData != null)
                    {
                        foreach (var item in configData)
                        {
                            offsets.Add(new OffsetData
                            {
                                x = item.X,
                                y = item.Y,
                                angle = item.Angle,
                                reverse = item.Reverse
                            });
                        }
                    }
                }
                // 未来可以添加更多配表键的支持
            }
            catch (Exception e)
            {
                Debug.LogError($"[MultiProjectileHandler] 从配表读取失败: {configKey}, 错误: {e.Message}");
            }
            
            return offsets;
        }

        /// <summary>
        /// 解析 JSON 数组格式的偏移列表
        /// 格式: [{"X":0,"Y":0,"Angle":0,"Reverse":1}, {"X":1,"Y":2,"Angle":45,"Reverse":-1}]
        /// </summary>
        private List<OffsetData> ParseOffsetList(string raw)
        {
            var offsets = new List<OffsetData>();
            if (string.IsNullOrEmpty(raw))
                return offsets;

            try
            {
                var jsonArray = JSON.Parse(raw);
                if (jsonArray == null || !jsonArray.IsArray)
                {
                    Debug.LogWarning($"[MultiProjectileHandler] offset_list 格式错误，应为 JSON 数组: {raw}");
                    return offsets;
                }

                foreach (var item in jsonArray.Children)
                {
                    offsets.Add(new OffsetData
                    {
                        x = item["X"]?.AsFloat ?? 0f,
                        y = item["Y"]?.AsFloat ?? 0f,
                        angle = item["Angle"]?.AsFloat ?? 0f,
                        reverse = item["Reverse"]?.AsFloat ?? 1f
                    });
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[MultiProjectileHandler] offset_list 解析失败: {e.Message}, raw={raw}");
            }

            return offsets;
        }

        private void CreateProjectile(string prefab, Vector3 position, float offsetAngle,
            float scaleX, float scaleY, float scaleZ,
            int maxPenetrate, float hitTargetDuration, string hitEffect, string endEffect,
            SkillContext context)
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

            var effect = EffectManager.Instance.PlayEffect(prefab, position,
                scale: new Vector3(scaleX, scaleY, scaleZ),
                rotation: Quaternion.Euler(0, 0, offsetAngle),
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
            else
            {
                Debug.LogWarning($"[MultiProjectileHandler] 预制体 {prefab} 上没有 BulletDamageDetector，跳过伤害检测");
            }

            Debug.Log($"[MultiProjectileHandler] 创建投射物 pos={position}, angle={offsetAngle}, scale=({scaleX:F1},{scaleY:F1},{scaleZ:F1})");
        }

        private class OffsetData
        {
            public float x;
            public float y;
            public float angle;
            public float reverse = 1f;
        }
    }
}
