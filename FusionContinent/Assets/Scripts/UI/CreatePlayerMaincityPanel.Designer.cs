using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:b49624a9-56f5-4da1-bd24-c827837d131f
	public partial class CreatePlayerMaincityPanel
	{
		public const string Name = "CreatePlayerMaincityPanel";
		
		[SerializeField]
		public RectTransform EffectParent;
		[SerializeField]
		public RectTransform PlayerTitleParent;
		[SerializeField]
		public RectTransform PlayerTitleObj;
		[SerializeField]
		public RectTransform Frame;
		[SerializeField]
		public RectTransform ClothingParent;
		[SerializeField]
		public RectTransform ClothingObj;
		[SerializeField]
		public RectTransform AttributeParent;
		[SerializeField]
		public TMPro.TextMeshProUGUI LandAttributetxt;
		[SerializeField]
		public ClothingShow ClothingShow;
		
		private CreatePlayerMaincityPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			EffectParent = null;
			PlayerTitleParent = null;
			PlayerTitleObj = null;
			Frame = null;
			ClothingParent = null;
			ClothingObj = null;
			AttributeParent = null;
			LandAttributetxt = null;
			ClothingShow = null;
			
			mData = null;
		}
		
		public CreatePlayerMaincityPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		CreatePlayerMaincityPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new CreatePlayerMaincityPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
