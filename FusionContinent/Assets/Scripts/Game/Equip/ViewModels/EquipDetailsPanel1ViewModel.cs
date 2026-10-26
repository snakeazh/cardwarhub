using FusionContinent;
using FusionContinent.Model;
using Game.CombatPower;
using UIFramework;
using UIFramework.Services;

namespace Game.Equip
{
    public class EquipDetailsPanel1ViewModel : ViewModel
    {
        public BindableProperty<int> SoldierId { get; } = new BindableProperty<int>(0);
        public BindableProperty<int> EquipOnlyId { get; } = new BindableProperty<int>(0);
        public BindableProperty<PlayerEquip> CurrentEquip { get; } = new BindableProperty<PlayerEquip>(null);
        public BindableProperty<bool> WearRequestPending { get; } = new BindableProperty<bool>(false);

        public Command OnWearCommand { get; private set; }
        public Command OnQuitCommand { get; private set; }
        public Command OnUpgradeCommand { get; private set; }

        private EquipService mEquipService;

        public System.Action OnCloseRequested;
        public System.Action<int, int> OnNavigateUpgradeRequested;

        public override void OnCreate()
        {
            base.OnCreate();
            mEquipService = GameServiceManager.Get<EquipService>();
            OnWearCommand = new Command(OnWear);
            OnQuitCommand = new Command(OnQuit);
            OnUpgradeCommand = new Command(OnUpgrade);
        }

        public void Initialize(int equipOnlyId, int soldierId)
        {
            EquipOnlyId.Value = equipOnlyId;
            SoldierId.Value = soldierId;
            ReloadEquipFromGlobal();
        }

        public void ReloadEquipFromGlobal()
        {
            var id = EquipOnlyId.Value;
            if (Global.PlayerEquips == null || !Global.PlayerEquips.TryGetValue(id, out var eq) || eq == null)
            {
                LogTool.LogError("[EquipDetailsPanel1ViewModel] 装备数据不存在");
                CurrentEquip.Value = null;
                return;
            }

            CurrentEquip.Value = eq;
        }

        private async void OnWear()
        {
            var eq = CurrentEquip.Value;
            if (eq == null || mEquipService == null)
            {
                return;
            }

            WearRequestPending.Value = true;
            try
            {
                CombatPowerChangeHelper.CaptureBaseline();

                var ok = await mEquipService.WearEquipByDetailApiAsync(eq.Id, SoldierId.Value);
                if (ok)
                {
                    OnWearCompleted?.Invoke();
                    ReloadEquipFromGlobal();
                }
            }
            finally
            {
                WearRequestPending.Value = false;
            }
        }

        private void OnQuit()
        {
            OnCloseRequested?.Invoke();
        }

        private void OnUpgrade()
        {
            OnNavigateUpgradeRequested?.Invoke(SoldierId.Value, EquipOnlyId.Value);
        }

        public System.Action OnWearCompleted;
    }
}
