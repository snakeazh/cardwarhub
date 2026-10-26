using System;
using System.Collections;
using System.Collections.Generic;
using Config;
using FusionContinent;
using FusionContinent.Helper;
using FusionContinent.Model;
using QFramework;
using ScrollView;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public interface IChoosePanel
{
    float PositionY { get; }
    bool Status { get; }
    void OnInit(CardGroupPanelNew cardGroupPanel,CardType cardType);


    void Show();


    void Refresh();
    void Hide();
}

public class ChooseHeroPanel : MonoBehaviour,IChoosePanel
{
    public float PositionY
    {
        get => 0;
    }
    private TbCardBaseConfig _cardBaseConfig;
    private TbSkinConfig _skinConfig;
    private TbSoldierBattleConfig _soldierBattleConfig;
    private CareerType? _currentFilter;
    CardGroupPanelNew _cardGroupPanel;
    public UIVerticalScrollViewLock ScrollViewLock;
    CardType currentCardType;
    public List<PlayerCard> allPlayerCards;
    public bool Status
    {
        get => status;
    }
    private bool status;
    public void OnInit(CardGroupPanelNew cardGroupPanel,CardType cardType)
    {
        this.gameObject.transform.LocalPositionX(9999);

        _cardBaseConfig = DataConfigManager.Tables.TbCardBaseConfig;
        _skinConfig = DataConfigManager.Tables.TbSkinConfig;
        _soldierBattleConfig = DataConfigManager.Tables.TbSoldierBattleConfig;
        _currentFilter = null;
        currentCardType = cardType;
        allPlayerCards = new List<PlayerCard>();
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
        // ScrollViewLock.SetUpdateSelectFunc(OnSelectAction);
    }

    private void OnSelectAction(int dataIndex, ScrollItem scrollItem)
    {
        var cardId = FilterList[dataIndex];

        _cardGroupPanel.Deploy(CardType.Soldier, cardId);
    }

    public void Show()
    {
        this.gameObject.transform.LocalPositionX(0);
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
            if (!playerCard.IsHave) continue;
            if (_currentFilter != null)
            {
                var soldierCfg = _soldierBattleConfig.GetOrDefault(playerCard.CardId);
                if (soldierCfg == null || soldierCfg.CarType != _currentFilter) continue;
            }
            FilterList.Add(playerCard.CardId);
        }

    }

    public void SetCareerFilter(int dropdownIndex)
    {
        _currentFilter = dropdownIndex == 0 ? null : (CareerType?)(dropdownIndex - 1);
        Filter();
        ScrollViewLock.ShowList(FilterList.Count);
        ScrollViewLock.ScrollToIndex(0, ScrollAlignment.Start, false);
    }
    
    public void Hide()
    {
        this.gameObject.transform.LocalPositionX(9999);
        status = false;
    }
   
    private void SetDefaultBg(Image heroBgImg, int excludeCardId)
    {
        for (int i = 0; i < FilterList.Count; i++)
        {
            var fallbackId = FilterList[i];
            if (fallbackId == excludeCardId) continue;

            var fallbackCfg = _cardBaseConfig.Get(fallbackId);
            if (fallbackCfg == null) continue;

            var fallbackIcon = fallbackCfg.Icon;
            var fallbackCard = Global.GetCardById(fallbackId);
            if (fallbackCard != null && fallbackCard.ObjData != null && fallbackCard.ObjData.Skin != 0)
            {
                var skinCfg = _skinConfig.GetOrDefault(fallbackCard.ObjData.Skin);
                if (skinCfg != null) fallbackIcon = skinCfg.Icon;
            }

            if (SpriteHelper.TryLoadPaintAsync(fallbackIcon + "Bg", heroBgImg, EPaintType.Medium, false))
                return;
        }
    }

    private void OnUpdatePlayerCard(int dataIndex, ScrollItem scrollItem)
    {
        scrollItem.OnSpawn();
        var cardId = FilterList[dataIndex];
        var isOnTeam = _cardGroupPanel.CheckCardInTeam(currentCardType, cardId) > 0;

        var cardCfg =  _cardBaseConfig.Get(cardId);
        var iconTf = scrollItem.transform.Find("Mask/Icon");
        var heroBg = scrollItem.transform.Find("Mask/HeroBg");
        var nameText = scrollItem.transform.Find("NameBg/HeroName");
        var isJoin = scrollItem.transform.Find("IsJoin");
        iconTf.gameObject.SetActive(cardCfg != null);
        if (isJoin != null) isJoin.gameObject.SetActive(isOnTeam);
        if (cardCfg != null)
        {
            var playerCard = Global.GetCardById(cardId);
            var bigIcon = cardCfg.Icon;
            if (playerCard != null && playerCard.ObjData != null && playerCard.ObjData.Skin != 0)
            {
                var skinCfg = _skinConfig.GetOrDefault(playerCard.ObjData.Skin);
                if (skinCfg != null)
                {
                    bigIcon = skinCfg.Icon;
                }
            }

            var soldierBattleConfig = DataConfigManager.Tables.TbSoldierBattleConfig.Get(cardId);
            SpriteHelper.TryLoadPaintAsync(bigIcon, iconTf.GetComponent<Image>(), EPaintType.Large, false);
            if (soldierBattleConfig.PaintingBg.Equals(""))
            {
                SetDefaultBg(heroBg.GetComponent<Image>(), cardId);
            }
            else
            {
                SpriteHelper.TryLoadPaintAsync(soldierBattleConfig.PaintingBg, heroBg.GetComponent<Image>(),
                    EPaintType.Medium, false);
            }
            nameText.GetComponent<TextMeshProUGUI>().text = cardCfg.Name;

        }

        var bagItem = scrollItem as BagScrollItem;
        bagItem.OnClick = () =>
        {
            if (isOnTeam) return;
            _cardGroupPanel.Deploy(CardType.Soldier, cardId);
        };
    }
}
