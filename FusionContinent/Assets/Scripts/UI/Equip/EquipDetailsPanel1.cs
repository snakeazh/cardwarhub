using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Localization;
using Config;
using DG.Tweening;
using FusionContinent.Animation;
using FusionContinent.Model;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using TMPro;
using UnityEngine.Events;
using UIFramework.Services;
using Game.CombatPower;
using Game.Equip;

namespace FusionContinent
{
	public class EquipDetailsPanel1Data : UIPanelData
	{
	}
	public partial class EquipDetailsPanel1 : UIPanel
	{


		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as EquipDetailsPanel1Data ?? new EquipDetailsPanel1Data();
		
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

		protected override void OnAnimationOpen()
		{
			base.OnAnimationOpen();

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
