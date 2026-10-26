using System;

namespace FusionContinent.Utils
{
    public class TimeTool
    {
        /// <summary>
        /// 格式化秒数为 年-月-天-时-分-秒 字符串（按 1年=365天, 1月=30天 固定换算）
        /// </summary>
        public static string FormatDurationWithYm(int totalSeconds)
        {
            if (totalSeconds < 0) totalSeconds = 0;

            int years = totalSeconds / (365 * 86400);
            int months = (totalSeconds % (365 * 86400)) / (30 * 86400);
            int days = (totalSeconds % (30 * 86400)) / 86400;
            int hours = (totalSeconds % 86400) / 3600;
            int minutes = (totalSeconds % 3600) / 60;
            int seconds = totalSeconds % 60;

            string result = "";
            if (years > 0) result += $"{years}年 ";
            if (months > 0) result += $"{months}月 ";
            if (days > 0) result += $"{days}天 ";
            if (hours > 0) result += $"{hours}小时 ";
            if (minutes > 0) result += $"{minutes}分 ";
            if (seconds > 0 || result == "") result += $"{seconds}秒";

            return result.Trim();
        }
        
        /// <summary>
        /// 根据起止时间，计算 年-月-天-时-分-秒 的差值
        /// </summary>
        public static string FormatDuration(DateTime start, DateTime end)
        {
            if (end < start) (start, end) = (end, start);

            int years = end.Year - start.Year;
            int months = end.Month - start.Month;
            int days = end.Day - start.Day;
            int hours = end.Hour - start.Hour;
            int minutes = end.Minute - start.Minute;
            int seconds = end.Second - start.Second;

            // 借位修正
            if (seconds < 0) { seconds += 60; minutes--; }
            if (minutes < 0) { minutes += 60; hours--; }
            if (hours < 0) { hours += 24; days--; }
            if (days < 0)
            {
                var prevMonth = end.AddMonths(-1);
                days += DateTime.DaysInMonth(prevMonth.Year, prevMonth.Month);
                months--;
            }
            if (months < 0) { months += 12; years--; }

            return $"{years}年 {months}月 {days}天 {hours}小时 {minutes}分 {seconds}秒";
        }
        
        /// <summary>
        /// 格式化秒数为 年-月-天-时-分-秒 字符串名字
        /// </summary>
        public static string FormatDurationWithName(int totalSeconds)
        {
            if (totalSeconds < 0) totalSeconds = 0;

            int years = totalSeconds / (365 * 86400);
            int months = (totalSeconds % (365 * 86400)) / (30 * 86400);
            int days = (totalSeconds % (30 * 86400)) / 86400;
            int hours = (totalSeconds % 86400) / 3600;
            int minutes = (totalSeconds % 3600) / 60;
            int seconds = totalSeconds % 60;

            string result = "";
            if (years > 0) return "年";
            if (months > 0) return "月";
            if (days > 0) return "天";
            if (hours > 0) return "小时";
            if (minutes > 0) return "分钟";
            if (seconds > 0 || result == "") return "秒";
            return result;
        }

        // 本周最后一刻（周日 23:59:59）
        public static DateTime GetLastWeekTime(DateTime now)
        {
            // 计算本周星期一（周起始）
            int diff = (int)now.DayOfWeek - (int)DayOfWeek.Monday;
            if (diff < 0) diff += 7;
            DateTime monday = now.Date.AddDays(-diff);

            // 本周最后时间 = 下周一 0 点 - 1 秒
            return monday.AddDays(7).AddSeconds(-1);
        }
    }
}