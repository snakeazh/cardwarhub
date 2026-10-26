using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	public class GeneralCardConbinePanelData : UIPanelData
	{
	}
	public partial class GeneralCardConbinePanel : UIPanel
	{
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as GeneralCardConbinePanelData ?? new GeneralCardConbinePanelData();
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
