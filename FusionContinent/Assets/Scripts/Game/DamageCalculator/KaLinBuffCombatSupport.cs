using Config;
using FusionContinent;
using FusionContinent.Skill;
using FusionContinent.Utils;
using UnityEngine;

/// <summary>
/// 卡琳 Buff（KaLinBuffId）与暗杀者技能属性（KaLinCd / KaLinDamage）的联动效果。
/// </summary>
public static class KaLinBuffCombatSupport
{
    private const string LogTag = "[KaLinBuff]";

    public static bool HasKaLinBuff(MonsterCard target)
    {
        var kaLinBuffId = DataConfigManager.Tables.TbGameBattleConst.KaLinBuffId;
        foreach (var buff in target.BuffController.GetAllBuffs())
        {
            if (buff.BuffId == kaLinBuffId)
            {
                LogTool.Log($"{LogTag} 怪物 {target.uniqueId} 携带卡琳 Buff buffId={kaLinBuffId}");
                return true;
            }
        }

        LogTool.Log($"{LogTag} 怪物 {target.uniqueId} 未携带卡琳 Buff，期望 buffId={kaLinBuffId}");
        return false;
    }

    /// <summary>
    /// 命中携带卡琳 Buff 的怪物时，按 KaLinCd 修正技能剩余冷却（不低于配置修正比例，多段叠加也不低于该比例）。
    /// </summary>
    public static void TryApplyCooldownReduction(ISkill hittingSkill, SoldierCard soldierCard = null)
    {
        var gameLand = GameLand.Default;
        if (gameLand == null)
        {
            LogTool.LogWarning($"{LogTag} CD修正跳过: GameLand 不可用");
            return;
        }

        var hitSkillId = hittingSkill != null ? hittingSkill.Id : 0;
        var hitKaLinCdRaw = hittingSkill != null ? GetKaLinCdValue(hittingSkill) : 0f;
        var hitKaLinCdFinal = hittingSkill != null ? hittingSkill.Attributes.GetFinal(AttributeType.KaLinCd) : 0f;
        LogTool.Log(
            $"{LogTag} CD修正入口 hitSkillId={hitSkillId}, soldierId={soldierCard?.cardId}, " +
            $"kaLinCdRaw={hitKaLinCdRaw:F3}, kaLinCdFinal={hitKaLinCdFinal:F3}");
        gameLand.LogSkillCooldownSnapshot(hitSkillId, "KaLin-入口");

        LogSoldierKaLinCdSkillStates(soldierCard, gameLand, "KaLin-入口");

        var cooldownSkills = ResolveKaLinCdCooldownSkills(hittingSkill, soldierCard, gameLand);
        if (cooldownSkills.Count == 0)
        {
            LogTool.LogWarning(
                $"{LogTag} CD修正跳过: 未找到带 KaLinCd 且正在冷却的技能 hitSkillId={hitSkillId}, soldierId={soldierCard?.cardId}");
            return;
        }

        for (int i = 0; i < cooldownSkills.Count; i++)
        {
            ApplyCooldownReductionForSkill(cooldownSkills[i], gameLand, hitSkillId);
        }
    }

    /// <summary>
    /// 命中携带卡琳 Buff 的怪物时，按 KaLinDamage 追加一次等额的税后伤害。
    /// </summary>
    public static int TryApplyDuplicateDamage(
        MonsterCard target,
        TakeDamageType damageType,
        DamageResult firstResult,
        ISkill hittingSkill,
        Vector3 pos,
        SoldierCard soldierCard)
    {
        if (hittingSkill == null || firstResult.IsMiss || firstResult.FinalDamage <= 0f)
        {
            return 0;
        }

        var attributeSkill = ResolveKaLinAttributeSkill(soldierCard, hittingSkill, AttributeType.KaLinDamage);
        var kaLinDamage = attributeSkill.Attributes.GetFinal(AttributeType.KaLinDamage);
        if (kaLinDamage <= 0f)
        {
            return 0;
        }

        LogTool.Log(
            $"{LogTag} 追加伤害: hitSkillId={hittingSkill.Id}, attrSkillId={attributeSkill.Id}, monster={target.uniqueId}, " +
            $"damageType={damageType}, finalDamage={firstResult.FinalDamage:F0}, kaLinDamage={kaLinDamage:F3}");

        var sourceCtx = firstResult.Context;
        var duplicateCtx = sourceCtx;
        duplicateCtx.FinalDamage = firstResult.FinalDamage;
        duplicateCtx.TrueDamage = 0f;
        duplicateCtx.ToughnessDamage = 0f;
        duplicateCtx.AttachedMagicDamage = 0f;
        duplicateCtx.AttachedPhysicalDamage = 0f;
        duplicateCtx.ThunderboltDamage = 0f;

        var duplicateResult = new DamageResult(duplicateCtx);
        var applied = target.TakeDamage(duplicateResult, new DamageApplyOptions
        {
            HitPosition = pos,
            DamageType = damageType,
            Skill = hittingSkill,
            IsAgainDamage = true
        });

        if (hittingSkill.Parent != null && applied > 0)
        {
            hittingSkill.Parent.TotalDamage += applied;
            Global.TotalDamage.Value += applied;
        }

        LogTool.Log($"{LogTag} 追加伤害完成: applied={applied}");
        return applied;
    }

    private static void ApplyCooldownReductionForSkill(ISkill skillCard, GameLand gameLand, int hitSkillId)
    {
        var kaLinCd = GetKaLinCdValue(skillCard);
        var kaLinCdBase = skillCard.Attributes.GetAttr(AttributeType.KaLinCd).BaseValue;
        var kaLinCdFinal = skillCard.Attributes.GetFinal(AttributeType.KaLinCd);
        if (kaLinCd == 0f)
        {
            LogTool.Log(
                $"{LogTag} CD修正跳过: skillId={skillCard.Id} 无 KaLinCd " +
                $"(base={kaLinCdBase:F3}, final={kaLinCdFinal:F3}, ValueLimit 可能将负值截断为 0)");
            return;
        }

        var minRatio = Mathf.Abs(kaLinCdBase);
        if (minRatio <= 0f)
        {
            minRatio = Mathf.Abs(kaLinCd);
        }

        if (minRatio <= 0f)
        {
            LogTool.LogWarning(
                $"{LogTag} CD修正跳过: skillId={skillCard.Id} KaLinCd 无效 base={kaLinCdBase:F3}, raw={kaLinCd:F3}");
            return;
        }

        var cdRatio = Mathf.Max(1f + kaLinCd, minRatio);
        gameLand.LogSkillCooldownSnapshot(skillCard.Id, "KaLin-修正前");

        if (!gameLand.TryGetSkillCooldownProgress(skillCard.Id, out var remainTime, out var totalTime))
        {
            LogTool.LogWarning(
                $"{LogTag} CD修正跳过: skillId={skillCard.Id} 未处于冷却中 " +
                $"(hitSkillId={hitSkillId}, kaLinCd={kaLinCd:F3}, base={kaLinCdBase:F3}, cdRatio={cdRatio:F3})");
            gameLand.LogSkillCooldownSnapshot(skillCard.Id, "KaLin-未冷却");
            return;
        }

        var targetRemainTime = totalTime * cdRatio;
        LogTool.Log(
            $"{LogTag} CD修正计算: skillId={skillCard.Id}, hitSkillId={hitSkillId}, kaLinCdRaw={kaLinCd:F3}, " +
            $"base={kaLinCdBase:F3}, final={kaLinCdFinal:F3}, minRatio={minRatio:F3}, cdRatio={cdRatio:F3}, " +
            $"remain={remainTime:F3}, total={totalTime:F3}, targetRemain={targetRemainTime:F3}");

        if (remainTime <= targetRemainTime)
        {
            LogTool.Log(
                $"{LogTag} CD修正跳过: skillId={skillCard.Id} 当前剩余 {remainTime:F3}s 已不高于目标 {targetRemainTime:F3}s");
            return;
        }

        var deltaSeconds = targetRemainTime - remainTime;
        LogTool.Log(
            $"{LogTag} CD修正执行 ReduceSkillCooldown: skillId={skillCard.Id}, delta={deltaSeconds:F3}s");
        var success = gameLand.ReduceSkillCooldown(skillCard.Id, deltaSeconds);
        gameLand.LogSkillCooldownSnapshot(skillCard.Id, success ? "KaLin-修正后" : "KaLin-修正失败");
        LogTool.Log(
            $"{LogTag} CD修正{(success ? "成功" : "失败")}: skillId={skillCard.Id}, hitSkillId={hitSkillId}, " +
            $"delta={deltaSeconds:F3}s, 目标剩余={targetRemainTime:F3}s");
    }

    private static void LogSoldierKaLinCdSkillStates(SoldierCard soldierCard, GameLand gameLand, string reason)
    {
        if (soldierCard == null)
        {
            LogTool.Log($"{LogTag}[{reason}] soldier 为空，无法扫描 KaLinCd 技能");
            return;
        }

        foreach (var skillPair in soldierCard.GetSkillAll())
        {
            var skill = skillPair.Value;
            var kaLinCdRaw = GetKaLinCdValue(skill);
            var kaLinCdFinal = skill.Attributes.GetFinal(AttributeType.KaLinCd);
            if (kaLinCdRaw == 0f && kaLinCdFinal == 0f)
            {
                continue;
            }

            var inDict = gameLand.skill.ContainsKey(skill.Id);
            var isCooling = gameLand.IsSkillCooling(skill.Id);
            var hasProgress = gameLand.TryGetSkillCooldownProgress(skill.Id, out var remain, out var total);
            LogTool.Log(
                $"{LogTag}[{reason}] 候选 skillId={skill.Id}, kaLinCdRaw={kaLinCdRaw:F3}, kaLinCdFinal={kaLinCdFinal:F3}, " +
                $"inDict={inDict}, isCooling={isCooling}, hasProgress={hasProgress}, remain={remain:F3}, total={total:F3}");
        }
    }

    private static System.Collections.Generic.List<ISkill> ResolveKaLinCdCooldownSkills(
        ISkill hittingSkill,
        SoldierCard soldierCard,
        GameLand gameLand)
    {
        var result = new System.Collections.Generic.List<ISkill>();

        if (hittingSkill != null)
        {
            var hitHasKaLinCd = HasKaLinCd(hittingSkill);
            var hitHasProgress = gameLand.TryGetSkillCooldownProgress(hittingSkill.Id, out var hitRemain, out var hitTotal);
            LogTool.Log(
                $"{LogTag} 解析命中技能 skillId={hittingSkill.Id}, hasKaLinCd={hitHasKaLinCd}, " +
                $"hasProgress={hitHasProgress}, remain={hitRemain:F3}, total={hitTotal:F3}");

            if (hitHasKaLinCd && hitHasProgress)
            {
                result.Add(hittingSkill);
                return result;
            }
        }

        if (soldierCard == null)
        {
            return result;
        }

        foreach (var skillPair in soldierCard.GetSkillAll())
        {
            var skill = skillPair.Value;
            if (!HasKaLinCd(skill))
            {
                continue;
            }

            if (!gameLand.TryGetSkillCooldownProgress(skill.Id, out _, out _))
            {
                continue;
            }

            result.Add(skill);
        }

        return result;
    }

    private static ISkill ResolveKaLinAttributeSkill(
        SoldierCard soldierCard,
        ISkill hittingSkill,
        AttributeType attributeType)
    {
        if (hittingSkill != null && hittingSkill.Attributes.GetFinal(attributeType) != 0f)
        {
            return hittingSkill;
        }

        if (soldierCard == null)
        {
            return hittingSkill;
        }

        var gameLand = GameLand.Default;
        foreach (var skillPair in soldierCard.GetSkillAll())
        {
            var skill = skillPair.Value;
            if (skill.Attributes.GetFinal(attributeType) == 0f)
            {
                continue;
            }

            if (gameLand != null && gameLand.IsSkillCooling(skill.Id))
            {
                return skill;
            }
        }

        foreach (var skillPair in soldierCard.GetSkillAll())
        {
            var skill = skillPair.Value;
            if (skill.Attributes.GetFinal(attributeType) != 0f)
            {
                return skill;
            }
        }

        return hittingSkill;
    }

    private static bool HasKaLinCd(ISkill skill)
    {
        return GetKaLinCdValue(skill) != 0f;
    }

    /// <summary>
    /// KaLinCd 配置为负值（如 -0.5），但属性表 ValueLimit.Minimum=0 会把 GetFinal 截断为 0，需读未截断值。
    /// </summary>
    private static float GetKaLinCdValue(ISkill skill)
    {
        var attr = skill.Attributes.GetAttr(AttributeType.KaLinCd);
        var modVals = attr.SumModifierValues();
        return attr.BaseValue * modVals.Item2 + modVals.Item1;
    }
}
