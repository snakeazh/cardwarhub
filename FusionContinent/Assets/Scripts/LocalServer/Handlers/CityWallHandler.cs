using System.Collections.Generic;
using Cardwar;
using Config;

namespace FusionContinent.LocalServer
{
    /// <summary>
    /// 城墙 Handler，逐行对照 Server/app/api/application/citywallapplication/application_city_wall.go。
    /// 存档读写对照 Server/app/model/CityWallModel/（wall/skills/strategy 三张表 → 本地聚合为 Archive.CityWall 单块；
    /// 策略固定 strategy_id=1，其 SlotData 即 Archive.CityWall.SlotSkills）。
    /// </summary>
    public class CityWallHandler
    {
        // 对照 application_city_wall.go:29-69 UpgradeCityWall
        public CityWallUpgradeResponse UpgradeCityWall(LocalServerContext ctx, CityWallUpgradeRequest req)
        {
            // 对照 CityWallService.GetWall（零值行 Id==0 → 城墙数据不存在）
            PlayerCityWallArchive archive = EnsureCityWallArchive(ctx);
            if (archive.Wall == null)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 城墙数据不存在");
                throw new LocalServerException("城墙数据不存在");
            }
            PlayerCityWall wall = archive.Wall;

            int nextLevel = wall.Level + 1;
            HeroSeatLevelConfig nextLevelCfg = DataConfigManager.Tables.TbHeroSeatLevelConfig.GetOrDefault(nextLevel);
            if (nextLevelCfg == null)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 城墙已满级");
                throw new LocalServerException("城墙已满级");
            }
            // 当前等级配置（:43）；Go 端 Get 查不到为 nil，后续 .NeedItems 解引用 panic，本地 Get 缺 key 直接抛（fail fast）
            HeroSeatLevelConfig levelConfig = DataConfigManager.Tables.TbHeroSeatLevelConfig.Get(wall.Level);

            Dictionary<int, long> consumeItems = ctx.ItemEntrance.ConsumeToItems(levelConfig.NeedItems);
            if (!ctx.ItemEntrance.CheckEnough(consumeItems))
            {
                LogTool.LogWarning($"userId={ctx.UserId} 道具不足");
                throw new LocalServerException("道具不足");
            }

            // 对照 :52-61（ResourceLockService 行锁不迁移）
            ResultItems rsItems = ctx.ItemEntrance.Execute(consumeItems);
            wall.Level = nextLevel;
            ctx.Store.MarkDirty(PlayerArchive.CityWallFieldNumber);

            // 城墙升级不直接影响战力（战力取决于槽位中已放置的技能）（:67）

            // 对照 city_wall_converter.go:11-16
            return new CityWallUpgradeResponse { CityWall = wall, Items = rsItems };
        }

        // 对照 application_city_wall.go:95-141 ActivateCityWallSkill
        public CityWallSkillActivateResponse ActivateCityWallSkill(LocalServerContext ctx, CityWallSkillActivateRequest req)
        {
            HeroSeatConfig cfg = DataConfigManager.Tables.TbHeroSeatConfig.GetOrDefault(req.SkillId);
            if (cfg == null)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 城墙技能[{req.SkillId}]配置不存在");
                throw new LocalServerException("城墙技能配置不存在");
            }

            PlayerCityWallArchive archive = EnsureCityWallArchive(ctx);
            if (GetSkill(archive, req.SkillId) != null)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 城墙技能[{req.SkillId}]已激活");
                throw new LocalServerException("城墙技能已激活");
            }

            Dictionary<int, long> consumeItems = ctx.ItemEntrance.ConsumeToItems(cfg.NeedItems);
            if (!ctx.ItemEntrance.CheckEnough(consumeItems))
            {
                LogTool.LogWarning($"userId={ctx.UserId} 道具不足");
                throw new LocalServerException("道具不足");
            }

            // 对照 :115-131
            ResultItems rsItems = ctx.ItemEntrance.Execute(consumeItems);
            var skill = new PlayerCityWallSkill
            {
                SkillId = req.SkillId,
                Level = 1,
            };
            archive.Skills.Add(skill);
            ctx.Store.MarkDirty(PlayerArchive.CityWallFieldNumber);

            // 激活技能就需要重新计算战力，因为槽位中可能放置了该技能（:137-139）
            long combatPower = ctx.CombatPower.RefreshModule(CombatPowerRefresher.ModuleHeroSeat);
            rsItems.CombatPower = new CombatPower { CombatPower_ = combatPower };

            // 对照 city_wall_converter.go:18-23
            return new CityWallSkillActivateResponse { Skill = skill, Items = rsItems };
        }

        // 对照 application_city_wall.go:152-197 UpgradeCityWallSkill
        public CityWallSkillUpgradeResponse UpgradeCityWallSkill(LocalServerContext ctx, CityWallSkillUpgradeRequest req)
        {
            PlayerCityWallArchive archive = EnsureCityWallArchive(ctx);
            PlayerCityWallSkill skill = GetSkill(archive, req.SkillId);
            if (skill == null)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 城墙技能[{req.SkillId}]未激活");
                throw new LocalServerException("城墙技能未激活");
            }

            HeroSeatConfig cfg = DataConfigManager.Tables.TbHeroSeatConfig.GetOrDefault(req.SkillId);
            if (cfg == null)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 城墙技能[{req.SkillId}]配置不存在");
                throw new LocalServerException("城墙技能配置不存在");
            }

            if (skill.Level >= cfg.SeatLevelList.Count)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 城墙技能[{req.SkillId}]已满级");
                throw new LocalServerException("城墙技能已满级");
            }

            // 对照 :170：取当前等级档（列表按等级升序，Level 从 1 开始）
            SeatLevelListStruct lvCfg = cfg.SeatLevelList[skill.Level - 1];
            Dictionary<int, long> consumeItems = ctx.ItemEntrance.ConsumeToItems(lvCfg.LevelUpItems);
            if (!ctx.ItemEntrance.CheckEnough(consumeItems))
            {
                LogTool.LogWarning($"userId={ctx.UserId} 道具不足");
                throw new LocalServerException("道具不足");
            }

            // 对照 :178-187
            ResultItems rsItems = ctx.ItemEntrance.Execute(consumeItems);
            skill.Level++;
            ctx.Store.MarkDirty(PlayerArchive.CityWallFieldNumber);

            long combatPower = ctx.CombatPower.RefreshModule(CombatPowerRefresher.ModuleHeroSeat);
            rsItems.CombatPower = new CombatPower { CombatPower_ = combatPower };

            // 对照 city_wall_converter.go:25-30
            return new CityWallSkillUpgradeResponse { Skill = skill, Items = rsItems };
        }

        // 对照 application_city_wall.go:203-262 SaveSlots（固定 strategy_id=1）
        public CityWallSaveResponse SaveCityWall(LocalServerContext ctx, CityWallSaveRequest req)
        {
            PlayerCityWallArchive archive = EnsureCityWallArchive(ctx);
            if (archive.Wall == null)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 城墙数据不存在");
                throw new LocalServerException("城墙数据不存在");
            }
            PlayerCityWall wall = archive.Wall;

            // 验证数量需要剔除0的（:211-213）
            int needCheckCount = 0;
            foreach (int skId in req.SlotSkills)
            {
                if (skId != 0)
                {
                    needCheckCount++;
                }
            }

            int unlockedSlots = GetUnlockedSlotCount(wall.Level);
            if (needCheckCount > unlockedSlots)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 槽位数超出上限 unlocked={unlockedSlots} requested={needCheckCount}");
                throw new LocalServerException("槽位数超出上限");
            }

            // 验证技能归属且不重复（:221-244）
            var seenSkills = new HashSet<int>();
            foreach (int skId in req.SlotSkills)
            {
                if (skId == 0)
                {
                    continue;
                }
                if (seenSkills.Contains(skId))
                {
                    LogTool.LogWarning($"userId={ctx.UserId} 城墙技能[{skId}]重复放置");
                    throw new LocalServerException("城墙技能不可重复放置");
                }
                seenSkills.Add(skId);

                if (GetSkill(archive, skId) == null)
                {
                    LogTool.LogWarning($"userId={ctx.UserId} 城墙技能[{skId}]未拥有");
                    throw new LocalServerException("城墙技能未拥有");
                }

                HeroSeatConfig cfg = DataConfigManager.Tables.TbHeroSeatConfig.GetOrDefault(skId);
                if (cfg == null)
                {
                    LogTool.LogWarning($"userId={ctx.UserId} 城墙技能[{skId}]配置不存在");
                    throw new LocalServerException("城墙技能配置不存在");
                }
            }

            // 固定操作 strategy_id=1（:246-258）：策略 SlotData → Archive.CityWall.SlotSkills
            archive.SlotSkills.Clear();
            archive.SlotSkills.AddRange(req.SlotSkills);
            ctx.Store.MarkDirty(PlayerArchive.CityWallFieldNumber);

            long combatPower = ctx.CombatPower.RefreshModule(CombatPowerRefresher.ModuleHeroSeat);

            // 对照 city_wall_converter.go:32-47：战力为 0 不下发 CombatPower
            var resp = new CityWallSaveResponse { Items = new ResultItems() };
            resp.SlotSkills.AddRange(req.SlotSkills);
            if (combatPower != 0)
            {
                resp.Items.CombatPower = new CombatPower { CombatPower_ = combatPower };
            }
            return resp;
        }

        // 对照 GetUnlockedSlotCount（application_city_wall.go:75-84）：累计 1~wallLevel 的 UnLockSeat
        private int GetUnlockedSlotCount(int wallLevel)
        {
            int count = 0;
            for (int lv = 1; lv <= wallLevel; lv++)
            {
                HeroSeatLevelConfig cfg = DataConfigManager.Tables.TbHeroSeatLevelConfig.GetOrDefault(lv);
                if (cfg != null)
                {
                    count += cfg.UnLockSeat;
                }
            }
            return count;
        }

        // 对照 CityWallService.GetSkill（零值行 Id==0 → 本地返回 null）
        private PlayerCityWallSkill GetSkill(PlayerCityWallArchive archive, int skillId)
        {
            foreach (PlayerCityWallSkill skill in archive.Skills)
            {
                if (skill.SkillId == skillId)
                {
                    return skill;
                }
            }
            return null;
        }

        // 对照 application_player_archive.go:420-432 + CityWallModel.InitCityWallForUser（:60-81）登录懒建：
        // 老存档可能缺 CityWall 块，补建 Level=1 城墙与空策略槽位
        private PlayerCityWallArchive EnsureCityWallArchive(LocalServerContext ctx)
        {
            if (ctx.Store.Archive.CityWall == null)
            {
                ctx.Store.Archive.CityWall = new PlayerCityWallArchive { Wall = new PlayerCityWall { Level = 1 } };
                ctx.Store.MarkDirty(PlayerArchive.CityWallFieldNumber);
            }
            return ctx.Store.Archive.CityWall;
        }
    }
}
