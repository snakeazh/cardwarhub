using System.Collections.Generic;
using Cardwar;
using Config;

namespace FusionContinent.LocalServer
{
    /// <summary>
    /// 天赋模块 Handler，逐行对照 Server/app/api/application/talentapplication/application_talent.go。
    /// 存档读写对照 Server/app/model/TalentModel/PlayerTalentModel.go（内存 Archive.Talents 替代 MySQL）。
    /// 业务校验失败抛 LocalServerException，消息逐字复刻 Go panic 文案。
    /// </summary>
    public class TalentHandler
    {
        // 对照 PlayerTalentModel.go:32-36
        private const int StatusNotUse = 0;
        private const int StatusInUse = 1;

        // 对照 application_talent.go:21-43
        public UseTalentResponse UseTalent(LocalServerContext ctx, UseTalentRequest req)
        {
            var talentCfg = DataConfigManager.Tables.TbTalentConfig.GetOrDefault(req.Id);
            if (talentCfg == null)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 天赋[{req.Id}]，配置不存在");
                throw new LocalServerException($"天赋[{req.Id}]，配置不存在");
            }

            if (req.Status != StatusInUse && req.Status != StatusNotUse)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 天赋参数错误 status={req.Status}");
                throw new LocalServerException("参数错误");
            }

            PlayerTalent talent = GetByTalentId(ctx, req.Id);

            if (talent == null)
            {
                // 对照 GetByTalentId 零值语义：未初始化天赋 status=0，仅当目标状态不同才落库（Go Save 插入新行）
                if (req.Status != StatusNotUse)
                {
                    talent = CreateTalent(ctx, req.Id);
                    ResetTalentStatus(ctx);
                    talent.Status = req.Status;
                    ctx.Store.Archive.Talents.Add(talent);
                    ctx.Store.MarkDirty(PlayerArchive.TalentsFieldNumber);
                }
            }
            else if (talent.Status != req.Status)
            {
                ResetTalentStatus(ctx);
                talent.Status = req.Status;
                ctx.Store.MarkDirty(PlayerArchive.TalentsFieldNumber);
            }

            // 返回全量天赋列表
            var resp = new UseTalentResponse();
            resp.Talents.AddRange(ctx.Store.Archive.Talents);
            return resp;
        }

        // 对照 application_talent.go:54-124
        public UpGradeTalentResponse UpGradeTalent(LocalServerContext ctx, UpGradeTalentRequest req)
        {
            var talentCfg = DataConfigManager.Tables.TbTalentConfig.GetOrDefault(req.Id);
            if (talentCfg == null)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 天赋[{req.Id}]，配置不存在");
                throw new LocalServerException($"天赋[{req.Id}]，配置不存在");
            }

            // 对照 GetByTalentId 零值语义：查不到即未初始化，先建内存对象，道具扣减成功后才入档
            PlayerTalent talent = GetByTalentId(ctx, req.Id);
            bool isNew = talent == null;
            if (isNew)
            {
                talent = CreateTalent(ctx, req.Id);
            }

            Dictionary<int, long> items;
            int newLv = talent.Lv;
            int newExtraLv = talent.ExtraLv;
            if (req.UpgradeExtra > 0)
            {
                // 对照 application_talent.go:64-85（特殊升级分支）
                if (talent.ExtraLv >= talentCfg.TalentList.Count)
                {
                    LogTool.LogWarning($"userId={ctx.UserId} 天赋已满级 talentId={req.Id} extraLv={talent.ExtraLv}");
                    throw new LocalServerException("已满级");
                }

                TalentListStruct lvCfg = null;
                foreach (var cfg in talentCfg.TalentList)
                {
                    if (cfg.Level > talent.ExtraLv && cfg.ExtraConsumeItems.Count > 0)
                    {
                        lvCfg = cfg;
                        break;
                    }
                }
                // Go 此处 lvCfg 为 nil 时空指针 panic（配置瑕疵），本地同样 fail fast 不兜底

                Dictionary<int, long> consumeItems = ctx.ItemEntrance.ConsumeToItems(lvCfg.ExtraConsumeItems);
                if (!ctx.ItemEntrance.CheckEnough(consumeItems))
                {
                    LogTool.LogWarning($"userId={ctx.UserId} 天赋升级道具不足 talentId={req.Id}");
                    throw new LocalServerException("道具不足");
                }

                items = consumeItems;
                // ExtraLv 跳级赋值为该档 Level（非 +1）
                newExtraLv = lvCfg.Level;
            }
            else
            {
                // 对照 application_talent.go:86-106（普通升级分支）
                if (talent.Lv >= talentCfg.TalentList.Count)
                {
                    LogTool.LogWarning($"userId={ctx.UserId} 天赋已满级 talentId={req.Id} lv={talent.Lv}");
                    throw new LocalServerException("已满级");
                }

                TalentListStruct lvCfg = talentCfg.TalentList[talent.Lv];
                if (lvCfg.UnlockLv > ctx.UserLv)
                {
                    LogTool.LogWarning($"userId={ctx.UserId} 天赋未解锁 talentId={req.Id} unlockLv={lvCfg.UnlockLv}");
                    throw new LocalServerException("未解锁");
                }

                Dictionary<int, long> consumeItems = ctx.ItemEntrance.ConsumeToItems(lvCfg.ConsumeItems);
                if (!ctx.ItemEntrance.CheckEnough(consumeItems))
                {
                    LogTool.LogWarning($"userId={ctx.UserId} 天赋升级道具不足 talentId={req.Id}");
                    throw new LocalServerException("道具不足");
                }

                items = consumeItems;
                newLv = talent.Lv + 1;
            }

            // 对照 application_talent.go:108-118：Go 在事务内扣道具、天赋 Save 在事务外（一致性瑕疵）。
            // Go 的 err!=nil 分支实际不可达（业务失败是 panic，gorm 回滚后 re-panic 原文案），
            // 单机版直接让 ItemEntrance 的 LocalServerException 原文案上抛，与线上真实行为一致；
            // 新值先存局部变量，Execute 成功后才写入 archive 对象，等价于事务回滚
            ResultItems rsItems = ctx.ItemEntrance.Execute(items);

            talent.Lv = newLv;
            talent.ExtraLv = newExtraLv;
            if (isNew)
            {
                ctx.Store.Archive.Talents.Add(talent);
            }
            ctx.Store.MarkDirty(PlayerArchive.TalentsFieldNumber);

            long cp = ctx.CombatPower.RefreshModule(CombatPowerRefresher.ModuleTalent);
            if (rsItems != null)
            {
                rsItems.CombatPower = new CombatPower { CombatPower_ = cp };
            }

            return new UpGradeTalentResponse { Talent = talent, Items = rsItems };
        }

        // 对照 PlayerTalentModel.go:80-88：查不到返回 null（Go 返回零值 struct，调用方按未初始化处理）
        private PlayerTalent GetByTalentId(LocalServerContext ctx, int talentId)
        {
            foreach (PlayerTalent t in ctx.Store.Archive.Talents)
            {
                if (t.TalentId == talentId)
                {
                    return t;
                }
            }
            return null;
        }

        // 懒新增：Id 为原自增主键，取当前集合最大 Id+1（从 1 开始）。
        // 注意 Go 零值 struct 直接 Save 会插入 talent_id=0 的脏数据（Go bug），此处按意图补上 TalentId/UserId。
        private PlayerTalent CreateTalent(LocalServerContext ctx, int talentId)
        {
            int maxId = 0;
            foreach (PlayerTalent t in ctx.Store.Archive.Talents)
            {
                if (t.Id > maxId)
                {
                    maxId = t.Id;
                }
            }
            return new PlayerTalent
            {
                Id = maxId + 1,
                UserId = ctx.UserId,
                TalentId = talentId,
                Status = StatusNotUse,
            };
        }

        // 对照 PlayerTalentModel.go:97-102：该玩家全部使用中的天赋置为未使用
        private void ResetTalentStatus(LocalServerContext ctx)
        {
            foreach (PlayerTalent t in ctx.Store.Archive.Talents)
            {
                if (t.Status == StatusInUse)
                {
                    t.Status = StatusNotUse;
                }
            }
        }
    }
}
