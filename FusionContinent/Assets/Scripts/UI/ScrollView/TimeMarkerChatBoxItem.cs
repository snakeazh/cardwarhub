using FusionContinent.Model;

namespace ScrollView
{
    public class TimeMarkerChatBoxItem : VariableHeightScrollItem
    {
        private const long MillisecondsTimestampThreshold = 100000000000L;
        private const float TimeMarkerExtraHeight = 24f;
        private const float TimeMarkerTargetExtraHeight = 20f;
        private const float TimeMarkerTargetExtraWidth = 40f;
        private const float TimeMarkerMinHeight = 64f;

        protected override void ApplyChatPush(ChatPushModel model)
        {
            // SetPlayerInfo(model, false);
            if (MeasureText != null)
            {
                MeasureText.text = GetMeasureContent(model);
            }
        }

        protected override string GetMeasureContent(ChatPushModel model)
        {
            if (model == null)
            {
                return string.Empty;
            }

            return FormatTimestamp(model.Timestamp);
        }

        public override void SetMeasureTextAndFit(string value)
        {
            ExtraHeight = TimeMarkerExtraHeight;
            MeasureTargetExtraHeight = TimeMarkerTargetExtraHeight;
            MeasureTargetExtraWidth = TimeMarkerTargetExtraWidth;
            TweenTargetMinHeight = TimeMarkerMinHeight;
            base.SetMeasureTextAndFit(value);
        }

        private static string FormatTimestamp(long timestamp)
        {
            var timestampSeconds = NormalizeTimestampToSeconds(timestamp);
            if (timestampSeconds <= 0)
            {
                return string.Empty;
            }

            var dateTime = System.DateTimeOffset.FromUnixTimeSeconds(timestampSeconds).ToLocalTime().DateTime;
            return dateTime.Date == System.DateTime.Now.Date
                ? dateTime.ToString("HH:mm")
                : dateTime.ToString("MM/dd HH:mm");
        }

        private static long NormalizeTimestampToSeconds(long timestamp)
        {
            return timestamp >= MillisecondsTimestampThreshold ? timestamp / 1000 : timestamp;
        }
    }
}
