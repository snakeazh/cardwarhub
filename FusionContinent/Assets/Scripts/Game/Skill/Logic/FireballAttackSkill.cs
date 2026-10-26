using Config;
using DG.Tweening;
using QFramework;
using UnityEngine;

namespace FusionContinent.Skill
{
    /**
     * 火球术
     */
    public class FireballAttackSkill : SkillBase
    {
        protected override void OnCast(object currentTargetPosition)
        {
            GameObject fireball = GamePlayManager.Default._resLoader
                .Get<GameObject>(Config.ProjectileName).Instantiate();
            // AudioKit.PlaySound("resources://Audio/火-发射后火焰");
            fireball.transform.SetParent(GameLand.Default.transform);
            fireball.transform.position = (Vector3)currentTargetPosition;

            fireball.transform.Find("ani_fs_jn2_04/baozha01/box").GetComponent<FireBallController>()
                .SetFireball(this);
            Object.Destroy(fireball, 2f);
        }
    }
}