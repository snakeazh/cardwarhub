using Config;

namespace FusionContinent.Model
{
    public static class ModuleFunction
    {
        public static int GetUnlockLv(ModuleFunctionType type)
        {
            var cfg = DataConfigManager.Tables.TbModuleFunctionConfig.DataList.Find(ele=>ele.ModuleType==type);
            if (cfg == null) return 0;
            return cfg.UnlockUserLv;
        }
        public static bool CheckOpen(ModuleFunctionType type, bool isShowTip = true)
        {
            var cfg = DataConfigManager.Tables.TbModuleFunctionConfig.DataList.Find(ele=>ele.ModuleType==type);
            if (cfg == null) return true;

            if (cfg.Status == 0 || cfg.UnlockUserLv > Global.User.Level)
            {
                if (isShowTip)
                {
                    if (GameMainPanel.Default != null)
                    {
                        FusionContinent.Toast.Show(cfg.Desc);
                    }
                    else if (GameLandUI.Default != null)
                    {
                        FusionContinent.Toast.Show(cfg.Desc);
                    }
                }
                return false;
            }

            return true;
        }
        
        
    }
}