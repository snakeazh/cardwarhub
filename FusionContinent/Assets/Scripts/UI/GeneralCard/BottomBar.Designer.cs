using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:7eadb8d4-b035-4e7b-ae7e-8bec09dccf38
	public partial class BottomBar
	{
		public const string Name = "BottomBar";
		
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress ExitBtn;
		[SerializeField]
		public UnityEngine.UI.ScrollRect ChangeScroll;
		[SerializeField]
		public FusionContinent.UI.Components.UISelectableToggle ConbineBtn;
		[SerializeField]
		public FusionContinent.UI.Components.UISelectableToggle ResonanceBtn;
		[SerializeField]
		public FusionContinent.UI.Components.UISelectableToggle ResolveBtn;
		
		private BottomBarData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			ExitBtn = null;
			ChangeScroll = null;
			ConbineBtn = null;
			ResonanceBtn = null;
			ResolveBtn = null;
			
			mData = null;
		}
		
		public BottomBarData Data
		{
			get
			{
				return mData;
			}
		}
		
		BottomBarData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new BottomBarData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
