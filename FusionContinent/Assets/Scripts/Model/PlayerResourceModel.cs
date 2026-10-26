using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace FusionContinent.Model
{
    public class PlayerResource
    {
        public int Id { get; set; } // 用户ID
        public int Stamina { get; set; } // 体力
        public DateTime StaminaTime { get; set; } // 体力刷新时间

        public int GetMaxStamina()
        {
            return DataConfigManager.Tables.TbPlayerConst.MaxStamina;
        }

        public bool CheckStamina(int staminaNum)
        {
            return Stamina >= staminaNum;
        }
    }

   
}