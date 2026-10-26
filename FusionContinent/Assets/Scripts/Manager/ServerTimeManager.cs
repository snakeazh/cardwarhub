using System;
using UnityEngine;
using TMPro;
using System.Collections;
using FusionContinent.Utils;

namespace FusionContinent.Manager
{
    public class ServerTimeManager : MonoBehaviour
    {
        public static ServerTimeManager Instance { get; private set; }

        private long serverTimestamp;
        private float clientTimeAtReceive;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void OnReceiveServerTime(long serverTime)
        {
            serverTimestamp = serverTime;
            clientTimeAtReceive = Time.realtimeSinceStartup;
        }

        public long GetCurrentServerTimestamp()
        {
            float elapsed = Time.realtimeSinceStartup - clientTimeAtReceive;
            return serverTimestamp + (long)elapsed;
        }

        // 获取间隔天数：两个时间戳之间跨过几个每日重置点（凌晨 GameResetHour 点）。
        // 语义为 0 基——同一天内返回 0；时间回拨返回负数（调用方 >0 判断天然不触发）。
        public static int GetDayInterval(long timestamp1, long timestamp2)
        {
            // 设定刷新时间为凌晨 5 点
            int resetHour = DataConfigManager.Tables.TbGameConst.GameResetHour;

            // 先减去偏移小时数，再取 Date
            DateTime date1D = DateTimeOffset.FromUnixTimeSeconds(timestamp1).LocalDateTime.AddHours(-resetHour).Date;
            DateTime date2D = DateTimeOffset.FromUnixTimeSeconds(timestamp2).LocalDateTime.AddHours(-resetHour).Date;

            return (date2D - date1D).Days;
        }
        
        // 获取当前时间对象
        public DateTime GetDataTime()
        {
            var timestamp = GetCurrentServerTimestamp();
            return DateTimeOffset.FromUnixTimeSeconds(timestamp).LocalDateTime;
        }
    }

}