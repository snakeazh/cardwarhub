using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Config;
using FusionContinent;
using TMPro;
using UnityEngine;

public class SoliderAttribute : MonoBehaviour
{
    public GameObject Attack;
    public GameObject AttackSpeed;
    public GameObject Hp;
    public GameObject AttackRange;
    public GameObject Angle;
    public GameObject Intelligence;
    public GameObject Agile;

    private TbCardDevelopConfig _cardDevelopConfig;
    private TbSoldierBattleConfig _soldierBattleConfig;
    // Start is called before the first frame update
    private void Awake()
    {
        _cardDevelopConfig=DataConfigManager.Tables.TbCardDevelopConfig;
        _soldierBattleConfig=DataConfigManager.Tables.TbSoldierBattleConfig;
    }

    void Start()
    {
        
    }

    public void SetAttributes( int cardId,int level)
    {
        
    }
    
    public void SetDimensions(int cardId, int level)
    {
        var levelPropertyList = _cardDevelopConfig.GetOrDefault(cardId).LevelPropertyList;
        int nextlevel = level+1;
        // 直接检查是否存在指定 level
        if (levelPropertyList?.Any(x => x.Level == nextlevel) == true)
        {
            // 存在对应 level 的配置
            var levelConfig = levelPropertyList.First(x => x.Level == nextlevel);
            // 进行后续操作
            Angle.gameObject.SetActive(true);
            Intelligence.gameObject.SetActive(true);
            Agile.gameObject.SetActive(true);
            Angle.transform.Find("value").GetComponent<TMP_Text>().text =
                levelConfig.UpGradeEffectsDesc[0].Split("+")[1].Split(",")[0];
            Intelligence.transform.Find("value").GetComponent<TMP_Text>().text =
                levelConfig.UpGradeEffectsDesc[0].Split("+")[2].Split(",")[0];
            Agile.transform.Find("value").GetComponent<TMP_Text>().text =
                levelConfig.UpGradeEffectsDesc[0].Split("+")[3];
        }
        else
        {
            Angle.gameObject.SetActive(false);
            Intelligence.gameObject.SetActive(false);
            Agile.gameObject.SetActive(false);
            // 不存在对应 level 的配置
        }
    }
    
}
