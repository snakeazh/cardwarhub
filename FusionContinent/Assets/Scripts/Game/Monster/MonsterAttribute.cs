using System;
using Config;
using FusionContinent;
using FusionContinent.Skill;

public class MonsterAttribute
{
    public float Physical_Resistance;
    public float Mana_Resistance;
    public float Armor;
    public float Dodge;
    
    // Start is called before the first frame update

    public int TakeDamage(TakeDamageType damageType, int damage, MonsterCard monsterCard, ISkill skillCard)
    {
        Physical_Resistance = monsterCard.Attributes.GetFinal(AttributeType.MonsterPhysicalResistance);// 物抗
        Mana_Resistance = monsterCard.Attributes.GetFinal(AttributeType.MonsterMagicResistance);// 魔抗
        
        // 临时抗性
        var addTempPhysical = monsterCard.Attributes.GetFinal(AttributeType.MonsterTempPhysicalResistance);
        if (addTempPhysical != 0)
        {
            Physical_Resistance += addTempPhysical;
            monsterCard.Attributes.GetAttr(AttributeType.MonsterTempPhysicalResistance).Clear();
        }
        var addTempMagic = monsterCard.Attributes.GetFinal(AttributeType.MonsterTempMagicResistance);
        if (addTempMagic != 0)
        {
            Mana_Resistance += addTempMagic;
            monsterCard.Attributes.GetAttr(AttributeType.MonsterTempMagicResistance).Clear();
        }

        if (damageType == TakeDamageType.PhysicalDamage)
        {
            var addArmorPenetration = skillCard.Parent.BaseAttributes.GetFinal(AttributeType.SoldierArmorPenetration);
            var addTempArmorPenetration = skillCard.Parent.BaseAttributes.GetFinal(AttributeType.SoldierTempArmorPenetration);
            if (addTempArmorPenetration != 0)
            {
                addArmorPenetration += addTempArmorPenetration;
                monsterCard.Attributes.GetAttr(AttributeType.SoldierTempArmorPenetration).Clear();
            }

            var addArmorPenetrationPer =
                skillCard.Parent.BaseAttributes.GetFinal(AttributeType.SoldierArmorPenetrationPer);
            
            damage = UpdateDamage(damage, addArmorPenetrationPer, addArmorPenetration, 1);
        }

        if (damageType == TakeDamageType.MagicDamage)
        {
            var addMagicPenetration = skillCard.Parent.BaseAttributes.GetFinal(AttributeType.SoldierMagicPenetration);
            var addTempMagicPenetration = skillCard.Parent.BaseAttributes.GetFinal(AttributeType.SoldierTempMagicPenetration);
            if (addTempMagicPenetration != 0)
            {
                addMagicPenetration += addTempMagicPenetration;
                monsterCard.Attributes.GetAttr(AttributeType.SoldierTempMagicPenetration).Clear();
            }
            
            var addMagicPenetrationPer =
                skillCard.Parent.BaseAttributes.GetFinal(AttributeType.SoldierMagicPenetrationPer);

            damage = UpdateDamage(damage, addMagicPenetrationPer, addMagicPenetration, 0);
        }
        
        return damage;
    }

    public int TakeMainAttributeDamage(MainAttributeType type, int damage, MonsterCard monsterCard, ISkill skillCard)
    {
        var powerFactor = monsterCard.Attributes.GetFinal(AttributeType.MonsterPowerDamage);
        var angileFactor = monsterCard.Attributes.GetFinal(AttributeType.MonsterAgileDamage);
        var intelligenceFactor = monsterCard.Attributes.GetFinal(AttributeType.MonsterIntelligenceDamage);
        switch (type)
        {
            case MainAttributeType.Strength:
                damage = (int)(damage * (1 + powerFactor));
                break;
            case MainAttributeType.Dexterity:
                damage = (int)(damage * (1 + angileFactor));
                break;
            case MainAttributeType.Intelligence:
                damage = (int)(damage * (1 + intelligenceFactor));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
        return damage;
    }

    /// <summary>
    /// 根据怪物类型调整伤害，普通怪、精英怪、Boss怪分别对应不同的伤害调整属性
    /// </summary>
    /// <param name="type"></param>
    /// <param name="damage"></param>
    /// <param name="soldierCard"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public int TakeMonsterTypeDamage(MonsterQuality type, int damage, SoldierCard soldierCard)
    {
        var countFactor = 0.0f;
        switch (type)
        {
            case MonsterQuality.Ordinary:
                countFactor = soldierCard.BaseAttributes.GetAttr(AttributeType.DamageAdjustmentForRegularEnemies).BaseValue;
                break;
            case MonsterQuality.Elite:
                countFactor = soldierCard.BaseAttributes.GetAttr(AttributeType.CorrectionOfDamageSufferedByEliteEnemies).BaseValue;
                break;
            case MonsterQuality.Boss:
                countFactor = soldierCard.BaseAttributes.GetAttr(AttributeType.CorrectionOfDamageSufferedByTheLeaderEnemies).BaseValue;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
        damage = (int)(damage * (1 + countFactor));
        return damage;
    }

    public int UpdateDamage(int damage, float perValue, float value, int attackId)
    {
        int newDamage = damage;
        float newResistance = attackId == 1 ? Physical_Resistance : Mana_Resistance;
        newResistance *= (1- perValue);
        newResistance -= value;
        
        newDamage = (int)(newDamage * (500 / ((newResistance < 0 ? 0 : newResistance) + 500)));
        return newDamage;
    }
    
}
