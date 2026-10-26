using System.Collections;
using System.Collections.Generic;
using Config;
using FusionContinent;
using FusionContinent.Attribute;
using UnityEngine;

public class BattleAttributeBuilder
{
    public static void BuildSolider(SoldierCard soldierCard,int id, int cardLevel, SoldierRoleType roleType = 0)
    {
        soldierCard.BaseAttributes.InitFromConfig(id,cardLevel);

        soldierCard.attackRange = soldierCard.soldierBattle.AttackRange > 0 ? soldierCard.soldierBattle.AttackRange : 1.0f;
        soldierCard.attackRange += soldierCard.BaseAttributes.GetFinal(AttributeType.SoldierAttackRange);
    }

    public static void BuildMonster()
    {
        
    }
}
