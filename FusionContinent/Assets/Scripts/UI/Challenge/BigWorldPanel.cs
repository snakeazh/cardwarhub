using System.Collections.Generic;
using Config;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using FusionContinent.Model;
using FusionContinent.Sounds;
using QFramework;
using UnityEngine;
using UnityEngine.UI;
using UIManager = UIFramework.UIManager;
using Game.RedDot;

namespace FusionContinent
{
	public class BigWorldPanelData : UIPanelData
	{
	}
	public partial class BigWorldPanel : UIPanel
	{
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as BigWorldPanelData ?? new BigWorldPanelData();
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
			// showSeq.Kill(true);
		}

		protected override void OnClose()
		{
		}
	}
}
