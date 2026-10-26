using System.Collections.Generic;
using Config;
using FusionContinent;
using FusionContinent.Attribute;
using UnityEngine;

/// <summary>
/// 伤害计算的上下文对象，贯穿整个 DamagePipeline 的执行链路。
/// 
/// 执行流程：
/// 1. 调用方（SoldierCard / MonsterCard）在进入 pipeline 前填充所有输入字段
/// 2. 各 IDamageModifier 按顺序读取并修改 ctx 中的值
/// 3. 最终通过 ctx.FinalDamage 获得结算后的伤害
/// 
/// 注意：pipeline 内各 Modifier 之间通过共享 ctx 通信，无返回值。
/// </summary>
public class DamageContext
{
    // ==================== 伤害基础 ====================

    public List<AttributeValue> attributeValues = new List<AttributeValue>();

    // 暴击预掷（替代旧 SkillCarrying.isCritical）：
    // SkillRuntime 释放时写入 → MonsterManager 投射物命中时拷贝 → TakeDamage 读入 IsCrit。

    /// <summary>施法侧是否已在 SkillRuntime 预掷暴击（投射物命中后消费一次即清零）</summary>
    public bool HasPreRolledCrit;

    /// <summary>预掷暴击结果；仅 HasPreRolledCrit 为 true 时有效</summary>
    public bool PreRolledCrit;

    /// <summary>为 true 时 BattleRuleDamageModifier 跳过 Before 规则（连击追加攻击等）</summary>
    public bool SkipBattleRuleBeforeTrigger;

    /// <summary>施法时刻规则临时伤害修正（由 SkillContext 拷贝，覆盖多段/延迟命中）</summary>
    public bool HasCastDamageModifier;

    public AttributeType CastDamageModifierAttributeType;

    public float CastDamageModifierBaseValue;

    public float CastDamageModifierPreValue;

    /// <summary>本次伤害对应的技能 Id（0 表示无技能来源）</summary>
    public int SkillId;
    
    /// <summary>进入 pipeline 前的原始伤害值（int→float 隐式转换）</summary>
    public float BaseDamage;

    /// <summary>经过 pipeline 全部节点结算后的最终伤害，由最后一个 Modifier 写出</summary>
    public float FinalDamage;

    public float TrueDamage;

    /// <summary>原始伤害的 int 副本，部分 Modifier（如 StrengthModifier）会同时修改 RawDamage</summary>
    public int RawDamage;

    /// <summary>当前的combo数量</summary>
    public int comboNum;

    public float MagicBloodSucking = 0;

    public float PhysicalBloodSucking = 0;

    // ==================== 暴击相关 ====================

    /// <summary>是否触发暴击，由 CritModifier 在 pipeline 中判定并写入</summary>
    public bool IsCrit;

    /// <summary>暴击率（0~1），由调用方在执行前从属性系统读取并填充</summary>
    public float CritRate = 0f;

    /// <summary>暴击伤害倍率（如 1.5 表示暴击时伤害 ×1.5）</summary>
    public float CritMultiplier = 1.5f;

    /// <summary>韧性伤害</summary>
    public float ToughnessDamage = 0f;

    /// <summary>怪物破韧后受到伤害加成</summary>
    public float MonsterToughnessBreakDamageBonus = 0f;

    /// <summary>破韧伤害修正</summary>
    public float BrokenStateDamageCorrection = 0f;

    /// <summary>精英伤害修正</summary>
    public float CorrectionOfDamageSufferedByEliteEnemies = 0;
    
    /// <summary>首领伤害修正</summary>
    public float CorrectionOfDamageSufferedByTheLeaderEnemies = 0;
    
    /// <summary>所有伤害加成（从 BaseAttributes 读取，用于开局计算等场景）</summary>
    public float AllDamageBonus = 0f;

    /// <summary>所有伤害固定加值（AttributeType.EveryDamage）；物理/魔法 FinalDamage 与 TrueDamage 在非 0 时各叠加。Buff/DOT 管线不生效。</summary>
    public float EveryDamage = 0f;
    
    /// <summary>暴击落雷的伤害</summary>
    public float ThunderboltDamage = 0;
    
    /// <summary>暴击落雷的倍率</summary>
    public float ThunderDamageRate = 0;

    /// <summary>普攻物理命中前规则累加的独立魔法一击（如 101005 AddMagicDamage / AddAPDamage）</summary>
    public float AttachedMagicDamage = 0;

    /// <summary>非物理主伤害时规则累加的独立物理一击（AddADDamage 在非物理命中时）</summary>
    public float AttachedPhysicalDamage = 0;
    
    // ==================== 闪避相关 ====================

    /// <summary>是否闪避（miss），由 DodgeModifier 写入。pipeline 检测到此标志后提前中断返回 0</summary>
    public bool IsMiss;

    /// <summary>闪避率（0~1），由调用方在执行前从属性系统读取并填充</summary>
    public float missRate;
    
    // ==================== 增伤 / 减伤 ====================
    /// <summary>是否为虚弱状态</summary>
    public bool IsWeak = false;
    
    /// <summary>受到伤害加成（如 0.1 表示受到伤害 +10%），正值增伤，负值减伤</summary>
    public float SoldierTakeDamage;

    /// <summary>易伤增伤倍率（如 0.2 表示受到伤害 +20%）</summary>
    public float FragileBonus;

    /// <summary>抗性 / 护甲减免值，由调用方按伤害类型选取对应属性填充</summary>
    public float Resistance;
    public float resistanceIgnore;
    //
    public float resistanceIgnorePenetraValue;
    
    //当前穿透值
    public float resistanceIgnoreValue;
    public float resistanceIgnorePenre;
    
    /// <summary>物理穿透百分比（固定值部分）</summary>
    public float resistanceIgnorePreValueArmor;
    /// <summary>物理穿透百分比（倍率修正部分）</summary>
    public float resistanceIgnorePreValuePenreArmor;

    /// <summary>魔法穿透百分比（固定值部分）</summary>
    public float resistanceIgnorePreValueMagic;
    /// <summary>魔法穿透百分比（倍率修正部分）</summary>
    public float resistanceIgnorePreValuePenreMagic;
    
    
    /// <summary>是否斩杀斩杀无视闪避</summary>
    public bool IsExecution;

    /// <summary>本次受击前怪物血量（用于斩杀阈值判定，0 表示未记录）</summary>
    public int PreHitMonsterHp;

    /// <summary>模板斩杀线（绝对血量，0 表示本段无模板斩杀）</summary>
    public float TemplateExecuteThreshold;

    /// <summary>本段命中的模板斩杀 BuffId（日志用，0 表示无）</summary>
    public int TemplateExecuteBuffId;

    /// <summary>combo伤害计算倍率</summary>
    public float ComboDamageRatio;

    /// <summary>combo韧性伤害计算倍率</summary>
    public float ComboResilienceRatio;
    
    // ==================== 伤害类型 ====================
    public MonsterQuality quality;
    
    /// <summary>本次伤害的类型（物理 / 魔法 / 无类型），用于 ResistanceModifier 选取对应抗性字段</summary>
    public TakeDamageType DamageType;

    /// <summary>伤害计算管线类型，默认 SoldierDamage。部分场景（如 Buff/DOT）可指定为 BuffSkillDamage 以跳过不必要的属性计算</summary>
    public DamagePipelineType PipelineType = DamagePipelineType.SoldierDamage;

    // ==================== 引用 ====================

    /// <summary>被击方（SoldierCard），ShieldModifier / StrengthModifier 等需要读写目标属性</summary>
    public SoldierCard Soldier;

    /// <summary>攻击方（MonsterCard），部分 Modifier 可能需要读取攻击者信息</summary>
    public MonsterCard Monster;
    
    

    // ==================== 调试 / 记录 ====================

    /// <summary>完整计算链的步骤记录，用于调试日志和伤害面板展示</summary>
    // public List<DamageCalculationStep> Steps;
    
    public override string ToString()
    {
        return $"DamageContext(" +
               $"BaseDamage={BaseDamage}, " +
               $"FinalDamage={FinalDamage}, " +
               $"RawDamage={RawDamage}, " +
               $"comboNum={comboNum}, " +
               $"IsCrit={IsCrit}, " +
               $"CritRate={CritRate}, " +
               $"CritMultiplier={CritMultiplier}, " +
               $"ToughnessDamage={ToughnessDamage}, " +
               $"IsMiss={IsMiss}, " +
               $"missRate={missRate}, " +
               $"IsWeak={IsWeak}, " +
               $"SoldierTakeDamage={SoldierTakeDamage}, " +
               $"FragileBonus={FragileBonus}, " +
               $"Resistance={Resistance}, " +
               $"IsExecution={IsExecution}, " +
               $"ComboDamageRatio={ComboDamageRatio}, " +
               $"DamageType={DamageType}, " +
               $")";
    }
}
