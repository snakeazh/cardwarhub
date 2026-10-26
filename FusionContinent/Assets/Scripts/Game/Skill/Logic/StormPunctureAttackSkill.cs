using Config;
using DG.Tweening;
using Game.Skill.Logic;
using QFramework;
using UnityEngine;

namespace FusionContinent.Skill
{
    /**
     * 风暴穿刺
     */
    public class StormPunctureAttackSkill : SkillBase
    {
        protected override void OnCast(object finalDirection)
        {
            GameObject magicianElectricBall = GamePlayManager.Default._resLoader
                .Get<GameObject>("AnnaSkillStormPierce02rect").Instantiate();

            var localScale = Attributes.GetFinal(AttributeType.SkillRange);
            magicianElectricBall.transform.SetParent(GameLand.Default.transform);
            magicianElectricBall.transform.localScale = Vector3.one * (100f + localScale);
            
            // 计算子弹的旋转角度，使其朝向目标方向
            var direction = (Vector3)finalDirection;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            magicianElectricBall.transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
            magicianElectricBall.transform.position = Parent.transform.position;

            var missileSpeed = Attributes.GetFinal(AttributeType.SkillMissileSpeed);
            magicianElectricBall.GetComponent<WallReboundBullet>().SetElectricBall(this, direction, missileSpeed);
        }
    }
}