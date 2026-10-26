using System.Collections.Generic;
using System.Linq;
using Config;
using UnityEngine;
using Random = System.Random;

namespace FusionContinent.Utils
{
    public static class RandomTool
    {
        // 单例 Random 实例
        private static readonly Random random = new Random();
        
        /**
         * 检查随机目标是否满足（1-100以内）
         */
        public static bool CheckRandomByTarget(int target)
        {
            
            if (target <= 0) return false;
            
            // 使用 Guid 的哈希码作为种子
            int randomNumber = random.Next(1, 101);// 0 ~ 99
            Debug.Log("当前随机数种子为  "+randomNumber);
            return target >= randomNumber;
        }
        
        /**
        * 检查随机目标是否满足（1-100以内）
        */
        public static int RandomRangeTarget(int min, int max)
        {
            if (min >= max) return min;

            // 使用 Guid 的哈希码作为种子
            return random.Next(min, max + 1);// 0 ~ 99
        }

        public static int RandomNumRange(List<int> numRange)
        {
            if (numRange.Count == 0) return 0;
            if (numRange.Count < 2) return numRange.First();
            return RandomRangeTarget(numRange[0], numRange[1]);
        }

        /**
         * 随机用户名称
         */
        public static string RandomUserName(int sex)
        {
            var cfgs = DataConfigManager.Tables.TbRandomUserNameConfig.DataList;
            var userName = cfgs[random.Next(0, cfgs.Count - 1)].Surname;
            // if (sex == SexType.Man)
            // {
            //     userName += cfgs[random.Next(0, cfgs.Count - 1)].ManName;
            // }
            // else
            {
                userName += cfgs[random.Next(0, cfgs.Count - 1)].GirlName;
            }

            return userName;
        }
    }
    
}