using System;
using System.Collections.Generic;
using System.Linq;
using Config;
using DG.Tweening;
using Newtonsoft.Json;
using QFramework;
using Spine.Unity;
using UnityEngine;

namespace FusionContinent.Model
{
    public class PlayerExchangeGift
    {
        public int Id { get; set; } // 用户ID
        public string CodeExchanges { get; set; }
        public Dictionary<string, int> CodeExchangesMap = new Dictionary<string, int>();

        public PlayerExchangeGift()
        {
        }

        
        public PlayerExchangeGift(PlayerExchangeGift v)
        {
            Id = v.Id;
            CodeExchanges = v.CodeExchanges ?? "{}";
            CodeExchangesMap = JsonConvert.DeserializeObject<Dictionary<string, int>>(CodeExchanges);
        }
    }
    
    
    // 返回数据
    public class PlayerExchangeGiftResult
    {
        public ItemGroup Items { get; set; } // 同步道具
        public Dictionary<string, int> Rewards { get; set; } // 奖励
        public PlayerExchangeGift ExchangeGift { get; set; } 
    }
    
}