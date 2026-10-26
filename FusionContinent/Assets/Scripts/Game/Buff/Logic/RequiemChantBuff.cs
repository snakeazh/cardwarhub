using System.Collections.Generic;
using Config;
using FusionContinent.Attribute;
using Manager;
using QFramework;
using UnityEngine;
using ValueType = Config.ValueType;

namespace FusionContinent.Buff
{
    /// <summary>
    /// 慰灵圣咏 Buff
    /// Buff 持续期间：攻击 + 攻速 + 法强 各获得 (SkillAttackInheritance) 百分比加成
    /// OnAdd → 添加三属性加成 & 脚下法阵特效
    /// OnRemove → 移除属性加成 & 归还法阵
    /// </summary>
    public class RequiemChantBuff : BuffBase
    {
        /// <summary>法阵特效预制体名</summary>
        public string CircleVfx { get; private set; }

        /// <summary>法阵ObjectPool引用</summary>
        private GameObject _circleObj;
        private GenericObjectPool _circlePool;

        /// <summary>三属性修饰器的唯一Key</summary>
        private readonly string _atkModifierKey = $"RequiemChant_atk_{System.Guid.NewGuid():N}";
        private readonly string _spdModifierKey = $"RequiemChant_spd_{System.Guid.NewGuid():N}";
        private readonly string _intModifierKey = $"RequiemChant_int_{System.Guid.NewGuid():N}";

        /// <summary>属性加成百分比（从 SkillContext 读取）</summary>
        private float _inheritanceValue;

        public RequiemChantBuff()
            : base()
        {
            Type = BuffType.RequiemChant;
            DurationType = BuffDurationType.Duration;
        }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="duration">持续时间（秒）</param>
        /// <param name="inheritanceValue">继承属性百分比</param>
        /// <param name="circleVfx">脚下法阵VFX池名</param>
        public void Init(float duration, float inheritanceValue, string circleVfx = "EliaSkillSoulComfort03")
        {
            Duration = duration;
            _inheritanceValue = inheritanceValue;
            CircleVfx = circleVfx;
        }

        public override void OnAdd(object owner)
        {
            if (owner is not SoldierCard soldier) return;

            // 读取 SkillContext 中的继承值（若没有则降级为0，即无加成）
            if (SkillContext?.Config != null && _inheritanceValue <= 0)
            {
                var level = Mathf.Max(1, SkillContext.Level);
                var lvCfg = DataConfigManager.Tables.TbSpecialEffectsConfig[SkillContext.SkillId].LevelPropertyList[level - 1];
                foreach (var attr in lvCfg.Attributes)
                {
                    if (attr.Type == AttributeType.SkillAttackInheritance)
                    {
                        _inheritanceValue = attr.Value;
                        break;
                    }
                }
            }

            ApplyAttributes(soldier);
            SpawnCircleVfx(soldier);
        }

        public override void OnRemove(object owner)
        {
            if (owner is not SoldierCard soldier) return;

            RemoveAttributes(soldier);
            ReturnCircleVfx();
        }

        public override void OnTick(object owner, float dt)
        {
            base.OnTick(owner, dt);
        }

        private void ApplyAttributes(SoldierCard soldier)
        {
            if (_inheritanceValue <= 0) return;

            var modifiers = new[]
            {
                (AttributeType.SoldierAttackPower, _atkModifierKey),
                (AttributeType.SoldierAttackSpeed, _spdModifierKey),
                (AttributeType.SoldierSpellPower, _intModifierKey),
            };

            foreach (var (attrType, key) in modifiers)
            {
                var modifier = new AttributeModifier
                {
                    SourceKey = key,
                    SourceType = AttributeSourceType.Skill,
                    PercentAdd = _inheritanceValue,
                };

                AttributeManager.SetAttributeLogic(modifier, new AttributeData
                {
                    Type = attrType,
                    Value = _inheritanceValue,
                    ValueType = ValueType.Percentage,
                }, false, 1, soldier.cardId);
            }
        }

        private void RemoveAttributes(SoldierCard soldier)
        {
            var keys = new[] { _atkModifierKey, _spdModifierKey, _intModifierKey };
            var types = new[]
            {
                AttributeType.SoldierAttackPower,
                AttributeType.SoldierAttackSpeed,
                AttributeType.SoldierSpellPower,
            };

            for (int i = 0; i < types.Length; i++)
            {
                AttributeManager.SetAttributeLogic(new AttributeModifier
                {
                    SourceKey = keys[i],
                    SourceType = AttributeSourceType.Skill,
                }, new AttributeData
                {
                    Type = types[i],
                    Value = 0,
                    ValueType = ValueType.Percentage,
                }, false, 0, soldier.cardId);
            }
        }

        private void SpawnCircleVfx(SoldierCard soldier)
        {
            if (string.IsNullOrEmpty(CircleVfx)) return;

            var pos = soldier.transform.position;
            (_circlePool, _circleObj) = ObjectPoolManager.Instance.PlayGameObjectPool(new GameObjectPoolData()
            {
                PoolName = CircleVfx,
                ParentTran = soldier.transform,
                Position = new Vector3(pos.x, pos.y - 0.8f, pos.z),
                LocalScale = 160,
                DelayReturn = Duration
            });
        }

        private void ReturnCircleVfx()
        {
            if (_circlePool == null || _circleObj == null) return;
            _circlePool.Return(_circleObj);
            _circleObj = null;
            _circlePool = null;
        }
    }
}
