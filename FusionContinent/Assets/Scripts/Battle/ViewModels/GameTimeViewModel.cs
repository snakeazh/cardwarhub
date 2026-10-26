using FusionContinent;
using UIFramework;
using UnityEngine;

namespace Game.Battle
{
    /// <summary>
    /// 游戏时间管理 ViewModel
    /// 管理游戏时间显示、倍速控制
    /// </summary>
    public class GameTimeViewModel : ViewModel
    {
        #region 属性
        /// <summary>
        /// 游戏开始时间（秒）
        /// </summary>
        public BindableProperty<float> StartTime { get; } = new BindableProperty<float>(0f);

        /// <summary>
        /// 格式化后的时间字符串（MM:SS）
        /// </summary>
        public BindableProperty<string> FormattedTime { get; } = new BindableProperty<string>("00:00");

        /// <summary>
        /// 当前倍速等级
        /// </summary>
        public BindableProperty<int> SpeedLevel { get; } = new BindableProperty<int>(0);

        /// <summary>
        /// 是否暂停
        /// </summary>
        public BindableProperty<bool> IsPaused { get; } = new BindableProperty<bool>(false);
        #endregion

        #region 命令
        public Command OnPauseCommand { get; private set; }
        public Command OnResumeCommand { get; private set; }
        public Command OnSpeedUpCommand { get; private set; }
        public Command OnSpeedDownCommand { get; private set; }
        public Command OnResetSpeedCommand { get; private set; }
        #endregion

        #region 常量
        private readonly int[] SPEED_MULTIPLIERS = { 1, 2, 3,4 };
        private const int MAX_SPEED_LEVEL = 3;
        #endregion

        public override void OnCreate()
        {
            base.OnCreate();
            
            // 初始化命令
            OnPauseCommand = new Command(OnPause);
            OnResumeCommand = new Command(OnResume);
            OnSpeedUpCommand = new Command(OnSpeedUp);
            OnSpeedDownCommand = new Command(OnSpeedDown);
            OnResetSpeedCommand = new Command(OnResetSpeed);
            
            // 订阅时间变化，自动格式化
            StartTime.OnValueChanged += (oldVal, newVal) =>
            {
                FormattedTime.Value = FormatTime(newVal);
            };
            
            // 订阅倍速变化
            SpeedLevel.OnValueChanged += (oldVal, newVal) =>
            {
                var multiplier = GetSpeedMultiplier();
                Global.SpeedMultiplier.Value = multiplier;
                OnSpeedChanged?.Invoke(multiplier);
            };
        }

        #region 命令实现

        private void OnPause()
        {
            IsPaused.Value = true;
            OnPauseRequested?.Invoke();
        }

        private void OnResume()
        {
            IsPaused.Value = false;
            OnResumeRequested?.Invoke();
        }

        private void OnSpeedUp()
        {
            if (SpeedLevel.Value < MAX_SPEED_LEVEL)
            {
                SpeedLevel.Value++;
            }
        }

        private void OnSpeedDown()
        {
            if (SpeedLevel.Value > 0)
            {
                SpeedLevel.Value--;
            }
        }

        private void OnResetSpeed()
        {
            SpeedLevel.Value = 0;
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 更新游戏时间
        /// </summary>
        public void UpdateTime(float deltaTime)
        {
            if (!IsPaused.Value)
            {
                StartTime.Value += deltaTime * Global.SpeedMultiplier.Value;
            }
        }

        /// <summary>
        /// 获取当前倍速倍数
        /// </summary>
        public int GetSpeedMultiplier()
        {
            int level = Mathf.Clamp(SpeedLevel.Value, 0, MAX_SPEED_LEVEL);
            return SPEED_MULTIPLIERS[level];
        }

        /// <summary>
        /// 重置时间
        /// </summary>
        public void ResetTime()
        {
            StartTime.Value = 0f;
            SpeedLevel.Value = 0;
            IsPaused.Value = false;
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 将秒数转换为 MM:SS 格式
        /// </summary>
        private string FormatTime(float seconds)
        {
            int minutes = Mathf.FloorToInt(seconds / 60);
            int secs = Mathf.FloorToInt(seconds % 60);
            return string.Format("{0:D2}:{1:D2}", minutes, secs);
        }

        #endregion

        #region 事件

        public System.Action OnPauseRequested;
        public System.Action OnResumeRequested;
        public System.Action<float> OnSpeedChanged;

        #endregion
    }
}
