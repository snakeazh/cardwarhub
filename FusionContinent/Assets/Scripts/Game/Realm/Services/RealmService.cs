using System;
using System.Collections.Generic;
using System.Linq;
using Config;
using FusionContinent;
using FusionContinent.Model;
using Game.Team;
using UIFramework.Services;
using Cysharp.Threading.Tasks;

namespace Game.Realm
{
	public class RealmService : GameServiceBase
	{
		public static int RealmShopId {
			get
			{
				return DataConfigManager.Tables.TbShopConfig.DataList.Find(ele => ele.Type == ShopType.HiddenRealmShop).Id;
			}
		}

		public int CurrentTowerId { get; private set; }
		public int LastClearedFloorId { get; private set; }

		public override async UniTask InitializeAsync()
		{
			await base.InitializeAsync();
			SyncTowerData();
		}

		/// <summary>
		/// 秘境数据变更时调用，更新缓存的 TowerId 和最后通关层 Id。
		/// </summary>
		public void SyncTowerData()
		{
			foreach (var pair in Global.PlayerHiddenRealmTowers)
			{
				CurrentTowerId = pair.Key;
				LastClearedFloorId = pair.Value.Floor;
				return;
			}

			CurrentTowerId = 0;
			LastClearedFloorId = 0;
		}

		/// <summary>
		/// 获取玩家的秘境塔数据。
		/// </summary>
		public PlayerHiddenRealmTower GetPlayerTower()
		{
			if (CurrentTowerId != 0 &&
				Global.PlayerHiddenRealmTowers.TryGetValue(CurrentTowerId, out var tower))
			{
				return tower;
			}

			return null;
		}

		/// <summary>
		/// 获取所有已解锁的层配置（ChapterId 对应章节已通关），按 Id 排序。
		/// </summary>
		public List<HiddenRealmTowerConfig> GetAllFloorConfigs()
		{
			var clearStageId = Global.PlayerBattle.ClearStageId;
			return DataConfigManager.Tables.TbHiddenRealmTowerConfig.DataList
				.Where(c => IsChapterCleared(c.ChapterId, clearStageId))
				.OrderBy(c => c.Id)
				.ToList();
		}

		/// <summary>
		/// 是否存在已解锁的秘境层。用于入口判断，避免打开空的秘境界面。
		/// </summary>
		public bool HasUnlockedFloor()
		{
			return GetAllFloorConfigs().Count > 0;
		}

		private bool IsChapterCleared(int chapterId, int clearStageId)
		{
			var chapterCfg = DataConfigManager.Tables.TbChapterStageConfig.GetOrDefault(chapterId);
			if (chapterCfg == null || chapterCfg.Stages.Count == 0)
				return false;

			int lastStageId = chapterCfg.Stages[chapterCfg.Stages.Count - 1];
			return lastStageId <= clearStageId;
		}

		/// <summary>
		/// 根据 StageId 获取单层配置。
		/// </summary>
		public HiddenRealmTowerConfig GetFloorConfigByStageId(int stageId)
		{
			return DataConfigManager.Tables.TbHiddenRealmTowerConfig.GetOrDefault(stageId);
		}

		/// <summary>
		/// 返回玩家当前待挑战的层：ChapterId 已通关且上一层秘境已打过的最前层。
		/// </summary>
		public int GetCurrentFloor()
		{
			var unlockedFloors = GetAllFloorConfigs();

			foreach (var floor in unlockedFloors)
			{
				if (!IsFloorCleared(floor.Id))
					return floor.Id;
			}

			return unlockedFloors.Count > 0 ? unlockedFloors.Last().Id : 1;
		}

		public int GetRemainingSweepCount()
		{
			var tower = GetPlayerTower();
			if (tower != null)
			{
				return Math.Clamp(tower.DailyStageSweepingNum - tower.DailyChallengeNum, 0, 9999);
			}

			return 0;
		}

		public bool HasSweepCount()
		{
			var tower = GetPlayerTower();
			return tower != null && tower.DailyStageSweepingNum - tower.DailyChallengeNum > 0;
		}

		/// <summary>
		/// 开战前校验。通过返回 null，失败返回提示文案。
		/// </summary>
		public string ValidateBattle(HiddenRealmTowerConfig floorConfig)
		{
			if (floorConfig == null)
			{
				return "数据错误";
			}

			var battleConfig = DataConfigManager.Tables.TbStageBattleConfig.Get(floorConfig.StageId);
			var consume = battleConfig.ConsumeItem.First();
			if (Global.GetItemNumById(consume.Id) < consume.Num)
			{
				var name = DataConfigManager.Tables.TbItemConfig.Get(consume.Id).Name;
				return name + "数量不足";
			}

			if (GameServiceManager.GetOrCreate<TeamService>().BuildBattleTeamsForCurrentTalent())
			{
				return "请配置卡组";
			}

			return null;
		}

		public HiddenRealmTowerConfig GetFloorConfig(List<HiddenRealmTowerConfig> floors, int floor)
		{
			return floors.Find(ele => ele.Id == floor);
		}

		public StageBattleConfig GetStageBattleConfig(int stageId)
		{
			return DataConfigManager.Tables.TbStageBattleConfig.Get(stageId);
		}

		public void Sweep(int stageId, Action onComplete)
		{
			BattleModel.Sweep(stageId, onComplete);
		}

		public void PrepareBattle(int stageId)
		{
			GlobalFunc.PreparePanel(stageId);
		}

		/// <summary>
		/// 获取指定层的怪物词条数据。
		/// </summary>
		public HiddenRealmFloorData GetFloorData(int floor)
		{
			var tower = GetPlayerTower();
			if (tower == null)
			{
				return null;
			}

			return tower.Floors.Find(f => f.Floor == floor);
		}

		/// <summary>
		/// 根据词条类型和等级查找对应的 MonsterEntryLevelStruct 配置。
		/// </summary>
		public MonsterEntryLevelStruct GetMonsterEntryLevel(int entryType, int level)
		{
			var entryConfigs = DataConfigManager.Tables.TbMonsterEntryConfig.DataList;
			foreach (var config in entryConfigs)
			{
				if ((int)config.MonsterEntryType == entryType)
				{
					return config.LevelList.Find(lv => lv.Level == level);
				}
			}

			return null;
		}

		/// <summary>
		/// 获取指定层的所有词条对应的 MonsterEntryLevelStruct 列表，用于战斗属性修正。
		/// </summary>
		public List<MonsterEntryLevelStruct> GetFloorMonsterEntries(int floor)
		{
			var floorData = GetFloorData(floor);
			if (floorData == null)
			{
				return new List<MonsterEntryLevelStruct>();
			}

			var result = new List<MonsterEntryLevelStruct>();
			foreach (var entry in floorData.Entries)
			{
				var entryLevel = GetMonsterEntryLevel(entry.EntryType, entry.Level);
				if (entryLevel != null)
				{
					result.Add(entryLevel);
				}
			}

			return result;
		}

		/// <summary>
		/// 指定层是否已首次通关（词条永久冻结）。
		/// </summary>
		public bool IsFloorCleared(int floor)
		{
			var floorData = GetFloorData(floor);
			return floorData != null && floorData.Cleared;
		}

		/// <summary>
		/// 从关卡刷怪配置反向查找 Boss：StageList → WaveList → GenCards，
		/// CardId 对应 MonsterBattleConfig.MonsterQuality == Boss 即命中。
		/// waveNumber 为命中波在当日 WaveList 中的序号（从 1 开始）；未找到则为 0。
		/// </summary>
		public MonsterBattleConfig FindStageBoss(int stageId, out int waveNumber)
		{
			waveNumber = 0;
			var genCfg = DataConfigManager.Tables.TbMonsterGenConfig.GetOrDefault(stageId);
			if (genCfg == null)
				return null;

			var battleTable = DataConfigManager.Tables.TbMonsterBattleConfig;
			for (int si = genCfg.StageList.Count - 1; si >= 0; si--)
			{
				var dayCfg = genCfg.StageList[si];
				for (int wi = dayCfg.WaveList.Count - 1; wi >= 0; wi--)
				{
					var wave = dayCfg.WaveList[wi];
					for (int ci = wave.GenCards.Count - 1; ci >= 0; ci--)
					{
						var battleCfg = battleTable.GetOrDefault(wave.GenCards[ci].CardId);
						if (battleCfg != null && battleCfg.MonsterQuality == MonsterQuality.Boss)
						{
							waveNumber = dayCfg.Day;
							return battleCfg;
						}
					}
				}
			}

			return null;
		}
	}
}
