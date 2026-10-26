using UIFramework;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Config;
using FusionContinent.Model;
using Cysharp.Threading.Tasks;
using FusionContinent;
using Game.Challenge;
using QFramework;
using UIFramework.Services;

using UILevel = UIFramework.UILevel;
using UIManager = UIFramework.UIManager;
using Cardwar;
using FusionContinent.Utils;
using PlayerStage = FusionContinent.Model.PlayerStage;
using RewardAndNum = Config.RewardAndNum;

namespace Game.MainInterface
{
    /// <summary>
    /// 主界面 ViewModel
    /// </summary>
    public class MainInterfacePanelViewModel : ViewModel, Game.UserTool.IUserToolConfigurable
    {
        #region 属性
        public UIFramework.BindableProperty<int> CurrentChapter { get; } = new UIFramework.BindableProperty<int>(1);
        public UIFramework.BindableProperty<int> MaxChapter { get; } = new UIFramework.BindableProperty<int>(1);
        public UIFramework.BindableProperty<StageBattleConfig> CurrentStageConfig { get; } = new UIFramework.BindableProperty<StageBattleConfig>();
        public UIFramework.BindableProperty<string> StageName { get; } = new UIFramework.BindableProperty<string>("");
        public UIFramework.BindableProperty<string> ChapterName { get; } = new UIFramework.BindableProperty<string>("");
        public UIFramework.BindableProperty<bool> CanGoLeft { get; } = new UIFramework.BindableProperty<bool>(false);
        public UIFramework.BindableProperty<bool> CanGoRight { get; } = new UIFramework.BindableProperty<bool>(false);
        public UIFramework.BindableProperty<long> AfkRewardNum { get; } = new UIFramework.BindableProperty<long>(0);
        public UIFramework.BindableProperty<string> AfkProgressText { get; } = new UIFramework.BindableProperty<string>("");
        public UIFramework.BindableProperty<bool> CanStartGame { get; } = new UIFramework.BindableProperty<bool>(false);
        public UIFramework.BindableProperty<bool> IsAfkBtnVisible { get; } = new UIFramework.BindableProperty<bool>(false);
        public UIFramework.BindableProperty<List<TreasureChestData>> StageChestsData { get; } = new UIFramework.BindableProperty<List<TreasureChestData>>(new List<TreasureChestData>());
        public UIFramework.BindableProperty<int> ConsumeStamina { get; } = new UIFramework.BindableProperty<int>(0);
        #endregion

        #region 命令
        public Command OnStartGameCommand { get; private set; }
        public Command<int> OnChangeChapterCommand { get; private set; }
        public Command OnGoLeftCommand { get; private set; }
        public Command OnGoRightCommand { get; private set; }
        public Command OnAfkRewardCommand { get; private set; }
        public Command OnRankingCommand { get; private set; }
        public Command OnSettingCommand { get; private set; }
        public Command OnBulletinCommand { get; private set; }
        public Command OnTaskCommand { get; private set; }
        public Command OnBackpackCommand { get; private set; }
        public Command OnHelpCommand { get; private set; }
        public Command OnLevelDetailCommand { get; private set; }
        #endregion

        #region 依赖
        private MainInterfaceService mMainInterfaceService;
        #endregion

        #region 配置数据
        private TbStageBattleConfig mStageBattleConfig;
        private TbChapterStageConfig mChapterStageConfig;
        private Dictionary<int, StageBattleConfig> mStageCfgMap = new Dictionary<int, StageBattleConfig>();
        private List<StageBattleConfig> mStageBattleConfigs = new List<StageBattleConfig>();
        private StageBattleConfig mClearedStageConfig; // 当前通关进度所在的关卡配置
        #endregion

        public override void OnCreate()
        {
            base.OnCreate();
            mMainInterfaceService = GameServiceManager.Get<MainInterfaceService>();
            
            // 加载配置
            LoadConfigs();
            
            // 初始化命令
            OnStartGameCommand = new Command(OnStartGame, CheckCanStartGame);
            OnChangeChapterCommand = new Command<int>(OnChangeChapter);
            OnGoLeftCommand = new Command(OnGoLeft, CheckCanGoLeft);
            OnGoRightCommand = new Command(OnGoRight, CheckCanGoRight);
            OnAfkRewardCommand = new Command(OnAfkReward);
            OnRankingCommand = new Command(OnRanking);
            OnSettingCommand = new Command(OnSetting);
            OnBulletinCommand = new Command(OnBulletin);
            OnTaskCommand = new Command(OnTask);
            OnBackpackCommand = new Command(OnBackpack);
            OnHelpCommand = new Command(OnHelp);
            OnLevelDetailCommand = new Command(OnLevelDetail);
            
            // 初始化关卡数据
            InitializeStage();
            
            LogTool.Log("[MainInterfacePanelViewModel] 创建完成");
        }
        
        /// <summary>
        /// 获取 UserTool 配置
        /// </summary>
        public override Game.UserTool.UserToolData GetUserToolConfig()
        {
            return GlobalFunc.GetUserResourceConfig(PageType.MainPage);
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            LogTool.Log("[MainInterfacePanelViewModel] 销毁");
        }

        #region 初始化

        private void LoadConfigs()
        {
            mStageBattleConfig = DataConfigManager.Tables.TbStageBattleConfig;
            mChapterStageConfig = DataConfigManager.Tables.TbChapterStageConfig;
            
            // 加载关卡配置
            var mainConfigs = mStageBattleConfig.DataList.FindAll(ele => ele.StageType == StageType.Main);
            var guideConfigs = mStageBattleConfig.DataList.FindAll(ele => ele.StageType == StageType.Guide);
            
            mStageBattleConfigs.AddRange(guideConfigs);
            mStageBattleConfigs.AddRange(mainConfigs);
            
            // 构建关卡配置映射
            foreach (var cfg in mStageBattleConfigs)
            {
                if (!mStageCfgMap.ContainsKey(cfg.ChapterId))
                {
                    mStageCfgMap[cfg.ChapterId] = cfg;
                }
            }
        }

        private bool IsMainOrGuide(StageBattleConfig cfg)
        {
            return cfg != null && (cfg.StageType == StageType.Main || cfg.StageType == StageType.Guide);
        }

        private void InitializeStage()
        {
            // 获取当前通关进度关卡
            StageBattleConfig currentStage = null;
            int changeStageState = 0;
            if (Global.LastPlayStage.Value == 0)
            {
                currentStage = DataConfigManager.Tables.TbStageBattleConfig.Get(GlobalFunc.GetNextStageId(Global.PlayerBattle.ClearStageId,1));
                changeStageState = 2;
            }
            else
            {
                currentStage = DataConfigManager.Tables.TbStageBattleConfig.Get( Global.LastPlayStage.Value);
            }

            if (!IsMainOrGuide(currentStage))
            {
                currentStage = DataConfigManager.Tables.TbStageBattleConfig.Get(GlobalFunc.GetNextStageId(Global.PlayerBattle.ClearStageId, 1));
                changeStageState = 2;
            }
            // changeStageState = 2;
            // if (Global.PlayerBattle.ClearStageId <= 0)
            // {
            //     // 首次进入游戏，没有通关记录
            //     currentStage = mStageBattleConfigs.First(ele => ele.StageType == StageType.Main);
            // }
            // else
            // {
            //     // 初始化当前关卡章节
            //     currentStage = mStageBattleConfigs.Find(ele => ele.Id == Global.PlayerBattle.ClearStageId);
            //     changeStageState = 2;
            // }
            
            if (currentStage != null)
            {
                // 保存通关进度关卡配置（用于判断是否可以战斗）
                mClearedStageConfig = currentStage;
                
                CurrentStageConfig.Value = currentStage;
                CurrentChapter.Value = currentStage.ChapterId;
                MaxChapter.Value = Mathf.Clamp(
                    CurrentChapter.Value + DataConfigManager.Tables.TbGameBattleConst.MaxNumberOfLevelCanBeViewed,
                    1,
                    mStageBattleConfigs.Last().ChapterId
                );
                
                UpdateStageInfo();
                UpdateCanGoButtons();
            }
        }

        #endregion

        #region 命令实现

        private void OnStartGame()
        {
            if (CurrentStageConfig.Value == null) return;
            
            OnStartGameRequested?.Invoke(CurrentStageConfig.Value);
        }

        // 删除重复的方法，使用下面的 CheckCanStartGame
        private bool played = false;
        private void OnChangeChapter(int chapter)
        {
            if (!mStageCfgMap.TryGetValue(chapter, out var stageCfg)) return;
            
            CurrentChapter.Value = chapter;
            CurrentStageConfig.Value = stageCfg;
            played = true;
            UpdateStageInfo();
            played = false;
            UpdateCanGoButtons();
            
            OnChapterChanged?.Invoke(chapter);
        }

        private void OnGoLeft()
        {
            if (CurrentChapter.Value <= 1) return;
            
            OnChangeChapterCommand.Execute(CurrentChapter.Value - 1);
        }

        private bool CheckCanGoLeft()
        {
            return CurrentChapter.Value > 1;
        }

        private void OnGoRight()
        {
            if (CurrentChapter.Value >= MaxChapter.Value) return;
            
            OnChangeChapterCommand.Execute(CurrentChapter.Value + 1);
        }

        private bool CheckCanGoRight()
        {
            return CurrentChapter.Value < MaxChapter.Value;
        }

        private async void OnAfkReward()
        {
            var vm = new StageSweepingPanelViewModel();
            vm.OnCreate();
            // 挂机页固定展示最新通关章节，不传主界面当前浏览关卡
            if (Global.PlayerBattle.ClearStageId > 0)
            {
                vm.CurrentOpenId = Global.PlayerBattle.ClearStageId;
            }
            else if (Global.StageId.Value > 0)
            {
                vm.CurrentOpenId = Global.StageId.Value;
            }
            UIManager.OpenPanelAsync<StageSweepingPanelNew, StageSweepingPanelViewModel>(vm, UILevel.PopUI).Forget();
        }

        /// <summary>
        /// 打开关卡怪物预览面板，展示当前选中关卡 20 天内出现的所有怪物种类（去重）。
        /// </summary>
        private void OnLevelDetail()
        {
            if (CurrentStageConfig.Value == null) return;

            var vm = new MonsterPreViewPanelViewModel();
            vm.OnCreate();
            vm.Setup(CurrentStageConfig.Value.Id);
            UIManager.OpenPanelAsync<MonsterPreViewPanelNew, MonsterPreViewPanelViewModel>(vm, UILevel.PopUI).Forget();
        }

        private void OnRanking() { OnRankingRequested?.Invoke(); }
        private void OnSetting() { OnSettingRequested?.Invoke(); }
        private void OnBulletin() { OnBulletinRequested?.Invoke(); }
        private void OnTask() { OnTaskRequested?.Invoke(); }
        private void OnBackpack() { OnBackpackRequested?.Invoke(); }
        private void OnHelp() { OnHelpRequested?.Invoke(); }

        #endregion

        #region 公共方法

        public void UpdateStageInfo()
        {
            if (CurrentStageConfig.Value == null) return;

            StageName.Value = CurrentStageConfig.Value.StageName;
            ConsumeStamina.Value = CurrentStageConfig.Value.ConsumeStamina;

            var chapterCfg = mChapterStageConfig.GetOrDefault(CurrentChapter.Value);
            ChapterName.Value = chapterCfg?.Name ?? "";

            // 更新是否可以开始游戏状态
            CanStartGame.Value = CheckCanStartGame();

            // 更新挂机按钮显示状态（与原界面逻辑一致：可以战斗且存在挂机时间）
            IsAfkBtnVisible.Value = CanStartGame.Value && Global.PlayerBattle.AfkTime != null;

            // 更新宝箱数据
            UpdateStageChests();
        }

        /// <summary>
        /// 更新关卡宝箱数据（与原界面SetStageChests逻辑一致）
        /// </summary>
        private void UpdateStageChests()
        {
            var chestsData = new List<TreasureChestData>();
            List<string> iconList = new List<string>() { "Silver", "Gold", "Diamond" };

            var stageCfg = CurrentStageConfig.Value;
            if (stageCfg == null)
            {
                StageChestsData.Value = chestsData;
                return;
            }

            var rewardsByChest = new List<List<RewardAndNum>>
            {
                stageCfg.TreasureRewardsOne ?? new List<RewardAndNum>(),
                stageCfg.TreasureRewardsTwo ?? new List<RewardAndNum>(),
                stageCfg.TreasureRewardsThree ?? new List<RewardAndNum>()
            };

            Global.PlayerStages.TryGetValue(stageCfg.Id, out var stageInfo);

            // 单条关卡配置控制三个宝箱奖励
            for (int i = 0; i < rewardsByChest.Count; i++)
            {
                int chestIndex = i + 1;
                int chestCondition = GetChestCondition(stageInfo, chestIndex);

                var chestData = new TreasureChestData()
                {
                    Icon = $"{iconList[i]}",
                    ChestCondition = chestCondition,
                    Rewards = rewardsByChest[i],
                    StyleType = TreasureChestStyleType.Reward,
                    ProgressText = "UI_REW_NOT_CLAIMED",
                    StartAnim = played,
                    OnClick = () =>
                    {
                        GetStageFirstRewards(stageCfg.Id, chestIndex);
                    },
                };

                chestData.Icon = chestCondition == (int)ConditionStatus.Collect ? $"{iconList[i]}Open" : $"{iconList[i]}";
                chestData.IsOnClick = true;
                chestData.CanReceive = chestCondition == (int)ConditionStatus.Achieve;
                chestData.ProgressText = chestCondition >= (int)ConditionStatus.Collect ? "UI_REW_CLAIMED" : "UI_REW_NOT_CLAIMED";

                chestsData.Add(chestData);
            }

            StageChestsData.Value = chestsData;
        }

        private int GetChestCondition(PlayerStage stageInfo, int chestIndex)
        {
            if (stageInfo == null)
            {
                return (int)ConditionStatus.Normal;
            }

            switch (chestIndex)
            {
                case 1:
                    return stageInfo.Condition1;
                case 2:
                    return stageInfo.Condition2;
                case 3:
                    return stageInfo.Condition3;
                default:
                    return (int)ConditionStatus.Normal;
            }
        }

        /// <summary>
        /// 领取关卡首通奖励（与原界面GetStageFirstRewards逻辑一致）
        /// </summary>
        private async void GetStageFirstRewards(int stageId, int chestIndex)
        {
            LogTool.Log($"[MainInterfacePanelViewModel] 领取关卡首通奖励，StageId={stageId}, ChestIndex={chestIndex}");
            try
            {
                var request = new StageReceiveRewardsRequest { StageId = stageId, Condition = chestIndex };
                var proto = await WebDataFun.SendProtoPostRequest<StageReceiveRewardsResponse>(ApiKeys.ApiBattleStageFirstRewards, request);
                if (proto != null)
                {
                    // 更新数据
                    if (proto.Stage != null)
                    {
                        var stageModel = ProtoMappers.MapPlayerStage(proto.Stage);
                        if (stageModel != null)
                        {
                            Global.PlayerStages[stageModel.StageId] = stageModel;
                        }
                    }

                    // 同步奖励
                    LogTool.Log($"[MainInterfacePanelViewModel] 领奖响应: Items={(proto.Items == null ? "null" : (proto.Items.Items?.Count ?? 0) + "项")}, Rewards={(proto.Rewards == null ? "null" : proto.Rewards.Count + "项")}");
                    Items.Update(proto.Items);
                    // 兜底：若服务端未在 proto.Items 中返回货币数据，用 proto.Rewards 增量手动同步
                    ApplyRewardsFallback(proto);

                    // 刷新宝箱显示
                    UpdateStageChests();

                    // 触发奖励领取事件（转换奖励结构）
                    var rewardsDict = new Dictionary<string, int>();
                    if (proto.Rewards != null)
                    {
                        foreach (var kv in proto.Rewards)
                        {
                            rewardsDict[kv.Key.ToString()] = (int)kv.Value;
                        }
                    }
                    OnStageFirstRewardClaimed?.Invoke(rewardsDict);
                }
            }
            catch (System.Exception ex)
            {
                LogTool.LogError($"领取奖励失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 兜底同步：当 StageReceiveRewardsResponse.Items 未包含道具/货币数据时，
        /// 用 Rewards 字段的增量手动更新 Global.PlayerItems / PlayerResource 并广播事件，
        /// 确保 UserToolPanel 能收到 CHANGE_ITEM_NUM / CHANGE_STAMINA 及时刷新货币显示。
        /// </summary>
        private static void ApplyRewardsFallback(StageReceiveRewardsResponse proto)
        {
            if (proto == null || proto.Rewards == null || proto.Rewards.Count == 0) return;

            bool itemsSynced = proto.Items != null && proto.Items.Items != null && proto.Items.Items.Count > 0;
            bool staminaSynced = proto.Items != null && proto.Items.Resource != null && proto.Items.Resource.Id != 0;

            // Items.Update 已同步全部数据，无需兜底
            if (itemsSynced && staminaSynced) return;

            foreach (var kv in proto.Rewards)
            {
                var itemId = kv.Key;
                var delta = kv.Value;
                if (delta == 0) continue;

                var itemCfg = DataConfigManager.Tables.TbItemConfig.GetOrDefault(itemId);
                if (itemCfg == null) continue;

                // 体力单独存储在 PlayerResource，不走 PlayerItems
                if (itemCfg.Type == ItemType.Stamina)
                {
                    if (staminaSynced) continue;
                    var newStamina = Global.PlayerResource.Stamina + (int)delta;
                    Global.PlayerResource.Stamina = newStamina;
                    Global.Publisher.Publish(MessagePublisher.CHANGE_STAMINA, newStamina);
                }
                else
                {
                    // 该道具已由 Items.Update 同步则跳过，避免重复累加
                    if (itemsSynced && proto.Items.Items.ContainsKey(itemId)) continue;
                    var newNum = Global.GetItemNumById(itemId) + delta;
                    if (Global.PlayerItems.TryGetValue(itemId, out var model))
                    {
                        model.ItemNum = newNum;
                    }
                    else
                    {
                        Global.PlayerItems[itemId] = new PlayerItemModel { ItemId = itemId, ItemNum = newNum };
                    }
                    Global.Publisher.Publish($"{MessagePublisher.CHANGE_ITEM_NUM}{itemId}", newNum);
                }
            }

            LogTool.Log("[MainInterfacePanelViewModel] proto.Items 缺失道具数据，已用 proto.Rewards 兜底同步");
        }

        private bool CheckCanStartGame()
        {
            if (mClearedStageConfig == null || CurrentStageConfig.Value == null) return false;
            
            // 检查关卡条件（使用通关进度关卡配置判断，与原界面逻辑一致）
            if (Global.PlayerStages.TryGetValue(mClearedStageConfig.Id, out var playerStage))
            {
                if (playerStage.Condition1 > (int)ConditionStatus.Normal)
                {
                    return mClearedStageConfig.ChapterId + 1 >= CurrentChapter.Value;
                }
            }
            
            return mClearedStageConfig.ChapterId >= CurrentChapter.Value;
        }

        public void UpdateCanGoButtons()
        {
            CanGoLeft.Value = CheckCanGoLeft();
            CanGoRight.Value = CheckCanGoRight();
        }

        public void PlayAnimation()
        {
            OnPlayAnimationRequested?.Invoke();
        }

        #endregion

        #region 事件

        public System.Action<StageBattleConfig> OnStartGameRequested;
        public System.Action<int> OnChapterChanged;
        public System.Action<List<RewardItem>> OnAfkRewardClaimed;
        public System.Action<Dictionary<string, int>> OnStageFirstRewardClaimed;
        public System.Action OnRankingRequested;
        public System.Action OnSettingRequested;
        public System.Action OnBulletinRequested;
        public System.Action OnTaskRequested;
        public System.Action OnBackpackRequested;
        public System.Action OnHelpRequested;
        public System.Action OnPlayAnimationRequested;

        #endregion
    }
}
