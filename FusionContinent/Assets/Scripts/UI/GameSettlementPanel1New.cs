using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Localization;
using Config;
using FusionContinent.Helper;
using FusionContinent.Model;
using FusionContinent.Sounds;
using Game.Battle;
using Helper;
using Manager;
using QFramework;
using UIFramework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FusionContinent.Utils;
using UIRoot = UIFramework.UIRoot;

namespace FusionContinent
{
    [PrefabPath("GameSettlementPanel1")]
    public class GameSettlementPanel1New : UIPanel<GameSettlementPanel1ViewModel>
    {
        private GameSettlementPanel1 mOldPanel;
        private ProgressBarSlider mLastBar;

        private UnityEngine.Events.UnityAction mBackBtnHandler;
        private UnityEngine.Events.UnityAction mAgineBtnHandler;
        private UnityEngine.Events.UnityAction mDoubleRewardBtnHandler;

        void Awake()
        {
            mOldPanel = GetComponent<GameSettlementPanel1>();
            if (mOldPanel == null)
            {
                LogTool.LogError("[GameSettlementPanel1New] 找不到 GameSettlementPanel1 组件");
            }
        }

        protected override void OnBindViewModel(GameSettlementPanel1ViewModel vm)
        {
            if (mOldPanel == null) return;

            CommonItemPoolManager.Instance.ReturnAll();

            // 返回按钮
            mBackBtnHandler = () =>
            {
                GamePlayManager.Default.BackHome();
                CloseSelf();
            };
            mOldPanel.BackBtn.onClick.RemoveAllListeners();
            mOldPanel.BackBtn.onClick.AddListener(mBackBtnHandler);

            // 再来一次按钮
            mAgineBtnHandler = () =>
            {
                mOldPanel.AgineBtn.interactable = false;
                GamePlayManager.Default.RestartGame(() =>
                {
                    CloseSelf();
                    SceneLoader.Load("GamePlay");
                }, () =>
                {
                    if (mOldPanel?.AgineBtn != null) mOldPanel.AgineBtn.interactable = true;
                });
            };
            mOldPanel.AgineBtn.onClick.RemoveAllListeners();
            mOldPanel.AgineBtn.onClick.AddListener(mAgineBtnHandler);

            // 双倍奖励按钮
            mDoubleRewardBtnHandler = OnDoubleRewardClicked;
            mOldPanel.DoubleRewardBtn.onClick.RemoveAllListeners();
            mOldPanel.DoubleRewardBtn.onClick.AddListener(mDoubleRewardBtnHandler);

            vm.OnDataRefreshed += RefreshUI;
            vm.OnShowError += ShowError;
            vm.OnPlaySound += PlaySound;
        }

        protected override void OnUnbindViewModel(GameSettlementPanel1ViewModel vm)
        {
            if (mOldPanel == null) return;

            if (mOldPanel.BackBtn != null && mBackBtnHandler != null)
                mOldPanel.BackBtn.onClick.RemoveListener(mBackBtnHandler);
            if (mOldPanel.AgineBtn != null && mAgineBtnHandler != null)
                mOldPanel.AgineBtn.onClick.RemoveListener(mAgineBtnHandler);
            if (mOldPanel.DoubleRewardBtn != null && mDoubleRewardBtnHandler != null)
                mOldPanel.DoubleRewardBtn.onClick.RemoveListener(mDoubleRewardBtnHandler);

            mBackBtnHandler = null;
            mAgineBtnHandler = null;
            mDoubleRewardBtnHandler = null;

            vm.OnDataRefreshed -= RefreshUI;
            vm.OnShowError -= ShowError;
            vm.OnPlaySound -= PlaySound;
        }

        protected override void OnOpen(UIFramework.IUIData uiData = null)
        {
            LogTool.Log($"[SettleTiming] 7.结算弹窗OnOpen +{GamePlayManager.SettleTimingElapsedMs():F0}ms");
            RefreshUI();
        }

        protected override void OnShow()
        {
        }

        protected override void OnHide()
        {
        }

        private void RefreshUI()
        {
            if (VM == null || mOldPanel == null) return;

            bool isWinner = VM.IsWinner.Value;

            // 按钮显隐
            mOldPanel.AgineBtn.gameObject.SetActive(!isWinner && !VM.IsPvpMode);
            if (VM.DisableRetry)
            {
                mOldPanel.AgineBtn.interactable = false;
            }
            // mOldPanel.Whear.gameObject.SetActive(isWinner);

            // 双倍奖励：仅 PVE 胜利时显示
            if (mOldPanel.DoubleRewardBtn != null)
            {
                mOldPanel.DoubleRewardBtn.gameObject.SetActive(isWinner && !VM.IsPvpMode);
            }

            // 背景
            SpriteHelper.TryLoadPaintAsync(isWinner ? "SuccessBackground" : "FailureBackground", mOldPanel.BgImg);

            // 头像框
            if (mOldPanel.HeadBox != null)
            {
                mOldPanel.HeadBox.Find("FailureBox")?.gameObject.SetActive(!isWinner);
                mOldPanel.HeadBox.Find("VictoryBox")?.gameObject.SetActive(isWinner);
            }

            // 统计面板样式
            var atlas = Global.GameSettlementBgIconAtlas;
            string textBoxKey = isWinner ? "SuccessTextBox" : "FailedTextBox";
            mOldPanel.ExpBoxBg.GetComponent<Image>().sprite = atlas.GetSprite(textBoxKey);
            mOldPanel.StatisticsBoxBg.GetComponent<Image>().sprite = atlas.GetSprite(textBoxKey);
            mOldPanel.RewardBoxBg.GetComponent<Image>().sprite = atlas.GetSprite(textBoxKey);
            // mOldPanel.StageBg.GetComponent<Image>().sprite =
            //     atlas.GetSprite(isWinner ? "SuccessfulBattleLevelFrame" : "FailedBattleLevelFrame");

            // 关卡名称
            mOldPanel.StageName.SetData(LocalizationModule.UITexts, VM.StageName.Value);

            // 奖励
            RefreshRewards();

            // 经验
            RefreshExp();

            // 伤害统计
            RefreshDamageStats();

            // 双倍奖励
            RefreshDoubleRewardNum(false);

            // 动画
            if (mLastBar != null)
            {
                var battleEndAnim = gameObject.GetComponent<UIBattleEndAnim>();
                if (battleEndAnim != null)
                {
                    battleEndAnim.OnAnimationEnd = () => mLastBar.Play();
                    battleEndAnim.PlayAnim();
                }
            }
        }

        private void RefreshRewards()
        {
            if (VM?.Result?.Rewards == null) return;

            var rewList = mOldPanel.Rewards.Find("Viewport/Content");
            foreach (Transform child in rewList)
            {
                Destroy(child.gameObject);
            }

            var resLoader = GamePlayManager.Default?._resLoader;
            if (resLoader == null) return;

            foreach (var v in VM.Result.Rewards)
            {
                var rewardObj = resLoader.Get<GameObject>("RewardEntity").Instantiate();
                rewardObj.transform.SetParent(rewList);
                rewardObj.transform.localScale = Vector3.one;
                var rewardEntity = rewardObj.GetComponent<RewardEntity>();
                rewardEntity.SetData(v.Key, v.Value);
            }
        }

        private void RefreshExp()
        {
            var lvInfo = VM.CurrentLevelInfo.Value;
            if (lvInfo == null) return;

            var expContent = mOldPanel.ExpStatistics.Find("Viewport/Content");
            if (expContent == null || expContent.childCount == 0) return;

            var expObj = expContent.GetChild(0);

            // 玩家昵称
            var nameText = expObj.Find("PlayerNameText")?.GetComponent<TMP_Text>();
            if (nameText != null)
            {
                nameText.text = Global.User?.UserName ?? string.Empty;
            }

            // 经验值：当前经验 / 升到下一级所需经验
            var lvCfg = DataConfigManager.Tables.TbPlayerLevelConfig.GetOrDefault(lvInfo.Level);
            var levelText = expObj.Find("LevelText")?.GetComponent<TMP_Text>();
            if (levelText != null && lvCfg != null)
            {
                levelText.text = $"经验值：{NumberTool.FormatNumber(lvInfo.Exp)}/{NumberTool.FormatNumber(lvCfg.Exp)}";
            }

            // 获得经验：当前 - 战斗前（支持跨级累加）
            var expLevelText = expObj.Find("ExpLevelText")?.GetComponent<TMP_Text>();
            if (expLevelText != null)
            {
                int gained = CalcGainedExp(VM.LastLevelInfo.Value, lvInfo);
                expLevelText.text = $"+{NumberTool.FormatNumber(gained)}";
            }

            expObj.gameObject.SetActive(true);

            mLastBar = expObj.Find("ProgressBarSlider").GetComponent<ProgressBarSlider>();
            if (mLastBar != null && VM.LastLevelInfo.Value != null)
            {
                mLastBar.SetProgress(VM.LastLevelInfo.Value, lvInfo);
            }

            expObj.Find("PlayerHead").GetComponent<PlayerHead>().SetData(Global.User);
        }

        /// <summary>
        /// 计算本次战斗获得的经验值。
        /// 战斗前后的等级 / 经验均为「当前等级已积累经验」（lvCfg.Exp 是从当前级升到下一级所需经验）。
        /// 跨级时需把途经的整级经验槽累加进来。
        /// </summary>
        private int CalcGainedExp(UserLevelInfo last, UserLevelInfo current)
        {
            if (current == null) return 0;
            if (last == null) return current.Exp;

            int lastLevel = last.Level;
            int curLevel = current.Level;
            int lastExp = last.Exp;
            int curExp = current.Exp;

            if (curLevel <= lastLevel)
            {
                // 同级或异常回退：直接相减
                return Mathf.Max(0, curExp - lastExp);
            }

            // 跨级：从 lastLevel 升到 curLevel 之间，每个整级经验槽全部填满
            int total = 0;
            for (int lv = lastLevel; lv < curLevel; lv++)
            {
                var cfg = DataConfigManager.Tables.TbPlayerLevelConfig.GetOrDefault(lv);
                if (cfg == null)
                {
                    // 配置缺失，无法准确计算，回退为末级差值
                    return Mathf.Max(0, curExp - lastExp);
                }
                // last 那一级先填满剩余槽位（cfg.Exp - lastExp），
                // 之后每一级都整级填满，最后一级之前的整级也计入
                total += cfg.Exp - (lv == lastLevel ? lastExp : 0);
            }
            total += curExp; // 当前级已积累经验
            return total;
        }

        private void RefreshDamageStats()
        {
            if (VM == null) return;

            // 注意：必须从 ViewModel 快照读取，而不是直接读 GamePlayManager.villageGameCards / Global.TotalDamage。
            // 因为结算面板是异步打开的，Settle() 在 OpenPanelAsync 之后会同步调用 ClearGame()，
            // 它会清空 villageGameCards 并把 TotalDamage 置 0。等到面板 OnOpen 执行时，
            // 直接读取这两个源将得到空数据，导致胜利时统计不显示。
            var snapshots = VM.DamageStats;
            var totalDamage = VM.TotalDamageSnapshot;

            var statisticList = mOldPanel.Statistics.Find("Viewport/Content");
            var statisticCard = statisticList.Find("Card")?.gameObject;

            // 清除旧数据（保留 Card 模板）
            for (int i = 1; i < statisticList.childCount; i++)
            {
                Destroy(statisticList.GetChild(i).gameObject);
            }

            if (statisticCard == null) return;

            foreach (var snap in snapshots)
            {
                GameObject cardObj = Instantiate(statisticCard, statisticList, true);
                cardObj.transform.localScale = Vector3.one;
                cardObj.SetActive(true);

                var cfg = DataConfigManager.Tables.TbCardBaseConfig.Get(snap.CardId);
                var img = cardObj.transform.Find("Icon").GetComponent<Image>();
                img.sprite = Global.HeroHeadIconAtlas.GetSpriteExt(cfg.Icon + "Little");

                float progress = snap.TotalDamage > 0 && totalDamage > 0
                    ? Mathf.Clamp01((float)snap.TotalDamage / (float)totalDamage)
                    : 0;

                // 占比百分比
                var percentText = cardObj.transform.Find("PercentText")?.GetComponent<TMP_Text>();
                if (percentText != null)
                {
                    percentText.text = $"占比：{System.Math.Round(progress * 100, 2)}%";
                }

                // 总伤害
                var damageText = cardObj.transform.Find("DamageText")?.GetComponent<TMP_Text>();
                if (damageText != null)
                {
                    damageText.text = $"伤害：{NumberTool.FormatNumber(snap.TotalDamage)}";
                }

                cardObj.transform.Find("ProgressBar/Progress").GetComponent<ProgressBar>().SetProgress(progress);
            }
        }

        private void RefreshDoubleRewardNum(bool isBtn)
        {
            if (VM == null || mOldPanel?.DoubleRewardBtn == null) return;

            var left = VM.LeftDoubleRewardNum.Value;
            var max = VM.MaxDoubleRewardNum.Value;

            var numText = mOldPanel.DoubleRewardBtn.transform.Find("NumText");
            if (numText != null)
            {
                numText.GetComponent<LocalizationText>().SetData(LocalizationModule.UITexts,
                    "UI_BTN_REMAINING_ATTEMPTS", left, max);
            }

            if (isBtn)
            {
                mOldPanel.DoubleRewardBtn.interactable = left > 0;
            }
        }

        private void OnDoubleRewardClicked()
        {
            UIRoot.Instance.transform.Find("Audio Source").GetComponent<AudioSource>().Play();
            mOldPanel.DoubleRewardBtn.interactable = false;
            VM?.OnDoubleRewardCommand.Execute();
        }

        private void ShowError(string message)
        {
            if (string.IsNullOrEmpty(message)) return;
            FusionContinent.Toast.Show(message);

            // 错误（请求失败/校验失败）后恢复双倍奖励按钮可点击
            if (mOldPanel != null && mOldPanel.DoubleRewardBtn != null && VM != null)
            {
                mOldPanel.DoubleRewardBtn.interactable = VM.LeftDoubleRewardNum.Value > 0;
            }
        }

        private void PlaySound(bool isWinner)
        {
            SoundManager.Instance.PlayVoice(isWinner ? SoundType.SoundBattleVictory : SoundType.SoundBattleDefeat);
        }
    }
}
