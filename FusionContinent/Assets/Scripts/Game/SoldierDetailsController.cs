using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Config;
using DG.Tweening;
using FusionContinent;
using FusionContinent.Attribute;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SoldierDetailsController : MonoBehaviour
{
    public static SoldierDetailsController Default;
    public float closeDelay = 5f; // 默认关闭延迟时间
    public Button closeBtn;
    private Tween closeTween; 
    public Button SoldierDetailsAllbtn;
    public GameObject SoldierDetailsAll;
    
    private SoldierCard _soldierCard;
    private TbCardBaseConfig _cardBaseConfig;
    private TbAttributeConfig _attributeConfig;
    private TbSoldierBattleConfig _soldierBattleConfig;
    // Start is called before the first frame update

    private void Awake()
    {
        _cardBaseConfig = DataConfigManager.Tables.TbCardBaseConfig;
        _soldierBattleConfig = DataConfigManager.Tables.TbSoldierBattleConfig;
        _attributeConfig = DataConfigManager.Tables.TbAttributeConfig;
        Default = this;
    }

    void Start()
    {
        SoldierDetailsAllbtn.onClick.AddListener(() =>
        {
            RefreshSoldierAttributesDetail();
            SoldierDetailsAll.SetActive(true);
            // 如果已经有 Tween 在运行，先杀掉它
            if (closeTween != null && closeTween.IsActive())
            {
                closeTween.Kill();
            }
        });
        closeBtn.onClick.AddListener(() =>
        {
            SoldierDetailsAll.SetActive(false);
        });
    }

    // Update is called once per frame
    void Update()
    {
    }

    public void InitSoldierDetails(SoldierCard soldierCard)
    {
        this._soldierCard = soldierCard;
        this.transform.localScale = Vector3.one;
        
        // 如果已经有 Tween 在运行，先杀掉它
        if (closeTween != null && closeTween.IsActive())
        {
            closeTween.Kill();
        }
        
        // 使用 DOTween 延迟关闭
        closeTween = DOVirtual.DelayedCall(closeDelay, () => 
        {
            this.transform.localScale = Vector3.zero;
            SoldierDetailsAll.SetActive(false);
        });

        var soldierCfg = _cardBaseConfig.GetOrDefault(soldierCard.cardId);
        transform.Find("SoldierIcon").GetComponent<Image>().sprite = Global.CardIconSpriteAtlas.GetSpriteExt(soldierCfg.Icon);
        SoldierDetailsAll.transform.Find("SoldierName").GetComponent<TMP_Text>().text =  soldierCfg.Name;
        SoldierDetailsAll.transform.Find("SoldierLevel").GetComponent<TMP_Text>().text = $"Lv.{soldierCard.level.Value}";
        
        // 基础属性
        var attributes = soldierCard.BaseAttributes;
        var attributeObj = transform.Find("Attribute");
        attributeObj.Find("Strength/Num").GetComponent<TMP_Text>().text = $"{attributes.GetMainAttributeValue(MainAttributeType.Strength)}";
        attributeObj.Find("Dexterity/Num").GetComponent<TMP_Text>().text = $"{attributes.GetMainAttributeValue(MainAttributeType.Dexterity)}";
        attributeObj.Find("Intelligence/Num").GetComponent<TMP_Text>().text = $"{attributes.GetMainAttributeValue(MainAttributeType.Intelligence)}";
    }

    private void RefreshSoldierAttributesDetail()
    {
        // 全部属性
        var attrParent = SoldierDetailsAll.transform.Find("AttributeList/Viewport/Content");
        var attrCard = attrParent.Find("Attribute").gameObject;

        var idx = 0;
        var attributes = _soldierCard.BaseAttributes.GetAll();
        foreach (var v in attributes)
        {
             Transform attrObj = idx >= attrParent.childCount 
                 ? Instantiate(attrCard, attrParent, true).transform 
                 : attrParent.GetChild(idx);
            
            attrObj.position = transform.position;
            attrObj.LocalScale(Vector3.one);
            attrObj.gameObject.SetActive(true);

            var attrCfg = _attributeConfig.GetOrDefault(v.Key);
            if (attrCfg == null) continue;

            attrObj.Find("Name").GetComponent<TMP_Text>().text = $"{attrCfg.Name}";
            attrObj.Find("Num").GetComponent<TMP_Text>().text = v.Value.GetValueDisplay();
            idx++;
        }
    }
}
