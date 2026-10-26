using System;
using System.Collections.Generic;
using System.Linq;
using Config;
using FusionContinent.Model;
using Helper;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using TMPro;
using ScrollView;
using Game.Soldier;
using Cysharp.Threading.Tasks;

namespace FusionContinent
{
	public class GeneralSkillPanelData : UIPanelData
	{
	}
	public partial class GeneralSkillPanel : UIPanel
	{
		

		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as GeneralSkillPanelData ?? new GeneralSkillPanelData();

		}

		protected override void OnOpen(IUIData uiData = null)
		{
		}

		protected override void OnShow()
		{
			base.OnShow();
		}

		protected override void OnHide()
		{
		}

		protected override void OnClose()
		{
		}
	}
}
