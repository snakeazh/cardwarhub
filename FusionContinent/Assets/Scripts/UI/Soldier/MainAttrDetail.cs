using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	public class MainAttrDetailData : UIPanelData
	{
	}
	public partial class MainAttrDetail : UIPanel
	{
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as MainAttrDetailData ?? new MainAttrDetailData();
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
