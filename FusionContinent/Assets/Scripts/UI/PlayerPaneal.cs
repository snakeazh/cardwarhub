using Config;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	public class PlayerPanealData : UIPanelData
	{
	}
	public partial class PlayerPaneal : UIPanel
	{
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as PlayerPanealData ?? new PlayerPanealData();
			// please add init code here
		}
		
		protected override void OnOpen(IUIData uiData = null)
		{
			// 点击天赋
			// GuideFlow.Instance.SetStep(GuideType.SetTalent, 1, TalentBtn.transform);
			// GuideFlow.Instance.SetStep(GuideType.SetCardGroup, 1, TalentBtn.transform);
			// GuideFlow.Instance.SetStep(GuideType.SetSpecialTalent, 1, TalentBtn.transform);
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
