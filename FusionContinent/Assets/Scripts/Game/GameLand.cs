using Config;
using UnityEngine;
using QFramework;
using FusionContinent.Model;

namespace FusionContinent
{
    public partial class GameLand : ViewController
    {
        public static GameLand Default;

        public Transform anglewalldown;
        public Transform cardInfoObj;

        [Header("自动释放技能随眠时间")]
        public float automaticallyReleaseSkillSleepTime = 0.5f;

        private TbCardBaseConfig _cardBaseConfig;
        private TbCityLevelConfig _cityLevelConfig;
        private TbSpecialEffectsConfig _specialEffectsConfig;

        private LaneContext _playerLane;
        private LaneContext _opponentLane;

        public LaneContext PlayerLane => _playerLane;
        public LaneContext OpponentLane => _opponentLane;

        private void Awake()
        {
            Default = this;
            _cardBaseConfig = DataConfigManager.Tables.TbCardBaseConfig;
            _cityLevelConfig = DataConfigManager.Tables.TbCityLevelConfig;
            _specialEffectsConfig = DataConfigManager.Tables.TbSpecialEffectsConfig;
        }

        /// <summary>
        /// 按 BattleSession 模式切换 pve/pvp 显隐，并构建双侧 LaneContext。
        /// </summary>
        public void InitializeBattleArena()
        {
            BattleArenaVisibility.Apply(BattleSession.CurrentMode);
            BuildLanes();
        }

        public LaneContext GetLane(BattleSide side)
        {
            if (side == BattleSide.Opponent)
            {
                if (_opponentLane == null)
                {
                    LogTool.LogError("[GameLand] Opponent Lane 未初始化，禁止回退到玩家侧");
                }

                return _opponentLane;
            }

            return _playerLane;
        }

        private void BuildLanes()
        {
            _playerLane = LaneContext.FromSerializedPlayer(
                wall,
                MonsterPos,
                OrdinaryMonster,
                EliteMonster,
                BossMonster,
                FriendlySlot,
                Bullet,
                GenerateEffect,
                DamageParent,
                HeavyBlowPos);

            _opponentLane = null;
            if (!BattleSession.IsPvpAi)
            {
                return;
            }

            var landPvp = transform.Find("pvp");
            Transform uiPvp = null;
            if (GameLandUI.Default != null)
            {
                uiPvp = GameLandUI.Default.transform.Find("pvp");
            }

            _opponentLane = LaneContext.FromArenaRoots(landPvp, uiPvp, BattleSide.Opponent);
        }

        private void OnDestroy()
        {
            StopAllSkillCooldownTweens();
            CleanupSkillSilence();
            _pendingReleaseSkillCounts.Clear();
            Default = null;
        }

        /**
         * 检查自动战斗状态
         */
        public bool CheckAutoBattle()
        {
            if (!UserCreatePlayerActivity.CheckMonthCardExpired())
            {
                return true;
            }

            // 玩家需要通过当前关卡后（或直接购买月卡），再次挑战才开启
            if (!Global.PlayerStages.TryGetValue(Global.StageId.Value, out var playerStage) || playerStage.Condition1 == 0)
            {
                return false;
            }

            return true;
        }
    }
}
