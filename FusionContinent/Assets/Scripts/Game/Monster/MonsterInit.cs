using System.Collections;
using System.Collections.Generic;
using Config;
using FusionContinent;
using Spine.Unity;
using UnityEngine;

public class MonsterInit 
{
    public static void Init(MonsterCard m)
    {
        m.monsterData.monsterType = m._monsterConfig.MonsterQuality;
        m.monsterData.monsterAbnormal = m.GetComponent<MonsterAbnormal>();
        m.monsterData.spineTransform = m.AnimationController.SpineRoot;
        m.monsterData.skeletonAnim = m.AnimationController.SkeletonAnimation;
        m.monsterData.monsterState = MonsterState.idle;
        m.monsterData.monsterControlState = MonsterControlState.none;
        
        //韧性
        if (m.monsterData.monsterType != MonsterQuality.Ordinary)
        {
            m.monsterData.MaxResilience = m.Attributes.GetFinal(AttributeType.MonsterResilienceValue);
            m.monsterData.CurrentResilience = m.monsterData.MaxResilience;
            m.monsterData.WeaknessDuration = m.Attributes.GetFinal(AttributeType.MonsterWeaknessDuration);
            m.monsterData.DamageBonus = m.Attributes.GetFinal(AttributeType.MonsterToughnessBreakDamageBonus);
        }
        else
        {
            m.monsterData.MaxResilience = 0;
            m.monsterData.CurrentResilience = 0;
            m.monsterData.WeaknessDuration = 0;
            m.monsterData.DamageBonus = 0;
        }

        m.monsterData.isRecoveringFromWeak = false;
        if (m.monsterData.monsterAbnormal != null)
            m.monsterData.monsterAbnormal.ResetAllAbnormalState();
        
        m.monsterData.monsterTweens = m.GetComponent<MonsterTweens>();
        m.monsterData.monsterTweens.InitTweens(m);
        m.monsterData.isMoveEngaged = false;
    }

    public static void MonsterCreate(Vector3 initPos = default)
    {
        
    }
}