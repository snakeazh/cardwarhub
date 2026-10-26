using System;
using System.Collections.Generic;
using System.Linq;
using Config;
using DG.Tweening;
using Newtonsoft.Json;
using QFramework;
using Spine.Unity;
using UnityEngine;
using Cardwar;
using FusionContinent.Utils;

namespace FusionContinent.Model
{
    public class UserPlayer
    {
        public int Id { get; set; } // 用户Id
        public string Figure { get; set; } // 形象
        public List<int> FigureData { get; set; } // 形象数据
        public int ModuleUnlockLv { get; set; }// 模块解锁顽疾等级
        public string GuideFlows { get; set; }
        public Dictionary<int, GuideFlowStatus> GuideFlowMap { get; set; }

        public UserPlayer(UserPlayer v = null)
        {
            if (v == null) return;
            
            Id = v.Id;
            Figure = v.Figure;
            ModuleUnlockLv = v.ModuleUnlockLv;
            FigureData = JsonConvert.DeserializeObject<List<int>>(v.Figure);
            GuideFlows = v.GuideFlows;
            GuideFlowMap = JsonConvert.DeserializeObject<Dictionary<int, GuideFlowStatus>>(v.GuideFlows);
        }

        public bool CheckFigureById(int figureId)
        {
            return FigureData != null && FigureData.Contains(figureId);
        }

        public GuideFlowStatus GetGuideStatusById(int guideId)
        {
            return GuideFlowMap.GetValueOrDefault(guideId, GuideFlowStatus.GuideFlowReady);
        }

        // 检查引导是否完成
        public bool CheckGuideStatus(int guideId, GuideFlowStatus status = GuideFlowStatus.GuideFlowReceive)
        {
            return GetGuideStatusById(guideId) >= status;
        }
        public bool CheckGuideStatus(GuideType type, GuideFlowStatus status = GuideFlowStatus.GuideFlowReceive)
        {
            var cfg = DataConfigManager.Tables.TbGuideFlowConfig.DataList.FirstOrDefault(v => v.Type == type);
            if (cfg == null) return false;
            
            return GetGuideStatusById(cfg.Id) >= status;
        }

        public async void SetGuideFlowsStatus(GuideType type, Action callback = null, GuideFlowStatus status = GuideFlowStatus.GuideFlowFinished)
        {
            var cfg = DataConfigManager.Tables.TbGuideFlowConfig.DataList.FirstOrDefault(v => v.Type == type);
            if (cfg == null) return;
            
            SetGuideFlowsStatus(cfg.Id, callback, status);
        }

        // 设置引导状态（服务器）
        public async void SetGuideFlowsStatus(int guideId, Action callback = null, GuideFlowStatus status = GuideFlowStatus.GuideFlowFinished)
        {
            if (CheckGuideStatus(guideId, status)) return;
            
            try
            {
                LogTool.Log("正在请求服务器添加引导状态，请稍候...");
                var req = new SaveGuidesRequest { Id = guideId };
                var proto = await WebDataFun.SendProtoPostRequest<SaveGuidesResponse>(ApiKeys.ApiPlayerSaveGuides, req);

                // 更新本地数据
                if (proto != null)
                {
                    if (proto.Player != null)
                    {
                        var mapped = ProtoMappers.MapUserPlayer(proto.Player);
                        if (mapped != null)
                        {
                            Global.Player = mapped;
                        }
                    }

                    // 同步奖励
                    if (proto.Items != null)
                    {
                        Items.Update(proto.Items);
                    }

                    // 奖励
                    if (proto.Rewards != null && proto.Rewards.Count > 0)
                    {
                        // RewardTips rewardTips = UIKit.OpenPanel<RewardTips>(UILevel.PopUI);
                        // rewardTips.SetData(result.Rewards);
                        // GlobalFunc.ShowRewardTips(result.Rewards);
                    }
                }

                if (status == GuideFlowStatus.GuideFlowReceive)
                {
                    // DOVirtual.DelayedCall(0.2f, () => GuideFlow.Instance.RefreshData());
                }

                if (callback != null) callback();
            }
            catch (System.Exception ex)
            {
                var errorMsg = $"设置引导状态失败：{ex.Message}";
                if (GameMainPanel.Default != null) FusionContinent.Toast.Show(errorMsg);
                LogTool.LogError(errorMsg);
            }
        }
        
        public async void SetModuleUnlockLv(int moduleUnlockLv, Action callback = null)
        {
            SetModuleUnlockLvAsync(moduleUnlockLv,callback);
        }
        
        // 设置引导状态（服务器）
        private async void SetModuleUnlockLvAsync(int moduleUnlockLv, Action callback = null)
        {
            try
            {
                LogTool.Log("正在请求服务器设置模块解锁状态，请稍候...");
                var req = new SaveModuleUnlockLvRequest { ModuleUnlockLv = moduleUnlockLv };
                var proto = await WebDataFun.SendProtoPostRequest<SaveModuleUnlockLvResponse>(ApiKeys.ApiPlayerSaveModuleLv, req);

                // 更新本地数据
                if (proto != null && proto.Player != null)
                {
                    var mapped = ProtoMappers.MapUserPlayer(proto.Player);
                    if (mapped != null)
                    {
                        Global.Player = mapped;
                    }
                }
                
                // // 同步奖励
                // if (result.Items != null) Items.Update(result.Items);
                //
                // // 奖励
                // if (result.Rewards != null)
                // {
                //     RewardTips rewardTips = UIKit.OpenPanel<RewardTips>(UILevel.PopUI);
                //     rewardTips.SetData(result.Rewards);
                // }
                

                if (callback != null) callback();
            }
            catch (System.Exception ex)
            {
                var errorMsg = $"设置引导状态失败：{ex.Message}";
                if (GameMainPanel.Default != null) FusionContinent.Toast.Show(errorMsg);
                LogTool.LogError(errorMsg);
            }
        }
    }
    
    public class GuideFlowController
    {
        private static GuideFlowController _instance;
        public static GuideFlowController Instance()
        {
            return _instance ??= new GuideFlowController();
        }

        private int _guideId;

        public int GetCurrentGuideId()
        {
            return _guideId;
        }

        public string GetCurrentGuideName()
        {
            return "";
        }
    }

    public class SavePlayerResult
    {
        public UserData User; 
        public UserPlayer Player; 
    }
    
    // 返回数据
    public class SetGuideFlowsResult
    {
        public UserPlayer Player { get; set; } 
        public ItemGroup Items { get; set; } // 同步道具
        public List<RewardItem> Rewards { get; set; } // 奖励
    }
    
    /**
     * 引导状态
     */
    public enum GuideFlowStatus
    {
        GuideFlowReady    = 0, // 准备中
        GuideFlowReceive  = 1, // 可领取
        GuideFlowFinished = 2, // 已完成
    }
    
}