using System;
using System.Collections.Generic;
using Config;
using FusionContinent.Manager;
using UnityEngine;
using Newtonsoft.Json;
using QFramework;
using Game.Battle;
using Game.Team;
using UIFramework.Services;
using Cardwar;
using Cysharp.Threading.Tasks;
using FusionContinent.Utils;

namespace FusionContinent.Model
{
    // 战斗
    public class PlayerBattle
    {
        public int Id { get; set; } // 用户Id
        public int Score { get; set; } // 总分
        public int CurrentStageId { get; set; } // 当前的关卡Id
        public int ClearStageId { get; set; } // 通关最大关卡Id
        public int ClearStageTime { get; set; } // 通关时间
        public int DailyDoubleRewardNum { get; set; } // 每日双倍奖励次数
        public int DailyStageSweepingNum { get; set; } // 每日关卡扫荡次数
        
        public int Status { get; set; } // 状态
        public DateTime? AfkTime { get; set; } // 挂机时间

        /**
         * 检查挂机产出奖励
         */
        public bool CheckAfkProduce()
        {
            var afkTime = BattleModel.GetAfkDuration();
            var produceNum = Mathf.FloorToInt((float)afkTime / DataConfigManager.Tables.TbGameBattleConst.AfkTimeProduce);
            return produceNum > 0;
        }
    }
    
    // 战斗数据参数
    public class BattleDataParam
    {
        public int Scores { get; set; } // 分数
        public Int64 Damage { get; set; } // 伤害
        public int Winner { get; set; } // 胜利者
        public int ClearDay { get; set; } // 通过天数
        public int ClearStageTime { get; set; } // 通关时间
        public bool SchoolMaxCardCount { get; set; } // 流派最大卡数
        public Dictionary<int, int> MonsterAtlas { get; set; }
        public bool IsSoldierDeath { get; set; } //是否有角色死亡
        public bool IsSoldierReduceHp { get; set; } //是否有角色掉血
    }
    
    
    public class BattleModel
    {
        /**
         * 获取挂机时长
         */
        public static int GetAfkDuration()
        {
            if (Global.PlayerBattle.AfkTime == null || Global.PlayerBattle.AfkTime == DateTime.MinValue) return 0;

            var serverTime = ServerTimeManager.Instance.GetDataTime();
            TimeSpan? diff =  serverTime - Global.PlayerBattle.AfkTime;  // 得到 TimeSpan 差值
            return diff == null ? 0 : (int)diff?.TotalSeconds;
        }

        /**
         * 战斗开始
         */
        public static async void Start(Action successCallback, Action<string> errorCallback = null, Action finallyCallback = null)
        {
            try
            {
                // 检查体力
                var stageCfg = DataConfigManager.Tables.TbStageBattleConfig.GetOrDefault(Global.StageId.Value);
                if (stageCfg == null)
                {
                    throw new Exception($"未配置的StageId: {Global.StageId.Value}");
                }

                if (stageCfg.ConsumeStamina > 0)
                {
                    if (stageCfg.ConsumeStamina > Global.PlayerResource.Stamina)
                    {
                        throw new Exception("体力不足，请稍后再试");
                    }

                    // Global.PlayerResource.Stamina -= stageCfg.ConsumeStamina;
                }

                LogTool.Log("正在请求服务器战斗开始，请稍候...");
                var request = new StartBattleRequest { StageId = stageCfg.Id };
                var proto = await WebDataFun.SendProtoPostRequest<StartBattleResponse>(ApiKeys.ApiBattleStart, request);
                if (proto != null)
                {
                    if (proto.Battle != null)
                    {
                        Global.PlayerBattle = ProtoMappers.MapPlayerBattle(proto.Battle);
                    }
                    // 关卡
                    if (proto.Stage != null)
                    {
                        var stageModel = ProtoMappers.MapPlayerStage(proto.Stage);
                        if (stageModel != null)
                        {
                            Global.PlayerStages.TryAdd(stageModel.StageId, stageModel);
                            Global.PlayerStages[stageModel.StageId] = stageModel;
                        }
                    }
                    
                    // 同步奖励
                    Items.Update(proto.Items);
                    
                    // 同步挑战Boss信息
                    
                    successCallback();
                }
            }
            catch (System.Exception ex)
            {
                if (errorCallback != null) errorCallback(ex.Message);
                LogTool.LogError($"开始战斗失败：{ex.Message} {ex.StackTrace}");
            }
            finally
            {
                if (finallyCallback != null) finallyCallback();
            }
        }

        /**
         * 战斗结算
         */
        public static async void Settle(int winner = 0, Action callback = null, bool isSettlementPanel = false)
        {
            try
            {
                LogTool.Log($"[SettleTiming] 4.开始请求结算接口 +{GamePlayManager.SettleTimingElapsedMs():F0}ms");
                LogTool.Log("正在请求服务器战斗结算接口，请稍候...");
                // 从 BattleTimeService 获取游戏时间
                var battleTimeService = BattleServiceManager.Instance?.GetService<BattleTimeService>();
                var currentTime = battleTimeService?.CurrentTime ?? 0f;
                ScoreController.Instance.AddScore(currentTime, Global.StageId.Value, Global.Date.Value);

                bool deathSoldier = false;
                ///检查是否有死亡
                foreach (var soldierCard in GamePlayManager.Default.villageGameCards)
                {
                    if (soldierCard.isDie)
                    {
                        deathSoldier = true;
                        break;
                    }
                }
                
                bool reduceHpSoldier = false;
                ///检查英灵是否掉血
                foreach (var soldierCard in GamePlayManager.Default.villageGameCards)
                {
                    if (soldierCard.isDie || soldierCard.CurrentHp < soldierCard.MaxHp)
                    {
                        reduceHpSoldier = true;
                        break;
                    }
                }
                var clearDay = winner == Global.User.UserId ? Global.Date.Value : Global.Date.Value - 1;
                if (Global.Date.Value <= 1)
                {
                    clearDay = 1;
                }
                var battleData = new BattleDataParam
                {
                    Scores = ScoreController.Instance.GetScore() + ScoreController.Instance.GetScoreTime(),
                    Damage = Global.TotalDamage.Value,
                    Winner = winner,
                    ClearDay = clearDay,
                    MonsterAtlas = ScoreController.Instance.GetKillMonsters(),
                    ClearStageTime = ScoreController.Instance.GetTime(),
                    SchoolMaxCardCount = SchoolManager.Instance.CheckSchoolMaxLevel(),
                    IsSoldierDeath = deathSoldier,
                    IsSoldierReduceHp = reduceHpSoldier,
                };
                var reqData = new BattleData
                {
                    Scores = battleData.Scores,
                    Damage = battleData.Damage,
                    Winner = battleData.Winner,
                    ClearDay = battleData.ClearDay,
                    ClearStageTime = battleData.ClearStageTime,
                    IsSoldierDeath = battleData.IsSoldierDeath,
                    IsSoldierReduceHp = battleData.IsSoldierReduceHp,
                    SchoolMaxCardCount = battleData.SchoolMaxCardCount
                };
                if (battleData.MonsterAtlas != null)
                {
                    foreach (var kv in battleData.MonsterAtlas)
                    {
                        reqData.MonsterAtlas[kv.Key] = kv.Value;
                    }
                }

                var request = new SettleBattleRequest { BattleData = reqData };
                var settleRequestStartMs = GamePlayManager.SettleTimingElapsedMs();
                var proto = await WebDataFun.SendProtoPostRequest<SettleBattleResponse>(ApiKeys.ApiBattleSettle, request);
                var settleResponseMs = GamePlayManager.SettleTimingElapsedMs();
                LogTool.Log(
                    $"[SettleTiming] 5.结算接口返回 +{settleResponseMs:F0}ms (接口耗时 {settleResponseMs - settleRequestStartMs:F0}ms, protoNull={proto == null})");
                if (proto != null)
                {
                    var lastLevelInfo = Global.User.GetLevelInfo();

                    if (proto.Battle != null)
                    {
                        Global.PlayerBattle = ProtoMappers.MapPlayerBattle(proto.Battle);
                    }
                    
                    // 关卡
                    if (proto.Stage != null)
                    {
                        var stageModel = ProtoMappers.MapPlayerStage(proto.Stage);
                        if (stageModel != null)
                        {
                            var stageId = stageModel.StageId;
                            Global.PlayerStages.TryAdd(stageId, stageModel);
                            Global.PlayerStages[stageId] = stageModel;
                        }
                    }
                    
                    // 同步奖励
                    Items.Update(proto.Items);
                    
                    // 新手引导战斗
                    Global.Player.SetGuideFlowsStatus(GuideType.GuideStage);
                    
                    // 对象池资源归还
                    CommonItemPoolManager.Instance.ReturnAll();
                    
                    // 重置游戏数据
                    Global.ResetGemData();
                    
                    // 同步卡牌
                    if (proto.Cards != null && proto.Cards.Count > 0)
                    {
                        Global.PlayerCards.Clear();
                        foreach (var card in proto.Cards)
                        {
                            var local = ProtoMappers.MapPlayerCard(card);
                            if (local != null)
                            {
                                Global.PlayerCards.Add(local.CardId, local);
                            }
                        }
                    }
                    
                    // 同步编队
                    if (proto.Teams != null && proto.Teams.Count > 0)
                    {
                        var list = new List<PlayerTeamModel>();
                        foreach (var t in proto.Teams)
                        {
                            var model = ProtoMappers.MapPlayerTeam(t);
                            if (model != null)
                            {
                                list.Add(model);
                            }
                        }
                        GameServiceManager.GetOrCreate<TeamService>().ApplyTeamModels(list);
                    }
                    
                    // 同步挑战Boss信息
                    if (proto.ChallengeBoss != null && proto.ChallengeBoss.Id > 0)
                    {
                        var boss = ProtoMappers.MapPlayerChallengeBoss(proto.ChallengeBoss);
                        if (boss != null)
                        {
                            Global.PlayerChallengeBoss[boss.BossId] = boss;
                        }
                    }
                    
                    // 同步无尽塔信息
                    if (proto.EndlessTower != null && proto.EndlessTower.Id > 0)
                    {
                        var tower = ProtoMappers.MapPlayerEndlessTower(proto.EndlessTower);
                        if (tower != null)
                        {
                            Global.PlayerEndlessTowers[tower.TowerId] = tower;
                        }
                    }
                    
                    // 同步秘境塔信息
                    if (proto.HiddenRealmTower != null && proto.HiddenRealmTower.Id > 0)
                    {
                        var hidden = ProtoMappers.MapPlayerHiddenRealmTower(proto.HiddenRealmTower);
                        if (hidden != null)
                        {
                            Global.PlayerHiddenRealmTowers[hidden.TowerId] = hidden;
                            GameServiceManager.GetOrCreate<Game.Realm.RealmService>().SyncTowerData();
                        }
                    }

                    // 同步怪物图鉴
                    if (proto.Atlas != null)
                    {
                        Global.PlayerAtlas = ProtoMappers.MapPlayerAtlas(proto.Atlas);
                        Game.Challenge.RedDot.MonsterAtlasRedDotHelper.MarkDirty();
                    }
                    
                    // 结算弹框
                    if (isSettlementPanel)
                    {
                        Global.LastPlayStage.Value = GlobalFunc.GetNextStageId(Global.StageId.Value, winner == Global.User.UserId ? 1:0);
                        UIKit.ClosePanel<SchoolChooseCardPanel>();// 关闭三选一
                        // 构造 BattleResult 供 ViewModel 使用
                        var rewardsDict = new Dictionary<string, int>();
                        if (proto.Rewards != null)
                        {
                            foreach (var kv in proto.Rewards)
                            {
                                rewardsDict[kv.Key.ToString()] = (int)kv.Value;
                            }
                        }
                        var uiBattleResult = new FusionContinent.Model.BattleResult
                        {
                            Stage = ProtoMappers.MapPlayerStage(proto.Stage),
                            Battle = ProtoMappers.MapPlayerBattle(proto.Battle),
                            Rewards = rewardsDict,
                            Items = proto.Items == null ? null : Items.ToItemGroup(proto.Items),
                            Cards = null,
                            Teams = null,
                            EndlessTower = ProtoMappers.MapPlayerEndlessTower(proto.EndlessTower),
                            ChallengeBoss = ProtoMappers.MapPlayerChallengeBoss(proto.ChallengeBoss),
                            HiddenRealmTower = ProtoMappers.MapPlayerHiddenRealmTower(proto.HiddenRealmTower)
                        };
                        var vm = new Game.Battle.GameSettlementPanel1ViewModel();
                        vm.OnCreate();
                        vm.SetData(uiBattleResult, winner, lastLevelInfo);
                        LogTool.Log($"[SettleTiming] 6.调用OpenPanelAsync打开结算弹窗 +{GamePlayManager.SettleTimingElapsedMs():F0}ms");
                        UIFramework.UIManager.OpenPanelAsync<GameSettlementPanel1New, Game.Battle.GameSettlementPanel1ViewModel>(vm, UIFramework.UILevel.PopUI).Forget();
                        Global.Publisher.Publish(MessagePublisher.CHANGE_GAME_SETTLE,winner);
                        // 新手引导关卡，禁用重新挑战按钮
                        var stageId = uiBattleResult.Stage?.StageId ?? 0;
                        vm.DisableRetry = stageId == DataConfigManager.Tables.TbPlayerConst.GuidesStageId;
                    }

                    if (callback != null) callback();
                }
            }
            catch (System.Exception ex)
            {
                FusionContinent.Toast.Show($"战斗结算失败：{ex.Message}");
                LogTool.LogError($"战斗结算失败：{ex.Message} {ex.StackTrace}");
            }
        }

        public static async void Sweep(int stageId,Action onComplete = null)
        {
            var request = new StageSweepingRequest { StageId = stageId };
            var proto = await WebDataFun.SendProtoPostRequest<StageSweepingResponse>(ApiKeys.ApiBattleStageSweeping, request);
            if (proto != null)
            {
                // 更新数据
                if (proto.Battle != null)
                {
                    Global.PlayerBattle = ProtoMappers.MapPlayerBattle(proto.Battle);
                }

                // 同步奖励
                Items.Update(proto.Items);

                // 奖励
                var rewardsDict = new Dictionary<string, int>();
                if (proto.Rewards != null)
                {
                    foreach (var kv in proto.Rewards)
                    {
                        rewardsDict[kv.Key.ToString()] = (int)kv.Value;
                    }
                }

                GlobalFunc.ShowRewardTips(rewardsDict);
                // RewardTips rewardTips = UIKit.OpenPanel<RewardTips>(UILevel.PopUI);
                // rewardTips.SetData(rewardsDict);
					
                // 同步秘境塔信息
                if (proto.HiddenRealmTower != null && proto.HiddenRealmTower.Id > 0)
                {
                    var hidden = ProtoMappers.MapPlayerHiddenRealmTower(proto.HiddenRealmTower);
                    if (hidden != null)
                    {
                        Global.PlayerHiddenRealmTowers[hidden.TowerId] = hidden;
                        GameServiceManager.GetOrCreate<Game.Realm.RealmService>().SyncTowerData();
                    }
                }
                
                onComplete?.Invoke();
            }
        }
    }
}