using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:2b105aa5-6511-4849-9c55-4064799063eb
	public partial class BackpackPanel
	{
		public const string Name = "BackpackPanel";
		
		[SerializeField]
		public RectTransform OpenAnimation;
		[SerializeField]
		public RectTransform CloseAnimation;
		[SerializeField]
		public UnityEngine.UI.Button bg;
		[SerializeField]
		public UnityEngine.UI.Button CloseBtn;
		[SerializeField]
		public RectTransform ItemShow;
		[SerializeField]
		public UnityEngine.UI.Image ItemShowbg;
		[SerializeField]
		public UnityEngine.UI.Image ItemShowicon;
		[SerializeField]
		public TMPro.TextMeshProUGUI ItemShowname;
		[SerializeField]
		public TMPro.TextMeshProUGUI ItemShowDesc;
		[SerializeField]
		public UnityEngine.UI.Button ItemShowUsebtn;
		[SerializeField]
		public UnityEngine.UI.Button ItemShowObtainbtn;
		[SerializeField]
		public ScrollView.UIVerticalScrollViewLock ItemScrollview;
		
		private BackpackPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			OpenAnimation = null;
			CloseAnimation = null;
			bg = null;
			CloseBtn = null;
			ItemShow = null;
			ItemShowbg = null;
			ItemShowicon = null;
			ItemShowname = null;
			ItemShowDesc = null;
			ItemShowUsebtn = null;
			ItemShowObtainbtn = null;
			ItemScrollview = null;
			
			mData = null;
		}
		
		public BackpackPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		BackpackPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new BackpackPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
