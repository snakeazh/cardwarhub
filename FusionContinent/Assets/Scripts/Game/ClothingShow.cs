using System;
using System.Collections;
using System.Collections.Generic;
using Config;
using FusionContinent;
using FusionContinent.Animation;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ClothingShow : MonoBehaviour
{
    public RectTransform CloseAnimation;
    public RectTransform OpenAnimation;
    public TMP_Text clothingName;
    public TMP_Text desc;
    public Button clothUpBtn;
    public Button quitBtn;
    public Image icon;
    public Action<int> clothingConfigFunc;
    
    void Start()
    {
        quitBtn.onClick.AddListener(() =>
        {
            UIAnimationUtils.PlayAnimations(transform.Find("CloseAnimation"), () =>
            {
                gameObject.SetActive(false);
            });
        });
    }

    public void SetClothing(PlayerFigureConfig playerFigure,bool isCurrentlyWorn)
    {
        this.gameObject.SetActive(true);
        UIAnimationUtils.PlayAnimations(transform.Find("OpenAnimation"));
        // desc.text = playerFigure.Desc;
        clothingName.text = playerFigure.Name;
        // icon.sprite = Global.PlayerFigureIconSpriteAtlas.GetSprite(playerFigure.Icon);
        
        // 更新按钮文本和监听器
        clothUpBtn.onClick.RemoveAllListeners();
        clothUpBtn.transform.GetChild(0).GetComponent<TMP_Text>().text = isCurrentlyWorn ? "卸下" : "穿戴";
        
        clothUpBtn.onClick.AddListener(() =>
        {
            clothingConfigFunc?.Invoke(playerFigure.Id);
            this.gameObject.SetActive(false);
        });
    }
    
    
}