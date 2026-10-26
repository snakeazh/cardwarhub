using System;
using System.Collections;
using System.Collections.Generic;
using Config;
using FusionContinent;
using FusionContinent.Skill;
using QFramework;
using UnityEngine;

public class FireBallController : MonoBehaviour
{
    private ISkill _skillCard;

    public void SetFireball(ISkill skillCard)
    {
        this._skillCard = skillCard;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 如果已经触发过，直接返回
        if (collision.name != "HitBox")
        {
            return;
        }

        MonsterCard hitTarget = collision.transform.parent.GetComponent<MonsterCard>();
        if (hitTarget != null && hitTarget.enabled != false)
        {

            // 受伤坐标位置
            var damageHitPos = collision.bounds.ClosestPoint(transform.position);
            try
            {
                // LogTool.Log(">>>> 攻击 - _cardId: "+hitTarget._cardId+", damage: "+damage+", baseAtk: "+ baseAtk);
                hitTarget.ReceiveDamage(MonsterDamageRequest.ForSkillSumTypes(hitTarget, _skillCard, damageHitPos));
            }
            catch
            {

            }
        }
    }
}
