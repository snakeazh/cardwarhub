using System.Threading.Tasks;
using Config;
using FusionContinent.Helper;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	public class SoldierGetPanelData : UIPanelData
	{
		public int itemId;
	}
	public partial class SoldierGetPanel : UIPanel
	{
		private TbItemConfig ItemConfig;
		private TbCardBaseConfig CardBaseConfig;
		TbSoldierBattleConfig SoldierBattleConfig;
		protected override void OnInit(IUIData uiData = null)
		{
			ItemConfig = DataConfigManager.Tables.TbItemConfig;
			CardBaseConfig = DataConfigManager.Tables.TbCardBaseConfig;
			SoldierBattleConfig = DataConfigManager.Tables.TbSoldierBattleConfig;
			mData = uiData as SoldierGetPanelData ?? new SoldierGetPanelData();
			// please add init code here
			this.BgButton.onClick.AddListener(() =>
			{
				UIKit.HidePanel<SoldierGetPanel>();
			});
		}
		
		protected override void OnOpen(IUIData uiData = null)
		{
			mData = uiData as SoldierGetPanelData ?? new SoldierGetPanelData();
			var itemConfig = ItemConfig.GetOrDefault(this.Data.itemId);
			if (itemConfig == null)
			{
				UIKit.HidePanel<SoldierGetPanel>();
				return;
			}

			var cardConfig = CardBaseConfig.Get(itemConfig.EffectValue);
		
			SpriteHelper.TryLoadPaintAsync(cardConfig.Icon, SoldierImg, EPaintType.Large,false);
			for (int i = 0; i < SpinePos.childCount; i++)
				Destroy(SpinePos.GetChild(i).gameObject);
			var soldierConfig = SoldierBattleConfig.Get(cardConfig.Id);
			var spineName = soldierConfig.Spine;
			var spineScale = soldierConfig.SpineScale;
			var soldierName = cardConfig.Name;
			var soldierName2 = soldierConfig.Name;
			SoldierName.text = soldierName;
			SoldierName2.text = soldierName2;
			MainAttrTypeIcon.sprite = Global.AttributeIconAtlas.GetSpriteExt(soldierConfig.MainAttrType.ToString());
			if (spineName != "")
			{
				var soliderSpine = Global.ResLoader.LoadSync<GameObject>(spineName).Instantiate().transform;
				soliderSpine.SetParent(SpinePos);
				soliderSpine.localScale = Vector3.one;
				soliderSpine.localPosition = Vector3.zero;
				SpinePos.localScale = spineScale > 0 ? Vector3.one * spineScale : Vector3.one;
			}
			
			
			// SoldierImg
			// itemConfig.EffectValue
		}
		
		protected override void OnShow()
		{
		}
		
		protected override void OnHide()
		{
			// 关键点：设置结果，这会让等待这个 Task 的代码继续执行
			RewardPopManager.Instance.Pop();
		}
		
		protected override void OnClose()
		{
		}
	}
}
