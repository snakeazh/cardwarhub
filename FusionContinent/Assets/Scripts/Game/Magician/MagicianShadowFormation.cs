using System;
using System.Collections.Generic;
using DG.Tweening;
using FusionContinent;
using FusionContinent.Skill;
using UnityEngine;

namespace FusionContinent
{
    public class MagicianShadowFormation : MonoBehaviour
    {
        private ISkill _skillCard;
        private Dictionary<string, float> _hitTargets = new Dictionary<string, float>();
        private float _hitInterval = 1f;
        public CircleCollider2D box_quan;// 一秒受击一次
        private bool _isAttractTogether = false;


        public void SetData(ISkill skillCard, bool isAttractTogether = false)
        {
            this._skillCard = skillCard;
            this._isAttractTogether = isAttractTogether;
        }

        public void StopRuntime()
        {
            enabled = false;
            _hitTargets.Clear();
            gameObject.SetActive(false);
        }

        private void Update()
        {
            this.GetComponent<CircleCollider2D>().radius=box_quan.radius;
        }

        private void Start()
        {
            DOTween.To(v => { }, 0, 0, 3f).onComplete += () =>
            {
                Destroy(this.gameObject);
            };
        }

        private void OnTriggerStay2D(Collider2D collision)
        {
            // 如果已经触发过，直接返回
            if (collision.name != "HitBox")
            {
                return;
            }

            MonsterCard hitTarget = collision.transform.parent.GetComponent<MonsterCard>();
            if (hitTarget != null && hitTarget.enabled != false)
            {
                _hitTargets.TryAdd(hitTarget.uniqueId, 0);
                _hitTargets[hitTarget.uniqueId] += Time.deltaTime;
                
                if (_hitTargets[hitTarget.uniqueId] >= _hitInterval)
                {
                    _hitTargets[hitTarget.uniqueId] = 0f;
                    
                    // 受伤坐标位置
                    var damageHitPos = collision.bounds.ClosestPoint(transform.position);
                    hitTarget.ReceiveDamage(MonsterDamageRequest.ForSkillSumTypes(hitTarget, _skillCard, damageHitPos));
                    // LogTool.Log(">>>> 攻击 - _cardId: "+hitTarget._cardId+", damage: "+damage+", baseAtk: "+ baseAtk);
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_isAttractTogether) return;
            
            // 如果已经触发过，直接返回
            if (other.name != "HitBox")
            {
                return;
            }
            MonsterCard hitTarget = other.transform.parent.GetComponent<MonsterCard>();
            if (hitTarget != null && hitTarget.enabled != false)
            {
                hitTarget.PullToPosition(this.transform.position, 2);
            }
        }
        
    }
}