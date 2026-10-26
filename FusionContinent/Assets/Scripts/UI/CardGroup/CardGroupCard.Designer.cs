using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:1e2fb0cc-05a3-4a39-865f-20129d2ec401
	public partial class CardGroupCard
	{
		public const string Name = "CardGroupCard";
		
		[SerializeField]
		public UnityEngine.UI.Image Bg;
		[SerializeField]
		public UnityEngine.UI.Image SkillCardIcon;
		[SerializeField]
		public UnityEngine.UI.Image Locked;
		
		private CardGroupCardData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			Bg = null;
			SkillCardIcon = null;
			Locked = null;
			
			mData = null;
		}
		
		public CardGroupCardData Data
		{
			get
			{
				return mData;
			}
		}
		
		CardGroupCardData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new CardGroupCardData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
