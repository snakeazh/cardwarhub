using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Config;
using DG.Tweening;
using FusionContinent;
using FusionContinent.Utils;
using UnityEngine;

public class MonsterEffect : MonoBehaviour
{
    private MonsterBattleConfig _monsterBattleConfig;

    public Dictionary<int, Action> ActionDict = new Dictionary<int, Action>();
    public Dictionary<int, bool> ActionbBools = new Dictionary<int, bool>();
    public Dictionary<int, float[]> Actionbeffectvalue = new Dictionary<int, float[]>();

    public bool NoFire;
    public float additionaldamage;

    public float dictionary;
    public int durationtimes;
    public int trigertimes;

    private void Start()
    {
        // DOVirtual.DelayedCall(1f, () =>
        // {
        //     // if (GetComponent<MonsterCard>()._cardId == 230002)
        //     // {
        //     //     _monsterBattleConfig = GetComponent<MonsterCard>()._monsterConfig;
        //     //     foreach (var v in _monsterBattleConfig.BuffEffect)
        //     //     {
        //     //         Actionbeffectvalue.Add(int.Parse(v.Type),v.Effects);
        //     //     }
        //     // }
        // });
    }

    public void Dictionary()
    {
        
    }

    private void OnEnable()
    {
        Actionbeffectvalue.Clear();
    }
/// <summary>
/// 受到攻击
/// </summary>
    public void TakeDamage()
    {
        if (Actionbeffectvalue.ContainsKey(810001))
        {
        }
    }
/// <summary>
/// 不能被拖拽
/// </summary>
    public void NopushMonster()
    {
        ActionbBools[820001] = true;
    }
/// <summary>
/// 恢复全场百分比血量
/// </summary>
    public void AddAllHealth()
    {
        if(ActionbBools[820002]==true) return;
        float value = Actionbeffectvalue[820002][0];
        // 获取所有 **启用** 的子物体（排除自己）
        MonsterCard[] MonsterCardchildren = transform.parent.GetComponentsInChildren<MonsterCard>()
            .Where(child =>
                child.gameObject.activeSelf && child.gameObject != this.gameObject) // 排除自己，且只选 active 的
            .ToArray();
        DOTween.To(v => { }, 0, 0, 0.5f).SetEase(Ease.Linear)
                .onComplete +=
            () =>
            {
                foreach (var a in MonsterCardchildren)
                {
                    if (a.hp + value <= a.maxHp)
                    {
                        a.hp += (int)value;
                    }
                    else
                    {
                        a.hp = a.maxHp;
                    }
                }
            };
    }
/// <summary>
/// 增加攻击速度
/// </summary>
    public void AddAtackSpeed()
    {
        if (ActionbBools[820003]==true) return;
        float value = Actionbeffectvalue[820003][0];
        GetComponent<MonsterCard>().monsterData.monsterTweens.AddAtkSpeed(value);
    }
/// <summary>
/// 增加移动速度
/// </summary>
    public void AddMoveSpeed()
    {
        if (ActionbBools[820004]==true) return;
        float value = Actionbeffectvalue[820004][0];
        GetComponent<MonsterCard>().monsterData.monsterTweens.AddMoveSpeed(value);
        GetComponent<Animator>().speed += value;
    }
/// <summary>
/// 增加战斗力
/// </summary>
    public void AttackAdd()
    {
        if (ActionbBools[820005]==true) return;
        float value = Actionbeffectvalue[820005][0];
        GetComponent<MonsterCard>().atk +=(int)value;
    }
/// <summary>
/// 毒液
/// </summary>
    public void Duye()
    {
        if (ActionbBools[820006]==true) return;
        float value = Actionbeffectvalue[820006][0];
        DOTween.To(v => { }, 0, 0, 1).SetEase(Ease.Linear).onComplete +=
            () =>
            {
                // for (int i = 0; i < GamePlayManager.Default.specialCardList.Count; i++)
                // {
                //     //GamePlayManager.Default.specialCardList[i].Hp -= value;
                // }
            };
    }
/// <summary>
/// 护甲降低
/// </summary>
    public void TakeHujia()
    {
        if (ActionbBools[820007]==true) return;
        float value = Actionbeffectvalue[820007][0];
        GetComponent<MonsterCard>().MonsterAttribute.Armor -= value;
    }
/// <summary>
/// 闪避率提升
/// </summary>
    public void AddDodge()
    {
        if (ActionbBools[820008]==true) return;
        float value = Actionbeffectvalue[820008][0];
        GetComponent<MonsterCard>().MonsterAttribute.Dodge += value;
    }
/// <summary>
/// 精灵攻击力提升
/// </summary>
    public void JingLingAddAtack()
    {
        // if (ActionbBools[820009]==true) return;
        // float value = Actionbeffectvalue[820009][0];
        // MonsterCard[] MonsterCardchildren = transform.parent.GetComponentsInChildren<MonsterCard>()
        //     .Where(child => child.gameObject.activeSelf && child.gameObject != this.gameObject &&
        //                     child._cardId == GetComponent<MonsterCard>()._cardId) // 排除自己，且只选 active 的
        //     .ToArray();
        // DOTween.To(v => { }, 0, 0, 0.5f).SetEase(Ease.Linear)
        //         .onComplete +=
        //     () =>
        //     {
        //         float atk = GetComponent<MonsterCard>().atk;
        //         foreach (var a in MonsterCardchildren)
        //         {
        //             a.atk = (int)atk + (int)value * MonsterCardchildren.Length;
        //         }
        //     };
    }
/// <summary>
/// 降低英雄攻击速度
/// </summary>
    public void TakeSubAttackSpeed()
    {
        // if (ActionbBools[820010]==true) return;
        // float value = Actionbeffectvalue[820010][0];
        // for (int i = 0; i < GamePlayManager.Default.specialCardList.Count; i++)
        // {
        //     GamePlayManager.Default.specialCardList[i].AttackInterval += value;
        // }
    }
/// <summary>
/// 不受火焰攻击
/// </summary>
    public void NoFireMonster()
    {
        if (ActionbBools[820011]==true) return;
        NoFire = true;
    }
/// <summary>
/// 受到额外的伤害
/// </summary>
    public void AdditionalDamage()
    {
        if (ActionbBools[820012]==true) return;
        float value = Actionbeffectvalue[820012][0];
        additionaldamage+=value;
    }
/// <summary>
/// 降低英雄攻击速度
/// </summary>
    public void TakeSubAttack()
    {
        // if (ActionbBools[820013]==true) return;
        // float value = Actionbeffectvalue[820013][0];
        // for (int i = 0; i < GamePlayManager.Default.specialCardList.Count; i++)
        // {
        //     GamePlayManager.Default.specialCardList[i].Attack -= (int)value;
        // }
    }
/// <summary>
/// 降低移动速度
/// </summary>
    public void SubMoveSpeed()
    {
        if (ActionbBools[820014]==true) return;
        float value = Actionbeffectvalue[820014][0];
        GetComponent<MonsterCard>().monsterData.monsterTweens.AddMoveSpeed(value);
        GetComponent<Animator>().speed -= value;
    }
}