using System.Collections.Generic;
using System.Text;
using Config;
using FusionContinent;
using FusionContinent.Attribute;
using Game.Battle;
using UnityEngine;

/// <summary>
/// 出站伤害管线加成计算流日志。开启 <see cref="GamePlayManager.battleLog"/>（调试键 P）后输出。
/// </summary>
public static class DamagePipelineFlowLogger
{
    /// <summary>管线开头汇总展示的增伤相关属性（含武器规则常写项）。</summary>
    private static readonly AttributeType[] TrackedDamageAttributeTypes =
    {
        AttributeType.AllDamageBonuses,
        AttributeType.EveryDamage,
        AttributeType.AddDamageValueTake,
        AttributeType.SoldierDamageChange,
        AttributeType.ArcherDamageChange,
        AttributeType.MageDamageChange,
        AttributeType.AssassinDamageChange,
        AttributeType.AssistantDamageChange,
        AttributeType.SoldierCriticalRate,
        AttributeType.SoldierCriticalDamage,
        AttributeType.SoldierArmorPenetration,
        AttributeType.SoldierMagicPenetration,
        AttributeType.SoldierArmorPenetrationPer,
        AttributeType.SoldierMagicPenetrationPer,
        AttributeType.NormalAttackDamage,
        AttributeType.AddPhysicalDamage,
        AttributeType.AddMagicDamage,
        AttributeType.AddRealDamage,
    };

    public readonly struct Snapshot
    {
        public readonly float FinalDamage;
        public readonly float TrueDamage;
        public readonly float AttachedMagicDamage;
        public readonly float AttachedPhysicalDamage;
        public readonly float PhysicalBloodSucking;
        public readonly float MagicBloodSucking;
        public readonly float ResistanceIgnoreValue;
        public readonly float ResistanceIgnorePreValueArmor;
        public readonly float ResistanceIgnorePreValueMagic;

        public Snapshot(DamageContext ctx)
        {
            FinalDamage = ctx.FinalDamage;
            TrueDamage = ctx.TrueDamage;
            AttachedMagicDamage = ctx.AttachedMagicDamage;
            AttachedPhysicalDamage = ctx.AttachedPhysicalDamage;
            PhysicalBloodSucking = ctx.PhysicalBloodSucking;
            MagicBloodSucking = ctx.MagicBloodSucking;
            ResistanceIgnoreValue = ctx.resistanceIgnoreValue;
            ResistanceIgnorePreValueArmor = ctx.resistanceIgnorePreValueArmor;
            ResistanceIgnorePreValueMagic = ctx.resistanceIgnorePreValueMagic;
        }

        public bool HasDamageChange(Snapshot other)
        {
            return !Mathf.Approximately(FinalDamage, other.FinalDamage)
                || !Mathf.Approximately(TrueDamage, other.TrueDamage)
                || !Mathf.Approximately(AttachedMagicDamage, other.AttachedMagicDamage)
                || !Mathf.Approximately(AttachedPhysicalDamage, other.AttachedPhysicalDamage)
                || !Mathf.Approximately(PhysicalBloodSucking, other.PhysicalBloodSucking)
                || !Mathf.Approximately(MagicBloodSucking, other.MagicBloodSucking)
                || !Mathf.Approximately(ResistanceIgnoreValue, other.ResistanceIgnoreValue)
                || !Mathf.Approximately(ResistanceIgnorePreValueArmor, other.ResistanceIgnorePreValueArmor)
                || !Mathf.Approximately(ResistanceIgnorePreValueMagic, other.ResistanceIgnorePreValueMagic);
        }
    }

    public static bool IsEnabled
    {
        get
        {
            var gamePlay = GamePlayManager.Default;
            return gamePlay != null && gamePlay.battleLog;
        }
    }

    public static Snapshot Capture(DamageContext ctx)
    {
        return new Snapshot(ctx);
    }

    public static void LogPipelineStart(DamageContext ctx, DamagePipelineType pipelineType)
    {
        if (!IsEnabled)
        {
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("════════ 伤害加成计算流 ════════");
        sb.AppendLine($"管线: {pipelineType}");
        sb.AppendLine(
            $"攻击方: {FormatSoldier(ctx.Soldier)} | 目标: {FormatMonster(ctx.Monster)}");
        sb.AppendLine(
            $"技能Id={ctx.SkillId} | 类型={ctx.DamageType} | 基础伤害={ctx.BaseDamage} | 预掷暴击={ctx.IsCrit}");
        sb.AppendLine(
            $"入管线 Final={ctx.FinalDamage:F1} True={ctx.TrueDamage:F1}");

        if (ctx.Soldier != null)
        {
            var attrs = ctx.Soldier.BaseAttributes;
            sb.AppendLine("── 攻击方关键增伤属性(Final) ──");
            sb.AppendLine(
                $"  AllDamageBonuses(135)={FormatPercent(attrs.GetFinal(AttributeType.AllDamageBonuses))}");
            sb.AppendLine($"  EveryDamage={attrs.GetFinal(AttributeType.EveryDamage):F2}");
            sb.AppendLine(
                $"  职业增伤={FormatPercent(ResolveCareerDamageBonus(ctx.Soldier))} ({ctx.Soldier.soldierBattle.CarType})");
            sb.AppendLine(
                $"  AddDamageValueTake(227)={FormatPercent(attrs.GetFinal(AttributeType.AddDamageValueTake))}");
            sb.AppendLine(
                $"  暴击率={FormatPercent(attrs.GetFinal(AttributeType.SoldierCriticalRate))} | 暴击伤害={attrs.GetFinal(AttributeType.SoldierCriticalDamage):F1}%");
            sb.AppendLine(
                $"  物穿%={FormatPercent(attrs.GetFinal(AttributeType.SoldierArmorPenetrationPer))} | 魔穿%={FormatPercent(attrs.GetFinal(AttributeType.SoldierMagicPenetrationPer))}");
            AppendBattleRuleModifiers(sb, attrs);
        }

        sb.AppendLine("── Modifier 步骤 ──");
        LogTool.Log(sb.ToString());
    }

    public static void LogModifierStep(IDamageModifier mod, DamageContext ctx, Snapshot before)
    {
        if (!IsEnabled)
        {
            return;
        }

        var after = Capture(ctx);
        if (!after.HasDamageChange(before))
        {
            LogTool.Log($"  [{mod.GetType().Name}] 无变化 | {BuildModifierHint(mod, ctx)}");
            return;
        }

        LogTool.Log(
            $"  [{mod.GetType().Name}] {FormatSnapshotDelta(before, after)} | {BuildModifierHint(mod, ctx)}");
    }

    public static void LogPipelineEnd(DamageContext ctx, Snapshot initial)
    {
        if (!IsEnabled)
        {
            return;
        }

        var final = Capture(ctx);
        var sb = new StringBuilder();
        sb.AppendLine("── 结算 ──");
        sb.AppendLine(
            $"FinalDamage: {initial.FinalDamage:F1} → {final.FinalDamage:F1} ({FormatDelta(initial.FinalDamage, final.FinalDamage)})");
        if (!Mathf.Approximately(initial.TrueDamage, 0f) || !Mathf.Approximately(final.TrueDamage, 0f))
        {
            sb.AppendLine(
                $"TrueDamage: {initial.TrueDamage:F1} → {final.TrueDamage:F1} ({FormatDelta(initial.TrueDamage, final.TrueDamage)})");
        }
        if (!Mathf.Approximately(final.AttachedMagicDamage, 0f))
        {
            sb.AppendLine($"附加魔法伤害: {final.AttachedMagicDamage:F1}");
        }
        if (!Mathf.Approximately(final.AttachedPhysicalDamage, 0f))
        {
            sb.AppendLine($"附加物理伤害: {final.AttachedPhysicalDamage:F1}");
        }
        if (ctx.IsMiss)
        {
            sb.AppendLine("结果: 闪避");
        }
        else if (ctx.IsCrit)
        {
            sb.AppendLine($"结果: 暴击 (倍率 {ctx.CritMultiplier:F1}%)");
        }
        sb.AppendLine("════════════════════════════════");
        LogTool.Log(sb.ToString());
    }

    /// <param name="persistToAttributeSet">0=持久 AttributeSet；1=当次 DamageContext；-1=未知</param>
    /// <param name="attrFinalBefore">属性 Final 写入前；无 AttributeSet 写入时传 NaN</param>
    /// <param name="attrFinalAfter">属性 Final 写入后</param>
    public static void LogRuleEffect(
        int ruleId,
        TriggerType trigger,
        EffectParamStruct effect,
        Snapshot before,
        Snapshot after,
        int persistToAttributeSet = -1,
        float attrFinalBefore = float.NaN,
        float attrFinalAfter = float.NaN)
    {
        if (!IsEnabled)
        {
            return;
        }

        var damageChanged = after.HasDamageChange(before);
        var attrChanged = !float.IsNaN(attrFinalBefore)
            && !float.IsNaN(attrFinalAfter)
            && !Mathf.Approximately(attrFinalBefore, attrFinalAfter);
        var isAttrOrDamageEffect = effect != null
            && (effect.EffectType == EffectGroupsType.ModifyAttr
                || effect.EffectType == EffectGroupsType.AddADDamage
                || effect.EffectType == EffectGroupsType.AddAPDamage
                || effect.EffectType == EffectGroupsType.AddRealDamage);

        // ModifyAttr / Add*Damage 始终打印（即使伤害未变，也能看到规则添加了什么属性）
        if (!damageChanged && !attrChanged && !isAttrOrDamageEffect)
        {
            return;
        }

        var sb = new StringBuilder();
        sb.Append($"    ↳ 规则 RuleId={ruleId} Trigger={trigger}");
        if (effect != null)
        {
            sb.Append($" | {FormatEffectAppliedAttribute(effect, persistToAttributeSet)}");
        }

        if (attrChanged)
        {
            sb.Append(
                $" | AttrFinal {attrFinalBefore:F4}→{attrFinalAfter:F4} ({FormatDelta(attrFinalBefore, attrFinalAfter)})");
        }

        if (damageChanged)
        {
            sb.Append($" | {FormatSnapshotDelta(before, after)}");
        }

        LogTool.Log(sb.ToString());
    }

    /// <summary>获得/学习规则写入 AttributeSet 时的属性日志（与伤害流同一开关）。</summary>
    public static void LogAcquireAttribute(
        int ruleId,
        EffectParamStruct effect,
        AttributeModifier modifier,
        string sourceKey,
        float attrFinalBefore,
        float attrFinalAfter)
    {
        if (!IsEnabled || effect == null || modifier == null)
        {
            return;
        }

        LogTool.Log(
            $"    ↳ 规则添加属性 RuleId={ruleId} | {effect.AttributeType}({(int)effect.AttributeType})"
            + $" Flat={modifier.FlatAdd:F4} Percent={FormatPercent(modifier.PercentAdd)}"
            + $" | AttrFinal {attrFinalBefore:F4}→{attrFinalAfter:F4} ({FormatDelta(attrFinalBefore, attrFinalAfter)})"
            + $" | key={sourceKey}"
            + $" | cfg base={effect.BaseValue} pre={effect.PreValue} src={effect.SourceAttrType}");
    }

    private static string BuildModifierHint(IDamageModifier mod, DamageContext ctx)
    {
        switch (mod)
        {
            case BattleRuleDamageModifier _:
                return "战斗规则 OnDamageBefore/AttackBefore/SkillBefore/CriticalBefore";
            case EveryDamageModifier _:
                return $"EveryDamage 固定加值={ctx.EveryDamage:F2}";
            case AttributesModifier _:
                return $"AllDamageBonuses 百分比={FormatPercent(ctx.AllDamageBonus)}";
            case CareerDamageModifier _:
                return $"职业增伤={FormatPercent(ResolveCareerDamageBonus(ctx.Soldier))}";
            case TakeDamageModifier _:
                return $"受击加减成 SoldierTakeDamage={FormatPercent(ctx.SoldierTakeDamage)}";
            case StrengthModifier _:
                return "力量主属性: RawDamage×20% 转 FinalDamage";
            case ResistanceModifier _:
                return
                    $"抗性={ctx.Resistance:F1} 穿透值={ctx.resistanceIgnoreValue:F1} 物穿%={FormatPercent(ctx.resistanceIgnorePreValueArmor)} 魔穿%={FormatPercent(ctx.resistanceIgnorePreValueMagic)}";
            case CritFinalModifier _:
                return ctx.IsCrit
                    ? $"暴击倍率={ctx.CritMultiplier:F1}%"
                    : "未暴击";
            case FragileModifier _:
                return $"易伤 FragileBonus={FormatPercent(ctx.FragileBonus)}";
            case SoulFlawsBreakModifier _:
                return ctx.IsWeak ? "破韧虚弱 ×3" : "未虚弱";
            case DodgeModifier _:
                return $"闪避率={FormatPercent(ctx.missRate)}";
            default:
                return string.Empty;
        }
    }

    private static void AppendBattleRuleModifiers(StringBuilder sb, AttributeSet attrs)
    {
        if (attrs == null)
        {
            return;
        }

        sb.AppendLine("── 规则叠加修饰(SchoolCard / BattleRule*) ──");
        var found = false;
        for (int i = 0; i < TrackedDamageAttributeTypes.Length; i++)
        {
            var attrType = TrackedDamageAttributeTypes[i];
            var attrValue = attrs.GetAttr(attrType);
            var modifiers = attrValue.GetModifiers();
            if (modifiers == null || modifiers.Count == 0)
            {
                continue;
            }

            foreach (var pair in modifiers)
            {
                var mod = pair.Value;
                if (!IsBattleRuleModifier(mod))
                {
                    continue;
                }

                found = true;
                sb.AppendLine(
                    $"  {attrType}({(int)attrType}) Flat={mod.FlatAdd:F4} Percent={FormatPercent(mod.PercentAdd)}"
                    + $" | Final={attrValue.FinalValue:F4} | [{mod.SourceType}:{mod.SourceKey}]");
            }
        }

        if (!found)
        {
            sb.AppendLine("  (无)");
        }
    }

    private static bool IsBattleRuleModifier(AttributeModifier mod)
    {
        if (mod == null || string.IsNullOrEmpty(mod.SourceKey))
        {
            return false;
        }

        return mod.SourceKey.StartsWith("BattleRule");
    }

    private static string FormatEffectAppliedAttribute(EffectParamStruct effect, int persistToAttributeSet)
    {
        var target = persistToAttributeSet == 1
            ? "当次伤害(DamageContext)"
            : persistToAttributeSet == 0
                ? "持久属性(AttributeSet)"
                : "目标未知";

        if (effect.EffectType == EffectGroupsType.AddADDamage
            || effect.EffectType == EffectGroupsType.AddAPDamage
            || effect.EffectType == EffectGroupsType.AddRealDamage)
        {
            return $"添加伤害 {effect.EffectType}"
                + $" base={effect.BaseValue} pre={effect.PreValue} src={effect.SourceAttrType}"
                + $" → {target}";
        }

        return $"添加属性 {effect.AttributeType}({(int)effect.AttributeType})"
            + $" base={effect.BaseValue} pre={effect.PreValue} src={effect.SourceAttrType}"
            + $" → {target}";
    }

    private static string FormatSnapshotDelta(Snapshot before, Snapshot after)
    {
        var sb = new StringBuilder();
        AppendFieldDelta(sb, "Final", before.FinalDamage, after.FinalDamage);
        if (!Mathf.Approximately(before.TrueDamage, 0f) || !Mathf.Approximately(after.TrueDamage, 0f))
        {
            AppendFieldDelta(sb, "True", before.TrueDamage, after.TrueDamage);
        }
        if (!Mathf.Approximately(before.AttachedMagicDamage, 0f)
            || !Mathf.Approximately(after.AttachedMagicDamage, 0f))
        {
            AppendFieldDelta(sb, "附魔", before.AttachedMagicDamage, after.AttachedMagicDamage);
        }
        if (!Mathf.Approximately(before.AttachedPhysicalDamage, 0f)
            || !Mathf.Approximately(after.AttachedPhysicalDamage, 0f))
        {
            AppendFieldDelta(sb, "附物", before.AttachedPhysicalDamage, after.AttachedPhysicalDamage);
        }
        if (!Mathf.Approximately(before.ResistanceIgnoreValue, after.ResistanceIgnoreValue))
        {
            sb.Append($" 穿透值 {before.ResistanceIgnoreValue:F1}→{after.ResistanceIgnoreValue:F1}");
        }
        if (!Mathf.Approximately(before.ResistanceIgnorePreValueArmor, after.ResistanceIgnorePreValueArmor))
        {
            sb.Append(
                $" 物穿% {FormatPercent(before.ResistanceIgnorePreValueArmor)}→{FormatPercent(after.ResistanceIgnorePreValueArmor)}");
        }
        if (!Mathf.Approximately(before.ResistanceIgnorePreValueMagic, after.ResistanceIgnorePreValueMagic))
        {
            sb.Append(
                $" 魔穿% {FormatPercent(before.ResistanceIgnorePreValueMagic)}→{FormatPercent(after.ResistanceIgnorePreValueMagic)}");
        }
        return sb.ToString().Trim();
    }

    private static void AppendFieldDelta(StringBuilder sb, string label, float before, float after)
    {
        if (sb.Length > 0)
        {
            sb.Append(" | ");
        }
        sb.Append($"{label} {before:F1}→{after:F1} ({FormatDelta(before, after)})");
    }

    private static string FormatDelta(float before, float after)
    {
        var delta = after - before;
        if (Mathf.Approximately(delta, 0f))
        {
            return "±0";
        }
        return delta >= 0f ? $"+{delta:F4}" : $"{delta:F4}";
    }

    private static string FormatPercent(float value)
    {
        return $"{value * 100f:F1}%";
    }

    private static string FormatSoldier(SoldierCard soldier)
    {
        if (soldier == null)
        {
            return "无";
        }
        return $"{soldier.name}(cardId={soldier.cardId})";
    }

    private static string FormatMonster(MonsterCard monster)
    {
        if (monster == null)
        {
            return "无";
        }
        return $"{monster.name}(hp={monster.hp})";
    }

    private static float ResolveCareerDamageBonus(SoldierCard soldier)
    {
        if (soldier == null)
        {
            return 0f;
        }

        AttributeType attrType;
        switch (soldier.soldierBattle.CarType)
        {
            case CareerType.Warrior:
                attrType = AttributeType.SoldierDamageChange;
                break;
            case CareerType.Archer:
                attrType = AttributeType.ArcherDamageChange;
                break;
            case CareerType.Wizard:
                attrType = AttributeType.MageDamageChange;
                break;
            case CareerType.Assassin:
                attrType = AttributeType.AssassinDamageChange;
                break;
            case CareerType.Assistant:
                attrType = AttributeType.AssistantDamageChange;
                break;
            default:
                return 0f;
        }

        return soldier.BaseAttributes.GetFinal(attrType);
    }
}
