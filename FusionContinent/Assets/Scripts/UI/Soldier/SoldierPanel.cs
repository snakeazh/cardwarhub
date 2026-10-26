using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Config;
using DG.Tweening;
using FusionContinent.Animation;
using FusionContinent.Helper;
using FusionContinent.Manager;
using FusionContinent.Model;
using FusionContinent.Sounds;
using Game.CombatPower;
using Helper;
using Manager;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using ScrollView;
using TMPro;

namespace FusionContinent
{
	public class SoldierPanelData : UIPanelData
	{
	}
	public partial class SoldierPanel : UIPanel
	{
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as SoldierPanelData ?? new SoldierPanelData();
		
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
