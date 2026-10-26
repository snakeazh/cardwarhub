using UnityEngine;

namespace FusionContinent
{
    /// <summary>
    /// 当前局战斗会话（模式、对手数据等）。进局前设置，默认 PVE。
    /// PVP 对侧真值源：<see cref="Opponent"/>（由 api/pvp/start 初始化）。
    /// </summary>
    public static class BattleSession
    {
        public static BattleMode CurrentMode { get; private set; } = BattleMode.Pve;

        public static bool IsPvpAi => CurrentMode == BattleMode.PvpAi;

        /// <summary>当前 PVP 对局 match_id（/api/pvp/start 返回），结算上报时使用。</summary>
        public static int CurrentMatchId { get; private set; }

        /// <summary>PVP 对侧局内上下文（pvp/start 初始化后可用）。</summary>
        public static OpponentBattleContext Opponent { get; private set; }

        /// <summary>原始对侧数据（Cardwar.PlayerProfile）；优先通过 <see cref="Opponent"/> 使用解析后的数据。</summary>
        public static Cardwar.PlayerProfile OpponentDetail
        {
            get
            {
                if (Opponent == null)
                {
                    return null;
                }

                return Opponent.Raw;
            }
        }

        public static void SetMode(BattleMode mode)
        {
            CurrentMode = mode;
        }

        /// <summary>
        /// 由 /api/pvp/start 响应初始化对侧上下文并切入 PVP AI 模式。
        /// </summary>
        public static void BindPvpStart(Cardwar.PvpStartResponse resp)
        {
            if (resp == null || resp.Opponent == null)
            {
                return;
            }

            CurrentMatchId = resp.MatchId;
            Opponent = OpponentBattleContext.FromProfile(resp.Opponent);
            CurrentMode = BattleMode.PvpAi;
        }

        /// <summary>
        /// 结算成功后清空 match_id，防止退局/返回时重复上报 settle。
        /// 保留 Opponent / Mode，直到 <see cref="ResetToPve"/>。
        /// </summary>
        public static void ClearMatchIdAfterSettle()
        {
            CurrentMatchId = 0;
        }

        /// <summary>兼容入口：直接用 PlayerProfile 绑定对侧（不设置 match_id）。</summary>
        public static void BindOpponentFromProfile(Cardwar.PlayerProfile profile)
        {
            if (profile == null)
            {
                return;
            }

            Opponent = OpponentBattleContext.FromProfile(profile);
            CurrentMode = BattleMode.PvpAi;
        }

        public static void ResetToPve()
        {
            CurrentMode = BattleMode.Pve;
            Opponent = null;
            CurrentMatchId = 0;
        }
    }
}
