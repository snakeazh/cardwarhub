using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	public class GameCardData : UIPanelData
	{
	}
	public partial class GameCard : UIPanel
	{
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as GameCardData ?? new GameCardData();
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
