using System.Collections.Generic;
using Cardwar;
using Config;

namespace FusionContinent.LocalServer
{
    /// <summary>
    /// 轮语者（皮肤/武器）Handler，逐行对照：
    /// - Server/app/api/application/application_cycle_speaker.go（setSkin / skin/activate / skin/upgrade / skin/unequip）
    /// - Server/app/api/application/weaponapplication/application_weapon.go（weapon/activate / upgrade / equip / unequip）
    /// 存档读写对照 Server/app/model/cycleSpeakerModel/ 与 Server/app/model/WeaponModel/PlayerWeaponModel.go
    /// （内存 Archive.PlayerSpeakerModule / CycleSpeakerSkins / Weapons 替代 MySQL）。
    /// 业务校验失败抛 LocalServerException，消息逐字复刻 Go panic 文案；事务/资源锁（ResourceLockService）不迁移。
    /// 注意：Go 应用层 err 分支的 panic("设置皮肤失败")/panic("激活皮肤失败: ...") 等包装对业务 panic 不可达
    /// （gorm 事务对 panic 回滚后直接 re-panic），本地让内层 LocalServerException 原文案直接上抛。
    /// </summary>
    public class CycleSpeakerHandler
    {
        // ============================================================
        // 皮肤
        // ============================================================

        // 对照 application_cycle_speaker.go:17-28 CycleSpeakerSetSkinHandler
        // → CycleSpeakerService.TxSetCycleSpeakerSkin（cycle_speaker_service.go:73-96）
        public SetCycleSpeakerSkinResponse SetSkin(LocalServerContext ctx, SetCycleSpeakerSkinRequest req)
        {
            // 检查皮肤是否存在
            PlayerFigureConfig conf = DataConfigManager.Tables.TbPlayerFigureConfig.GetOrDefault(req.SkinId);
            if (conf == null)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 皮肤配置不存在");
                throw new LocalServerException("皮肤配置不存在");
            }

            // 用户是否拥有该皮肤（cycle_speaker_service.go:80-87）
            if (GetSkinById(ctx, req.SkinId) == null)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 用户皮肤不存在");
                throw new LocalServerException("用户皮肤不存在");
            }

            // 对照 :89-95：Go 的 speaker==nil 检查是死代码（Find 恒返回非 nil 零值行），
            // 本地 PlayerSpeakerModule 缺失时懒建档（proto 无 UserId 字段，单机恒为当前玩家）
            PlayerSpeakerModule speaker = GetOrCreateSpeakerModule(ctx);
            speaker.UseSkinId = req.SkinId;
            ctx.Store.MarkDirty(PlayerArchive.PlayerSpeakerModuleFieldNumber);

            // Go 响应只回 Speaker，Items 为 nil
            return new SetCycleSpeakerSkinResponse { Speaker = speaker };
        }

        // 对照 application_cycle_speaker.go:39-75 SkinActivateHandler
        public SkinActivateResponse SkinActivate(LocalServerContext ctx, SkinActivateRequest req)
        {
            PlayerFigureConfig cfg = DataConfigManager.Tables.TbPlayerFigureConfig.GetOrDefault(req.SkinId);
            if (cfg == null)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 皮肤[{req.SkinId}]配置不存在");
                throw new LocalServerException("皮肤配置不存在");
            }

            if (GetSkinById(ctx, req.SkinId) != null)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 皮肤[{req.SkinId}]已激活");
                throw new LocalServerException("皮肤已激活");
            }

            Dictionary<int, long> consumeItems = ctx.ItemEntrance.ConsumeToItems(cfg.NeedItems);
            if (!ctx.ItemEntrance.CheckEnough(consumeItems))
            {
                LogTool.LogWarning($"userId={ctx.UserId} 道具不足");
                throw new LocalServerException("道具不足");
            }

            // 对照 :59-66：事务内先扣道具、后建皮肤；Execute 抛错等价于事务回滚
            ResultItems rsItems = ctx.ItemEntrance.Execute(consumeItems);

            // 对照 cycleSpeakerModel.TxActivate（cycleSpeakerSkinModel.go:93-101）
            var skin = new PlayerCycleSpeakerSkin
            {
                Id = NextSkinId(ctx),
                SkinId = req.SkinId,
                Level = 1,
            };
            ctx.Store.Archive.CycleSpeakerSkins.Add(skin);
            ctx.Store.MarkDirty(PlayerArchive.CycleSpeakerSkinsFieldNumber);

            // 刷新轮语者皮肤战力（:71-73）
            long cp = ctx.CombatPower.RefreshModule(CombatPowerRefresher.ModuleSpeaker);
            rsItems.CombatPower = new CombatPower { CombatPower_ = cp };

            return new SkinActivateResponse { Skin = skin, Items = rsItems };
        }

        // 对照 application_cycle_speaker.go:86-128 SkinUpgradeHandler
        public SkinUpgradeResponse SkinUpgrade(LocalServerContext ctx, SkinUpgradeRequest req)
        {
            PlayerCycleSpeakerSkin skin = GetSkinById(ctx, req.SkinId);
            if (skin == null)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 皮肤[{req.SkinId}]未激活");
                throw new LocalServerException("皮肤未激活");
            }

            PlayerFigureConfig cfg = DataConfigManager.Tables.TbPlayerFigureConfig.GetOrDefault(req.SkinId);
            if (cfg == null)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 皮肤[{req.SkinId}]配置不存在");
                throw new LocalServerException("皮肤配置不存在");
            }

            if (skin.Level >= cfg.ImageStarList.Count)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 皮肤[{req.SkinId}]已满级");
                throw new LocalServerException("皮肤已满级");
            }

            Dictionary<int, long> consumeItems = ctx.ItemEntrance.ConsumeToItems(cfg.NeedItems);
            if (!ctx.ItemEntrance.CheckEnough(consumeItems))
            {
                LogTool.LogWarning($"userId={ctx.UserId} 道具不足");
                throw new LocalServerException("道具不足");
            }

            // 对照 :111-118：事务内先扣道具、后升级；Execute 抛错等价于事务回滚
            ResultItems rsItems = ctx.ItemEntrance.Execute(consumeItems);

            // 对照 cycleSpeakerModel.TxUpgrade（cycleSpeakerSkinModel.go:103-106）
            skin.Level++;
            ctx.Store.MarkDirty(PlayerArchive.CycleSpeakerSkinsFieldNumber);

            // 刷新轮语者皮肤战力（:125-126）
            long cp = ctx.CombatPower.RefreshModule(CombatPowerRefresher.ModuleSpeaker);
            rsItems.CombatPower = new CombatPower { CombatPower_ = cp };

            return new SkinUpgradeResponse { Skin = skin, Items = rsItems };
        }

        // 对照 application_cycle_speaker.go:134-160 SkinUnequipHandler
        public SkinUnequipResponse SkinUnequip(LocalServerContext ctx, SkinUnequipRequest req)
        {
            PlayerCycleSpeakerSkin skin = GetSkinById(ctx, req.SkinId);
            if (skin == null)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 皮肤[{req.SkinId}]未激活");
                throw new LocalServerException("皮肤未激活");
            }

            PlayerSpeakerModule speaker = GetOrCreateSpeakerModule(ctx);
            if (speaker.UseSkinId != req.SkinId)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 皮肤[{req.SkinId}]未佩戴");
                throw new LocalServerException("皮肤未佩戴");
            }
            speaker.UseSkinId = 0;
            ctx.Store.MarkDirty(PlayerArchive.PlayerSpeakerModuleFieldNumber);

            // Go 响应只回 Speaker，Items 为 nil
            return new SkinUnequipResponse { Speaker = speaker };
        }

        // ============================================================
        // 武器
        // ============================================================

        // 对照 weaponapplication/application_weapon.go:27-65 ActivateHandler
        public WeaponActivateResponse WeaponActivate(LocalServerContext ctx, WeaponActivateRequest req)
        {
            PlayerWeaponConfig cfg = DataConfigManager.Tables.TbPlayerWeaponConfig.GetOrDefault(req.WeaponId);
            if (cfg == null)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 武器[{req.WeaponId}]配置不存在");
                throw new LocalServerException("武器配置不存在");
            }

            if (GetWeaponById(ctx, req.WeaponId) != null)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 武器[{req.WeaponId}]已激活");
                throw new LocalServerException("武器已激活");
            }

            Dictionary<int, long> consumeItems = ctx.ItemEntrance.ConsumeToItems(cfg.NeedItems);
            if (!ctx.ItemEntrance.CheckEnough(consumeItems))
            {
                LogTool.LogWarning($"userId={ctx.UserId} 道具不足");
                throw new LocalServerException("道具不足");
            }

            // 对照 :47-53：事务内先扣道具、后建武器；Execute 抛错等价于事务回滚
            ResultItems rsItems = ctx.ItemEntrance.Execute(consumeItems);

            // 对照 WeaponModel.TxActivate（PlayerWeaponModel.go:59-67）
            var weapon = new PlayerWeapon
            {
                Id = NextWeaponId(ctx),
                WeaponId = req.WeaponId,
                Level = 1,
            };
            ctx.Store.Archive.Weapons.Add(weapon);
            ctx.Store.MarkDirty(PlayerArchive.WeaponsFieldNumber);

            // 刷新轮语者武器战力（:60-63）
            long cp = ctx.CombatPower.RefreshModule(CombatPowerRefresher.ModuleWeapon);
            rsItems.CombatPower = new CombatPower { CombatPower_ = cp };

            return new WeaponActivateResponse { Weapon = weapon, Items = rsItems };
        }

        // 对照 weaponapplication/application_weapon.go:76-120 UpgradeHandler
        public WeaponUpgradeResponse WeaponUpgrade(LocalServerContext ctx, WeaponUpgradeRequest req)
        {
            PlayerWeapon weapon = GetWeaponById(ctx, req.WeaponId);
            if (weapon == null)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 武器[{req.WeaponId}]未激活");
                throw new LocalServerException("武器未激活");
            }

            PlayerWeaponConfig cfg = DataConfigManager.Tables.TbPlayerWeaponConfig.GetOrDefault(req.WeaponId);
            if (cfg == null)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 武器[{req.WeaponId}]配置不存在");
                throw new LocalServerException("武器配置不存在");
            }

            if (weapon.Level >= cfg.WeaponStarList.Count)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 武器[{req.WeaponId}]已满级");
                throw new LocalServerException("武器已满级");
            }

            Dictionary<int, long> consumeItems = ctx.ItemEntrance.ConsumeToItems(cfg.NeedItems);
            if (!ctx.ItemEntrance.CheckEnough(consumeItems))
            {
                LogTool.LogWarning($"userId={ctx.UserId} 道具不足");
                throw new LocalServerException("道具不足");
            }

            // 对照 :101-108：事务内先扣道具、后升级；Execute 抛错等价于事务回滚
            ResultItems rsItems = ctx.ItemEntrance.Execute(consumeItems);

            // 对照 WeaponModel.TxUpgrade（PlayerWeaponModel.go:69-72）
            weapon.Level++;
            ctx.Store.MarkDirty(PlayerArchive.WeaponsFieldNumber);

            // 刷新轮语者武器战力（:115-118）
            long cp = ctx.CombatPower.RefreshModule(CombatPowerRefresher.ModuleWeapon);
            rsItems.CombatPower = new CombatPower { CombatPower_ = cp };

            return new WeaponUpgradeResponse { Weapon = weapon, Items = rsItems };
        }

        // 对照 weaponapplication/application_weapon.go:126-148 EquipHandler
        public WeaponEquipResponse WeaponEquip(LocalServerContext ctx, WeaponEquipRequest req)
        {
            PlayerWeapon weapon = GetWeaponById(ctx, req.WeaponId);
            if (weapon == null)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 武器[{req.WeaponId}]未激活");
                throw new LocalServerException("武器未激活");
            }

            PlayerSpeakerModule speaker = GetOrCreateSpeakerModule(ctx);
            speaker.EquippedWeaponId = req.WeaponId;
            ctx.Store.MarkDirty(PlayerArchive.PlayerSpeakerModuleFieldNumber);

            // Go 响应只回 Speaker，Items 为 nil
            return new WeaponEquipResponse { Speaker = speaker };
        }

        // 对照 weaponapplication/application_weapon.go:154-180 UnequipHandler
        public WeaponUnequipResponse WeaponUnequip(LocalServerContext ctx, WeaponUnequipRequest req)
        {
            PlayerWeapon weapon = GetWeaponById(ctx, req.WeaponId);
            if (weapon == null)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 武器[{req.WeaponId}]未激活");
                throw new LocalServerException("武器未激活");
            }

            PlayerSpeakerModule speaker = GetOrCreateSpeakerModule(ctx);
            if (speaker.EquippedWeaponId != req.WeaponId)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 武器[{req.WeaponId}]未装备");
                throw new LocalServerException("武器未装备");
            }
            speaker.EquippedWeaponId = 0;
            ctx.Store.MarkDirty(PlayerArchive.PlayerSpeakerModuleFieldNumber);

            // Go 响应只回 Speaker，Items 为 nil
            return new WeaponUnequipResponse { Speaker = speaker };
        }

        // 对照 cycleSpeakerModel.GetBySkinId（cycleSpeakerSkinModel.go:83-91）：查不到返回 null（Go 返回 Id==0 零值行）
        private PlayerCycleSpeakerSkin GetSkinById(LocalServerContext ctx, int skinId)
        {
            foreach (PlayerCycleSpeakerSkin skin in ctx.Store.Archive.CycleSpeakerSkins)
            {
                if (skin.SkinId == skinId)
                {
                    return skin;
                }
            }
            return null;
        }

        // 对照 WeaponModel.GetByWeaponId（PlayerWeaponModel.go:39-47）：查不到返回 null（Go 返回 Id==0 零值行）
        private PlayerWeapon GetWeaponById(LocalServerContext ctx, int weaponId)
        {
            foreach (PlayerWeapon weapon in ctx.Store.Archive.Weapons)
            {
                if (weapon.WeaponId == weaponId)
                {
                    return weapon;
                }
            }
            return null;
        }

        // 对照 cycleSpeakerModel.TxGetCycleSpeakerByUserId（cycleSpeakerModel.go:51-60）：缺失时懒建档
        private PlayerSpeakerModule GetOrCreateSpeakerModule(LocalServerContext ctx)
        {
            if (ctx.Store.Archive.PlayerSpeakerModule == null)
            {
                ctx.Store.Archive.PlayerSpeakerModule = new PlayerSpeakerModule();
            }
            return ctx.Store.Archive.PlayerSpeakerModule;
        }

        // 本地替代 Go DB 自增主键：取现有最大 Id + 1（同 ItemEntrance 口径）
        private int NextSkinId(LocalServerContext ctx)
        {
            int max = 0;
            foreach (PlayerCycleSpeakerSkin skin in ctx.Store.Archive.CycleSpeakerSkins)
            {
                if (skin.Id > max)
                {
                    max = skin.Id;
                }
            }
            return max + 1;
        }

        private int NextWeaponId(LocalServerContext ctx)
        {
            int max = 0;
            foreach (PlayerWeapon weapon in ctx.Store.Archive.Weapons)
            {
                if (weapon.Id > max)
                {
                    max = weapon.Id;
                }
            }
            return max + 1;
        }
    }
}
