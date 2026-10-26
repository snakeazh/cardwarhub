using System.Collections.Generic;
using System.Threading.Tasks;
using Config;
using QFramework;
using UnityEngine;

namespace FusionContinent
{
    public class RewardPopManager
    {
        public static RewardPopManager Instance = new RewardPopManager();
        public void ShowSoldierPanel(int itemId)
        {
            var panel = UIKit.OpenPanel<SoldierGetPanel>(UILevel.PopUI, new SoldierGetPanelData()
            {
                itemId =  itemId,
            });
            
            // await tas
        }
        Queue<int> cardDisplayQueue = new Queue<int>();
        
        public  void PushCardShow(int itemId)
        {
            cardDisplayQueue.Enqueue(itemId);
        }

        public void SetRewardShowValue(Dictionary<string, int> target)
        {
            Rewards = target;
        }

        private Dictionary<string, int> Rewards;
        public void Pop()
        {
            if (cardDisplayQueue.Count > 0)
            {
                var itemId = cardDisplayQueue.Dequeue();
                ShowSoldierPanel(itemId);
            }
            else
            {
                if (Rewards != null && Rewards.Count > 0 )
                {
                    GlobalFunc.ShowRewardTips(Rewards);
                }
                Rewards = null;
                
            }
            
        }

        public static Dictionary<string, int> FilterReward(Dictionary<string, int> Rewards)
        {
            Dictionary<string, int> result = new Dictionary<string, int>();
            // var rewards = new List<RewardItem>();
            foreach (var v in Rewards)
            {
                var itemId = int.Parse(v.Key);
                var itemCfg = DataConfigManager.Tables.TbItemConfig.GetOrDefault(itemId);
                if (itemCfg.Type != ItemType.HeroCard)
                {
                    result.Add(v.Key, v.Value);
                }
            }

            return result;
        }
        
        public static Dictionary<string, int> FilterCard(Dictionary<string, int> Rewards)
        {
            Dictionary<string, int> result = new Dictionary<string, int>();
            // var rewards = new List<RewardItem>();
            foreach (var v in Rewards)
            {
                var itemId = int.Parse(v.Key);
                var itemCfg = DataConfigManager.Tables.TbItemConfig.GetOrDefault(itemId);
                if (itemCfg.Type == ItemType.HeroCard)
                {
                    result.Add(v.Key, v.Value);
                }
            }

            return result;
        }
    }
}