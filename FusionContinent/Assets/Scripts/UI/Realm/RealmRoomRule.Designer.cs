using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:8ca1b321-3588-47d1-b3d9-848de657143c
	public partial class RealmRoomRule
	{
		public const string Name = "RealmRoomRule";
		
		[SerializeField]
		public UnityEngine.UI.Button Mask;
		[SerializeField]
		public TMPro.TextMeshProUGUI Desc;
		
		private RealmRoomRuleData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			Mask = null;
			Desc = null;
			
			mData = null;
		}
		
		public RealmRoomRuleData Data
		{
			get
			{
				return mData;
			}
		}
		
		RealmRoomRuleData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new RealmRoomRuleData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
