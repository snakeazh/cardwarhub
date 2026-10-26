namespace Helper
{
    using System;
    using UnityEngine;
    using UnityEngine.Events;

    public class CountdownTimer : MonoBehaviour
    {
        [Header("时间参数")]
        public DateTime StartTime;    // 开始时间
        public DateTime EndTime;      // 结束时间

        [Header("回调")]
        public Action<TimeSpan> OnTick; // 每秒回调，传剩余时间
        public UnityEvent OnFinished;       // 倒计时结束

        private bool _isRunning = false;
        private float _timer = 0f;

        /// <summary>
        /// 初始化倒计时（必须调用）
        /// </summary>
        public void Init(DateTime start, DateTime end, Action<TimeSpan> onTick = null)
        {
            StartTime = start;
            EndTime = end;
            _isRunning = true;
            if (onTick != null) OnTick = onTick;
        }

        public void StartCountdown()
        {
            _isRunning = true;
        }

        public void Pause()
        {
            _isRunning = false;
        }

        public void ResetCountdown(DateTime start, DateTime end)
        {
            StartTime = start;
            EndTime = end;
            _isRunning = true;
        }

        private void Update()
        {
            if (!_isRunning)
                return;

            // 每秒调用一次 OnTick
            _timer += Time.deltaTime;
            if (_timer >= 1f)
            {
                _timer = 0f;
                Tick();
            }
        }

        private void Tick()
        {
            DateTime now = DateTime.Now;
            if (now < StartTime)
            {
                TimeSpan waitTime = StartTime - now;
                OnTick?.Invoke(waitTime);
                return;
            }

            if (now >= EndTime)
            {
                OnFinished?.Invoke();
                _isRunning = false;
                return;
            }

            TimeSpan remain = EndTime - now;
            OnTick?.Invoke(remain);
        }
    }

}