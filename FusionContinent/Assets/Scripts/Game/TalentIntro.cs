using System;
using System.Collections;
using System.Collections.Generic;
using Config;
using FusionContinent;
using FusionContinent.Attribute;
using FusionContinent.Model;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TalentIntro : MonoBehaviour
{
    public Button quitbtn;

    public Transform talentIntroParent;
    
    private TbAttributeConfig _attributeConfig;
    // Start is called before the first frame update
    private void Awake()
    {
        _attributeConfig=DataConfigManager.Tables.TbAttributeConfig;
    }

    void Start()
    {
        quitbtn.onClick.AddListener(() =>
        {
            this.gameObject.SetActive(false);
        });
    }

    private void OnDisable()
    {
        ObjectPoolManager.Instance.GetGameObjectPool("TalentIntroObj").ReturnAllEffect();
    }

    public void SetTalentIntor(int talentId)
    {
        var attributeAggregatList = Talent.GetAttributes(talentId).Attributes.GetAll(); // 获取天赋属性
        foreach (var attrAggregat in attributeAggregatList)
        {
            var talentintropool=ObjectPoolManager.Instance.GetGameObjectPool("TalentIntroObj");
            GameObject talentIntorObj = talentintropool.Get();
            if(talentIntorObj==null) continue;
            talentIntorObj.transform.SetParent(talentIntroParent);
            talentIntorObj.transform.localScale = Vector3.one;
            talentIntorObj.transform.localPosition = Vector3.zero;
           // LogTool.LogError(_attributeConfig.GetOrDefault(attrAggregat.Key).Name);
            talentIntorObj.transform.Find("TalentIntroObjtxt")
                .GetComponent<TMP_Text>().text = _attributeConfig.GetOrDefault(attrAggregat.Key).Name +":"+attrAggregat.Value.BaseValue;
        }

    }
}
