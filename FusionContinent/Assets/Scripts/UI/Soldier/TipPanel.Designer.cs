using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:3e9f7b01-9e1a-4fde-a0ac-bab7dc484a0d
	public partial class TipPanel
	{
		public const string Name = "TipPanel";
		
		[SerializeField]
		public UnityEngine.UI.Button Mask;
		[SerializeField]
		public UnityEngine.UI.Image Title;
		[SerializeField]
		public TMPro.TextMeshProUGUI Detail;
		
		private TipPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			Mask = null;
			Title = null;
			Detail = null;
			
			mData = null;
		}
		
		public TipPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		TipPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new TipPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
