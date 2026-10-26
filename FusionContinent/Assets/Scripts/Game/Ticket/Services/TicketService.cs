using System;
using System.Collections.Generic;
using Cardwar;
using Config;
using Cysharp.Threading.Tasks;
using FusionContinent;
using FusionContinent.Manager;
using FusionContinent.Model;
using FusionContinent.Utils;
using UIFramework.Services;
using PlayerTicket = FusionContinent.Model.PlayerTicket;

namespace Game.Ticket
{
    /// <summary>
    /// 门票服务：领取每日刷新门票，并维护玩家门票状态。
    /// </summary>
    public class TicketService : GameServiceBase
    {
        public PlayerTicket GetTicket(int ticketId)
        {
            Global.PlayerTickets.TryGetValue(ticketId, out var ticket);
            return ticket;
        }

        public TicketConfig GetTicketConfig(int ticketId)
        {
            return DataConfigManager.Tables.TbTicketConfig.GetOrDefault(ticketId);
        }

        public TicketConfig GetTicketConfigByType(StageType stageType)
        {
            var list = DataConfigManager.Tables.TbTicketConfig.DataList;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Type == stageType)
                {
                    return list[i];
                }
            }

            return null;
        }

        public bool CanClaim(int ticketId)
        {
            if (!Global.PlayerTickets.TryGetValue(ticketId, out var ticket))
            {
                return true;
            }

            if (ticket.NextClaimTime <= 0)
            {
                return true;
            }

            return ServerTimeManager.Instance.GetCurrentServerTimestamp() >= ticket.NextClaimTime;
        }

        public void ApplyArchive(IEnumerable<Cardwar.PlayerTicket> tickets)
        {
            Global.PlayerTickets.Clear();
            foreach (var ticket in tickets)
            {
                var local = ProtoMappers.MapPlayerTicket(ticket);
                if (local != null)
                {
                    Global.PlayerTickets[local.TicketId] = local;
                }
            }

            NotifyDataChanged();
        }

        public async UniTask<bool> ClaimTicketAsync(int ticketId)
        {
            try
            {
                var request = new ClaimTicketRequest { TicketId = ticketId };
                var response = await WebDataFun.SendProtoPostRequest<ClaimTicketResponse>(
                    ApiKeys.ApiTicketClaim,
                    request);

                if (response == null)
                {
                    Toast.Show("领取失败");
                    return false;
                }

                if (response.Ticket != null)
                {
                    UpdateTicket(ProtoMappers.MapPlayerTicket(response.Ticket));
                }

                if (response.Items != null)
                {
                    Items.Update(response.Items);
                }

                var config = GetTicketConfig(ticketId);
                if (config != null && config.DailyRefreshItem.Count > 0)
                {
                    await GlobalFunc.ShowRewardTips(config.DailyRefreshItem);
                }

                NotifyDataChanged();
                return true;
            }
            catch (Exception ex)
            {
                LogTool.LogError("[TicketService] ClaimTicket failed: " + ex.Message);
                Toast.Show(ex.Message);
                return false;
            }
        }

        public override void Reset()
        {
            Global.PlayerTickets.Clear();
            base.Reset();
        }

        private void UpdateTicket(PlayerTicket ticket)
        {
            Global.PlayerTickets[ticket.TicketId] = ticket;
        }
    }
}
