using System;
using System.Collections.Generic;
using System.Linq;
using Config;
using Cysharp.Threading.Tasks;
using FusionContinent;
using FusionContinent.Model;
using Game.Ticket;
using Game.UserTool;
using UIFramework;
using UIFramework.Services;

namespace Game.Realm
{
	public class RealmRoomViewModel : ViewModel
	{
		#region 属性

		public BindableProperty<int> SelectedFloor { get; } = new BindableProperty<int>(0);
		public BindableProperty<int> CurrentFloor { get; } = new BindableProperty<int>(0);
		public BindableProperty<int> MaxFloor { get; } = new BindableProperty<int>(0);
		public BindableProperty<string> FloorNumText { get; } = new BindableProperty<string>("0");
		public BindableProperty<string> TipText { get; } = new BindableProperty<string>("");
		public BindableProperty<string> EntryDescText { get; } = new BindableProperty<string>("");
		public BindableProperty<bool> ShowSweepButton { get; } = new BindableProperty<bool>(false);
		public BindableProperty<List<HiddenRealmTowerConfig>> StageConfigs { get; } =
			new BindableProperty<List<HiddenRealmTowerConfig>>(new List<HiddenRealmTowerConfig>());
		public BindableProperty<List<RewardAndNum>> BasicRewards { get; } =
			new BindableProperty<List<RewardAndNum>>(new List<RewardAndNum>());
		public BindableProperty<List<RewardAndNumAndPro>> RandomRewards { get; } =
			new BindableProperty<List<RewardAndNumAndPro>>(new List<RewardAndNumAndPro>());
		public BindableProperty<List<HiddenRealmFloorEntryData>> FloorEntries { get; } =
			new BindableProperty<List<HiddenRealmFloorEntryData>>(new List<HiddenRealmFloorEntryData>());
		public BindableProperty<string> BossSpineName { get; } = new BindableProperty<string>("");
		public BindableProperty<float> BossSpineScale { get; } = new BindableProperty<float>(1f);
		public BindableProperty<int> BossWaveNumber { get; } = new BindableProperty<int>(0);
		public BindableProperty<string> BossWaveText { get; } = new BindableProperty<string>("");
		public BindableProperty<int> TicketItemId { get; } = new BindableProperty<int>(0);
		public BindableProperty<int> TicketId { get; } = new BindableProperty<int>(0);
		public BindableProperty<long> TicketNextClaimTime { get; } = new BindableProperty<long>(0);

		#endregion

		#region 命令

		public Command OnCloseCommand { get; private set; }
		public Command OnShopCommand { get; private set; }
		public Command OnRuneCommand { get; private set; }
		public Command OnAttackCommand { get; private set; }
		public Command OnSweepCommand { get; private set; }
		public Command OnClaimTicketCommand { get; private set; }
		public Command<int> OnSelectFloorCommand { get; private set; }

		#endregion

		#region 依赖

		private RealmService mService;
		private TicketService mTicketService;

		#endregion

		#region 事件

		public Action OnCloseRequested;
		public Action OnOpenShopRequested;
		public Action OnOpenRuneRequested;
		public Action OnDisplayChanged;
		public Action<string> OnToastRequested;

		#endregion

		public override void OnCreate()
		{
			base.OnCreate();
			mService = GameServiceManager.Get<RealmService>();
			mTicketService = GameServiceManager.Get<TicketService>();

			OnCloseCommand = new Command(OnClose);
			OnShopCommand = new Command(OnShop);
			OnRuneCommand = new Command(OnRune);
			OnAttackCommand = new Command(OnAttack);
			OnSweepCommand = new Command(OnSweep);
			OnClaimTicketCommand = new Command(OnClaimTicket);
			OnSelectFloorCommand = new Command<int>(OnSelectFloor);

			LoadTicket();
			LoadTower();
		}

		/// <summary>
		/// 秘境界面顶部工具栏：显示钻石、金币、体力，不显示头像。
		/// （tbresourceshowconfig 未配置 HiddenRealmPage 条目，这里直接声明显示内容。）
		/// </summary>
		public override UserToolData GetUserToolConfig()
		{
			return UserToolData.OnlyItems(new List<int> { 1010002, 1010001, 1010006 });
		}

		public void RefreshOnShow()
		{
			LoadTicket();
			LoadTower();
			SelectedFloor.Value = Math.Min(CurrentFloor.Value, MaxFloor.Value);
			ApplySelectedFloor();
		}

		private void LoadTicket()
		{
			var ticketConfig = mTicketService.GetTicketConfigByType(StageType.GreaterRift);
			if (ticketConfig == null || ticketConfig.DailyRefreshItem.Count == 0)
			{
				TicketId.Value = 0;
				TicketItemId.Value = 0;
				TicketNextClaimTime.Value = 0;
				return;
			}

			TicketId.Value = ticketConfig.Id;
			TicketItemId.Value = ticketConfig.DailyRefreshItem[0].Id;
			RefreshTicketTime(ticketConfig.Id);
		}

		private void RefreshTicketTime(int ticketId)
		{
			var ticket = mTicketService.GetTicket(ticketId);
			TicketNextClaimTime.Value = ticket != null ? ticket.NextClaimTime : 0;
		}

		private void LoadTower()
		{
			var floors = mService.GetAllFloorConfigs();
			StageConfigs.Value = floors;
			MaxFloor.Value = floors.Last().Id;
			CurrentFloor.Value = mService.GetCurrentFloor();
		}

		private void OnClose()
		{
			if (OnCloseRequested != null)
			{
				OnCloseRequested.Invoke();
			}
		}

		private void OnShop()
		{
			if (OnOpenShopRequested != null)
			{
				OnOpenShopRequested.Invoke();
			}
		}

		private void OnRune()
		{
			if (OnOpenRuneRequested != null)
			{
				OnOpenRuneRequested.Invoke();
			}
		}

		private void OnSelectFloor(int floor)
		{
			if (floor > CurrentFloor.Value)
			{
				RaiseToast("请先通关上一层级！");
				return;
			}

			SelectedFloor.Value = floor;
			ApplySelectedFloor();
		}

		private void ApplySelectedFloor()
		{
			FloorNumText.Value = $"第{SelectedFloor.Value}层";
			ShowSweepButton.Value = mService.IsFloorCleared(SelectedFloor.Value);

			var selectConfig = mService.GetFloorConfig(StageConfigs.Value, SelectedFloor.Value);
			if (selectConfig == null)
			{
				ClearBossDisplay();
				RaiseDisplayChanged();
				return;
			}

			EntryDescText.Value = "Lv." + selectConfig.MonsterEntryLevel;

			if (mService.GetPlayerTower() != null)
			{
				TipText.Value = "剩余次数：" + mService.GetRemainingSweepCount();
			}

			BasicRewards.Value = selectConfig.BasicRewards;
			RandomRewards.Value = selectConfig.RandomRewards;

			var floorData = mService.GetFloorData(SelectedFloor.Value);
			FloorEntries.Value = floorData != null ? floorData.Entries : new List<HiddenRealmFloorEntryData>();

			var bossCfg = mService.FindStageBoss(selectConfig.StageId, out int waveNumber);
			if (bossCfg != null)
			{
				BossSpineName.Value = bossCfg.Spine;
				BossSpineScale.Value = bossCfg.SpineScale;
				BossWaveNumber.Value = waveNumber;
				BossWaveText.Value = $"第{waveNumber}波出现";
			}
			else
			{
				ClearBossDisplay();
			}

			RaiseDisplayChanged();
		}

		private void ClearBossDisplay()
		{
			BossSpineName.Value = "";
			BossSpineScale.Value = 1f;
			BossWaveNumber.Value = 0;
			BossWaveText.Value = "";
		}

		private void OnAttack()
		{
			var selectConfig = mService.GetFloorConfig(StageConfigs.Value, SelectedFloor.Value);
			var error = mService.ValidateBattle(selectConfig);
			if (error != null)
			{
				RaiseToast(error);
				return;
			}

			mService.PrepareBattle(selectConfig.StageId);
		}

		private void OnSweep()
		{
			var selectConfig = mService.GetFloorConfig(StageConfigs.Value, SelectedFloor.Value);
			var error = mService.ValidateBattle(selectConfig);
			if (error != null)
			{
				RaiseToast(error);
				return;
			}

			mService.Sweep(selectConfig.StageId, ApplySelectedFloor);
		}

		private void OnClaimTicket()
		{
			int ticketId = TicketId.Value;
			if (ticketId <= 0)
			{
				RaiseToast("当前不可领取");
				return;
			}

			if (!mTicketService.CanClaim(ticketId))
			{
				RaiseToast("当前不可领取");
				return;
			}

			ClaimTicketAsync(ticketId).Forget();
		}

		private async UniTaskVoid ClaimTicketAsync(int ticketId)
		{
			bool success = await mTicketService.ClaimTicketAsync(ticketId);
			if (!success)
			{
				return;
			}

			RefreshTicketTime(ticketId);
			RaiseDisplayChanged();
		}

		private void RaiseToast(string message)
		{
			if (OnToastRequested != null)
			{
				OnToastRequested.Invoke(message);
			}
		}

		private void RaiseDisplayChanged()
		{
			if (OnDisplayChanged != null)
			{
				OnDisplayChanged.Invoke();
			}
		}
	}
}
