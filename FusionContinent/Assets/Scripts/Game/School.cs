using System.Collections.Generic;
using System.Linq;
using Config;
using FusionContinent.Attribute;
using FusionContinent.Model;
using Manager;
using UnityEngine;
using QFramework;

namespace FusionContinent
{
    /**
     * 流派卡类
     */
    public class School
    {
        public int SchoolId;
        public SchoolType SchoolType;

        public SchoolConfig _schoolCfg;
        public int HaveCardCount;
        private int currentCardCount;
        public SchoolCardCountListStruct CurrentCardCountCfg;
        
        public School(int schoolId, SchoolType schoolType)
        {
            SchoolId = schoolId;
            SchoolType = schoolType;
            currentCardCount = 0;
            HaveCardCount = 0;

            _schoolCfg = DataConfigManager.Tables.TbSchoolConfig.GetOrDefault(schoolId);
            CurrentCardCountCfg = _schoolCfg.CardCountList.First();
        }

        public void AddCardCount(int addCount = 1)
        {
            // 增加当前拥有的卡数量
            if (HaveCardCount >= _schoolCfg.CardCountList.Last().CardCount) return;
            HaveCardCount += addCount;
            LogTool.Log(">>>>>> 添加流派数量 - AddCardCount("+SchoolId+").HaveCardCount: " + HaveCardCount);

            var isFirstUnlock = false;
            // 遍历卡数量属性列表
            foreach (var cfg in _schoolCfg.CardCountList)
            {
                if (cfg.CardCount <= currentCardCount || HaveCardCount < cfg.CardCount)
                {
                    continue;
                }

                if (!isFirstUnlock && currentCardCount == 0)
                {
                    isFirstUnlock = true;
                }

                UnlockSchool(cfg.CardCount);
                CurrentCardCountCfg = cfg;
                
                // 属性生效
                var attrModifier = new AttributeModifier
                {
                    SourceKey = $"{_schoolCfg.Id}:{cfg.CardCount}",
                    SourceType = AttributeSourceType.SchoolCard,
                };
                AttributeManager.SetAttributesLogic(attrModifier, cfg.Attributes);
                // SchoolManager.Instance.SetAttributeLogic($"{_schoolCfg.Id}:{cfg.CardCount}", cfg.Attributes, true);
                
                // BUFF生效
                SchoolManager.Instance.SetBuffLogic(cfg.Buffs, false);
                LogTool.Log("卡数量属性列表 - cfg.CardCount: "+cfg.CardCount);

                // 通知订阅流派星级修改
                Global.Publisher.Publish(MessagePublisher.CHANGE_SCHOOL_STAR, new ChangeSchoolStar(_schoolCfg.Id, cfg.CardCount));
            }

            if (isFirstUnlock && _schoolCfg.SchoolType != SchoolType.Skill)
            {
                // 监听条件 - 解锁流派数量
                GamePlayManager.Default.AddListenerCondition(ConditionType.UnlockSchoolNum, 1);
                
                // 解锁流派英灵加成
                if (GamePlayManager.Default?.ModuleAttributes.GetFinal(AttributeType.UnlockSchoolSoldierAddition) > 0f)
                {
                    // 属性生效
                    var attrModifier = new AttributeModifier
                    {
                        SourceKey = "UnlockSchoolSoldierAddition",
                        SourceType = AttributeSourceType.SurpriseCard,
                    };
                    var attrCfg =
                        DataConfigManager.Tables.TbAttributeConfig.Get(AttributeType.UnlockSchoolSoldierAddition);
                    AttributeManager.SetAttributesLogic(attrModifier, attrCfg.AdditionAttributes, true);
                }
            }

            // 添加解锁流派显示刷新
            if (_schoolCfg.SchoolType != SchoolType.Skill) 
                Global.Publisher.Publish(MessagePublisher.UNLOCK_SCHOOL, _schoolCfg.Id);
        }

        /**
         * 检查是否解锁
         */
        public bool CheckUnlock()
        {
            return currentCardCount > 0;
        }

        /**
         * 解锁流派
         */
        private void UnlockSchool(int cardCount)
        {
            currentCardCount = cardCount;
        }
        
        public int GetCurrentCardCount()
        {
            return currentCardCount;
        }
    }
    
    /**
     * 流派范围攻击
     */
    public class SchoolCardRangedAttack
    {
        public int Param1;
        public float PropertyVal;

        public SchoolCardRangedAttack(int param1)
        {
            Param1 = param1;
        }
    }
    
    /**
     * 流派概率累加
     */
    public class SchoolCardWeightAccrual
    {
        public int Weight;
        public int Num;

        public SchoolCardWeightAccrual(int weight, int num)
        {
            Num = num;
            Weight = weight;
        }
    }

    public class ChangeSchoolStar
    {
        public int SchoolId;
        public int StarLv;

        public ChangeSchoolStar(int id, int starLv)
        {
            SchoolId = id;
            StarLv = starLv;
        }
    }

}