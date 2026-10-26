using System.Collections.Generic;
using System.Linq;
using Config;
using FusionContinent.Model;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using TMPro;

namespace FusionContinent
{
	public class PlayerMainPanelData : UIPanelData
	{
	}
	public partial class PlayerMainPanel : UIPanel
	{
		public static PlayerMainPanel Default;
		
		private readonly Dictionary<ModuleFunctionType, Button> _btnMap = new Dictionary<ModuleFunctionType, Button>();

		protected void Awake()
		{
			Default = this;
		}
		
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as PlayerMainPanelData ?? new PlayerMainPanelData();
			var btnList = DataConfigManager.Tables.TbGameConst.PlayerMainButtonList;

			// 页签切换
			for (int i = 0; i < Change.childCount; i++)
			{
				int idx = i;
				var btnCfg = btnList[idx];
				Transform changeBtn = Change.GetChild(idx);
				changeBtn.Find("Text").GetComponent<TMP_Text>().text = btnCfg.Name;
				// changeBtn.Find("Icon").GetComponent<Image>().sprite = Global.PlayerMainIconAtlas.GetSprite(btnCfg.Icon);
				changeBtn.GetComponent<Button>().onClick.AddListener(() =>
				{
					for (int j = 0; j < Change.childCount; j++)
					{
						if (idx != j)
						{
							Change.GetChild(j).Find("Image").localScale = Vector3.zero;
						}
					}
					
					changeBtn.Find("Text").GetComponent<TMP_Text>().text = btnCfg.Name;
					changeBtn.Find("Image").localScale = Vector3.one;
					changeBtn.gameObject.SetActive(true);
					ChangePageLogic(btnCfg.Type);
				});
                    
				_btnMap.Add(btnCfg.Type, changeBtn.GetComponent<Button>());
			}

			Global.Publisher.Subscribe(MessagePublisher.CHANGE_PLAYER_MAIN_FOOT_BTN_ENABLED, (enabledBtn) =>
			{
				Change.gameObject.SetActive((bool)enabledBtn);
			});
		}

		private void ChangePageLogic(ModuleFunctionType type)
		{
			UIKit.ClosePanel<PlayerPaneal>();
			// UIKit.ClosePanel<CardGroupPanel01>();
			UIKit.HidePanel<TalentPanel>();
			switch (type)
			{
				// case ModuleFunctionType.SetCardGroup:
				// 	UIKit.OpenPanel<CardGroupPanel01>(UILevel.PopUI).SetData(Talent.GetUseTalent().TalentId);
					// UIKit.OpenPanel<CardGroupPanel>(UILevel.PopUI).SetData(Talent.GetUseTalent().TalentId);
					// break;

				case ModuleFunctionType.SetTalent:
				{
					UIKit.OpenPanel<TalentPanel>();
					break;
				}
				case ModuleFunctionType.PlayerFigure:
					UIKit.OpenPanel<PlayerPaneal>();
					break;
			}
		}
		
		public void ChangePage(ModuleFunctionType type)
		{
			if (_btnMap.TryGetValue(type, out var btn))
			{
				btn.onClick.Invoke();
			}
		}

		protected override void OnOpen(IUIData uiData = null)
		{
			// 默认选中第一个
			if (_btnMap.Count > 0)
				_btnMap.First().Value.onClick.Invoke();
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
