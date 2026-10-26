using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:ed48b002-668d-465b-9e2d-c50a2608b92f
	public partial class UserToolPanel
	{
		public const string Name = "UserToolPanel";
		
		[SerializeField]
		public UnityEngine.UI.HorizontalLayoutGroup MoneyList;
		[SerializeField]
		public FusionContinent.UI.ResourceItem ResourceItem;
		[SerializeField]
		public UnityEngine.UI.Button AvatarBtn;
		[SerializeField]
		public PlayerHead PlayerHead;
		[SerializeField]
		public TMPro.TextMeshProUGUI UserName;
		[SerializeField]
		public RectTransform UserLevel;
		[SerializeField]
		public TMPro.TextMeshProUGUI UserLevelText;
		[SerializeField]
		public UnityEngine.UI.Image LevelExpProgressBar;
		[SerializeField]
		public TMPro.TextMeshProUGUI LevelExpText;
		
		private UserToolPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			MoneyList = null;
			ResourceItem = null;
			AvatarBtn = null;
			PlayerHead = null;
			UserName = null;
			UserLevel = null;
			UserLevelText = null;
			LevelExpProgressBar = null;
			LevelExpText = null;
			
			mData = null;
		}
		
		public UserToolPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		UserToolPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new UserToolPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
