using System.Collections.Generic;
using Config;
using FusionContinent.Model;
using FusionContinent.Sounds;
using Manager;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using TMPro;

namespace FusionContinent
{
	public class LoginPanel01Data : UIPanelData
	{
	}

	public partial class LoginPanel01 : UIPanel
	{
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as LoginPanel01Data ?? new LoginPanel01Data();
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
