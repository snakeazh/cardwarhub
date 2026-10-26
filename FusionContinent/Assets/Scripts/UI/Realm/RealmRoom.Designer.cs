using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:544234c4-347c-42b4-9c3c-4ceb84b39be5
	public partial class RealmRoom
	{
		public const string Name = "RealmRoom";
		
		[SerializeField]
		public TMPro.TextMeshProUGUI FloorNumText;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress ShopBtn;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress Ticket;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress RuleBtn;
		[SerializeField]
		public UnityEngine.UI.Image DifficultTips;
		[SerializeField]
		public RectTransform CurLevelEffectList;
		[SerializeField]
		public RectTransform BossSpinePos;
		[SerializeField]
		public TMPro.TextMeshProUGUI MonsterGenText;
		[SerializeField]
		public UnityEngine.UI.Button BackBtn;
		[SerializeField]
		public ScrollView.UIHorizontalScrollViewLock RewardScroll;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress SweepButton;
		[SerializeField]
		public TMPro.TextMeshProUGUI TipText;
		[SerializeField]
		public UnityEngine.UI.Button AtkButton;
		[SerializeField]
		public UnityEngine.UI.Image TicketNum;
		[SerializeField]
		public HaveItemNode HaveItemNode;
		[SerializeField]
		public RectTransform BasicRewards;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress LeftBtn;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress RightBtn;
		
		private RealmRoomData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			FloorNumText = null;
			ShopBtn = null;
			Ticket = null;
			RuleBtn = null;
			DifficultTips = null;
			CurLevelEffectList = null;
			BossSpinePos = null;
			MonsterGenText = null;
			BackBtn = null;
			RewardScroll = null;
			SweepButton = null;
			TipText = null;
			AtkButton = null;
			TicketNum = null;
			HaveItemNode = null;
			BasicRewards = null;
			LeftBtn = null;
			RightBtn = null;
			
			mData = null;
		}
		
		public RealmRoomData Data
		{
			get
			{
				return mData;
			}
		}
		
		RealmRoomData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new RealmRoomData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
