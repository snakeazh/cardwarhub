using UIFramework.Services;
using Cysharp.Threading.Tasks;
using FusionContinent;
using UnityEngine;

namespace Game.Battle
{
    /// <summary>
    /// 战斗时间管理服务
    /// 全局管理战斗时间、倍速控制
    /// </summary>
    public class BattleTimeService : GameServiceBase
    {
        #region 属性
        /// <summary>
        /// 当前游戏时间（秒）
        /// </summary>
        public float CurrentTime { get; private set; }

        /// <summary>
        /// 当前倍速
        /// </summary>
        public int CurrentSpeed { get; private set; } = 1;

        /// <summary>
        /// 是否暂停
        /// </summary>
        public bool IsPaused { get; private set; }

        /// <summary>
        /// 倍速等级
        /// </summary>
        public int SpeedLevel { get; private set; }

        /// <summary>
        /// 是否正在运行
        /// </summary>
        public bool IsRunning { get; private set; }
        #endregion

        #region 常量
        private readonly int[] SPEED_MULTIPLIERS = { 1, 2, 3, 4 };
        private const int MAX_SPEED_LEVEL = 3;
        #endregion

        #region 事件
        public event System.Action<float> OnTimeUpdated;
        public event System.Action<float> OnSpeedChanged;
        public event System.Action OnPaused;
        public event System.Action OnResumed;
        public event System.Action OnReset;
        #endregion

        public override async UniTask InitializeAsync()
        {
            await base.InitializeAsync();
            LogTool.Log("[BattleTimeService] 初始化完成");
        }

        #region 公共方法

        /// <summary>
        /// 开始计时
        /// </summary>
        public void StartTimer()
        {
            IsRunning = true;
            IsPaused = false;
            LogTool.Log("[BattleTimeService] 开始计时");
        }

        /// <summary>
        /// 停止计时
        /// </summary>
        public void StopTimer()
        {
            IsRunning = false;
            LogTool.Log("[BattleTimeService] 停止计时");
        }

        /// <summary>
        /// 暂停
        /// </summary>
        public void Pause()
        {
            IsPaused = true;
            OnPaused?.Invoke();
            LogTool.Log("[BattleTimeService] 暂停");
        }

        /// <summary>
        /// 恢复
        /// </summary>
        public void Resume()
        {
            IsPaused = false;
            OnResumed?.Invoke();
            LogTool.Log("[BattleTimeService] 恢复");
        }

        /// <summary>
        /// 更新计时
        /// </summary>
        public void UpdateTime(float deltaTime)
        {
            if (!IsRunning || IsPaused) return;

            CurrentTime += deltaTime * CurrentSpeed;
            OnTimeUpdated?.Invoke(CurrentTime);
        }

        /// <summary>
        /// 加速
        /// </summary>
        public void SpeedUp()
        {
            if (SpeedLevel < MAX_SPEED_LEVEL)
            {
                SpeedLevel++;
                UpdateSpeed();
            }
        }

        /// <summary>
        /// 减速
        /// </summary>
        public void SpeedDown()
        {
            if (SpeedLevel > 0)
            {
                SpeedLevel--;
                UpdateSpeed();
            }
        }

        /// <summary>
        /// 重置倍速
        /// </summary>
        public void ResetSpeed()
        {
            SpeedLevel = 0;
            UpdateSpeed();
        }

        /// <summary>
        /// 重置所有
        /// </summary>
        public void Reset()
        {
            CurrentTime = 0f;
            SpeedLevel = 0;
            CurrentSpeed = 1;
            IsPaused = false;
            IsRunning = false;
            OnReset?.Invoke();
            LogTool.Log("[BattleTimeService] 重置完成");
        }

        /// <summary>
        /// 获取格式化时间
        /// </summary>
        public string GetFormattedTime()
        {
            int minutes = Mathf.FloorToInt(CurrentTime / 60);
            int seconds = Mathf.FloorToInt(CurrentTime % 60);
            return string.Format("{0:D2}:{1:D2}", minutes, seconds);
        }

        /// <summary>
        /// 检查是否可以加速
        /// </summary>
        public bool CanSpeedUp()
        {
            return SpeedLevel < MAX_SPEED_LEVEL;
        }

        /// <summary>
        /// 检查是否可以减速
        /// </summary>
        public bool CanSpeedDown()
        {
            return SpeedLevel > 0;
        }

        #endregion

        #region 私有方法

        private void UpdateSpeed()
        {
            CurrentSpeed = SPEED_MULTIPLIERS[SpeedLevel];
            Global.SpeedMultiplier.Value = CurrentSpeed;
            OnSpeedChanged?.Invoke(CurrentSpeed);
            LogTool.Log($"[BattleTimeService] 倍速变更: x{CurrentSpeed}");
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            
            StopTimer();
            Reset();
            
            // 清理事件监听
            OnTimeUpdated = null;
            OnSpeedChanged = null;
            OnPaused = null;
            OnResumed = null;
            OnReset = null;
            
            LogTool.Log("[BattleTimeService] 已销毁清理");
        }

        #endregion
    }
}
