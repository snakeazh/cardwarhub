using Cardwar;
using Cysharp.Threading.Tasks;
using FusionContinent;
using FusionContinent.Model;
using Game.RedDot;
using UIFramework.Services;

namespace Game.Station
{
    /// <summary>
    /// 体力驿站服务：查询与领取驿站存放的体力。
    /// 网络请求与数据同步集中在此，ViewModel 只负责调用并消费结果。
    /// </summary>
    public class StationService : GameServiceBase
    {
        /// <summary>缓存的可领槽位数；红点 Evaluate 用同步值，避免网络请求。</summary>
        public int ClaimableSlotCount { get; private set; }

        public bool HasClaimableStamina => ClaimableSlotCount > 0;

        public override async UniTask InitializeAsync()
        {
            await base.InitializeAsync();
            // await RefreshClaimableCacheAsync();
        }

        /// <summary>
        /// 查询驿站当前存放的体力信息，失败返回 null。成功时同步红点缓存。
        /// </summary>
        public async UniTask<StationStamina> GetStaminaInfoAsync()
        {
            try
            {
                var proto = await WebDataFun.SendProtoPostRequest<GetStationStaminaInfoResponse>(ApiKeys.ApiStaminaInfo, null);
                StationStamina stamina = proto != null ? proto.Stamina : null;
                UpdateClaimableCache(stamina);
                return stamina;
            }
            catch (System.Exception ex)
            {
                LogTool.LogError("[StationService] GetStaminaInfoAsync error: " + ex.Message);
                return null;
            }
        }

        /// <summary>
        /// 领取驿站全部存放体力。成功后同步物品数据并返回响应，失败返回 null。
        /// </summary>
        public async UniTask<ClaimStaminaResponse> ClaimStaminaAsync()
        {
            try
            {
                var proto = await WebDataFun.SendProtoPostRequest<ClaimStaminaResponse>(ApiKeys.ApiStaminaClaim, null);
                if (proto == null)
                {
                    return null;
                }

                if (proto.Items != null)
                {
                    Items.Update(proto.Items);
                }

                UpdateClaimableCache(proto.Stamina);
                return proto;
            }
            catch (System.Exception ex)
            {
                LogTool.LogError("[StationService] ClaimStaminaAsync error: " + ex.Message);
                return null;
            }
        }

        public async UniTask RefreshClaimableCacheAsync()
        {
            await GetStaminaInfoAsync();
        }

        public void UpdateClaimableCache(StationStamina stamina)
        {
            int count = 0;
            if (stamina != null && stamina.Slots != null)
            {
                count = stamina.Slots.Count;
            }

            if (ClaimableSlotCount == count)
            {
                return;
            }

            ClaimableSlotCount = count;
            RedDotExtensions.MarkRedDotDirty(RedNodeType.StationReceiveBtn);
            RedDotExtensions.MarkRedDotDirty(RedNodeType.StationBtn);
        }
    }
}
