using System.Collections;
using System.Collections.Generic;
using Config;
using DG.Tweening;
using FusionContinent;
using FusionContinent.Skill;
using FusionContinent.Utils;
using QFramework;
using UnityEngine;

public class TianLiuZhanProjectil : MonoBehaviour
{
    private int baseAtk;
    private bool unusualStatus;
    private ISkill _skillCard;
    private Sequence damageSequence;
    // Start is called before the first frame update

    void Start()
    {
    }

    public void StartAttack(ISkill skillCard)
    {
        this._skillCard = skillCard;
        
        // 剑气溢流 - 格斯的【天流斩】有20%概率让敌人受到【灼烧】效果，使其进入持续5秒的“扣血”状态，且格【易损】斯后续对该敌人造成的伤害+100%
        // if (RandomTool.CheckRandomByTarget(20))
        // {
        //     // 灼烧
        //     _skillCard.SetUnusualStatus(UnusualStatusType.Burn, 5, 1, ValueType.Number);
        //     // 易损
        //     _skillCard.SetUnusualStatus(UnusualStatusType.Vulnerable, 5, 1, ValueType.Percentage);
        //     unusualStatus = true;
        // }

        ApplyContinuousDamage((int)skillCard.Attributes.GetFinal(AttributeType.SkillContinuousDamageNum));
    }

    private void ApplyContinuousDamage(int damageNum)
    {
        // AudioKit.PlaySound("resources://Audio/天流斩攻击音效");
        MonsterCard[] monsterCards = GameObject.FindObjectsOfType<MonsterCard>();
        foreach (var target in monsterCards)
        {
            if (target.transform.localScale != Vector3.zero)
            {
                GameObject tianliueffect = GamePlayManager.Default._resLoader.Get<GameObject>("GusSkillSkyFlow03").Instantiate();
                tianliueffect.transform.SetParent(GameLand.Default.Bullet);
                tianliueffect.transform.localScale = Vector3.one * 10;
                tianliueffect.transform.position = target.transform.position;
                tianliueffect.GetComponent<TianLiuZhanProjectil>().enabled = false;
                Destroy(tianliueffect, 1f);

                // 使用 DOTween 的 Sequence 代替协程处理连续伤害
                damageSequence = DOTween.Sequence();
                for (int i = 0; i < damageNum; i++)
                {
                    damageSequence.AppendCallback(() =>
                    {
                        if (target != null && target.enabled != false)
                        {
                            try
                            {
                                target.ReceiveDamage(MonsterDamageRequest.ForSkillSumTypes(target, _skillCard, target.transform.position));
                            }
                            catch
                            {

                            }

                        }
                    });
                    damageSequence.AppendInterval(0.1f); // 每次伤害间隔
                }
            }
        }

        // 伤害完成异常效果移除
        if (unusualStatus)
        {
            // DOTween.To(v => { }, 0, 0, 1f).onComplete += () =>
            // {
            //     unusualStatus = false;
            //     _skillCard.RemoveUnusualStatus(UnusualStatusType.Burn);
            //     _skillCard.RemoveUnusualStatus(UnusualStatusType.Vulnerable);
            // };
        }
    }

}
