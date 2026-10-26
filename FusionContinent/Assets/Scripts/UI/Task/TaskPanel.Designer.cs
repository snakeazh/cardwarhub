using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:121b38be-5b84-4915-a262-29ec9f573a10
	public partial class TaskPanel
	{
		public const string Name = "TaskPanel";
		
		[SerializeField]
		public RectTransform CloseAnimation;
		[SerializeField]
		public RectTransform OpenAnimation;
		[SerializeField]
		public UnityEngine.UI.Button QuitBg;
		[SerializeField]
		public TMPro.TextMeshProUGUI TitleName;
		[SerializeField]
		public RectTransform Activity;
		[SerializeField]
		public RectTransform ActivityBar;
		[SerializeField]
		public UnityEngine.UI.Image GreenBur;
		[SerializeField]
		public UnityEngine.UI.Slider ActivitySlider;
		[SerializeField]
		public RectTransform ActivityIcon;
		[SerializeField]
		public TMPro.TextMeshProUGUI ActivityNum;
		[SerializeField]
		public RectTransform Chest;
		[SerializeField]
		public RectTransform Chest1;
		[SerializeField]
		public RectTransform Chest2;
		[SerializeField]
		public RectTransform Chest3;
		[SerializeField]
		public ScrollView.UIVerticalScrollViewLock TaskScrollView;
		[SerializeField]
		public RectTransform TaskContent;
		[SerializeField]
		public RectTransform ChangeBtn;
		[SerializeField]
		public UnityEngine.UI.Button DailyTaskBtn;
		[SerializeField]
		public UnityEngine.UI.Button WeekTaskBtn;
		[SerializeField]
		public UnityEngine.UI.Button CloseBtn;
		
		private TaskPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			CloseAnimation = null;
			OpenAnimation = null;
			QuitBg = null;
			TitleName = null;
			Activity = null;
			ActivityBar = null;
			GreenBur = null;
			ActivitySlider = null;
			ActivityIcon = null;
			ActivityNum = null;
			Chest = null;
			Chest1 = null;
			Chest2 = null;
			Chest3 = null;
			TaskScrollView = null;
			TaskContent = null;
			ChangeBtn = null;
			DailyTaskBtn = null;
			WeekTaskBtn = null;
			CloseBtn = null;
			
			mData = null;
		}
		
		public TaskPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		TaskPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new TaskPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
