using System.Linq;
using Config;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	public class GameCardTipsData : UIPanelData
	{
	}
	public partial class GameCardTips : UIPanel
	{
		private TbCardBaseConfig _baseConfig;
		private TbCardBattleConfig _battleConfig;
		private TbSoldierBattleConfig _tbSoldierBattle;
		private TbBuildingBattleConfig _tbBuildingBattleConfig;
		private TbSpecialEffectsConfig _tbSpecialEffectsConfig;
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as GameCardTipsData ?? new GameCardTipsData();
			_baseConfig = DataConfigManager.Tables.TbCardBaseConfig;
			_battleConfig = DataConfigManager.Tables.TbCardBattleConfig;
			_tbSoldierBattle = DataConfigManager.Tables.TbSoldierBattleConfig;
			_tbBuildingBattleConfig = DataConfigManager.Tables.TbBuildingBattleConfig;
			_tbSpecialEffectsConfig = DataConfigManager.Tables.TbSpecialEffectsConfig;
			// please add init code here
		}
		
		protected override void OnOpen(IUIData uiData = null)
		{
			ShowGameCardTips();
			
		}

		public void SetData(int cardID,int level,BuildingType type,string cardName)
		{
			CardName.text = $"{cardName}";
			if (type == BuildingType.Production)
			{
				string soldieratk = "";
				string soldierInterval = "";
				
				if (cardID == 210001 || cardID == 211001)
				{
					soldieratk = Global.SpearmanAtk.Value switch
					{
						0 => "",
						> 0 => $"+{Mathf.CeilToInt(Global.SpearmanAtk.Value * 100)}%",
						_ => $"-{Mathf.CeilToInt(Global.SpearmanAtk.Value * 100)}%"
					};
					soldierInterval = Global.SpearmanInterval.Value switch
					{
						0 => "",
						> 0 => $"+{Mathf.CeilToInt(Global.SpearmanInterval.Value * 100)}%",
						_ => $"-{Mathf.CeilToInt(Global.SpearmanInterval.Value * 100)}%"
					};
				}
				else if (cardID == 210002 || cardID == 211002)
				{
					soldieratk = Global.ArcherAtk.Value switch
					{
						0 => "",
						> 0 => $"+{Mathf.CeilToInt(Global.ArcherAtk.Value * 100)}%",
						_ => $"-{Mathf.CeilToInt(Global.ArcherAtk.Value * 100)}%"
					};
					soldierInterval = Global.ArcherInterval.Value switch
					{
						0 => "",
						> 0 => $"+{Mathf.CeilToInt(Global.ArcherInterval.Value * 100)}%",
						_ => $"-{Mathf.CeilToInt(Global.ArcherInterval.Value * 100)}%"
					};
				}
				else if (cardID == 210003)
				{
					soldieratk = Global.MagicianAtk.Value switch
					{
						0 => "",
						> 0 => $"+{Mathf.CeilToInt(Global.MagicianAtk.Value * 100)}%",
						_ => $"-{Mathf.CeilToInt(Global.MagicianAtk.Value * 100)}%"
					};
					soldierInterval = Global.MagicianInterval.Value switch
					{
						0 => "",
						> 0 => $"+{Mathf.CeilToInt(Global.MagicianInterval.Value * 100)}%",
						_ => $"-{Mathf.CeilToInt(Global.MagicianInterval.Value * 100)}%"
					};
				}

				// var soldierBattle = _tbSoldierBattle.GetOrDefault(cardID);
				// var soldierAkt = Mathf.CeilToInt(soldierBattle.Attack + (soldierBattle.GrowAtk * level - 1));
				
				// Description.text = $"{_baseConfig.GetOrDefault(cardID).Desc}\n" +
				//                    $"产出lv.{level}的{_baseConfig.GetOrDefault(cardID).Name}\n" +
				//                    $"攻击力：{soldierAkt}{soldieratk}\n" +
				//                    $"攻击间隔：{soldierBattle.AttackTime}{soldierInterval}\n" +
				//                    $"攻击距离：{soldierBattle.AttackRange}";
			}
			else if (type == BuildingType.Special || type == BuildingType.Population)
			{
				Description.text = $"{_baseConfig.GetOrDefault(cardID).Desc}";
			}
		}
		
		public void ShowGameCardTips()
		{
			// 获取鼠标位置（手指点击位置）
			Vector3 mousePosition = Input.mousePosition;

			// 获取面板的 RectTransform
			RectTransform tipsRectTransform = BG.GetComponent<RectTransform>();
			float panelWidth = tipsRectTransform.rect.width;
			float panelHeight = tipsRectTransform.rect.height;

			// 计算初步放置的位置（鼠标/手指上方）
			Vector3 targetPosition = new Vector3(mousePosition.x, mousePosition.y + panelHeight / 2, 0);

			// 检查是否超出屏幕的左右边界
			if (targetPosition.x + panelWidth / 2 > Screen.width)
			{
				// 如果右侧超出屏幕，将面板移到左侧
				targetPosition.x = Screen.width - panelWidth / 2;
			}
			else if (targetPosition.x - panelWidth / 2 < 0)
			{
				// 如果左侧超出屏幕，将面板移到右侧
				targetPosition.x = panelWidth / 2;
			}

			// 检查是否超出屏幕的顶部边界
			if (targetPosition.y + panelHeight / 2 > Screen.height)
			{
				// 如果顶部超出屏幕，将面板移到手指点击点的下方
				targetPosition.y = mousePosition.y - panelHeight / 2;
			}

			// 设置面板的位置
			tipsRectTransform.position = targetPosition;
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
