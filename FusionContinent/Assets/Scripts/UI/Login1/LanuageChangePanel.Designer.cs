using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:bdddd26d-b1fb-4fdb-a2b7-95d270301239
	public partial class LanuageChangePanel
	{
		public const string Name = "LanuageChangePanel";
		
		[SerializeField]
		public RectTransform OpenAnimation;
		[SerializeField]
		public RectTransform CloseAnimation;
		[SerializeField]
		public RectTransform LanuageChangeScroll;
		[SerializeField]
		public RectTransform LanuageBtn1;
		
		private LanuageChangePanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			OpenAnimation = null;
			CloseAnimation = null;
			LanuageChangeScroll = null;
			LanuageBtn1 = null;
			
			mData = null;
		}
		
		public LanuageChangePanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		LanuageChangePanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new LanuageChangePanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
