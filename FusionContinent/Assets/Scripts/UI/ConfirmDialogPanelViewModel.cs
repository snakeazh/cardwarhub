using System;
using UIFramework;

namespace FusionContinent
{
    public class ConfirmDialogPanelViewModel : ViewModel
    {
        public string Title;
        public string Message;
        public bool NoCancel;

        public Command OnConfirmCommand { get; private set; }
        public Command OnCancelCommand { get; private set; }
        public Command OnToggleOptionCommand { get; private set; }

        public event Action OnConfirmRequested;
        public event Action OnCancelRequested;

        public override void OnCreate()
        {
            base.OnCreate();
            OnConfirmCommand = new Command(OnConfirm);
            OnCancelCommand = new Command(OnCancel);
            OnToggleOptionCommand = new Command(OnToggleOption);
        }

        private void OnConfirm()
        {
            OnConfirmRequested?.Invoke();
        }

        private void OnCancel()
        {
            // 取消时撤销"不再提示"状态，只有点击确认才保存
            Global.ConfirmDialogPanelStatus = 1;
            OnCancelRequested?.Invoke();
        }

        private void OnToggleOption()
        {
            var activeSelf = Global.ConfirmDialogPanelStatus == 2;
            Global.ConfirmDialogPanelStatus = !activeSelf ? 2 : 1;
        }
    }
}
