using System;
using System.Collections.Generic;
using Config;
using Newtonsoft.Json;
using QFramework;
using UnityEngine;

namespace FusionContinent.Model
{
    public class PlayerActivityModel
    {
        public int Id { get; set; } // 用户ID
        public int DailyCharge { get; set; } // 每日充值
        public int PassCheckNum { get; set; } // 通行证数量
        public DateTime CreateSinginDate { get; set; } // 创角签到日期
        
        public int AccumulateCharge { get; set; }
    }
}