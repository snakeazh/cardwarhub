using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Localization;
using Config;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using FusionContinent.Animation;
using FusionContinent.Helper;
using FusionContinent.Manager;
using FusionContinent.Model;
using FusionContinent.UI.Components;
using Manager;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using TMPro;
using UIManager = UIFramework.UIManager;
using Game.RedDot;

namespace FusionContinent
{
	public class MainInterfacePanelData : UIPanelData
	{
	}
	public partial class MainInterfacePanel : UIPanel
	{
		
	
		
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as MainInterfacePanelData ?? new MainInterfacePanelData();
			
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
