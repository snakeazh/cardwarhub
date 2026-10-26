using System.Collections.Generic;
using Cardwar;
using Cysharp.Threading.Tasks;
using FusionContinent;
using FusionContinent.Model;
using FusionContinent.Utils;
using UIFramework.Services;

namespace Game.Challenge
{
    /// <summary>
    /// 怪兽图鉴服务：领取怪兽图鉴奖励。
    /// 网络请求与数据同步集中在此，ViewModel 只负责调用并消费结果。
    /// </summary>
    public class AtlasService : GameServiceBase
    {
        public override async UniTask InitializeAsync()
        {
            await base.InitializeAsync();
        }

        /// <summary>
        /// 领取指定怪兽的图鉴奖励。
        /// 成功后更新 Global.PlayerAtlas 与 Items，并弹出奖励提示。
        /// </summary>
        public async UniTask<bool> ReceiveMonsterRewardAsync(int monsterId)
        {
            try
            {
                var request = new ReceiveMonsterRewardRequest { MonsterId = monsterId };
                var proto = await WebDataFun.SendProtoPostRequest<ReceiveMonsterRewardResponse>(ApiKeys.ApiAtlasReceiveMonsterReward, request);
                if (proto == null)
                {
                    Toast.Show("请求失败：空响应");
                    return false;
                }

                // 更新图鉴数据（整体替换，状态以服务端返回为准）
                Global.PlayerAtlas = ProtoMappers.MapPlayerAtlas(proto.PlayerAtlas);
                Game.Challenge.RedDot.MonsterAtlasRedDotHelper.MarkDirty();

                // 同步物品
                if (proto.Items != null)
                {
                    Items.Update(proto.Items);
                }

                // 弹出奖励提示
                var rewardsDict = new Dictionary<string, int>();
                if (proto.Rewards != null)
                {
                    foreach (var kv in proto.Rewards)
                    {
                        rewardsDict[kv.Key.ToString()] = (int)kv.Value;
                    }
                }

                if (rewardsDict.Count > 0)
                {
                    GlobalFunc.ShowRewardTips(rewardsDict).Forget();
                }

                return true;
            }
            catch (System.Exception ex)
            {
                LogTool.LogError("[AtlasService] ReceiveMonsterRewardAsync error: " + ex.Message);
                Toast.Show($"领取失败：{ex.Message}");
                return false;
            }
        }
    }
}
