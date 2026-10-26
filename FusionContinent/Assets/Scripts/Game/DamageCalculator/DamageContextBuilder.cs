using Config;

using FusionContinent;

using FusionContinent.Skill;

using Manager;



/// <summary>

/// 伤害上下文构建器：负责在进入 <see cref="DamagePipeline"/> 前填充 <see cref="DamageContext"/>。

/// <para>

/// 设计约束：本类只读取角色/技能/配置属性并写入 ctx，<b>不修改</b> SoldierCard / MonsterCard 状态（不扣血、不加 Buff、不发事件）。

/// </para>

/// <para>

/// 典型调用链：

/// <list type="number">

/// <item>受击方调用 <see cref="CalculateIncoming"/> 或 <see cref="CalculateOutgoing"/></item>

/// <item>内部调用 <see cref="FillIncomingContext"/> / <see cref="FillOutgoingContext"/> 填充 ctx</item>

/// <item>（出站）可选 <see cref="ApplyPrePipelineModifiers"/> 做 SkillDamage / 灼烧加成</item>

/// <item><see cref="GamePlayManager.GetDamagePipeline"/> → <see cref="DamagePipeline.Execute"/> 执行 Modifier 链</item>

/// <item>返回 <see cref="DamageResult"/>，由 Receiver 层扣血/护盾/飘字</item>

/// </list>

/// </para>

/// </summary>

public static class DamageContextBuilder

{

    #region 入站伤害（怪物 → 英灵）



    /// <summary>

    /// 计算怪物对英灵的入站伤害：构建 ctx 并执行 <see cref="DamagePipelineType.MonsterDamage"/> 管线。

    /// </summary>

    /// <param name="defender">受击英灵</param>

    /// <param name="attacker">攻击怪物（可为 null，如 BurnBuff 场景）</param>

    /// <param name="damageType">物理 / 魔法伤害类型，决定读取哪项抗性</param>

    /// <param name="baseDamage">进入管线前的原始伤害</param>

    /// <returns>管线结算结果；闪避时 FinalDamage = 0</returns>

    public static DamageResult CalculateIncoming(

        SoldierCard defender,

        MonsterCard attacker,

        TakeDamageType damageType,

        int baseDamage)

    {

        var ctx = new DamageContext();

        FillIncomingContext(ctx, defender, attacker, damageType, baseDamage);

        return GamePlayManager.Default

            .GetDamagePipeline(DamagePipelineType.MonsterDamage)

            .Execute(ctx);

    }



    /// <summary>

    /// 填充「怪物攻击英灵」的 DamageContext 输入字段。

    /// <para>特点：有闪避（missRate）、无暴击（CritRate=0）、无穿透、无 combo 增伤。</para>

    /// </summary>

    public static void FillIncomingContext(

        DamageContext ctx,

        SoldierCard defender,

        MonsterCard attacker,

        TakeDamageType damageType,

        int baseDamage)

    {

        // --- 基础伤害三件套：BaseDamage / FinalDamage / RawDamage 初始相同 ---

        ctx.BaseDamage = baseDamage;

        ctx.FinalDamage = baseDamage;

        ctx.RawDamage = baseDamage;



        // --- 攻防双方引用 ---

        ctx.Monster = attacker;

        ctx.Soldier = defender;

        ctx.DamageType = damageType;

        ctx.PipelineType = DamagePipelineType.MonsterDamage;



        // --- 英灵侧防御属性 ---

        ctx.missRate = defender.BaseAttributes.GetFinal(AttributeType.SoldierMissNum);

        ctx.CritRate = 0;           // 怪物攻击不暴击

        ctx.CritMultiplier = 1;

        ctx.comboNum = defender != null
            ? global::Manager.ConsecutiveSkillReleaseCounter.GetComboCount(defender.BattleSide)
            : Global.ConsecutiveSkillReleaseCount.Value;

        ctx.ComboDamageRatio = 0;   // 入站无 combo 增伤

        ctx.ComboResilienceRatio = 0;

        ctx.SoldierTakeDamage = defender.BaseAttributes.GetFinal(AttributeType.SoldierTakeDamage);

        ctx.FragileBonus = defender.BaseAttributes.GetFinal(AttributeType.SoldierFragileDamage);

        ctx.IsExecution = false;

        ctx.IsWeak = false;

        ctx.SkillId = 0;



        // --- 按伤害类型选取英灵物理/魔法抗性 ---

        ResistanceType resistanceType = ResistanceType.Armor;

        AttributeType resistanceAttr;

        switch (damageType)

        {

            case TakeDamageType.PhysicalDamage:

                resistanceType = ResistanceType.Physical;

                resistanceAttr = AttributeType.SoldierPhysicalResistance;

                break;

            case TakeDamageType.MagicDamage:

                resistanceType = ResistanceType.Magic;

                resistanceAttr = AttributeType.SoldierMagicResistance;

                break;

            default:

                resistanceAttr = AttributeType.SoldierPhysicalResistance;

                break;

        }



        ctx.Resistance = resistanceType != ResistanceType.Armor

            ? defender.BaseAttributes.GetFinal(resistanceAttr)

            : 0f;

    }



    #endregion



    #region 出站伤害（英灵/场景 → 怪物）



    /// <summary>

    /// 填充「英灵/场景技能攻击怪物」的 DamageContext 输入字段。

    /// <para>

    /// 暴击判定在此完成（或消费 SkillRuntime 预掷结果），管线内 <see cref="CritFinalModifier"/> 只应用倍率。

    /// </para>

    /// </summary>

    /// <param name="ctx">目标怪物的 damageContext（可复用，避免 GC）</param>

    /// <param name="target">受击怪物</param>

    /// <param name="damageType">物理 / 魔法</param>

    /// <param name="baseDamage">经 ApplyPrePipelineModifiers 修正后的基础伤害</param>

    /// <param name="skillCard">技能来源（null 表示场景技能）</param>

    /// <param name="soldierCard">攻击英灵（null 时从 skillCard.Parent 推断）</param>

    /// <param name="pipelineType">SoldierDamage / SceneSkillDamage / BuffSkillDamage</param>

    public static void FillOutgoingContext(

        DamageContext ctx,

        MonsterCard target,

        TakeDamageType damageType,

        int baseDamage,

        ISkill skillCard,

        SoldierCard soldierCard,

        DamagePipelineType pipelineType)

    {

        var attacker = soldierCard ?? skillCard?.Parent;



        ctx.quality = target._monsterConfig.MonsterQuality;



        // --- 暴击判定：优先消费投射物命中时拷贝的预掷结果 ---

        if (ctx.HasPreRolledCrit)

        {

            ctx.IsCrit = ctx.PreRolledCrit;

            ctx.HasPreRolledCrit = false;

        }

        else if (attacker != null)

        {

            float critRate = attacker.BaseAttributes.GetFinal(AttributeType.SoldierCriticalRate);

            // 暴击率不再生效（CriticalNoTakeEffect）
            if (attacker.BaseAttributes.GetFinal(AttributeType.CriticalNoTakeEffect) > 0f)
            {
                critRate = 0;
            }
            // 非普攻技能且未解锁「技能触发暴击」时，暴击率归零
            else if (attacker.BaseAttributes.GetFinal(AttributeType.SkillTriggersCriticalStrike) <= 0

                && skillCard != null

                && skillCard.Id != attacker.NormalAttackSkillId)

            {

                critRate = 0;

            }

            // 待消耗的「下次技能必暴」不走伪随机，避免污染失败叠加
            bool pendingMustCrit = skillCard != null
                && skillCard.Id != attacker.NormalAttackSkillId
                && attacker.HasPendingNextSkillMustCritical(skillCard.Id);

            if (pendingMustCrit)
            {
                ctx.IsCrit = true;
            }
            else if (critRate <= 0f)
            {
                ctx.IsCrit = false;
            }
            else
            {
                ctx.IsCrit = attacker.RollCriticalHit(critRate);
            }

        }

        else

        {

            ctx.IsCrit = false;

        }

        if (attacker != null && skillCard != null && skillCard.Id != attacker.NormalAttackSkillId)
        {
            Game.Battle.BattleRuleMustCriticalSupport.TryApplyPendingNextSkillMustCritical(
                attacker, skillCard.Id, ctx);
        }



        // --- 基础伤害与管线类型 ---

        ctx.DamageType = damageType;

        ctx.BaseDamage = baseDamage;

        ctx.FinalDamage = baseDamage;

        ctx.RawDamage = baseDamage;

        ctx.PipelineType = pipelineType;

        ctx.SkillId = skillCard != null ? skillCard.Id : 0;



        // --- 规则附加伤害（BattleRuleDamageModifier 写入，结算后独立走第二段管线）---

        ctx.TrueDamage = 0;

        ctx.ToughnessDamage = 0;

        ctx.ThunderboltDamage = 0;

        ctx.AttachedMagicDamage = 0;

        ctx.AttachedPhysicalDamage = 0;



        // --- 技能侧修正属性（虚弱/精英/Boss/落雷）---

        if (skillCard != null)

        {

            ctx.BrokenStateDamageCorrection =

                skillCard.Parent.BaseAttributes.GetFinal(AttributeType.BrokenStateDamageCorrection);

            ctx.CorrectionOfDamageSufferedByEliteEnemies =

                skillCard.Parent.BaseAttributes.GetFinal(AttributeType.CorrectionOfDamageSufferedByEliteEnemies);

            ctx.CorrectionOfDamageSufferedByTheLeaderEnemies =

                skillCard.Parent.BaseAttributes.GetFinal(AttributeType.CorrectionOfDamageSufferedByTheLeaderEnemies);

            ctx.ThunderDamageRate = skillCard.Attributes.GetFinal(AttributeType.SkillCriticalThunderbolt);

        }

        else

        {

            ctx.BrokenStateDamageCorrection = 0;

            ctx.CorrectionOfDamageSufferedByEliteEnemies = 0;

            ctx.CorrectionOfDamageSufferedByTheLeaderEnemies = 0;

            ctx.ThunderDamageRate = 0;

        }



        ctx.Monster = target;

        ctx.Soldier = attacker;

        ctx.PreHitMonsterHp = target.hp;  // 斩杀判定用：本段伤害前血量

        ctx.missRate = 0;                 // 出站攻击怪物无闪避



        // --- 攻击方英灵属性 ---

        if (attacker != null)

        {

            ctx.MonsterToughnessBreakDamageBonus =

                attacker.BaseAttributes.GetFinal(AttributeType.MonsterToughnessBreakDamageBonus);

            ctx.CritMultiplier = attacker.BaseAttributes.GetFinal(AttributeType.SoldierCriticalDamage);

            ctx.SoldierTakeDamage = ctx.Soldier.BaseAttributes.GetFinal(AttributeType.SoldierTakeDamage);

            ctx.AllDamageBonus = attacker.BaseAttributes.GetFinal(AttributeType.AllDamageBonuses);

            ctx.EveryDamage = attacker.BaseAttributes.GetFinal(AttributeType.EveryDamage);

            ctx.resistanceIgnorePreValueArmor = attacker.BaseAttributes.GetFinal(AttributeType.SoldierArmorPenetrationPer);

            ctx.resistanceIgnorePreValueMagic = attacker.BaseAttributes.GetFinal(AttributeType.SoldierMagicPenetrationPer);

        }

        else

        {

            // 无攻击者 → 场景技能，降级为 SceneSkillDamage 管线

            ctx.MonsterToughnessBreakDamageBonus = 0;

            ctx.CritRate = 0;

            ctx.AllDamageBonus = 0;

            ctx.EveryDamage = 0;

            ctx.CritMultiplier = 0;

            ctx.PipelineType = DamagePipelineType.SceneSkillDamage;

        }



        // --- Combo 连击增伤（出站攻击怪物时生效）---

        ctx.comboNum = attacker != null
            ? global::Manager.ConsecutiveSkillReleaseCounter.GetComboCount(attacker.BattleSide)
            : Global.ConsecutiveSkillReleaseCount.Value;

        ctx.ComboDamageRatio = DataConfigManager.Tables.TbGameBattleConst.ComboDamageRatio;

        ctx.ComboResilienceRatio = DataConfigManager.Tables.TbGameBattleConst.ComboResilienceRatio;

        ctx.SoldierTakeDamage = 0;   // 出站打怪不使用英灵受击加减成

        ctx.FragileBonus = 0;        // 出站 FragileModifier 对怪物侧不生效（FragileBonus 为 0）



        // --- 穿透 / 吸血（穿透固定值由 BattleRuleDamageModifier 在管线内写入 resistanceIgnoreValue）---

        if (attacker != null)

        {

            ctx.PhysicalBloodSucking = attacker.BaseAttributes.GetFinal(AttributeType.PhysicalBloodSucking);

            ctx.MagicBloodSucking = attacker.BaseAttributes.GetFinal(AttributeType.MagicBloodSucking);

            ctx.resistanceIgnoreValue = 0f;

            ctx.resistanceIgnorePenre = 1f;

            ctx.resistanceIgnorePreValueArmor = 0f;

            ctx.resistanceIgnorePreValueMagic = 0f;

            ctx.resistanceIgnorePreValuePenreArmor = 1f;

            ctx.resistanceIgnorePreValuePenreMagic = 1f;

        }



        // --- 怪物抗性 + 技能穿透 ---

        ResistanceType resistanceType = ResistanceType.Armor;

        AttributeType resistanceAttr;

        AttributeType soldierAttr;

        AttributeType soldierPreAttr;

        switch (damageType)

        {

            case TakeDamageType.PhysicalDamage:

                resistanceType = ResistanceType.Physical;

                resistanceAttr = AttributeType.MonsterPhysicalResistance;

                soldierAttr = AttributeType.SoldierArmorPenetration;

                soldierPreAttr = AttributeType.SoldierArmorPenetrationPer;

                break;

            case TakeDamageType.MagicDamage:

                resistanceType = ResistanceType.Magic;

                resistanceAttr = AttributeType.MonsterMagicResistance;

                soldierAttr = AttributeType.SoldierMagicPenetration;

                soldierPreAttr = AttributeType.SoldierMagicPenetrationPer;

                break;

            default:

                resistanceAttr = AttributeType.MonsterPhysicalResistance;

                soldierAttr = AttributeType.SoldierArmorPenetration;

                soldierPreAttr = AttributeType.SoldierArmorPenetrationPer;

                break;

        }



        if (resistanceType != ResistanceType.Armor)

        {

            ctx.Resistance = ctx.Monster.Attributes.GetFinal(resistanceAttr);

            if (attacker != null && skillCard != null)

            {

                ctx.resistanceIgnore = skillCard.Attributes.GetFinal(soldierAttr);

                ctx.resistanceIgnorePenetraValue = skillCard.Attributes.GetFinal(soldierPreAttr);

            }

        }

        else

        {

            ctx.Resistance = 0;

        }



        // --- 模块斩杀阈值预判（普通怪 + 血量比例 ≤ MonsterExecutionThreshold）---

        var executionThresholdValue =

            GamePlayManager.Default.ModuleAttributes.GetFinal(AttributeType.MonsterExecutionThreshold);

        var residueHpPerValue = target.maxHp > 0 ? (float)target.hp / target.maxHp : 0f;

        ctx.IsExecution = target._monsterConfig.MonsterQuality == MonsterQuality.Ordinary

                          && target.hp > 0

                          && executionThresholdValue > 0

                          && executionThresholdValue >= residueHpPerValue;



        ctx.ToughnessDamage = skillCard != null

            ? skillCard.Attributes.GetFinal(AttributeType.SkillToughnessDamage)

            : 0f;

        ctx.IsWeak = target.monsterData.monsterState == MonsterState.weak;

    }



    /// <summary>

    /// 管线前伤害修正（在 FillOutgoingContext 之前调用）。

    /// <para>当前包含：SkillDamage 百分比加成、灼烧层数 × SkillFireDamage 加成。</para>

    /// </summary>

    /// <returns>修正后的 baseDamage，供 FillOutgoingContext 使用</returns>

    public static int ApplyPrePipelineModifiers(MonsterCard target, ISkill skillCard, int damage)

    {

        if (skillCard == null)

        {

            return damage;

        }



        float skillDamageBonus = skillCard.Attributes.GetFinal(AttributeType.SkillDamage);

        if (skillDamageBonus > 0)

        {

            damage = (int)(damage * (1 + skillDamageBonus));

        }



        float fireDamageBonus = skillCard.Attributes.GetFinal(AttributeType.SkillFireDamage);

        if (fireDamageBonus > 0)

        {

            int burnStack = 0;

            foreach (var buff in target.BuffController.GetBuffs(BuffType.Burn))

            {

                burnStack += buff.Stack;

            }



            if (burnStack > 0)

            {

                damage = (int)(damage * (1 + fireDamageBonus * burnStack));

            }

        }



        return damage;

    }



    /// <summary>

    /// 计算出站伤害：填充 ctx → 执行对应管线 → 清理临时标志。

    /// <para>由 <see cref="MonsterDamageService"/> 调用，ctx 通常复用 target.damageContext。</para>

    /// </summary>

    /// <param name="skipBattleRuleBeforeTrigger">

    /// true 时跳过 BattleRuleDamageModifier 的 Before 规则（连击追加攻击、附加魔法/物理伤害第二段等）

    /// </param>

    public static DamageResult CalculateOutgoing(

        DamageContext ctx,

        MonsterCard target,

        TakeDamageType damageType,

        int baseDamage,

        ISkill skillCard,

        SoldierCard soldierCard,

        DamagePipelineType pipelineType,

        bool skipBattleRuleBeforeTrigger)

    {

        FillOutgoingContext(ctx, target, damageType, baseDamage, skillCard, soldierCard, pipelineType);

        if (skipBattleRuleBeforeTrigger)

        {

            ctx.SkipBattleRuleBeforeTrigger = true;

        }



        var result = GamePlayManager.Default

            .GetDamagePipeline(ctx.PipelineType)

            .Execute(ctx);



        // 单次命中结束后重置，避免污染下次命中

        ctx.SkipBattleRuleBeforeTrigger = false;

        ctx.HasCastDamageModifier = false;



        return result;

    }



    #endregion

}


