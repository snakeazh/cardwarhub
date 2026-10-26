using System;
using System.Collections.Generic;
using Config;
using Newtonsoft.Json;
using QFramework;
using UnityEngine;
using Cardwar;
using FusionContinent.Utils;

namespace FusionContinent.Model
{
    public class PlayerShop
    {
        public int Id { get; set; } // 主键
        public int UserId { get; set; } // 用户ID
        public int ShopId { get; set; } // 商店ID
        public int RefreshNum { get; set; } // 刷新次数
        public DateTime RefreshTime { get; set; } // 刷新时间

        public int DiscountRate { get; set; } // 3折：30，4折：40
    }
    
    public class PlayerGoods
    {
        public int Id { get; set; } // 主键
        public int UserId { get; set; } // 用户ID
        public int ShopId { get; set; } // 商店ID
        public int GoodsId { get; set; } // 商品ID
        public int BuyNum { get; set; } // 购买次数
        public int LockStatus { get; set; } // 锁定状态

        /**
         * 购买商品
         */
        public async void BuyGoods(int buyCount = 1, Action callback = null)
        {
            try
            {
                LogTool.Log("正在请求服务器购买商品，请稍候...");
                var req = new BuyGoodsRequest { GoodsId = this.GoodsId, Count = buyCount };
                var proto = await WebDataFun.SendProtoPostRequest<BuyGoodsResponse>(ApiKeys.ApiBuyGoods, req);
                
                // 更新本地数据
                if (proto != null && proto.Goods != null)
                {
                    var localGoods = ProtoMappers.MapPlayerShopGoods(proto.Goods);
                    if (localGoods != null)
                    {
                        Global.PlayerGoods[localGoods.GoodsId] = localGoods;
                    }
                }
                // Id = result.Goods.Id;
                // UserId = result.Goods.UserId;
                // ShopId = result.Goods.ShopId;
                // BuyNum = result.Goods.BuyNum;
                // LockStatus = result.Goods.LockStatus;
                // CreateTime = result.Goods.CreateTime;
                
                // 活动信息
                if (proto != null && proto.Activity != null && proto.Activity.Id > 0)
                {
                    Global.PlayerActivity = ProtoMappers.MapPlayerActivity(proto.Activity);
                    Global.Publisher.Publish(MessagePublisher.CHANGE_ACCUMULATE_CHARGE_NUM, Global.PlayerActivity.AccumulateCharge);
                }

                // 同步奖励
                if (proto != null)
                {
                    Items.Update(proto.Items);
                }

                // 将 proto.Rewards 转为 UI 使用的字典
                var rewardsDict = new Dictionary<string, int>();
                if (proto != null && proto.Rewards != null)
                {
                    foreach (var kv in proto.Rewards)
                    {
                        rewardsDict[kv.Key.ToString()] = (int)kv.Value;
                    }
                }

                foreach (var item in RewardPopManager.FilterCard(rewardsDict))
                {
                    var itemId = int.Parse(item.Key);
                    RewardPopManager.Instance.PushCardShow(itemId);
                }
                
                // 奖励
                if (rewardsDict.Count > 0)
                {
                    RewardPopManager.Instance.SetRewardShowValue(RewardPopManager.FilterReward(rewardsDict));
                }
                RewardPopManager.Instance.Pop();
                if (callback != null) callback();
            }
            catch (System.Exception ex)
            {
                LogTool.LogError($"购买商品失败：{ex.Message}\n{ex.StackTrace}");
            }
        }
    }
    
    // 返回数据
    public class BuyGoodsResult
    {
        public PlayerGoods Goods { get; set; } 
        public ItemGroup Items { get; set; } // 同步道具
        public Dictionary<string, int> Rewards { get; set; } // 奖励
        public PlayerActivityModel Activity { get; set; } // 活动数据
        
    }


    public static class Shop
    {
    }

}