using FusionContinent;
using Game.UI;
using UIFramework.Services;

namespace Game.CombatPower
{
    /// <summary>
    /// 账号战力真值来自服务端下发；客户端仅记录基线并在提升时弹 Tip。
    /// 养成前 CaptureBaseline，回包经 Items.Update 写入战力后，再 RecalculateAndShowTipIfIncreased（仅 Tip）。
    /// </summary>
    public static class CombatPowerChangeHelper
    {
        private static int sFallbackBaseline = -1;

        /// <summary>养成/更换装备前调用，记录当前账号战力（服务端真值）。</summary>
        public static void CaptureBaseline()
        {
            var service = GameServiceManager.Get<CombatPowerService>();
            if (service != null)
            {
                service.CaptureBaseline();
                return;
            }

            sFallbackBaseline = Global.User.FightingNum;
        }

        /// <summary>
        /// 应用服务端下发的账号战力。
        /// showTipIfIncreased 为 false 时只写入，由调用方之后调用 RecalculateAndShowTipIfIncreased 弹 Tip。
        /// </summary>
        public static void ApplyServerPower(int power, bool showTipIfIncreased = false)
        {
            var service = GameServiceManager.Get<CombatPowerService>();
            if (service != null)
            {
                service.ApplyServerPower(power, showTipIfIncreased);
                return;
            }

            var before = sFallbackBaseline >= 0 ? sFallbackBaseline : Global.User.FightingNum;
            Global.User.FightingNum = power;
            if (!showTipIfIncreased)
            {
                return;
            }

            sFallbackBaseline = -1;
            if (power <= before)
            {
                return;
            }

            if (CombatTipController.Instance != null)
            {
                CombatTipController.Instance.ShowCombatDiff(before, power);
            }
        }

        /// <summary>
        /// 养成成功后调用：账号战力应由 Items.Update 已写入；此处仅对比基线，提升时弹 Tip。
        /// 方法名保留以兼容既有养成调用点。
        /// </summary>
        public static void RecalculateAndShowTipIfIncreased()
        {
            var service = GameServiceManager.Get<CombatPowerService>();
            if (service != null)
            {
                service.ShowTipIfIncreasedFromBaseline();
                return;
            }

            var before = sFallbackBaseline >= 0 ? sFallbackBaseline : Global.User.FightingNum;
            var after = Global.User.FightingNum;
            sFallbackBaseline = -1;

            if (after <= before)
            {
                return;
            }

            if (CombatTipController.Instance != null)
            {
                CombatTipController.Instance.ShowCombatDiff(before, after);
            }
        }

        /// <summary>登录读档等：用已写入的 FightingNum 同步服务缓存，不弹 Tip、不本地重算。</summary>
        public static void RecalculateSilent()
        {
            SyncFromUserFightingNum();
        }

        /// <summary>用 Global.User.FightingNum 同步 CombatPowerService / Tip 基线。</summary>
        public static void SyncFromUserFightingNum()
        {
            sFallbackBaseline = -1;
            var service = GameServiceManager.Get<CombatPowerService>();
            if (service != null)
            {
                service.SyncFromUserFightingNum();
                return;
            }

            if (CombatTipController.Instance != null)
            {
                CombatTipController.Instance.SetCombatNum(Global.User.FightingNum);
            }
        }
    }
}
