using Config;
using FusionContinent.Utils;
using UnityEngine;

namespace FusionContinent.Model
{
    
    /**
     * 条件进度类型
     */
    public class ConditionProgress
    {
        public int Id;
        public ConditionType ConditionType;
        public bool IsNotAchievedCall;// 是否未达成回调
        
        private int _progress;// 条件进度
        public readonly ConditionConfig ConditionConfig;

        public ConditionProgress(int id, ConditionConfig cfg)
        {
            Id = id;
            _progress = 0;

            ConditionConfig = cfg;
            ConditionType = ConditionConfig.ConditionType;
        }

        /**
         * 添加进度并返回是否完成
         */
        public int AddProgress(int num)
        {
            // 不记录进度，直接判断当前参数是否达成条件
            if (ConditionConfig.IsRecord == 0)
            {
                _progress = num;
            }
            else
            {
                _progress += num;
            }

            return _progress;
        }
        
        /**
         * 完成一轮条件判断
         **/
        public int CompletedRound()
        {
            // 计算完成轮数
            var roundNum = Mathf.FloorToInt(_progress / ConditionConfig.ConditionValue);
            
            // 未完成或未触发概率条件
            if (roundNum <= 0 || !ProbabilityTrigger()) return 0;
            
            _progress = 0;// 完成重置进度
            return roundNum;
        }
        
        /**
         * 概率触发条件
         */
        private bool ProbabilityTrigger()
        {
            return ConditionConfig.ProbabilityTrigger == 0 || 
                RandomTool.CheckRandomByTarget(Mathf.CeilToInt(ConditionConfig.ProbabilityTrigger * 100));
        }

    }
}