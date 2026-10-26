using System.Collections.Generic;
using System.Linq;
using Config;
using FusionContinent.Model;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using ScrollView;
using TMPro;

namespace FusionContinent
{
    public class ChallengeShopListData : UIPanelData
    {
        public int ShopId;

    }
    public partial class ChallengeShopList : UIPanel
    {
        private TbShopGoodsConfig _goodsConfig;
        private TbItemConfig _itemConfig;

		
        protected override void OnInit(IUIData uiData = null)
        {
            mData = uiData as ChallengeShopListData ?? new ChallengeShopListData();
            // please add init code here
            _goodsConfig = DataConfigManager.Tables.TbShopGoodsConfig;
            _itemConfig = DataConfigManager.Tables.TbItemConfig;

            this.ShopListScroll.SetUpdateFunc(OnUpdateCardInfo);
            // Exit.onClick.AddListener(() =>
            // {
            //     UIKit.ClosePanel(this);
            // });
        }
		
        private List<ShopGoodsConfig> displayData;

        private void OnUpdateCardInfo(int dataIndex, ScrollItem scrollItem)
        {
            var referenceItem = scrollItem.GetComponent<ShopItem>();
            var goods = this.displayData[dataIndex];
			
            var goodsInfo = Global.GetGoodsById(goods.Id);
			
            //收费
            if (goods.BuyConsumes != null && goods.BuyConsumes.Count > 0)
            {
                var consume = goods.BuyConsumes[0]; // 这里只取第一个
                var itemCfg = _itemConfig.GetOrDefault(consume.Id);
                if (itemCfg != null)
                {
                    long playerItemCount = Global.GetItemNumById(consume.Id);
                    referenceItem.consumebtn.transform.Find("consume").gameObject.SetActive(true);
                    referenceItem.consumeicon.sprite =Global.ItemIconSpriteAtlas.GetSpriteExt(itemCfg.Icon);
                    referenceItem.consumenum.color = playerItemCount < consume.Num ? Color.red : Color.white;
                    referenceItem.consumenum.text = consume.Num.ToString();
                }
            }
            else//不收费
            {
                referenceItem.consumebtn.transform.Find("consume").transform.parent.gameObject.SetActive(false);
                referenceItem.consumebtn.transform.Find("freetxt").gameObject.SetActive(true);
            }

            referenceItem.shopnametxt.text = goods.Name;
            referenceItem.shopicon.sprite = Global.ShopGoodsIconAtlas.GetSpriteExt(goods.Shopgoodsicon);
            referenceItem.ShopNums.text = goods.BuyItemNum.ToString();
            bool isDailyLimit = goods.LimitType == GoodsLimitType.Daily;
            referenceItem.buylimitnum.gameObject.SetActive(isDailyLimit);
            if (isDailyLimit)
            {
                referenceItem.buylimitnum.text = "限购" + $"{goods.LimitNum - goodsInfo.BuyNum}/{goods.LimitNum}";
            }

            bool isLimitReached = (goods.LimitNum - goodsInfo.BuyNum) <= 0;
            
            // bool isLimitReached = Global.GetCardById(_itemConfig.GetOrDefault(goods.BuyItemId).EffectValue
            // ) != null;
            // referenceItem.Mask.gameObject.SetActive(isLimitReached);
            // referenceItem.clickBtn.gameObject.SetActive(!isLimitReached);
            // 购买
            referenceItem.GetComponent<BagScrollItem>().OnSpawn();
            referenceItem.GetComponent<BagScrollItem>().OnClick = () =>
            {
                if (isLimitReached)
                {
                    FusionContinent.Toast.Show("购买次数已用完");
                    return;
                }
                referenceItem.transform.Find("shopitembg").GetComponent<Button>().interactable = true;
                // 消耗品检查
                if (goods.BuyConsumes != null && goods.BuyConsumes.Count > 0)
                {
                    var consume = goods.BuyConsumes[0];
                    long playerItemCount = Global.GetItemNumById(consume.Id);
                    if (playerItemCount < consume.Num)
                    {
                        string itemName = _itemConfig.GetOrDefault(consume.Id)?.Name ?? "物品";
                        FusionContinent.Toast.Show(itemName + "不足");
                        return;
                    }

                    var msg = $"是否消耗{Items.GetRewardsStr(goods.BuyConsumes)}";
                    GameMainPanel.Default.ShowConfirm("购买确认", msg, () =>
                    {
                        goodsInfo.BuyGoods(1, () =>
                        {
                            RefreshData();
                            ShowList();
                        });
                    });
                }
                else
                {
                    goodsInfo.BuyGoods(1, () =>
                    {
                        RefreshData();
                        ShowList();
                    });
                }
            };
        }
		
        void RefreshData()
        {
            displayData = _goodsConfig.DataList
                .Where(x => x.ShopId == this.Data.ShopId)
                .OrderBy(x => x.Id)
                .ToList();
        }
		
        void ShowList()
        {
			
            this.ShopListScroll.ShowList(displayData.Count);
        }
		
		
        protected override void OnOpen(IUIData uiData = null)
        {
            RefreshData();
            ShowList();
        }
		
        protected override void OnShow()
        {
        }
		
        protected override void OnHide()
        {
        }
		
        protected override void OnClose()
        {
        }
    }
}