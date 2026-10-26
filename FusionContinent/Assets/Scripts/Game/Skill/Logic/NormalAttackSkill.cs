using System.Collections;
using Config;
using DG.Tweening;
using QFramework;
using UnityEngine;
using UnityEngine.UI;

namespace FusionContinent.Skill
{
    /**
     * 普通攻击
     */
    public class NormalAttackSkill : SkillBase
    {
        private Tween atktween;

        protected override void OnCast(object finalDirection)
        {
            // 投射子弹
            var pool = ObjectPoolManager.Instance.GetGameObjectPoolOrCreate(Config.ProjectileName);
            if (pool != null)
            {
                // 实例化投射物
                var projectileAttack = pool.Get();
                projectileAttack.transform.position = Parent.transform.position;
                projectileAttack.SetActive(true);
                projectileAttack.transform.SetParent(GameLand.Default.Bullet);
                projectileAttack.LocalScale(
                    Parent.BaseAttributes.GetFinal(AttributeType.SoldierBulletSizeCorrection));
                var projectileScript = projectileAttack.GetComponent<Projectile>();

                switch (Config.ProjectileName)
                {
                    case "ArcherProjectile":
                        // 弓箭射出灰尘
                        // var archerDust = GamePlayManager.Default._resLoader
                        //     .LoadSync<GameObject>("ArcherProjectileDust").Instantiate();
                        // archerDust.transform.position = new Vector3(Parent.transform.position.x,
                        //     Parent.transform.position.y + Parent.transform.localScale.y + 1f, 0);
                        // archerDust.transform.SetParent(GameLand.Default.Bullet);
                        // archerDust.LocalScale(1.5f);
                        // DOTween.To(v => { }, 0, 0, 1f).onComplete += () => { Object.Destroy(archerDust, 0.01f); };
                        // AudioKit.PlaySound("resources://Audio/弓箭发射出去的音效");
                        break;
                    case "SpearmenProjectile01":
                        // AudioKit.PlaySound("resources://Audio/长剑发射出去的音效");
                        break;
                    case "AssassinProjectile01":
                        projectileScript.GetComponent<ArrowRotationController>().StartRotationAnimation(
                            projectileScript.parentCard.AttackRange._currentTarget.transform.position);
                        break;
                }

                projectileScript.SpecialEffectType = Type;
                projectileScript.pool = pool;

                // 特殊技能效果
                switch (Type)
                {
                    // 万箭齐发拖尾效果
                    case SpecialEffectType.Wanjian:
                    case SpecialEffectType.SquallyShowers:
                        if (projectileAttack.transform.Find("AVolleyOfArrows") == null)
                        {
                            GenericObjectPool pool2 =
                                ObjectPoolManager.Instance.GetGameObjectPool("AnnaSkillArrows01");
                            if (pool2 != null)
                            {
                                var annaSkillArrows = pool2.Get();
                                annaSkillArrows.transform.name = "AVolleyOfArrows";
                                annaSkillArrows.transform.localScale = Vector3.one;
                                annaSkillArrows.transform.SetParent(projectileAttack.transform);
                                annaSkillArrows.transform.localPosition = Vector3.zero;
                            }
                        }

                        // 学习技能穿透数量，才会加拖尾
                        var skillPenetrateNum = Attributes.GetFinal(AttributeType.SkillPenetrateNum);
                        projectileAttack.transform.Find("AVolleyOfArrows").gameObject
                            .SetActive(skillPenetrateNum > 0f);
                        break;
                    default:
                        if (projectileAttack.transform.Find("AVolleyOfArrows") != null)
                        {
                            projectileAttack.transform.Find("AVolleyOfArrows").gameObject.SetActive(false);
                        }

                        break;
                }

                // 发射子弹并传入方向
                projectileScript.Launch((Vector3)finalDirection, GetLaunchSpeed(), this, Parent);
            }
        }
        //);
        //}
    }
}