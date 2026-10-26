using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	public class GameLobbyPanelData : UIPanelData
	{
	}
	public partial class GameLobbyPanel : UIPanel
	{
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as GameLobbyPanelData ?? new GameLobbyPanelData();
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
