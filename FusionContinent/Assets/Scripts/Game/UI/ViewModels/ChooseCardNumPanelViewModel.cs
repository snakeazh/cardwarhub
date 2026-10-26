using System;
using UIFramework;

namespace Game.UI
{
    public class ChooseCardNumPanelViewModel : ViewModel
    {
        #region 属性
        /// <summary>
        /// 外部传入的最大值
        /// </summary>
        public BindableProperty<int> MaxValue { get; } = new BindableProperty<int>(1);

        /// <summary>
        /// Slider 0..1 的比例值
        /// </summary>
        public BindableProperty<float> SliderValue { get; } = new BindableProperty<float>(0f);

        /// <summary>
        /// 计算出的选中整数值（0..MaxValue）
        /// </summary>
        public BindableProperty<int> SelectedValue { get; } = new BindableProperty<int>(0);

        /// <summary>
        /// 文本规则："{SelectedValue}/{MaxValue}"
        /// </summary>
        public BindableProperty<string> NumText { get; } = new BindableProperty<string>("0/1");
        #endregion

        #region 命令
        public Command CloseCommand { get; private set; }
        public Command ConfirmCommand { get; private set; }
        public Command CancelCommand { get; private set; }
        public Command<float> SetSliderValueCommand { get; private set; }
        #endregion

        #region 事件
        public Action OnRequestClose;
        public Action<int> OnConfirmed;
        public Action OnCancelled;
        #endregion

        public override void OnCreate()
        {
            base.OnCreate();
            CloseCommand = new Command(RequestClose);
            CancelCommand = new Command(Cancel);
            ConfirmCommand = new Command(Confirm);
            SetSliderValueCommand = new Command<float>(SetSliderValue);

            Recalculate();
        }

        /// <summary>
        /// 打开前由外部传入 maxValue
        /// </summary>
        public void Initialize(int maxValue, float initialSliderValue = 1f)
        {
            MaxValue.Value = maxValue;
            SetSliderValue(initialSliderValue);
        }

        private void SetSliderValue(float sliderValue)
        {
            if (sliderValue < 0f) sliderValue = 0f;
            if (sliderValue > 1f) sliderValue = 1f;
            SliderValue.Value = sliderValue;
            Recalculate();
        }

        private void Recalculate()
        {
            var max = MaxValue.Value;
            if (max < 0) max = 0;

            var selected = (int)Math.Round(SliderValue.Value * max, MidpointRounding.AwayFromZero);
            if (selected < 0) selected = 0;
            if (selected > max) selected = max;

            SelectedValue.Value = selected;
            NumText.Value = $"{selected}/{max}";
        }

        private void RequestClose()
        {
            OnRequestClose?.Invoke();
        }

        private void Confirm()
        {
            OnConfirmed?.Invoke(SelectedValue.Value);
            OnRequestClose?.Invoke();
        }

        private void Cancel()
        {
            OnCancelled?.Invoke();
            OnRequestClose?.Invoke();
        }
    }
}

