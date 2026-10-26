using System;
using System.Collections.Generic;
using System.Linq;
using Config;
using FusionContinent.Utils;
using UnityEngine;

namespace FusionContinent.Model
{
    public class PlayerTalent
    {
        public int Id { get; set; } // 主键
        public int UserId { get; set; } // 用户ID
        public int TalentId { get; set; } // 天赋ID
        public int Lv { get; set; } // 天赋等级
        public int ExtraLv { get; set; } // 额外天赋等级
        public int Status { get; set; } // 状态: 0.否，1使用

        // 状态
        public const int STATUS_NOT_USE = 0; // 未使用
        public const int STATUS_IN_USE  = 1; // 已使用
        
        
        // 使用天赋（server）
        public async void UseTalent(int status, Action successCallback = null, Action<string> errorCallback = null, Action finallyCallback = null)
        {
            try
            {
                LogTool.Log("正在请求服务器战斗开始，请稍候...");
                var request = new Cardwar.UseTalentRequest { Id = TalentId, Status = status };
                var proto = await WebDataFun.SendProtoPostRequest<Cardwar.UseTalentResponse>(ApiKeys.ApiTalentUse, request);
                if (proto != null)
                {
                    foreach (var talent in proto.Talents)
                    {
                        Global.PlayerTalents[talent.TalentId] = ProtoMappers.MapPlayerTalent(talent);
                    }

                    if (successCallback != null) successCallback();
                }
            }
            catch (System.Exception ex)
            {
                if (errorCallback != null) errorCallback(ex.Message);
                LogTool.LogError($"操作失败：{ex.Message}");
            }
            finally
            {
                if (finallyCallback != null) finallyCallback();
            }
        }
        
    }

    public static class Talent
    {
        public static PlayerTalent GetUseTalent()
        {
            return Global.PlayerTalents.Values.FirstOrDefault(v => v.Status == PlayerTalent.STATUS_IN_USE);
        }
        
        public static PlayerTalent GetTalentById(int talentId = 0)
        {
            return Global.PlayerTalents.GetValueOrDefault(talentId, GetUseTalent());
        }

        // 获取人口
        public static int GetPopulation(int talentId = 0)
        {
            GetAttributes(talentId);
            
            // 基础值
            var population = DataConfigManager.Tables.TbPlayerConst.DefaultPopulation;
            population += (int)_attributeAggregation.Attributes.GetFinal(AttributeType.NumberOfHeroCardSlots);
            
            return population;
        }


        public static int GetSlotUnlockLv(int talentId = 101,int slot = 0)
        {
            List<TalentListStruct> talentConfigs = new List<TalentListStruct>();
            foreach (var config in DataConfigManager.Tables.TbTalentConfig.DataList)
            {
                if (config.Id == talentId)
                {
                    foreach (var itemTalent in config.TalentList)
                    {
                        if (itemTalent.ExtraAttributes.Count>0)
                        {
                            foreach (var attributeStruct in itemTalent.ExtraAttributes)
                            {
                                if (attributeStruct.Type == AttributeType.NumberOfHeroCardSlots)
                                {
                                    talentConfigs.Add(itemTalent);
                                    break;
                                }
                            }
                        }
                    }
                }
                
                // DataConfigManager.Tables.TbTalentConfig.DataList
            }
            var population = DataConfigManager.Tables.TbPlayerConst.DefaultPopulation;
            if (slot < population)
            {
                return 0;
            }
            else
            {
                return talentConfigs[slot - population].Level;
            }
        }
        
        // 获取技能卡槽数量
        public static int GetSkillSlotNum(int talentId = 0)
        {
            GetAttributes(talentId);
            
            // 基础值
            var slotNum = DataConfigManager.Tables.TbPlayerConst.DefaultSkillCount;
            slotNum += (int)_attributeAggregation.Attributes.GetFinal(AttributeType.NumberOfSkillCardSlots);
            
            return slotNum;
        }
        
        // 获取技能被动卡槽数量
        public static int GetSkillPassiveSlotNum(int talentId = 0)
        {
            GetAttributes(talentId);
            
            // 基础值
            var slotNum = DataConfigManager.Tables.TbPlayerConst.DefaultArcaneCount;
            slotNum += (int)_attributeAggregation.Attributes.GetFinal(AttributeType.NumberOfPassiveCardSlots);
            return slotNum;
        }

        private static PlayerTalent _lastTalent;
        private static AttributeAggregation _attributeAggregation = new AttributeAggregation();
        
        // 获取当前天赋属性
        public static AttributeAggregation GetAttributes(int talentId = 0)
        {
            var talent = GetTalentById(talentId);
            if (talent == null || talent == _lastTalent) return _attributeAggregation;

            _attributeAggregation = new AttributeAggregation();
            var talentCfg = DataConfigManager.Tables.TbTalentConfig.GetOrDefault(talent.TalentId);
            foreach (var cfg in talentCfg.TalentList)
            {
                if (talent.Lv >= cfg.Level)
                {
                    _attributeAggregation.Attributes.AddRangeBaseValue(cfg.Attributes);
                    _attributeAggregation.SubAttributes.AddRange(cfg.Attributes);
                    _attributeAggregation.MainAttributes.AddRange(cfg.MainAttributes);
                }

                // 特殊天赋
                if (talent.ExtraLv >= cfg.Level && cfg.ExtraAttributes.Count > 0)
                {
                    _attributeAggregation.Attributes.AddRangeBaseValue(cfg.ExtraAttributes);
                    _attributeAggregation.MainAttributes.AddRange(cfg.ExtraMainAttributes);
                    _attributeAggregation.SubAttributes.AddRange(cfg.ExtraAttributes);
                }
            }

            _lastTalent = talent; 
            return _attributeAggregation;
        }
        
        // 获取战力
        public static int GetFightingNumById(int talentId)
        {
            var talent = Talent.GetTalentById(talentId);
            var talentCfg = DataConfigManager.Tables.TbTalentConfig.GetOrDefault(talent.TalentId);
            if (talentCfg == null) return 0;
            
            var fightNum = 0;
            foreach (var lvCfg in talentCfg.TalentList)
            {
                if (lvCfg.Level > talent.Lv) break;
                fightNum += lvCfg.FightNum;
                    
                // 特殊天赋
                if (talent.ExtraLv >= lvCfg.Level)
                {
                    fightNum += lvCfg.ExtraFightNum;
                }
            }

            return fightNum;
        }
        
    }
}