using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:ae6bc387-433e-4e60-9645-f7e7463c62b9
	public partial class BattleSurpriseCardPanel
	{
		public const string Name = "BattleSurpriseCardPanel";
		
		[SerializeField]
		public UnityEngine.RectTransform SurpriseCards;
		
		private BattleSurpriseCardPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			SurpriseCards = null;
			
			mData = null;
		}
		
		public BattleSurpriseCardPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		BattleSurpriseCardPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new BattleSurpriseCardPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
