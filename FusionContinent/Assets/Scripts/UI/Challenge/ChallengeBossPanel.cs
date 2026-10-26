using System;
using System.Collections.Generic;
using System.Linq;
using Cardwar;
using Config;
using FusionContinent.Model;
using FusionContinent.Utils;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using TMPro;
using PlayerChallengeBoss = FusionContinent.Model.PlayerChallengeBoss;

namespace FusionContinent
{
    public class ChallengeBossPanelData : UIPanelData
    {
    }

    public partial class ChallengeBossPanel : UIPanel
    {
        public static ChallengeBossPanel Instance;
        
        private string _dragonSpine;
        private PlayerChallengeBoss _boss;
        private StageBattleConfig _stageBattleCfg;
        private ChallengeBossConfig _challengeBossCfg;
        
        private TbStageBattleConfig _stageBattleConfig;
        private TbMonsterBattleConfig _monsterBattleConfig;
        private TbChallengeBossConfig _challengeBossConfig;

        protected void Awake()
        {
            Instance = this;
            _challengeBossConfig = DataConfigManager.Tables.TbChallengeBossConfig;
            _stageBattleConfig = DataConfigManager.Tables.TbStageBattleConfig;
            _monsterBattleConfig = DataConfigManager.Tables.TbMonsterBattleConfig;
        }

        protected override void OnInit(IUIData uiData = null)
        {
            mData = uiData as ChallengeBossPanelData ?? new ChallengeBossPanelData();
            ReturnBtn.onClick.AddListener(OnClose);
            Rules.onClick.AddListener(() =>
            {
                UIKit.OpenPanel<RulesPopupPanel>().SetData($"{_challengeBossCfg.Name}规则", _challengeBossCfg.RulesDesc);
            });
            RankingBtn.onClick.AddListener(() =>
            {
                // UIKit.OpenPanel<ChallengeRankingPanel>().SetData(_challengeBossCfg.RankRewardList, StageType.ChallengeBoss);
            });
            DailyBtn.onClick.AddListener(() =>
            {
                var canReceive = _boss.DailyRewardList.Count == 0;
                UIKit.OpenPanel<DailyRewardsPanel>().SetData(new DailyRewardsPanelData()
                {
                    TitleText = "每日挑战奖励",
                    TipText = "每日进行一次挑战可领取",
                    CanReceive = _boss.DailyChallengeNum > 0 && canReceive,
                    RewardItems = _challengeBossCfg.DailyChallengeRewards,
                    ReceiveBtnText = canReceive ? "领取" : "已领取",
                    ReceiveCallback = ReceiveDailyRewards,
                });
            });
            ChallengeBtn.onClick.AddListener(PlayGame);
            ShopBtn.onClick.AddListener(OpenShop);
            
            // 遍历当前开启的Boss
            foreach (var v in _challengeBossConfig.DataList)
            {
                if (v.Status == ActivityStatusType.Open)
                {
                    _challengeBossCfg = v;
                    break;
                }
            }
        }

        public void SetData()
        {
            if (_challengeBossCfg == null)
            {
                FusionContinent.Toast.Show("暂未开放");
                OnClose();
                return;
            }

            if (!Global.PlayerChallengeBoss.TryGetValue(_challengeBossCfg.Id, out _boss))
            {
                FusionContinent.Toast.Show("暂未开放2");
                OnClose();
                return;
            }

            var challengeNum = _challengeBossCfg.DailyChallengeNum - _boss.DailyChallengeNum;
            Title.transform.Find("BossName").GetComponent<TMP_Text>().text = _challengeBossCfg.Name;
            ChallengeNum.transform.Find("CurrentValue").GetComponent<TMP_Text>().text = $"{challengeNum}";
            ChallengeNum.transform.Find("MaxValue").GetComponent<TMP_Text>().text =$"/{ _challengeBossCfg.DailyChallengeNum.ToString()}";
            ChallengeBtn.interactable = challengeNum > 0;

            _stageBattleCfg = _stageBattleConfig.GetOrDefault(_challengeBossCfg.StageId);
            if (_stageBattleCfg == null)
            {
                FusionContinent.Toast.Show($"配置关卡:{_challengeBossCfg.StageId}错误");
                OnClose();
                return;
            }

            var monsterCfg = _monsterBattleConfig.GetOrDefault(_stageBattleCfg.ShowBoss);
            if (monsterCfg == null)
            {
                FusionContinent.Toast.Show($"配置BOSS:{_stageBattleCfg.ShowBoss}错误");
                OnClose();
                return;
            }

            // Spine
            var dragonSpine = monsterCfg.Spine;
            if (dragonSpine != _dragonSpine)
            {
                _dragonSpine = dragonSpine;
                var soliderSpine = Global.ResLoader.LoadSync<GameObject>(_dragonSpine).Instantiate().transform;
                soliderSpine.SetParent(Dragon);
                soliderSpine.localScale = Vector3.one;
                soliderSpine.localPosition = Vector3.zero;
                // Dragon.localScale = monsterCfg.SpineScale > 0 ? Vector3.one * soldierCfg.SpineScale : Vector3.one;
            }

            var totalScore = _challengeBossCfg.ScoreRewardList.Last().Score;
            for (int i = 0; i < _challengeBossCfg.ScoreRewardList.Count; i++)
            {
                if (i > 2) break;
                
                var v = _challengeBossCfg.ScoreRewardList[i];
                
                // 宝箱
                var isReceive = _boss.ScoreRewardList.Contains(v.Score);
                Accumulateddamage.transform.Find($"Image/TreasureChest{i + 1}").GetComponent<TreasureChest>().SetData(new TreasureChestData(){
                    Icon = isReceive ? "SilverOpen" : "Silver",
                    StyleType = TreasureChestStyleType.Reward,
                    CanReceive = _boss.TotalScore >= v.Score && !isReceive,
                    ProgressText = v.Score.ToString(),
                    Rewards = v.Rewards,
                    OnClick = () =>
                    {
                        ReceiveScoreRewards(v.Score);
                    }
                });
            }

            // 进度条
            YellowProgressBar.fillAmount = Mathf.Clamp01((float)_boss.TotalScore / (float)totalScore);
            YellowProgressBar.transform.Find("TextValue").GetComponent<TMP_Text>().text = _boss.TotalScore.ToString();
            Accumulateddamage.transform.Find("Text").GetComponent<TMP_Text>().text = $"本周累计伤害：{_boss.TotalDamage}";
            
            // 每日奖励红点
            DailyBtn.transform.Find("RedPoint").gameObject.SetActive(_boss.DailyChallengeNum > 0 && _boss.DailyRewardList.Count == 0);
        }


        private void PlayGame()
        {
            if (_stageBattleCfg == null) return;
            GlobalFunc.PreparePanel(_stageBattleCfg.Id);				
            // LogTool.Log($"点击开始游戏，关卡ID: {_stageBattleCfg.Id}");
            // ChallengeBtn.interactable = false;
            // UIKit.ClosePanel<BigWorldPanel>();
            // UIKit.ClosePanel<ChallengeBossPanel>();
				        //
            // // 设置关卡Id
            // Global.StageId.Value = _stageBattleCfg.Id;
            // MainInterfacePanel.Default.PlayGame();
        }

        private async void ReceiveScoreRewards(int score)
        {
            if (_boss == null) return;

            try
            {
                var request = new ReceiveChallengeBossScoreRewardRequest { BossId = _boss.BossId, Score = score };
                var proto = await WebDataFun.SendProtoPostRequest<ReceiveChallengeBossScoreRewardResponse>(ApiKeys.ApiChallengeBossReceiveScoreReward, request);
                if (proto != null && proto.Boss != null)
                {
                    _boss = ProtoMappers.MapPlayerChallengeBoss(proto.Boss);
                    Global.PlayerChallengeBoss[_boss.BossId] = _boss;

                    // 同步奖励
                    Items.Update(proto.Items);

                    var rewardsDict = new Dictionary<string, int>();
                    if (proto.Rewards != null)
                    {
                        foreach (var kv in proto.Rewards)
                        {
                            rewardsDict[kv.Key.ToString()] = (int)kv.Value;
                        }
                    }

                    // 奖励
                    // RewardTips rewardTips = UIKit.OpenPanel<RewardTips>(UILevel.PopUI);
                    // rewardTips.SetData(rewardsDict);
                    SetData();
                }
            }
            catch (System.Exception ex)
            {
                LogTool.LogError($"请求失败：{ex.Message}");
                FusionContinent.Toast.Show($"操作失败：{ex.Message}");
            }
            finally
            {
            }
        }
        
        /**
         * 领取每日奖励
         * 
         * @return 
         * @author Zpp
         * @create 2025/10/30
         **/
        private async void ReceiveDailyRewards()
        {
            if (_boss == null) return;

            try
            {
                var request = new ReceiveChallengeBossDailyRewardRequest { BossId = _boss.BossId };
                var proto = await WebDataFun.SendProtoPostRequest<ReceiveChallengeBossDailyRewardResponse>(ApiKeys.ApiChallengeBossReceiveDailyReward, request);
                if (proto != null && proto.Boss != null)
                {
                    _boss = ProtoMappers.MapPlayerChallengeBoss(proto.Boss);
                    Global.PlayerChallengeBoss[_boss.BossId] = _boss;

                    // 同步奖励
                    Items.Update(proto.Items);

                    var rewardsDict = new Dictionary<string, int>();
                    if (proto.Rewards != null)
                    {
                        foreach (var kv in proto.Rewards)
                        {
                            rewardsDict[kv.Key.ToString()] = (int)kv.Value;
                        }
                    }

                    // 奖励
                    // RewardTips rewardTips = UIKit.OpenPanel<RewardTips>(UILevel.PopUI);
                    // rewardTips.SetData(rewardsDict);

                    // 刷新数据
                    SetData();
                }
            }
            catch (System.Exception ex)
            {
                LogTool.LogError($"请求失败：{ex.Message}");
                FusionContinent.Toast.Show($"操作失败：{ex.Message}");
            }
            finally
            {
            }
        }

        private void OpenShop()
        {
            ShopPanel.gameObject.SetActive(true);
            var shopPanel = ShopPanel.Find("NormalShopList01").GetComponent<NormalShopList01>();
            shopPanel.SetInitShopItem(ShopType.Challenge);
        }

        protected override void OnOpen(IUIData uiData = null)
        {
            Global.Publisher.Publish(MessagePublisher.CHANGE_GAME_MAIN_FOOT_BTN_ENABLED, false);
            SetData();
        }

        protected override void OnShow()
        {
        }

        protected override void OnHide()
        {
        }

        protected override void OnClose()
        {
            if (ShopPanel.gameObject.activeSelf)
            {
                ShopPanel.gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(false);
            Global.Publisher.Publish(MessagePublisher.CHANGE_GAME_MAIN_FOOT_BTN_ENABLED, true);
        }
    }
}