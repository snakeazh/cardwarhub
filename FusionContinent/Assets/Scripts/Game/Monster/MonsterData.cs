using System.Collections;
using System.Collections.Generic;
using Config;
using Game.StateMachine;
using Spine.Unity;
using UnityEngine;

namespace FusionContinent
{

    public enum MonsterState
    {
        none = 0,
        idle = 1<< 0, //idle 
        move  = 1 << 1, //移动
        engage = 1<< 2, //索敌
        attack = 1<< 3, 
        hurt = 1<< 4,
        weak = 1<<5,   //虚弱
        die = 1<< 6,
        specialMove = 1<< 7,    //特殊移动状态 例如 击退 强行tween然后结束重新变为移动
    }

    
    public enum MonsterControlState
    {
        none=0,
        Pull = 1<<0, //击退
        KnockUp = 1 << 1,//击飞
        
    }

    public class MonsterData
    {
        #region 状态机信息
        // 添加状态机
        public StateMachine _stateMachine;
        
        // 预创建的状态实例（供CheckTransition使用）
        #endregion 状态机信息
        
        #region 这里先存一些新的,老的后面加
        public MonsterQuality monsterType; //怪物类型
        public Transform spineTransform; //"Spine/" + _monsterConfig.Spine
        public SkeletonAnimation skeletonAnim; //spineTransform.GetChild(0).GetComponent<SkeletonAnimation>();
        public MonsterAbnormal monsterAbnormal;
        public MonsterState monsterState;
        public  MonsterControlState monsterControlState;
        public float MaxResilience;   //韧性条 为0时候切换为虚弱状态
        public float CurrentResilience; //当前韧性值
        public float WeaknessDuration; //虚弱持续时间
        public float DamageBonus;      //虚弱状态下受伤加成
        public bool isRecoveringFromWeak; //虚弱恢复保护期，虚弱退出动画期间拒绝新的韧性扣减

        public MonsterTweens monsterTweens;
        public (Vector2, float) pullinfo;   //击退信息
        public (float,float,float,float) knockupinfo;   //击飞信息
        public bool isMoveEngaged = false;

        /// <summary>机制（如分裂）触发时抑制死亡掉落，由 Mechanic 写入。</summary>
        public bool SuppressDeathLoot;

        #endregion 这里先存一些新的,老的后面加



    }
}
