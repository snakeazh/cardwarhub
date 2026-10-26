using UIFramework;
using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cardwar;
using Config;
using FusionContinent;
using FusionContinent.Model;
using FusionContinent.Utils;
using UIFramework.Services;
using Game.UI;
using PlayerChallenge = FusionContinent.Model.PlayerChallenge;

namespace Game.Challenge
{
    /// <summary>
    /// 融合石2048游戏界面 ViewModel
    /// </summary>
    public class GameGemPanelViewModel : ViewModel
    {
        #region 属性
        // 游戏状态
        public BindableProperty<bool> IsStartGame { get; } = new BindableProperty<bool>(false);
        
        // 当前分数
        public BindableProperty<int> CurrentScore { get; } = new BindableProperty<int>(0);
        
        // 剩余次数
        public BindableProperty<int> RemainingNum { get; } = new BindableProperty<int>(0);
        
        // 是否有奖励
        public BindableProperty<bool> IsHaveReward { get; } = new BindableProperty<bool>(false);
        
        // 奖励进度
        public BindableProperty<List<int>> RewardProgressList { get; } = new BindableProperty<List<int>>();
        
        // 奖励图标列表
        public BindableProperty<List<string>> RewardIcons { get; } = new BindableProperty<List<string>>();
        
        // 当前进度条填充值
        public BindableProperty<float> FillAmount { get; } = new BindableProperty<float>(0f);
        
        // 当前显示的奖励图标
        public BindableProperty<Sprite> CurrentGemSprite { get; } = new BindableProperty<Sprite>();
        #endregion

        #region 命令
        public Command OnCloseCommand { get; private set; }
        public Command OnStartGameCommand { get; private set; }
        public Command OnGameOverCommand { get; private set; }
        #endregion

        #region 依赖
        // private ChallengeService mService;
        #endregion

        #region 配置数据
        private TbGameConst mGameConst;
        private TbItemConfig mItemConfig;
        #endregion

        #region 常量
        private readonly float[] FillArray = new[] { 0.395f, 0.311f, 0.3f };
        #endregion

        public override void OnCreate()
        {
            base.OnCreate();
            // mService = GameServiceManager.Get<ChallengeService>();
            
            // 加载配置
            mGameConst = DataConfigManager.Tables.TbGameConst;
            mItemConfig = DataConfigManager.Tables.TbItemConfig;
            
            // 初始化命令
            OnCloseCommand = new Command(OnClose);
            OnStartGameCommand = new Command(OnStartGame);
            OnGameOverCommand = new Command(OnGameOver);
            
            // 初始化奖励进度
            RewardProgressList.Value = mGameConst.GemFuinGameRewardProgress;
            RewardIcons.Value = new List<string>();
        }

        /// <summary>
        /// 初始化界面数据
        /// </summary>
        public void Initialize()
        {
            IsStartGame.Value = false;
            CurrentScore.Value = 0;
            IsHaveReward.Value = false;
            FillAmount.Value = 0f;
            
            // 计算剩余次数
            int remaining = mGameConst.GemFuinGameDailyNum - Global.PlayerChallenge.DailyGemFusionNum;
            RemainingNum.Value = Mathf.Max(0, remaining);
            
            // 清空奖励图标
            RewardIcons.Value = new List<string>();
            
            // 触发次数变化事件，更新按钮状态
            OnRemainingNumChanged?.Invoke(RemainingNum.Value);
        }

        /// <summary>
        /// 开始游戏
        /// </summary>
        private async void OnStartGame()
        {
            if (RemainingNum.Value <= 0)
            {
                Toast.Show("次数不足");
                return;
            }
            
            // 触发引导
            OnSendGuide?.Invoke();
            
            // 立即隐藏开始界面（与原逻辑一致）
            OnHideStartGame?.Invoke();
            
            try
            {
                OnStartGameStarted?.Invoke();
                
                var request = new StartFusionGemGameRequest();
                var proto = await WebDataFun.SendProtoPostRequest<StartFusionGemGameResponse>(ApiKeys.ApiStartFusionGemGame, request);
                if (proto != null && proto.Challenge != null)
                {
                    var challenge = ProtoMappers.MapPlayerChallenge(proto.Challenge);
                    Global.PlayerChallenge = challenge;

                    // 检查奖励列表是否为空
                    if (challenge.GemFusionRewardList == null || challenge.GemFusionRewardList.Count == 0)
                    {
                        LogTool.LogError("[GameGemPanelViewModel] GemFusionRewardList 为空，无法开始游戏");
                        Toast.Error("获取奖励数据失败，请重试");
                        return;
                    }

                    // 更新奖励图标列表
                    var icons = new List<string>();
                    var rewardItems = new List<(int itemId, string icon, int effectValue)>();
                    for (int i = 0; i < challenge.GemFusionRewardList.Count; i++)
                    {
                        int itemId = challenge.GemFusionRewardList[i];
                        var itemConfig = mItemConfig.Get(itemId);
                        if (itemConfig != null)
                        {
                            icons.Add(itemConfig.Icon);
                            rewardItems.Add((itemId, itemConfig.Icon, itemConfig.EffectValue));
                        }
                    }
                    RewardIcons.Value = icons;

                    // 更新奖励显示（包含点击事件）
                    OnUpdateRewardDisplay?.Invoke(rewardItems);

                    // 开始游戏
                    IsStartGame.Value = true;
                    OnGameStarted?.Invoke();

                    // 完成引导
                    OnGuideStepComplete?.Invoke();
                }
            }
            catch (System.Exception ex)
            {
                LogTool.LogError($"[GameGemPanelViewModel] 开始游戏失败: {ex.Message} {ex.StackTrace}");
                Toast.Error($"开始游戏失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 游戏结束
        /// </summary>
        private async void OnGameOver()
        {
            IsStartGame.Value = false;
            OnCloseRequested?.Invoke();
            
            // 判断有没有获得奖励
            if (!IsHaveReward.Value)
            {
                // 打开奖励面板（挑战失败）
                OnShowFailReward?.Invoke();
            }
            else
            {
                // 领取奖励
                await GameFinish();
            }
        }

        /// <summary>
        /// 结算游戏
        /// </summary>
        private async System.Threading.Tasks.Task GameFinish()
        {
            try
            {
                var request = new SettleFusionGemGameRequest { Progress = CurrentScore.Value };
                var proto = await WebDataFun.SendProtoPostRequest<SettleFusionGemGameResponse>(ApiKeys.ApiSettleFusionGemGame, request);
                if (proto != null && proto.Challenge != null)
                {
                    Global.PlayerChallenge = ProtoMappers.MapPlayerChallenge(proto.Challenge);

                    // 同步道具
                    Items.Update(proto.Items);

                    var rewardsDict = new Dictionary<string, int>();
                    if (proto.Rewards != null)
                    {
                        foreach (var kv in proto.Rewards)
                        {
                            rewardsDict[kv.Key.ToString()] = (int)kv.Value;
                        }
                    }

                    // 打开奖励面板（挑战成功）
                    OnShowSuccessReward?.Invoke(rewardsDict);
                }
            }
            catch (System.Exception ex)
            {
                LogTool.LogError($"[GameGemPanelViewModel] 结算游戏失败: {ex.Message}");
                Toast.Error($"结算失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新分数（由游戏管理器调用）
        /// </summary>
        public void UpdateScore(int score)
        {
            if (score > CurrentScore.Value)
            {
                CurrentScore.Value = score;
                UpdateProgress(score);
                
                // 检查是否达到胜利条件
                if (score >= mGameConst.GemFuinGameWinNum)
                {
                    OnGameOverCommand.Execute();
                }
            }
        }

        /// <summary>
        /// 更新进度
        /// </summary>
        private void UpdateProgress(int score)
        {
            int index = 0;
            int gemScore = RewardProgressList.Value[0];
            float sum = 0;
            int fillScore = score;
            
            for (int i = 0; i < RewardProgressList.Value.Count; i++)
            {
                if (score >= RewardProgressList.Value[i])
                {
                    // 更新当前奖励图标（使用 RewardIcons 列表，与原界面的 _itemsIcon 对应）
                    if (i < RewardIcons.Value.Count)
                    {
                        OnUpdateCurrentGemIcon?.Invoke(RewardIcons.Value[i]);
                    }
                    
                    IsHaveReward.Value = true;
                    index++;
                    int k = Mathf.Clamp(i + 1, 0, RewardProgressList.Value.Count - 1);
                    gemScore = RewardProgressList.Value[k] - RewardProgressList.Value[k - 1];
                    sum += FillArray[i];
                }
                else
                {
                    break;
                }
            }

            if (index > 0)
                fillScore = score - RewardProgressList.Value[index - 1];
            
            if (index < RewardProgressList.Value.Count)
                FillAmount.Value = sum + (FillArray[index]) * ((float)fillScore / (float)gemScore);
            else
                FillAmount.Value = 1f;
        }

        /// <summary>
        /// 关闭前确认
        /// </summary>
        public void RequestClose()
        {
            if (!IsStartGame.Value)
            {
                OnCloseRequested?.Invoke();
            }
            else
            {
                // 游戏中，需要确认
                OnShowConfirmDialog?.Invoke("提示", "当你退出游戏时，默认你已经完成游戏，自动为您领取奖励", 
                    () => OnGameOverCommand.Execute());
            }
        }

        private void OnClose()
        {
            OnCloseRequested?.Invoke();
        }

        #region 事件
        public System.Action OnCloseRequested;

        public System.Action OnStartGameStarted;
        public System.Action OnGameStarted;
        public System.Action OnGuideStepComplete;
        public System.Action OnShowFailReward;
        public System.Action<Dictionary<string, int>> OnShowSuccessReward;
        public System.Action<string> OnUpdateCurrentGemIcon;
        public System.Action<string, string, System.Action> OnShowConfirmDialog;
        public System.Action<int> OnRemainingNumChanged;
        public System.Action OnSendGuide;  // 触发引导
        public System.Action OnHideStartGame;  // 隐藏开始界面
        public System.Action<List<(int itemId, string icon, int effectValue)>> OnUpdateRewardDisplay;  // 更新奖励显示
        #endregion
    }
}
