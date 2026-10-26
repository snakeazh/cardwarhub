using System.Collections.Generic;
using Config;
using FusionContinent.Skill;
using Manager;

namespace FusionContinent
{
    /// <summary>
    /// 角色技能组件 - 管理技能字典与学习/查询，不处理释放、动画与目标选择。
    /// </summary>
    public class SoldierSkillComponent
    {
        private readonly SoldierCard _soldier;
        private Dictionary<int, ISkill> _skills = new();

        public SoldierSkillComponent(SoldierCard soldier)
        {
            _soldier = soldier;
        }

        public void Reset()
        {
            _skills = new();
        }

        public Dictionary<int, ISkill> GetSkillAll()
        {
            return _skills;
        }

        public ISkill GetSkillById(int skillId)
        {
            return _skills.GetValueOrDefault(skillId);
        }

        public ISkill GetSkillByType(SpecialEffectType skillType)
        {
            foreach (var skill in _skills.Values)
            {
                if (skill.Type == skillType)
                {
                    return skill;
                }
            }

            return null;
        }

        public ISkill GetReadySkillById(int skillId, bool isActiveRelease = false)
        {
            var skill = GetSkillById(skillId);
            if (skill == null)
            {
                LogTool.LogWarning($"[SkillRelease-GetReady] {_soldier.cardId} 获取技能失败: skillId={skillId} 未找到");
                return null;
            }

            if (isActiveRelease)
            {
                return skill;
            }

            if (skillId == _soldier.NormalAttackSkillId || skill.IsReady(false))
            {
                return skill;
            }

            LogTool.LogWarning($"[SkillRelease-GetReady] {_soldier.cardId} 获取技能失败: skillId={skillId} 冷却中, isActiveRelease={isActiveRelease}");
            return null;
        }

        public ISkill LearnSkill(int skillId, int skillLevel, bool isPassSkill = false)
        {
            var cardCfg = DataConfigManager.Tables.TbCardBaseConfig.GetOrDefault(skillId);
            if (cardCfg.Type != CardType.SkillPassive && !SkillConfigManager.IsLegacyCastOnlySkill(skillId))
                GamePlayManager.Default._skillConfigManager.SkillConfigInsert(skillId);
            if (!_skills.ContainsKey(skillId))
            {
                LogTool.Log(skillId);
                var skill = SkillFactory.CreateSkill(skillId, skillLevel, _soldier);
                _skills.Add(skillId, skill);
                return skill;
            }

            return null;
        }

        public ISkill RelearnSkill(int skillId, int skillLevel)
        {
            var skill = SkillFactory.CreateSkill(skillId, skillLevel, _soldier);
            _skills[skillId] = skill;
            return skill;
        }
    }
}
