using UIFramework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.Battle
{
    /// <summary>
    /// 游戏时间显示组件
    /// 显示游戏时间、控制倍速
    /// </summary>
    public class GameTimeView : MonoBehaviour
    {
        [Header("UI组件")]
        [SerializeField] private TMP_Text mTimeText;
        [SerializeField] private TMP_Text mSpeedText;
        [SerializeField] private Button mPauseBtn;
        [SerializeField] private Button mPlayBtn;
        [SerializeField] private Button mSpeedUpBtn;
        [SerializeField] private Button mSpeedDownBtn;
        [SerializeField] private Button mResetSpeedBtn;

        private GameTimeViewModel mViewModel;
        private bool mIsInitialized = false;

        /// <summary>
        /// 初始化
        /// </summary>
        public void Initialize(GameTimeViewModel viewModel)
        {
            if (mIsInitialized) return;
            
            mViewModel = viewModel;
            
            // 绑定数据
            BindProperties();
            
            // 绑定按钮
            BindButtons();
            
            mIsInitialized = true;
        }

        /// <summary>
        /// 清理
        /// </summary>
        public void Cleanup()
        {
            if (!mIsInitialized) return;
            
            // 解绑按钮
            if (mPauseBtn != null) mPauseBtn.onClick.RemoveAllListeners();
            if (mPlayBtn != null) mPlayBtn.onClick.RemoveAllListeners();
            if (mSpeedUpBtn != null) mSpeedUpBtn.onClick.RemoveAllListeners();
            if (mSpeedDownBtn != null) mSpeedDownBtn.onClick.RemoveAllListeners();
            if (mResetSpeedBtn != null) mResetSpeedBtn.onClick.RemoveAllListeners();
            
            mIsInitialized = false;
        }

        /// <summary>
        /// 每帧更新
        /// </summary>
        public void Update()
        {
            if (mViewModel != null)
            {
                mViewModel.UpdateTime(Time.deltaTime);
            }
        }

        #region 数据绑定

        private void BindProperties()
        {
            if (mViewModel == null) return;

            // 时间显示
            mViewModel.FormattedTime.OnValueChanged += (oldVal, newVal) =>
            {
                if (mTimeText != null)
                    mTimeText.text = newVal;
            };

            // 倍速显示
            mViewModel.SpeedLevel.OnValueChanged += (oldVal, newVal) =>
            {
                if (mSpeedText != null)
                {
                    float multiplier = mViewModel.GetSpeedMultiplier();
                    mSpeedText.text = $"x{multiplier}";
                }
                
                // 更新按钮状态
                UpdateButtonStates();
            };

            // 暂停状态
            mViewModel.IsPaused.OnValueChanged += (oldVal, newVal) =>
            {
                if (mPauseBtn != null) mPauseBtn.gameObject.SetActive(!newVal);
                if (mPlayBtn != null) mPlayBtn.gameObject.SetActive(newVal);
            };

            // 初始化显示
            if (mTimeText != null)
                mTimeText.text = mViewModel.FormattedTime.Value;
            if (mSpeedText != null)
                mSpeedText.text = $"x{mViewModel.GetSpeedMultiplier()}";
        }

        #endregion

        #region 按钮绑定

        private void BindButtons()
        {
            if (mViewModel == null) return;

            if (mPauseBtn != null)
            {
                mPauseBtn.onClick.AddListener(() =>
                {
                    PlayClickSound();
                    mViewModel.OnPauseCommand.Execute();
                });
            }

            if (mPlayBtn != null)
            {
                mPlayBtn.onClick.AddListener(() =>
                {
                    PlayClickSound();
                    mViewModel.OnResumeCommand.Execute();
                });
            }

            if (mSpeedUpBtn != null)
            {
                mSpeedUpBtn.onClick.AddListener(() =>
                {
                    PlayClickSound();
                    mViewModel.OnSpeedUpCommand.Execute();
                });
            }

            if (mSpeedDownBtn != null)
            {
                mSpeedDownBtn.onClick.AddListener(() =>
                {
                    PlayClickSound();
                    mViewModel.OnSpeedDownCommand.Execute();
                });
            }

            if (mResetSpeedBtn != null)
            {
                mResetSpeedBtn.onClick.AddListener(() =>
                {
                    PlayClickSound();
                    mViewModel.OnResetSpeedCommand.Execute();
                });
            }

            // 初始化按钮状态
            UpdateButtonStates();
        }

        /// <summary>
        /// 更新按钮状态
        /// </summary>
        private void UpdateButtonStates()
        {
            if (mViewModel == null) return;

            int speedLevel = mViewModel.SpeedLevel.Value;
            
            if (mSpeedUpBtn != null)
                mSpeedUpBtn.interactable = speedLevel < 3;
            
            if (mSpeedDownBtn != null)
                mSpeedDownBtn.interactable = speedLevel > 0;
        }

        /// <summary>
        /// 播放点击音效
        /// </summary>
        private void PlayClickSound()
        {
            var audioSource = UIRoot.Instance?.transform.Find("Audio Source")?.GetComponent<AudioSource>();
            if (audioSource != null)
                audioSource.Play();
        }

        #endregion
    }
}
