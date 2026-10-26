using System;
using System.Collections.Generic;
using System.Linq;
using Config;
using FusionContinent.Animation;
using FusionContinent.Helper;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using ScrollView;
using UnityEngine.Events;

namespace FusionContinent
{
	public class MonsterAtlasPanelData : UIPanelData
	{
	}
	public partial class MonsterAtlasPanel : UIPanel
	{
		public static MonsterAtlasPanel Default;
		
		private TbCardBaseConfig _cardConfig;
		private List<MonsterBattleConfig> _monsterConfigList;
		
		private void Awake()
		{
			Default = this;
			_cardConfig = DataConfigManager.Tables.TbCardBaseConfig;
			_monsterConfigList = DataConfigManager.Tables.TbMonsterBattleConfig.DataList;
		}

		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as MonsterAtlasPanelData ?? new MonsterAtlasPanelData();
			CloseBtn.onClick.AddListener(OnClose);
			
			CardScrollview.SetUpdateFunc(OnUpdateCardItem);
			
		}

		public void RefreshCardScrollview()
		{
			CardScrollview.ShowList(_monsterConfigList.Count);
		}

		private void OnUpdateCardItem(int dataIndex, ScrollItem scrollItem)
		{
			var bagScrollItem = (BagScrollItem)scrollItem;
			var monsterCfg = _monsterConfigList[dataIndex];
			var cardCfg = _cardConfig.GetOrDefault(monsterCfg.Id);

			var atlasStatus = Global.PlayerAtlas.GetMonsterAtlasStatus(monsterCfg.Id);
			bagScrollItem.GetComponent<MonsterCardAtlasSlot>().SetData(new MonsterCardAtlasSlotData()
			{
				Quality = monsterCfg.MonsterQuality,
				CardName = cardCfg?.Name,
				IconSprite = Global.MonsterIconSpriteAtlas.GetSpriteExt(cardCfg?.Icon),
				//			IconSprite = SpriteHelper.TryLoadPaint(cardCfg?.Icon),
				IsUnlock = atlasStatus != RewardStatus.Default,
				Label = monsterCfg.Label
			});

			bagScrollItem.GetComponent<LocalRedDot>().SetRed(atlasStatus == RewardStatus.CanReceive ? 1 : 0);
			bagScrollItem.OnClick = () =>
			{
				if (atlasStatus == RewardStatus.Default) return;
				UIKit.OpenPanel<MonsterAtlasDetailPanel>(UILevel.PopUI).SetData(monsterCfg.Id);
			};
        }
        
		protected override void OnOpen(IUIData uiData = null)
		{
			CardScrollview.ScrollToIndex(0, ScrollAlignment.Start, false);
			CardScrollview.PlayScaleAnimationDOTween();
		}
		
		protected override void OnShow()
		{
			
			RefreshCardScrollview();
		}
		
		protected override void OnHide()
		{
		}
		
		protected override void RunAnimation(GameObject target, UnityAction complete)
		{
			UIAnimationUtils.PlayAnimations(target, complete);
		}
		
		protected override GameObject GetAnimationCloseNode
		{
			get => this.CloseAnimation.gameObject;
		}

		protected override GameObject GetAnimationOpenNode
		{
			get => this.OpenAnimation.gameObject;
		}
		
		protected override void OnClose()
		{
			RunAnimation(this.GetAnimationCloseNode, () =>
			{
				UIKit.ClosePanel(this);
			});
		}
	}
}
