using Cardwar;
using Config;

namespace FusionContinent.LocalServer
{
    /// <summary>
    /// 功能模块 Handler，逐行对照 Server/app/api/application/moduleapplication/application_module.go。
    /// 存档读写对照 Server/app/model/modlueModel/PlayerModuleModel.go（内存 Archive.Modules 替代 MySQL）。
    /// </summary>
    public class ModuleHandler
    {
        // 对照 application_module.go:10-29 SaveModuleStatus
        public SaveModuleStatusResponse SaveStatus(LocalServerContext ctx, SaveModuleStatusRequest req)
        {
            // 对照 :11-15
            ModuleFunctionConfig moduleCfg = DataConfigManager.Tables.TbModuleFunctionConfig.GetOrDefault(req.ModuleId);
            if (moduleCfg == null)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 模块配置不存在");
                throw new LocalServerException("模块配置不存在");
            }

            // 对照 :17-22 GetModuleById：查不到返回零值行（Id=0），补齐字段后待 Save 插入
            PlayerModule module = FindModule(ctx, req.ModuleId);
            if (module == null)
            {
                module = new PlayerModule
                {
                    Id = NextModuleId(ctx),
                    UserId = ctx.UserId,
                    ModuleId = req.ModuleId,
                };
                ctx.Store.Archive.Modules.Add(module);
            }

            // 对照 :24-26 状态切换（proto 无 UpdateTime/CreateTime 字段，不迁移）
            module.Status = req.Status;
            ctx.Store.MarkDirty(PlayerArchive.ModulesFieldNumber);

            return new SaveModuleStatusResponse { Module = module };
        }

        // 对照 ModuleModel.GetModuleById：查不到返回 null（Go 返回零值 struct，调用方按 Id==0 处理）
        private PlayerModule FindModule(LocalServerContext ctx, int moduleId)
        {
            foreach (PlayerModule module in ctx.Store.Archive.Modules)
            {
                if (module.ModuleId == moduleId)
                {
                    return module;
                }
            }
            return null;
        }

        // proto Id（原 MySQL 自增主键）在集合内递增分配，同 NewPlayerInitializer 口径
        private int NextModuleId(LocalServerContext ctx)
        {
            int maxId = 0;
            foreach (PlayerModule module in ctx.Store.Archive.Modules)
            {
                if (module.Id > maxId)
                {
                    maxId = module.Id;
                }
            }
            return maxId + 1;
        }
    }
}
