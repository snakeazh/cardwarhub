using System;
using System.Collections;
using System.Collections.Generic;
using Config;
using FusionContinent;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SoliderIntroducecontroller : MonoBehaviour
{
    public Image cardIcon;

    public TMP_Text cardName;
    
    public TMP_Text cardDesc;

    public Button QuitPaneal;
    
    private TbCardBaseConfig _cardBaseConfig;

    private void Start()
    {
        QuitPaneal.onClick.AddListener(()=>this.gameObject.SetActive(false));
    }

    // Start is called before the first frame update
    public void SetCard(int cardid)
    {
        _cardBaseConfig=DataConfigManager.Tables.TbCardBaseConfig;
        cardIcon.sprite=Global.CardIconSpriteAtlas.GetSpriteExt(_cardBaseConfig.GetOrDefault(cardid).Icon+"Big");
        cardName.text = _cardBaseConfig.GetOrDefault(cardid).Name;
        cardDesc.text = _cardBaseConfig.GetOrDefault(cardid).Desc;
        UIHelper.SafeRefreshLayout(cardDesc.transform.parent.GetComponent<RectTransform>() as RectTransform);
    }

    private void OnDisable()
    {
        this.gameObject.SetActive(false);
    }
}
