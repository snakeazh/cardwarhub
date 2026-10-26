using System.Collections.Generic;
using Assets.Scripts.Localization;
using Config;
using FusionContinent.Animation;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using TMPro;
using UnityEngine.Events;

namespace FusionContinent
{
	public class LanuageChangePanelData : UIPanelData
	{
	}
	public partial class LanuageChangePanel : UIPanel
	{
		
		
		private int _lastSelectedIndex = 0;
		
		public Button quitBtn;
		private readonly List<GameObject> buttons = new List<GameObject>();
		
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as LanuageChangePanelData ?? new LanuageChangePanelData();
			// please add init code here
			// quitBtn.onClick.RemoveAllListeners();
			// quitBtn.onClick.AddListener(() =>
			// {
			// 	RunAnimation(this.GetAnimationCloseNode, () =>
			// 	{
			// 		UIKit.ClosePanel(this);
			// 	});
			// });
			//
			// InitLanuageBtn();
			
		}

		// private void InitLanuageBtn()
		// {
		// 	var langList = DataConfigManager.Tables.TbLanguageConfig.DataList;
		// 	int i = 0;
		// 	foreach (var cfg in langList)
		// 	{
		// 		if (cfg.State == StateType.Close) continue;
		// 		// langOptions.Add(cfg.Name);
		// 		var obj = i < LanuageChangeScroll.transform.childCount ? LanuageChangeScroll.transform.GetChild(0).gameObject: Instantiate(LanuageChangeScroll.transform.GetChild(0).gameObject, LanuageChangeScroll);
		// 		obj.SetActive(true);
		// 		var selectedImg = obj.transform.Find("Selected").gameObject;
		// 		var selectedText = obj.transform.Find("SelectedText").gameObject.GetComponent<TextMeshProUGUI>();
		// 		var unSelectedText = obj.transform.Find("UnSelectedText").gameObject.GetComponent<TextMeshProUGUI>();
		// 		selectedText.text = cfg.Name;
		// 		unSelectedText.text = cfg.Name;
		// 		selectedImg.SetActive(false);
		// 		selectedText.gameObject.SetActive(false);
		// 		int index = i;
		// 		obj.gameObject.GetComponent<Button>().onClick.RemoveAllListeners();
		// 		obj.gameObject.GetComponent<Button>().onClick.AddListener(() =>
		// 		{
		// 			OnLanguageSelected(index);
		// 		});
		// 		buttons.Add(obj);
		// 		i++;
		// 	}
		// }

		// private void OnLanguageSelected(int idx)
		// {
		// 	for (int j = 0; j < LanuageChangeScroll.childCount; j++)
		// 	{
		// 		Transform item = LanuageChangeScroll.GetChild(j);
		// 		bool isSelected = (j == idx);
		// 		item.GetChild(1).gameObject.SetActive(isSelected); 
		// 		item.GetChild(2).gameObject.SetActive(!isSelected); 
		// 		item.GetChild(3).gameObject.SetActive(isSelected); 
		// 	}
		// 	LocalizationManager.Instance.SetLanguage((LanguageType)idx);
		// 	_lastSelectedIndex = idx;
		// 	PlayerPrefs.SetInt("lastSelectedLanguageIndex", _lastSelectedIndex);
		// }
		//
		// protected override void RunAnimation(GameObject target, UnityAction complete)
		// {
		// 	UIAnimationUtils.PlayAnimations(target,complete);
		// }

		protected override GameObject GetAnimationCloseNode
		{
			get => this.CloseAnimation.gameObject;
		}

		protected override GameObject GetAnimationOpenNode
		{
			get => this.OpenAnimation.gameObject;
		}
		
		protected override void OnOpen(IUIData uiData = null)
		{
			// int i = PlayerPrefs.GetInt("lastSelectedLanguageIndex");
			// buttons[i].GetComponent<Button>().onClick.Invoke();
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
