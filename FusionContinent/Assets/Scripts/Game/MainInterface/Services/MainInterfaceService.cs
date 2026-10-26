using UIFramework.Services;
using Cysharp.Threading.Tasks;
using UnityEngine;
using System.Collections.Generic;
using Config;
using FusionContinent;
using FusionContinent.Model;
using Newtonsoft.Json;

namespace Game.MainInterface
{
    /// <summary>
    /// 主界面服务 - 管理主界面数据和逻辑
    /// </summary>
    public class MainInterfaceService : GameServiceBase
    {
        #region 属性
        public bool IsInitialized { get; private set; }
        #endregion

        public override async UniTask InitializeAsync()
        {
            await base.InitializeAsync();
            
            IsInitialized = true;
            LogTool.Log("[MainInterfaceService] 初始化完成");
        }

        #region 挂机收益

        /// <summary>
        /// 领取挂机收益
        /// </summary>
        public async UniTask<AfkRewardResult> ClaimAfkRewardAsync()
        {
            try
            {
                // TODO: 实现领取挂机收益的网络请求
                // var response = await WebDataFun.SendPostRequest(ApiKeys.ApiClaimAfkReward, null);
                
                // 模拟成功
                await UniTask.Delay(500);
                
                return new AfkRewardResult
                {
                    Success = true,
                    Rewards = new List<RewardItem>()
                };
            }
            catch (System.Exception ex)
            {
                LogTool.LogError($"[MainInterfaceService] 领取挂机收益失败：{ex.Message}");
                return new AfkRewardResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        /// <summary>
        /// 获取挂机收益数量
        /// </summary>
        public long GetAfkRewardNum()
        {
            // TODO: 从服务器或本地获取挂机收益数量
            return 0;
        }

        #endregion
    }

    #region 数据类

    public class AfkRewardResult
    {
        public bool Success;
        public string ErrorMessage;
        public List<RewardItem> Rewards;
    }

    #endregion
}
