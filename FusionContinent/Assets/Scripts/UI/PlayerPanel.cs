using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	public class PlayerPanelData : UIPanelData
	{
	}
	public partial class PlayerPanel : UIPanel
	{
		public Button returnBtn;
		
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as PlayerPanelData ?? new PlayerPanelData();
			// please add init code here
			
			returnBtn.onClick.AddListener(() =>
			{
				UIKit.ClosePanel(this);
			});
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
