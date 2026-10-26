using UIFramework;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// 战力提示 ViewModel
    /// </summary>
    public class CombatTipViewModel : ViewModel
    {
        public BindableProperty<int> StartValue { get; } = new BindableProperty<int>(0);
        public BindableProperty<int> EndValue { get; } = new BindableProperty<int>(0);
        public BindableProperty<int> ChangeValue { get; } = new BindableProperty<int>(0);
        public BindableProperty<bool> IsAdd { get; } = new BindableProperty<bool>(true);

        public event System.Action OnShowRequested;
        public event System.Action OnHideRequested;

        public void ShowDiff(int startValue, int endValue)
        {
            if (startValue == endValue)
            {
                return;
            }

            StartValue.Value = startValue;
            EndValue.Value = endValue;
            ChangeValue.Value = Mathf.Abs(endValue - startValue);
            IsAdd.Value = endValue >= startValue;
            OnShowRequested?.Invoke();
        }

        public void HideTip()
        {
            OnHideRequested?.Invoke();
        }
    }
}
