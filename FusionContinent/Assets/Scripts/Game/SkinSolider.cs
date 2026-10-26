using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Config;
using FusionContinent;
using FusionContinent.Animation;
using FusionContinent.Helper;
using FusionContinent.Model;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkinSolider : MonoBehaviour
{
    public Image icon;
    public Button closeButton;
    public Button bg;
    public TMP_Text titleText;
    public RectTransform AttributeScrollContent;

    public Button ChooeseBtn;

    public Transform SelectBtnParent;
    private TbSkinConfig _skinConfig;
    public GameObject selcetImage;

    private int skinId;
    private int cardId;
    private int globalSkinId;

    public Action<int> SelectSkinAction;

    // Start is called before the first frame update
    private void Awake()
    {
        _skinConfig = DataConfigManager.Tables.TbSkinConfig;
    }

    void Start()
    {
        closeButton.onClick.AddListener(() =>
        {
            UIAnimationUtils.PlayAnimations(transform.Find("CloseAnimation"), () =>
            {
                ObjectPoolManager.Instance.GetGameObjectPool("SelectSkinBtnobj").ReturnAllEffect();
                this.gameObject.SetActive(false);
            });
        });
        bg.onClick.AddListener(() =>
        {
            UIAnimationUtils.PlayAnimations(transform.Find("CloseAnimation"), () =>
            {
                ObjectPoolManager.Instance.GetGameObjectPool("SelectSkinBtnobj").ReturnAllEffect();
                this.gameObject.SetActive(false);
            });
        });
    }

    private void OnDisable()
    {
        ObjectPoolManager.Instance.GetGameObjectPool("SelectSkinBtnobj").ReturnAllEffect();
    }

    public void SetSkin(int cardId)
    {
        this.gameObject.SetActive(true);
        UIAnimationUtils.PlayAnimations(transform.Find("OpenAnimation"));
        this.cardId = cardId;
        List<SkinConfig> skinConfigs = _skinConfig.DataList.Where(t => t.ActsOnCard == cardId).ToList();
        if (skinConfigs.Count == 0)
        {
            this.gameObject.SetActive(false);
            FusionContinent.Toast.Show("暂无皮肤");
            return;
        }
        
        // 保证字典有这个 key
        if (!Global.PlayerCards.ContainsKey(cardId) || Global.PlayerCards[cardId] == null)
        {
            Global.PlayerCards[cardId] = new PlayerCard(); // 这里用你实际的类名替换
        }

        // 保证 ObjData 不为空
        if (Global.PlayerCards[cardId].ObjData == null)
        {
            Global.PlayerCards[cardId].ObjData = new CardDataStruct(); // 用你的数据类替换
        }

        globalSkinId = Global.PlayerCards[cardId].ObjData.Skin;

        for (int j = 0; j < skinConfigs.Count; j++)
        {
            var skinCfg = skinConfigs[j];
            
            // var selectBtnObjpool = ObjectPoolManager.Instance.GetGameObjectPool("SelectSkinBtnobj");
            // GameObject selectBtnObj = selectBtnObjpool.Get();
            // if (selectBtnObj == null) return;
            // selectBtnObj.transform.SetParent(SelectBtnParent);
            
            var selectBtnObj = j >= SelectBtnParent.childCount
                ? Instantiate(SelectBtnParent.GetChild(0).gameObject, SelectBtnParent)
                : SelectBtnParent.GetChild(j).gameObject;
            selectBtnObj.SetActive(true);
            selectBtnObj.transform.localScale = Vector3.one;
            selectBtnObj.GetComponent<SelectSkinBtnobj>().skinId = skinCfg.Id;
            var selectSkinScript = selectBtnObj.GetComponent<SelectSkinBtnobj>();
            selectSkinScript.nameText.text = skinCfg.Name;
            
            Button selectBtn = selectSkinScript.selectBtn;
            SkinConfig config = _skinConfig.GetOrDefault(selectBtnObj.GetComponent<SelectSkinBtnobj>().skinId);
            // selectBtn.GetComponent<Image>().sprite = Global.CardIconSpriteAtlas.GetSpriteExt(config.Icon + "Select");
            SpriteHelper.TryLoadTextureAsync(config.Icon + "Select", selectBtn.GetComponent<Image>(),false);
            selectBtn.onClick.RemoveAllListeners();
            selectBtn.onClick.AddListener(() =>
            {
                SkinConfig selectCfg = _skinConfig.GetOrDefault(selectBtnObj.GetComponent<SelectSkinBtnobj>().skinId);
                //icon.sprite = ResLoader.GetSprite(configselect.Icon);
                
                // 属性
                for (int i = 0; i < AttributeScrollContent.childCount; i++)
                    AttributeScrollContent.GetChild(i).gameObject.SetActive(false);
                
                if (selectCfg.Attributes.Count == 0)
                {
                    var defObj = AttributeScrollContent.GetChild(0).gameObject;
                    defObj.SetActive(true);
                    // defObj.transform.Find()
                    defObj.transform.Find("Star").gameObject.SetActive(false);
                    defObj.transform.Find("Text").GetComponent<LocalizationaAutoWidthText>()
                        .SetData(LocalizationModule.UITexts, "UI_ATTR_NOT_BONUS");
                }
                else
                {
                    for (int i = 0; i < selectCfg.Attributes.Count; i++)
                    {
                        var attrV = selectCfg.Attributes[i];
                        var attrCfg = DataConfigManager.Tables.TbAttributeConfig.GetOrDefault(attrV.Type);
                        if (attrCfg == null)
                        {
                            Debug.LogWarning($"[SkinSolider] 属性配置未找到: Type={attrV.Type}, SkinId={selectCfg.Id}");
                            continue;
                        }

                        var attrObj = i >= AttributeScrollContent.childCount
                            ? Instantiate(AttributeScrollContent.GetChild(0).gameObject, AttributeScrollContent)
                            : AttributeScrollContent.GetChild(i).gameObject;

                        attrObj.SetActive(true);
                        var symbolStr = attrV.Value > 0 ? "+" : "";
                        attrObj.transform.Find("Star").gameObject.SetActive(true);
                        attrObj.transform.Find("Text").GetComponent<LocalizationaAutoWidthText>()
                            .SetData(LocalizationModule.UITexts, attrCfg.Name, $"{symbolStr}{attrV.Value}");
                    }
                }

                SpriteHelper.TryLoadTextureAsync(selectCfg.Icon, icon,true);
                icon.gameObject.transform.localScale =
                    _skinConfig.GetOrDefault(selectBtnObj.GetComponent<SelectSkinBtnobj>().skinId).IllustrationScale *
                    Vector3.one;
                selcetImage.transform.SetParent(selectBtnObj.transform);
                selcetImage.transform.localScale = Vector3.one;
                selcetImage.transform.GetComponent<RectTransform>().anchoredPosition =
                    new Vector2(-98.491f, -57.6122f);
                titleText.text=selectCfg.Name;
                if (selectCfg.UnlockItemId.HasValue && 
                    Global.GetItemNumById(selectCfg.UnlockItemId.Value) == 0 && 
                    globalSkinId != 0 &&
                    selectCfg.Id != skinConfigs[0].Id)
                {
                    // 安全地获取子对象的Text组件
                    Transform childTransform = ChooeseBtn.transform.GetChild(0);
                    if (childTransform != null)
                    {
                        TMP_Text textComponent = childTransform.GetComponent<TMP_Text>();
                        if (textComponent != null)
                        {
                            textComponent.text = "暂未解锁";
                        }
                    }
                    ChooeseBtn.interactable = false;
                    return;
                }

                ChooeseBtn.interactable = true;
                ChooeseBtn.transform.GetChild(0).GetComponent<TMP_Text>().text = "穿戴";
                this.skinId = selectCfg.Id;
                if (skinId == globalSkinId)
                {
                    ChooeseBtn.transform.GetChild(0).GetComponent<TMP_Text>().text = "已穿戴";
                    ChooeseBtn.interactable = false;
                }

                SetChooseBtn();
            });
        }
        
        if (globalSkinId == 0)
        {
            if(skinConfigs.Count<=0) return;
            skinId = skinConfigs[0].Id;
            Global.PlayerCards[cardId].SetSkin(skinId, () =>
            {
                globalSkinId = Global.PlayerCards[cardId].ObjData.Skin;
                SelectBtnParent.GetChild(1).GetComponent<SelectSkinBtnobj>().selectBtn.onClick.Invoke();
            });
            return;
        }

        for (int i = 0; i < SelectBtnParent.childCount; i++)
        {
            if (SelectBtnParent.GetChild(i).GetComponent<SelectSkinBtnobj>().skinId == globalSkinId)
            {
                SelectBtnParent.GetChild(i).GetComponent<SelectSkinBtnobj>().selectBtn.onClick.Invoke();
            }
        }

    }

    private void SetChooseBtn()
    {
        ChooeseBtn.onClick.RemoveAllListeners();
        ChooeseBtn.onClick.AddListener(() =>
        {
            Global.PlayerCards[cardId].SetSkin(skinId, () =>
            {
                ChooeseBtn.transform.GetChild(0).GetComponent<TMP_Text>().text = "已穿戴";
                globalSkinId = Global.PlayerCards[cardId].ObjData.Skin;
                SelectSkinAction?.Invoke(skinId);
            });
        });
    }
}