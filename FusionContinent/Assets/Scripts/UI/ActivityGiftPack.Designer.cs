using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:84224a0c-0764-4ab6-b399-cd70559271a6
	public partial class ActivityGiftPack
	{
		public const string Name = "ActivityGiftPack";
		
		[SerializeField]
		public UnityEngine.UI.Image Bg;
		[SerializeField]
		public UnityEngine.UI.Image Bg2;
		[SerializeField]
		public UnityEngine.UI.Image Tab;
		[SerializeField]
		public Assets.Scripts.Localization.LocalizationText TitleText;
		[SerializeField]
		public RectTransform Content;
		[SerializeField]
		public Assets.Scripts.Localization.LocalizationText FreeText;
		[SerializeField]
		public RewardEntity RewardEntityNotDetail;
		[SerializeField]
		public UnityEngine.UI.Button BuyBtn;
		[SerializeField]
		public RectTransform RedDot;
		
		private ActivityGiftPackData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			Bg = null;
			Bg2 = null;
			Tab = null;
			TitleText = null;
			Content = null;
			FreeText = null;
			RewardEntityNotDetail = null;
			BuyBtn = null;
			RedDot = null;
			
			mData = null;
		}
		
		public ActivityGiftPackData Data
		{
			get
			{
				return mData;
			}
		}
		
		ActivityGiftPackData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new ActivityGiftPackData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
