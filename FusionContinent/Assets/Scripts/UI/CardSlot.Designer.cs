using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:7b84bfee-ca22-4c74-b938-726a15030a9f
	public partial class CardSlot
	{
		public const string Name = "CardSlot";
		
		
		private CardSlotData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			
			mData = null;
		}
		
		public CardSlotData Data
		{
			get
			{
				return mData;
			}
		}
		
		CardSlotData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new CardSlotData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
