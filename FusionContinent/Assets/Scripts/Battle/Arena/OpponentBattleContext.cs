using System.Collections.Generic;
using System.Linq;
using Cardwar;
using Config;
using FusionContinent.Model;
using FusionContinent.Utils;
using Game.Team;
using Newtonsoft.Json;
using PlayerCard = FusionContinent.Model.PlayerCard;

namespace FusionContinent
{
    /// <summary>
    /// PVP 对侧局内运行时上下文。
    /// 由 <c>api/pvp/opponentInfo</c> 响应初始化，供上场、查卡、皮肤等局内逻辑使用。
    /// </summary>
    public class OpponentBattleContext
    {
        public Cardwar.PlayerProfile Raw { get; private set; }
        public PlayerBaseInfo BaseInfo { get; private set; }

        /// <summary>对侧卡牌：CardId → 本地 PlayerCard（含等级/皮肤）。</summary>
        public Dictionary<int, PlayerCard> Cards { get; } = new Dictionary<int, PlayerCard>();

        /// <summary>当前出战编队解析后的士兵列表。</summary>
        public List<BattleCardGroup> SoldierTeams { get; } = new List<BattleCardGroup>();

        /// <summary>士兵站位映射（CardsPosition）。</summary>
        public List<int> CardsPosition { get; private set; } = new List<int>();

        public int ActiveTeamId { get; private set; }

        /// <summary>对侧编队中的奥数卡（通用技能，被动类型）。</summary>
        public List<BattleCardGroup> GeneralSkillTeams { get; } = new List<BattleCardGroup>();

        /// <summary>对侧编队中的主动/被动技能卡（ActsOnCard 指定作用英灵）。</summary>
        public List<BattleCardGroup> SkillTeams { get; } = new List<BattleCardGroup>();

        /// <summary>对侧佩戴装备（本地模型）。</summary>
        public List<FusionContinent.Model.PlayerEquip> WearEquips { get; private set; } = new List<FusionContinent.Model.PlayerEquip>();

        /// <summary>对侧奥数共鸣（本地模型）。</summary>
        public List<FusionContinent.Model.PlayerArcResonance> ArcResonances { get; private set; } = new List<FusionContinent.Model.PlayerArcResonance>();

        /// <summary>对侧佩戴装备上的融合宝石（WearerId 对应装备实例 Id）。</summary>
        public List<FusionContinent.Model.PlayerFusionGem> WearFusionGems { get; private set; } = new List<FusionContinent.Model.PlayerFusionGem>();

        /// <summary>对侧天赋列表（含使用中的天赋）。</summary>
        public List<FusionContinent.Model.PlayerTalent> Talents { get; private set; } = new List<FusionContinent.Model.PlayerTalent>();

        /// <summary>对侧城墙等级。</summary>
        public int CityWallLevel { get; private set; }

        /// <summary>对侧武器列表（本地模型）。</summary>
        public List<FusionContinent.Model.PlayerWeapon> Weapons { get; private set; } = new List<FusionContinent.Model.PlayerWeapon>();

        /// <summary>对侧当前穿戴武器配置 Id（来自 SpeakerModule.EquippedWeaponId）。</summary>
        public int EquippedWeaponConfigId { get; private set; }

        /// <summary>对侧皮肤列表（本地模型）。</summary>
        public List<FusionContinent.Model.PlayerCycleSpeakerSkin> Skins { get; private set; } = new List<FusionContinent.Model.PlayerCycleSpeakerSkin>();

        /// <summary>对侧当前穿戴皮肤配置 Id（来自 SpeakerModule.UseSkinId）。</summary>
        public int EquippedSkinConfigId { get; private set; }

        /// <summary>对侧城墙技能（SkillId → 等级）。</summary>
        public Dictionary<int, int> CityWallSkills { get; } = new Dictionary<int, int>();

        /// <summary>对侧城墙技能槽位（SlotSkills 顺序，值为技能 Id）。</summary>
        public List<int> CityWallSlotSkills { get; } = new List<int>();

        /// <summary>对侧是否持有贪婪预支奥数卡（开局金币额外加成）。</summary>
        public bool HasGreedyAdvanceCard { get; private set; }

        public static OpponentBattleContext FromProfile(Cardwar.PlayerProfile detail)
        {
            if (detail == null)
            {
                LogTool.LogError("[OpponentBattleContext] PlayerProfile 为空，无法构建对侧上下文");
                return null;
            }

            var ctx = new OpponentBattleContext();
            ctx.Initialize(detail);
            return ctx;
        }

        public void Initialize(Cardwar.PlayerProfile detail)
        {
            Raw = detail;
            Cards.Clear();
            SoldierTeams.Clear();
            CardsPosition = new List<int>();
            ActiveTeamId = 0;
            BaseInfo = null;
            GeneralSkillTeams.Clear();
            SkillTeams.Clear();
            WearEquips.Clear();
            ArcResonances.Clear();
            WearFusionGems.Clear();
            Talents.Clear();
            CityWallLevel = 0;
            Weapons.Clear();
            EquippedWeaponConfigId = 0;
            Skins.Clear();
            EquippedSkinConfigId = 0;
            CityWallSkills.Clear();
            CityWallSlotSkills.Clear();
            HasGreedyAdvanceCard = false;

            if (detail == null)
            {
                LogTool.LogError("[OpponentBattleContext] opponentInfo 响应为空");
                return;
            }

            BaseInfo = detail.BaseInfo;
            BuildCards(detail);
            BuildSoldierTeams(detail);
            BuildSkillTeams(detail);
            BuildGeneralSkills(detail);
            BuildWearEquips(detail);
            BuildArcResonances(detail);
            BuildFusionGems(detail);
            BuildTalents(detail);
            BuildCityWall(detail);
            BuildWeapons(detail);
            BuildSkins(detail);

            var name = BaseInfo != null ? BaseInfo.PlayerName : "";
            LogTool.Log(
                $"[OpponentBattleContext] 已从 opponentInfo 初始化: name={name}, cards={Cards.Count}, soldiers={SoldierTeams.Count}, teamId={ActiveTeamId}");
        }

        public PlayerCard GetCardById(int cardId)
        {
            if (Cards.TryGetValue(cardId, out var card))
            {
                return card;
            }

            return null;
        }

        public OpponentBattleTeamBuildResult ToTeamBuildResult()
        {
            return new OpponentBattleTeamBuildResult
            {
                SoldierTeams = new List<BattleCardGroup>(SoldierTeams),
                CardsPosition = new List<int>(CardsPosition),
            };
        }

        private void BuildCards(Cardwar.PlayerProfile detail)
        {
            if (detail.Cards == null)
            {
                return;
            }

            for (int i = 0; i < detail.Cards.Count; i++)
            {
                var protoCard = detail.Cards[i];
                if (protoCard == null || protoCard.CardId <= 0)
                {
                    continue;
                }

                try
                {
                    var local = ProtoMappers.MapPlayerCard(protoCard);
                    if (local == null)
                    {
                        continue;
                    }

                    Cards[local.CardId] = local;
                }
                catch (System.Exception ex)
                {
                    LogTool.LogWarning(
                        $"[OpponentBattleContext] 跳过无效卡牌 cardId={protoCard.CardId}: {ex.Message}");
                }
            }
        }

        private void BuildSoldierTeams(Cardwar.PlayerProfile detail)
        {
            if (detail.Teams == null || detail.Teams.Count == 0)
            {
                LogTool.LogError("[OpponentBattleContext] opponentInfo.Teams 为空");
                TryBuildSoldiersFromCardBag();
                return;
            }

            Cardwar.PlayerTeam teamProto = null;
            for (int i = 0; i < detail.Teams.Count; i++)
            {
                if (detail.Teams[i].CurrentUse == 1)
                {
                    teamProto = detail.Teams[i];
                    break;
                }
            }

            if (teamProto == null)
            {
                teamProto = detail.Teams[0];
            }

            ActiveTeamId = teamProto.TeamId;
            var cardIds = DeserializeIntList(teamProto.Cards);
            CardsPosition = DeserializeIntList(teamProto.CardsPosition);

            AddSoldiersFromCardIds(cardIds);

            // Teams.Cards 若无士兵，回退 HeroTeam
            if (SoldierTeams.Count == 0)
            {
                var heroIds = ParseHeroCardIds(teamProto.HeroTeam);
                if (heroIds.Count > 0)
                {
                    LogTool.LogWarning("[OpponentBattleContext] Teams.Cards 无士兵，改用 HeroTeam");
                    AddSoldiersFromCardIds(heroIds);
                }
            }

            // 仍为空：用 Cards 背包里所有士兵卡
            if (SoldierTeams.Count == 0)
            {
                TryBuildSoldiersFromCardBag();
            }

            if (SoldierTeams.Count == 0)
            {
                LogTool.LogError("[OpponentBattleContext] opponentInfo 编队无士兵卡");
            }
        }

        private void TryBuildSoldiersFromCardBag()
        {
            var ids = new List<int>();
            foreach (var kv in Cards)
            {
                var cfg = DataConfigManager.Tables.TbCardBaseConfig.GetOrDefault(kv.Key);
                if (cfg != null && cfg.Type == CardType.Soldier)
                {
                    ids.Add(kv.Key);
                }
            }

            if (ids.Count == 0)
            {
                return;
            }

            LogTool.LogWarning($"[OpponentBattleContext] 从 Cards 列表回退构建士兵 count={ids.Count}");
            AddSoldiersFromCardIds(ids);
        }

        private void AddSoldiersFromCardIds(List<int> cardIds)
        {
            if (cardIds == null)
            {
                return;
            }

            var soldierIdx = SoldierTeams.Count;
            for (int i = 0; i < cardIds.Count; i++)
            {
                var cardId = cardIds[i];
                var cardCfg = DataConfigManager.Tables.TbCardBaseConfig.GetOrDefault(cardId);
                if (cardCfg == null || cardCfg.Type != CardType.Soldier)
                {
                    continue;
                }

                int level = 1;
                var cardInfo = GetCardById(cardId);
                if (cardInfo != null && cardInfo.Lv > 0)
                {
                    level = cardInfo.Lv;
                }

                var cardGroup = new BattleCardGroup(cardId, level, cardCfg.Type, 0)
                {
                    IsUnlock =
                    {
                        Value = true
                    }
                };
                cardGroup.CardSlotIndex = soldierIdx;
                var soldierCfg = DataConfigManager.Tables.TbSoldierBattleConfig.GetOrDefault(cardId);
                if (soldierCfg != null)
                {
                    cardGroup.MainAttrType = soldierCfg.MainAttrType;
                }

                SoldierTeams.Add(cardGroup);
                soldierIdx++;
            }
        }

        private static List<int> ParseHeroCardIds(string heroTeamJson)
        {
            var result = new List<int>();
            if (string.IsNullOrEmpty(heroTeamJson))
            {
                return result;
            }

            try
            {
                var heroes = JsonConvert.DeserializeObject<List<HeroTeam>>(heroTeamJson);
                if (heroes == null)
                {
                    return result;
                }

                for (int i = 0; i < heroes.Count; i++)
                {
                    if (heroes[i] != null && heroes[i].HeroCardId > 0)
                    {
                        result.Add(heroes[i].HeroCardId);
                    }
                }
            }
            catch (System.Exception ex)
            {
                LogTool.LogError("[OpponentBattleContext] 解析 HeroTeam 失败: " + ex.Message);
            }

            return result;
        }

        /// <summary>
        /// 汇总对侧养成属性（装备 + 奥数卡 + 奥数共鸣 + 融合宝石 + 天赋 + 城墙 + 城墙槽位技能 + 武器 + 皮肤），供对侧英灵加成使用。
        /// 仅统计数值类属性（不含 Module/LearnSkillRule/LearnConditionBuff，避免作为基础值误写）。
        /// </summary>
        public AttributeAggregation BuildBattleAttributes()
        {
            var agg = new AttributeAggregation();

            // 装备：词条 + 临时词条
            foreach (var equip in WearEquips)
            {
                CollectEquipEntryAttrs(equip.Entrys, agg);
                CollectEquipEntryAttrs(equip.TempEntrys, agg);
            }

            // 装备：特效 + 临时特效
            foreach (var equip in WearEquips)
            {
                CollectEquipEffectAttrs(equip.Effects, agg);
                CollectEquipEffectAttrs(equip.TempEffects, agg);
            }

            // 奥数卡（通用技能）：等级属性 + 主属性
            foreach (var skill in GeneralSkillTeams)
            {
                var specialCfg = DataConfigManager.Tables.TbSpecialEffectsConfig.GetOrDefault(skill.CardId);
                if (specialCfg == null || specialCfg.LevelPropertyList == null || specialCfg.LevelPropertyList.Count == 0)
                {
                    continue;
                }

                int level = skill.CardLevel <= 0 ? 1 : skill.CardLevel;
                if (level > specialCfg.LevelPropertyList.Count)
                {
                    level = specialCfg.LevelPropertyList.Count;
                }

                var lvCfg = specialCfg.LevelPropertyList[level - 1];
                if (lvCfg == null)
                {
                    continue;
                }

                AddOpponentSkillAttrs(agg, lvCfg.Attributes);
                agg.MainAttributes.AddRange(lvCfg.MainAttributes);
            }

            // 奥数共鸣
            foreach (var resonance in ArcResonances)
            {
                if (resonance.Config == null || resonance.Level <= 0)
                {
                    continue;
                }

                var levelCfg = resonance.GetLevelCfg();
                if (levelCfg == null || levelCfg.Attributes == null || levelCfg.Attributes.Count == 0)
                {
                    continue;
                }

                agg.Attributes.AddRangeBaseValue(levelCfg.Attributes);
                agg.SubAttributes.AddRange(levelCfg.Attributes);
            }

            // 融合宝石（佩戴在装备上，对齐玩家侧 Equip 佩戴宝石规则）
            CollectWearFusionGemAttrs(agg);

            // 天赋（对齐玩家侧 Talent.GetAttributes）
            CollectTalentAttrs(agg);

            // 城墙（对齐玩家侧 SeatService.GetWallAttributes）
            CollectCityWallAttrs(agg);

            // 武器（对齐玩家侧 WeaponService.GetAttributes）
            CollectWeaponAttrs(agg);

            // 皮肤（对齐玩家侧 SpeakerService.GetAttributes）
            CollectSkinAttrs(agg);

            // 城墙槽位技能（对齐玩家侧 SeatService.GetEquippedSlotSkillAttributes）
            CollectCityWallSlotSkillAttrs(agg);

            LogTool.Log(
                $"[OpponentBattleContext] 对侧养成属性汇总 attrs={agg.Attributes.GetAll().Count}, main={agg.MainAttributes.Count}, equips={WearEquips.Count}, generalSkills={GeneralSkillTeams.Count}, arcResonances={ArcResonances.Count}, fusionGems={WearFusionGems.Count}, talents={Talents.Count}, wall={CityWallLevel}, weapons={Weapons.Count}, skins={Skins.Count}, wallSkills={CityWallSkills.Count}");
            return agg;
        }

        private static void CollectEquipEntryAttrs(List<int> ids, AttributeAggregation agg)
        {
            if (ids == null)
            {
                return;
            }

            foreach (var id in ids)
            {
                var cfg = DataConfigManager.Tables.TbEquipEntryConfig.GetOrDefault(id);
                if (cfg != null)
                {
                    AddOpponentNonModuleAttrs(agg, cfg.Attributes);
                }
            }
        }

        private static void CollectEquipEffectAttrs(List<int> ids, AttributeAggregation agg)
        {
            if (ids == null)
            {
                return;
            }

            foreach (var id in ids)
            {
                var cfg = DataConfigManager.Tables.TbEquipEffectsConfig.GetOrDefault(id);
                if (cfg != null)
                {
                    AddOpponentNonModuleAttrs(agg, cfg.Attributes);
                }
            }
        }

        /// <summary>装备数值属性：跳过 Module 类（如 LearnSkillRule），避免作为英灵基础值误写。</summary>
        private static void AddOpponentNonModuleAttrs(AttributeAggregation agg, List<AttributeStruct> attrs)
        {
            if (attrs == null || attrs.Count == 0)
            {
                return;
            }

            var filtered = new List<AttributeStruct>();
            foreach (var attr in attrs)
            {
                if (Equip.IsModuleCategoryAttribute(attr.Type))
                {
                    continue;
                }

                filtered.Add(attr);
            }

            if (filtered.Count > 0)
            {
                agg.Attributes.AddRangeBaseValue(filtered);
                agg.SubAttributes.AddRange(filtered);
            }
        }

        /// <summary>奥数卡属性：跳过条件Buff/技能规则类，仅统计数值属性。</summary>
        private static void AddOpponentSkillAttrs(AttributeAggregation agg, List<AttributeStruct> attrs)
        {
            if (attrs == null || attrs.Count == 0)
            {
                return;
            }

            var filtered = new List<AttributeStruct>();
            foreach (var attr in attrs)
            {
                if (attr.Type == AttributeType.LearnConditionBuff || attr.Type == AttributeType.LearnSkillRule)
                {
                    continue;
                }

                filtered.Add(attr);
            }

            if (filtered.Count > 0)
            {
                agg.Attributes.AddRangeBaseValue(filtered);
                agg.SubAttributes.AddRange(filtered);
            }
        }

        /// <summary>解析对侧出战编队（CurrentUse==1，否则取第一套）。</summary>
        private static Cardwar.PlayerTeam ResolveActiveTeam(Cardwar.PlayerProfile detail)
        {
            if (detail.Teams == null || detail.Teams.Count == 0)
            {
                return null;
            }

            for (int i = 0; i < detail.Teams.Count; i++)
            {
                if (detail.Teams[i].CurrentUse == 1)
                {
                    return detail.Teams[i];
                }
            }

            return detail.Teams[0];
        }

        /// <summary>解析对侧出战编队中的主动/被动技能卡（作用于 ActsOnCard 指定的英灵）。</summary>
        private void BuildSkillTeams(Cardwar.PlayerProfile detail)
        {
            SkillTeams.Clear();

            var teamProto = ResolveActiveTeam(detail);
            if (teamProto == null)
            {
                return;
            }

            var cardIds = DeserializeIntList(teamProto.Cards);
            foreach (var cardId in cardIds)
            {
                var cardCfg = DataConfigManager.Tables.TbCardBaseConfig.GetOrDefault(cardId);
                if (cardCfg == null || (cardCfg.Type != CardType.Skill && cardCfg.Type != CardType.SkillPassive))
                {
                    continue;
                }

                int level = 1;
                var cardInfo = GetCardById(cardId);
                if (cardInfo != null && cardInfo.Lv > 0)
                {
                    level = cardInfo.Lv;
                }

                SkillTeams.Add(new BattleCardGroup(cardId, level, cardCfg.Type, SkillTeams.Count));
            }

            LogTool.Log($"[OpponentBattleContext] 对侧出战技能卡: count={SkillTeams.Count}");
        }

        /// <summary>从出战编队解析奥数卡（通用技能），并检测贪婪预支卡。</summary>
        private void BuildGeneralSkills(Cardwar.PlayerProfile detail)
        {
            GeneralSkillTeams.Clear();
            HasGreedyAdvanceCard = false;

            var teamProto = ResolveActiveTeam(detail);
            if (teamProto == null)
            {
                return;
            }

            var cardIds = DeserializeIntList(teamProto.Cards);
            foreach (var cardId in cardIds)
            {
                var cardCfg = DataConfigManager.Tables.TbCardBaseConfig.GetOrDefault(cardId);
                if (cardCfg == null || cardCfg.Type != CardType.GeneralSkill)
                {
                    continue;
                }

                int level = 1;
                var cardInfo = GetCardById(cardId);
                if (cardInfo != null && cardInfo.Lv > 0)
                {
                    level = cardInfo.Lv;
                }

                GeneralSkillTeams.Add(new BattleCardGroup(cardId, level, cardCfg.Type, GeneralSkillTeams.Count));
            }

            foreach (var skill in GeneralSkillTeams)
            {
                var specialCfg = DataConfigManager.Tables.TbSpecialEffectsConfig.GetOrDefault(skill.CardId);
                if (specialCfg != null && specialCfg.SpecialEffectType == SpecialEffectType.GreedyAdvancePayment)
                {
                    HasGreedyAdvanceCard = true;
                    break;
                }
            }
        }

        /// <summary>解析对侧佩戴装备（本地模型，无效装备跳过）。</summary>
        private void BuildWearEquips(Cardwar.PlayerProfile detail)
        {
            WearEquips.Clear();
            if (detail.Equips == null || detail.Equips.Count == 0)
            {
                return;
            }

            for (int i = 0; i < detail.Equips.Count; i++)
            {
                var proto = detail.Equips[i];
                if (proto == null || proto.EquipId <= 0)
                {
                    continue;
                }

                try
                {
                    var model = ProtoMappers.MapPlayerEquip(proto);
                    if (model == null)
                    {
                        continue;
                    }

                    WearEquips.Add(new FusionContinent.Model.PlayerEquip(model));
                }
                catch (System.Exception ex)
                {
                    LogTool.LogWarning($"[OpponentBattleContext] 跳过无效装备 equipId={proto.EquipId}: {ex.Message}");
                }
            }
        }

        /// <summary>解析对侧奥数共鸣（本地模型，等级大于 0 才生效）。</summary>
        private void BuildArcResonances(Cardwar.PlayerProfile detail)
        {
            ArcResonances.Clear();
            if (detail.ArcResonances == null || detail.ArcResonances.Count == 0)
            {
                return;
            }

            for (int i = 0; i < detail.ArcResonances.Count; i++)
            {
                var proto = detail.ArcResonances[i];
                if (proto == null || proto.ResonanceId <= 0 || proto.Level <= 0)
                {
                    continue;
                }

                var resonance = new FusionContinent.Model.PlayerArcResonance
                {
                    ResonanceId = proto.ResonanceId,
                    Slot = proto.Slot,
                    Level = proto.Level,
                    Stage = proto.Stage,
                };
                resonance.Config = DataConfigManager.Tables.TbArcResonanceConfig.GetOrDefault(proto.ResonanceId);
                if (resonance.Config == null)
                {
                    continue;
                }

                ArcResonances.Add(resonance);
            }
        }

        /// <summary>解析对侧佩戴在装备上的融合宝石（本地模型）。</summary>
        private void BuildFusionGems(Cardwar.PlayerProfile detail)
        {
            WearFusionGems.Clear();
            if (detail.FusionGems == null)
            {
                return;
            }

            for (int i = 0; i < detail.FusionGems.Count; i++)
            {
                var proto = detail.FusionGems[i];
                if (proto == null || proto.GemId <= 0 || proto.WearerId <= 0 || proto.Level <= 0)
                {
                    continue;
                }

                var gem = new FusionContinent.Model.PlayerFusionGem
                {
                    Id = proto.Id,
                    GemId = proto.GemId,
                    WearerId = proto.WearerId,
                    Level = proto.Level,
                };
                gem.Config = DataConfigManager.Tables.TbGemFusionConfig.GetOrDefault(proto.GemId);
                if (gem.Config == null)
                {
                    continue;
                }

                gem.Quality = gem.Config.Quality;
                WearFusionGems.Add(gem);
            }
        }

        /// <summary>解析对侧天赋列表（含使用中的天赋）。</summary>
        private void BuildTalents(Cardwar.PlayerProfile detail)
        {
            Talents.Clear();
            if (detail.Talents == null)
            {
                return;
            }

            for (int i = 0; i < detail.Talents.Count; i++)
            {
                var proto = detail.Talents[i];
                if (proto == null || proto.TalentId <= 0)
                {
                    continue;
                }

                Talents.Add(new FusionContinent.Model.PlayerTalent
                {
                    Id = proto.Id,
                    TalentId = proto.TalentId,
                    Lv = proto.Lv,
                    ExtraLv = proto.ExtraLv,
                    Status = proto.Status,
                });
            }
        }

        /// <summary>解析对侧城墙：等级 + 城墙技能（SkillId → 等级）+ 槽位技能（顺序列表）。</summary>
        private void BuildCityWall(Cardwar.PlayerProfile detail)
        {
            CityWallLevel = 0;
            CityWallSkills.Clear();
            CityWallSlotSkills.Clear();
            if (detail.CityWall == null)
            {
                return;
            }

            if (detail.CityWall.Wall != null)
            {
                CityWallLevel = detail.CityWall.Wall.Level;
            }

            if (detail.CityWall.Skills != null)
            {
                for (int i = 0; i < detail.CityWall.Skills.Count; i++)
                {
                    var proto = detail.CityWall.Skills[i];
                    if (proto == null || proto.SkillId <= 0)
                    {
                        continue;
                    }

                    CityWallSkills[proto.SkillId] = proto.Level;
                }
            }

            if (detail.CityWall.SlotSkills != null)
            {
                for (int i = 0; i < detail.CityWall.SlotSkills.Count; i++)
                {
                    CityWallSlotSkills.Add(detail.CityWall.SlotSkills[i]);
                }
            }
        }

        /// <summary>解析对侧武器列表与当前穿戴武器配置 Id。</summary>
        private void BuildWeapons(Cardwar.PlayerProfile detail)
        {
            Weapons.Clear();
            EquippedWeaponConfigId = 0;
            if (detail.SpeakerModule != null)
            {
                EquippedWeaponConfigId = detail.SpeakerModule.EquippedWeaponId;
            }

            if (detail.Weapons == null)
            {
                return;
            }

            for (int i = 0; i < detail.Weapons.Count; i++)
            {
                var proto = detail.Weapons[i];
                if (proto == null || proto.WeaponId <= 0)
                {
                    continue;
                }

                Weapons.Add(new FusionContinent.Model.PlayerWeapon
                {
                    Id = proto.Id,
                    WeaponId = proto.WeaponId,
                    Level = proto.Level,
                });
            }
        }

        /// <summary>解析对侧皮肤列表与当前穿戴皮肤配置 Id。</summary>
        private void BuildSkins(Cardwar.PlayerProfile detail)
        {
            Skins.Clear();
            EquippedSkinConfigId = 0;
            if (detail.SpeakerModule != null)
            {
                EquippedSkinConfigId = detail.SpeakerModule.UseSkinId;
            }

            if (detail.Skins == null)
            {
                return;
            }

            for (int i = 0; i < detail.Skins.Count; i++)
            {
                var proto = detail.Skins[i];
                if (proto == null || proto.SkinId <= 0)
                {
                    continue;
                }

                Skins.Add(new FusionContinent.Model.PlayerCycleSpeakerSkin
                {
                    Id = proto.Id,
                    SkinId = proto.SkinId,
                    Level = proto.Level,
                });
            }
        }

        /// <summary>融合宝石属性：主属性 + 数值属性（跳 Module），与玩家侧佩戴宝石规则一致。</summary>
        private void CollectWearFusionGemAttrs(AttributeAggregation agg)
        {
            foreach (var gem in WearFusionGems)
            {
                if (gem == null || gem.Config == null || gem.Level <= 0
                    || gem.Config.LevelList == null || gem.Level > gem.Config.LevelList.Count)
                {
                    continue;
                }

                var levelCfg = gem.GetLevelCfg();
                if (levelCfg == null)
                {
                    continue;
                }

                agg.MainAttributes.AddRange(levelCfg.MainAttributes);
                AddOpponentNonModuleAttrs(agg, levelCfg.Attributes);
            }
        }

        /// <summary>天赋属性：基础 + 额外（ExtraLv），与玩家侧 Talent.GetAttributes 一致。</summary>
        private void CollectTalentAttrs(AttributeAggregation agg)
        {
            var talent = Talents.FirstOrDefault(
                v => v != null && v.Status == FusionContinent.Model.PlayerTalent.STATUS_IN_USE);
            if (talent == null || talent.TalentId <= 0)
            {
                return;
            }

            var cfg = DataConfigManager.Tables.TbTalentConfig.GetOrDefault(talent.TalentId);
            if (cfg == null || cfg.TalentList == null || cfg.TalentList.Count == 0)
            {
                return;
            }

            for (int i = 0; i < cfg.TalentList.Count; i++)
            {
                var levelCfg = cfg.TalentList[i];
                if (talent.Lv < levelCfg.Level)
                {
                    continue;
                }

                AddOpponentNonModuleAttrs(agg, levelCfg.Attributes);
                agg.MainAttributes.AddRange(levelCfg.MainAttributes);

                if (talent.ExtraLv >= levelCfg.Level
                    && levelCfg.ExtraAttributes != null
                    && levelCfg.ExtraAttributes.Count > 0)
                {
                    AddOpponentNonModuleAttrs(agg, levelCfg.ExtraAttributes);
                    agg.MainAttributes.AddRange(levelCfg.ExtraMainAttributes);
                }
            }
        }

        /// <summary>城墙属性：当前城墙等级基础属性，与玩家侧 SeatService.GetWallAttributes 一致。</summary>
        private void CollectCityWallAttrs(AttributeAggregation agg)
        {
            if (CityWallLevel <= 0)
            {
                return;
            }

            var levelCfg = DataConfigManager.Tables.TbHeroSeatLevelConfig.GetOrDefault(CityWallLevel);
            if (levelCfg == null)
            {
                return;
            }

            AddOpponentNonModuleAttrs(agg, levelCfg.BaseAttributes);
        }

        /// <summary>武器属性：当前穿戴武器星级基础 + 额外属性，与玩家侧 WeaponService.GetAttributes 一致。</summary>
        private void CollectWeaponAttrs(AttributeAggregation agg)
        {
            if (EquippedWeaponConfigId <= 0)
            {
                return;
            }

            var weapon = Weapons.FirstOrDefault(v => v != null && v.WeaponId == EquippedWeaponConfigId);
            if (weapon == null || weapon.Level <= 0)
            {
                return;
            }

            var cfg = DataConfigManager.Tables.TbPlayerWeaponConfig.GetOrDefault(EquippedWeaponConfigId);
            if (cfg == null || cfg.WeaponStarList == null || cfg.WeaponStarList.Count == 0)
            {
                return;
            }

            var levelCfg = cfg.WeaponStarList.FirstOrDefault(v => v.Level == weapon.Level)
                           ?? cfg.WeaponStarList[0];
            AddOpponentNonModuleAttrs(agg, levelCfg.BaseAttributes);
            AddOpponentNonModuleAttrs(agg, levelCfg.ExtraAttributes);
        }

        /// <summary>皮肤属性：当前穿戴皮肤星级基础 + 额外属性，与玩家侧 SpeakerService.GetAttributes 一致。</summary>
        private void CollectSkinAttrs(AttributeAggregation agg)
        {
            if (EquippedSkinConfigId <= 0)
            {
                return;
            }

            var skin = Skins.FirstOrDefault(v => v != null && v.SkinId == EquippedSkinConfigId && v.Level > 0);
            if (skin == null)
            {
                return;
            }

            var cfg = DataConfigManager.Tables.TbPlayerFigureConfig.GetOrDefault(EquippedSkinConfigId);
            if (cfg == null || cfg.ImageStarList == null || cfg.ImageStarList.Count == 0)
            {
                return;
            }

            var levelCfg = cfg.ImageStarList.FirstOrDefault(v => v.Level == skin.Level)
                           ?? cfg.ImageStarList[0];
            AddOpponentNonModuleAttrs(agg, levelCfg.BaseAttributes);
            AddOpponentNonModuleAttrs(agg, levelCfg.ExtraAttributes);
        }

        /// <summary>城墙槽位技能属性：已解锁槽位且已激活的技能按当前等级汇总，与玩家侧 SeatService.GetEquippedSlotSkillAttributes 一致。</summary>
        private void CollectCityWallSlotSkillAttrs(AttributeAggregation agg)
        {
            if (CityWallLevel <= 0)
            {
                return;
            }

            var wallLevelCfgs = DataConfigManager.Tables.TbHeroSeatLevelConfig.DataList;
            if (wallLevelCfgs == null || wallLevelCfgs.Count == 0)
            {
                return;
            }

            // 槽位解锁等级：slotIndex 对应第 slotIndex + 1 条 UnLockSeat &gt; 0 的等级配置（与玩家侧 GetSeatSlotUnlockLevel 一致）
            var slotUnlockLevels = new List<int>();
            foreach (var cfg in wallLevelCfgs)
            {
                if (cfg != null && cfg.UnLockSeat > 0)
                {
                    slotUnlockLevels.Add(cfg.Level);
                }
            }

            for (int i = 0; i < slotUnlockLevels.Count; i++)
            {
                if (CityWallLevel < slotUnlockLevels[i] || i >= CityWallSlotSkills.Count)
                {
                    continue;
                }

                var skillId = CityWallSlotSkills[i];
                if (skillId <= 0)
                {
                    continue;
                }

                if (!CityWallSkills.TryGetValue(skillId, out var skillLevel) || skillLevel <= 0)
                {
                    continue;
                }

                var seatCfg = DataConfigManager.Tables.TbHeroSeatConfig.GetOrDefault(skillId);
                if (seatCfg == null || seatCfg.SeatLevelList == null || seatCfg.SeatLevelList.Count == 0)
                {
                    continue;
                }

                var levelCfg = seatCfg.SeatLevelList.FirstOrDefault(v => v != null && v.Level == skillLevel);
                if (levelCfg == null)
                {
                    continue;
                }

                AddOpponentNonModuleAttrs(agg, levelCfg.BaseAttributes);
                AddOpponentNonModuleAttrs(agg, levelCfg.ExtraAttributes);
            }
        }

        private static List<int> DeserializeIntList(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return new List<int>();
            }

            try
            {
                return JsonConvert.DeserializeObject<List<int>>(json) ?? new List<int>();
            }
            catch (System.Exception ex)
            {
                LogTool.LogError("[OpponentBattleContext] 解析 int 列表失败 json=" + json + " err=" + ex.Message);
                return new List<int>();
            }
        }
    }
}



