using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:11cd4f1e-f448-46cb-9499-8710360f74be
	public partial class GuildDetail
	{
		public const string Name = "GuildDetail";
		
		[SerializeField]
		public RectTransform OpenAnimation;
		[SerializeField]
		public RectTransform CloseAnimation;
		
		private GuildDetailData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			OpenAnimation = null;
			CloseAnimation = null;
			
			mData = null;
		}
		
		public GuildDetailData Data
		{
			get
			{
				return mData;
			}
		}
		
		GuildDetailData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new GuildDetailData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
