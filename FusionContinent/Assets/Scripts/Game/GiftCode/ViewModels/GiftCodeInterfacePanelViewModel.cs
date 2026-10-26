using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using Cardwar;
using Config;
using FusionContinent;
using FusionContinent.Model;
using FusionContinent.Utils;
using UIFramework;

namespace Game.GiftCode
{
    public class GiftCodeInterfacePanelViewModel : ViewModel
    {
        public Command OnCloseCommand { get; private set; }
        public Command<string> OnExchangeCommand { get; private set; }

        public System.Action<string> OnShowError;

        public override void OnCreate()
        {
            base.OnCreate();

            OnCloseCommand = new Command(OnClose);
            OnExchangeCommand = new Command<string>(OnExchange);
        }

        private void OnClose()
        {
        }

        private async void OnExchange(string codeField)
        {
            try
            {
                if (string.IsNullOrEmpty(codeField))
                {
                    OnShowError?.Invoke("礼包码不能为空");
                    return;
                }

                var codeCfg = DataConfigManager.Tables.TbCodeExchangeGiftsConfig.GetOrDefault(codeField);
                if (codeCfg == null)
                {
                    OnShowError?.Invoke("礼包码不存在");
                    return;
                }

                if (Global.PlayerExchangeGift.CodeExchangesMap.TryGetValue(codeField, out var status) && status > 0)
                {
                    OnShowError?.Invoke("礼包码已兑换");
                    return;
                }

                var request = new CodeExchangeGiftsRequest { Code = codeField };
                var proto = await WebDataFun.SendProtoPostRequest<CodeExchangeGiftsResponse>(ApiKeys.ApiCodeExchangeGifts, request);
                if (proto == null || proto.ExchangeGift == null)
                {
                    throw new System.Exception("服务器返回空响应");
                }

                Global.PlayerExchangeGift = ProtoMappers.MapPlayerExchangeGift(proto.ExchangeGift);
                Items.Update(proto.Items);

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
            }
            catch (System.Exception ex)
            {
                OnShowError?.Invoke(ex.Message);
            }
        }
    }
}
