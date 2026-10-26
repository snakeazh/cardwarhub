using System;
using System.Threading.Tasks;
using Assets.Scripts.Localization;
using Config;
using FusionContinent.Manager;
using FusionContinent.Model;
using UnityEngine;
using Google.Protobuf;
using Cardwar;

namespace FusionContinent
{
    public static class WebDataFun
    {
        public static async Task<TResponse> SendProtoPostRequest<TResponse>(string url, IMessage requestMessage) where TResponse : class, IMessage, new()
        {
            // 服务端逻辑单机化：全部 proto 请求改走本地分发（LocalApi），不再发起 HTTP。
            // 响应后处理（code 检查 / 服务器时间 / 解包 TResponse）与原网络版保持一致，调用方零改动。
            var msgStr = $"POST-PROTO-API[Local]: {url}; ";
            await Task.Yield();
            try
            {
                GameResponse resp = Global.LocalApi.Dispatch(url, requestMessage);
                int tokenCode = resp.Code;
                string tokenMsg = resp.Msg;
                long serverTime = resp.Time;

                if (tokenCode == 401)
                {
                    Global.ShowConfirm(LocalizationManager.Instance.GetText(
                        LocalizationModule.SystemTexts, "SYS_ERR_AUTHENTICATION_FAILED"), tokenMsg, UserModel.LogOut, null, true);
                }
                if (tokenCode != 200)
                {
                    throw new System.Exception(LocalizationManager.Instance.GetText(LocalizationModule.SystemTexts, tokenMsg));
                }

                if (serverTime != 0)
                {
                    ServerTimeManager.Instance.OnReceiveServerTime(serverTime);
                }

                var result = new TResponse();
                if (resp.Data != null && resp.Data.Length > 0)
                {
                    result.MergeFrom(resp.Data.ToByteArray());
                }

                LogTool.Log($"{msgStr}; msg: {tokenMsg}; dataLen: {resp.Data.Length} data: {resp.Data}");
                return result;
            }
            catch (System.Exception ex)
            {
                LogTool.LogError($"{msgStr}; 请求异常：{ex.Message}");
                throw;
            }
        }

    }
}
