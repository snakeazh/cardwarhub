using UnityEngine;
using UnityEngine.UI;
using QFramework;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using Config;
using FusionContinent.Model;
using Manager;

namespace FusionContinent
{
    public class GameSuccessPanelData : UIPanelData
    {
    }

    public partial class GameSuccessPanel : UIPanel
    {
        //public LevelTable levelTable;
        //public LevelScoreSituationTable levelScoreSituationTable;
        public Text scoretxt;

        private void OnEnable()
        {
            var score = ScoreController.Instance.GetScore();
            var scoreTime = ScoreController.Instance.GetScoreTime();
            scoretxt.text = $"本局获得的分数：{score} <color=#FFF04E>({scoreTime})</color>";
            if (DataConfigManager.Tables.TbChapterStageConfig.GetOrDefault(DataConfigManager.Tables
                    .TbStageBattleConfig.GetOrDefault(Global.StageId.Value).ChapterId).DisplayStatus !=
                DisplayStatustype.Maininterface)
            {
                return;
            }

            // 判断下一关
            var stageCfg = DataConfigManager.Tables.TbStageBattleConfig.GetOrDefault(Global.StageId.Value + 1);
            if (stageCfg != null)
            {
                // 关卡id自增
                Global.StageId.Value += 1;
                NextBtn.transform.localScale = Vector3.one;
                NextBtn.transform.Find("Text (Legacy)").GetComponent<Text>().text = $"下一关：{stageCfg.StageName}";
                Global.LevelStage[Global.StageId.Value] = true;
            }
            else
            {
                var chapter = DataConfigManager.Tables.TbChapterStageConfig.GetOrDefault(DataConfigManager.Tables
                    .TbStageBattleConfig.GetOrDefault(Global.StageId.Value).ChapterId+1);
                if (chapter != null)
                {
                    Global.StageId.Value = chapter.InitUnlockStageId;
                    NextBtn.transform.localScale = Vector3.one;
                    NextBtn.transform.Find("Text (Legacy)").GetComponent<Text>().text =
                        $"下一关：{DataConfigManager.Tables.TbStageBattleConfig.GetOrDefault(Global.StageId.Value).StageName}";
                    Global.LevelStage[Global.StageId.Value] = true;
                }
                else
                {
                    NextBtn.transform.localScale = Vector3.zero;
                }
            }

            if (stageCfg != null && (Global.StageId.Value - 1 == DataConfigManager.Tables.TbChapterStageConfig
                    .GetOrDefault(DataConfigManager.Tables
                        .TbStageBattleConfig.GetOrDefault(Global.StageId.Value).ChapterId).InitUnlockStageId))
            {
                var nextChapterStageId = DataConfigManager.Tables.TbChapterStageConfig.GetOrDefault(DataConfigManager
                    .Tables
                    .TbStageBattleConfig.GetOrDefault(Global.StageId.Value).ChapterId + 1).InitUnlockStageId;
                if (Global.LevelStage.ContainsKey(nextChapterStageId))
                {
                    Global.LevelStage[nextChapterStageId] = true;
                }
                else
                {
                }

            }
        }

        protected override void OnInit(IUIData uiData = null)
        {
            mData = uiData as GameSuccessPanelData ?? new GameSuccessPanelData();
            BackBtn.onClick.AddListener(() =>
            {
                GamePlayManager.Default.BackHome();
                UIKit.ClosePanel<GameSuccessPanel>();
            });
            NextBtn.onClick.AddListener(() =>
            {
                NextBtn.interactable = false; // 禁用按钮
                UIRoot.Instance.transform.Find("Audio Source").GetComponent<AudioSource>().Play();
                GamePlayManager.Default.RestartGame(() =>
                {
                    UIKit.ClosePanel<GameSuccessPanel>();
                    SceneLoader.Load("GamePlay");
                }, () =>
                {
                    if (NextBtn != null) NextBtn.interactable = true; // 恢复按钮
                });

                // Global.GameGold.Value = 0;
                // CoinManager.Instance.ResetCoin();
                // GamePlayManager.Default.IsOpenChooseCard = false;
                // Global.ResetProperties();
                // Global.Difficulty.Value = 1;
            });
        }

        public void SetData(Dictionary<string, int> rewards)
        {
            // 清空 Content 的子对象
            foreach (Transform child in Content)
            {
                Destroy(child.gameObject);
            }

            foreach (var v in rewards)
            {
                var rewardObj = GamePlayManager.Default._resLoader.Get<GameObject>("RewardEntity").Instantiate();
                rewardObj.transform.SetParent(Content);
                var rewardEntity = rewardObj.GetComponent<RewardEntity>();
                var rewItem = new RewardItem(int.Parse(v.Key), v.Value, 0);
                rewardEntity.SetData(rewItem);
            }
        }

        protected override void OnOpen(IUIData uiData = null)
        {
        }

        protected override void OnShow()
        {
        }

        protected override void OnHide()
        {
        }

        protected override void OnClose()
        {
        }
    }
}