using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:74299029-03ce-4e7c-8414-963e48b8ae70
	public partial class MonsterCardAtlasSlot
	{
		public const string Name = "MonsterCardAtlasSlot";
		
		[SerializeField]
		public UnityEngine.UI.Image Bg;
		[SerializeField]
		public UnityEngine.UI.Image Icon;
		[SerializeField]
		public TMPro.TextMeshProUGUI CardName;
		[SerializeField]
		public UnityEngine.Transform Lock;
		[SerializeField]
		public UnityEngine.UI.Image Tag;
		[SerializeField]
		public TMPro.TextMeshProUGUI TagText;
		[SerializeField]
		public UnityEngine.UI.Button CardBtn;
		
		private MonsterCardAtlasSlotData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			Bg = null;
			Icon = null;
			CardName = null;
			Lock = null;
			Tag = null;
			TagText = null;
			CardBtn = null;
			
			mData = null;
		}
		
		public MonsterCardAtlasSlotData Data
		{
			get
			{
				return mData;
			}
		}
		
		MonsterCardAtlasSlotData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new MonsterCardAtlasSlotData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
