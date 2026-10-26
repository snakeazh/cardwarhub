using Config;
using DG.Tweening;
using QFramework;
using UnityEngine;

namespace FusionContinent.Skill
{
    /**
     * 超级斩
     */
    public class SuperSlashAttackSkill : SkillBase
    {
        protected override void OnCast(object finalDirection)
        {
            var spearmenRaiseHand = GamePlayManager.Default._resLoader
                .Get<GameObject>(Config.ProjectileName).Instantiate();
            spearmenRaiseHand.transform.position = new Vector3(Parent.transform.position.x,
                Parent.transform.position.y + Parent.transform.localScale.y + Parent.cardLevelScale.x, 0);
            spearmenRaiseHand.transform.SetParent(GameLand.Default.Bullet);

            var skillRange = Attributes.GetFinal(AttributeType.SkillRange);
            spearmenRaiseHand.LocalScale(skillRange > 0f ? skillRange : 1f);

            // AudioKit.PlaySound("resources://Audio/超级斩攻击音效");

            // 伤害
            var raiseHandScript = spearmenRaiseHand.transform.Find("eff_js_ts03").transform
                .Find("ani_js_ts03").transform.Find("box").GetComponent<RaiseHand>();
            raiseHandScript.Raise(this);
            Object.Destroy(spearmenRaiseHand, 2f);
        }
    }
}