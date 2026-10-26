using System;
using System.Collections;
using System.Collections.Generic;
using Config;
using DG.Tweening;
using FusionContinent;
using FusionContinent.Skill;
using UnityEngine;

public class DrawSlashBullet : MonoBehaviour
{
    private bool _isKnockUp;
    private ISkill _skillCard;

    // 记录已经命中过的怪
    private readonly HashSet<string> _hitTargets = new HashSet<string>();
    private float _hitTargetDuration = 0f;
    private float _hitTargetDurationTime = 0f;

    // 受击显示
    private int _spawnHitEffectNum;
    public int spawnHitEffectMaxNum = 10;// 最大手机显示数量
    
    // 受击效果
    public float knockUpVal;// 击飞高度
    public float repelMonsterVal;// 击退值

    /*
     * 开始攻击设置
     * 
     * @param skillCard
     * @param hitTargetDuration 攻击目标保留时长
     * @return null
     * @author Zpp
     * @create 2025/10/24
     **/
    public void SetStartAttack(ISkill skillCard, float hitTargetDuration = 0f)
    {
        _hitTargets.Clear();
        _skillCard = skillCard;
        _hitTargetDuration = hitTargetDuration;
        _spawnHitEffectNum = 0;
    }
    
    private void OnTriggerStay2D(Collider2D collision)
    {
        // 如果已经触发过，直接返回
        if (_skillCard == null || collision.name != "HitBox")
        {
            return;
        }

        MonsterCard hitTarget = collision.transform.parent.GetComponent<MonsterCard>();
        if (hitTarget == null || !hitTarget.enabled)
        {
            return;
        }
        
        // 检查是否已经命中过
        if (!_hitTargets.Add(hitTarget.uniqueId)) return;
        
        // 伤害
        hitTarget.ReceiveDamage(MonsterDamageRequest.ForSkillSumTypes(
            hitTarget, _skillCard, collision.bounds.ClosestPoint(transform.position)));
        
        // 击飞效果
        if (knockUpVal > 0f)
        {
            hitTarget.KnockUpMonster(knockUpVal);
        }
        
        // 击退效果
        if (repelMonsterVal != 0f)
        {
            hitTarget.RepelMonster(repelMonsterVal);
        }

        // 特效
        SpawnHitEffect(hitTarget.transform.position);
    }

    private void Update()
    {
        // 命中过的列表延时重置
        if (_hitTargetDuration > 0f)
        {
            _hitTargetDurationTime += Time.deltaTime;
            if (_hitTargetDurationTime >= _hitTargetDuration)
            {
                _hitTargets.Clear();
                _hitTargetDurationTime = 0f;
            }
        }
    }

    /**
     * 受击表现
     */
    private void SpawnHitEffect(Vector3 position)
    {
        string hitEffect = _skillCard.Config.SkillHitEffect ?? "";
        if (hitEffect == "" || _spawnHitEffectNum > spawnHitEffectMaxNum) return;
        _spawnHitEffectNum++;
        
        QFramework.GenericObjectPool pool = ObjectPoolManager.Instance.GetGameObjectPoolOrCreate(hitEffect);
        if (pool == null) return;

        var effect = pool.Get();
        if (effect == null) return;

        effect.transform.SetParent(GameLand.Default.DamageParent.transform);
        effect.transform.position = position;
        effect.transform.localScale = Vector3.one * 200;

        StartCoroutine(DisableAfterDelay(pool, effect, 0.5f));
    }

    private IEnumerator DisableAfterDelay(QFramework.GenericObjectPool pool, GameObject effect, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (effect != null)
        {
            pool.Return(effect);
        }
    }

    private void OnDisable()
    {
        // 清理集合，避免对象池复用后旧数据残留
        _hitTargets.Clear();
    }
}
