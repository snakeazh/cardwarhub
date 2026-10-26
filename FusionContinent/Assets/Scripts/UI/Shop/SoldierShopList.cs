using System.Collections.Generic;
using System.Linq;
using Config;
using FusionContinent.Model;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using ScrollView;
using TMPro;
using UIFramework;

namespace FusionContinent
{
	public class SoldierShopListData : UIPanelData
	{
		public int ShopId;
	}
	public partial class SoldierShopList : QFramework.UIPanel
	{
		private TbItemConfig _itemConfig;
		private TbShopConfig _shopConfig;
		private TbShopGoodsConfig _goodsConfig;
		protected override void OnInit(QFramework.IUIData uiData = null)
		{
			mData = uiData as SoldierShopListData ?? new SoldierShopListData();
			_goodsConfig = DataConfigManager.Tables.TbShopGoodsConfig;
			_shopConfig = DataConfigManager.Tables.TbShopConfig;
			_itemConfig = DataConfigManager.Tables.TbItemConfig;

			// please add init code here
			this.ShopListScroll.SetUpdateFunc(OnUpdateCardInfo);
		}

		private List<ShopGoodsConfig> displayData;
		
		private void OnUpdateCardInfo(int dataIndex, ScrollItem scrollItem)
		{
			var referenceItem = scrollItem as SoldierShopItem;
			// var itemTransform = scrollItem.transform as RectTransform;
			var goods = this.displayData[dataIndex];

			bool isLimitReached = Global.GetCardById(_itemConfig.GetOrDefault(goods.BuyItemId).EffectValue
			) != null;
            // 获取玩家商品数据
            var goodsInfo = Global.GetGoodsById(goods.Id);
            // var consumeItemObj = referenceItem.Find("shopitembg/consumebtn/consume/Layout");
            // ✅ 读取第一个消耗品的图标（如果有）
            if (goods.BuyConsumes != null && goods.BuyConsumes.Count > 0)
            {
                var consume = goods.BuyConsumes[0]; // 这里只取第一个
                var itemCfg = _itemConfig.GetOrDefault(consume.Id);
                if (itemCfg != null)
                {
	                long playerItemCount = Global.GetItemNumById(consume.Id);
	                referenceItem.consumeImg.transform.parent.gameObject.SetActive(true);
	                referenceItem.consumeImg.sprite =Global.ItemIconSpriteAtlas.GetSpriteExt(itemCfg.Icon);
	                referenceItem.consumeText.color = playerItemCount < consume.Num ? Color.red : Color.white;
	                referenceItem.consumeText.text = consume.Num.ToString();
                }
            }
            else
            {
	            referenceItem.consumeImg.transform.parent.gameObject.SetActive(false);
	            referenceItem.consumeText.GetComponent<TMP_Text>().text = "免费";
	            referenceItem.consumeText.color =  Color.white;
            }
            
            referenceItem.shopNameText.text = goods.Name;
            referenceItem.shopIcon.sprite = Global.ShopGoodsIconAtlas.GetSpriteExt(goods.Shopgoodsicon);
            var shopiconbtn = referenceItem.clickBtn;
            //装备详情
            shopiconbtn.onClick.RemoveAllListeners();
            shopiconbtn.onClick.AddListener(() =>
            {
                var equipId = _itemConfig.GetOrDefault(goods.BuyItemId).EffectValue;
                GlobalFunc.OpenEquipDetailsPanel(equipId, 0, UIFramework.UILevel.Const, panel =>
                {
                    if (panel != null)
                    {
                        panel.transform.SetParent(transform);
                    }
                });
            });
            // 限购显示
            bool isDailyLimit = goods.LimitType == GoodsLimitType.Daily;
            // item.buylimitnum.gameObject.SetActive(isDailyLimit);
            // if (isDailyLimit)
            // {
            //     item.buylimitnum.text = "限购" + $"{goods.LimitNum - goodsInfo.BuyNum}/{goods.LimitNum}";
            // }


            if (goods.BuyConsumes == null || goods.BuyConsumes.Count == 0)
            {
	            GuideFlow.Instance.SetStep(GuideType.BuyHero, 2, shopiconbtn.transform);
            }
            
            referenceItem.Mask.gameObject.SetActive(isLimitReached);
            referenceItem.clickBtn.gameObject.SetActive(!isLimitReached);
            // item.consumebtn.interactable = (goods.LimitNum - goodsInfo.BuyNum )> 0;
            // 购买
            referenceItem.OnSpawn();
            // referenceItem.clickBtn.onClick.RemoveAllListeners();
            referenceItem.OnClick = () =>
            {
	            if (isLimitReached)
	            {
		            return;
	            }

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
            
            // item.buylimitnum.text = "限购" + $"{goods.LimitNum - goodsInfo.BuyNum}/{goods.LimitNum}";

            if ((goods.LimitNum - goodsInfo.BuyNum) == 0 &&goods.LimitNum != 0)
            {
                // item.shopitembgmask.gameObject.SetActive(true);
            }
            if (Global.GetCardById(_itemConfig.GetOrDefault(goods.BuyItemId).EffectValue
                    )!=null)
            {
                // item.shopitembgmask.gameObject.SetActive(true);
                // item.consumebtn.gameObject.SetActive(false);
            }
		}

		void ShowList()
		{
			
			this.ShopListScroll.ShowList(displayData.Count);
		}

		void RefreshData()
		{
			displayData = _goodsConfig.DataList
				.Where(x => x.ShopId == this.Data.ShopId)
				.OrderBy(x => x.Id)
				.ToList();
		}
		
		
		protected override void OnOpen(QFramework.IUIData uiData = null)
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
