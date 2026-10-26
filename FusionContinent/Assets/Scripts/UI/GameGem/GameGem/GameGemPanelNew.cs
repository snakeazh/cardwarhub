using Game.Challenge;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using QFramework;
using UIFramework;
using Config;
using FusionContinent.Model;
using FusionContinent;
using System.Collections.Generic;
using System;

namespace FusionContinent
{
    /// <summary>
    /// 融合石2048游戏界面 - 新框架版本 (MVVM)
    /// 复用原 GameGemPanel 的 UI 组件
    /// </summary>
    [UIFramework.PrefabPath("GameGemPanel")]
    public class GameGemPanelNew : UIFramework.UIPanel<GameGemPanelViewModel>
    {
        // 引用原 Panel，复用其 UI 组件
        private GameGemPanel mOldPanel;
        
        // 游戏管理器
        private GameManager2048 _gameManager2048;
        
        // 按钮事件处理器缓存
        private UnityEngine.Events.UnityAction _closeBtnHandler;
        private UnityEngine.Events.UnityAction _startGameBtnHandler;

        void Awake()
        {
            // 获取原 Panel 引用
            mOldPanel = GetComponent<GameGemPanel>();
            if (mOldPanel == null)
            {
                LogTool.LogError("[GameGemPanelNew] 找不到 GameGemPanel 组件！");
                return;
            }

            // 获取游戏管理器
            if (mOldPanel.GemGameManager != null)
            {
                _gameManager2048 = mOldPanel.GemGameManager.GetComponent<GameManager2048>();
            }

            LogTool.Log("[GameGemPanelNew] 已关联 GameGemPanel");
        }

        protected override void OnBindViewModel(GameGemPanelViewModel vm)
        {
            if (mOldPanel == null) return;

            // 绑定属性
            BindProperties(vm);
            
            // 绑定 ViewModel 事件
            BindEvents(vm);

            // 初始化按钮
            InitButtons(vm);
        }

        protected override void OnUnbindViewModel(GameGemPanelViewModel vm)
        {
            // 取消属性监听
            UnbindProperties(vm);
            
            // 取消事件订阅
            UnbindEvents(vm);
            
            // 移除按钮监听
            RemoveButtonListeners();
        }

        #region 属性绑定

        private void BindProperties(GameGemPanelViewModel vm)
        {
            if (mOldPanel == null) return;

            // 剩余次数
            vm.RemainingNum.OnValueChanged += (oldVal, newVal) =>
            {
                if (mOldPanel.NumText != null)
                {
                    mOldPanel.NumText.text = $"今日剩余免费游戏次数（{newVal}）";
                }
            };

            // 游戏状态变化
            vm.IsStartGame.OnValueChanged += (oldVal, newVal) =>
            {
                if (newVal)
                {
                    // 游戏开始，隐藏开始界面
                    if (mOldPanel.StartGame != null)
                        mOldPanel.StartGame.gameObject.SetActive(false);
                    
                    // 启动游戏
                    if (_gameManager2048 != null)
                        _gameManager2048.NewGame();
                }
            };

            // 进度条填充
            vm.FillAmount.OnValueChanged += (oldVal, newVal) =>
            {
                if (mOldPanel.Fill != null)
                    mOldPanel.Fill.fillAmount = newVal;
            };

            // 当前奖励图标
            vm.CurrentGemSprite.OnValueChanged += (oldVal, newVal) =>
            {
                if (mOldPanel.CurrentGemImage != null && newVal != null)
                {
                    mOldPanel.CurrentGemImage.gameObject.SetActive(true);
                    mOldPanel.CurrentGemImage.sprite = newVal;
                }
            };
        }

        private void UnbindProperties(GameGemPanelViewModel vm)
        {
            if (vm == null) return;
            
            // 取消属性监听
            vm.RemainingNum.OnValueChanged -= (oldVal, newVal) => { };
            vm.IsStartGame.OnValueChanged -= (oldVal, newVal) => { };
            vm.FillAmount.OnValueChanged -= (oldVal, newVal) => { };
            vm.CurrentGemSprite.OnValueChanged -= (oldVal, newVal) => { };
        }

        #endregion

        #region 按钮初始化

        private void InitButtons(GameGemPanelViewModel vm)
        {
            if (mOldPanel == null) return;

            // 关闭按钮
            if (mOldPanel.CloseBtn != null)
            {
                _closeBtnHandler = () => vm.RequestClose();
                mOldPanel.CloseBtn.onClick.AddListener(_closeBtnHandler);
            }

            // 开始游戏按钮
            if (mOldPanel.StartGameButton != null)
            {
                _startGameBtnHandler = () => vm.OnStartGameCommand.Execute();
                mOldPanel.StartGameButton.onClick.AddListener(_startGameBtnHandler);
            }
        }

        #endregion

        #region 事件绑定

        private void BindEvents(GameGemPanelViewModel vm)
        {
            // 订阅消息
            Global.Publisher.Subscribe(MessagePublisher.GEMGAME_SCORE_CHANGE, OnGemGameScoreChange);
            
            // ViewModel 事件
            vm.OnCloseRequested += OnCloseRequested;
            vm.OnGameStarted += OnGameStarted;
            vm.OnGuideStepComplete += OnGuideStepComplete;
            vm.OnShowFailReward += OnShowFailReward;
            vm.OnShowSuccessReward += OnShowSuccessReward;
            vm.OnUpdateCurrentGemIcon += OnUpdateCurrentGemIcon;
            vm.OnShowConfirmDialog += OnShowConfirmDialog;
            // vm.OnRemainingNumChanged += OnRemainingNumChanged;
            // vm.OnSendGuide += OnSendGuide;
            // vm.OnHideStartGame += OnHideStartGame;
            // vm.OnUpdateRewardDisplay += OnUpdateRewardDisplay;
        }

        private void UnbindEvents(GameGemPanelViewModel vm)
        {
            // 取消消息订阅
            Global.Publisher.Unsubscribe(MessagePublisher.GEMGAME_SCORE_CHANGE, OnGemGameScoreChange);
            
            if (vm == null) return;
            
            vm.OnCloseRequested -= OnCloseRequested;
            vm.OnGameStarted -= OnGameStarted;
            vm.OnGuideStepComplete -= OnGuideStepComplete;
            vm.OnShowFailReward -= OnShowFailReward;
            vm.OnShowSuccessReward -= OnShowSuccessReward;
            vm.OnUpdateCurrentGemIcon -= OnUpdateCurrentGemIcon;
            vm.OnShowConfirmDialog -= OnShowConfirmDialog;
            // vm.OnRemainingNumChanged -= OnRemainingNumChanged;
            // vm.OnSendGuide -= OnSendGuide;
            // vm.OnHideStartGame -= OnHideStartGame;
            // vm.OnUpdateRewardDisplay -= OnUpdateRewardDisplay;
        }

        private void RemoveButtonListeners()
        {
            if (mOldPanel == null) return;

            if (mOldPanel.CloseBtn != null && _closeBtnHandler != null)
                mOldPanel.CloseBtn.onClick.RemoveListener(_closeBtnHandler);
            if (mOldPanel.StartGameButton != null && _startGameBtnHandler != null)
                mOldPanel.StartGameButton.onClick.RemoveListener(_startGameBtnHandler);
        }

        #endregion

        #region UI 更新

        /// <summary>
        /// 初始化奖励显示
        /// </summary>
        private void InitRewardDisplay(GameGemPanelViewModel vm)
        {
            if (mOldPanel?.Gem == null) return;

            var gameConst = DataConfigManager.Tables.TbGameConst;
            if (gameConst.GemFuinGameRewardProgress.Count != 3)
            {
                LogTool.LogError("GemFuinGameRewardProgress 融合2048 奖励分数错误");
                return;
            }

            // 设置奖励进度文本
            for (int i = 0; i < gameConst.GemFuinGameRewardProgress.Count; i++)
            {
                var txt = mOldPanel.Gem.transform.Find(i.ToString())?.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null)
                {
                    txt.text = gameConst.GemFuinGameRewardProgress[i].ToString();
                }
            }
        }

        /// <summary>
        /// 更新奖励图标
        /// </summary>
        private void UpdateRewardIcons(GameGemPanelViewModel vm)
        {
            if (mOldPanel?.Gem == null) return;

            var icons = vm.RewardIcons.Value;
            for (int i = 0; i < icons.Count && i < 3; i++)
            {
                var gemTransform = mOldPanel.Gem.transform.GetChild(i);
                if (gemTransform == null) continue;

                var img = gemTransform.GetChild(0)?.GetComponent<Image>();
                if (img == null) continue;

                var itemConfig = DataConfigManager.Tables.TbItemConfig.Get(
                    Convert.ToInt32(Global.PlayerChallenge.GemFusionRewardList[i]));
                if (itemConfig == null) continue;

                img.sprite = Global.FusionGemIconSpriteAtlas.GetSprite(itemConfig.Icon);
                img.GetComponent<RectTransform>().sizeDelta = new Vector2(120, 120);

                // 添加点击事件
                var btn = img.GetOrAddComponent<Button>();
                btn.onClick.RemoveAllListeners();
                int effectValue = itemConfig.EffectValue;
                btn.onClick.AddListener(() =>
                {
                    // UIKit.OpenPanel<FusionGemDetailsPanel>().SetData(effectValue, 0, FusionGemDetailType.Detail);
                });
            }
        }

        #endregion

        #region 事件处理

        private void OnCloseRequested()
        {
            UIFramework.UIManager.ClosePanel(this);
        }

        private void OnShowTipRequested(string message)
        {
            FusionContinent.Toast.Show(message);
        }

        private void OnGameStarted()
        {
            // 游戏已开始
        }

        private void OnGuideStepComplete()
        {
            // 完成引导
            GuideFlow.Instance.SetStepComplete(GuideType.FusionGemChallenge);
        }

        private void OnShowFailReward()
        {
            // 打开奖励面板（挑战失败）
            GlobalFunc.ShowRewardTips(new List<RewardItem>(), "挑战失败", "未到达基础奖励分数");
        }

        private void OnShowSuccessReward(Dictionary<string, int> rewards)
        {
            // 打开奖励面板（挑战成功）
            GlobalFunc.ShowRewardTips(rewards, "挑战成功");
        }

        private void OnUpdateCurrentGemIcon(string iconName)
        {
            if (mOldPanel?.CurrentGemImage != null)
            {
                mOldPanel.CurrentGemImage.gameObject.SetActive(true);
                mOldPanel.CurrentGemImage.sprite = Global.ItemIconSpriteAtlas.GetSprite(iconName);
            }
        }

        private void OnShowConfirmDialog(string title, string content, System.Action callback)
        {
            GameMainPanel.Default?.ShowConfirm(title, content, callback);
        }

        /// <summary>
        /// 游戏分数变化（消息订阅回调）
        /// </summary>
        private void OnGemGameScoreChange(object score)
        {
            if (VM != null && score is int scoreValue)
            {
                VM.UpdateScore(scoreValue);
            }
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 设置数据（供外部调用）
        /// </summary>
        public void SetData()
        {
            if (VM != null)
            {
                VM.Initialize();
                InitRewardDisplay(VM);
            }
        }

        #endregion

        #region 生命周期

        protected override void OnOpen(UIFramework.IUIData uiData = null)
        {
            // 初始化数据
            if (VM != null)
            {
                VM.Initialize();
                InitRewardDisplay(VM);
            }
        }

        protected override void OnShow()
        {
            // 引导
            if (VM != null && VM.RemainingNum.Value > 0)
            {
                if (mOldPanel?.StartGameButton != null)
                {
                    UnlockGuideManager.Instance.SetUnlockTypeStep(ModuleFunctionType.FusionStoneGamePlay, 3, 
                        mOldPanel.StartGameButton.GetComponent<RectTransform>());
                }
            }
            else
            {
                UnlockGuideManager.Instance.SetErrorTypeStep(ModuleFunctionType.FusionStoneGamePlay, 3);
            }
        }

        protected override void OnHide()
        {
        }

        protected override void OnClose()
        {
            // 打开大世界界面（与原逻辑一致）
            var vm = new Game.Challenge.BigWorldPanelViewModel();
            vm.OnCreate();
            UIFramework.UIManager.OpenPanelAsync<BigWorldPanelNew, Game.Challenge.BigWorldPanelViewModel>(vm);
        }

        #endregion
    }
}
