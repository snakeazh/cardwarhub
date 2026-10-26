using System.Collections.Generic;
using Cardwar;
using Config;

namespace FusionContinent.LocalServer
{
    /// <summary>
    /// 装备 + 装备槽 Handler，逐行对照：
    /// - Server/app/api/application/equipapplication/application_equip.go
    /// - Server/app/api/application/equipslotapplication/application_equip_slot.go
    /// - 词条/特效随机：Server/app/service/EntryService/EntryService.go
    /// - 存档读写：Server/app/model/EquipModel/PlayerEquipModel.go、EquipSlotModel/PlayerEquipSlotModel.go
    /// Go 端 Entrys/Effects 是 JSON 字符串，本地 proto 存档即 repeated int，直接操作列表。
    /// </summary>
    public class EquipHandler
    {
        // 对照 application_equip.go:26-69 WearEquipHandler
        public WearEquipResponse WearEquip(LocalServerContext ctx, WearEquipRequest req)
        {
            // 对照 EquipModel.GetById（PlayerEquipModel.go:82-90），查不到等价 Id==0
            PlayerEquip equip = GetEquipById(ctx, req.Id);
            if (equip == null || equip.UserId != ctx.UserId)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 参数错误");
                throw new LocalServerException("参数错误");
            }

            EquipConfig equipCfg = DataConfigManager.Tables.TbEquipConfig.GetOrDefault(equip.EquipId);
            if (equipCfg == null)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 装备[{equip.EquipId}]，配置不存在");
                throw new LocalServerException($"装备[{equip.EquipId}]，配置不存在");
            }

            var rs = new List<PlayerEquip>();
            // 卸下路径跳过替换查询（:41-43）
            if (equip.WearerId == 0)
            {
                // 对照 EquipModel.GetByWearerEquip（PlayerEquipModel.go:184-192）
                foreach (PlayerEquip other in ctx.Store.Archive.Equips)
                {
                    if (other.UserId == ctx.UserId && other.WearerId == req.WearerId && other.PartType == equip.PartType)
                    {
                        other.WearerId = 0;
                        rs.Add(other);
                    }
                }
            }

            // 对照 :53-57：已穿戴则卸下，否则穿到目标穿戴位
            if (equip.WearerId > 0)
            {
                equip.WearerId = 0;
            }
            else
            {
                equip.WearerId = req.WearerId;
            }
            rs.Add(equip);
            ctx.Store.MarkDirty(PlayerArchive.EquipsFieldNumber);

            // 对照 :67
            long newCP = ctx.CombatPower.RefreshModule(CombatPowerRefresher.ModuleEquip);

            // 对照 equip_converter.go:13-24：Items 仅回填战力
            var resp = new WearEquipResponse
            {
                Items = new ResultItems { CombatPower = new CombatPower { CombatPower_ = newCP } },
            };
            resp.Equips.AddRange(rs);
            return resp;
        }

        // 对照 application_equip.go:291-360 UpPhaseEquipHandler（Go 已整体注释，路由无实现）
        // 偏差：被注释代码依赖的 EquipConfig.PhaseList / UpPhaseConsumes / EntryGroupId 在现行配置
        // （Go data/configFiles/EquipConfig.go 与客户端 Luban EquipConfig.cs）均已删除：
        // - 「已满级」判断（原 PhaseList 长度）无配置来源，跳过；
        // - 升阶消耗（原 PhaseList[].UpPhaseConsumes）无配置来源，items 恒为空；
        // - 词条补齐的品质取自 equipCfg.Quality（原取 PhaseList[Phase-1].Quality），
        //   并按 EquipQualityTypeEntryCountStruct.Type 匹配（原按品质枚举值直接索引切片）。
        public UpPhaseEquipResponse UpPhaseEquip(LocalServerContext ctx, UpPhaseEquipRequest req)
        {
            // 对照 EquipModel.GetByIds（PlayerEquipModel.go:92-101，IN 查询，重复 Id 只返回一行）
            var equips = new List<PlayerEquip>();
            var seen = new HashSet<int>();
            foreach (int id in req.Ids)
            {
                if (seen.Contains(id))
                {
                    continue;
                }
                seen.Add(id);
                PlayerEquip equip = GetEquipById(ctx, id);
                if (equip != null)
                {
                    equips.Add(equip);
                }
            }
            if (equips.Count != req.Ids.Count)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 参数错误");
                throw new LocalServerException("参数错误");
            }

            foreach (PlayerEquip equip in equips)
            {
                if (equip.UserId != ctx.UserId)
                {
                    LogTool.LogWarning($"userId={ctx.UserId} 参数错误");
                    throw new LocalServerException("参数错误");
                }

                EquipConfig equipCfg = DataConfigManager.Tables.TbEquipConfig.GetOrDefault(equip.EquipId);
                if (equipCfg == null)
                {
                    LogTool.LogWarning($"userId={ctx.UserId} 装备[{equip.EquipId}]，配置不存在");
                    throw new LocalServerException($"装备[{equip.EquipId}]，配置不存在");
                }
            }

            var items = new Dictionary<int, long>(); // 无消耗来源，恒为空；保留 Execute 以对齐返回结构
            ResultItems rsItems = ctx.ItemEntrance.Execute(items);

            // 对照 :318
            List<EquipQualityTypeEntryCountStruct> entryCountList = DataConfigManager.Tables.TbGameBattleConst.EquipQualityTypeEntryCountList;

            foreach (PlayerEquip equip in equips)
            {
                equip.Phase++;

                EquipConfig equipCfg = DataConfigManager.Tables.TbEquipConfig.Get(equip.EquipId);
                int qualityEntryCount = 0;
                foreach (EquipQualityTypeEntryCountStruct ec in entryCountList)
                {
                    if (ec.Type == equipCfg.Quality)
                    {
                        qualityEntryCount = ec.Count;
                        break;
                    }
                }

                // 对照 :339-348：品质词条数上限超过现有词条数时随机补齐
                if (qualityEntryCount > equip.Entrys.Count)
                {
                    List<int> newEntrys = GetRandomEntrys(ctx, qualityEntryCount - equip.Entrys.Count, equipCfg.EntryGroup);
                    equip.Entrys.AddRange(newEntrys);
                }
            }
            ctx.Store.MarkDirty(PlayerArchive.EquipsFieldNumber);

            // 对照 equip_converter.go:85-89
            var resp = new UpPhaseEquipResponse { Items = rsItems };
            resp.Equips.AddRange(equips);
            return resp;
        }

        // 对照 application_equip.go:85-137 FragmentEquip
        public FragmentEquipResponse FragmentEquip(LocalServerContext ctx, FragmentEquipRequest req)
        {
            // 对照 EquipModel.GetByIds（IN 查询，重复 Id 只返回一行）
            var equips = new List<PlayerEquip>();
            var seen = new HashSet<int>();
            foreach (int id in req.Ids)
            {
                if (seen.Contains(id))
                {
                    continue;
                }
                seen.Add(id);
                PlayerEquip equip = GetEquipById(ctx, id);
                if (equip != null)
                {
                    equips.Add(equip);
                }
            }
            if (equips.Count != req.Ids.Count)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 参数错误");
                throw new LocalServerException("参数错误");
            }

            var items = new Dictionary<int, long>();
            var rewards = new List<Cardwar.RewardAndNum>();
            foreach (PlayerEquip equip in equips)
            {
                if (equip.UserId != ctx.UserId)
                {
                    LogTool.LogWarning($"userId={ctx.UserId} 参数违法");
                    throw new LocalServerException("参数违法");
                }

                if (equip.WearerId > 0)
                {
                    LogTool.LogWarning($"userId={ctx.UserId} 佩戴中的装备不能被分解");
                    throw new LocalServerException("佩戴中的装备不能被分解");
                }

                EquipConfig equipCfg = DataConfigManager.Tables.TbEquipConfig.GetOrDefault(equip.EquipId);
                if (equipCfg == null)
                {
                    LogTool.LogWarning($"userId={ctx.UserId} 装备[{equip.EquipId}]，配置不存在");
                    throw new LocalServerException($"装备[{equip.EquipId}]，配置不存在");
                }

                // 装备分解返还（:112-116）：Num 恒取 RandomNumRange[0]，Go 原逻辑不做随机
                foreach (FragmentRewardAndNum fItem in equipCfg.FragmentItems)
                {
                    rewards.Add(new Cardwar.RewardAndNum { Id = fItem.Id, Num = fItem.RandomNumRange[0] });
                    // 对照 RewardsToItems + MergeMapsAndSum64
                    Accumulate(items, fItem.Id, fItem.RandomNumRange[0]);
                }
            }

            // 对照 :121-126：先发返还道具，再删除装备（单机顺序执行，Execute 失败等价于事务回滚）
            ResultItems rsItems = ctx.ItemEntrance.Execute(items);

            // 对照 EquipModel.TransactionDeleteByIds（PlayerEquipModel.go:250-259）
            foreach (PlayerEquip equip in equips)
            {
                ctx.Store.Archive.Equips.Remove(equip);
            }
            ctx.Store.MarkDirty(PlayerArchive.EquipsFieldNumber);

            // 对照 equip_converter.go:26-36
            var resp = new FragmentEquipResponse { Items = rsItems };
            resp.Rewards.AddRange(rewards);
            resp.FragmentIds.AddRange(req.Ids);
            return resp;
        }

        // 对照 application_equip.go:149-237 RefreshEntryEquip
        public RefreshEntryEquipResponse RefreshEntryEquip(LocalServerContext ctx, RefreshEntryEquipRequest req)
        {
            PlayerEquip equip = GetEquipById(ctx, req.Id);
            if (equip == null || equip.UserId != ctx.UserId)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 参数错误");
                throw new LocalServerException("参数错误");
            }

            EquipConfig equipCfg = DataConfigManager.Tables.TbEquipConfig.GetOrDefault(equip.EquipId);
            if (equipCfg == null)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 装备[{equip.EquipId}]，配置不存在");
                throw new LocalServerException($"装备[{equip.EquipId}]，配置不存在");
            }

            // 对照 :162-168
            Dictionary<int, long> items;
            if (equipCfg.RefreshEntryConsume.Count > 0)
            {
                items = ctx.ItemEntrance.ConsumeToItemsRide(equipCfg.RefreshEntryConsume, 1);
            }
            else
            {
                LogTool.LogWarning($"userId={ctx.UserId} 当前装备没有配置刷新词条的消耗");
                throw new LocalServerException("当前装备没有配置刷新词条的消耗");
            }

            // 锁定特效额外消耗（:170-174）
            if (req.LockedEffects.Count > 0 && equipCfg.LockEffectRefreshEntryConsume.Count > 0)
            {
                Dictionary<int, long> lockItems = ctx.ItemEntrance.ConsumeToItemsRide(equipCfg.LockEffectRefreshEntryConsume, req.LockedEffects.Count);
                foreach (var kv in lockItems)
                {
                    Accumulate(items, kv.Key, kv.Value);
                }
            }

            if (!ctx.ItemEntrance.CheckEnough(items))
            {
                LogTool.LogWarning($"userId={ctx.UserId} 道具不足");
                throw new LocalServerException("道具不足");
            }

            // 刷新词条：全量替换，写入 TempEntrys（:181-188）
            List<int> newEntrys = GetRandomEntrys(ctx, equipCfg.EntryNum, equipCfg.EntryGroup);
            equip.TempEntrys.Clear();
            equip.TempEntrys.AddRange(newEntrys);

            // 收集锁定的特效（:199-209）
            var lockedSet = new HashSet<int>();
            foreach (int id in req.LockedEffects)
            {
                lockedSet.Add(id);
            }
            var lockedIDs = new List<int>();
            foreach (int id in equip.Effects)
            {
                if (lockedSet.Contains(id))
                {
                    lockedIDs.Add(id);
                }
            }

            // 生成特效（概率触发 + 等概率个数，锁定 ID 不参与随机，不超上限）（:212）
            List<int> tempEffects = GetRandomEffects(ctx, equipCfg, lockedIDs);
            equip.TempEffects.Clear();
            equip.TempEffects.AddRange(tempEffects);
            // 对照 :220 equip.Save()：Go 在扣道具事务之前先落库装备，单机保持同顺序
            ctx.Store.MarkDirty(PlayerArchive.EquipsFieldNumber);

            // 操作道具（:223-227）
            ResultItems rsItems = ctx.ItemEntrance.Execute(items);

            return new RefreshEntryEquipResponse { Equip = equip, Items = rsItems };
        }

        // 对照 application_equip.go:243-289 UseRefreshEntryEquip
        public UseRefreshEntryEquipResponse UseRefreshEntryEquip(LocalServerContext ctx, UseRefreshEntryEquipRequest req)
        {
            PlayerEquip equip = GetEquipById(ctx, req.Id);
            if (equip == null || equip.UserId != ctx.UserId)
            {
                throw new LocalServerException("参数错误");
            }

            EquipConfig equipCfg = DataConfigManager.Tables.TbEquipConfig.GetOrDefault(equip.EquipId);
            if (equipCfg == null)
            {
                throw new LocalServerException($"装备[{equip.EquipId}]，配置不存在");
            }

            // 对照 :254（Go 判 "" / "[]"，proto 即空列表）
            if (equip.TempEntrys.Count == 0)
            {
                LogTool.LogWarning($"userId={ctx.UserId} equipId={req.Id} 未刷新词条");
                throw new LocalServerException("请先刷新词条");
            }

            // 刷新词条（:260-279）：tempEntrys 中 >0 的按位覆盖 entrys
            var entrys = new List<int>(equip.Entrys);
            for (int idx = 0; idx < equip.TempEntrys.Count; idx++)
            {
                int entryId = equip.TempEntrys[idx];
                if (entryId > 0)
                {
                    entrys[idx] = entryId;
                }
            }
            equip.Entrys.Clear();
            equip.Entrys.AddRange(entrys);
            equip.TempEntrys.Clear();

            // 使用特效：TempEffects 已是完整结果（locked + new），直接替换（:281-283）
            equip.Effects.Clear();
            equip.Effects.AddRange(equip.TempEffects);
            equip.TempEffects.Clear();
            ctx.Store.MarkDirty(PlayerArchive.EquipsFieldNumber);

            // 对照 :287
            long newCP = ctx.CombatPower.RefreshModule(CombatPowerRefresher.ModuleEquip);

            // 对照 equip_converter.go:72-83
            return new UseRefreshEntryEquipResponse
            {
                Equip = equip,
                Items = new ResultItems { CombatPower = new CombatPower { CombatPower_ = newCP } },
            };
        }

        // 对照 equipslotapplication/application_equip_slot.go:25-78 UpgradeEquipSlotHandler
        public EquipSlotUpgradeResponse UpgradeEquipSlot(LocalServerContext ctx, EquipSlotUpgradeRequest req)
        {
            // 对照 :26-32：按 SlotType 线性查找槽位配置
            EquipSlotConfig slotCfg = null;
            foreach (EquipSlotConfig cfg in DataConfigManager.Tables.TbEquipSlotConfig.DataList)
            {
                if ((int)cfg.SlotType == req.Slot)
                {
                    slotCfg = cfg;
                    break;
                }
            }
            if (slotCfg == null)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 槽位[{req.Slot}]配置不存在");
                throw new LocalServerException($"槽位[{req.Slot}]配置不存在");
            }

            // 对照 EquipSlotModel.CreateOrGet（PlayerEquipSlotModel.go:57-66）：查不到即建档 Level=1
            PlayerEquipSlot record = null;
            foreach (PlayerEquipSlot slot in ctx.Store.Archive.EquipSlots)
            {
                if (slot.Slot == req.Slot)
                {
                    record = slot;
                    break;
                }
            }
            if (record == null)
            {
                int maxId = 0;
                foreach (PlayerEquipSlot slot in ctx.Store.Archive.EquipSlots)
                {
                    if (slot.Id > maxId)
                    {
                        maxId = slot.Id;
                    }
                }
                record = new PlayerEquipSlot
                {
                    Id = maxId + 1,
                    UserId = ctx.UserId,
                    Slot = req.Slot,
                    Level = 1,
                };
                ctx.Store.Archive.EquipSlots.Add(record);
                ctx.Store.MarkDirty(PlayerArchive.EquipSlotsFieldNumber);
            }

            if (record.Level >= slotCfg.SlotLevelList.Count)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 已满级");
                throw new LocalServerException("已满级");
            }

            // 对照 :45：取当前等级档（列表按等级升序，Level 从 1 开始）
            SlotLevelListStruct curEntry = slotCfg.SlotLevelList[record.Level - 1];
            Dictionary<int, long> consumeItems = ctx.ItemEntrance.ConsumeToItems(curEntry.UpPhaseConsumes);
            if (consumeItems.Count > 0 && !ctx.ItemEntrance.CheckEnough(consumeItems))
            {
                LogTool.LogWarning($"userId={ctx.UserId} 道具不足");
                throw new LocalServerException("道具不足");
            }

            // 对照 :53-63（ResourceLockService 行锁不迁移）
            ResultItems rsItems = null;
            if (consumeItems.Count > 0)
            {
                rsItems = ctx.ItemEntrance.Execute(consumeItems);
            }
            record.Level++;
            ctx.Store.MarkDirty(PlayerArchive.EquipSlotsFieldNumber);

            // 对照 :69-71
            if (rsItems == null)
            {
                rsItems = new ResultItems();
            }

            long cp = ctx.CombatPower.RefreshModule(CombatPowerRefresher.ModuleEquipSlot);
            rsItems.CombatPower = new CombatPower { CombatPower_ = cp };

            // 对照 equip_slot_converter.go:10-15
            return new EquipSlotUpgradeResponse { EquipSlot = record, Items = rsItems };
        }

        // 对照 EquipModel.GetById（PlayerEquipModel.go:82-90）：查不到返回 null（Go 返回 Id==0 零值行）
        private PlayerEquip GetEquipById(LocalServerContext ctx, int id)
        {
            foreach (PlayerEquip equip in ctx.Store.Archive.Equips)
            {
                if (equip.Id == id)
                {
                    return equip;
                }
            }
            return null;
        }

        // 对照 EntryService.GetRandomEntrys（EntryService.go:13-44）：按词条组过滤后洗牌取前 num 个
        private List<int> GetRandomEntrys(LocalServerContext ctx, int num, int entryGroup)
        {
            var speEntryList = new List<EquipEntryConfig>();
            foreach (EquipEntryConfig entry in DataConfigManager.Tables.TbEquipEntryConfig.DataList)
            {
                if (entry.EntryGroup == entryGroup)
                {
                    speEntryList.Add(entry);
                }
            }

            if (speEntryList.Count == 0 || num <= 0)
            {
                return new List<int>();
            }
            if (num > speEntryList.Count)
            {
                num = speEntryList.Count;
            }

            // Fisher-Yates 洗牌，对照 Go rand.Shuffle
            for (int i = speEntryList.Count - 1; i > 0; i--)
            {
                int j = ctx.Rng.Next(i + 1);
                EquipEntryConfig tmp = speEntryList[i];
                speEntryList[i] = speEntryList[j];
                speEntryList[j] = tmp;
            }

            var entrys = new List<int>(num);
            for (int i = 0; i < num; i++)
            {
                entrys.Add(speEntryList[i].Id);
            }
            return entrys;
        }

        // 对照 EntryService.GetRandomEffects（EntryService.go:47-95）：概率触发 + 等概率个数，排除 lockedIDs
        private List<int> GetRandomEffects(LocalServerContext ctx, EquipConfig equipCfg, List<int> lockedIDs)
        {
            if (equipCfg.MaxEffectsNum == 0)
            {
                return new List<int>();
            }
            var lockedSet = new HashSet<int>();
            foreach (int id in lockedIDs)
            {
                lockedSet.Add(id);
            }

            int maxSlots = equipCfg.MaxEffectsNum - lockedIDs.Count;
            if (maxSlots <= 0)
            {
                return new List<int>(lockedIDs);
            }

            var speEffectList = new List<EquipEffectsConfig>();
            foreach (EquipEffectsConfig effect in DataConfigManager.Tables.TbEquipEffectsConfig.DataList)
            {
                if (lockedSet.Contains(effect.Id))
                {
                    continue;
                }
                if (effect.EffectsGroup == equipCfg.EffectsGroup && equipCfg.Part == effect.Type)
                {
                    speEffectList.Add(effect);
                }
            }

            if (speEffectList.Count == 0)
            {
                return new List<int>(lockedIDs);
            }

            // 概率触发：是否刷出特效（:74-77）
            float prob = DataConfigManager.Tables.TbPlayerConst.ProbabilityOfEquipOccurrence;
            if ((float)ctx.Rng.NextDouble() >= prob)
            {
                return new List<int>(lockedIDs);
            }

            // 等概率决定新特效个数: 1 ~ maxSlots（:80-83）
            int num = ctx.Rng.Next(maxSlots) + 1;
            if (num > speEffectList.Count)
            {
                num = speEffectList.Count;
            }

            // Fisher-Yates，对照 rand.Shuffle（:85-87）
            for (int i = speEffectList.Count - 1; i > 0; i--)
            {
                int j = ctx.Rng.Next(i + 1);
                EquipEffectsConfig tmp = speEffectList[i];
                speEffectList[i] = speEffectList[j];
                speEffectList[j] = tmp;
            }

            var effects = new List<int>(lockedIDs);
            for (int i = 0; i < num; i++)
            {
                effects.Add(speEffectList[i].Id);
            }
            return effects;
        }

        private void Accumulate(Dictionary<int, long> map, int key, long value)
        {
            map.TryGetValue(key, out long cur);
            map[key] = cur + value;
        }
    }
}
