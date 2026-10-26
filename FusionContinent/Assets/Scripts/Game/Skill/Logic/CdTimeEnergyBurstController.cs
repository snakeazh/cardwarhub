using System;
using System.Collections;
using System.Collections.Generic;
using Config;
using DG.Tweening;
using FusionContinent;
using FusionContinent.Skill;
using FusionContinent.Utils;
using UnityEngine;
using Random = UnityEngine.Random;

/**
 * 能量迸发控制器
 */
public class CdTimeEnergyBurstController : MonoBehaviour
{
    public QFramework.GenericObjectPool Pool;
    
    private ISkill _skillCard;
    private float _targetDuration = 0f;
    private float _targetDurationTime = 0f;


    /*
     * 开始设置
     **/
    public void SetStart(ISkill skillCard, float targetDuration = 1f)
    {
        if (GamePlayManager.Default == null) return;

        _skillCard = skillCard;
        _targetDuration = targetDuration;
        
        // 设置无cd限制
        GamePlayManager.Default.UnlimitedCdTime();
    }

    private void Update()
    {
        if (GamePlayManager.Default == null || GamePlayManager.Default.IsPaused()) 
            return;
        
        // 特效生命周期
        if (_targetDuration > 0f)
        {
            _targetDurationTime += Time.deltaTime;
            if (_targetDurationTime >= _targetDuration)
            {
                // 关闭无CDTime状态
                Global.UnlimitedCdTime.Value = false;
                _targetDurationTime = 0f;
                Pool?.Return(gameObject);
                
                // 通知主城技能进行重置cd时间
                // GameLandUI.Default.SetCardCoolingById(_skillCard.Id);
            }
        }
    }
    

    private void OnDisable()
    {
        if (_targetDuration <= 0f) return;

        Global.UnlimitedCdTime.Value = false;
        _targetDuration = 0f;
        _targetDurationTime = 0f;
    }
}