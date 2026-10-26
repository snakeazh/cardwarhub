using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Localization;
using Config;
using FusionContinent;
using FusionContinent.Model;
using QFramework;
using ScrollView;
using TMPro;
using UnityEngine;

public class ChooseGeneralSkillPanel : MonoBehaviour,IChoosePanel
{
    public UIHorizontalScrollViewLock HorizontalScrollViewLock;
    private TbCardBaseConfig _cardBaseConfig;
    CardGroupPanelNew _cardGroupPanel;
    public UIVerticalScrollViewLock ScrollViewLock;
    CardType currentCardType;
    public List<PlayerCard> allPlayerCards;
    private List<string> _qualityNameList;
    public float PositionY
    {
        get => 1220;
    }

    public bool Status
    {
        get => status;
    }
    private readonly List<QualityType> _filterGeneralQuality = new List<QualityType>();// 筛选通用技能卡品质
    private QualityType _selectedQuality = QualityType.Default;

    private bool status;
    public void OnInit(CardGroupPanelNew cardGroupPanel, CardType cardType)
    {
        this.gameObject.transform.LocalPositionX(9999);
        _qualityNameList = DataConfigManager.Tables.TbGameBattleConst.QualityTypeListKey;
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
        _filterGeneralQuality.Clear();
        _filterGeneralQuality.Add(QualityType.Ordinary);
        _filterGeneralQuality.Add(QualityType.Rarity);
        _filterGeneralQuality.Add(QualityType.Epic);
        _filterGeneralQuality.Add(QualityType.Legend);
       
        status = false;
        ScrollViewLock.SetUpdateFunc(OnUpdatePlayerCard);
        
        HorizontalScrollViewLock.SetUpdateFunc(OnUpdateItemQuality);
    }

    private void OnUpdateItemQuality(int dataIndex, ScrollItem scrollItem)
    {
        var item = scrollItem.TweenTarget;
        item.Find("Enable/QualityName").GetComponent<LocalizationText>()
            .SetData(LocalizationModule.UITexts, _qualityNameList[dataIndex + 1]);
        item.Find("UnEnable/QualityName").GetComponent<LocalizationText>()
            .SetData(LocalizationModule.UITexts, _qualityNameList[dataIndex + 1]);
        var bagItem = scrollItem as BagScrollItem;
        item.Find("Enable").gameObject.SetActive(_selectedQuality == this._filterGeneralQuality[dataIndex]);
        item.Find("UnEnable").gameObject.SetActive(_selectedQuality != this._filterGeneralQuality[dataIndex]);

        bagItem.OnClick = () =>
        {
            _selectedQuality = this._filterGeneralQuality[dataIndex];
            Refresh();
        };
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

        cardSlotData.IconName = cardBaseCfg.Icon;
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
        _selectedQuality = _filterGeneralQuality.First();
        Filter();
        ScrollViewLock.ShowList(FilterList.Count);
        ScrollViewLock.ScrollToIndex(0,ScrollAlignment.Start,false);
        HorizontalScrollViewLock.ShowList(this._filterGeneralQuality.Count);
        status = true;
    }

    public void Refresh()
    {
        Filter();
        ScrollViewLock.ShowList(FilterList.Count);
        HorizontalScrollViewLock.ShowList(this._filterGeneralQuality.Count);
    }

    public List<int> FilterList = new List<int>();
    void Filter()
    {
        FilterList.Clear();
        foreach (var playerCard in allPlayerCards)
        {
            if (playerCard.IsHave)
            {
                var haveCount = _cardGroupPanel.CheckCardInTeam(currentCardType, playerCard.CardId);
                var count = playerCard.Num - haveCount;
                if (count > 0)
                {
                    var skillCfg = _specialEffectsConfig.GetOrDefault(playerCard.CardId);
                    if (skillCfg.LevelPropertyList[playerCard.Lv-1].Quality ==  _selectedQuality)
                    {
                        for (int i = 0; i < count; i++)
                        {
                            FilterList.Add(playerCard.CardId);
                        }
                    }
                }

            }
        }
        
    }
    public void Hide()
    {
        this.gameObject.transform.LocalPositionX(9999);
        status = false;
    }
}
