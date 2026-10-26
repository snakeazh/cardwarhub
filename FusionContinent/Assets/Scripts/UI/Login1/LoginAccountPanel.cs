using System;
using System.Collections.Generic;
using FusionContinent.Animation;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using TMPro;
using UnityEngine.Events;

namespace FusionContinent
{
	public class LoginAccountPanelData : UIPanelData
	{
	}
	public partial class LoginAccountPanel : UIPanel
	{
		

		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as LoginAccountPanelData ?? new LoginAccountPanelData();
			
		}



		
		protected override void RunAnimation(GameObject target, UnityAction complete)
		{
			UIAnimationUtils.PlayAnimations(target,complete);
		}

		protected override GameObject GetAnimationCloseNode
		{
			get => this.CloseAnimation.gameObject;
		}

		protected override GameObject GetAnimationOpenNode
		{
			get => this.OpenAnimation.gameObject;
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
