using System;
using System.Collections;
using System.Collections.Generic;
using Config;
using FusionContinent.Animation;
using FusionContinent.Model;
using FusionContinent.Sounds;
using Manager;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using ScrollView;
using TMPro;
using UnityEngine.Events;

namespace FusionContinent
{
	public class RewardTipsData : UIPanelData
	{
	}
	public partial class RewardTips : UIPanel
	{
		private RewardTipsData _data = new RewardTipsData();
		
		protected override void OnInit(IUIData uiData = null)
		{
			Transform.localScale = Vector3.one;
			
			mData = uiData as RewardTipsData ?? new RewardTipsData();
			
			
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
		
		protected override GameObject GetAnimationCloseNode
		{
			get => this.CloseAnimation.gameObject;
		}

		protected override GameObject GetAnimationOpenNode
		{
			get => this.OpenAnimation.gameObject;
		}
	}
}
