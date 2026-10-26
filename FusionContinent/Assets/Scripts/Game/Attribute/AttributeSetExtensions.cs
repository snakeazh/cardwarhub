using System;
using System.Collections.Generic;
using Config;
using ValueType = Config.ValueType;

namespace FusionContinent.Attribute
{
    /// <summary>
    /// 兼容现有 AttributeSet 的扩展能力。
    /// 负责：元数据记录、主属性元数据、初始化、Buff/羁绊便捷接口。
    /// </summary>
    public static partial class AttributeSetExtensions
    {
        /// <summary>
        /// AttributeSet 的扩展元数据。
        /// 用于保存配置来源以及主属性运行时状态。
        /// </summary>
        private sealed class MainAttributeModifierRecord
        {
            public AttributeSourceType SourceType;
            public string SourceKey;
            public MainAttributeType MainType;
            public float Flat;
            public float Percent;
            public float EffectParam;
        }

        private sealed class AttributeSetMeta
        {
            /// <summary>角色配置 ID。</summary>
            public int ConfigId;
            /// <summary>角色等级。</summary>
            public int Level;
            /// <summary>当前角色的主属性类型。</summary>
            public MainAttributeType MainAttributeType;
            /// <summary>初始化时读取到的主属性基础值。</summary>
            public readonly Dictionary<MainAttributeType, float> BaseMainAttributeValues = new();
            /// <summary>运行时最新的主属性值。</summary>
            public readonly Dictionary<MainAttributeType, float> CurrentMainAttributeValues = new();
            /// <summary>主属性额外修正项（例如装备、Buff、羁绊带来的主属性变化）。</summary>
            public readonly List<MainAttributeModifierRecord> MainAttributeModifiers = new();
        }

        /// <summary>
        /// AttributeSet 到扩展元数据的映射表。
        /// </summary>
        private static readonly Dictionary<AttributeSet, AttributeSetMeta> _metaData = new();

        /// <summary>
        /// 获取指定 AttributeSet 的扩展元数据；若不存在则自动创建。
        /// </summary>
        private static AttributeSetMeta GetOrCreateMeta(AttributeSet attrSet)
        {
            if (!_metaData.TryGetValue(attrSet, out var meta))
            {
                meta = new AttributeSetMeta();
                _metaData[attrSet] = meta;
            }

            return meta;
        }

        /// <summary>
        /// 根据基础值和修正项，计算某个主属性类型最终应有的值。
        /// </summary>
        private static float GetResolvedMainAttributeValue(AttributeSetMeta meta, MainAttributeType mainType)
        {
            meta.BaseMainAttributeValues.TryGetValue(mainType, out var baseValue);
            float flat = 0f;
            float percent = 0f;

            foreach (var modifier in meta.MainAttributeModifiers)
            {
                if (modifier.MainType != mainType)
                {
                    continue;
                }

                flat += modifier.Flat;
                percent += modifier.Percent;
            }

            return baseValue * (1f + percent) + flat;
        }

        /// <summary>
        /// 记录 AttributeSet 的来源信息。
        /// 一般在从配置初始化时调用。
        /// </summary>
        public static void SetMetaData(this AttributeSet attrSet, int configId, int level, int star = 0, int awaken = 0)
        {
            var meta = GetOrCreateMeta(attrSet);
            meta.ConfigId = configId;
            meta.Level = level;
        }

        /// <summary>
        /// 获取 AttributeSet 对应的配置 ID。
        /// 未初始化时返回 0。
        /// </summary>
        public static int GetConfigId(this AttributeSet attrSet)
        {
            return _metaData.TryGetValue(attrSet, out var meta) ? meta.ConfigId : 0;
        }

        /// <summary>
        /// 获取 AttributeSet 对应的等级。
        /// 未初始化时返回 0。
        /// </summary>
        public static int GetLevel(this AttributeSet attrSet)
        {
            return _metaData.TryGetValue(attrSet, out var meta) ? meta.Level : 0;
        }

        /// <summary>
        /// 获取 AttributeSet 对应的星级。
        /// 未初始化时返回 0。
        /// </summary>
        public static int GetStar(this AttributeSet attrSet)
        {
            // return _metaData.TryGetValue(attrSet, out var meta) ? meta.Star : 0;
            return 0;
        }

        /// <summary>
        /// 获取 AttributeSet 对应的觉醒等级。
        /// 未初始化时返回 0。
        /// </summary>
        public static int GetAwaken(this AttributeSet attrSet)
        {
            // return _metaData.TryGetValue(attrSet, out var meta) ? meta.Awaken : 0;
            return 0;
        }

        /// <summary>
        /// 清理 AttributeSet 对应的扩展元数据。
        /// 当对象不再使用时可调用，避免缓存残留。
        /// </summary>
        public static void ClearMetaData(this AttributeSet attrSet)
        {
            _metaData.Remove(attrSet);
        }

        /// <summary>
        /// 设置当前角色的主属性类型。
        /// </summary>
        public static void SetMainAttributeType(this AttributeSet attrSet, MainAttributeType mainType)
        {
            var meta = GetOrCreateMeta(attrSet);
            meta.MainAttributeType = mainType;
        }
        
        /// <summary>
        /// 获取当前角色的暴击率。
        /// 若尚未初始化，则默认返回 1。
        /// </summary>
        public static float GetCritRate(this AttributeSet attrSet,bool isPre = true)
        {
            var baseRate = attrSet.GetAttr(AttributeType.SoldierCriticalRate).SumFinalValue();
            var maxRate = attrSet.GetAttr(AttributeType.SoldierMaxCriticalRate).SumFinalValue();
            var baseValue = attrSet.GetAttr(AttributeType.SoldierNumOfCritical).SumFinalValue();
            return Formula.GetCritRate(baseRate, maxRate, baseValue,
                DataConfigManager.Tables.TbGameBattleConst.CriticalNumBase,isPre);
        }
        
        /// <summary>
        /// 获取当前角色的暴击伤害。
        /// 若尚未初始化，则默认返回 1。
        /// </summary>
        public static float GetCritDamage(this AttributeSet attrSet)
        {
            return Formula.GetCriticalDamage(attrSet.GetCritRate(), attrSet.GetAttr(AttributeType.SoldierCriticalDamage).SumFinalValue());
        }

        /// <summary>
        /// 获取当前角色的闪避值。
        /// 若尚未初始化，则默认返回 1。
        /// </summary>
        public static float GetSoldierMissNum(this AttributeSet attrSet)
        {
            return Formula.GetMissValue(DataConfigManager.Tables.TbGameBattleConst.MissRateSumBase, attrSet.GetAttr(AttributeType.SoldierMissNum).SumFinalValue());
        }

        /// <summary>
        /// 获取攻击间隔
        /// </summary>
        /// <param name="attrSet"></param>
        /// <returns></returns>
        public static float GetSoldierAttackInterval(this AttributeSet attrSet)
        {
            float attackInterval = 1;
            if (!_metaData.TryGetValue(attrSet, out var meta))
            {
                return attackInterval;   //当没有初始化的时候 默认为1
            }

            attackInterval = attrSet.GetAttr(AttributeType.SoldierAttackInterval).SumFinalValue();
            if (GamePlayManager.Default != null && GamePlayManager.Default.UseLockAttackInterval)
            {
                attackInterval = GlobalConst.LOCK_ATTACK_INTERVAL;
            }
            return attackInterval;
        }

        /// <summary>
        /// 获取当前角色攻击速度（次/秒，供 1/speed 得到攻击间隔）。
        /// 存在 <see cref="AttributeType.LockAtkSpeed"/> 时，直接返回配表 BaseValue（如 0.5），不受攻速值影响。
        /// </summary>
        public static float GetAttackTime(this AttributeSet attrSet)
        {
            if (attrSet.GetAttr(Config.AttributeType.LockNormalAttack).SumFinalValue() != 0)
            {
                return attrSet.GetAttr(Config.AttributeType.LockNormalAttack).SumFinalValue();
            }

            // 锁定攻击速度：读取 Module 属性「锁定攻击速度」配置数值（如 自身:无:自身:0.5:0 → 0.5）
            if (GamePlayManager.Default != null)
            {
                var lockedAtkSpeed = GamePlayManager.Default.ModuleAttributes
                    .GetAttr(AttributeType.LockAtkSpeed)
                    .SumFinalValue();
                if (lockedAtkSpeed > 0f)
                {
                    return lockedAtkSpeed;
                }
            }

            var nextAttackTime = Formula.AttackInterval(attrSet.GetAttr(Config.AttributeType.SoldierNumOfAttackSpeed).SumFinalValue(),  DataConfigManager.Tables.TbGameBattleConst.AttackSpeedBase,
                DataConfigManager.Tables.TbGameBattleConst.AfkTimeLimitMax);

            return nextAttackTime/GetSoldierAttackInterval(attrSet);
        }

        /// <summary>
        /// 获取当前角色的主属性类型。
        /// 若尚未初始化，则默认返回 Strength。
        /// </summary>
        public static MainAttributeType GetMainAttributeType(this AttributeSet attrSet)
        {
            return _metaData.TryGetValue(attrSet, out var meta) ? meta.MainAttributeType : MainAttributeType.Strength;
        }
        

        /// <summary>
        /// 获取当前主属性类型对应的主属性值。
        /// </summary>
        public static float GetMainAttributeValue(this AttributeSet attrSet)
        {
            return attrSet.GetMainAttributeValue(attrSet.GetMainAttributeType());
        }

        /// <summary>
        /// 获取指定主属性类型的当前值。
        /// 若没有记录则返回 0。
        /// </summary>
        public static float GetMainAttributeValue(this AttributeSet attrSet, MainAttributeType mainType)
        {
            if (_metaData.TryGetValue(attrSet, out var meta) && meta.CurrentMainAttributeValues.TryGetValue(mainType, out var value))
            {
                return value;
            }

            return 0f;
        }

        /// <summary>
        /// 获取当前记录的全部主属性值。
        /// 返回副本，避免外部直接修改内部缓存。
        /// </summary>
        public static Dictionary<MainAttributeType, float> GetAllMainAttributeValues(this AttributeSet attrSet)
        {
            if (!_metaData.TryGetValue(attrSet, out var meta))
            {
                return new Dictionary<MainAttributeType, float>();
            }

            return new Dictionary<MainAttributeType, float>(meta.CurrentMainAttributeValues);
        }

        /// <summary>
        /// 更新当前主属性类型的主属性值。
        /// </summary>
        public static void UpdateMainAttributeValue(this AttributeSet attrSet, float newValue)
        {
            attrSet.UpdateMainAttributeValue(attrSet.GetMainAttributeType(), newValue);
        }

        /// <summary>
        /// 更新指定主属性类型的值，并把变化量同步转换到基础属性。
        /// </summary>
        /// <param name="attrSet">目标属性集。</param>
        /// <param name="mainType">要更新的主属性类型。</param>
        /// <param name="newValue">目标值；当 isadd=true 时表示增量值。</param>
        /// <param name="isadd">是否按增量叠加更新。</param>
        public static void UpdateMainAttributeValue(this AttributeSet attrSet, MainAttributeType mainType, float newValue, bool isadd = false)
        {
            var meta = GetOrCreateMeta(attrSet);
            meta.CurrentMainAttributeValues.TryGetValue(mainType, out var oldValue);
            if (isadd)
                newValue = oldValue + newValue;
            if (Math.Abs(newValue - oldValue) < 0.001f)
            {
                return;
            }

            float delta = newValue - oldValue;
            MainAttributeConfigHelper.ApplyMainAttributeDelta(attrSet, meta.MainAttributeType, mainType, delta);
            meta.CurrentMainAttributeValues[mainType] = newValue;
        }

        /// <summary>
        /// 重新按当前主属性值重建主属性带来的基础属性加成。
        /// 适用于主属性缓存与实际属性需要重新同步的场景。
        /// </summary>
        public static void RecalculateMainAttributeEffects(this AttributeSet attrSet)
        {
            var meta = GetOrCreateMeta(attrSet);

            foreach (var mainPair in meta.CurrentMainAttributeValues)
            {
                var cfg = MainAttributeConfigHelper.GetConfig(mainPair.Key);
                if (cfg?.ConvertToBaseAttributes == null)
                {
                    continue;
                }

                for (int i = 0; i < cfg.ConvertToBaseAttributes.Count; i++)
                {
                    foreach (var attrPair in attrSet.GetAll())
                    {
                        attrPair.Value.RemoveModifier(AttributeSourceType.MainAttribute, $"main_{(int)mainPair.Key}_{i}");
                    }
                }
            }

            foreach (var mainType in new List<MainAttributeType>(meta.CurrentMainAttributeValues.Keys))
            {
                var resolvedValue = GetResolvedMainAttributeValue(meta, mainType);
                meta.CurrentMainAttributeValues[mainType] = meta.BaseMainAttributeValues.TryGetValue(mainType, out var baseValue) ? baseValue : 0f;
                attrSet.UpdateMainAttributeValue(mainType, resolvedValue);
            }
        }

        /// <summary>
        /// 使用配置初始化属性集。
        /// 主属性类型由配置自动决定。
        /// </summary>
        public static void InitFromConfig(this AttributeSet attrSet, int configId, int level)
        {
            attrSet.InitFromConfig(configId, level, null);
        }

        /// <summary>
        /// 使用配置初始化属性集，并建立主属性运行时缓存。
        /// </summary>
        /// <param name="attrSet">目标属性集。</param>
        /// <param name="configId">角色配置 ID。</param>
        /// <param name="level">等级。</param>
        /// <param name="mainType">可选主属性类型；不传时自动从配置读取。</param>
        public static void InitFromConfig(this AttributeSet attrSet, int configId, int level, MainAttributeType? mainType)
        {
            attrSet.SetMetaData(configId, level);

            var resolvedMainType = MainAttributeConfigHelper.ResolveMainAttributeType(configId, mainType);
            attrSet.SetMainAttributeType(resolvedMainType);

            var baseAttrs = StaticAttributeCalculator.CalcBaseAttributes(configId, level);
            foreach (var kv in baseAttrs)
            {
                attrSet.GetAttr(kv.Key).SetBaseValue(kv.Value);
            }

            var meta = GetOrCreateMeta(attrSet);
            meta.BaseMainAttributeValues.Clear();
            meta.CurrentMainAttributeValues.Clear();
            meta.MainAttributeModifiers.Clear();

            var mainAttrs = MainAttributeConfigHelper.GetMainAttributeValues(configId, level);
            foreach (var kv in mainAttrs)
            {
                meta.BaseMainAttributeValues[kv.Key] = kv.Value;
                meta.CurrentMainAttributeValues[kv.Key] = kv.Value;
            }
        }

        /// <summary>
        /// 应用 equip 属性加成。
        /// 当前方法保留为扩展入口，具体逻辑暂未启用。
        /// </summary>
        public static void ApplyEquip(this AttributeSet attrSet, string equipID, Dictionary<AttributeType, (float flat, float percent)> effects)
        {
            foreach (var kv in effects)
            {
                var mod = new AttributeModifier
                {
                    SourceType = AttributeSourceType.Equip,
                    SourceKey = equipID,
                    FlatAdd = kv.Value.flat,
                    PercentAdd = kv.Value.percent
                };
                attrSet.GetAttr(kv.Key).AddModifier(mod);
            }
        }

        /// <summary>
        /// 使用 AttributeStruct 列表应用 equip 属性加成。
        /// 每条配置直接转换为 AttributeModifier，可保留 EffectParam 与 ProbabilityTrigger。
        /// </summary>
        public static void ApplyEquip(this AttributeSet attrSet, string equipID, List<AttributeStruct> effects)
        {
            foreach (var effect in effects)
            {
                var mod = new AttributeModifier
                {
                    SourceType = AttributeSourceType.Equip,
                    SourceKey = equipID,
                    FlatAdd = effect.ValueType == ValueType.Number ? effect.Value : 0f,
                    PercentAdd = effect.ValueType == ValueType.Percentage ? effect.Value : 0f,
                    EffectParam = effect.EffectParam,
                    ProbabilityTrigger = effect.ProbabilityTrigger
                };

                attrSet.GetAttr(effect.Type).AddModifier(mod, true);
            }
        }
        
        
        /// <summary>
        /// 应用 equip 属性加成。
        /// 当前方法保留为扩展入口，具体逻辑暂未启用。
        /// </summary>
        public static void ApplyAttributeSource(this AttributeSet attrSet, AttributeSourceType attributeSourceType, string equipID, Dictionary<AttributeType, (float flat, float percent)> effects)
        {
            foreach (var kv in effects)
            {
                var mod = new AttributeModifier
                {
                    SourceType = attributeSourceType,
                    SourceKey = equipID,
                    FlatAdd = kv.Value.flat,
                    PercentAdd = kv.Value.percent
                };
                attrSet.GetAttr(kv.Key).AddModifier(mod);
            }
        }
        
        
        public static void ApplyAttributeSource(this AttributeSet attrSet, AttributeSourceType attributeSourceType, string equipID, List<AttributeStruct> attributeStructs)
        {
            foreach (var kv in attributeStructs)
            {
                var mod = new AttributeModifier
                {
                    SourceType = attributeSourceType,
                    SourceKey = equipID,
                    FlatAdd = kv.ValueType == Config.ValueType.Number ? kv.Value : 0,
                    PercentAdd = kv.ValueType == Config.ValueType.Percentage ? kv.Value : 0
                };
                attrSet.GetAttr(kv.Type).AddModifier(mod);
            }
        }
        /// <summary>
        /// 移除 equip 属性加成。
        /// 当前方法保留为扩展入口，具体逻辑暂未启用。
        /// </summary>
        public static void RemoveAttributeSource(this AttributeSet attrSet,AttributeSourceType attributeSourceType, string equipID)
        {
            foreach (var kv in attrSet.GetAll())
            {
                kv.Value.RemoveModifier(attributeSourceType, equipID);
            }
        }
        /// <summary>
        /// 移除 equip 属性加成。
        /// 当前方法保留为扩展入口，具体逻辑暂未启用。
        /// </summary>
        public static void RemoveEquip(this AttributeSet attrSet, string equipID, Dictionary<AttributeType, (float flat, float percent)> effects)
        {
            foreach (var kv in attrSet.GetAll())
            {
                kv.Value.RemoveModifier(AttributeSourceType.Equip, equipID);
            }
        }

        /// <summary>
        /// 以类似装备的方式应用主属性修正。
        /// 支持固定值与百分比两种主属性增减。
        /// </summary>
        public static void ApplyMainAttribute(this AttributeSet attrSet, string sourceKey, Dictionary<MainAttributeType, (float flat, float percent)> effects, AttributeSourceType sourceType = AttributeSourceType.Equip)
        {
            var meta = GetOrCreateMeta(attrSet);
            foreach (var kv in effects)
            {
                meta.MainAttributeModifiers.RemoveAll(x => x.SourceType == sourceType && x.SourceKey == sourceKey && x.MainType == kv.Key);
                meta.MainAttributeModifiers.Add(new MainAttributeModifierRecord
                {
                    SourceType = sourceType,
                    SourceKey = sourceKey,
                    MainType = kv.Key,
                    Flat = kv.Value.flat,
                    Percent = kv.Value.percent,
                    EffectParam = 0f
                });

                var resolvedValue = GetResolvedMainAttributeValue(meta, kv.Key);
                attrSet.UpdateMainAttributeValue(kv.Key, resolvedValue);
            }
        }

        /// <summary>
        /// 使用 MainAttributeStruct 列表应用主属性修正。
        /// Number 视为固定值，Percentage 视为百分比值。
        /// </summary>
        public static void ApplyMainAttribute(this AttributeSet attrSet, string sourceKey, List<MainAttributeStruct> effects, AttributeSourceType sourceType = AttributeSourceType.Equip)
        {
            var meta = GetOrCreateMeta(attrSet);
            var changedTypes = new List<MainAttributeType>();

            foreach (var effect in effects)
            {
                meta.MainAttributeModifiers.Add(new MainAttributeModifierRecord
                {
                    SourceType = sourceType,
                    SourceKey = sourceKey,
                    MainType = effect.Type,
                    Flat = effect.ValueType == ValueType.Number ? effect.Value : 0f,
                    Percent = effect.ValueType == ValueType.Percentage ? effect.Value : 0f,
                    EffectParam = effect.EffectParam
                });

                if (!changedTypes.Contains(effect.Type))
                {
                    changedTypes.Add(effect.Type);
                }
            }

            foreach (var mainType in changedTypes)
            {
                var resolvedValue = GetResolvedMainAttributeValue(meta, mainType);
                attrSet.UpdateMainAttributeValue(mainType, resolvedValue);
            }
        }

        /// <summary>
        /// 移除某个来源施加的主属性修正。
        /// </summary>
        public static void RemoveMainAttribute(this AttributeSet attrSet, string sourceKey, AttributeSourceType sourceType = AttributeSourceType.Equip)
        {
            var meta = GetOrCreateMeta(attrSet);
            var changedTypes = new List<MainAttributeType>();

            foreach (var modifier in new List<MainAttributeModifierRecord>(meta.MainAttributeModifiers))
            {
                if (modifier.SourceType != sourceType || modifier.SourceKey != sourceKey)
                {
                    continue;
                }

                meta.MainAttributeModifiers.Remove(modifier);
                if (!changedTypes.Contains(modifier.MainType))
                {
                    changedTypes.Add(modifier.MainType);
                }
            }

            foreach (var mainType in changedTypes)
            {
                var resolvedValue = GetResolvedMainAttributeValue(meta, mainType);
                attrSet.UpdateMainAttributeValue(mainType, resolvedValue);
            }
        }
        
        
        /// <summary>
        /// 应用 Buff 属性加成。
        /// 当前方法保留为扩展入口，具体逻辑暂未启用。
        /// </summary>
        public static void ApplyBuff(this AttributeSet attrSet, string buffId, Dictionary<AttributeType, (float flat, float percent)> effects)
        {
            attrSet.ApplyBuff(buffId, effects, AttributeSourceType.Skill);
        }

        public static void ApplyBuff(this AttributeSet attrSet, string sourceKey, Dictionary<AttributeType, (float flat, float percent)> effects, AttributeSourceType sourceType)
        {
            foreach (var kv in effects)
            {
                var mod = new AttributeModifier
                {
                    SourceType = sourceType,
                    SourceKey = sourceKey,
                    FlatAdd = kv.Value.flat,
                    PercentAdd = kv.Value.percent
                };
                attrSet.GetAttr(kv.Key).AddModifier(mod,true);
            }
        }

        /// <summary>
        /// 移除指定 Buff 带来的属性加成。
        /// 当前方法保留为扩展入口，具体逻辑暂未启用。
        /// </summary>
        public static void RemoveBuff(this AttributeSet attrSet, string buffId)
        {
            attrSet.RemoveBuff(buffId, AttributeSourceType.Skill);
        }

        public static void RemoveBuff(this AttributeSet attrSet, string sourceKey, AttributeSourceType sourceType)
        {
            foreach (var kv in attrSet.GetAll())
            {
                kv.Value.RemoveModifier(sourceType, sourceKey);
            }
        }

        /// <summary>
        /// 应用羁绊属性加成。
        /// 当前方法保留为扩展入口，具体逻辑暂未启用。
        /// </summary>
        public static void ApplyBond(this AttributeSet attrSet, string bondId, Dictionary<AttributeType, (float flat, float percent)> effects)
        {
            // foreach (var kv in effects)
            // {
            //     var mod = new AttributeModifier
            //     {
            //         SourceType = AttributeSourceType.Bond,
            //         SourceKey = bondId,
            //         FlatAdd = kv.Value.flat,
            //         PercentAdd = kv.Value.percent
            //     };
            //     attrSet.GetAttr(kv.Key).AddModifier(mod);
            // }
        }

        /// <summary>
        /// 移除指定羁绊带来的属性加成。
        /// 当前方法保留为扩展入口，具体逻辑暂未启用。
        /// </summary>
        public static void RemoveBond(this AttributeSet attrSet, string bondId)
        {
            // foreach (var kv in attrSet.GetAll())
            // {
            //     kv.Value.RemoveModifier(AttributeSourceType.Bond, bondId);
            // }
        }
    }
}
