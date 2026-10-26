using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:7058e5db-8802-458f-adac-c51bfad6aca1
	public partial class BattleConfirmPanel
	{
		public const string Name = "BattleConfirmPanel";
		
		[SerializeField]
		public UnityEngine.UI.Button Quit;
		[SerializeField]
		public TMPro.TextMeshProUGUI TitleText;
		[SerializeField]
		public TMPro.TextMeshProUGUI TipText;
		[SerializeField]
		public UnityEngine.UI.Button StaminaBtn;
		[SerializeField]
		public UnityEngine.UI.Button NexFloorBtn;
		
		private BattleConfirmPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			Quit = null;
			TitleText = null;
			TipText = null;
			StaminaBtn = null;
			NexFloorBtn = null;
			
			mData = null;
		}
		
		public BattleConfirmPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		BattleConfirmPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new BattleConfirmPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
