using System.Collections.Generic;
using System.Linq;
using Cardwar;
using Config;
using Cysharp.Threading.Tasks;
using FusionContinent;
using FusionContinent.Model;
using FusionContinent.Sounds;
using FusionContinent.Utils;
using UIFramework;
using UserLevelInfo = FusionContinent.Model.UserLevelInfo;

namespace Game.Battle
{
    /// <summary>
    /// 单张兵卡的伤害统计快照（结算面板展示用）。
    /// 为什么需要快照：结算面板是异步打开的，而 ClearGame() 会同步清空
    /// GamePlayManager.villageGameCards 与 Global.TotalDamage，若面板直接读取
    /// 这两个源，在 OnOpen 执行时数据已被清空，导致胜利时不显示统计。
    /// </summary>
    public class DamageStatSnapshot
    {
        public int CardId;
        public int TotalDamage;
    }

    public class GameSettlementPanel1ViewModel : ViewModel
    {
        public BindableProperty<bool> IsWinner { get; } = new BindableProperty<bool>(false);
        public BindableProperty<string> StageName { get; } = new BindableProperty<string>(string.Empty);
        public BindableProperty<int> LeftDoubleRewardNum { get; } = new BindableProperty<int>(0);
        public BindableProperty<int> MaxDoubleRewardNum { get; } = new BindableProperty<int>(0);
        public BindableProperty<UserLevelInfo> CurrentLevelInfo { get; } = new BindableProperty<UserLevelInfo>(null);
        public BindableProperty<UserLevelInfo> LastLevelInfo { get; } = new BindableProperty<UserLevelInfo>(null);
        public BindableProperty<float> ExpProgress { get; } = new BindableProperty<float>(0f);

        public BattleResult Result { get; private set; }
        public bool DisableRetry { get; set; }
        /// <summary>PVP 结算：隐藏双倍奖励 / 再来一次，不推进主线关卡。</summary>
        public bool IsPvpMode { get; private set; }

        /// <summary>本局每张兵卡的伤害统计快照（在 ClearGame 之前采集）</summary>
        public List<DamageStatSnapshot> DamageStats { get; private set; } = new List<DamageStatSnapshot>();
        /// <summary>本局总伤害快照（在 ClearGame 之前采集）</summary>
        public long TotalDamageSnapshot { get; private set; }

        public Command OnBackCommand { get; private set; }
        public Command OnDoubleRewardCommand { get; private set; }

        public System.Action OnDataRefreshed;
        public System.Action<string> OnShowError;
        public System.Action<bool> OnPlaySound;

        public override void OnCreate()
        {
            base.OnCreate();

            OnBackCommand = new Command(OnBack);
            OnDoubleRewardCommand = new Command(OnDoubleReward);
        }

        public void SetData(BattleResult result, int winner, UserLevelInfo lastLevelInfo, bool isPvp = false)
        {
            Result = result;
            IsPvpMode = isPvp;
            bool isWinner = winner == Global.User.UserId;
            IsWinner.Value = isWinner;
            LastLevelInfo.Value = lastLevelInfo;

            // 采集伤害统计快照：必须在 ClearGame() 之前完成，
            // 因为 Settle() 会先调用本方法，之后才调用 callback(=ClearGame)，
            // 而 ClearGame 会清空 villageGameCards 与 Global.TotalDamage。
            CaptureDamageSnapshot();

            OnPlaySound?.Invoke(isWinner);

            if (isPvp)
            {
                string opponentName = null;
                if (BattleSession.Opponent != null && BattleSession.Opponent.BaseInfo != null)
                {
                    opponentName = BattleSession.Opponent.BaseInfo.PlayerName;
                }

                StageName.Value = string.IsNullOrEmpty(opponentName) ? "PVP" : "VS " + opponentName;
                MaxDoubleRewardNum.Value = 0;
                LeftDoubleRewardNum.Value = 0;
                DisableRetry = true;
            }
            else
            {
                // 关卡名称
                var stageCfg = DataConfigManager.Tables.TbStageBattleConfig.GetOrDefault(Global.StageId.Value);
                StageName.Value = stageCfg?.StageName ?? string.Empty;

                // 双倍奖励
                MaxDoubleRewardNum.Value = DataConfigManager.Tables.TbGameBattleConst.DailyDoubleRewardNum;
                LeftDoubleRewardNum.Value = MaxDoubleRewardNum.Value - Global.PlayerBattle.DailyDoubleRewardNum;

                // 主线关卡推进
                if (isWinner && stageCfg?.StageType == StageType.Main)
                {
                    AdvanceStage();
                }
            }

            // 经验
            var lvInfo = new UserLevelInfo(Global.User.Level, Global.User.Exp);
            if (result.Items?.UserLevelInfo != null)
            {
                lvInfo = result.Items.UserLevelInfo;
            }
            CurrentLevelInfo.Value = lvInfo;

            var lvCfg = DataConfigManager.Tables.TbPlayerLevelConfig.GetOrDefault(lvInfo.Level);
            ExpProgress.Value = lvInfo.Exp > 0
                ? UnityEngine.Mathf.Clamp01((float)lvInfo.Exp / (float)lvCfg.Exp)
                : 0f;

            OnDataRefreshed?.Invoke();
        }

        /// <summary>
        /// 把当前战斗的伤害统计快照到 ViewModel，避免被后续 ClearGame 清空。
        /// 注意：SoldierCard 是 UnityEngine.Object，这里只拷贝需要展示的值类型字段。
        /// </summary>
        private void CaptureDamageSnapshot()
        {
            DamageStats.Clear();
            TotalDamageSnapshot = Global.TotalDamage?.Value ?? 0;

            var gamePlay = GamePlayManager.Default;
            if (gamePlay?.villageGameCards == null) return;

            foreach (var soldierCard in gamePlay.villageGameCards)
            {
                if (soldierCard == null) continue;
                DamageStats.Add(new DamageStatSnapshot
                {
                    CardId = soldierCard.cardId,
                    TotalDamage = soldierCard.TotalDamage
                });
            }
        }

        private void AdvanceStage()
        {
            var stageCfg = DataConfigManager.Tables.TbStageBattleConfig.GetOrDefault(Global.StageId.Value);
            var nextStageCfg = DataConfigManager.Tables.TbStageBattleConfig.GetOrDefault(Global.StageId.Value + 1);
            if (nextStageCfg != null)
            {
                Global.StageId.Value += 1;
                Global.LevelStage[Global.StageId.Value] = true;
            }
            else
            {
                var chapter = DataConfigManager.Tables.TbChapterStageConfig.GetOrDefault(
                    stageCfg.ChapterId + 1);
                if (chapter != null)
                {
                    Global.StageId.Value = chapter.InitUnlockStageId;
                    Global.LevelStage[Global.StageId.Value] = true;
                }
            }

            if (nextStageCfg != null)
            {
                var currentChapter = DataConfigManager.Tables.TbChapterStageConfig.GetOrDefault(stageCfg.ChapterId);
                if (currentChapter != null && Global.StageId.Value - 1 == currentChapter.InitUnlockStageId)
                {
                    var nextChapterStageId = DataConfigManager.Tables.TbChapterStageConfig
                        .GetOrDefault(stageCfg.ChapterId + 1)?.InitUnlockStageId ?? 0;
                    if (nextChapterStageId > 0 && Global.LevelStage.ContainsKey(nextChapterStageId))
                    {
                        Global.LevelStage[nextChapterStageId] = true;
                    }
                }
            }
        }

        private void OnBack()
        {
            GamePlayManager.Default.BackHome();
        }

        private async void OnDoubleReward()
        {
            if (IsPvpMode)
            {
                return;
            }

            // 前置校验：剩余次数 <= 0 时拦截，不发无效请求
            if (LeftDoubleRewardNum.Value <= 0)
            {
                OnShowError?.Invoke("今日双倍奖励次数已用完");
                return;
            }

            try
            {
                var reqData = new BattleData { ClearDay = Global.Date.Value };
                var request = new SettleBattleRequest { BattleData = reqData };
                var proto = await WebDataFun.SendProtoPostRequest<SettleBattleResponse>(
                    ApiKeys.ApiBattleDoubleReward, request);

                if (proto.Battle != null)
                {
                    Global.PlayerBattle = ProtoMappers.MapPlayerBattle(proto.Battle);
                }

                if (proto.Atlas != null)
                {
                    Global.PlayerAtlas = ProtoMappers.MapPlayerAtlas(proto.Atlas);
                    Game.Challenge.RedDot.MonsterAtlasRedDotHelper.MarkDirty();
                }

                Items.Update(proto.Items);

                LeftDoubleRewardNum.Value = MaxDoubleRewardNum.Value - Global.PlayerBattle.DailyDoubleRewardNum;

                if (proto.Rewards != null && proto.Rewards.Count > 0)
                {
                    var rewardsDict = new Dictionary<string, int>();
                    foreach (var kv in proto.Rewards)
                    {
                        rewardsDict[kv.Key.ToString()] = (int)kv.Value;
                    }
                    GlobalFunc.ShowRewardTips(rewardsDict).Forget();
                }

                OnDataRefreshed?.Invoke();
            }
            catch (System.Exception ex)
            {
                LogTool.LogError($"双倍奖励请求失败：{ex.Message}");
                OnShowError?.Invoke($"请求失败：{ex.Message}");
            }
        }
    }
}
