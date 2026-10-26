using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Config;
using FusionContinent.Model;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using TMPro;

namespace FusionContinent
{
    public class GameGemPanelData : UIPanelData
    {
    }

    public partial class GameGemPanel : UIPanel
    {
        
        private bool _isStartGame = false;
        private GameManager2048 _gameManager2048;
        private TbGameConst _tbGameConst;
        private int _currentScore;
        private TbItemConfig _tbItemConfig;
        private List<string> _itemsIcon = new List<string>();
        private bool isHaveReward = false;
        private float[] _fillArray = new[] { 0.395f, 0.311f, 0.3f };

        private void Awake()
        {
            _tbGameConst = DataConfigManager.Tables.TbGameConst;
            _tbItemConfig = DataConfigManager.Tables.TbItemConfig;
        }

        private void GameOver()
        {
            _isStartGame = false;
            UIKit.ClosePanel<GameGemPanel>();
            
            // 判断有没有获得奖励
            if (!isHaveReward)
            {
                // 打开奖励面板
                // RewardTips rewardTips = UIKit.OpenPanel<RewardTips>(UILevel.PopUI);
                // rewardTips.SetData(new List<RewardItem>(), new RewardTipsData()
                // {
                //     Title = "挑战失败",
                //     // TitleBgSprite = Global.CommonAtlas.GetSprite("Tittle3"),
                //     TipText = "未到达基础奖励分数"
                // });
            }
            else
            {
                // 领取奖励给服务器发送
                GameFinish();
            }
        }

        protected override void OnInit(IUIData uiData = null)
        {
            mData = uiData as GameGemPanelData ?? new GameGemPanelData();
            // please add init code here
            _gameManager2048 = GemGameManager.GetComponent<GameManager2048>();
            _gameManager2048.GameOverAction = GameOver;
            if (_tbGameConst.GemFuinGameRewardProgress.Count != 3)
            {
                LogTool.LogError("GemFuinGameRewardProgress   融合2048 奖励分数错误");
            }
            else
            {
                for (int i = 0; i < _tbGameConst.GemFuinGameRewardProgress.Count; i++)
                {
                    TextMeshProUGUI txt = Gem.transform.Find(i.ToString()).GetComponentInChildren<TextMeshProUGUI>();
                    txt.text = _tbGameConst.GemFuinGameRewardProgress[i].ToString();
                }
            }

            CloseBtn.onClick.AddListener(() =>
            {
                if (!_isStartGame)
                {
                    UIKit.ClosePanel<GameGemPanel>();
                }
                else
                {
                    UIKit.GetPanel<GameMainPanel>().ShowConfirm("提示", "当你退出游戏时，默认你已经完成游戏，自动为您领取奖励", GameOver);
                }
                // 弹窗
            });
            
            StartGameButton.onClick.AddListener(async () =>
            {
                UnlockGuideManager.Send(ModuleFunctionType.FusionStoneGamePlay,3);
                //判断次数
            //     if (_tbGameConst.GemFuinGameDailyNum - Global.PlayerChallenge.DailyGemFusionNum > 0)
            //     {
            //         StartGame.gameObject.SetActive(false);
            //         var response = await WebDataFun.SendPostRequest(ApiKeys.ApiStartFusionGemGame,
            //             new Dictionary<string, string>());
            //         if (!string.IsNullOrEmpty(response))
            //         {
            //             _isStartGame = true;
            //             var result = JsonConvert.DeserializeObject<GemGameResult>(response);
            //             PlayerChallenge playerChallenge = new PlayerChallenge(result.Challenge);
            //             Global.PlayerChallenge = playerChallenge;
            //             for (int i = 0; i < playerChallenge.GemFusionRewardList.Count; i++)
            //             {
            //                 ItemConfig itemConfig =
            //                     _tbItemConfig.Get(Convert.ToInt32(playerChallenge.GemFusionRewardList[i]));
            //                 Image img = Gem.transform.GetChild(i).GetChild(0).GetComponent<Image>();
            //                 img.sprite = Global.FusionGemIconSpriteAtlas.GetSprite(itemConfig.Icon);
            //                 img.GetComponent<RectTransform>().sizeDelta = new Vector2(120, 120);
            //                 _itemsIcon.Add(itemConfig.Icon);
            //                 img.GetOrAddComponent<Button>().onClick.AddListener(() =>
            //                 {
            //                     // UIKit.OpenPanel<FusionGemDetailsPanel>().SetData(itemConfig.EffectValue, 0,
            //                     //     FusionGemDetailType.Detail);
            //                 });
            //             }
            //
            //             //然后在开始
            //             _gameManager2048.NewGame();
            //             
            //             // 完成引导
            //             GuideFlow.Instance.SetStepComplete(GuideType.FusionGemChallenge);
            //         }
            //     }
            //     else
            //     {
            //         FusionContinent.Toast.Show("次数不足");
            //         //次数不足 提示
            //         // UIKit.GetPanel<GameMainPanel>().ShowTip("次数不足");
            //     }
            });
            // Global.Publisher.Subscribe(MessagePublisher.GEMGAME_SCORE_CHANGE, GemGameScoreChange);
        }

        protected override void OnOpen(IUIData uiData = null)
        {
            // UIKit.HidePanel<BigWorldPanel>();
            _isStartGame = false;
            NumText.text = $"今日剩余免费游戏次数（{_tbGameConst.GemFuinGameDailyNum - Global.PlayerChallenge.DailyGemFusionNum}）";
        }

        private void GemGameScoreChange(object score)
        {
            int _scoure = (int)score;
            if (_scoure > _currentScore)
            {
                UpdateProgress(Convert.ToInt32(_scoure));
                if (_scoure >= _tbGameConst.GemFuinGameWinNum)
                {
                    // 游戏结束  获取奖励
                    GameFinish();
                }
            }
        }

        /// <summary>
        /// 更新进度
        /// </summary>
        private void UpdateProgress(int score)
        {
            _currentScore = score;
            int index = 0;
            int gemScore = _tbGameConst.GemFuinGameRewardProgress[0];
            float sum = 0;
            int fillScore = _currentScore;
            for (int i = 0; i < _tbGameConst.GemFuinGameRewardProgress.Count; i++)
            {
                if (_currentScore >= _tbGameConst.GemFuinGameRewardProgress[i])
                {
                    CurrentGemImage.gameObject.SetActive(true);
                    CurrentGemImage.sprite = Global.ItemIconSpriteAtlas.GetSprite(_itemsIcon[i]);
                    isHaveReward = true;
                    index++;
                    int k = Math.Clamp(i + 1, 0, _tbGameConst.GemFuinGameRewardProgress.Count - 1);
                    gemScore = _tbGameConst.GemFuinGameRewardProgress[k] -
                               _tbGameConst.GemFuinGameRewardProgress[k - 1];
                    sum += _fillArray[i];
                }
                else
                {
                    break;
                }
            }

            if (index > 0)
                fillScore = _currentScore - _tbGameConst.GemFuinGameRewardProgress[index - 1];
            if (index < _tbGameConst.GemFuinGameRewardProgress.Count)
                Fill.fillAmount = sum + (_fillArray[index]) * ((float)fillScore / (float)gemScore);
            else
            {
                Fill.fillAmount = 1;
            }
        }

        private async Task GameFinish()
        {
            _isStartGame = false;
            // var formData = new Dictionary<string, string>
            // {
            //     { "Progress", _currentScore.ToString() }
            // };
            // var response = await WebDataFun.SendPostRequest(ApiKeys.ApiSettleFusionGemGame, formData);
            // if (!string.IsNullOrEmpty(response))
            // {
            //     var result = JsonConvert.DeserializeObject<GemGameResult>(response);
            //     Global.PlayerChallenge = new PlayerChallenge(result.Challenge);
            //     // 打开奖励面板
            //     RewardTips rewardTips = UIKit.OpenPanel<RewardTips>(UILevel.PopUI);
            //     rewardTips.SetData(result.Rewards, new RewardTipsData()
            //     {
            //         Title = "挑战成功",
            //     });
            //     
            //     Items.Update(result.Items);
            //     UIKit.ClosePanel<GameGemPanel>();
            // }
        }

        protected override void OnShow()
        {
            if (_tbGameConst.GemFuinGameDailyNum - Global.PlayerChallenge.DailyGemFusionNum > 0)
            {
                UnlockGuideManager.Instance.SetUnlockTypeStep(ModuleFunctionType.FusionStoneGamePlay,3,StartGameButton.GetComponent<RectTransform>());
            }
            else
            {
                UnlockGuideManager.Instance.SetErrorTypeStep(ModuleFunctionType.FusionStoneGamePlay,3);
                
            }

            // StartGameButton
        }

        protected override void OnHide()
        {
        }

        protected override void OnClose()
        {
            Global.Publisher.Unsubscribe(MessagePublisher.GEMGAME_SCORE_CHANGE, GemGameScoreChange);
            var vm = new Game.Challenge.BigWorldPanelViewModel();
            vm.OnCreate();
            UIFramework.UIManager.OpenPanelAsync<BigWorldPanelNew, Game.Challenge.BigWorldPanelViewModel>(vm);
        }
    }
}