using Config;
using DG.Tweening;
using QFramework;
using UnityEngine;

namespace FusionContinent.Skill
{
    /**
     * 疾风骤雨
     */
    public class SquallyShowersAttackSkill : SkillBase
    {
        protected override void OnCast(object finalDirection)
        {
            var continuousAtkNum = (int)Attributes.GetFinal(AttributeType.SkillContinuousAtkNum);
            GamePlayManager.Default.violentConflicts.SetViolentConf(continuousAtkNum, Parent.transform.position, this,
                GetLaunchSpeed());
        }
    }
}