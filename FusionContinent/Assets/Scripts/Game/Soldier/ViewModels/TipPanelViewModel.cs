using System;
using UIFramework;

namespace FusionContinent
{
    public class TipPanelViewModel : ViewModel
    {
        public string Title;
        public string Detail;
        public bool ShowTitle => !string.IsNullOrEmpty(Title);

        public Command OnCloseCommand { get; private set; }

        public event Action OnCloseRequested;

        public override void OnCreate()
        {
            base.OnCreate();
            OnCloseCommand = new Command(Close);
        }

        private void Close()
        {
            OnCloseRequested?.Invoke();
        }
    }
}
