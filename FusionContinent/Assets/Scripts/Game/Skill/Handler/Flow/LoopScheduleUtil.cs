using FusionContinent;
using FusionContinent.Skill.Config;
using UnityEngine;

namespace FusionContinent.Skill.Handler
{
    /// <summary>
    /// Loop / LoopSequential 共用的持续时间调度辅助。
    /// </summary>
    internal static class LoopScheduleUtil
    {
        internal const int MaxComboDurationScale = 6;

        /// <summary>
        /// 开启 duration_scale_by_combo 时，将 duration 乘以当前 Combo 层数（1~6）。
        /// </summary>
        internal static float ApplyDurationComboScale(SkillNodeConfig node, float duration, string logPrefix, BattleSide side = BattleSide.Player)
        {
            if (duration <= 0f || !node.GetParamBool("duration_scale_by_combo", false))
            {
                return duration;
            }

            int comboMultiplier = global::Manager.ConsecutiveSkillReleaseCounter.GetComboCount(side);
            if (comboMultiplier < 1)
            {
                comboMultiplier = 1;
            }

            if (comboMultiplier > MaxComboDurationScale)
            {
                comboMultiplier = MaxComboDurationScale;
            }

            float scaledDuration = duration * comboMultiplier;
            LogTool.Log(
                $"[{logPrefix}] duration 按 Combo 倍率缩放: base={duration}, combo=x{comboMultiplier}, final={scaledDuration}");
            return scaledDuration;
        }
    }
}
