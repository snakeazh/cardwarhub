using Config;
using Game.UI;
using UIFramework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FusionContinent
{
    [PrefabPath("ChooseCardNumPanel")]
    public class ChooseCardNumPanelNew : UIPanel<ChooseCardNumPanelViewModel>
    {
        private ChooseCardNumPanel mOldPanel;

        private UnityAction _confirmHandler;
        private UnityAction _exitHandler;
        private UnityAction _maskHandler;
        private UnityAction<float> _sliderHandler;

        private System.Action<string, string> _numTextHandler;
        private System.Action<int, int> _maxHandler;
        private System.Action<float, float> _sliderValueHandler;
        private System.Action _requestCloseHandler;

        void Awake()
        {
            mOldPanel = GetComponent<ChooseCardNumPanel>();
            if (mOldPanel == null)
            {
                LogTool.LogError("[ChooseCardNumPanelNew] 找不到 ChooseCardNumPanel 组件");
            }
        }

        protected override void OnOpen(IUIData uiData = null)
        {
            
        }

        protected override void OnBindViewModel(ChooseCardNumPanelViewModel vm)
        {
            if (mOldPanel == null) return;

            _requestCloseHandler = () => CloseSelf();
            vm.OnRequestClose += _requestCloseHandler;

            // 首次绑定也走统一 Rebind（保证与后续 OnOpen 行为一致）
            RebindUI(vm);
        }

        protected override void OnUnbindViewModel(ChooseCardNumPanelViewModel vm)
        {
            if (_numTextHandler != null)
            {
                vm.NumText.OnValueChanged -= _numTextHandler;
                _numTextHandler = null;
            }

            if (mOldPanel != null)
            {
                var slider = mOldPanel.Slider;

                if (_sliderHandler != null)
                {
                    slider.onValueChanged.RemoveListener(_sliderHandler);
                    _sliderHandler = null;
                }

                if (_maxHandler != null) { vm.MaxValue.OnValueChanged -= _maxHandler; _maxHandler = null; }
                if (_sliderValueHandler != null) { vm.SliderValue.OnValueChanged -= _sliderValueHandler; _sliderValueHandler = null; }
            }

            if (_confirmHandler != null && mOldPanel != null && mOldPanel.ConfirmButton != null)
            {
                mOldPanel.ConfirmButton.onClick.RemoveListener(_confirmHandler);
                _confirmHandler = null;
            }

            if (_exitHandler != null && mOldPanel != null)
            {
                var exitButton = mOldPanel.ExitBtn;
                exitButton.onClick.RemoveListener(_exitHandler);
                _exitHandler = null;
            }

            if (_maskHandler != null && mOldPanel != null)
            {
                mOldPanel.Mask.onClick.RemoveListener(_maskHandler);
                _maskHandler = null;
            }

            if (_requestCloseHandler != null)
            {
                vm.OnRequestClose -= _requestCloseHandler;
                _requestCloseHandler = null;
            }
        }

        private void RebindUI(ChooseCardNumPanelViewModel vm)
        {
            if (mOldPanel == null || vm == null) return;
            var slider = mOldPanel.Slider;
            var exitButton = mOldPanel.ExitBtn;

            // 重新同步 UI 值（以 VM 为准）
            mOldPanel.NumText.text = vm.NumText.Value;
            slider.wholeNumbers = false;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.SetValueWithoutNotify(vm.SliderValue.Value);

            // 重新绑定监听（先移除旧的，再添加）
            if (_numTextHandler != null)
            {
                vm.NumText.OnValueChanged -= _numTextHandler;
            }
            _numTextHandler = (o, n) => mOldPanel.NumText.text = n;
            vm.NumText.OnValueChanged += _numTextHandler;

            if (_maxHandler != null) vm.MaxValue.OnValueChanged -= _maxHandler;
            if (_sliderValueHandler != null) vm.SliderValue.OnValueChanged -= _sliderValueHandler;

            _maxHandler = (o, n) =>
            {
                // max 变化时，重新同步一次当前 slider 值（VM 已完成 Recalculate）
                slider.SetValueWithoutNotify(vm.SliderValue.Value);
            };
            _sliderValueHandler = (o, n) =>
            {
                if (Mathf.Abs(slider.value - n) > 0.001f)
                {
                    slider.SetValueWithoutNotify(n);
                }
            };

            vm.MaxValue.OnValueChanged += _maxHandler;
            vm.SliderValue.OnValueChanged += _sliderValueHandler;

            if (_sliderHandler != null)
            {
                slider.onValueChanged.RemoveListener(_sliderHandler);
            }
            _sliderHandler = v => vm.SetSliderValueCommand.Execute(v);
            slider.onValueChanged.AddListener(_sliderHandler);

            if (_confirmHandler != null)
            {
                mOldPanel.ConfirmButton.onClick.RemoveListener(_confirmHandler);
            }
            _confirmHandler = () =>
            {
                UnlockGuideManager.Send(ModuleFunctionType.ArcCardDecompose, 5);
                vm.ConfirmCommand.Execute();
            };
            mOldPanel.ConfirmButton.onClick.AddListener(_confirmHandler);

            if (UnlockGuideManager.Instance != null)
            {
                UnlockGuideManager.Instance.SetUnlockTypeStep(ModuleFunctionType.ArcCardDecompose, 5, mOldPanel.ConfirmButton.GetComponent<RectTransform>());
            }

            if (_exitHandler != null)
            {
                exitButton.onClick.RemoveListener(_exitHandler);
            }
            _exitHandler = () => vm.CancelCommand.Execute();
            exitButton.onClick.AddListener(_exitHandler);

            if (_maskHandler != null)
            {
                mOldPanel.Mask.onClick.RemoveListener(_maskHandler);
            }
            _maskHandler = () => vm.CancelCommand.Execute();
            mOldPanel.Mask.onClick.AddListener(_maskHandler);
        }
    }
}

