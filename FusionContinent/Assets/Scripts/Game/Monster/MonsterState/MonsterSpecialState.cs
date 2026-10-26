using System.Collections;
using System.Collections.Generic;
using FusionContinent;
using Game.StateMachine;
using UnityEngine;

//怪物进入特殊状态
public class MonsterSpecialState : State
{
    // Start is called before the first frame update
    public MonsterSpecialState(StateMachine sm, MonsterCard monster) : base(sm, monster)
    {
    }

    public override void Enter()
    {
        switch (monster.monsterData.monsterControlState)
        {
            case MonsterControlState.Pull:
                monster.monsterData.monsterTweens.PullToPosition(monster.monsterData.pullinfo);      
                break;
            case MonsterControlState.KnockUp:
                monster.monsterData.monsterTweens.KnockUpMonster(monster.monsterData.knockupinfo);
                break;
        }

        monster.monsterData.isMoveEngaged = false;

    }

    public override void Exit()
    {
        
    }

}
