using System;
using System.Collections.Generic;
using Config;
using FusionContinent.Attribute;
using FusionContinent.Buff;
using UnityEngine;
using SkillContext = FusionContinent.Skill.Handler.SkillContext;
using ValueType = Config.ValueType;

namespace FusionContinent.Buff
{
    /// <summary>
    /// Buff 工厂
    /// 使用注册制，根据目标类型（Soldier/Monster）创建不同实现
    /// </summary>
    public static class BuffFactory
    {
        /// <summary>
        /// Soldier Buff 创建委托
        /// </summary>
        public delegate IBuff SoldierBuffCreator(float duration, float value, ValueType valueType, object caster, float tickInterval, int maxStack);

        /// <summary>
        /// Monster Buff 创建委托
        /// </summary>
        public delegate IBuff MonsterBuffCreator(float duration, float value, ValueType valueType, float tickInterval, int maxStack);

        private static readonly Dictionary<BuffType, SoldierBuffCreator> _soldierCreators = new Dictionary<BuffType, SoldierBuffCreator>();
        private static readonly Dictionary<BuffType, MonsterBuffCreator> _monsterCreators = new Dictionary<BuffType, MonsterBuffCreator>();

        static BuffFactory()
        {
            // === Soldier Buff 注册 ===
            _soldierCreators[BuffType.Health] = (d, v, vt, c, ti, ms) =>
            {
                var buff = new HealBuff(d, v, vt, c);
                buff.TickInterval = ti;
                return buff;
            };
            _soldierCreators[BuffType.Vertigo] = (d, v, vt, c, ti, ms) => new StunBuff(d);
            _soldierCreators[BuffType.Burn] = (d, v, vt, c, ti, ms) => new SoldierBurnBuff(d, v, vt, tickInterval: ti, maxStack: ms);
            _soldierCreators[BuffType.RequiemChant] = (d, v, vt, c, ti, ms) => new RequiemChantBuff();
            _soldierCreators[BuffType.EnergyPouring] = (d, v, vt, c, ti, ms) => new EnergyPouringBuff(BuffType.EnergyPouring , d, v, vt,AttributeType.SoldierAttackPower, ModifierMode.Additive );
            // _soldierCreators[BuffType.Attrbute] = (d, v, vt, c, ti) => new AttrbuteBuff(d, v, vt);
            // === Monster Buff 注册 ===
            _monsterCreators[BuffType.Burn] = (d, v, vt, ti, ms) => new BurnAbnormalBuff(d, v, vt, ti, ms);
            _monsterCreators[BuffType.Vertigo] = (d, v, vt, ti, ms) => new VertigoAbnormalBuff(d);
            _monsterCreators[BuffType.Vulnerable] = (d, v, vt, ti, ms) => new VulnerableAbnormalBuff(d, v, vt);
            _monsterCreators[BuffType.Moderate] = (d, v, vt, ti, ms) => new SlowAbnormalBuff(d, v, vt);
            _monsterCreators[BuffType.Invincible] = (d, v, vt, ti, ms) => new InvincibleAbnormalBuff(d);
            _monsterCreators[BuffType.Accelerate] = (d, v, vt, ti, ms) => new AccelerateAbnormalBuff(d, v, vt);
            _monsterCreators[BuffType.Disarm] = (d, v, vt, ti, ms) => new DisarmAbnormalBuff(d);
            _monsterCreators[BuffType.KnockBack] = (d, v, vt, ti, ms) => new KnockBackAbnormalBuff(d, v, vt);
            _monsterCreators[BuffType.Health] = (d, v, vt, ti, ms) =>
            {
                var buff = new HealBuff(d, v, vt);
                buff.TickInterval = ti;
                return buff;
            };
            // _monsterCreators[BuffType.Attrbute] = (d, v, vt, ti) => new AttrbuteBuff(d, v, vt);
        }

        /// <summary>
        /// 注册 Soldier Buff
        /// </summary>
        public static void RegisterSoldier(BuffType type, SoldierBuffCreator creator)
            => _soldierCreators[type] = creator;

        /// <summary>
        /// 注册 Monster Buff
        /// </summary>
        public static void RegisterMonster(BuffType type, MonsterBuffCreator creator)
            => _monsterCreators[type] = creator;

        /// <summary>
        /// 移除已注册的 Soldier Buff
        /// </summary>
        public static void UnregisterSoldier(BuffType type)
            => _soldierCreators.Remove(type);

        /// <summary>
        /// 移除已注册的 Monster Buff
        /// </summary>
        public static void UnregisterMonster(BuffType type)
            => _monsterCreators.Remove(type);

        /// <summary>
        /// 创建月影护盾 Buff（支持护盾特效与破裂特效）
        /// </summary>
        public static IBuff CreateMoonlightShield(float duration, int shieldNum, float spellPowerBonus,
            string shieldVfx, string breakVfx, object caster = null, object target = null,
            SkillContext context = null)
        {
            var buff = new MoonlightShieldBuff();
            buff.Init(duration, shieldNum, spellPowerBonus, shieldVfx, breakVfx);
            buff.BuffId = (int)BuffType.MoonlightShield;
            buff.Caster = caster;
            buff.SkillContext = context;
            buff.BuffVfx = shieldVfx;
            return buff;
        }

        /// <summary>
        /// 创建慰灵圣咏 Buff（三属性加成 + 法阵特效）
        /// </summary>
        public static IBuff CreateRequiemChant(float duration, float inheritanceValue,
            string circleVfx, object caster = null, object target = null,
            SkillContext context = null)
        {
            var buff = new RequiemChantBuff();
            buff.Init(duration, inheritanceValue, circleVfx);
            buff.BuffId = (int)BuffType.RequiemChant;
            buff.Caster = caster;
            buff.SkillContext = context;
            buff.BuffVfx = circleVfx;
            return buff;
        }

        /// <summary>
        /// 创建 Buff，自动根据目标类型选择实现
        /// </summary>
        public static IBuff Create(BuffType type, float duration = -1f, float value = 0f, float tickInterval = 0f,
            object caster = null, object target = null, SkillContext context = null, int maxStack = 1)
        {
            if (type == BuffType.Attrbute)
            {
                LogTool.LogError("[BuffFactory] Attrbute 类型须通过 CreateAttribute 创建，不能走 Create()");
                return null;
            }

            if (target is MonsterCard)
            {
                if (_monsterCreators.TryGetValue(type, out var creator))
                {
                    var buff = creator(duration, value, ValueType.Number, tickInterval, maxStack);
                    buff.BuffId = (int)type;
                    buff.Caster = caster;
                    buff.SkillContext = context;
                    return buff;
                }
                LogTool.E($"[BuffFactory] Monster BuffType 未注册: {type}");
                return null;
            }

            if (_soldierCreators.TryGetValue(type, out var soldierCreator))
            {
                var buff = soldierCreator(duration, value, ValueType.Number, caster, tickInterval, maxStack);
                buff.BuffId = (int)type;
                buff.SkillContext = context;
                return buff;
            }
            LogTool.E($"[BuffFactory] BuffType 未注册: {type}");
            return null;
        }

        /// <summary>
        /// 创建属性修正 Buff（支持额外参数）
        /// </summary>
        public static IBuff CreateAttribute(BuffType type, AttributeType attrType, float duration,
            float effectValue, ValueType valueType, float cooldownTime = 0f, int skillId = 0,
            float tickInterval = 0f,
            object caster = null, object target = null, SkillContext context = null,
            AttributeSourceType sourceType = AttributeSourceType.Skill, string sourceKey = null,
            int maxStack = 1)
        {
            var resolvedSourceKey = !string.IsNullOrEmpty(sourceKey)
                ? sourceKey
                : (skillId > 0 ? skillId.ToString() : type.ToString());

            var buff = new AttributeModifierBuff(type, attrType, duration, effectValue, valueType,
                cooldownTime, skillId, sourceType, resolvedSourceKey, maxStack)
            {
                Caster = caster,
                SkillContext = context,
                TickInterval = tickInterval
            };

            return buff;
        }

        /// <summary>
        /// 创建聚合 Buff（支持多效果）
        /// 用于同一技能释放的多个 Buff（如战神祝福 = [攻击+10%, 血量+20%]）
        /// </summary>
        public static IBuff CreateMultiEffect(int buffId, BuffType buffType, float duration, int maxStack,
            List<BuffEffectData> effects, int skillId = 0, float tickInterval = 0f,
            object caster = null, object target = null, SkillContext context = null,
            AttributeSourceType sourceType = AttributeSourceType.Skill, string sourceKey = null)
        {
            // 生成 SourceKey：BuffId_SkillId_CasterHash
            var resolvedSourceKey = !string.IsNullOrEmpty(sourceKey)
                ? sourceKey
                : $"{buffId}_{skillId}_{caster?.GetHashCode() ?? 0}";

            var buff = new MultiEffectBuff(buffType, buffId, duration, maxStack, effects,
                skillId, caster, tickInterval, context, sourceType, resolvedSourceKey);

            return buff;
        }

        /// <summary>
        /// 创建触发型 Buff
        /// 监听指定事件，触发时为拥有者添加另一个 Buff
        /// 使用 BuffEffectsConfig 的 TakeType 作为触发类型
        /// </summary>
        public static IBuff CreateTrigger(BuffEffectsConfig config, float duration = -1f,
            object caster = null, object target = null, SkillContext context = null)
        {
            if (config == null)
            {
                LogTool.E("[BuffFactory] CreateTrigger: config is null");
                return null;
            }

            var buff = new TriggerBuff();
            buff.Init(config, duration);
            buff.BuffId = config.ID;
            buff.Caster = caster;
            buff.SkillContext = context;
            return buff;
        }

        public static bool IsRegistered(BuffType type)
            => _soldierCreators.ContainsKey(type) || _monsterCreators.ContainsKey(type);
    }
}
