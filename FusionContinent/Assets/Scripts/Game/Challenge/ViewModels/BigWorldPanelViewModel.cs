using Config;
using FusionContinent;
using FusionContinent.Model;
using UIFramework;
using UIFramework.Services;
using Game.UserTool;

namespace Game.Challenge
{
	public class BigWorldPanelViewModel : ViewModel
	{
		#region 命令
		public Command OnChallengeBossCommand { get; private set; }
		public Command OnEndlessTowerCommand { get; private set; }
		public Command OnMonsterAtlasCommand { get; private set; }
		public Command OnStationCommand { get; private set; }
		public Command OnRankingCommand { get; private set; }
		#endregion

		public override UserToolData GetUserToolConfig()
		{
			return GlobalFunc.GetUserResourceConfig(PageType.WorldPage);
		}

		public override void OnCreate()
		{
			base.OnCreate();

			OnChallengeBossCommand = new Command(OnChallengeBoss);
			OnEndlessTowerCommand = new Command(OnEndlessTower);
			OnMonsterAtlasCommand = new Command(OnMonsterAtlas);
			OnStationCommand = new Command(OnStation);
			OnRankingCommand = new Command(OnRanking);
		}

		#region 命令实现

		private void OnChallengeBoss()
		{
			if (!ModuleFunction.CheckOpen(ModuleFunctionType.WorldBoss))
			{
				return;
			}

			if (OnOpenChallengeBoss != null)
			{
				OnOpenChallengeBoss.Invoke();
			}
		}

		private void OnEndlessTower()
		{
			if (!ModuleFunction.CheckOpen(ModuleFunctionType.EndlessTower))
			{
				return;
			}

			if (!GameServiceManager.Get<Game.Realm.RealmService>().HasUnlockedFloor())
			{
				Toast.Show("暂无已解锁的秘境层，请先通关更多章节");
				return;
			}

			if (OnOpenEndlessTower != null)
			{
				OnOpenEndlessTower.Invoke();
			}
		}

		private void OnMonsterAtlas()
		{
			if (!ModuleFunction.CheckOpen(ModuleFunctionType.MonsterAtlas))
			{
				return;
			}

			if (OnOpenMonsterAtlas != null)
			{
				OnOpenMonsterAtlas.Invoke();
			}
		}

		private void OnStation()
		{
			if (OnOpenStation != null)
			{
				OnOpenStation.Invoke();
			}
		}

		private void OnRanking()
		{
			if (OnOpenRanking != null)
			{
				OnOpenRanking.Invoke();
			}
		}

		#endregion

		#region 事件
		public System.Action OnOpenChallengeBoss;
		public System.Action OnOpenEndlessTower;
		public System.Action OnOpenMonsterAtlas;
		public System.Action OnOpenStation;
		public System.Action OnOpenRanking;
		#endregion
	}
}
