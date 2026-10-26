using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	public class FusionCompoundResultPanelData : UIPanelData
	{
	}
	public partial class FusionCompoundResultPanel : UIPanel
	{
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as FusionCompoundResultPanelData ?? new FusionCompoundResultPanelData();
			// please add init code here
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
