using System;
using Cysharp.Threading.Tasks;
using FusionContinent;
using Game.CombatPower;
using UIFramework;
using UIFramework.Services;

namespace Game.UI
{
    /// <summary>
    /// 战力模块服务：账号战力以服务端下发为准，客户端只做展示、基线对比与 Tip。
    /// </summary>
    public class CombatPowerService : GameServiceBase
    {
        private bool mIsPanelOpened;
        private int mChangeBaseline = -1;

        public int CurrentCombat { get; private set; }
        public int PreviousCombat { get; private set; }
        public CombatPowerSnapshot CurrentSnapshot { get; private set; }

        public event Action<CombatPowerSnapshot> OnCombatPowerChanged;

        public override async UniTask InitializeAsync()
        {
            SyncFromUserFightingNum();
            await base.InitializeAsync();
            LogTool.Log("[CombatPowerService] 初始化完成");
        }

        public void ResetCombat(int value)
        {
            PreviousCombat = value;
            CurrentCombat = value;
        }

        /// <summary>账号展示战力（服务端真值）。</summary>
        public int GetDisplayedAccountPower()
        {
            return Global.User != null ? Global.User.FightingNum : CurrentCombat;
        }

        public CombatPowerSnapshot GetAccountPower()
        {
            return new CombatPowerSnapshot
            {
                Scope = CombatPowerScope.Account,
                TotalPower = GetDisplayedAccountPower(),
            };
        }

        public void SyncSnapshot(CombatPowerSnapshot snapshot)
        {
            ApplySnapshot(snapshot, syncPrevious: false);
        }

        public void SyncFromUserFightingNum()
        {
            if (Global.User == null)
            {
                return;
            }

            var power = Global.User.FightingNum;
            ApplySnapshot(new CombatPowerSnapshot
            {
                Scope = CombatPowerScope.Account,
                TotalPower = power,
            }, syncPrevious: true);
            mChangeBaseline = -1;
        }

        /// <summary>养成操作前记录战力基线（服务端真值）。</summary>
        public void CaptureBaseline()
        {
            mChangeBaseline = GetDisplayedAccountPower();
        }

        /// <summary>写入服务端战力；可选立即弹 Tip。</summary>
        public void ApplyServerPower(int power, bool showTipIfIncreased = false)
        {
            var before = mChangeBaseline >= 0 ? mChangeBaseline : CurrentCombat;
            ApplySnapshot(new CombatPowerSnapshot
            {
                Scope = CombatPowerScope.Account,
                TotalPower = power,
            }, syncPrevious: false);

            if (!showTipIfIncreased)
            {
                return;
            }

            mChangeBaseline = -1;
            if (power <= before)
            {
                return;
            }

            ShowCombatIncreaseTip(before, power);
        }

        /// <summary>战力已由服务端写入后，仅当提升时弹 Tip。</summary>
        public void ShowTipIfIncreasedFromBaseline()
        {
            var before = mChangeBaseline >= 0 ? mChangeBaseline : PreviousCombat;
            var after = GetDisplayedAccountPower();
            CurrentCombat = after;
            mChangeBaseline = -1;

            if (after <= before)
            {
                return;
            }

            ShowCombatIncreaseTip(before, after);
        }

        /// <summary>兼容旧调用名：等价于 ShowTipIfIncreasedFromBaseline。</summary>
        public void RecalculateAndShowTipIfIncreased()
        {
            ShowTipIfIncreasedFromBaseline();
        }

        /// <summary>静默同步后清基线（登录读档等场景）。</summary>
        public void SyncBaselineAfterSilentRecalculate()
        {
            mChangeBaseline = -1;
            PreviousCombat = CurrentCombat;
        }

        public void RecalculateAndNotify(bool showTip = false)
        {
            var before = CurrentCombat;
            var after = GetDisplayedAccountPower();
            ApplySnapshot(new CombatPowerSnapshot
            {
                Scope = CombatPowerScope.Account,
                TotalPower = after,
            }, syncPrevious: false);

            if (showTip)
            {
                ShowCombatDiff(before, after);
            }
        }

        public void RefreshFromGlobal()
        {
            ShowCombatDiff(CurrentCombat, Global.User.FightingNum);
        }

        public void ShowCombatDiff(int startValue, int endValue)
        {
            if (endValue <= startValue)
            {
                PreviousCombat = endValue;
                CurrentCombat = endValue;
                return;
            }

            PreviousCombat = startValue;
            CurrentCombat = endValue;
            ShowTipAsync(startValue, endValue).Forget();
        }

        private void ShowCombatIncreaseTip(int before, int after)
        {
            if (CombatTipController.Instance != null)
            {
                CombatTipController.Instance.ShowCombatDiff(before, after);
                return;
            }

            ShowTipAsync(before, after).Forget();
        }

        public void CloseTip()
        {
            if (!mIsPanelOpened)
            {
                return;
            }

            UIManager.ClosePanel<CombatTipPanelNew>();
            mIsPanelOpened = false;
        }

        private void ApplySnapshot(CombatPowerSnapshot snapshot, bool syncPrevious)
        {
            CurrentSnapshot = snapshot;
            if (Global.User != null)
            {
                Global.User.FightingNum = snapshot.TotalPower;
            }

            CurrentCombat = snapshot.TotalPower;
            if (syncPrevious)
            {
                PreviousCombat = snapshot.TotalPower;
            }

            OnCombatPowerChanged?.Invoke(snapshot);
        }

        private async UniTask ShowTipAsync(int startValue, int endValue)
        {
            var panel = await EnsurePanelOpen();
            if (panel?.VM == null)
            {
                return;
            }

            panel.VM.ShowDiff(startValue, endValue);
        }

        private async UniTask<CombatTipPanelNew> EnsurePanelOpen()
        {
            if (mIsPanelOpened)
            {
                return UIManager.GetPanel<CombatTipPanelNew>();
            }

            var vm = new CombatTipViewModel();
            vm.OnCreate();
            var panel = await UIManager.OpenPanelAsync<CombatTipPanelNew, CombatTipViewModel>(
                vm,
                UIFramework.UILevel.Tips);

            mIsPanelOpened = panel != null;
            return panel;
        }
    }
}
