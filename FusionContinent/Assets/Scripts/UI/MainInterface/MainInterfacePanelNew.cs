using Game.MainInterface;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using QFramework;
using UIFramework;
using Config;
using FusionContinent.Animation;
using DG.Tweening;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FusionContinent.Helper;
using FusionContinent.Manager;
using FusionContinent.Model;
using Manager;
using UIFramework.Services;
using UILevel = QFramework.UILevel;
using Game.RedDot;
using Game.Team;
using Utils;
using UIManager = UIFramework.UIManager;

namespace FusionContinent
{
    /// <summary>
    /// 主界面 - 新框架版本 (MVVM)
    /// 复用原 MainInterfacePanel 的 UI 组件
    /// </summary>
    [UIFramework.PrefabPath("MainInterfacePanel")]
    public class MainInterfacePanelNew : UIFramework.UIPanel<MainInterfacePanelViewModel>
    {
        // 引用原 Panel，复用其 UI 组件
        private MainInterfacePanel mOldPanel;

        public MainInterfacePanel GetOldPanel()
        {
            return mOldPanel;
        }
        
        void Awake()
        {
            // 获取原 Panel 引用
            mOldPanel = GetComponent<MainInterfacePanel>();
            if (mOldPanel == null)
            {
                LogTool.LogError("[MainInterfacePanelNew] 找不到 MainInterfacePanel 组件！请确保预制体上同时挂载了两个脚本");
                return;
            }

            LogTool.Log("[MainInterfacePanelNew] 已关联 MainInterfacePanel");
        }

        protected override void OnBindViewModel(MainInterfacePanelViewModel vm)
        {
            if (mOldPanel == null) return;

            // 绑定属性
            BindProperties(vm);

            // 绑定 ViewModel 事件
            BindViewModelEvents(vm);

            // 初始化按钮
            InitButtons(vm);

            BindGlobalEvents();
        }

        protected override void OnUnbindViewModel(MainInterfacePanelViewModel vm)
        {
            UnbindProperties(vm);
            UnbindGlobalEvents();
            UnbindViewModelEvents(vm);
            RemoveButtonListeners();
        }

        private void BindGlobalEvents()
        {
            Global.Publisher.Subscribe(MessagePublisher.CHANGE_USER_LEVEL_INFO, OnUserLevelChanged);
            Global.Publisher.Subscribe(MessagePublisher.STAGE_SWEEPING_PANEL_CLOSED, OnStageSweepingPanelClosed);
        }

        private void UnbindGlobalEvents()
        {
            Global.Publisher.Unsubscribe(MessagePublisher.CHANGE_USER_LEVEL_INFO, OnUserLevelChanged);
            Global.Publisher.Unsubscribe(MessagePublisher.STAGE_SWEEPING_PANEL_CLOSED, OnStageSweepingPanelClosed);
        }

        /// <summary>
        /// 挂机收益界面关闭后，等 0.2s 再跑每日弹出链（等关闭流程彻底结束）
        /// </summary>
        private async void OnStageSweepingPanelClosed(object message)
        {
            await UniTask.Delay(500);
            if (this == null) return;
            DailyPopUpLogic();
        }

        private void OnUserLevelChanged(object message)
        {
            RefreshLevelGatedButtons();
            // 等级变化后重新走弹出链，否则 UnlockGuide 只入队不会 ShowGuide
            DailyPopUpLogic();
        }

        /// <summary>
        /// 刷新 PlayerQueue 编队 Spine 槽位：按当前使用编队的槽位顺序（CardsPosition）显示英灵，空槽位清理。
        /// </summary>
        private void RefreshPlayerQueue()
        {
            if (mOldPanel == null || mOldPanel.PlayerQueue == null) return;

            var teamService = GameServiceManager.Get<TeamService>();
            if (teamService == null) return;

            var team = teamService.GetTeamByTeamId(GlobalFunc.GetCommonTeamId());
            var soldierIds = new List<int>();
            foreach (var cardId in team.Cards)
            {
                var cardCfg = DataConfigManager.Tables.TbCardBaseConfig.GetOrDefault(cardId);
                if (cardCfg != null && cardCfg.Type == CardType.Soldier)
                {
                    soldierIds.Add(cardId);
                }
            }

            var cardsPosition = team.CardsPosition;
            var playerQueue = mOldPanel.PlayerQueue;
            for (int i = 0; i < playerQueue.childCount; i++)
            {
                var slot = playerQueue.GetChild(i).GetComponent<MainInterfaceSpineSlot>();
                if (slot == null)
                {
                    LogTool.LogError($"[MainInterfacePanelNew] PlayerQueue 子节点 {i} 未挂载 MainInterfaceSpineSlot");
                    continue;
                }

                // 槽位 i 显示 soldierIds 中第 CardsPosition[i] 个英灵，
                // 与编队界面（CardGroupPanel.GetCardIdByPosition）同一语义，缺省时按 i 兜底
                int sourceIndex = cardsPosition != null && i < cardsPosition.Count ? cardsPosition[i] : i;
                if (sourceIndex >= 0 && sourceIndex < soldierIds.Count)
                {
                    slot.SetData(soldierIds[sourceIndex]);
                }
                else
                {
                    slot.Clear();
                }
            }
        }

        /// <summary>
        /// 单机化：活动区（LeftList）/好友/邮件/聊天入口已移除，统一隐藏对应节点。
        /// </summary>
        private void RefreshLevelGatedButtons()
        {
            if (mOldPanel == null)
            {
                return;
            }

            if (mOldPanel.LeftList != null)
            {
                mOldPanel.LeftList.gameObject.SetActive(false);
            }

            if (mOldPanel.FriendBtn != null)
            {
                mOldPanel.FriendBtn.gameObject.SetActive(false);
            }

            if (mOldPanel.MailBtn != null)
            {
                mOldPanel.MailBtn.gameObject.SetActive(false);
            }

            if (mOldPanel.ChatPanel != null)
            {
                mOldPanel.ChatPanel.gameObject.SetActive(false);
            }
        }

        #region 属性绑定

        private void BindProperties(MainInterfacePanelViewModel vm)
        {
            if (mOldPanel == null) return;

            vm.StageName.OnValueChanged += OnStageNameChanged;
            vm.ChapterName.OnValueChanged += OnChapterNameChanged;
            vm.CanGoLeft.OnValueChanged += OnCanGoLeftChanged;
            vm.CanGoRight.OnValueChanged += OnCanGoRightChanged;
            vm.CanStartGame.OnValueChanged += OnCanStartGameChanged;
            vm.IsAfkBtnVisible.OnValueChanged += OnIsAfkBtnVisibleChanged;
            vm.StageChestsData.OnValueChanged += OnStageChestsDataChanged;
            vm.ConsumeStamina.OnValueChanged += OnConsumeStaminaChanged;
        }

        private void UnbindProperties(MainInterfacePanelViewModel vm)
        {
            if (vm == null) return;

            vm.StageName.OnValueChanged -= OnStageNameChanged;
            vm.ChapterName.OnValueChanged -= OnChapterNameChanged;
            vm.CanGoLeft.OnValueChanged -= OnCanGoLeftChanged;
            vm.CanGoRight.OnValueChanged -= OnCanGoRightChanged;
            vm.CanStartGame.OnValueChanged -= OnCanStartGameChanged;
            vm.IsAfkBtnVisible.OnValueChanged -= OnIsAfkBtnVisibleChanged;
            vm.StageChestsData.OnValueChanged -= OnStageChestsDataChanged;
            vm.ConsumeStamina.OnValueChanged -= OnConsumeStaminaChanged;
        }

        private void OnStageNameChanged(string oldVal, string newVal)
        {
            UpdateStageName(newVal);
        }

        private void OnChapterNameChanged(string oldVal, string newVal)
        {
            UpdateChapterName();
        }

        private void OnCanGoLeftChanged(bool oldVal, bool newVal)
        {
            UpdateLeftBtnState(newVal);
        }

        private void OnCanGoRightChanged(bool oldVal, bool newVal)
        {
            UpdateRightBtnState(newVal);
        }

        private void OnCanStartGameChanged(bool oldVal, bool newVal)
        {
            UpdateCanStartGameState(newVal);
        }

        private void OnIsAfkBtnVisibleChanged(bool oldVal, bool newVal)
        {
            UpdateAfkBtnVisibility(newVal);
        }

        private void OnStageChestsDataChanged(List<TreasureChestData> oldVal, List<TreasureChestData> newVal)
        {
            UpdateStageChests(newVal);
        }

        private void OnConsumeStaminaChanged(int oldVal, int newVal)
        {
            UpdateConsumeStamina(newVal);
        }

        #region UI更新方法（可被初始化和属性变化复用）

        private void UpdateStageName(string stageName)
        {
            if (mOldPanel?.StageNameText != null)
            {
                mOldPanel.StageNameText.text = stageName;

                if (VM?.CurrentStageConfig.Value != null)
                {
                    var stageCfg = VM.CurrentStageConfig.Value;
                    TextMeshProTools.SetTMPVerticalGradient(mOldPanel.StageNameText, stageCfg.StageNameColor1, stageCfg.StageNameColor2);

                    // if (stageCfg.StageNameColor2 != null && stageCfg.StageNameColor2.Count >= 3)
                    // {
                    //     var underlay = stageCfg.StageNameColor2;
                    //     TextMeshProTools.SetUnderlayColor(
                    //         mOldPanel.StageNameText,
                    //         new Color32((byte)underlay[0], (byte)underlay[1], (byte)underlay[2], 255));
                    // }
                }
            }
        }

        private void UpdateChapterName()
        {
            if (mOldPanel?.StageChapterNameText != null && VM?.CurrentStageConfig.Value != null)
            {
                var stageCfg = VM.CurrentStageConfig.Value;
                mOldPanel.StageChapterNameText.SetData(LocalizationModule.UITexts, "UI_STAGE_CHAPTERNAME",
                    stageCfg.ChapterId);
            }
        }

        private void UpdateLeftBtnState(bool canGoLeft)
        {
            if (mOldPanel?.StageLeftBtn != null)
                mOldPanel.StageLeftBtn.interactable = canGoLeft;
        }

        private void UpdateRightBtnState(bool canGoRight)
        {
            if (mOldPanel?.StageRightBtn != null)
                mOldPanel.StageRightBtn.interactable = canGoRight;
        }

        private void UpdateCanStartGameState(bool canStartGame)
        {
            if (mOldPanel?.PlayGameBtn != null)
                mOldPanel.PlayGameBtn.interactable = canStartGame;
        }

        private void UpdateAfkBtnVisibility(bool isVisible)
        {
            if (mOldPanel?.StageAfkBtn != null)
                mOldPanel.StageAfkBtn.gameObject.SetActive(isVisible);
        }

        /// <summary>
        /// 更新体力消耗显示
        /// </summary>
        private void UpdateConsumeStamina(int consumeStamina)
        {
            if (mOldPanel?.PlayGameBtn != null)
            {
                var startText = mOldPanel.PlayGameBtn.transform.Find("StartText");
                if (startText != null)
                {
                    var tmpText = startText.GetComponent<TMP_Text>();
                    if (tmpText != null)
                        tmpText.text = consumeStamina.ToString();
                }
            }
        }

        /// <summary>
        /// 更新关卡宝箱显示（与原界面SetStageChests逻辑一致）
        /// </summary>
        private void UpdateStageChests(List<TreasureChestData> chestsData,bool playAnim = false)
        {
            if (mOldPanel?.StageChests == null || chestsData == null) return;

            for (int i = 0; i < mOldPanel.StageChests.childCount && i < chestsData.Count; i++)
            {
                var chestTran = mOldPanel.StageChests.GetChild(i);
                var treasureChest = chestTran.GetComponent<TreasureChest>();
                if (treasureChest != null)
                {
                    // chestsData[i].StartAnim = playAnim;
                    treasureChest.SetData(chestsData[i]);
                }
            }
        }
        public void PlayAnimation()
        {
            if (mOldPanel?.OnSelectAnimation == null) return;

            UIAnimationUtils.PlayAnimations(mOldPanel.OnSelectAnimation.gameObject);

         
        }
        /// <summary>
        /// 刷新所有UI状态（用于初始化）
        /// </summary>
        private void RefreshAllUIState()
        {
            if (VM == null || mOldPanel == null) return;

            UpdateStageName(VM.StageName.Value);
            UpdateChapterName();
            UpdateLeftBtnState(VM.CanGoLeft.Value);
            UpdateRightBtnState(VM.CanGoRight.Value);
            UpdateCanStartGameState(VM.CanStartGame.Value);
            UpdateAfkBtnVisibility(VM.IsAfkBtnVisible.Value);
            UpdateStageChests(VM.StageChestsData.Value,true);
            UpdateConsumeStamina(VM.ConsumeStamina.Value);
        }

        #endregion

        #endregion

        #region 事件绑定

        private void BindViewModelEvents(MainInterfacePanelViewModel vm)
        {
            vm.OnStartGameRequested += OnStartGameRequested;
            vm.OnChapterChanged += OnChapterChangedEvent;
            vm.OnAfkRewardClaimed += OnAfkRewardClaimed;
            vm.OnStageFirstRewardClaimed += OnStageFirstRewardClaimed;
            vm.OnRankingRequested += OnRankingRequested;
            vm.OnSettingRequested += OnSettingRequested;
            vm.OnBulletinRequested += OnBulletinRequested;
            vm.OnTaskRequested += OnTaskRequested;
            vm.OnBackpackRequested += OnBackpackRequested;
            vm.OnHelpRequested += OnHelpRequested;
            vm.OnPlayAnimationRequested += OnPlayAnimationRequested;
        }

        private void UnbindViewModelEvents(MainInterfacePanelViewModel vm)
        {
            vm.OnStartGameRequested -= OnStartGameRequested;
            vm.OnChapterChanged -= OnChapterChangedEvent;
            vm.OnAfkRewardClaimed -= OnAfkRewardClaimed;
            vm.OnStageFirstRewardClaimed -= OnStageFirstRewardClaimed;
            vm.OnRankingRequested -= OnRankingRequested;
            vm.OnSettingRequested -= OnSettingRequested;
            vm.OnBulletinRequested -= OnBulletinRequested;
            vm.OnTaskRequested -= OnTaskRequested;
            vm.OnBackpackRequested -= OnBackpackRequested;
            vm.OnHelpRequested -= OnHelpRequested;
            vm.OnPlayAnimationRequested -= OnPlayAnimationRequested;
        }

        #endregion

        #region 按钮初始化

        private void InitButtons(MainInterfacePanelViewModel vm)
        {
            if (mOldPanel == null) return;

            if (mOldPanel.PlayGameBtn != null)
                mOldPanel.PlayGameBtn.onClick.AddListener(OnPlayGameBtnClicked);

            if (mOldPanel.StageLeftBtn != null)
                mOldPanel.StageLeftBtn.onClick.AddListener(OnStageLeftBtnClicked);

            if (mOldPanel.StageRightBtn != null)
                mOldPanel.StageRightBtn.onClick.AddListener(OnStageRightBtnClicked);

            if (mOldPanel.StageAfkBtn != null)
            {
                mOldPanel.StageAfkBtn.onClick.AddListener(OnStageAfkBtnClicked);
                mOldPanel.StageAfkBtn.GetOrAddComponent<RedDotBinder>().SetRedType(RedNodeType.MainPageAFKBtn);
            }

            if (mOldPanel.RankingBtn != null)
                mOldPanel.RankingBtn.onClick.AddListener(OnRankingBtnClicked);

            if (mOldPanel.SettingBtn != null)
                mOldPanel.SettingBtn.onClick.AddListener(OnSettingBtnClicked);

            if (mOldPanel.BulletinBtn != null)
                mOldPanel.BulletinBtn.onClick.AddListener(OnBulletinBtnClicked);

            if (mOldPanel.TaskBtn != null)
            {
                mOldPanel.TaskBtn.onClick.AddListener(OnTaskBtnClicked);
                mOldPanel.TaskBtn.SetRedType(RedNodeType.MainPageTaskBtn);
            }

            if (mOldPanel.BackpackBtn != null)
                mOldPanel.BackpackBtn.onClick.AddListener(OnBackpackBtnClicked);

            if (mOldPanel.LevelDetailBtn != null)
                mOldPanel.LevelDetailBtn.onClick.AddListener(OnLevelDetailBtnClicked);

            if (mOldPanel.SwipeArea != null)
            {
                var swipeListener = mOldPanel.SwipeArea.GetOrAddComponent<UISwipeListener>();
                swipeListener.onSwipeLeft.AddListener(OnSwipeLeftForNextChapter);
                swipeListener.onSwipeRight.AddListener(OnSwipeRightForPrevChapter);
            }
        }

        private void RemoveButtonListeners()
        {
            if (mOldPanel == null) return;

            if (mOldPanel.PlayGameBtn != null)
                mOldPanel.PlayGameBtn.onClick.RemoveListener(OnPlayGameBtnClicked);

            if (mOldPanel.StageLeftBtn != null)
                mOldPanel.StageLeftBtn.onClick.RemoveListener(OnStageLeftBtnClicked);

            if (mOldPanel.StageRightBtn != null)
                mOldPanel.StageRightBtn.onClick.RemoveListener(OnStageRightBtnClicked);

            if (mOldPanel.StageAfkBtn != null)
                mOldPanel.StageAfkBtn.onClick.RemoveListener(OnStageAfkBtnClicked);

            if (mOldPanel.RankingBtn != null)
                mOldPanel.RankingBtn.onClick.RemoveListener(OnRankingBtnClicked);

            if (mOldPanel.SettingBtn != null)
                mOldPanel.SettingBtn.onClick.RemoveListener(OnSettingBtnClicked);

            if (mOldPanel.BulletinBtn != null)
                mOldPanel.BulletinBtn.onClick.RemoveListener(OnBulletinBtnClicked);

            if (mOldPanel.TaskBtn != null)
                mOldPanel.TaskBtn.onClick.RemoveListener(OnTaskBtnClicked);

            if (mOldPanel.BackpackBtn != null)
                mOldPanel.BackpackBtn.onClick.RemoveListener(OnBackpackBtnClicked);

            if (mOldPanel.LevelDetailBtn != null)
                mOldPanel.LevelDetailBtn.onClick.RemoveListener(OnLevelDetailBtnClicked);

            if (mOldPanel.SwipeArea != null)
            {
                var swipeListener = mOldPanel.SwipeArea.GetComponent<UISwipeListener>();
                if (swipeListener != null)
                {
                    swipeListener.onSwipeLeft.RemoveListener(OnSwipeLeftForNextChapter);
                    swipeListener.onSwipeRight.RemoveListener(OnSwipeRightForPrevChapter);
                }
            }
        }

        private void OnPlayGameBtnClicked()
        {
            VM.OnStartGameCommand.Execute();
        }

        private void OnStageLeftBtnClicked()
        {
            VM.OnGoLeftCommand.Execute();
        }

        private void OnStageRightBtnClicked()
        {
            VM.OnGoRightCommand.Execute();
        }

        private void OnStageAfkBtnClicked()
        {
            VM.OnAfkRewardCommand.Execute();
        }

        private void OnRankingBtnClicked()
        {
            VM.OnRankingCommand.Execute();
        }

        private void OnSettingBtnClicked()
        {
            VM.OnSettingCommand.Execute();
        }

        private void OnBulletinBtnClicked()
        {
            VM.OnBulletinCommand.Execute();
        }

        private void OnTaskBtnClicked()
        {
            if (!ModuleFunction.CheckOpen(ModuleFunctionType.Task)) return;
            VM.OnTaskCommand.Execute();
        }

        private void OnBackpackBtnClicked()
        {
            VM.OnBackpackCommand.Execute();
        }

        // 关卡怪物预览：展示当前选中关卡 20 天内出现的所有怪物种类（去重）
        private void OnLevelDetailBtnClicked()
        {
            VM.OnLevelDetailCommand.Execute();
        }

        private void OnSwipeLeftForNextChapter()
        {
            VM.OnGoRightCommand.Execute();
        }

        private void OnSwipeRightForPrevChapter()
        {
            VM.OnGoLeftCommand.Execute();
        }

        #endregion

        #region 事件处理

        private void OnStartGameRequested(StageBattleConfig stageConfig)
        {
            LogTool.Log($"[MainInterfacePanelNew] 开始游戏，StageId={stageConfig.Id}");
            GlobalFunc.PreparePanel(stageConfig.Id);
        }

        /// <summary>
        /// 获取技能卡数量
        /// </summary>
        private int GetSkillCardNum(List<BattleCardGroup> cardList)
        {
            int skillNum = 0;
            foreach (var c in cardList)
            {
                if (c.CardType == CardType.Skill || c.CardType == CardType.SkillPassive)
                    skillNum++;
            }

            return skillNum;
        }

        private void OnChapterChangedEvent(int chapter)
        {
            // 同步执行切换逻辑
            PlayChangeStageAnimations();
        }

        private void OnAfkRewardClaimed(List<RewardItem> rewards)
        {
            // 打开挂机收益面板（固定最新通关章节）
            var vm = new StageSweepingPanelViewModel();
            vm.OnCreate();
            if (Global.PlayerBattle.ClearStageId > 0)
            {
                vm.CurrentOpenId = Global.PlayerBattle.ClearStageId;
            }
            else if (Global.StageId.Value > 0)
            {
                vm.CurrentOpenId = Global.StageId.Value;
            }
            UIFramework.UIManager.OpenPanelAsync<StageSweepingPanelNew, StageSweepingPanelViewModel>(vm, UIFramework.UILevel.PopUI).Forget();
        }

        private void OnStageFirstRewardClaimed(Dictionary<string, int> rewards)
        {
            // 显示首通奖励
            GlobalFunc.ShowRewardTips(rewards);
        }

        private void OnRankingRequested()
        {
            // 使用新的 UIFramework 打开排行榜界面
            // var vm = new Game.Achievement.AchievementPanelViewModel();
            // vm.OnCreate();
            //
            // UIFramework.UIManager.OpenPanelAsync<AchievementPanelNew, Game.Achievement.AchievementPanelViewModel>(vm, UIFramework.UILevel.PopUI).Forget();
        }

        private void OnSettingRequested()
        {
            var vm = new Game.Setting.SettingPanelViewModel();
            vm.OnCreate();
            UIFramework.UIManager
                .OpenPanelAsync<SettingPanelNew, Game.Setting.SettingPanelViewModel>(vm, UIFramework.UILevel.PopUI)
                .Forget();
        }

        private void OnBulletinRequested()
        {
            // TODO: 等待 BulletinPanel 迁移完成后使用新框架打开
            LogTool.Log("[MainInterfacePanelNew] 打开公告界面（等待迁移）");
        }

        private void OnTaskRequested()
        {
            var vm = new Game.Task.TaskPanelViewModel();
            vm.OnCreate();

            UIFramework.UIManager
                .OpenPanelAsync<TaskPanelNew, Game.Task.TaskPanelViewModel>(vm, UIFramework.UILevel.PopUI).Forget();
        }

        private void OnBackpackRequested()
        {
            // 使用老界面打开背包面板
            UIKit.OpenPanel<BackpackPanel>(QFramework.UILevel.PopUI);
        }

        private void OnHelpRequested()
        {
            // 使用老界面打开帮助面板
            // UIKit.OpenPanel<HelpPanel>(QFramework.UILevel.PopUI);
        }

        private void OnPlayAnimationRequested()
        {
        }

        #endregion

        #region 关卡切换动画

        /// <summary>
        /// 播放关卡切换动画（同步执行）
        /// </summary>
        private void PlayChangeStageAnimations()
        {
            if (mOldPanel == null) return;

            // 播放切换动画
            if (mOldPanel.ChangeStageAnimation != null)
            {
                UIAnimationUtils.PlayAnimations(mOldPanel.ChangeStageAnimation.gameObject);
            }

            // 更新场景背景和信息
            UpdateSceneBackground();

            // 更新ViewModel中的信息（这会更新CanStartGame等状态）
            if (VM != null)
            {
                VM.UpdateStageInfo();
            }
            
        }

        private void UpdateSceneBackground()
        {
            if (VM?.CurrentStageConfig.Value == null || mOldPanel == null) return;

            var stageCfg = VM.CurrentStageConfig.Value;
        }

        

        #endregion
        

        #region 生命周期

        void Update()
        {
            // 调试模式
            if (Global.Debug.Value == 1)
            {
                if (Input.GetKeyUp(KeyCode.T))
                {
                    UIKit.OpenPanel<ToolPanel>(QFramework.UILevel.PopUI).SetMainInterfacePanel();
                }
                
                if (Input.GetKeyUp(KeyCode.F))
                {
                    GlobalFunc.ShowRewardTips(new List<RewardItem>()
                    {
                        new RewardItem(1010009,1),
                        new RewardItem(1010010,1),
                        new RewardItem(1010011,1),
                        new RewardItem(1010001,1),
                        new RewardItem(1010002,1),
                        new RewardItem(1010007,1),
                        new RewardItem(1010004,1),



                    });
                }
            }
        }

        protected override void OnOpen(UIFramework.IUIData uiData = null)
        {
            PlayAnimation();
            // 显示当前章节的背景和信息
            if (VM != null)
            {
                UpdateSceneBackground();
                VM.UpdateStageInfo();
                VM.UpdateCanGoButtons();

                // 刷新所有UI状态（使用统一方法，避免重复代码）
                RefreshAllUIState();
            }

           

            // 每日弹出逻辑
            DailyPopUpLogic();

            // 刷新红点
            RefreshRedPoint();

            // 检查当前关卡类型（挑战Boss/无尽塔自动跳转）
            CheckCurrentStageTypeLogic();
            RefreshLevelGatedButtons();
            RefreshPlayerQueue();
        }

        protected override void OnShow()
        {
            PlayAnimation();
            // 每日弹出逻辑
            DailyPopUpLogic();
            RefreshLevelGatedButtons();
            RefreshPlayerQueue();
        }

        protected override void OnHide()
        {
        }

        #endregion

        #region 常驻子界面逻辑（保持现状）

        /// <summary>
        /// 每日弹出逻辑（与原界面DailyPopUpLogic一致）
        /// </summary>
        public void DailyPopUpLogic()
        {
            if (!gameObject.activeSelf) return;
            if (UIManager.GetPanel<StageSweepingPanelNew>() != null)
            {
                return;
            }
            // 检查模块解锁等级
            List<ModuleFunctionType> displayList = CheckModuleUnlockLv();
            if (displayList.Count > 0 && Global.User.LastDisplayLevel < Global.User.Level)
            {
                Global.User.LastDisplayLevel = Global.User.Level;
                OpenUpgradePanel();
                return;
            }
            if (CheckTriggerGuide())
            {
                ShowGuideAll();
                return;
            }
        }

        /// <summary>
        /// 打开升级解锁面板（UIFramework 异步加载预制体，WebGL 安全）
        /// </summary>
        private void OpenUpgradePanel()
        {
            var vm = new UpgradePanelViewModel();
            vm.OnCreate();
            vm.Setup(Global.Player.ModuleUnlockLv, Global.User.Level);
            vm.OnClosed = OnUpgradePanelClosed;
            UIManager.OpenPanelAsync<UpgradePanelNew, UpgradePanelViewModel>(vm, UIFramework.UILevel.PopUI).Forget();
        }

        private void OnUpgradePanelClosed()
        {
            DailyPopUpLogic();
        }

        /// <summary>
        /// 检查模块解锁等级
        /// </summary>
        private List<ModuleFunctionType> CheckModuleUnlockLv()
        {
            var displayList = new List<ModuleFunctionType>();
            int lastDisplay = Global.User.LastDisplayLevel;
            int currentLevel = Global.User.Level;
            foreach (var item in DataConfigManager.Tables.TbModuleFunctionConfig.DataList)
            {
                // 只收集「上次展示等级之后」新解锁且需要飞图标的模块，避免历史 FlyIcon 挡住引导
                if (item != null
                    && item.FlyIcon
                    && item.UnlockUserLv > lastDisplay
                    && item.UnlockUserLv <= currentLevel)
                {
                    displayList.Add(item.ModuleType);
                }
            }

            return displayList;
        }

        /// <summary>
        /// 检查是否触发引导
        /// </summary>
        private bool CheckTriggerGuide()
        {
            UnlockGuideManager.Instance.StartGuide();
            if (UnlockGuideManager.Instance.CurrentModuleFunction != ModuleFunctionType.None)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 显示所有引导
        /// </summary>
        private void ShowGuideAll()
        {
            //检查如果是引导就添加节点


            if (!gameObject.activeSelf) return;
            UnlockGuideManager.Instance.ShowGuide();
        }

        /// <summary>
        /// 刷新红点（与原界面RefreshRedPoint一致）
        /// </summary>
        private void RefreshRedPoint()
        {
            if (mOldPanel?.StageAfkBtn != null)
            {
                mOldPanel.StageAfkBtn.gameObject.SetActive(Global.PlayerBattle.AfkTime != null);
            }

            // 发布红点变化消息
            Global.Publisher.Publish(MessagePublisher.CHANGE_RED_TYPE + RedNodeType.MainPageAFKBtn, null);

            // 更新挂机进度文本
            if (mOldPanel?.AfkProgressText != null && VM?.CurrentStageConfig.Value != null)
            {
                var stageCfg = VM.CurrentStageConfig.Value;
                mOldPanel.AfkProgressText.SetData(LocalizationModule.UITexts, "UI_STAGE_CHAPTERNAME",
                    stageCfg.ChapterId);
            }
        }

        /// <summary>
        /// 检查当前关卡类型（挑战Boss/无尽塔自动跳转）
        /// </summary>
        private void CheckCurrentStageTypeLogic()
        {
            var stageCfg = DataConfigManager.Tables.TbStageBattleConfig.GetOrDefault(Global.StageId.Value);
            if (stageCfg != null)
            {
                switch (stageCfg.StageType)
                {
                    case StageType.ChallengeBoss:
                        DOTween.To(v => { }, 0, 0, 0.5f).SetEase(Ease.Linear).onComplete += () =>
                        {
                            var panel = GameMainPanel.Default.GotoBigWorldPanel();
                            panel?.OpenChallengeBossPanel();
                        };
                        break;

                    case StageType.EndlessTower:
                        DOTween.To(v => { }, 0, 0, 0.5f).SetEase(Ease.Linear).onComplete += () =>
                        {
                            var panel = GameMainPanel.Default.GotoBigWorldPanel();
                            panel?.OpenEndlessTowerPanel();
                        };
                        break;
                }
            }
        }

        #endregion
    }
}
