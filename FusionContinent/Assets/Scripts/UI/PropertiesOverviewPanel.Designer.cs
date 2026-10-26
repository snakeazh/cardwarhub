using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:85a67916-5884-435a-8791-322d177e58b1
	public partial class PropertiesOverviewPanel
	{
		public const string Name = "PropertiesOverviewPanel";
		
		[SerializeField]
		public UnityEngine.UI.Text CityLordText;
		[SerializeField]
		public UnityEngine.UI.Image GemInlaySlot1;
		[SerializeField]
		public UnityEngine.UI.Text Slot1Text;
		[SerializeField]
		public UnityEngine.UI.Image GemInlaySlot2;
		[SerializeField]
		public UnityEngine.UI.Text Slot2Text;
		[SerializeField]
		public UnityEngine.UI.Image GemInlaySlot3;
		[SerializeField]
		public UnityEngine.UI.Text Slot3Text;
		[SerializeField]
		public UnityEngine.UI.Image GemInlaySlot4;
		[SerializeField]
		public UnityEngine.UI.Text Slot4Text;
		[SerializeField]
		public UnityEngine.UI.Image GemInlaySlot5;
		[SerializeField]
		public UnityEngine.UI.Text Slot5Text;
		[SerializeField]
		public UnityEngine.UI.Image GemInlaySlot6;
		[SerializeField]
		public UnityEngine.UI.Text Slot6Text;
		[SerializeField]
		public UnityEngine.UI.Button CloseBtn;
		
		private PropertiesOverviewPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			CityLordText = null;
			GemInlaySlot1 = null;
			Slot1Text = null;
			GemInlaySlot2 = null;
			Slot2Text = null;
			GemInlaySlot3 = null;
			Slot3Text = null;
			GemInlaySlot4 = null;
			Slot4Text = null;
			GemInlaySlot5 = null;
			Slot5Text = null;
			GemInlaySlot6 = null;
			Slot6Text = null;
			CloseBtn = null;
			
			mData = null;
		}
		
		public PropertiesOverviewPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		PropertiesOverviewPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new PropertiesOverviewPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
