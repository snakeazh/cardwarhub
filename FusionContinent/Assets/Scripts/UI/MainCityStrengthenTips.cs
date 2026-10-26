using Config;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	public class MainCityStrengthenTipsData : UIPanelData
	{
	}
	public partial class MainCityStrengthenTips : UIPanel
	{
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as MainCityStrengthenTipsData ?? new MainCityStrengthenTipsData();
			// please add init code here
			Bg.onClick.AddListener(() =>
			{
                UIRoot.Instance.transform.Find("Audio Source").GetComponent<AudioSource>().Play();
                UIKit.HidePanel<MainCityStrengthenTips>();
			});
		}
		
		protected override void OnOpen(IUIData uiData = null)
		{
			var mainCity = UIKit.GetPanel<MainCityPanel>();
			LastMaxHpText.text = $"{Global.MaxCityHp}";
			LastDefenseText.text = $"{Global.CityDefense}";
			LastMaxPopulationText.text = $"{Global.MaxCityPopulation}";
			LastRetaliationText.text = $"{Global.Retaliation}";
			LastBuildingTimeText.text = $"{Global.BuildingTime}";
			NowMaxHpText.text = $"";
			NowDefenseText.text = $"";
			NowMaxPopulationText.text = $"";
			NowRetaliationText.text = $"";
			NowBuildingTimeText.text = $"";
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
