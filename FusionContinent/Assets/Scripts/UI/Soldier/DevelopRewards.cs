using System.Collections.Generic;
using System.Linq;
using Config;
using FusionContinent.Animation;
using FusionContinent.Model;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using ScrollView;
using TMPro;
using Game.RedDot;

namespace FusionContinent
{
    public class DevelopRewardsData : UIPanelData
    {
    }
    public partial class DevelopRewards : UIPanel
    {
        private List<DevelopLevelPropertyList> developRewCfg;
        private PrioritySorter<DevelopLevelPropertyList> _prioritySorter =  new PrioritySorter<DevelopLevelPropertyList>();
        
        protected override void OnInit(IUIData uiData = null)
        {
            mData = uiData as DevelopRewardsData ?? new DevelopRewardsData();
            // please add init code here
            

        }
		
        private int _cardId;
        
        public void SetData(int cardId)
        {
            _cardId = cardId;
            var cardInfo = Global.GetCardById(_cardId);
            if (cardInfo == null) return;
            
            var quitBtn = this.Transform.Find("Quit").GetComponent<Button>();
            quitBtn.onClick.RemoveAllListeners();
            quitBtn.onClick.AddListener(() =>
            {
                UIAnimationUtils.PlayAnimations(transform.Find("CloseAnimation"), () =>
                {
                    this.RewardScroll.ShowList(0);
                    UIKit.HidePanel<DevelopRewards>();
                });
          
            });

            var developCfg = DataConfigManager.Tables.TbCardDevelopConfig.GetOrDefault(_cardId);
            developRewCfg = developCfg.LevelPropertyList.Where(t => t.TakeRewards.Count > 0).ToList();
            if (developRewCfg.Count == 0) return;

            gameObject.SetActive(true);
            
            this.RewardScroll.SetUpdateFunc(OnUpdateRewardItem);
            
            UIAnimationUtils.PlayAnimations(transform.Find("OpenAnimation"),SetDevelopReward);
        }

        private void OnUpdateRewardItem(int dataIndex, ScrollItem scrollItem)
        {
            var cardInfo = Global.GetCardById(_cardId);
            var rewObj = scrollItem.TweenTarget.gameObject;

            rewObj.SetActive(true);
            rewObj.transform.localScale = Vector3.one;
            var devCfg = developRewCfg[dataIndex];
            var rewardItems = rewObj.transform.Find("RewardItems");
            for (int i = 0; i < rewardItems.childCount; i++)
            {
                var itemUnit = rewardItems.GetChild(i);
                itemUnit.gameObject.SetActive(devCfg.TakeRewards.Count > i);
                if (!itemUnit.gameObject.activeSelf) break;

                var itemAndNum = devCfg.TakeRewards[i];
                itemUnit.GetComponent<RewardEntity>().SetData(itemAndNum.Id, itemAndNum.Num, default, false);
            }
                
            rewObj.transform.Find("TitleText").GetComponent<TMP_Text>().text = devCfg.UpGradeRewardDes.Count > 0 ? devCfg.UpGradeRewardDes.First() : "";
            rewObj.transform.Find("DescText").GetComponent<TMP_Text>().text = $"Lv {devCfg.Level}";
                
            // 点击事件
            var receiveBtn = rewObj.transform.Find("ReceiveBtn").GetComponent<Button>();
            receiveBtn.interactable = true;
            receiveBtn.onClick.RemoveAllListeners();
            receiveBtn.onClick.AddListener(() =>
            {
                ReceiveReward(receiveBtn, devCfg.Level, devCfg.TakeRewards);
            });
                
            receiveBtn.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = "领取";
            
            // 判断是否已领取
            receiveBtn.gameObject.SetActive(true);
            rewObj.transform.Find("ReceivedText").gameObject.SetActive(false);
            if (cardInfo.ObjData.ReceiveDevelops != null && cardInfo.ObjData.ReceiveDevelops.Contains(devCfg.Level))
            {
                rewObj.transform.Find("ReceivedText").gameObject.SetActive(true);
                receiveBtn.gameObject.SetActive(false);
                receiveBtn.interactable = false;
            }
            
            if (cardInfo.Lv < devCfg.Level)
            {
                receiveBtn.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = "待升级";
                receiveBtn.interactable = false;
            }
            
        }
        
        private async void ReceiveReward(Button receiveBtn, int cardLv, List<RewardAndNum> rewardItems)
        {
            receiveBtn.interactable = false;
            try
            {
                // var formData = new Dictionary<string, string>
                // {
                //     { "CardId", _cardId.ToString() },
                //     { "CardLv",  cardLv.ToString() },
                // };
                // var response = await WebDataFun.SendPostRequest(ApiKeys.ApiCardReceiveDevelopReward, formData);
                // if (!string.IsNullOrEmpty(response))
                // {
                //     var result = JsonConvert.DeserializeObject<CardResult>(response);
                //     Global.PlayerCards[result.Card.CardId] = new PlayerCard(result.Card);
                //
                //     // 同步奖励
                //     Items.Update(result.Items);
                //
                //     // 奖励
                //     RewardTips rewardTips = UIKit.OpenPanel<RewardTips>(UILevel.PopUI);
                //     rewardTips.SetData(rewardItems);
                //     
                //     // 更新按钮状态
                //     receiveBtn.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = "已领取";
                //     receiveBtn.interactable = false;
                //     
                //     Global.Publisher.Publish(MessagePublisher.CHANGE_RED_TYPE+RedNodeType.SoliderDevelopCard,null);
                //     
                //     // 刷新父级红点
                //     // SoldierPanel.Default.RefreshDevelopRedPoint();
                //     RefreshDevelopReward();
                // }
            }
            catch (System.Exception ex)
            {
                receiveBtn.interactable = true;
                LogTool.LogError($"领取奖励失败：{ex.Message}");
                FusionContinent.Toast.Show(ex.Message);
            }
        }

        void RefreshDevelopReward()
        {
            _prioritySorter.SetData(developRewCfg);
            _prioritySorter.SetScoreCalculator(Calculator);
            developRewCfg = _prioritySorter.GetSortedList();
            this.RewardScroll.ShowList(this.developRewCfg.Count);
        }
		
        void SetDevelopReward()
        {
            _prioritySorter.SetData(developRewCfg);
            _prioritySorter.SetScoreCalculator(Calculator);
            developRewCfg = _prioritySorter.GetSortedList();
            this.RewardScroll.ShowList(this.developRewCfg.Count);
            this.RewardScroll.ScrollToIndex(0,ScrollAlignment.Start,false);
            this.RewardScroll.PlayScaleAnimationDOTween();
        }

        private int Calculator(DevelopLevelPropertyList arg)
        {
            var cardInfo = Global.GetCardById(_cardId);
            var stateScore = -arg.Level;
            if (cardInfo.ObjData.ReceiveDevelops != null && cardInfo.ObjData.ReceiveDevelops.Contains(arg.Level))
            {
                //已领取
                stateScore += 100;
            }else if (cardInfo.Lv < arg.Level)
            {
                //待升级
                stateScore += 1000;
            }
            else
            {
                //未领取
                stateScore += 10000;
            }
            
            return stateScore;
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