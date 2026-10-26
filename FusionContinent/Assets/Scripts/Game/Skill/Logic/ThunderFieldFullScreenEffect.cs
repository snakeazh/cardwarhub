using System;
using System.Collections;
using System.Collections.Generic;
using Config;
using DG.Tweening;
using FusionContinent;
using FusionContinent.Skill;
using FusionContinent.Utils;
using Manager;
using UnityEngine;
using Random = UnityEngine.Random;

/**
 * 雷霆领域效果
 */
public class ThunderFieldFullScreenEffect : MonoBehaviour
{
    private ISkill _skillCard;
    
    // 记录已经命中过的怪
    private readonly HashSet<MonsterCard> _hitTargets = new HashSet<MonsterCard>();
    private float _hitTargetDuration = 0f;
    private float _hitTargetDurationTime = 0f;

    // 受击显示
    private int _spawnHitEffectNum;
    public int spawnHitEffectMaxNum = 10;// 最大手机显示数量
    

    /*
     * 开始设置
     **/
    public void SetStart(ISkill skillCard, float hitTargetDuration = 1f)
    {
        _skillCard = skillCard;
        _hitTargets.Clear();
        _hitTargetDuration = hitTargetDuration;
        _spawnHitEffectNum = 0;
    }
    
    private void OnTriggerStay2D(Collider2D collision)
    {
        // 如果已经触发过，直接返回
        if (collision.name != "HitBox")
        {
            return;
        }

        MonsterCard hitTarget = collision.transform.parent.GetComponent<MonsterCard>();
        if (hitTarget == null || !hitTarget.enabled)
        {
            return;
        }
        
        // 已经命中过
        _hitTargets.Add(hitTarget);
    }

    private void Update()
    {
        if (GamePlayManager.Default == null || GamePlayManager.Default.IsPaused()) 
            return;
        
        // 命中过的列表延时重置
        if (_hitTargetDuration > 0f)
        {
            _hitTargetDurationTime += Time.deltaTime;
            if (_hitTargetDurationTime >= _hitTargetDuration && _hitTargets.Count > 0)
            {
                var targetV = RandomOne(_hitTargets);
                _hitTargets.Clear();
                if (!targetV.enabled || targetV.monsterData.monsterState == MonsterState.die || targetV.transform.position.y > 7.5f) return;
                
                PlayEffectThunder(targetV.transform.position);
                _hitTargetDurationTime = 0f;
            }
        }
    }
    
    private void PlayEffectThunder(Vector3 pos)
    {
        if (_skillCard.Config.SoundId > 0) SoundManager.Instance.PlayVoiceInGame(_skillCard.Config.SoundId);
        
        var pool = ObjectPoolManager.Instance.GetGameObjectPoolOrCreate("ThunderFieldFullScreenShoot");
        if (pool == null) return;

        GameObject bullet = pool.Get();
        if (bullet == null) return;
            
        // 子弹设置
        bullet.transform.SetParent(GameLand.Default.Bullet);
        bullet.transform.localScale = Vector3.one * 100f;
        bullet.transform.position = pos;
        bullet.transform.Find("ani_yx_jn1_08/box").GetComponent<ArrowBullet>().StartAttackSet(_skillCard, _skillCard.Attributes.GetFinal(AttributeType.SkillCriticalDamage), 2.5f);
        
        // 技能持续时间（忽略 pause/timeScale，避免暂停期间子弹永不回池）
        DOVirtual.DelayedCall(3f, () => { pool.Return(bullet); }).SetUpdate(true);
    }
    
    private T RandomOne<T>(HashSet<T> set)
    {
        if (set == null || set.Count == 0)
            return default;

        int index = Random.Range(0, set.Count);
        int i = 0;

        foreach (var item in set)
        {
            if (i == index)
                return item;
            i++;
        }

        return default;
    }

    public void StopRuntime()
    {
        enabled = false;
        _hitTargets.Clear();
        gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        // 清理集合，避免对象池复用后旧数据残留
        _hitTargets.Clear();
    }
}