using FusionContinent.Base;
using FusionContinent.Helper;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	public class SoldierCardGroupSlotData : UIPanelData
	{
	}
	public partial class SoldierCardGroupSlot : UIPanel
	{
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as SoldierCardGroupSlotData ?? new SoldierCardGroupSlotData();
			// please add init code here
			
			
		}

		public void SetData(CardSlotData data)
		{
			// var soldierCfg = DataConfigManager.Tables.TbSoldierBattleConfig.GetOrDefault(data.CardId);
			// if (soldierCfg == null) return;
			if (!string.IsNullOrEmpty(data.IconName))
			{
				SpriteHelper.TryLoadPaintAsync(data.IconName, Icon, groupName: "GeneralSkill");
			}
			else
			{
				AsyncTextureManager.Instance.InvalidateImage(Icon);
				Icon.sprite = data.Icon;
			}
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
