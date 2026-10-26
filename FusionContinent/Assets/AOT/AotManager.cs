using System.Collections.Generic;

namespace FusionContinent
{
    public static class AotManager
    {
        /// <summary>渠道信息。单机化后由 ResourceInitStep.EnsureChannelConfig 启动时从本地配置（AotConfig）填充。</summary>
        public static HotFixData HotFixData
        {
            get { return _hotFixDataValue; }
            set { _hotFixDataValue = value; }
        }

        private static HotFixData _hotFixDataValue;
    }
}