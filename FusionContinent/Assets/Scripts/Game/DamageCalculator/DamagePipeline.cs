using System.Collections.Generic;
using Game.Battle;

namespace FusionContinent
{
    /// <summary>
    /// 伤害计算管线，按固定顺序执行一系列 IDamageModifier 节点。
    /// 
    /// 设计思路：
    /// - 所有节点共享同一个 DamageContext，通过读写 ctx 字段传递数据
    /// - 顺序由构造时注册顺序决定，节点之间无返回值
    /// - 任意节点将 ctx.IsMiss 设为 true 时，pipeline 立即中断并返回 0（后续节点不执行）
    /// - 最终伤害从 ctx.FinalDamage 读取
    /// 
    /// 不同 PipelineType 的节点顺序：
    /// 
    /// 【MonsterDamage】怪物攻击人物：
    ///   Attributes → Dodge → TakeDamage → Resistance → Crit → CritFinal → Fragile
    ///   - 无暴击（CritModifier 跳过）
    ///   - 无combo增伤
    ///   - 无破盾增伤
    ///   - 无斩杀判定
    /// 
/// 【SoldierDamage】人物攻击怪物：
///   Attributes → CareerDamage → TakeDamage → Strength → Resistance → Crit → CritFinal → Fragile → Combo → BrokenState → SoulFlaws → Execution
    ///   - 完整的伤害计算流程
    /// 
    /// 【SceneSkillDamage】场景技能伤害：
    ///   Attributes → TakeDamage → Resistance → Crit → CritFinal → BrokenState
    ///   - 无闪避、无combo、无斩杀
    ///   - 有暴击和抗性计算
    /// </summary>
    /// 
    public class DamagePipeline
    {
        private readonly List<IDamageModifier> _mods;
        public DamagePipelineType PipelineType { get; }

        /// <summary>
        /// 构造函数 - 创建指定类型的伤害计算管线
        /// </summary>
        public DamagePipeline(DamagePipelineType type)
        {
            PipelineType = type;
            _mods = BuildPipeline(type);
        }

        /// <summary>
        /// 根据类型构建对应的 Modifier 列表
        /// </summary>
        private List<IDamageModifier> BuildPipeline(DamagePipelineType type)
        {
            return type switch
            {
                DamagePipelineType.MonsterDamage => BuildMonsterDamagePipeline(),
                DamagePipelineType.SoldierDamage => BuildSoldierDamagePipeline(),
                DamagePipelineType.SceneSkillDamage => BuildSceneSkillDamagePipeline(),
                DamagePipelineType.BuffSkillDamage => BuildBuffSkillDamagePipeline(),
                _ => BuildSoldierDamagePipeline()
            };
        }

        /// <summary>
        /// 构建怪物攻击人物的伤害计算管线
        /// 特点：
        /// - 无暴击判定（怪物攻击不暴击）
        /// - 无 Strength（力量分摊）
        /// - 无 Combo（怪物不触发连击）
        /// - 无斩杀判定
        /// - 无 SoulFlaws 破盾增伤
        /// - 保留：属性加成、闪避、受击加减成、抗性、易伤、虚弱/精英/首领修正
        /// </summary>
        private List<IDamageModifier> BuildMonsterDamagePipeline()
        {
            return new List<IDamageModifier>
            {
                new AttributesModifier(),
                new DodgeModifier(),       // 1. 闪避判定（miss 时中断 pipeline）
                new TakeDamageModifier(),  // 2. 受到伤害加减成（属性：SoldierTakeDamage）
                new ResistanceModifier(), // 3. 抗性 / 护甲减伤（按 DamageType 选取物理/魔法抗性）
                new CritFinalModifier(),   // 4. 暴击伤害应用（注意：MonsterDamage 的 CritRate 固定为0，所以不会触发）
                new FragileModifier(),     // 5. 易伤增伤（属性：SoldierFragileDamage）
                new ComboModifier(),            // 7. combo增伤（属性：ComboDamageRatio）
                new BrokenStateDamageModifier(), // 6. 虚弱/精英/首领伤害修正
                new ExecutionThresholdModifier()// 10. 斩杀判定
            };
        }

        /// <summary>
        /// 构建人物攻击怪物的伤害计算管线
        /// 特点：
        /// - 完整的伤害计算流程
        /// - 有暴击、有 Strength 分摊、有 Combo、有斩杀、有破盾增伤
        /// </summary>
        private List<IDamageModifier> BuildSoldierDamagePipeline()
        {
            return new List<IDamageModifier>
            {
                new BattleRuleDamageModifier(),
                new EveryDamageModifier(),
                new AttributesModifier(),
                new CareerDamageModifier(),
                new TakeDamageModifier(),      // 2. 受到伤害加减成（属性：SoldierTakeDamage）
                new StrengthModifier(),         // 3. 力量型角色特殊处理（RawDamage 分摊）
                new ResistanceModifier(),       // 4. 抗性 / 护甲减伤（按 DamageType 选取物理/魔法抗性）
                new CritFinalModifier(),        // 5. 暴击伤害应用（ctx.IsCrit 时 FinalDamage × CritMultiplier）
                new FragileModifier(),          // 6. 易伤增伤（属性：SoldierFragileDamage）
                new SoulFlawsBreakModifier(),  // 9. 破盾的增伤
            };
        }

        /// <summary>
        /// 构建场景技能伤害的计算管线
        /// 特点：
        /// - 无闪避（场景技能必中）
        /// - 无 Strength（力量分摊不适用于场景技能）
        /// - 无 Combo（场景技能不触发连击）
        /// - 无斩杀判定
        /// - 无破盾增伤
        /// - 保留：属性加成、受击加减成、抗性、暴击、易伤、虚弱/精英/首领修正
        /// </summary>
        private List<IDamageModifier> BuildSceneSkillDamagePipeline()
        {
            return new List<IDamageModifier>
            {
                new EveryDamageModifier(),
                new ResistanceModifier(),       // 2. 抗性 / 护甲减伤
                // new FragileModifier(),          // 4. 易伤增伤
            };
        }
        
        /// <summary>
        /// 构建buff技能伤害的计算管线
        /// 特点：
        /// - 无闪避（场景技能必中）
        /// - 无 Strength（力量分摊不适用于场景技能）
        /// - 无 Combo（场景技能不触发连击）
        /// - 无斩杀判定
        /// - 无破盾增伤
        /// - 保留：抗性、易伤、虚弱/精英/首领修正
        /// </summary>
        private List<IDamageModifier> BuildBuffSkillDamagePipeline()
        {
            return new List<IDamageModifier>
            {
                new ResistanceModifier(),       // 2. 抗性 / 护甲减伤
                new FragileModifier(),          // 4. 易伤增伤
            };
        }

        /// <summary>
        /// 执行完整伤害计算管线
        /// </summary>
        /// <param name="ctx">伤害上下文，由调用方预先填充所有输入字段</param>
        /// <returns>从 ctx 构造的结算结果，miss 时 FinalDamage 为 0</returns>
        public DamageResult Execute(DamageContext ctx)
        {
            var flowEnabled = DamagePipelineFlowLogger.IsEnabled;
            var initialSnapshot = flowEnabled ? DamagePipelineFlowLogger.Capture(ctx) : default;

            if (flowEnabled)
            {
                DamagePipelineFlowLogger.LogPipelineStart(ctx, PipelineType);
            }

            foreach (var mod in _mods)
            {
                var before = flowEnabled ? DamagePipelineFlowLogger.Capture(ctx) : default;
                mod.Apply(ctx);
                if (flowEnabled)
                {
                    DamagePipelineFlowLogger.LogModifierStep(mod, ctx, before);
                }

                if (ctx.IsMiss)
                {
                    if (flowEnabled)
                    {
                        DamagePipelineFlowLogger.LogPipelineEnd(ctx, initialSnapshot);
                    }
                    return new DamageResult(ctx);
                }
            }

            if (flowEnabled)
            {
                DamagePipelineFlowLogger.LogPipelineEnd(ctx, initialSnapshot);
            }

            return new DamageResult(ctx);
        }

        /// <summary>
        /// 获取当前管线包含的 Modifier 类型列表（用于调试）
        /// </summary>
        public List<string> GetModifierTypes()
        {
            var types = new List<string>();
            foreach (var mod in _mods)
            {
                types.Add(mod.GetType().Name);
            }
            return types;
        }
    }
}
