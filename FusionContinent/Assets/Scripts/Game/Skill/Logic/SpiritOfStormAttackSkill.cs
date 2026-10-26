using Config;
using DG.Tweening;
using QFramework;
using UnityEngine;

namespace FusionContinent.Skill
{
    /**
     * 风暴之灵
     */
    public class SpiritOfStormAttackSkill : SkillBase
    {
        protected override void OnCast(object currentTargetPosition)
        {
            GameObject archerSpiritOfStorm = GamePlayManager.Default._resLoader
                .Get<GameObject>(Config.ProjectileName).Instantiate();
            // AudioKit.PlaySound("resources://Audio/火-发射后火焰");
            
            archerSpiritOfStorm.transform.SetParent(GameLand.Default.transform);
            archerSpiritOfStorm.transform.position = (Vector3)currentTargetPosition;
            archerSpiritOfStorm.transform.Find("eff_yx_jn4_02/box").GetComponent<FireBallController>().SetFireball(this);
            Object.Destroy(archerSpiritOfStorm, 2f);
        }
    }
}