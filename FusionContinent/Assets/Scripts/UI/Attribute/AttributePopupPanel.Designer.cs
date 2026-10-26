using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:a781c298-0a57-4176-8004-cfc43805f61c
	public partial class AttributePopupPanel
	{
		public const string Name = "AttributePopupPanel";
		
		[SerializeField]
		public RectTransform OpenAnimation;
		[SerializeField]
		public RectTransform CloseAnimation;
		[SerializeField]
		public UnityEngine.UI.Button Mask;
		[SerializeField]
		public UnityEngine.UI.Button CloseBtn;
		[SerializeField]
		public ScrollView.UIVerticalScrollViewLock AttrScrollview;
		
		private AttributePopupPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			OpenAnimation = null;
			CloseAnimation = null;
			Mask = null;
			CloseBtn = null;
			AttrScrollview = null;
			
			mData = null;
		}
		
		public AttributePopupPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		AttributePopupPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new AttributePopupPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
