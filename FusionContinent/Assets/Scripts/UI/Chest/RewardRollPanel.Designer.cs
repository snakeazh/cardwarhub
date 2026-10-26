using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:9da411fe-bd6b-41a4-9fea-4526af83f434
	public partial class RewardRollPanel
	{
		public const string Name = "RewardRollPanel";
		
		[SerializeField]
		public RectTransform Content;
		
		private RewardRollPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			Content = null;
			
			mData = null;
		}
		
		public RewardRollPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		RewardRollPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new RewardRollPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
