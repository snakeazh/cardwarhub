using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:32113415-11d0-4fcf-a80e-c254b19dba17
	public partial class ItemShowPanel
	{
		public const string Name = "ItemShowPanel";
		
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress Mask;
		[SerializeField]
		public UnityEngine.UI.Image NotActivated;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress RewardEntity;
		[SerializeField]
		public TMPro.TextMeshProUGUI NameText;
		[SerializeField]
		public TMPro.TextMeshProUGUI TypeText;
		[SerializeField]
		public TMPro.TextMeshProUGUI NumText;
		[SerializeField]
		public TMPro.TextMeshProUGUI DescText;
		[SerializeField]
		public RectTransform CloseAnimation;
		[SerializeField]
		public RectTransform OpenAnimation;
		
		private ItemShowPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			Mask = null;
			NotActivated = null;
			RewardEntity = null;
			NameText = null;
			TypeText = null;
			NumText = null;
			DescText = null;
			CloseAnimation = null;
			OpenAnimation = null;
			
			mData = null;
		}
		
		public ItemShowPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		ItemShowPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new ItemShowPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
