using System;
using System.Collections.Generic;
using System.Linq;
using Config;
using DG.Tweening;
using FusionContinent.Animation;
using FusionContinent.Model;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using ScrollView;
using TMPro;
using UnityEngine.U2D;

namespace FusionContinent
{
    public class BackpackPanelData : UIPanelData
    {
    }

    public partial class BackpackPanel : UIPanel
    {
        UIScrollView  scrollView;
        private TbGameBattleConst _tbBattleConst;
        private TbItemConfig _itemConfig;
        private UIPoolScope mPoolScope;
        private const string DetailRewardGroup = "BackpackDetailReward";
        protected override void OnInit(IUIData uiData = null)
        {
            mPoolScope = UIPoolScope.Get(gameObject);
            mPoolScope.ReleaseOnDisable = true;
            mPoolScope.ReleaseOnDestroy = true;
            mData = uiData as BackpackPanelData ?? new BackpackPanelData();
            bg.onClick.AddListener(() =>
            {
                ClosePanelAnimation();
            });
            CloseBtn.onClick.AddListener(() =>
            {
                ClosePanelAnimation();
                //Global.Publisher.Publish(MessagePublisher.CHANGE_GAME_MAIN_FOOT_BTN_ENABLED, true);
            });
            
        }

        void ClosePanelAnimation()
        {
            
            UIAnimationUtils.PlayAnimations(this.CloseAnimation, () =>
            {
                UIKit.HidePanel<BackpackPanel>();
            });
        
        }
        
        private int selectedCardId = 0;
        /// <summary>
        /// 初始化背包
        /// </summary>
        /// <param name="Cards"></param>
        public void InitBackpackItems(List<CardItems> Cards)
        {
            
        }

/// <summary>
/// 选择道具
/// </summary>
/// <param name="cardid"></param>
        public void ItemSelectBtns(int cardid)
        {
            
        }


        protected override void OnOpen(IUIData uiData = null)
        {
            _itemConfig = DataConfigManager.Tables.TbItemConfig;
            _tbBattleConst = DataConfigManager.Tables.TbGameBattleConst;
        }

        protected override void OnShow()
        {
            scrollView = this.ItemScrollview;
            Cards.Clear();
            selectedCardId = -1;
            foreach (var v in Global.PlayerItems)
            {
                CardItems card = new CardItems();
                card.cardid = v.Value.ItemId;
                card.cardnumber = v.Value.ItemNum;
                if (_itemConfig.GetOrDefault(v.Value.ItemId) == null) continue;
                card.cardtype = (int)_itemConfig.GetOrDefault(v.Value.ItemId).Quality;
                Cards.Add(card);
            }


            displayedCards = GetAllCards();
            if (displayedCards.Count > 0)
            {
                selectedCardId = 0;
            }
            scrollView.SetPrefabFactory(CreateItemNode);
            scrollView.SetReleaseAction(ReleaseItemNode);
            scrollView.SetUpdateFunc(OnUpdateCardItem);
 
            scrollView.SetUpdateSelectFunc(SelectCardItem);
            scrollView.ShowList(0);
            // scrollView
            UIAnimationUtils.PlayAnimations(this.OpenAnimation, () =>
            {
                scrollView.ShowList(displayedCards.Count);
                scrollView.ScrollToIndex(0,ScrollAlignment.Start,false);
                scrollView.PlayScaleAnimationDOTween();
                RefreshSelectItem();
            });

        }

        private void SelectCardItem(int dataIndex, ScrollItem scrollItem)
        {
            var item = (scrollItem as BagScrollItem).TweenTarget;
            item.transform.Find("Frame").gameObject.SetActive(selectedCardId == dataIndex);
        }

        private void ReleaseItemNode(GameObject obj)
        {
            if (obj == null)
            {
                return;
            }

            var rewardEntity = obj.transform.Find("backobj/RewardEntity");
            if (rewardEntity != null)
            {
                var rewardPool = CommonItemPoolManager.Instance.GetGameObjectPoolOrCreate("RewardEntity");
                rewardPool?.Return(rewardEntity.gameObject);
            }

            obj.GetComponent<ScrollItem>().OnRelease();
            var itemPool = CommonItemPoolManager.Instance.GetGameObjectPoolOrCreate("BackPackItem");
            itemPool?.Return(obj);
        }

        private GameObject CreateItemNode(RectTransform parent)
        {
            var itemPool = CommonItemPoolManager.Instance.GetGameObjectPoolOrCreate("BackPackItem");
            var item = itemPool?.Get(parent)?.GetComponent<ScrollItem>();
            if (item == null)
            {
                return null;
            }

            item.OnSpawn();
            return item.gameObject;
        }

        private void OnUpdateCardItem(int dataIndex, ScrollItem scrollItem)
        {
            var bagScrollItem = scrollItem as BagScrollItem;
            var item = bagScrollItem.TweenTarget;
            var config = _itemConfig.Get(displayedCards[dataIndex].cardid);
            var match = _tbBattleConst.QualityTypeColors
                .FirstOrDefault(t => t.Type == config.Quality);
            float3 matchColor = new float3();
            if (match != null)
                matchColor = match.Color;
            var rewardEntity =  item.transform.Find("backobj").Find("RewardEntity");
            var rewardObj = rewardEntity ? rewardEntity.gameObject : CommonItemPoolManager.Instance.GetGameObjectPoolOrCreate("RewardEntity")?.Get();
            if (rewardObj == null) return;

            rewardObj.name = "RewardEntity";
            rewardObj.SetActive(true);
            rewardObj.transform.SetParent(item.transform.Find("backobj"));
            rewardObj.transform.localScale = Vector3.one;
            rewardObj.transform.localPosition = Vector3.zero;
            rewardObj.GetComponent<RewardEntity>().SetData(
                displayedCards[dataIndex].cardid,
                displayedCards[dataIndex].cardnumber,
                RewardStyleType.Default,
                false);

            item.transform.Find("cardid").GetComponent<Text>().text = displayedCards[dataIndex].cardid.ToString();
            item.transform.Find("cardtype").GetComponent<Text>().text = displayedCards[dataIndex].cardtype.ToString();
            item.transform.Find("Frame").gameObject.SetActive(selectedCardId == dataIndex);
            // item.transform.Find("itembtn").GetComponent<Button>().onClick.AddListener(() =>
            // {
            //     // Frame.SetActive(true);
            //     // Frame.transform.SetParent(item.transform);
            //     // Frame.transform.localPosition = Vector3.zero;
            //     // usecardid = item.transform.Find("cardid").GetComponent<Text>().text.ToInt();
            //     // ItemSelectBtns(usecardid);
            // });

            bagScrollItem.OnClick = () =>
            {
                selectedCardId = dataIndex;
                scrollView.RefreshSelect();
                RefreshSelectItem();
            };
        }

        protected override void OnHide()
        {
            if (scrollView != null)
            {
                scrollView.ReleaseScrollView();
            }
            mPoolScope?.ReleaseGroup(DetailRewardGroup);
            rewardEntity = null;
        }

        GameObject rewardEntity = null;
        void RefreshSelectItem()
        {
            if (selectedCardId < 0)
            {
                this.ItemShow?.gameObject.SetActive(false);
            }
            else
            {
                this.ItemShow?.gameObject.SetActive(true);
                ItemShowicon.gameObject.SetActive(true);
                ItemShowname.gameObject.SetActive(true);
                ItemShowDesc.gameObject.SetActive(true);
                ItemShowUsebtn.gameObject.SetActive(true);
                ItemShowObtainbtn.gameObject.SetActive(true);
                var data = displayedCards[selectedCardId];
                mPoolScope?.ReleaseGroup(DetailRewardGroup);
                var detailRewardPool = CommonItemPoolManager.Instance.GetGameObjectPoolOrCreate("RewardEntity");
                var rewardObj = detailRewardPool?.Get();
                if (rewardObj == null) return;
                rewardEntity = rewardObj;
                mPoolScope?.Track(detailRewardPool, rewardObj, DetailRewardGroup);
                rewardObj.transform.SetParent(ItemShowicon.transform);
                rewardObj.GetComponent<RectTransform>().localScale = Vector3.one; 
                rewardObj.GetComponent<RectTransform>().sizeDelta = new Vector2(150f,150f);
                rewardObj.transform.localScale = new Vector3(1.3f, 1.3f, 1.3f);
                rewardObj.transform.localPosition = Vector3.zero;
                rewardObj.GetComponent<RewardEntity>().SetData(data.cardid, 0, RewardStyleType.Default, false);

                // 隐藏数量
                Transform numTransform = rewardObj.transform.Find("Num");
                if (numTransform != null)
                {
                    TMP_Text numText = numTransform.GetComponent<TMP_Text>();
                    if (numText != null) numText.text = "";
                }

                // 更新物品信息显示
                var config = _itemConfig.GetOrDefault(data.cardid);
                // obtiantxt = config?.Access ?? "";
                ItemShowname.text = config?.Name ?? "未知物品";
                ItemShowDesc.text = config?.Desc ?? "暂无描述";
            }
        }

        protected override void OnClose()
        {
            mPoolScope?.ReleaseAll();
            rewardEntity = null;
            if (scrollView != null)
            {
                scrollView.ReleaseScrollView();
                scrollView = null;
            }
        }
        
        public List<CardItems> displayedCards = new List<CardItems>();
        public List<CardItems> Cards = new List<CardItems>();
        // 4. 辅助方法：获取所有卡片（类型1）
        private List<CardItems> GetAllCards()
        {
            return Cards
                .Select(t => new { Card = t, Config = _itemConfig.GetOrDefault(t.cardid) })
                .Where(x => x.Config != null && (
                    x.Config.ModuleType == ModuleType.Prop ||
                    x.Config.ModuleType == ModuleType.CardFragment ||
                    x.Config.ModuleType == ModuleType.GiftPack)&&x.Card.cardnumber>0)
                .OrderByDescending(x => (int)x.Card.cardtype) // 先排序
                .Select(x => x.Card) // 最后提取Card
                .ToList(); // 只转换一次
        }
    }

    [Serializable]
    public class CardItems
    {
        public int cardid;
        public int cardtype;
        public long cardnumber;
    }
}