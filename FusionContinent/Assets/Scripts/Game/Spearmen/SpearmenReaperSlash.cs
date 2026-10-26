using System;
using System.Collections;
using System.Collections.Generic;
using Config;
using DG.Tweening;
using FusionContinent;
using FusionContinent.Skill;
using QFramework;
using UnityEngine;

namespace FusionContinent
{
    public class SpearmenReaperSlash : MonoBehaviour
    {
        private ISkill _skillCard;
        
        public void SetData(ISkill skillCard)
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
                // LogTool.Log($">>>> 镰刀勾到怪物 - cardId: {hitTarget.cardId}, position: {hitTarget.transform.position}");
                hitTarget.PullToPosition(new Vector2(hitTarget.transform.position.x, -5f), 0.5f);
            }
        }

    }
}