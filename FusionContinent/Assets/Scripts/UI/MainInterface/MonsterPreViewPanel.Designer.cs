using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:f84430dd-65cd-4f6f-9291-1c374a0e318a
	public partial class MonsterPreViewPanel
	{
		public const string Name = "MonsterPreViewPanel";
		
		[SerializeField]
		public RectTransform OpenAnimation;
		[SerializeField]
		public RectTransform CloseAnimation;
		[SerializeField]
		public UnityEngine.UI.Button Quit;
		[SerializeField]
		public ScrollView.UIHorizontalScrollViewLock MonsterSCView;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress ExitBtn;
		[SerializeField]
		public ScrollView.UIHorizontalScrollViewLock RewardSCView;
		
		private MonsterPreViewPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			OpenAnimation = null;
			CloseAnimation = null;
			Quit = null;
			MonsterSCView = null;
			ExitBtn = null;
			RewardSCView = null;
			
			mData = null;
		}
		
		public MonsterPreViewPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		MonsterPreViewPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new MonsterPreViewPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
