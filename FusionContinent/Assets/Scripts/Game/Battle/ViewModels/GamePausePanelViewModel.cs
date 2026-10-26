using System.Collections.Generic;
using Config;
using UIFramework;

namespace Game.Battle
{
    public class GamePausePanelViewModel : ViewModel
    {
        public BindableProperty<bool> IsSchoolTabSelected { get; } = new BindableProperty<bool>(true);

        public Command OnBackCommand { get; private set; }
        public Command OnContinueCommand { get; private set; }
        public Command OnSelectSchoolTabCommand { get; private set; }
        public Command OnSelectRewardTabCommand { get; private set; }

        public System.Action OnBackRequested;
        public System.Action OnContinueRequested;
        public System.Action OnSelectSchoolTabRequested;
        public System.Action OnSelectRewardTabRequested;

        public override void OnCreate()
        {
            base.OnCreate();

            OnBackCommand = new Command(OnBack);
            OnContinueCommand = new Command(OnContinue);
            OnSelectSchoolTabCommand = new Command(OnSelectSchoolTab);
            OnSelectRewardTabCommand = new Command(OnSelectRewardTab);
        }

        private void OnBack()
        {
            OnBackRequested?.Invoke();
        }

        private void OnContinue()
        {
            OnContinueRequested?.Invoke();
        }

        private void OnSelectSchoolTab()
        {
            if (IsSchoolTabSelected.Value) return;
            IsSchoolTabSelected.Value = true;
            OnSelectSchoolTabRequested?.Invoke();
        }

        private void OnSelectRewardTab()
        {
            if (!IsSchoolTabSelected.Value) return;
            IsSchoolTabSelected.Value = false;
            OnSelectRewardTabRequested?.Invoke();
        }
    }
}
