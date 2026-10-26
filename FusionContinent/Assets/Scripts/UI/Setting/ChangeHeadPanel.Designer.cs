using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:b3f36e8c-ab97-419b-be81-05b5508eeda5
	public partial class ChangeHeadPanel
	{
		public const string Name = "ChangeHeadPanel";
		
		[SerializeField]
		public RectTransform OpenAnimation;
		[SerializeField]
		public RectTransform CloseAnimation;
		[SerializeField]
		public UnityEngine.UI.Button Mask;
		[SerializeField]
		public UnityEngine.RectTransform PlayerHead;
		[SerializeField]
		public UnityEngine.UI.Toggle Head;
		[SerializeField]
		public UnityEngine.UI.Toggle HeadFrame;
		[SerializeField]
		public ScrollView.UIVerticalScrollViewLock HeadSCView;
		[SerializeField]
		public ScrollView.UIVerticalScrollViewLock HeadFrameSCView;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress WearBtn;
		[SerializeField]
		public UnityEngine.UI.Button CloseBtn;
		
		private ChangeHeadPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			OpenAnimation = null;
			CloseAnimation = null;
			Mask = null;
			PlayerHead = null;
			Head = null;
			HeadFrame = null;
			HeadSCView = null;
			HeadFrameSCView = null;
			WearBtn = null;
			CloseBtn = null;
			
			mData = null;
		}
		
		public ChangeHeadPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		ChangeHeadPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new ChangeHeadPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
