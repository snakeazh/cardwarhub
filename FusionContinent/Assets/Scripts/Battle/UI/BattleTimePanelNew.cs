using Config;
using DG.Tweening;
using FusionContinent.Model;
using Game.Battle;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FusionContinent.Guide;
using UIFramework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FusionContinent
{
    [PrefabPath("BattleTimePanel")]
    /// <summary>
    /// 战斗时间管理子界面 - 新框架版本
    /// 作为 BattlePanelNew 的子界面，不参与 UIManager 堆栈管理
    /// </summary>
    public class BattleTimePanelNew : UIPanel<BattleTimeViewModel>
    {
        /// <summary>
        /// 作为子界面，不参与UIManager堆栈管理
        /// </summary>
        public override PanelStackMode StackMode => PanelStackMode.Unmanaged;

        // 引用 GameLandUI 的 UI 组件
        public BattleTimePanel mOldPanel;

        // 直接引用的 UI 组件（不依赖 mOldPanel）
        private TMPro.TextMeshProUGUI mSpeedTxt;
        private UnityEngine.UI.Toggle mAutoToggle;

        // 事件处理器（用于解绑）
        private System.Action<string, string> _formattedTimeHandler;
        private System.Action<string, string> _speedTextHandler;
        private System.Action<bool, bool> _isPausedHandler;
        private System.Action<bool, bool> _isAutoReleaseSkillHandler;
        private System.Action<bool, bool> _canAutoReleaseSkillHandler;
        private System.Action<string, string> _stageTitleHandler;
        private System.Action<string, string> _cityLevelTextHandler;
        private System.Action<string, string> _totalDamageTextHandler;
        private System.Action<bool, bool> _showTotalDamageHandler;
        private UnityEngine.Events.UnityAction _pauseBtnHandler;
        private UnityEngine.Events.UnityAction _playBtnHandler;
        private UnityEngine.Events.UnityAction _speedToggleBtnHandler;
        private UnityEngine.Events.UnityAction<bool> _autoToggleHandler;
        public Tween _rotateTween; // 保存旋转的 Tween 对象
        void Awake()
        {
            // 获取 GameLandUI 引用
            mOldPanel = GetComponent<BattleTimePanel>();
            if (mOldPanel == null)
            {
                LogTool.LogError("[BattleTimePanelNew] GameLandUI.Default 为空");
            }

            // 直接从 SpeedUpBtn 子物体获取 speedtxt
            if (mOldPanel?.SpeedUpBtn != null)
            {
                var speedTxtObj = mOldPanel.SpeedUpBtn.transform.Find("speedtxt");
                if (speedTxtObj != null)
                {
                    mSpeedTxt = speedTxtObj.GetComponent<TMPro.TextMeshProUGUI>();
                }
            }

            if (mSpeedTxt == null)
            {
                LogTool.LogError("[BattleTimePanelNew] speedtxt 子物体未找到");
            }

            // 获取 AutoToggle 引用
            if (mOldPanel?.AutoToggle != null)
            {
                mAutoToggle = mOldPanel.AutoToggle;
            }

            if (mAutoToggle == null)
            {
                LogTool.LogError("[BattleTimePanelNew] AutoToggle 未找到");
            }
        }

        protected override void OnBindViewModel(BattleTimeViewModel vm)
        {
            if (mOldPanel == null) return;

            // 绑定属性
            BindProperties(vm);

            // 绑定按钮
            BindButtons(vm);
            RefreshAutoToggle();
            
            Global.Publisher.Subscribe(MessagePublisher.CHANGE_GUIDE_PROGRESS_DONE,OnGuideDone);
            // mAutoToggle.gameObject.SetActive();
            LogTool.Log("[BattleTimePanelNew] 绑定完成");
        }

        private void OnGuideDone(object obj)
        {
            int value = (int)obj;
            
            if (value == (int)GuideType.WeaknessWordTip)
            {
                RefreshAutoToggle();
            }
            
        }

        void RefreshAutoToggle()
        {
            // mAutoToggle.gameObject.SetActive();
            if (GamePlayManager.Default.GuideIsRunning() )
            {
                mAutoToggle.gameObject.SetActive( GuideManager.Instance.GuideIsSuccess(GuideType.WeaknessWordTip));
            }
        }

        protected override void OnUnbindViewModel(BattleTimeViewModel vm)
        {
            // 解绑属性
            UnbindProperties(vm);

            // 解绑按钮
            UnbindButtons();
            Global.Publisher.Unsubscribe(MessagePublisher.CHANGE_GUIDE_PROGRESS_DONE,OnGuideDone);
        }

        #region 数据绑定

        private void BindProperties(BattleTimeViewModel vm)
        {
            // 时间显示绑定
            _formattedTimeHandler = (oldVal, newVal) =>
            {
                // if (mOldPanel?.timetxt != null)
                //     mOldPanel.timetxt.text = newVal;
            };
            vm.FormattedTime.OnValueChanged += _formattedTimeHandler;

            // 倍速文本绑定
            _speedTextHandler = (oldVal, newVal) =>
            {
                if (mSpeedTxt != null)
                    mSpeedTxt.text = newVal;
                else if (mOldPanel?.speedtxt != null)
                    mOldPanel.speedtxt.text = newVal;
            };
            vm.SpeedText.OnValueChanged += _speedTextHandler;

            // 暂停状态绑定
            _isPausedHandler = (oldVal, newVal) =>
            {
                // if (mOldPanel?.PausedBtn != null) 
                //     mOldPanel.PausedBtn.gameObject.SetActive(!newVal);
                // if (mOldPanel?.PlayBtn != null) 
                //     mOldPanel.PlayBtn.gameObject.SetActive(newVal);
            };
            vm.IsPaused.OnValueChanged += _isPausedHandler;

            // 自动释放技能状态绑定
            _isAutoReleaseSkillHandler = (oldVal, newVal) =>
            {
                UpdateTween();
                SyncAutoToggleWithoutNotify(newVal);
            };
            vm.IsAutoReleaseSkill.OnValueChanged += _isAutoReleaseSkillHandler;

            // 是否可以自动释放技能绑定（控制Toggle交互）
            _canAutoReleaseSkillHandler = (oldVal, newVal) =>
            {
                if (mAutoToggle != null)
                    mAutoToggle.interactable = newVal;
                else if (mOldPanel?.AutoToggle != null)
                    mOldPanel.AutoToggle.interactable = newVal;
            };
            vm.CanAutoReleaseSkill.OnValueChanged += _canAutoReleaseSkillHandler;

            // 关卡名称绑定
            _stageTitleHandler = (oldVal, newVal) =>
            {
                if (mOldPanel.StageTitle != null)
                    mOldPanel.StageTitle.text = newVal;
                // 应用渐变颜色
                GlobalFunc.SetTMPVerticalGradient(
                    mOldPanel.StageTitle,
                    vm.StageTitleColor1.Value,
                    vm.StageTitleColor2.Value);
            };
            vm.StageTitle.OnValueChanged += _stageTitleHandler;

            // 波数显示绑定
            _cityLevelTextHandler = (oldVal, newVal) =>
            {
                if (mOldPanel.CityLevelTxt != null)
                    mOldPanel.CityLevelTxt.text = newVal;
                // 应用渐变颜色
                GlobalFunc.SetTMPVerticalGradient(
                    mOldPanel.CityLevelTxt,
                    vm.StageTitleColor1.Value,
                    vm.StageTitleColor2.Value);
            };
            vm.CityLevelText.OnValueChanged += _cityLevelTextHandler;

            // 伤害显示绑定
            _totalDamageTextHandler = (oldVal, newVal) =>
            {
                if (mOldPanel?.TotalDamageText != null)
                    mOldPanel.TotalDamageText.text = newVal;
            };
            vm.TotalDamageText.OnValueChanged += _totalDamageTextHandler;

            // 伤害显示开关绑定
            _showTotalDamageHandler = (oldVal, newVal) =>
            {
                if (mOldPanel?.TotalDamageText != null)
                    mOldPanel.TotalDamageText.gameObject.SetActive(newVal);
            };
            vm.ShowTotalDamage.OnValueChanged += _showTotalDamageHandler;

            // 初始化显示
            // if (mOldPanel?.timetxt != null)
            //     mOldPanel.timetxt.text = vm.FormattedTime.Value;
            if (mSpeedTxt != null)
                mSpeedTxt.text = vm.SpeedText.Value;
            else if (mOldPanel?.speedtxt != null)
                mOldPanel.speedtxt.text = vm.SpeedText.Value;
            // AutoToggle.isOn 放到 BindButtons 之后用 SyncAutoToggleWithoutNotify 同步，避免监听顺序问题
            if (mOldPanel.StageTitle != null)
            {
                mOldPanel.StageTitle.text = vm.StageTitle.Value;
                // 应用渐变颜色
                GlobalFunc.SetTMPVerticalGradient(
                    mOldPanel.StageTitle,
                    vm.StageTitleColor1.Value,
                    vm.StageTitleColor2.Value);
            }
            if (mOldPanel.CityLevelTxt != null)
            {
                mOldPanel.CityLevelTxt.text = vm.CityLevelText.Value;
                // 应用渐变颜色
                GlobalFunc.SetTMPVerticalGradient(
                    mOldPanel.CityLevelTxt,
                    vm.StageTitleColor1.Value,
                    vm.StageTitleColor2.Value);
            }
            if (mOldPanel.TotalDamageText != null)
            {
                mOldPanel.TotalDamageText.text = vm.TotalDamageText.Value;
                mOldPanel.TotalDamageText.gameObject.SetActive(vm.ShowTotalDamage.Value);
            }
        }

        private void UnbindProperties(BattleTimeViewModel vm)
        {
            if (_formattedTimeHandler != null)
            {
                vm.FormattedTime.OnValueChanged -= _formattedTimeHandler;
                _formattedTimeHandler = null;
            }

            if (_speedTextHandler != null)
            {
                vm.SpeedText.OnValueChanged -= _speedTextHandler;
                _speedTextHandler = null;
            }

            if (_isPausedHandler != null)
            {
                vm.IsPaused.OnValueChanged -= _isPausedHandler;
                _isPausedHandler = null;
            }

            // if (_speedLevelHandler != null)
            // {
            //     vm.SpeedLevel.OnValueChanged -= _speedLevelHandler;
            //     _speedLevelHandler = null;
            // }

            if (_isAutoReleaseSkillHandler != null)
            {
                vm.IsAutoReleaseSkill.OnValueChanged -= _isAutoReleaseSkillHandler;
                _isAutoReleaseSkillHandler = null;
            }

            if (_canAutoReleaseSkillHandler != null)
            {
                vm.CanAutoReleaseSkill.OnValueChanged -= _canAutoReleaseSkillHandler;
                _canAutoReleaseSkillHandler = null;
            }

            if (_stageTitleHandler != null)
            {
                vm.StageTitle.OnValueChanged -= _stageTitleHandler;
                _stageTitleHandler = null;
            }

            if (_cityLevelTextHandler != null)
            {
                vm.CityLevelText.OnValueChanged -= _cityLevelTextHandler;
                _cityLevelTextHandler = null;
            }

            if (_totalDamageTextHandler != null)
            {
                vm.TotalDamageText.OnValueChanged -= _totalDamageTextHandler;
                _totalDamageTextHandler = null;
            }

            if (_showTotalDamageHandler != null)
            {
                vm.ShowTotalDamage.OnValueChanged -= _showTotalDamageHandler;
                _showTotalDamageHandler = null;
            }
        }

        #endregion

        #region 按钮绑定

        private void BindButtons(BattleTimeViewModel vm)
        {
           
            // 暂停按钮 - 打开暂停界面
            if (mOldPanel.PausedBtn != null)
            {
                _pauseBtnHandler = () =>
                {
                    if (!Global.Player.CheckGuideStatus(GuideType.GuideStage) && Global.StageId.Value == DataConfigManager.Tables.TbPlayerConst.GuidesStageId)
                    {
                        FusionContinent.Toast.Show("请先完成引导关卡");
                        return;
                    }
                    // PlayClickSound();
                    // 暂停游戏
                    GamePlayManager.Default?.PauseGame();
                    vm.OnPauseCommand.Execute();
                    // 打开暂停面板
                    var pauseVm = new Game.Battle.GamePausePanelViewModel();
                    pauseVm.OnCreate();
                    UIFramework.UIManager.OpenPanelAsync<GamePausePanelNew, Game.Battle.GamePausePanelViewModel>(pauseVm, UIFramework.UILevel.PopUI).Forget();
                };
                mOldPanel.PausedBtn.onClick.AddListener(_pauseBtnHandler);
            }

            // 播放按钮 - 从暂停界面恢复
            if (mOldPanel?.PlayBtn != null)
            {
                _playBtnHandler = () =>
                {
                    PlayClickSound();
                    // 恢复游戏
                    GamePlayManager.Default?.ResumeGame();
                    vm.OnResumeCommand.Execute();
                    // 关闭暂停面板（如果打开的话）
                    UIFramework.UIManager.ClosePanel<GamePausePanelNew>();
                };
                mOldPanel.PlayBtn.onClick.AddListener(_playBtnHandler);
            }

            // 切换倍速按钮（一倍速/二倍速循环）
            if (mOldPanel?.SpeedUpBtn != null)
            {
                _speedToggleBtnHandler = () =>
                {
                    PlayClickSound();
                    vm.ToggleSpeedCommand.Execute();
                };
                mOldPanel.SpeedUpBtn.onClick.AddListener(_speedToggleBtnHandler);
            }
            // 自动释放技能Toggle
            var toggleForBinding = mAutoToggle ?? mOldPanel?.AutoToggle;
            if (toggleForBinding != null)
            {
                _autoToggleHandler = (isOn) =>
                {
                    PlayClickSound();
                    vm.OnAutoReleaseSkillChangedCommand.Execute(isOn);
                    Global.Publisher.Publish(MessagePublisher.CHANGE_GUIDE_PROGRESS,GuideType.AutoBattleWordTip);
                };
                toggleForBinding.onValueChanged.AddListener(_autoToggleHandler);
                // 先挂监听再静默同步，避免开局 isOn 写回把自动关掉，或漏掉旋转动画
                SyncAutoToggleWithoutNotify(vm.IsAutoReleaseSkill.Value);
                toggleForBinding.interactable = vm.CanAutoReleaseSkill.Value;
                UpdateTween();
            }
            // 初始化按钮状态
            UpdateButtonStates(vm);
        }

        public void UpdateTween()
        {
            var toggle = mAutoToggle ?? mOldPanel?.AutoToggle;
            if (toggle == null) return;

            if (Global.AutoReleaseSkillState.Value)
            {
                var imageTransform = toggle.transform.Find("Image");
                if (imageTransform != null)
                {
                    _rotateTween = imageTransform
                        .DORotate(new Vector3(0, 0, -360), 2f, RotateMode.LocalAxisAdd)
                        .SetEase(Ease.Linear)
                        .SetLoops(-1, LoopType.Restart);
                }
            }
            else
            {
                var imageTransform = toggle.transform.Find("Image");
                if (imageTransform != null)
                {
                    imageTransform.DOKill();
                }
            }
        }

        /// <summary>
        /// 同步 AutoToggle 显示状态，不触发 onValueChanged（避免重复写回 ViewModel / 播放点击音）
        /// </summary>
        private void SyncAutoToggleWithoutNotify(bool isOn)
        {
            var toggle = mAutoToggle ?? mOldPanel?.AutoToggle;
            if (toggle == null || toggle.isOn == isOn)
            {
                return;
            }

            if (_autoToggleHandler != null)
            {
                toggle.onValueChanged.RemoveListener(_autoToggleHandler);
                toggle.isOn = isOn;
                toggle.onValueChanged.AddListener(_autoToggleHandler);
                return;
            }

            toggle.isOn = isOn;
        }

        private void UnbindButtons()
        {
            if (_pauseBtnHandler != null && mOldPanel?.PausedBtn != null)
            {
                mOldPanel.PausedBtn.onClick.RemoveListener(_pauseBtnHandler);
                _pauseBtnHandler = null;
            }

            if (_playBtnHandler != null && mOldPanel?.PlayBtn != null)
            {
                mOldPanel.PlayBtn.onClick.RemoveListener(_playBtnHandler);
                _playBtnHandler = null;
            }

            if (_speedToggleBtnHandler != null && mOldPanel?.SpeedUpBtn != null)
            {
                mOldPanel.SpeedUpBtn.onClick.RemoveListener(_speedToggleBtnHandler);
                _speedToggleBtnHandler = null;
            }

            if (_autoToggleHandler != null)
            {
                var toggleForUnbind = mAutoToggle ?? mOldPanel?.AutoToggle;
                if (toggleForUnbind != null)
                {
                    toggleForUnbind.onValueChanged.RemoveListener(_autoToggleHandler);
                }
                _autoToggleHandler = null;
            }
        }

        private void UpdateButtonStates(BattleTimeViewModel vm)
        {
            // 倍速切换按钮始终可点击（循环逻辑由 ViewModel 处理）
        }

        private void PlayClickSound()
        {
            var audioSource = UIRoot.Instance?.transform.Find("Audio Source")?.GetComponent<AudioSource>();
            if (audioSource != null)
                audioSource.Play();
        }

        #endregion

        protected override void OnOpen(IUIData uiData = null)
        {
            // 子界面显示时的逻辑
            gameObject.SetActive(true);
            ApplyAutoReleaseUiState();
        }

        protected override void OnShow()
        {
            gameObject.SetActive(true);
            ApplyAutoReleaseUiState();
        }

        private void ApplyAutoReleaseUiState()
        {
            if (VM == null)
            {
                return;
            }

            SyncAutoToggleWithoutNotify(VM.IsAutoReleaseSkill.Value);
            UpdateTween();
        }

        protected override void OnHide()
        {
            gameObject.SetActive(false);
        }

        protected override void OnClose()
        {
            base.OnClose();
        }
    }
}
