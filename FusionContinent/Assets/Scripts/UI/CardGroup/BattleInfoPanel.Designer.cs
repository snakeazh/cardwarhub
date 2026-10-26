using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:5413f366-ef59-4d6e-b2ac-bf0ae06ec2b9
	public partial class BattleInfoPanel
	{
		public const string Name = "BattleInfoPanel";
		
		[SerializeField]
		public UnityEngine.UI.Image Img;
		[SerializeField]
		public TMPro.TextMeshProUGUI Desc;
		[SerializeField]
		public UnityEngine.UI.ScrollRect MonsterCardScroll;
		[SerializeField]
		public UnityEngine.UI.ScrollRect RewardItemScroll;
		
		private BattleInfoPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			Img = null;
			Desc = null;
			MonsterCardScroll = null;
			RewardItemScroll = null;
			
			mData = null;
		}
		
		public BattleInfoPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		BattleInfoPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new BattleInfoPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
