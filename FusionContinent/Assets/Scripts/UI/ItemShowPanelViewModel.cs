using UIFramework;

namespace FusionContinent
{
    public class ItemShowPanelViewModel : ViewModel
    {
        public RewardEntityData Data;

        public Command OnCloseCommand { get; private set; }

        public override void OnCreate()
        {
            base.OnCreate();
            OnCloseCommand = new Command(OnClose);
        }

        private void OnClose()
        {
            OnCloseRequested?.Invoke();
        }

        public System.Action OnCloseRequested;
    }
}
