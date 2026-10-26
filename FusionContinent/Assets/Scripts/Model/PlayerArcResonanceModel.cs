using Config;

namespace FusionContinent.Model
{
    public class PlayerArcResonance
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int ResonanceId { get; set; }
        public int Slot { get; set; }
        public int Level { get; set; }
        public int Stage { get; set; }
        public float Progress { get; set; }
        public ArcResonanceConfig Config { get; set; }

        public PlayerArcResonance()
        {
        }

        public PlayerArcResonance(PlayerArcResonance v)
        {
            Config = DataConfigManager.Tables.TbArcResonanceConfig.GetOrDefault(v.ResonanceId);
            if (Config == null)
            {
                LogTool.LogError($"初始化奥数共鸣配置不存在 ResonanceId[{v.ResonanceId}]");
                return;
            }

            Id = v.Id;
            UserId = v.UserId;
            ResonanceId = v.ResonanceId;
            Slot = v.Slot;
            Level = v.Level;
            Stage = v.Stage;
            Progress = v.Progress;
        }

        public ResonanceListStruct GetLevelCfg()
        {
            foreach (var cfg in Config.ResonanceList)
            {
                if (cfg.Level == Level && cfg.Stage == Stage)
                {
                    return cfg;
                }
            }

            return null;
        }
    }
}
