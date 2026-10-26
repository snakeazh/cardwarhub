using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:3030f558-1ca3-497d-83d1-72f0ceee3a4e
	public partial class GeneralCardResolvePanel
	{
		public const string Name = "GeneralCardResolvePanel";
		
		[SerializeField]
		public RectTransform TopAnimationLeft;
		[SerializeField]
		public RectTransform TopAnimationRight;
		[SerializeField]
		public TMPro.TMP_Dropdown CardFilter;
		[SerializeField]
		public ScrollView.UIVerticalScrollViewLock GeneralCardScroll;
		[SerializeField]
		public UnityEngine.UI.Button ResolveBtn;
		[SerializeField]
		public RectTransform GetItems;
		[SerializeField]
		public UnityEngine.UI.Button CloseBtn;
		
		private GeneralCardResolvePanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			TopAnimationLeft = null;
			TopAnimationRight = null;
			CardFilter = null;
			GeneralCardScroll = null;
			ResolveBtn = null;
			GetItems = null;
			CloseBtn = null;
			
			mData = null;
		}
		
		public GeneralCardResolvePanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		GeneralCardResolvePanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new GeneralCardResolvePanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
