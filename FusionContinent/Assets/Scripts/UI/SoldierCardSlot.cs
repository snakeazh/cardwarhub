using Config;
using FusionContinent.Helper;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using TMPro;

namespace FusionContinent
{
	public class SoldierCardSlotData : UIPanelData
	{
	}
	public partial class SoldierCardSlot : UIPanel
	{
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as SoldierCardSlotData ?? new SoldierCardSlotData();
			// please add init code here
		}
		
		public void SetData(CardSlotData data)
		{
			var soldierCfg = DataConfigManager.Tables.TbSoldierBattleConfig.GetOrDefault(data.CardId);
			if (soldierCfg == null) return;
			//立绘修改为特定的直接通用接口
			SpriteHelper.TryLoadPaintAsync(soldierCfg.BattleQutIcon, transform.Find("Icon").GetComponent<Image>());
			transform.Find("CardName").GetComponent<TMP_Text>().text = $"{data.NameText}";
			
			// 主属性
			transform.Find("MainAttribute").GetComponent<Image>().sprite = Global.CardIconSpriteAtlas.GetSpriteExt(soldierCfg.MainAttrType.ToString());
			
			// 锁定
			transform.Find("Mask").gameObject.SetActive(data.State == CardState.Develop);
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
