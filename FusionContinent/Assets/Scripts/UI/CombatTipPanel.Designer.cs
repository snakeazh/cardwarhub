using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:dd14e31d-2bc0-4bf5-a029-f58f1e39273a
	public partial class CombatTipPanel
	{
		public const string Name = "CombatTipPanel";
		
		
		private CombatTipPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			
			mData = null;
		}
		
		public CombatTipPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		CombatTipPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new CombatTipPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
