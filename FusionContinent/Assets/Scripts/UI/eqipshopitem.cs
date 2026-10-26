using System;
using Config;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	public class eqipshopitemData : UIPanelData
	{
	}
	public partial class eqipshopitem : UIPanel
	{
		public int eqipshopitemid;
		public GameObject rawimage;
		public Image mainAttrType;
		
		private TbShopConfig _shopConfig;
		private TbShopGoodsConfig _goodsConfig;
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as eqipshopitemData ?? new eqipshopitemData();
			// please add init code here
			_shopConfig = DataConfigManager.Tables.TbShopConfig;
			_goodsConfig = DataConfigManager.Tables.TbShopGoodsConfig;
		}
		public void SetEqipShopitemid(int eqipshopitemid)
		{
			this.eqipshopitemid = eqipshopitemid;
		}

		private void OnDisable()
		{
			effect1.gameObject.SetActive(false);
			effect2.gameObject.SetActive(false);
		}

		protected override void OnOpen(IUIData uiData = null)
		{
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
