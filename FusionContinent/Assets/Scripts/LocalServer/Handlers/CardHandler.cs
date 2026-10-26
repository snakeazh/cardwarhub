using System;
using System.Collections.Generic;
using Cardwar;
using Config;
using Newtonsoft.Json;

namespace FusionContinent.LocalServer
{
    /// <summary>
    /// 卡牌模块 Handler，逐行对照：
    /// - Server/app/api/application/application_card.go（升级/分解/合成）
    /// - Server/app/api/application/cardapplication/application_card.go（皮肤/养成奖励/英灵激活）
    /// - Server/app/api/application/cardapplication/application_unlock_skill_card.go（解锁技能卡）
    /// 存档读写对照 Server/app/model/CardModel/PlayerCardModel.go（内存 Archive.Cards 替代 MySQL）。
    /// 业务校验失败抛 LocalServerException，消息逐字复刻 Go panic 文案；事务/行锁不迁移。
    /// 产品改动（无 Go 对照）：技能升级已移除，技能等级跟随英灵等级同步提升（SyncSoldierSkillLevels），
    /// LevelUpCard 的技能卡升级分支与 CheckCardLevelUpCondition 随之删除。
    /// </summary>
    public class CardHandler
    {
        // ============================================================
        // 卡牌升级 api/card/levelUp
        // 对照 application_card.go:31-153 LevelUpCardHandlerLogic
        // ============================================================
        public LevelUpCardResponse LevelUpCard(LocalServerContext ctx, LevelUpCardRequest req)
        {
            Tables tables = DataConfigManager.Tables;
            int cardId = req.CardId;

            CardDevelopConfig cardCfg = tables.TbCardDevelopConfig.GetOrDefault(cardId);
            if (cardCfg == null)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 卡牌不能养成");
                throw new LocalServerException("卡牌不能养成");
            }

            // Go 此处 cardConfig 为 nil 时 IsAsCard 内解引用 panic，本地 fail fast 同样直接抛
            CardBaseConfig cardConfig = tables.TbCardBaseConfig.GetOrDefault(cardId);
            if (cardConfig.Type == CardType.GeneralSkill) // cfgutil.IsAsCard
            {
                LogTool.LogWarning($"userId={ctx.UserId} 奥数卡不能养成");
                throw new LocalServerException("奥数卡不能养成");
            }

            // 新语义（单机化后产品改动，无 Go 对照）：技能升级已移除，技能等级跟随英灵等级同步提升。
            // 技能卡走本接口一律拒绝（防御性，UI 已不再发起）
            if (cardConfig.Type == CardType.Skill || cardConfig.Type == CardType.SkillPassive)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 技能等级跟随英灵等级提升 cardId={cardId}");
                throw new LocalServerException("技能等级跟随英灵等级提升");
            }

            PlayerCard card = GetCardByCardId(ctx, cardId);
            if (card == null)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 未解锁");
                throw new LocalServerException("未解锁");
            }

            if (card.Lv >= cardCfg.LevelPropertyList.Count)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 已满级");
                throw new LocalServerException("已满级");
            }

            int heroAwakingLevel = tables.TbPlayerConst.HeroAwakingLevel;
            // 对照 :59-62 card.Data != "" 才反序列化；proto 结构体老数据可能为空需判空
            bool isAwaked = card.Data != null && card.Data.IsAwaked;
            bool isSoldier = cardConfig.Type == CardType.Soldier; // cfgutil.CardCfgIsSoldierCard

            var items = new Dictionary<int, long>();
            ResultItems rsItems;

            // 觉醒卡点：英灵卡在觉醒等级且未觉醒 → 只能执行觉醒，不能升级（:72-81）
            if (isSoldier && heroAwakingLevel > 0 && card.Lv == heroAwakingLevel && !isAwaked)
            {
                if (req.ConsumeItems != "")
                {
                    LogTool.LogWarning($"userId={ctx.UserId} 需要先觉醒才能继续升级");
                    throw new LocalServerException("需要先觉醒才能继续升级");
                }
                DevelopLevelPropertyList lvCfg = cardCfg.LevelPropertyList[card.Lv - 1];
                rsItems = ExecAwaking(ctx, card, lvCfg);
            }
            else if (req.ConsumeItems != "")
            {
                // 英灵卡升级（:85-98）
                DevelopLevelPropertyList lvCfg = cardCfg.LevelPropertyList[card.Lv - 1];

                Dictionary<int, long> needItems = ctx.ItemEntrance.ConsumeToItems(lvCfg.ConsumeItems);
                items = ValidateAndMergeConsumeItems(req.ConsumeItems, needItems, items);

                if (items.Count > 0 && !ctx.ItemEntrance.CheckEnough(items))
                {
                    LogTool.LogWarning($"userId={ctx.UserId} 道具不足");
                    throw new LocalServerException("道具不足");
                }
                if (lvCfg.TakeRewards.Count > 0)
                {
                    MergeMapsAndSum(items, ctx.ItemEntrance.RewardsToItems(lvCfg.TakeRewards));
                }
                card.Lv++;
                // 对照 :132 card.TransactionSave(tx)
                ctx.Store.MarkDirty(PlayerArchive.CardsFieldNumber);
                rsItems = ctx.ItemEntrance.Execute(items);
            }
            else
            {
                // 新语义（无 Go 对照）：技能升级分支已删除；空消耗且非觉醒场景的请求均为旧技能升级/非法请求
                LogTool.LogWarning($"userId={ctx.UserId} 技能等级跟随英灵等级提升 cardId={cardId}");
                throw new LocalServerException("技能等级跟随英灵等级提升");
            }

            // 新语义（无 Go 对照）：技能等级跟随英灵等级，英灵升级/觉醒后同步向上补齐已拥有技能卡，
            // 变动的技能卡填进 ResultItems.Cards 随响应下发（客户端 Items.Update 的 cards 段回流 Global.PlayerCards）
            List<PlayerCard> syncedSkills = SyncSoldierSkillLevels(ctx, card);
            foreach (PlayerCard skillCard in syncedSkills)
            {
                rsItems.Cards[skillCard.CardId] = skillCard;
            }

            // 刷新英灵或者技能战力（:141-151）
            string moduleName = tables.TbSoldierBattleConfig.GetOrDefault(card.CardId) != null
                ? CombatPowerRefresher.ModuleSoldier
                : CombatPowerRefresher.ModuleSkill;
            long cp = ctx.CombatPower.RefreshModule(moduleName);
            if (syncedSkills.Count > 0)
            {
                // 新语义：技能卡等级随英灵提升，技能模块战力一并重算（RefreshModule 返回最新总战力）
                cp = ctx.CombatPower.RefreshModule(CombatPowerRefresher.ModuleSkill);
            }
            rsItems.CombatPower = new CombatPower { CombatPower_ = cp };

            return new LevelUpCardResponse { Card = card, Items = rsItems };
        }

        // 对照 application_card.go:156-177 validateAndMergeConsumeItems（校验上报消耗与配置一致）
        private Dictionary<int, long> ValidateAndMergeConsumeItems(string consumeItemsJson, Dictionary<int, long> needItems, Dictionary<int, long> items)
        {
            if (needItems.Count == 0)
            {
                return items;
            }
            Dictionary<int, long> consumeItems;
            try
            {
                consumeItems = JsonConvert.DeserializeObject<Dictionary<int, long>>(consumeItemsJson);
            }
            catch (Exception e)
            {
                LogTool.LogWarning($"json解析失败: {e.Message}");
                throw new LocalServerException("json解析失败: " + e.Message);
            }
            int submitCount = consumeItems == null ? 0 : consumeItems.Count; // Go 对 "null" 反序列化得 nil map，len=0
            if (submitCount != needItems.Count)
            {
                LogTool.LogWarning("消耗道具与配置不匹配");
                throw new LocalServerException("消耗道具与配置不匹配");
            }
            foreach (var kv in needItems)
            {
                long submitNum;
                bool ok = consumeItems.TryGetValue(kv.Key, out submitNum);
                if (!ok || Math.Abs(submitNum) != Math.Abs(kv.Value))
                {
                    LogTool.LogWarning("消耗道具与配置不匹配");
                    throw new LocalServerException("消耗道具与配置不匹配");
                }
            }
            return MergeMapsAndSum(items, needItems);
        }

        // 对照 application_card.go:182-218 execAwaking（觉醒：扣 AwakeningConsume、发 AwakeningRewards、授予觉醒技能卡，等级不变）
        private ResultItems ExecAwaking(LocalServerContext ctx, PlayerCard card, DevelopLevelPropertyList lvCfg)
        {
            Dictionary<int, long> needItems = ctx.ItemEntrance.ConsumeToItems(lvCfg.AwakeningConsume);
            if (needItems.Count == 0)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 觉醒配置错误：无觉醒消耗材料");
                throw new LocalServerException("觉醒配置错误：无觉醒消耗材料");
            }

            var items = new Dictionary<int, long>();
            MergeMapsAndSum(items, needItems);

            if (!ctx.ItemEntrance.CheckEnough(items))
            {
                LogTool.LogWarning($"userId={ctx.UserId} 道具不足");
                throw new LocalServerException("道具不足");
            }

            if (lvCfg.AwakeningRewards.Count > 0)
            {
                MergeMapsAndSum(items, ctx.ItemEntrance.RewardsToItems(lvCfg.AwakeningRewards));
            }

            if (card.Data == null)
            {
                card.Data = new PlayerCard.Types.CardData();
            }
            card.Data.IsAwaked = true;
            // 对照 :204-205 card.TransactionSave(tx)
            ctx.Store.MarkDirty(PlayerArchive.CardsFieldNumber);

            // 觉醒成功后，自动授予觉醒技能卡（:207-215）
            foreach (SpecialEffectsConfig speCfg in DataConfigManager.Tables.TbSpecialEffectsConfig.DataList)
            {
                if (speCfg.AwakingSkill == 1 && speCfg.ActsOnCard == card.CardId)
                {
                    int itemId = GetItemIdByCardId(speCfg.Id);
                    if (itemId > 0)
                    {
                        Accumulate(items, itemId, 1);
                    }
                }
            }

            return ctx.ItemEntrance.Execute(items);
        }

        // 新语义（单机化后产品改动，无 Go 对照）：技能等级跟随英灵等级同步提升，只升不降。
        // 遍历已拥有（Num>0）的 Skill/SkillPassive 卡，TbSpecialEffectsConfig.ActsOnCard == 英灵卡Id 的，
        // Lv 向上提到 min(英灵Lv, TbSpecialEffectsConfig.LevelPropertyList.Count)；返回有变更的技能卡。
        public List<PlayerCard> SyncSoldierSkillLevels(LocalServerContext ctx, PlayerCard soldierCard)
        {
            var changed = new List<PlayerCard>();
            foreach (PlayerCard card in ctx.Store.Archive.Cards)
            {
                if (card.Num <= 0)
                {
                    continue;
                }
                CardBaseConfig cardCfg = DataConfigManager.Tables.TbCardBaseConfig.GetOrDefault(card.CardId);
                if (cardCfg == null || (cardCfg.Type != CardType.Skill && cardCfg.Type != CardType.SkillPassive))
                {
                    continue;
                }
                SpecialEffectsConfig speCfg = DataConfigManager.Tables.TbSpecialEffectsConfig.GetOrDefault(card.CardId);
                if (speCfg == null)
                {
                    LogTool.LogWarning($"userId={ctx.UserId} 技能卡缺少 SpecialEffects 配置，跳过同步 cardId={card.CardId}");
                    continue;
                }
                if (speCfg.ActsOnCard != soldierCard.CardId)
                {
                    continue;
                }
                int targetLv = Math.Min(soldierCard.Lv, speCfg.LevelPropertyList.Count);
                if (card.Lv < targetLv)
                {
                    card.Lv = targetLv;
                    changed.Add(card);
                }
            }
            if (changed.Count > 0)
            {
                ctx.Store.MarkDirty(PlayerArchive.CardsFieldNumber);
            }
            return changed;
        }

        // 新语义（无 Go 对照）：老档迁移用——对所有已拥有英灵做技能等级同步，返回是否有变更
        public bool SyncAllSoldierSkillLevels(LocalServerContext ctx)
        {
            bool changed = false;
            foreach (PlayerCard card in ctx.Store.Archive.Cards)
            {
                if (card.Num <= 0)
                {
                    continue;
                }
                CardBaseConfig cardCfg = DataConfigManager.Tables.TbCardBaseConfig.GetOrDefault(card.CardId);
                if (cardCfg == null || cardCfg.Type != CardType.Soldier)
                {
                    continue;
                }
                if (SyncSoldierSkillLevels(ctx, card).Count > 0)
                {
                    changed = true;
                }
            }
            return changed;
        }

        // ============================================================
        // 设置皮肤 api/card/setSkin
        // 对照 cardapplication/application_card.go:23-51 SetSkin
        // ============================================================
        public SetSkinResponse SetSkin(LocalServerContext ctx, SetSkinRequest req)
        {
            Tables tables = DataConfigManager.Tables;
            CardDevelopConfig cardCfg = tables.TbCardDevelopConfig.GetOrDefault(req.CardId);
            if (cardCfg == null)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 卡牌不能养成");
                throw new LocalServerException("卡牌不能养成");
            }

            SkinConfig skinCfg = tables.TbSkinConfig.GetOrDefault(req.SkinId);
            if (skinCfg == null)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 参数错误");
                throw new LocalServerException("参数错误");
            }

            PlayerCard card = GetCardByCardId(ctx, req.CardId);
            if (card == null)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 未解锁");
                throw new LocalServerException("未解锁");
            }

            if (card.Data == null)
            {
                card.Data = new PlayerCard.Types.CardData();
            }
            if (card.Data.Skin != req.SkinId)
            {
                card.Data.Skin = req.SkinId;
                // 对照 :46-47 card.Save()
                ctx.Store.MarkDirty(PlayerArchive.CardsFieldNumber);
            }

            // 对照 card_converter.go:12-16 toSetSkinResponse：只回 Card，不回 Items
            return new SetSkinResponse { Card = card };
        }

        // ============================================================
        // 领取养成奖励 api/card/receiveDevelopReward
        // 对照 cardapplication/application_card.go:62-112 ReceiveDevelopReward
        // ============================================================
        public ReceiveDevelopRewardResponse ReceiveDevelopReward(LocalServerContext ctx, ReceiveDevelopRewardRequest req)
        {
            Tables tables = DataConfigManager.Tables;
            CardDevelopConfig cardCfg = tables.TbCardDevelopConfig.GetOrDefault(req.CardId);
            if (cardCfg == null)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 卡牌不能养成");
                throw new LocalServerException("卡牌不能养成");
            }

            // Go 此处 cardLv-1 越界即 panic，本地 fail fast 同样直接抛
            DevelopLevelPropertyList lvCfg = cardCfg.LevelPropertyList[req.CardLv - 1];
            if (lvCfg.TakeRewards.Count == 0)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 参数错误");
                throw new LocalServerException("参数错误");
            }

            PlayerCard card = GetCardByCardId(ctx, req.CardId);
            if (card == null || card.Lv < req.CardLv)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 未解锁");
                throw new LocalServerException("未解锁");
            }

            if (card.Data == null)
            {
                card.Data = new PlayerCard.Types.CardData();
            }
            if (card.Data.ReceiveDevelops.Contains(req.CardLv))
            {
                LogTool.LogWarning($"userId={ctx.UserId} 已领取");
                throw new LocalServerException("已领取");
            }

            // 奖励（:89-97，Go 事务失败 panic「领取奖励失败」；本地已前置校验，Execute 直接抛）
            ResultItems rsItems = ctx.ItemEntrance.Execute(ctx.ItemEntrance.RewardsToItems(lvCfg.TakeRewards));

            // 保存领取状态（:99-106）
            card.Data.ReceiveDevelops.Add(req.CardLv);
            ctx.Store.MarkDirty(PlayerArchive.CardsFieldNumber);

            return new ReceiveDevelopRewardResponse { Card = card, Items = rsItems };
        }

        // ============================================================
        // 分解奥术卡 api/card/decompositionCard
        // 对照 application_card.go:260-306 AsCardFragmentHandler + :385-438
        // ============================================================
        public AsCardFragmentResponse DecompositionCard(LocalServerContext ctx, AsCardFragmentRequest req)
        {
            Tables tables = DataConfigManager.Tables;
            var resp = new AsCardFragmentResponse();

            if (req.CardIds.Count == 0)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 没有填写消耗卡");
                throw new LocalServerException("没有填写消耗卡");
            }

            // 校验卡片（:400-438 cardFragmentCheckCardIsOK）
            var reduceCardMap = new Dictionary<int, int>();
            var cardQualityMap = new Dictionary<int, int>();
            foreach (int cid in req.CardIds)
            {
                CardBaseConfig cfg = tables.TbCardBaseConfig.GetOrDefault(cid);
                if (cfg == null)
                {
                    LogTool.LogWarning($"userId={ctx.UserId} 奥数卡信息不存在");
                    throw new LocalServerException("奥数卡信息不存在");
                }
                if (cfg.Type != CardType.GeneralSkill) // !cfgutil.IsAsCard
                {
                    LogTool.LogWarning($"userId={ctx.UserId} 不是奥数卡无法分解");
                    throw new LocalServerException("不是奥数卡无法分解");
                }
                SpecialEffectsConfig specialCfg = tables.TbSpecialEffectsConfig.GetOrDefault(cid);
                if (specialCfg == null)
                {
                    LogTool.LogWarning($"userId={ctx.UserId} 特殊效果信息不存在");
                    throw new LocalServerException("特殊效果信息不存在");
                }
                if (specialCfg.LevelPropertyList.Count == 0)
                {
                    LogTool.LogWarning($"userId={ctx.UserId} 奥数卡等级配置问题");
                    throw new LocalServerException("奥数卡等级配置问题");
                }
                int quality = (int)specialCfg.LevelPropertyList[0].Quality;
                cardQualityMap.TryGetValue(quality, out int qualityNum);
                cardQualityMap[quality] = qualityNum + 1;
                reduceCardMap.TryGetValue(cid, out int reduceNum);
                reduceCardMap[cid] = reduceNum - 1;
            }
            // 逐个判断卡是否充足（:430-437）
            foreach (var kv in reduceCardMap)
            {
                PlayerCard card = GetCardByCardId(ctx, kv.Key);
                if (card == null || card.Num < -kv.Value)
                {
                    LogTool.LogWarning($"userId={ctx.UserId} 卡牌不充足");
                    throw new LocalServerException("卡牌不充足");
                }
            }

            // 计算分解条目（:385-398 calculateCardFragments；注意 Go 为赋值语义，同 key 后者覆盖）
            var itemMap = new Dictionary<int, long>();
            foreach (var kv in cardQualityMap)
            {
                ArcCardProConfig proCfg = tables.TbArcCardProConfig.GetOrDefault((QualityType)kv.Key);
                if (proCfg == null)
                {
                    LogTool.LogWarning("奥数卡pro数据不存在");
                    throw new LocalServerException("奥数卡pro数据不存在");
                }
                foreach (Config.RewardAndNum reward in proCfg.DecomposeRewards)
                {
                    itemMap[reward.Id] = (long)reward.Num * kv.Value;
                }
            }

            // 扣减用户的卡（:289-290 → CardModel.TxChangeCardsNum:166-207，资产流水不迁移）
            foreach (var kv in reduceCardMap)
            {
                PlayerCard card = GetCardByCardId(ctx, kv.Key);
                if (card == null)
                {
                    LogTool.LogWarning($"userId={ctx.UserId} 卡片不存在: cardId={kv.Key}");
                    throw new LocalServerException("卡片不存在");
                }
                card.Num += kv.Value;
                if (card.Num < 0)
                {
                    LogTool.LogWarning($"userId={ctx.UserId} 卡片数量小于0: cardId={kv.Key}");
                    throw new LocalServerException("卡片数量小于0");
                }
            }
            ctx.Store.MarkDirty(PlayerArchive.CardsFieldNumber);

            // 增加分解道具（:293）
            ResultItems rsItems = ctx.ItemEntrance.Execute(itemMap);
            // 对照 :303 ret.Items = rsItems.Items、:290 ret.Cards = cardsRet：回执含变动道具行 + 变动卡牌行
            foreach (var kv in reduceCardMap)
            {
                rsItems.Cards[kv.Key] = GetCardByCardId(ctx, kv.Key);
            }
            resp.Items = rsItems;
            resp.Rewards.Add(itemMap);

            // 如果消耗的卡牌在队伍里面，需要从队伍里面剔除（:296 → TeamService.AsCardFragmentUpdateTeam:119-162）
            RemoveFragmentCardsFromTeams(ctx, reduceCardMap);

            return resp;
        }

        // 对照 TeamService/PlayerTeamService.go:119-162 AsCardFragmentUpdateTeam
        private void RemoveFragmentCardsFromTeams(LocalServerContext ctx, Dictionary<int, int> changeCardMap)
        {
            if (changeCardMap.Count == 0)
            {
                return;
            }
            bool changed = false;
            foreach (PlayerTeam team in ctx.Store.Archive.Teams)
            {
                // 偏差：Go 对空串 json.Unmarshal 会 panic，本地按空编队处理（空串本就无卡可剔）
                if (string.IsNullOrEmpty(team.Cards) && string.IsNullOrEmpty(team.AsCardTeam))
                {
                    continue;
                }

                // cards
                List<int> teamCards = string.IsNullOrEmpty(team.Cards)
                    ? new List<int>()
                    : JsonConvert.DeserializeObject<List<int>>(team.Cards);
                if (teamCards.Count == 0)
                {
                    continue; // 对照 :126-128
                }
                var tempCards = new List<int>();
                foreach (int cid in teamCards)
                {
                    if (changeCardMap.ContainsKey(cid))
                    {
                        continue;
                    }
                    tempCards.Add(cid);
                }

                // as card team
                List<int> asCardIdTeams = new List<int>();
                if (!string.IsNullOrEmpty(team.AsCardTeam))
                {
                    foreach (int cid in JsonConvert.DeserializeObject<List<int>>(team.AsCardTeam))
                    {
                        if (changeCardMap.ContainsKey(cid))
                        {
                            continue;
                        }
                        asCardIdTeams.Add(cid);
                    }
                }

                team.Cards = JsonConvert.SerializeObject(tempCards);
                team.AsCardTeam = JsonConvert.SerializeObject(asCardIdTeams);
                changed = true;
            }
            if (changed)
            {
                ctx.Store.MarkDirty(PlayerArchive.TeamsFieldNumber);
            }
        }

        // ============================================================
        // 合成奥术卡 api/card/compositionCard
        // 对照 application_card.go:308-372 AsCardFusionHandler + :482-518 cardFusionCheckItemIsOKV2
        // ============================================================
        public AsCardFusionResponse CompositionCard(LocalServerContext ctx, AsCardFusionRequest req)
        {
            Tables tables = DataConfigManager.Tables;

            // 对照 :314（QualityType_Ordinary~Ultimate = 1~5）
            if (req.Quality < (int)QualityType.Ordinary || req.Quality > (int)QualityType.Ultimate)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 品质错误");
                throw new LocalServerException("品质错误");
            }

            ArcCardProConfig asProCfg = tables.TbArcCardProConfig.GetOrDefault((QualityType)req.Quality);
            if (asProCfg == null || asProCfg.ComposeRewards.Count == 0)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 奥数卡合成配置错误");
                throw new LocalServerException("奥数卡合成配置错误");
            }

            // 计算加卡候选（:331 → cfgutil.CardCfgGroupAsCardByQuality card_utils.go:46-65）
            var qualitySpeCards = new List<CardBaseConfig>();
            foreach (CardBaseConfig cardCfg in tables.TbCardBaseConfig.DataList)
            {
                if (cardCfg.Type != CardType.GeneralSkill)
                {
                    continue;
                }
                SpecialEffectsConfig speConfig = tables.TbSpecialEffectsConfig.GetOrDefault(cardCfg.Id);
                if (speConfig == null)
                {
                    throw new LocalServerException("奥数卡特殊配置不存在");
                }
                if (speConfig.LevelPropertyList.Count == 0)
                {
                    throw new LocalServerException("奥数卡等级配置问题");
                }
                if ((int)speConfig.LevelPropertyList[0].Quality == req.Quality)
                {
                    qualitySpeCards.Add(cardCfg);
                }
            }
            if (qualitySpeCards.Count == 0)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 品质对应的卡配置错误");
                throw new LocalServerException("品质对应的卡配置错误");
            }

            // 检查用户道具是否充足（:344-348 → cardFusionCheckItemIsOKV2，只合成一次）
            // Go 的 itemIds 直接取自 ComposeRewards，EqualSliceCount 恒等校验（:488-496）为死代码，不迁移
            var userItemMap = new Dictionary<int, PlayerItem>();
            foreach (PlayerItem item in ctx.Store.Archive.Items)
            {
                userItemMap[item.ItemId] = item;
            }
            int ownedCount = 0;
            foreach (Config.RewardAndNum composeCfg in asProCfg.ComposeRewards)
            {
                if (userItemMap.ContainsKey(composeCfg.Id))
                {
                    ownedCount++;
                }
            }
            if (ownedCount == 0)
            {
                LogTool.LogWarning("用户道具不存在");
                throw new LocalServerException("用户道具不存在");
            }
            foreach (Config.RewardAndNum composeCfg in asProCfg.ComposeRewards)
            {
                // 偏差：Go 此处 map 缺 key 是 nil 解引用 panic，本地以同文案 fail fast
                if (!userItemMap.TryGetValue(composeCfg.Id, out PlayerItem userItem))
                {
                    LogTool.LogWarning("用户道具不存在");
                    throw new LocalServerException("用户道具不存在");
                }
                // 至少够一次
                if (userItem.ItemNum < composeCfg.Num)
                {
                    LogTool.LogWarning("依赖道具数量不足");
                    throw new LocalServerException("依赖道具数量不足");
                }
            }

            // 随机一卡（:374-383 asCardFusionGenCards，addCardCount=1；洗牌取首张等价于均匀随机一张）
            CardBaseConfig addCardCfg = qualitySpeCards[ctx.Rng.Next(qualitySpeCards.Count)];

            // 扣减道具 + 加卡（:353-364）；偏差：Go 分两步 TxChangeItemsNum/TxAddCardsLogic，
            // 本地合并为一次统一道具入口 Execute，消耗校验/扣减与加卡语义一致；
            // 重复奥术卡的 Lv+1/满级转分解产物语义由 ItemEntrance 承担，此处不自建
            Dictionary<int, long> items = ctx.ItemEntrance.ConsumeToItems(asProCfg.ComposeRewards);
            int cardItemId = GetItemIdByCardId(addCardCfg.Id);
            if (cardItemId <= 0)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 卡牌道具配置不存在 cardId={addCardCfg.Id}");
                throw new LocalServerException("卡牌道具配置不存在");
            }
            Accumulate(items, cardItemId, 1);
            ResultItems rsItems = ctx.ItemEntrance.Execute(items);

            // 对照 http/Card.go:258-261：cardId 取加卡结果首张
            return new AsCardFusionResponse { Items = rsItems, CardId = addCardCfg.Id };
        }

        // ============================================================
        // 英灵激活 api/card/soliderCardActive
        // 对照 cardapplication/application_card.go:118-158 SoliderCardActive
        // ============================================================
        public SoliderCardActiveResponse SoliderCardActive(LocalServerContext ctx, SoliderCardActiveRequest req)
        {
            Tables tables = DataConfigManager.Tables;
            SoldierBattleConfig cfg = tables.TbSoldierBattleConfig.GetOrDefault(req.SoldierCardId);
            if (cfg == null)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 英灵卡[{req.SoldierCardId}]配置不存在");
                throw new LocalServerException("配置不存在");
            }

            PlayerCard exist = GetCardByCardId(ctx, req.SoldierCardId);
            if (exist != null)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 英灵卡[{req.SoldierCardId}]已激活");
                throw new LocalServerException("英灵卡已激活");
            }

            Dictionary<int, long> consumeItems = ctx.ItemEntrance.ConsumeToItems(cfg.ActivationCost);
            if (!ctx.ItemEntrance.CheckEnough(consumeItems))
            {
                LogTool.LogWarning($"userId={ctx.UserId} 道具不足");
                throw new LocalServerException("道具不足");
            }

            // 对照 :138-149 事务：消耗用户道具 + 添加用户卡
            // 偏差：Go 直接 TxAddCardsLogic 加卡，本地把卡牌道具并入一次 Execute，
            // HandleCards → AddCardsLogic(isAllCard=true) 与 TxAddCardsLogic(_, true) 等价（英灵附带已解锁技能卡）
            int cardItemId = GetItemIdByCardId(req.SoldierCardId);
            if (cardItemId <= 0)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 卡牌道具配置不存在 cardId={req.SoldierCardId}");
                throw new LocalServerException("卡牌道具配置不存在");
            }
            Accumulate(consumeItems, cardItemId, 1);
            ResultItems result = ctx.ItemEntrance.Execute(consumeItems);

            long cp = ctx.CombatPower.RefreshModule(CombatPowerRefresher.ModuleSoldier);
            result.CombatPower = new CombatPower { CombatPower_ = cp };

            return new SoliderCardActiveResponse { Items = result };
        }

        // ============================================================
        // 英灵解锁技能卡 api/card/unlockSkill
        // 对照 cardapplication/application_unlock_skill_card.go:22-86 UnlockSkillCardLogic
        // ============================================================
        public UnlockSkillCardResponse UnlockSkillCard(LocalServerContext ctx, UnlockSkillCardRequest req)
        {
            Tables tables = DataConfigManager.Tables;

            // ① 技能卡配置存在
            CardBaseConfig skillCfg = tables.TbCardBaseConfig.GetOrDefault(req.SkillCardId);
            if (skillCfg == null)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 技能卡配置不存在 skillCardId={req.SkillCardId}");
                throw new LocalServerException("技能卡配置不存在");
            }

            // ② 技能卡有解锁等级
            if (skillCfg.UnLockLevel == 0)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 该技能卡无需解锁 skillCardId={req.SkillCardId}");
                throw new LocalServerException("该技能卡无需解锁");
            }

            // ③ 技能卡关联了英灵卡
            SpecialEffectsConfig speCfg = tables.TbSpecialEffectsConfig.GetOrDefault(req.SkillCardId);
            if (speCfg == null || speCfg.ActsOnCard == 0)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 技能卡未关联英灵卡 skillCardId={req.SkillCardId}");
                throw new LocalServerException("技能卡未关联英灵卡");
            }

            // ④ 英灵卡存在且已拥有
            PlayerCard heroCard = GetCardByCardId(ctx, speCfg.ActsOnCard);
            if (heroCard == null)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 英灵卡未解锁 skillCardId={req.SkillCardId} heroCardId={speCfg.ActsOnCard}");
                throw new LocalServerException("英灵卡未解锁");
            }

            // ⑤ 英灵卡等级满足条件
            if (heroCard.Lv < skillCfg.UnLockLevel)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 英灵卡等级不足 skillCardId={req.SkillCardId} heroLv={heroCard.Lv} unlockLv={skillCfg.UnLockLevel}");
                throw new LocalServerException("英灵卡等级不足");
            }

            // ⑥ 技能卡未解锁
            PlayerCard existing = GetCardByCardId(ctx, req.SkillCardId);
            if (existing != null)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 技能卡已解锁 skillCardId={req.SkillCardId}");
                throw new LocalServerException("技能卡已解锁");
            }

            // ⑦ 发放（cfgutil.GetItemIdByCardId）
            int itemId = GetItemIdByCardId(req.SkillCardId);
            if (itemId <= 0)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 技能卡道具ID未找到 skillCardId={req.SkillCardId}");
                throw new LocalServerException("技能卡道具ID未找到");
            }

            ResultItems rsItems = ctx.ItemEntrance.Execute(new Dictionary<int, long> { { itemId, 1 } });

            // 新语义（无 Go 对照）：技能等级跟随英灵等级，发卡即置 min(英灵Lv, 技能上限)。
            // rsItems.Cards 与存档是同一对象引用，改 Lv 随响应一并下发
            PlayerCard skillCard = GetCardByCardId(ctx, req.SkillCardId);
            int targetLv = Math.Min(heroCard.Lv, speCfg.LevelPropertyList.Count);
            if (skillCard.Lv < targetLv)
            {
                skillCard.Lv = targetLv;
                ctx.Store.MarkDirty(PlayerArchive.CardsFieldNumber);
            }

            long cp = ctx.CombatPower.RefreshModule(CombatPowerRefresher.ModuleSkill);
            rsItems.CombatPower = new CombatPower { CombatPower_ = cp };

            return new UnlockSkillCardResponse { Items = rsItems };
        }

        // 对照 CardModel.GetInfoByCardId（PlayerCardModel.go:211-220）：查不到返回 null（Go 返回 Id=0 零值行）
        private PlayerCard GetCardByCardId(LocalServerContext ctx, int cardId)
        {
            foreach (PlayerCard card in ctx.Store.Archive.Cards)
            {
                if (card.CardId == cardId)
                {
                    return card;
                }
            }
            return null;
        }

        // 对照 cfgutil.GetItemIdByCardId（card_utils.go:67-74）
        private int GetItemIdByCardId(int cardId)
        {
            foreach (ItemConfig itemCfg in DataConfigManager.Tables.TbItemConfig.DataList)
            {
                if (itemCfg.ModuleType == ModuleType.Card && itemCfg.EffectValue == cardId)
                {
                    return itemCfg.Id;
                }
            }
            return 0;
        }

        // 对照 utils.MergeMapsAndSum64：按 key 累加合并
        private Dictionary<int, long> MergeMapsAndSum(Dictionary<int, long> target, Dictionary<int, long> source)
        {
            foreach (var kv in source)
            {
                Accumulate(target, kv.Key, kv.Value);
            }
            return target;
        }

        private void Accumulate(Dictionary<int, long> map, int key, long delta)
        {
            map.TryGetValue(key, out long cur);
            map[key] = cur + delta;
        }
    }
}
