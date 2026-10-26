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
 * 风舞疾袭效果
 */
public class WindDanceAttackEffect : MonoBehaviour
{
    private ISkill _skillCard;
    
    // 记录已经命中过的怪
    private float _hitTargetDuration = 0f;
    private float _hitTargetDurationTime = 0f;

    private float _slashSpeed = 70f; // 光波飞行速度 (米/秒)
    private float _slashDistance = 100f; // 光波最大飞行距离
    
    /*
     * 开始设置
     **/
    public void SetStart(ISkill skillCard, float hitTargetDuration = 1f)
    {
        _skillCard = skillCard;
        _hitTargetDuration = hitTargetDuration;
        _hitTargetDurationTime = _hitTargetDuration;// 立即生效一次
    }
    
    private void Update()
    {
        // 游戏暂停不执行
        if (GamePlayManager.Default == null || GamePlayManager.Default.IsPaused())
        {
            return;
        }

        // 命中过的列表延时重置
        if (_hitTargetDuration > 0f)
        {
            _hitTargetDurationTime += Time.deltaTime;
            if (_hitTargetDurationTime >= _hitTargetDuration)
            {
                _hitTargetDurationTime = 0f;
                PlayEffectWindDance();
            }
        }
    }

    private void PlayEffectWindDance()
    {
        if (_skillCard.Config.SoundId > 0) SoundManager.Instance.PlayVoiceInGame(_skillCard.Config.SoundId);
        
        var pool = ObjectPoolManager.Instance.GetGameObjectPoolOrCreate($"{_skillCard.Config.ProjectileName}02");
        if (pool == null) return;

        GameObject bullet = pool.Get();
        if (bullet == null) return;
        
        // 获取随机位置
        var slotTran = GameLand.Default.FriendlySlot.GetChild(Random.Range(0, GameLand.Default.FriendlySlot.childCount));
        var slotTranPos = slotTran.position;
            
        // 子弹设置
        bullet.transform.SetParent(GameLand.Default.Bullet);
        bullet.transform.localScale = Vector3.one * 130f;
        bullet.transform.position = new Vector3(slotTranPos.x, slotTranPos.y - 0.1f, slotTranPos.z);
        
        // 撞击脚本
        float repelMonster = (int)_skillCard.Attributes.GetFinal(AttributeType.SkillRepelMonster);
        var bulletScript = bullet.transform.Find("ani_tianfu_gongsu_02/box");
        if (bulletScript != null)
        {
            bulletScript.GetComponent<DrawSlashBullet>().repelMonsterVal = repelMonster;
            bulletScript.GetComponent<DrawSlashBullet>().SetStartAttack(_skillCard);
        }
        
        // 技能持续时间
        DOVirtual.DelayedCall(3f, () => pool.Return(bullet)).SetUpdate(true);
    }

   

    public void StopRuntime()
    {
        enabled = false;
        _hitTargetDuration = 0f;
        _hitTargetDurationTime = 0f;
        gameObject.SetActive(false);
    }

    private void OnDisable()
    {
    }
}