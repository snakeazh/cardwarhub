using System.Collections.Generic;
using Config;
using Cysharp.Threading.Tasks;
using Game.Realm;
using QFramework;
using ScrollView;
using Spine.Unity;
using TMPro;
using UIFramework;
using UnityEngine;
using UnityEngine.UI;
using IUIData = UIFramework.IUIData;

namespace FusionContinent
{
	[PrefabPath("RealmRoom")]
	public class RealmRoomNew : UIPanel<RealmRoomViewModel>
	{
		private RealmRoom mOldPanel;
		private UIRewardItem mRewardItemTemplate;
		private List<UIRewardItem> mRewardItems = new List<UIRewardItem>();
		private MonsterEntryItem mEntryItemTemplate;
		private List<MonsterEntryItem> mEntryItems = new List<MonsterEntryItem>();
		private string mLoadedBossSpine;

		void Awake()
		{
			mOldPanel = GetComponent<RealmRoom>();
			if (mOldPanel == null)
			{
				LogTool.LogError("[RealmRoomNew] 找不到 RealmRoom 组件");
				return;
			}

			mOldPanel.enabled = false;

			mRewardItemTemplate = mOldPanel.BasicRewards.GetComponentInChildren<UIRewardItem>(true);
			mRewardItemTemplate.gameObject.SetActive(false);

			mEntryItemTemplate = mOldPanel.CurLevelEffectList.GetComponentInChildren<MonsterEntryItem>(true);
			mEntryItemTemplate.gameObject.SetActive(false);
		}

		protected override void OnBindViewModel(RealmRoomViewModel vm)
		{
			mOldPanel.BackBtn.onClick.AddListener(OnBackClicked);
			mOldPanel.ShopBtn.onClick.AddListener(OnShopClicked);
			mOldPanel.Ticket.onClick.AddListener(OnTicketClicked);
			mOldPanel.RuleBtn.onClick.AddListener(OnRuleClicked);
			mOldPanel.AtkButton.onClick.AddListener(OnAttackClicked);
			mOldPanel.SweepButton.onClick.AddListener(OnSweepClicked);
			mOldPanel.LeftBtn.onClick.AddListener(OnLeftClicked);
			mOldPanel.RightBtn.onClick.AddListener(OnRightClicked);

			vm.FloorNumText.OnValueChanged += OnFloorNumTextChanged;
			vm.TipText.OnValueChanged += OnTipTextChanged;
			vm.EntryDescText.OnValueChanged += OnEntryDescTextChanged;
			vm.ShowSweepButton.OnValueChanged += OnShowSweepButtonChanged;
			vm.BossWaveText.OnValueChanged += OnBossWaveTextChanged;

			vm.OnCloseRequested += OnCloseRequested;
			vm.OnOpenShopRequested += OnOpenShopRequested;
			vm.OnOpenRuneRequested += OnOpenRuneRequested;
			vm.OnDisplayChanged += OnDisplayChanged;
			vm.OnToastRequested += OnToastRequested;

			// mOldPanel.LevelStage.SetUpdateFunc(OnStageRefresh);
			mOldPanel.RewardScroll.SetUpdateFunc(OnUpdateReward);

			SyncBoundTexts(vm);
		}

		protected override void OnUnbindViewModel(RealmRoomViewModel vm)
		{
			mOldPanel.BackBtn.onClick.RemoveListener(OnBackClicked);
			mOldPanel.ShopBtn.onClick.RemoveListener(OnShopClicked);
			mOldPanel.Ticket.onClick.RemoveListener(OnTicketClicked);
			mOldPanel.RuleBtn.onClick.RemoveListener(OnRuleClicked);
			mOldPanel.AtkButton.onClick.RemoveListener(OnAttackClicked);
			mOldPanel.SweepButton.onClick.RemoveListener(OnSweepClicked);
			mOldPanel.LeftBtn.onClick.RemoveListener(OnLeftClicked);
			mOldPanel.RightBtn.onClick.RemoveListener(OnRightClicked);

			vm.FloorNumText.OnValueChanged -= OnFloorNumTextChanged;
			vm.TipText.OnValueChanged -= OnTipTextChanged;
			vm.EntryDescText.OnValueChanged -= OnEntryDescTextChanged;
			vm.ShowSweepButton.OnValueChanged -= OnShowSweepButtonChanged;
			vm.BossWaveText.OnValueChanged -= OnBossWaveTextChanged;

			vm.OnCloseRequested -= OnCloseRequested;
			vm.OnOpenShopRequested -= OnOpenShopRequested;
			vm.OnOpenRuneRequested -= OnOpenRuneRequested;
			vm.OnDisplayChanged -= OnDisplayChanged;
			vm.OnToastRequested -= OnToastRequested;

			// mOldPanel.LevelStage.ShowList(0);
			// mOldPanel.LevelStage.SetUpdateFunc(null);
			mOldPanel.RewardScroll.ShowList(0);
			mOldPanel.RewardScroll.SetUpdateFunc(null);
			ClearBossSpine();
		}

		protected override void OnOpen(IUIData uiData = null)
		{
			VM.RefreshOnShow();
		}

		protected override void OnShow()
		{
			VM.RefreshOnShow();
		}

		private void SyncBoundTexts(RealmRoomViewModel vm)
		{
			mOldPanel.FloorNumText.text = vm.FloorNumText.Value;
			mOldPanel.TipText.text = vm.TipText.Value;
			mOldPanel.MonsterGenText.text = vm.BossWaveText.Value;
			// mOldPanel.RandomEntryDescText.text = vm.EntryDescText.Value;
			mOldPanel.SweepButton.gameObject.SetActive(vm.ShowSweepButton.Value);
			mOldPanel.AtkButton.gameObject.SetActive(!vm.ShowSweepButton.Value);
		}

		#region 按钮

		private void OnBackClicked()
		{
			VM.OnCloseCommand.Execute();
		}

		private void OnShopClicked()
		{
			VM.OnShopCommand.Execute();
		}

		private void OnTicketClicked()
		{
			VM.OnClaimTicketCommand.Execute();
		}

		private void OnRuleClicked()
		{
			var ruleVm = new RealmRoomRuleViewModel();
			ruleVm.OnCreate();
			ruleVm.Initialize("规则内容", "所有敌人移动速度、攻击速度+100%");
			UIFramework.UIManager.OpenPanelAsync<RealmRoomRuleNew, RealmRoomRuleViewModel>(
				ruleVm, UIFramework.UILevel.PopUI).Forget();
		}

		private void OnRuneClicked()
		{
			VM.OnRuneCommand.Execute();
		}

		private void OnAttackClicked()
		{
			VM.OnAttackCommand.Execute();
		}

		private void OnSweepClicked()
		{
			VM.OnSweepCommand.Execute();
		}

		private void OnLeftClicked()
		{
			VM.OnSelectFloorCommand.Execute(VM.SelectedFloor.Value - 1);
		}

		private void OnRightClicked()
		{
			VM.OnSelectFloorCommand.Execute(VM.SelectedFloor.Value + 1);
		}

		#endregion

		#region VM 事件 / 属性

		private void OnCloseRequested()
		{
			CloseSelf();
		}

		private void OnOpenShopRequested()
		{
			var vm = new Game.Shop.ChallengeShopListViewModel();
			vm.OnCreate();
			vm.Initialize(RealmService.RealmShopId);
			UIFramework.UIManager.OpenPanelAsync<ChallengeShopListNew, Game.Shop.ChallengeShopListViewModel>(
				vm,
				UIFramework.UILevel.PopUI).Forget();
		}

		private void OnOpenRuneRequested()
		{
			UIKit.OpenPanel<MonsterEntryDetail>(QFramework.UILevel.PopUI);
		}

		private void OnToastRequested(string message)
		{
			Toast.Show(message);
		}

		private void OnFloorNumTextChanged(string oldValue, string newValue)
		{
			mOldPanel.FloorNumText.text = newValue;
		}

		private void OnTipTextChanged(string oldValue, string newValue)
		{
			mOldPanel.TipText.text = newValue;
		}

		private void OnBossWaveTextChanged(string oldValue, string newValue)
		{
			mOldPanel.MonsterGenText.text = newValue;
		}

		private void OnEntryDescTextChanged(string oldValue, string newValue)
		{
			// mOldPanel.RandomEntryDescText.text = newValue;
		}

		private void OnShowSweepButtonChanged(bool oldValue, bool newValue)
		{
			mOldPanel.SweepButton.gameObject.SetActive(newValue);
			mOldPanel.AtkButton.gameObject.SetActive(!newValue);
		}

		private void OnDisplayChanged()
		{
			var floors = VM.StageConfigs.Value;
			// mOldPanel.LevelStage.ShowList(floors.Count);

			mOldPanel.RewardScroll.ShowList(VM.RandomRewards.Value.Count);

			RefreshBasicRewards();
			RefreshHaveItemNode(floors);
			RefreshTicket();
			RefreshDifficultTips(floors);
			RefreshFloorEntries();
			RefreshLeftRightButtons();
			RefreshBossSpine();
		}

		private void RefreshTicket()
		{
			int itemId = VM.TicketItemId.Value;
			var iconTransform = mOldPanel.Ticket.transform.Find("Icon");
			if (iconTransform == null)
			{
				LogTool.LogError("[RealmRoomNew] Ticket 下找不到 Icon 节点");
				return;
			}

			var iconImage = iconTransform.GetComponent<Image>();
			if (itemId > 0)
			{
				var itemCfg = DataConfigManager.Tables.TbItemConfig.GetOrDefault(itemId);
				iconImage.sprite = Global.ItemIconSpriteAtlas.GetSpriteExt(itemCfg.Icon);
			}

			var timeTransform = mOldPanel.Ticket.transform.Find("Time");
			if (timeTransform == null)
			{
				LogTool.LogError("[RealmRoomNew] Ticket 下找不到 Time 节点");
				return;
			}

			var timeBind = timeTransform.GetComponent<TimeRecordBind>();
			if (timeBind == null)
			{
				timeBind = timeTransform.gameObject.AddComponent<TimeRecordBind>();
			}

			timeBind.SetData(VM.TicketNextClaimTime.Value);
		}

		private void RefreshBossSpine()
		{
			string spineName = VM.BossSpineName.Value;
			float spineScale = VM.BossSpineScale.Value;

			if (spineName == mLoadedBossSpine)
				return;

			ClearBossSpine();
			mLoadedBossSpine = spineName;

			if (string.IsNullOrEmpty(spineName))
				return;

			var spine = Global.ResLoader.LoadSync<GameObject>(spineName).Instantiate().transform;
			spine.SetParent(mOldPanel.BossSpinePos);
			spine.localScale = Vector3.one*100f;
			spine.localPosition = Vector3.zero;
			// spine.GetComponent<>()
			spine.GetComponentInChildren<MeshRenderer>().sortingLayerName = "Default";
			spine.GetComponentInChildren<MeshRenderer>().sortingOrder = 820;
			spine.GetComponentInChildren<MeshRenderer>().Layer("UI");
			// mOldPanel.BossSpinePos.localScale = spineScale > 0 ? Vector3.one * spineScale : Vector3.one;
		}

		private void ClearBossSpine()
		{
			mLoadedBossSpine = null;
			var parent = mOldPanel.BossSpinePos;
			for (int i = parent.childCount - 1; i >= 0; i--)
			{
				Destroy(parent.GetChild(i).gameObject);
			}
		}

		private void RefreshLeftRightButtons()
		{
			int minFloor = VM.StageConfigs.Value.Count > 0 ? VM.StageConfigs.Value[0].Id : 1;
			int maxFloor = VM.MaxFloor.Value;
			int selected = VM.SelectedFloor.Value;

			mOldPanel.LeftBtn.gameObject.SetActive(selected > minFloor);
			mOldPanel.RightBtn.gameObject.SetActive(selected < maxFloor && selected < VM.CurrentFloor.Value);
		}

		private void RefreshFloorEntries()
		{
			var entries = VM.FloorEntries.Value;

			for (int i = 0; i < mEntryItems.Count; i++)
			{
				mEntryItems[i].gameObject.SetActive(i < entries.Count);
			}

			for (int i = mEntryItems.Count; i < entries.Count; i++)
			{
				var obj = Instantiate(mEntryItemTemplate.gameObject, mOldPanel.CurLevelEffectList);
				obj.SetActive(true);
				mEntryItems.Add(obj.GetComponent<MonsterEntryItem>());
			}

			for (int i = 0; i < entries.Count; i++)
			{
				mEntryItems[i].SetData(entries[i]);
			}
		}

		private void RefreshDifficultTips(List<HiddenRealmTowerConfig> floors)
		{
			var selectConfig = floors.Find(ele => ele.Id == VM.SelectedFloor.Value);
			if (selectConfig == null)
				return;

			var tipText = mOldPanel.DifficultTips.transform.Find("Text").GetComponent<TextMeshProUGUI>();
			tipText.text = selectConfig.Desc;
		}

		private void RefreshHaveItemNode(List<HiddenRealmTowerConfig> floors)
		{
			var selectConfig = floors.Find(ele => ele.Id == VM.SelectedFloor.Value);
			if (selectConfig == null || selectConfig.StageId_Ref == null)
				return;

			var consumeItems = selectConfig.StageId_Ref.ConsumeItem;
			if (consumeItems.Count > 0)
			{
				mOldPanel.HaveItemNode.SetData(consumeItems[0].Id);
			}
		}

		private void RefreshBasicRewards()
		{
			var rewards = VM.BasicRewards.Value;

			for (int i = 0; i < mRewardItems.Count; i++)
			{
				mRewardItems[i].gameObject.SetActive(i < rewards.Count);
			}

			for (int i = mRewardItems.Count; i < rewards.Count; i++)
			{
				var obj = Instantiate(mRewardItemTemplate.gameObject, mOldPanel.BasicRewards);
				obj.SetActive(true);
				mRewardItems.Add(obj.GetComponent<UIRewardItem>());
			}

			for (int i = 0; i < rewards.Count; i++)
			{
				mRewardItems[i].SetData(rewards[i]);
			}

		
		}

		#endregion

		#region ScrollView

		private void OnStageRefresh(int dataIndex, ScrollItem scrollItem)
		{
			var floors = VM.StageConfigs.Value;
			var cfg = floors[dataIndex];
			var item = (BagScrollItem)scrollItem;
			var obj = scrollItem.transform;
			var lvState = obj.Find("LevelImg");

			scrollItem.OnSpawn();
			item.OnClick = () => VM.OnSelectFloorCommand.Execute(cfg.Id);

			var currentLevel = lvState.Find("currentLevel");
			var preLevel = lvState.Find("preLevel");
			var nextLevel = lvState.Find("nextLevel");

			obj.Find("LevelText").GetComponent<TextMeshProUGUI>().text = cfg.Id.ToString();

			bool isSelected = cfg.Id == VM.SelectedFloor.Value;
			currentLevel.gameObject.SetActive(isSelected);
			if (isSelected)
			{
				preLevel.gameObject.SetActive(false);
				nextLevel.gameObject.SetActive(false);
			}
			else
			{
				preLevel.gameObject.SetActive(cfg.Id < VM.CurrentFloor.Value);
				nextLevel.gameObject.SetActive(cfg.Id >= VM.CurrentFloor.Value);
			}

			obj.Find("Arrow").gameObject.SetActive(cfg.Id != VM.MaxFloor.Value && cfg.Id >= VM.CurrentFloor.Value);
			obj.Find("ArrowLight").gameObject.SetActive(cfg.Id != VM.MaxFloor.Value && cfg.Id < VM.CurrentFloor.Value);
		}

		private void OnUpdateReward(int dataIndex, ScrollItem scrollItem)
		{
			var rewards = VM.RandomRewards.Value;
			if (dataIndex >= 0 && dataIndex < rewards.Count)
			{
				scrollItem.GetComponent<DropRewardItem>().SetData(rewards[dataIndex]);
			}
		}

		#endregion
	}
}
