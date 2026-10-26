using System;
using System.Collections.Generic;
using System.Linq;
using Config;
using DG.Tweening;
using FusionContinent.Animation;
using FusionContinent.Model;
using FusionContinent.Utils;
using Game.UI;
using Newtonsoft.Json;
using TMPro;
using UIFramework;
using UIFramework.Services;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Cardwar;
using UserCreatePlayerActivity = FusionContinent.Model.UserCreatePlayerActivity;
using Game.RedDot;

namespace FusionContinent
{
    [PrefabPath("StageSweepingPanel")]
    public class StageSweepingPanelNew : UIPanel<StageSweepingPanelViewModel>
    {
        private StageSweepingPanel mOldPanel;

        private TbItemConfig _itemConfig;
        private TbRewardGroupConfig _rewardGroupConfig;
        private TbStageBattleConfig _stageBattleConfig;
        private TbChapterStageConfig _chapterStageConfig;
        private TbSoldierBattleConfig _soldierBattleConfig;

        private int _afkTimeProduce;
        private int _afkTimeLimitMax;
        private int _dailyStageSweepingNum;
        private float _afkTime;
        private bool _isClosing;
        private bool _isHidden;
        private bool _runDailyPopupOnHide = true;
        private UnityAction _onPanelHidden;

        // 按钮 handler
        private UnityEngine.Events.UnityAction mCloseHandler1;
        private UnityEngine.Events.UnityAction mCloseHandler2;
        private UnityEngine.Events.UnityAction mReceiveHandler;
        private UnityEngine.Events.UnityAction mHarvestHandler;

        void Awake()
        {
            mOldPanel = GetComponent<StageSweepingPanel>();
            if (mOldPanel == null)
            {
                LogTool.LogError("[StageSweepingPanelNew] 找不到 StageSweepingPanel 组件");
                return;
            }

            // 禁用旧 Panel 组件，避免其 Update() 与新 Panel 冲突
            mOldPanel.enabled = false;

            _itemConfig = DataConfigManager.Tables.TbItemConfig;
            _rewardGroupConfig = DataConfigManager.Tables.TbRewardGroupConfig;
            _stageBattleConfig = DataConfigManager.Tables.TbStageBattleConfig;
            _chapterStageConfig = DataConfigManager.Tables.TbChapterStageConfig;
            _soldierBattleConfig = DataConfigManager.Tables.TbSoldierBattleConfig;

            _afkTimeProduce = DataConfigManager.Tables.TbGameBattleConst.AfkTimeProduce;
            _afkTimeLimitMax = DataConfigManager.Tables.TbGameBattleConst.AfkTimeLimitMax;
            _dailyStageSweepingNum = DataConfigManager.Tables.TbGameBattleConst.DailyStageSweepingNum;
        }

        protected override void OnBindViewModel(StageSweepingPanelViewModel vm)
        {
            if (mOldPanel == null) return;

            mCloseHandler1 = () => vm.OnCloseCommand.Execute();
            mCloseHandler2 = () => vm.OnCloseCommand.Execute();
            mReceiveHandler = () => vm.OnReceiveAfkRewardsCommand.Execute();
            mHarvestHandler = () => vm.OnHarvestSweepingCommand.Execute();

            mOldPanel.Mask.onClick.AddListener(mCloseHandler1);
            mOldPanel.ExitBtn.onClick.AddListener(mCloseHandler2);
            mOldPanel.ReceiveBtn.onClick.AddListener(mReceiveHandler);
            mOldPanel.HarvestBtn.onClick.AddListener(mHarvestHandler);

            vm.OnCloseRequested += RequestClosePanel;
            vm.OnReceiveAfkRewardsRequested += ReceiveAfkRewards;
            vm.OnHarvestSweepingRequested += HarvestSweepingRewards;

            mOldPanel.ReceiveBtn.SetRedType(RedNodeType.AFKPageClaimBtn);
        }

        protected override void OnUnbindViewModel(StageSweepingPanelViewModel vm)
        {
            if (mOldPanel == null) return;

            if (mCloseHandler1 != null) { mOldPanel.Mask.onClick.RemoveListener(mCloseHandler1); mCloseHandler1 = null; }
            if (mCloseHandler2 != null) { mOldPanel.ExitBtn.onClick.RemoveListener(mCloseHandler2); mCloseHandler2 = null; }
            if (mReceiveHandler != null) { mOldPanel.ReceiveBtn.onClick.RemoveListener(mReceiveHandler); mReceiveHandler = null; }
            if (mHarvestHandler != null) { mOldPanel.HarvestBtn.onClick.RemoveListener(mHarvestHandler); mHarvestHandler = null; }

            vm.OnCloseRequested -= RequestClosePanel;
            vm.OnReceiveAfkRewardsRequested -= ReceiveAfkRewards;
            vm.OnHarvestSweepingRequested -= HarvestSweepingRewards;
        }

        protected override void OnOpen(IUIData uiData = null)
        {
            if (VM == null || mOldPanel == null) return;

            _isClosing = false;
            _isHidden = false;
            _runDailyPopupOnHide = true;
            _onPanelHidden = null;

            RefreshUI();

            if (mOldPanel.OpenAnimation != null)
            {
                UIAnimationUtils.PlayAnimations(mOldPanel.OpenAnimation.gameObject);
            }
        }

        protected void Update()
        {
            _afkTime = BattleModel.GetAfkDuration();
            if (_afkTime > _afkTimeLimitMax) _afkTime = _afkTimeLimitMax;
            SetSweepingTime();
        }

        #region UI Refresh

        private void RefreshUI()
        {
            // 挂机页固定展示最新通关章节，不跟主界面左右切换的浏览关卡走
            VM.CurrentOpenId = ResolveLatestStageId();

            var stageCfg = _stageBattleConfig.GetOrDefault(VM.CurrentOpenId);
            if (stageCfg != null)
            {
                mOldPanel.StageName.Find("Text").GetComponent<TMP_Text>().text =
                    $"第{stageCfg.ChapterId}章  {stageCfg.StageName}";
            }

            _afkTime = BattleModel.GetAfkDuration();
            if (_afkTime > _afkTimeLimitMax) _afkTime = _afkTimeLimitMax;

            mOldPanel.MaxAFKTextDes.GetComponent<TMP_Text>().text = $"最大挂机{_afkTimeLimitMax / 3600}小时";
            SetSweepingTime();

            var sweepingNum = GetSweepingNum();
            var sweepingNumText = sweepingNum < 0 ? "无限" : sweepingNum.ToString();
            mOldPanel.HarvestNum.text = $"快速收获次数：{sweepingNumText}次";
            mOldPanel.HarvestBtn.interactable = sweepingNum != 0;

            ShowAfkRewards();
            ShowRewardsLayout();
        }

        /// <summary>
        /// 最新通关关卡：优先 ClearStageId，未通关时回退 StageId。
        /// </summary>
        private int ResolveLatestStageId()
        {
            if (Global.PlayerBattle.ClearStageId > 0)
                return Global.PlayerBattle.ClearStageId;

            if (Global.StageId.Value > 0)
                return Global.StageId.Value;

            return VM.CurrentOpenId;
        }

        /// <summary>
        /// 解析用于挂机收益展示的关卡配置（始终对应当前最新通关章节）。
        /// </summary>
        private StageBattleConfig ResolveRewardStageConfig()
        {
            return _stageBattleConfig.GetOrDefault(ResolveLatestStageId());
        }

        /// <summary>
        /// 在 RewardsLayout 下的 RewardItem 上展示当前通关章节的每小时挂机奖励。
        /// 数据来源：ChapterStageConfig.AfkRewards（按 AfkTimeProduce=3600s 归一化为每小时数量）。
        /// </summary>
        private void ShowRewardsLayout()
        {
            if (mOldPanel.RewardsLayout == null) return;

            var stageCfg = ResolveRewardStageConfig();
            if (stageCfg == null)
            {
                LogTool.LogWarning($"[StageSweepingPanelNew] 无法解析挂机收益关卡 ClearStageId={Global.PlayerBattle.ClearStageId} StageId={Global.StageId.Value}");
                return;
            }

            var chapterCfg = _chapterStageConfig.GetOrDefault(stageCfg.ChapterId);
            if (chapterCfg == null || chapterCfg.AfkRewards == null || chapterCfg.AfkRewards.Count == 0)
            {
                LogTool.LogWarning($"[StageSweepingPanelNew] 章节{stageCfg.ChapterId}无 AfkRewards 配置");
                return;
            }

            // AfkTimeProduce 为产出周期（秒），换算成每小时的倍数
            float hourMultiple = _afkTimeProduce > 0 ? 3600f / _afkTimeProduce : 1f;

            var layout = mOldPanel.RewardsLayout;
            var template = layout.GetChild(0);

            for (int i = 0; i < chapterCfg.AfkRewards.Count; i++)
            {
                var rewardData = chapterCfg.AfkRewards[i];
                if (rewardData == null) continue;

                Transform rewardItem = i < layout.childCount
                    ? layout.GetChild(i)
                    : Instantiate(template.gameObject, layout, false).transform;
                rewardItem.gameObject.SetActive(true);

                var iconImg = rewardItem.Find("Icon").GetComponent<Image>();
                var itemCfg = _itemConfig.GetOrDefault(rewardData.Id);
                iconImg.sprite = Global.ItemIconSpriteAtlas.GetSpriteExt(itemCfg.Icon);

                var detailText = rewardItem.Find("TextDetail").GetComponent<TMP_Text>();
                int perHour = Mathf.RoundToInt(rewardData.Num * hourMultiple);
                detailText.text = $"{perHour}/小时";
            }

            for (int i = chapterCfg.AfkRewards.Count; i < layout.childCount; i++)
            {
                layout.GetChild(i).gameObject.SetActive(false);
            }
        }

        private void SetSweepingTime()
        {
            // _afkTime = BattleModel.GetAfkDuration();
            // if (_afkTime > _afkTimeLimitMax) _afkTime = _afkTimeLimitMax;
            var produceNum = Mathf.FloorToInt(_afkTime / _afkTimeProduce);
            mOldPanel.SweepingProgress.GetComponent<Slider>().value = Mathf.Clamp01(_afkTime / _afkTimeLimitMax);
            mOldPanel.SweepingTime.GetChild(0).GetComponent<TMP_Text>().text = $"{TimeTool.FormatDurationWithYm((int)_afkTime)}";
            mOldPanel.ReceiveBtn.interactable = produceNum > 0;
        }

        private void ShowAfkRewards()
        {
            var stageCfg = ResolveRewardStageConfig();
            if (stageCfg == null) return;

            var chapterCfg = _chapterStageConfig.GetOrDefault(stageCfg.ChapterId);
            if (chapterCfg == null) return;
            if (chapterCfg.AfkRewards.Count == 0 && chapterCfg.AfkRewardGroups.Count == 0) return;

            var produceNum = Mathf.FloorToInt(_afkTime / _afkTimeProduce);
            var rewards = RewardModel.ToRewardItems(chapterCfg.AfkRewards, produceNum);

            foreach (var group in chapterCfg.AfkRewardGroups)
            {
                var groupCfg = _rewardGroupConfig.GetOrDefault(group.Id);
                if (groupCfg == null) continue;
                rewards.AddRange(groupCfg.RewardGroupList.Select(rew => new RewardItem(rew.ItemId, rew.ItemNum * produceNum)));
            }

            var listScroll = mOldPanel.RewardScroll.transform.Find("Viewport/Content");
            // 所有奖励（含金币）都作为普通节点显示，列表索引与 Content 子节点一一对应。
            for (int r = 0; r < rewards.Count; r++)
            {
                RewardItem item = rewards[r];
                GameObject rewObj = r >= listScroll.childCount
                    ? Instantiate(mOldPanel.RewardEntity.gameObject, listScroll, false)
                    : listScroll.GetChild(r).gameObject;

                rewObj.SetActive(produceNum > 0);
                rewObj.GetComponent<RewardEntity>().SetData(item);
            }
            // 清理本次未使用到的旧节点，避免重复打开面板时残留上一轮的数据
            for (int i = rewards.Count; i < listScroll.childCount; i++)
            {
                listScroll.GetChild(i).gameObject.SetActive(false);
            }
        }

        private int GetSweepingNum()
        {
            if (!UserCreatePlayerActivity.CheckMonthCardExpired())
                return -1;

            var sweepingNum = _dailyStageSweepingNum - Global.PlayerBattle.DailyStageSweepingNum;
            return sweepingNum < 0 ? 0 : sweepingNum;
        }

        #endregion

        #region Network Actions

        private async void ReceiveAfkRewards()
        {
            try
            {
                var proto = await WebDataFun.SendProtoPostRequest<ChapterAfkRewardsResponse>(ApiKeys.ApiBattleChapterAfkRewards, null);
                if (proto != null)
                {
                    Global.PlayerBattle = ProtoMappers.MapPlayerBattle(proto.Battle);
                    Items.Update(proto.Items);

                    // 使用 RewardTipsService 替代旧 RewardTips
                    var rewardsDict = new Dictionary<string, int>();
                    if (proto.Rewards != null)
                    {
                        foreach (var kv in proto.Rewards)
                        {
                            rewardsDict[kv.Key.ToString()] = (int)kv.Value;
                        }
                    }
                    await GameServiceManager.GetOrCreate<RewardTipsService>().ShowRewardTips(rewardsDict);

                    // 数据已更新（AfkTime 被服务器重置），立即同步发布红点刷新消息，
                    // 让 AFKPageClaimBtn / MainPageAFKBtn 重新 Evaluate → CheckAfkProduce() 返回 false → 红点消失。
                    // 不能把刷新消息塞进延迟 tween 回调——ClosePanel 会隐藏/销毁面板，
                    // 该延迟回调生命周期不受控，可能不执行，导致红点一直不消失。
                    Global.Publisher.Publish(MessagePublisher.CHANGE_RED_TYPE + RedNodeType.MainPageAFKBtn, null);

                    ClosePanel();
                }
            }
            catch (Exception ex)
            {
                LogTool.LogError($"领取奖励失败：{ex.Message}");
                Toast.Show(ex.Message);
            }
        }

        private async void HarvestSweepingRewards()
        {
            try
            {
                BattleModel.Sweep(Global.PlayerBattle.ClearStageId, () =>
                {
                    DOTween.To(v => { }, 0, 0, 0.2f).onComplete += RefreshUI;
                });
            }
            catch (Exception ex)
            {
                LogTool.LogError($"领取奖励失败：{ex.Message}");
                Toast.Show(ex.Message);
            }
        }

        #endregion

        #region Open / Close

        private void RequestClosePanel()
        {
            _runDailyPopupOnHide = true;
            PlayCloseAnimation(HidePanelInternal);
        }

        private void PlayCloseAnimation(UnityAction onComplete)
        {
            if (_isClosing) return;

            _onPanelHidden = () =>
            {
                var callback = onComplete;
                onComplete = null;
                callback?.Invoke();
            };

            if (mOldPanel.CloseAnimation != null)
            {
                _isClosing = true;
                UIAnimationUtils.PlayAnimations(mOldPanel.CloseAnimation.gameObject, HidePanelInternal);
                return;
            }

            HidePanelInternal();
        }

        private void HidePanelInternal()
        {
            // 同一次关闭只走一次（动画回调与直接关闭路径可能重复进入），保证关闭事件只发布一次
            if (_isHidden) return;
            _isHidden = true;

            var listScroll = mOldPanel.RewardScroll.transform.Find("Viewport/Content");
            for (int r = 0; r < listScroll.childCount; r++)
            {
                listScroll.GetChild(r).gameObject.SetActive(false);
            }

            _isClosing = false;

            if (_runDailyPopupOnHide)
            {
                // MainInterfacePanel.Default?.DailyPopUpLogic();
            }

            var callback = _onPanelHidden;
            _onPanelHidden = null;
            callback?.Invoke();

            CloseSelf();

            // 挂机界面关闭后广播事件，由主界面监听并延迟一帧触发每日弹出逻辑
            if (_runDailyPopupOnHide)
            {
                Global.Publisher.Publish(MessagePublisher.STAGE_SWEEPING_PANEL_CLOSED, null);
            }
        }

        private void ClosePanel()
        {
            HidePanelInternal();
        }

        #endregion
    }
}
