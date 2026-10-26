using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:7d179885-cb64-43b7-9fe7-cfd489c08112
	public partial class MonsterDetailPopPanel
	{
		public const string Name = "MonsterDetailPopPanel";
		
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress Mask;
		[SerializeField]
		public RectTransform PosNode;
		[SerializeField]
		public TMPro.TextMeshProUGUI MonsterName;
		[SerializeField]
		public TMPro.TextMeshProUGUI MonsterDetail;
		
		private MonsterDetailPopPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			Mask = null;
			PosNode = null;
			MonsterName = null;
			MonsterDetail = null;
			
			mData = null;
		}
		
		public MonsterDetailPopPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		MonsterDetailPopPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new MonsterDetailPopPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
