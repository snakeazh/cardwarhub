using Config;
using DG.Tweening;
using QFramework;
using UnityEngine;

namespace FusionContinent.Skill
{
    /**
     * 裂空风刃
     */
    public class CrackedAirBladeAttackSkill : SkillBase
    {
        protected override void OnCast(object finalDirection)
        {
            GamePlayManager.Default.crackedAirBlade.SetCrackedAirBlade(Parent.transform.position, this, 
                Parent, GetLaunchSpeed());
        }
    }
}