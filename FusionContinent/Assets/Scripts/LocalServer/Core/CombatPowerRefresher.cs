using System.Collections.Generic;
using Cardwar;
using Config;

namespace FusionContinent.LocalServer
{
    /// <summary>
    /// 战力提供者接口，对照 Server/app/service/combatPowerService/combat_power_service.go:48-51。
    /// </summary>
    public interface ICombatPowerProvider
    {
        string Name { get; }
        long Calculate(LocalServerContext ctx);
    }

    /// <summary>
    /// 战力刷新骨架，对照 combat_power_service.go 的 CombatPowerService：
    /// Provider 注册表 + 各模块值缓存，RefreshModule 增量重算一个模块并刷新总值。
    /// Go 端用 Redis 做模块缓存、 persistAndRank 写榜；单机版缓存驻内存、总值写 Store.User.CombatPower。
    /// 当前注册 8 个模块 Provider（equip/equip_slot/hero_seat/speaker/weapon/soldier/skill/talent）；
    /// resonance（奥术共鸣）模块服务端正在删除，按需求跳过不注册。
    /// </summary>
    public class CombatPowerRefresher
    {
        // 对照 combat_power_service.go:30-42 模块名常量（跳过 ModuleResonance）
        public const string ModuleEquip = "equip";
        public const string ModuleEquipSlot = "equip_slot";
        public const string ModuleHeroSeat = "hero_seat";
        public const string ModuleSpeaker = "speaker";
        public const string ModuleWeapon = "weapon";
        public const string ModuleSoldier = "soldier";
        public const string ModuleSkill = "skill";
        public const string ModuleTalent = "talent";

        private readonly LocalServerContext _ctx;
        private readonly Dictionary<string, ICombatPowerProvider> _providers = new Dictionary<string, ICombatPowerProvider>();
        private readonly Dictionary<string, long> _moduleValues = new Dictionary<string, long>();

        public CombatPowerRefresher(LocalServerContext ctx)
        {
            _ctx = ctx;
            // 对照 combat_power_service.go:559-579 InitCombatPower（跳过 ArcResonanceProvider，共鸣模块删除中）
            Register(new EquipCombatPowerProvider());
            Register(new EquipSlotCombatPowerProvider());
            Register(new HeroSeatCombatPowerProvider());
            Register(new SpeakerCombatPowerProvider());
            Register(new WeaponCombatPowerProvider());
            Register(new SoldierCombatPowerProvider());
            Register(new SkillCombatPowerProvider());
            Register(new TalentCombatPowerProvider());
        }

        public void Register(ICombatPowerProvider provider)
        {
            _providers[provider.Name] = provider;
        }

        /// <summary>
        /// 重算指定模块战力、刷新总值（= 已注册 Provider 之和）、写回 Store.User.CombatPower 并 MarkUserDirty，返回最新总战力。
        /// 对照 combat_power_service.go:117-155 RefreshModule。
        /// </summary>
        public long RefreshModule(string module)
        {
            // 对照 combat_power_service.go:79-86 getProvider
            if (!_providers.TryGetValue(module, out var provider))
            {
                LogTool.LogWarning($"combat power provider {module} not registered");
                throw new LocalServerException("combat power provider not registered: " + module);
            }

            long contribution = provider.Calculate(_ctx);
            _moduleValues[module] = contribution;

            // 对照 combat_power_service.go:132-135：缓存缺已注册模块时按 RefreshAll 语义全量补齐
            foreach (var kv in _providers)
            {
                if (!_moduleValues.ContainsKey(kv.Key))
                {
                    _moduleValues[kv.Key] = kv.Value.Calculate(_ctx);
                }
            }

            long total = 0;
            foreach (var kv in _moduleValues)
            {
                total += kv.Value;
            }

            LogTool.Log($"[CombatPower] RefreshModule module={module} contribution={contribution} total={total}");

            // 对照 persistAndRank（combat_power_service.go:192-202）：单机版只写 User，无榜单
            _ctx.Store.User.CombatPower = (int)total;
            _ctx.Store.MarkUserDirty();
            return total;
        }

        /// <summary>
        /// 全量重算所有已注册模块战力并刷新总值，写回 Store.User.CombatPower 并 MarkUserDirty，返回最新总战力。
        /// 对照 combat_power_service.go RefreshAll（入口 cpapplication.RefreshAll，application_combat_power.go:21-23）。
        /// </summary>
        public long RefreshAll()
        {
            long total = 0;
            foreach (var kv in _providers)
            {
                long contribution = kv.Value.Calculate(_ctx);
                _moduleValues[kv.Key] = contribution;
                total += contribution;
            }

            LogTool.Log($"[CombatPower] RefreshAll total={total}");

            _ctx.Store.User.CombatPower = (int)total;
            _ctx.Store.MarkUserDirty();
            return total;
        }
    }

    /// <summary>
    /// 装备战力 Provider（基础+词条+特效+融合石），对照 combat_power_service.go:212-272 EquipProvider。
    /// 口径：只有穿戴到全局穿戴位（WearerId=TbPlayerConst.GlobalUserWearId）的装备才计入；
    /// 融合石只计入绑定到已穿戴装备的（gem.WearerId ∈ 已穿戴装备 Id 集合）。
    /// 偏差：Go 端 Entrys/Effects 是 JSON 字符串，客户端 proto 已是 repeated int，直接取 Count。
    /// </summary>
    internal class EquipCombatPowerProvider : ICombatPowerProvider
    {
        public string Name => CombatPowerRefresher.ModuleEquip;

        public long Calculate(LocalServerContext ctx)
        {
            // 对照 combat_power_service.go:220 GlobalUserWearId 标识装备穿戴在玩家身上（非某个英雄）
            int globalWearId = DataConfigManager.Tables.TbPlayerConst.GlobalUserWearId;
            long total = 0;
            var wornEquipIds = new HashSet<int>();

            foreach (PlayerEquip equip in ctx.Store.Archive.Equips)
            {
                if (equip.WearerId != globalWearId)
                {
                    continue;
                }
                wornEquipIds.Add(equip.Id);

                EquipConfig cfg = DataConfigManager.Tables.TbEquipConfig.GetOrDefault(equip.EquipId);
                if (cfg == null)
                {
                    continue;
                }
                // 装备基础战力 + 词条战力 + 特效词条战力（:236-250）
                total += cfg.FightNum;
                total += (long)equip.Entrys.Count * cfg.EntryFightNum;
                total += (long)equip.Effects.Count * cfg.EffectsFightNum;
            }

            // 融合石 — 仅计入绑定到穿戴装备的（:253-269）
            foreach (PlayerFusionGemToC gem in ctx.Store.Archive.FusionGemsV2)
            {
                if (gem.WearerId <= 0 || !wornEquipIds.Contains(gem.WearerId))
                {
                    continue;
                }
                GemFusionConfig cfg = DataConfigManager.Tables.TbGemFusionConfig.GetOrDefault(gem.GemId);
                if (cfg == null)
                {
                    continue;
                }
                foreach (GemFusionLevelListStruct lvCfg in cfg.LevelList)
                {
                    if (lvCfg.Level == gem.Level)
                    {
                        total += lvCfg.FightNum;
                        break;
                    }
                }
            }

            return total;
        }
    }

    /// <summary>
    /// 装备槽位锻造战力 Provider，对照 combat_power_service.go:278-310 EquipSlotProvider。
    /// 按 Slot 匹配 TbEquipSlotConfig.SlotType，再按 Level 匹配 SlotLevelList 档累加 FightNum。
    /// </summary>
    internal class EquipSlotCombatPowerProvider : ICombatPowerProvider
    {
        public string Name => CombatPowerRefresher.ModuleEquipSlot;

        public long Calculate(LocalServerContext ctx)
        {
            long total = 0;
            foreach (PlayerEquipSlot slot in ctx.Store.Archive.EquipSlots)
            {
                // 对照 findSlotConfig（:302-310）：按 SlotType 线性查找槽位配置
                EquipSlotConfig slotCfg = null;
                foreach (EquipSlotConfig cfg in DataConfigManager.Tables.TbEquipSlotConfig.DataList)
                {
                    if ((int)cfg.SlotType == slot.Slot)
                    {
                        slotCfg = cfg;
                        break;
                    }
                }
                if (slotCfg == null)
                {
                    continue;
                }
                foreach (SlotLevelListStruct lvCfg in slotCfg.SlotLevelList)
                {
                    if (lvCfg.Level == slot.Level)
                    {
                        total += lvCfg.FightNum;
                        break;
                    }
                }
            }
            return total;
        }
    }

    /// <summary>
    /// 备战席/城墙战力 Provider，对照 combat_power_service.go:316-365 HeroSeatProvider。
    /// 口径：遍历城墙全部已学技能，按 SkillId 匹配 TbHeroSeatConfig、按 Level 匹配 SeatLevelList 档累加 FightNum。
    /// Go 端先建 skillLevelMap 再二次遍历（:336-351），key 即来自同一列表，等价于直接用 skill.Level，此处简化。
    /// </summary>
    internal class HeroSeatCombatPowerProvider : ICombatPowerProvider
    {
        public string Name => CombatPowerRefresher.ModuleHeroSeat;

        public long Calculate(LocalServerContext ctx)
        {
            long total = 0;
            foreach (PlayerCityWallSkill skill in ctx.Store.Archive.CityWall.Skills)
            {
                if (skill.SkillId == 0)
                {
                    continue;
                }
                HeroSeatConfig cfg = DataConfigManager.Tables.TbHeroSeatConfig.GetOrDefault(skill.SkillId);
                if (cfg == null)
                {
                    continue;
                }
                foreach (SeatLevelListStruct lvCfg in cfg.SeatLevelList)
                {
                    if (lvCfg.Level == skill.Level)
                    {
                        total += lvCfg.FightNum;
                        break;
                    }
                }
            }
            return total;
        }
    }

    /// <summary>
    /// 轮语者形象战力 Provider，对照 combat_power_service.go:374-397 SpeakerProvider。
    /// 口径：拥有的所有皮肤都计入（不限于当前使用的），按 Level 匹配 ImageStarList 档累加 FightNum。
    /// </summary>
    internal class SpeakerCombatPowerProvider : ICombatPowerProvider
    {
        public string Name => CombatPowerRefresher.ModuleSpeaker;

        public long Calculate(LocalServerContext ctx)
        {
            long total = 0;
            foreach (PlayerCycleSpeakerSkin skin in ctx.Store.Archive.CycleSpeakerSkins)
            {
                PlayerFigureConfig cfg = DataConfigManager.Tables.TbPlayerFigureConfig.GetOrDefault(skin.SkinId);
                if (cfg == null)
                {
                    continue;
                }
                foreach (ImageStarListStruct starCfg in cfg.ImageStarList)
                {
                    if (starCfg.Level == skin.Level)
                    {
                        total += starCfg.FightNum;
                        break;
                    }
                }
            }
            return total;
        }
    }

    /// <summary>
    /// 轮语者武器战力 Provider，对照 combat_power_service.go:403-426 WeaponProvider。
    /// 按 Level 匹配 WeaponStarList 档累加 FightNum。
    /// </summary>
    internal class WeaponCombatPowerProvider : ICombatPowerProvider
    {
        public string Name => CombatPowerRefresher.ModuleWeapon;

        public long Calculate(LocalServerContext ctx)
        {
            long total = 0;
            foreach (PlayerWeapon weapon in ctx.Store.Archive.Weapons)
            {
                PlayerWeaponConfig cfg = DataConfigManager.Tables.TbPlayerWeaponConfig.GetOrDefault(weapon.WeaponId);
                if (cfg == null)
                {
                    continue;
                }
                foreach (WeaponStarListStruct starCfg in cfg.WeaponStarList)
                {
                    if (starCfg.Level == weapon.Level)
                    {
                        total += starCfg.FightNum;
                        break;
                    }
                }
            }
            return total;
        }
    }

    /// <summary>
    /// 英灵战力 Provider，对照 combat_power_service.go:461-489 SoldierProvider。
    /// 遍历全部卡牌，能匹配 TbSoldierBattleConfig 的即英灵卡；按 Lv 匹配 LevelPropertyList 档累加 FightNum，
    /// 已觉醒（Data.IsAwaked）额外加 AwakingFightNum。
    /// </summary>
    internal class SoldierCombatPowerProvider : ICombatPowerProvider
    {
        public string Name => CombatPowerRefresher.ModuleSoldier;

        public long Calculate(LocalServerContext ctx)
        {
            long total = 0;
            foreach (PlayerCard card in ctx.Store.Archive.Cards)
            {
                SoldierBattleConfig cfg = DataConfigManager.Tables.TbSoldierBattleConfig.GetOrDefault(card.CardId);
                if (cfg == null)
                {
                    continue;
                }
                foreach (SoldierLevelPropertyList lvCfg in cfg.LevelPropertyList)
                {
                    if (lvCfg.Level == card.Lv)
                    {
                        total += lvCfg.FightNum;
                        break;
                    }
                }
                // 对照 :481-485 cardData.FromJson + IsAwaked；客户端 proto 已是结构体，老数据 Data 可能为空需判空
                if (card.Data != null && card.Data.IsAwaked)
                {
                    total += cfg.AwakingFightNum;
                }
            }
            return total;
        }
    }

    /// <summary>
    /// 技能战力 Provider，对照 combat_power_service.go:495-518 SkillProvider。
    /// 遍历全部卡牌，能匹配 TbSpecialEffectsConfig 的即技能卡；按 Lv 匹配 LevelPropertyList 档累加 FightNum。
    /// </summary>
    internal class SkillCombatPowerProvider : ICombatPowerProvider
    {
        public string Name => CombatPowerRefresher.ModuleSkill;

        public long Calculate(LocalServerContext ctx)
        {
            long total = 0;
            foreach (PlayerCard card in ctx.Store.Archive.Cards)
            {
                SpecialEffectsConfig cfg = DataConfigManager.Tables.TbSpecialEffectsConfig.GetOrDefault(card.CardId);
                if (cfg == null)
                {
                    continue;
                }
                foreach (SpecialLevelPropertyList lvCfg in cfg.LevelPropertyList)
                {
                    if (lvCfg.Level == card.Lv)
                    {
                        total += lvCfg.FightNum;
                        break;
                    }
                }
            }
            return total;
        }
    }

    /// <summary>
    /// 天赋战力 Provider，对照 combat_power_service.go:527-553 TalentProvider。
    /// 口径：所有已拥有天赋都计入（非仅激活的）；按 Lv 匹配档位累加 FightNum，ExtraLv>0 时额外累加该档 ExtraFightNum。
    /// </summary>
    internal class TalentCombatPowerProvider : ICombatPowerProvider
    {
        public string Name => CombatPowerRefresher.ModuleTalent;

        public long Calculate(LocalServerContext ctx)
        {
            long total = 0;
            foreach (PlayerTalent talent in ctx.Store.Archive.Talents)
            {
                var cfg = DataConfigManager.Tables.TbTalentConfig.GetOrDefault(talent.TalentId);
                if (cfg == null)
                {
                    continue;
                }
                foreach (TalentListStruct lvCfg in cfg.TalentList)
                {
                    if (lvCfg.Level == talent.Lv)
                    {
                        total += lvCfg.FightNum;
                        if (talent.ExtraLv > 0)
                        {
                            // 对照 Go：ExtraFightNum 取自 Lv 匹配档，而非 ExtraLv 匹配档（保留 Go 原口径）
                            total += lvCfg.ExtraFightNum;
                        }
                        break;
                    }
                }
            }
            return total;
        }
    }
}
