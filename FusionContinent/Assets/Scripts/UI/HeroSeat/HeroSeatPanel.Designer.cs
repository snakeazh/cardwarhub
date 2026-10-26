using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:480a270f-e79b-4935-aaa0-61c92667bf1e
	public partial class HeroSeatPanel
	{
		public const string Name = "HeroSeatPanel";
		
		[SerializeField]
		public RectTransform OpenAnimation;
		[SerializeField]
		public RectTransform CloseAnimation;
		[SerializeField]
		public UnityEngine.UI.Button Quit;
		[SerializeField]
		public UnityEngine.UI.Image CityWallIcon;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress UpGradeBtn;
		[SerializeField]
		public TMPro.TextMeshProUGUI CityWallNameText;
		[SerializeField]
		public RectTransform MechanicList;
		[SerializeField]
		public ScrollView.UIVerticalScrollViewLock MechanicSCView;
		[SerializeField]
		public UnityEngine.UI.Image ConsumeNode;
		[SerializeField]
		public UIConsumeItem Consume;
		[SerializeField]
		public UnityEngine.UI.Button CloseBtn;
		[SerializeField]
		public TMPro.TextMeshProUGUI MechanicStatisticsText;
		
		private HeroSeatPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			OpenAnimation = null;
			CloseAnimation = null;
			Quit = null;
			CityWallIcon = null;
			UpGradeBtn = null;
			CityWallNameText = null;
			MechanicList = null;
			MechanicSCView = null;
			ConsumeNode = null;
			Consume = null;
			CloseBtn = null;
			MechanicStatisticsText = null;
			
			mData = null;
		}
		
		public HeroSeatPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		HeroSeatPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new HeroSeatPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
