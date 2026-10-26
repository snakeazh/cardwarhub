using System.Collections;
using System.Collections.Generic;
using Config;
using FusionContinent;
using FusionContinent.Model;
using QFramework;
using ScrollView;
using UnityEngine;

public class ChooseSkillPanel : MonoBehaviour,IChoosePanel
{
    private TbCardBaseConfig _cardBaseConfig;
    CardGroupPanelNew _cardGroupPanel;
    public UIVerticalScrollViewLock ScrollViewLock;
    CardType currentCardType;
    public List<PlayerCard> allPlayerCards;
    public float PositionY
    {
        get => 400;
    }

    public bool Status
    {
        get => status;
    }
    private bool status;
    public void OnInit(CardGroupPanelNew cardGroupPanel, CardType cardType)
    {
        this.gameObject.transform.LocalPositionX(9999);
        allPlayerCards =  new List<PlayerCard>();
        _cardBaseConfig = DataConfigManager.Tables.TbCardBaseConfig;
        currentCardType = cardType;
        _specialEffectsConfig = DataConfigManager.Tables.TbSpecialEffectsConfig;

        _cardGroupPanel =  cardGroupPanel;
        foreach (var playerCardsValue in Global.PlayerCards.Values)
        {
            if (playerCardsValue.Config.Type == cardType)
            {
                allPlayerCards.Add(playerCardsValue);
            }
        }
        status = false;
        ScrollViewLock.SetUpdateFunc(OnUpdatePlayerCard);
    }
    private TbSpecialEffectsConfig _specialEffectsConfig;
    private void OnUpdatePlayerCard(int dataIndex, ScrollItem scrollItem)
    {
        var cardSlot = scrollItem.TweenTarget;
        scrollItem.OnSpawn();
        var cardId = FilterList[dataIndex];
        
        var cardBaseCfg =  _cardBaseConfig.Get(cardId);
        // cardData.CardTran.Find("Empty").gameObject.SetActive(cardCfg == null);
        var skillCfg =_specialEffectsConfig.Get(cardId);

        var playerCard = allPlayerCards.Find(ele => ele.CardId == cardId);
        var cardSlotData = new CardSlotData()
        {
            CardId = cardId,
            NameText = cardBaseCfg.Name,
            LevelText = $"{playerCard.Lv}",
            QualityType = skillCfg.LevelPropertyList[playerCard.Lv- 1].Quality,
            Desc = cardBaseCfg.Desc,
            State = cardBaseCfg.State,
        };

        cardSlotData.Icon = Global.CardIconSpriteAtlas.GetSpriteExt(cardBaseCfg.Icon); 
        // cardData.CardTran.Find("Empty").gameObject.SetActive(cardCfg == null);
        if (cardBaseCfg != null)
        {
            cardSlot.GetComponent<CardSlot>().SetData(cardSlotData);
        }
        var bagItem = scrollItem as BagScrollItem;
        bagItem.OnClick = () =>
        {
            _cardGroupPanel.Deploy(currentCardType, cardId);
        };
    }

    public void Show()
    {
        this.gameObject.transform.LocalPositionX(0);
        allPlayerCards.Clear();
        foreach (var playerCardsValue in Global.PlayerCards.Values)
        {
            if (playerCardsValue.Config.Type == currentCardType)
            {
                allPlayerCards.Add(playerCardsValue);
            }
        }
        Filter();
        ScrollViewLock.ShowList(FilterList.Count);
        ScrollViewLock.ScrollToIndex(0,ScrollAlignment.Start,false);
        status = true;
    }

    public void Refresh()
    {
        Filter();
        ScrollViewLock.ShowList(FilterList.Count);
    }

    public List<int> FilterList = new List<int>();
    void Filter()
    {
        FilterList.Clear();
        foreach (var playerCard in allPlayerCards)
        {
            if (playerCard.IsHave && !(_cardGroupPanel.CheckCardInTeam(currentCardType, playerCard.CardId)>0) &&
                _cardGroupPanel.CheckCardSkillSoliderInTeam(playerCard.CardId)) 
            {
                FilterList.Add(playerCard.CardId);
            }
        }
        
    }
    public void Hide()
    {
        this.gameObject.transform.LocalPositionX(9999);
        status = false;
        
    }
}
