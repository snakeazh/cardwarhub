using System;

namespace FusionContinent.Skill
{
    public abstract class SkillFactory
    {
        /**
         * 创建技能实例
         */
        public static ISkill CreateSkill(int skillId, int skillLevel, SoldierCard parent)
        {
            var skillCfg = DataConfigManager.Tables.TbSpecialEffectsConfig.GetOrDefault(skillId);
            if (skillCfg == null) 
                throw new Exception($"未找到技能ID: {skillId}");

            // 通过反射根据 type 创建对应技能类
            var logicName = $"FusionContinent.Skill.{skillCfg.LogicType}";
            Type skillType = Type.GetType(logicName);
            if (skillType == null)
                throw new Exception($"创建技能实例失败: SkillId: {skillId};未找到技能类: {logicName}");

            var skill = (SkillBase)Activator.CreateInstance(skillType);
            skill.Initialize(parent, skillLevel, skillCfg);
            return skill;
        }
    }
}