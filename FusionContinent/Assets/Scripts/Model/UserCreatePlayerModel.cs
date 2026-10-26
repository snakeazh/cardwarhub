using System;
using System.Collections.Generic;
using Config;
using FusionContinent.Manager;
using Newtonsoft.Json;
using QFramework;
using UnityEngine;

namespace FusionContinent.Model
{
    public class UserCreatePlayerActivityModel
    {
        public int Id { get; set; } 
        public int UserId { get; set; } 
        public int ActivityId { get; set; } 
        public string PartReceives { get; set; }
        public Dictionary<int, int> PartReceiveMap = new Dictionary<int, int>(); 
        public CreatePlayerActivityStatus Status { get; set; } 
        public DateTime CreateTime { get; set; }

        public void SetData()
        {
            // 第一次反序列化成 string，去掉多余的转义
            string cleanJson = PartReceives;
            if (PartReceives != "{}" && PartReceives != "[]")
            {
                cleanJson = cleanJson.Trim('"').Replace("\\\"", "\"");
            }
            PartReceiveMap = JsonConvert.DeserializeObject<Dictionary<int, int>>(cleanJson);
        }

        public int GetOpenDayNum()
        {
            long ts = GetCreateTimeToTimestamp();
            if (ts <= 0) return 1;

            // 1 基天数（创建当天 = 第 1 天）；底层 GetDayInterval 是 0 基重置点间隔，时间回拨时至少返回 1
            int interval = ServerTimeManager.GetDayInterval(ts, ServerTimeManager.Instance.GetCurrentServerTimestamp());
            return Mathf.Max(1, interval + 1);
        }

        public long GetCreateTimeToTimestamp()
        {
            if (CreateTime == default) return 0;
            
            DateTime utcTime = CreateTime.ToUniversalTime();
            return new DateTimeOffset(utcTime).ToUnixTimeSeconds();
        }

        public int GetPartReceiveNum(int partId)
        {
            return PartReceiveMap.GetValueOrDefault(partId, 0);
        }
        
    }

    public enum  CreatePlayerActivityStatus
    {
        /// <summary>
        /// 未解锁
        /// </summary>
        NotUnlocked = 0,
        
        /// <summary>
        /// 已解锁
        /// </summary>
        Unlocked = 1,
        
        /// <summary>
        /// 结束
        /// </summary>
        Finish = 2,
    }

    public static class UserCreatePlayerActivity
    {
        private static int _monthCardActId = 0;
        
        public static UserCreatePlayerActivityModel GetDataByActId(int actId)
        {
            if (Global.CreatePlayerActivitys.TryGetValue(actId, out var data))
            {
                return data;
            }

            return new UserCreatePlayerActivityModel(){};
        }

        
        /**
         * 检查月卡是否过期
         */
        public static bool CheckMonthCardExpired()
        {
            if (_monthCardActId == 0)
            {
                var monthCardCfg = DataConfigManager.Tables.TbCreatePlayerActivityConfig.DataList.
                    Find(cfg => cfg.Type == ActivityType.MonthlySubscription);
                if (monthCardCfg != null)
                {
                    _monthCardActId = monthCardCfg.Id;
                }
            }
            
            // 判断是否解锁，或者超过30天
            var activity = GetDataByActId(_monthCardActId);
            return activity.Status != CreatePlayerActivityStatus.Unlocked || activity.GetOpenDayNum() > 30;
        }
    }
    
    public class CreatePlayerActivityResult
    {
        public UserCreatePlayerActivityModel Activity { get; set; }
        public Dictionary<string, int> Rewards { get; set; } // 奖励内容
        public ItemGroup Items { get; set; } // 同步道具
    }
}