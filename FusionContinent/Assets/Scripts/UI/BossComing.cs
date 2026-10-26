using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	public class BossComingData : UIPanelData
	{
	}
	public partial class BossComing : UIPanel
	{
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as BossComingData ?? new BossComingData();
			// please add init code here
		}
		
		protected override void OnOpen(IUIData uiData = null)
		{
			DOVirtual.DelayedCall(2f, UIKit.ClosePanel<BossComing>);
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
