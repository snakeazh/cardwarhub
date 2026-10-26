using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	public class BaseAttrDetailData : UIPanelData
	{
	}
	public partial class BaseAttrDetail : UIPanel
	{
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as BaseAttrDetailData ?? new BaseAttrDetailData();
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
