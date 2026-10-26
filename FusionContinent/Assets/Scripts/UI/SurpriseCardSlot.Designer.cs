using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:4118d5cb-97bd-4fe0-a5c6-e1a41818c29d
	public partial class SurpriseCardSlot
	{
		public const string Name = "SurpriseCardSlot";
		
		[SerializeField]
		public RectTransform DeBuffCard;
		
		private SurpriseCardSlotData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			DeBuffCard = null;
			
			mData = null;
		}
		
		public SurpriseCardSlotData Data
		{
			get
			{
				return mData;
			}
		}
		
		SurpriseCardSlotData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new SurpriseCardSlotData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
