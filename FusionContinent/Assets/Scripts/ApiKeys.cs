namespace FusionContinent
{
    public class ApiKeys
    {
        private static string Api = "api";
        
        // 登录
        public static string ApiLogin = $"login";
        
        // 渠道
        private static string ApiChannel = $"channel";
        public static string ApiChannelGet = $"{ApiChannel}/getChannel";// 获取渠道配置
        
        
        // 战斗
        private static string ApiBattle = $"{Api}/battle";
        public static string ApiBattleStart = $"{ApiBattle}/start";// 战斗开始
        public static string ApiBattleSettle = $"{ApiBattle}/settle";// 战斗结算
        public static string ApiBattleExit = $"{ApiBattle}/exit";// 战斗结算
        public static string ApiBattleDoubleReward = $"{ApiBattle}/doubleReward";// 双倍奖励
        public static string ApiBattleStageSweeping = $"{ApiBattle}/stageSweeping";// 关卡扫荡
        public static string ApiBattleChapterAfkRewards = $"{ApiBattle}/chapterAfkRewards";// 挂机奖励
        public static string ApiBattleStageFirstRewards = $"{ApiBattle}/stageReceiveRewards";// 首通奖励

        // 体力驿站
        private static string ApiStamina = $"{Api}/stamina";
        public static string ApiStaminaInfo = $"{ApiStamina}/info";// 驿站体力信息
        public static string ApiStaminaClaim = $"{ApiStamina}/claim";// 领取驿站体力
        
        // 轮语者
        private static string ApiSpeaker = $"{Api}/cycleSpeaker";
        public static string ApiSpeakerSetSkin = $"{ApiSpeaker}/setSkin";// 修改轮语者皮肤
        public static string ApiSpeakerSkinActivate = $"{ApiSpeaker}/skin/activate";// 激活皮肤
        public static string ApiSpeakerSkinUpgrade = $"{ApiSpeaker}/skin/upgrade";// 皮肤升级
        public static string ApiSpeakerSkinUnequip = $"{ApiSpeaker}/skin/unequip";// 卸载皮肤
        public static string ApiSpeakerWeaponActivate = $"{ApiSpeaker}/weapon/activate";// 激活武器
        public static string ApiSpeakerWeaponUpgrade = $"{ApiSpeaker}/weapon/upgrade";// 武器升级
        public static string ApiSpeakerWeaponEquip = $"{ApiSpeaker}/weapon/equip";// 装备武器
        public static string ApiSpeakerWeaponUnequip = $"{ApiSpeaker}/weapon/unequip";// 卸载武器
        
        // 用户
        private static string ApiUser = $"{Api}/user";
        // public static string ApiUserUpdate = $"{ApiUser}/update";// 修改用户信息
        public static string ApiUserUpdateAvatar = $"{ApiUser}/updateAvatar";// 更换头像
        public static string ApiUserUpdateAvatarFrame = $"{ApiUser}/updateAvatarFrame";// 更换头像框
        public static string ApiUserAddGuides = $"{ApiUser}/addGuides";// 添加引导状态
        
        // 玩家
        private static string ApiPlayer = $"{Api}/player";
        public static string ApiPlayerSaveGuides = $"{ApiPlayer}/saveGuides";// 保存引导状态
        public static string ApiPlayerSavePlayer = $"{ApiPlayer}/savePlayer";// 保存玩家信息
        public static string ApiPlayerSaveModuleLv = $"{ApiPlayer}/saveModuleUnlockLv";// 保存玩家模块解锁信息
        
        // 活动
        private static string ApiActivity = $"{Api}/activity";
        public static string ApiActivitySignin = $"{ApiActivity}/signin";// 添加签到
        public static string ApiActivityPassCheckRewards = $"{ApiActivity}/passCheckRewards";// 通行证领奖
        public static string ApiActivityFirstRechargeUnlock = $"{ApiActivity}/firstRechargeUnlock";// 首充解锁
        public static string ApiActivityFirstRechargeRewards = $"{ApiActivity}/firstRechargeRewards";// 首充领奖
        public static string ApiActivityAccumulateChargeRewards = $"{ApiActivity}/accumulateChargeRewards";// 累充领奖
        public static string ApiActivityMonthlyRewards = $"{ApiActivity}/monthlyRewards";// 月卡领奖
        
        // 卡牌
        private static string ApiCard = $"{Api}/card";
        public static string ApiCardLevelUp = $"{ApiCard}/levelUp";// 升级
        public static string ApiCardSetSkin = $"{ApiCard}/setSkin";// 设置皮肤
        public static string ApiCardReceiveDevelopReward = $"{ApiCard}/receiveDevelopReward";// 领取养成奖励
        public static string ApiCardDecompositionCard = $"{ApiCard}/decompositionCard";//分解奥术卡接口
        public static string ApiCardCompositionCard = $"{ApiCard}/compositionCard";//合成奥术卡接口
        public static string ApiCardSoliderCardActive = $"{ApiCard}/soliderCardActive";// 英灵激活
        public static string ApiCardUnlockSkill = $"{ApiCard}/unlockSkill";// 英灵解锁技能卡
        
        // 抽卡
        private static string ApiChest = $"{Api}/chest";
        public static string ApiChestchestCard = $"{ApiChest}/chestCard";
        
        // 编队
        private static string ApiTeam = $"{Api}/team";
        public static string ApiTeamSaveTeams = $"{ApiTeam}/saveTeams";
        public static string ApiTeamSaveHeroTeams = $"{ApiTeam}/saveHeroTeam";
        public static string ApiTeamGeneralTeams = $"{ApiTeam}/saveAsCardTeam";
        
        // 装备
        private static string ApiEquip = $"{Api}/equip";
        public static string ApiEquipWear = $"{ApiEquip}/wearEquip";
        public static string ApiEquipUpPhase = $"{ApiEquip}/upPhaseEquip";
        public static string ApiEquipFragment = $"{ApiEquip}/fragmentEquip";
        public static string ApiEquipRefreshEntry = $"{ApiEquip}/refreshEntryEquip";
        public static string ApiEquipUseRefreshEntry = $"{ApiEquip}/useRefreshEntryEquip";

        // 装备槽位
        private static string ApiEquipSlot = $"{Api}/equipSlot";
        public static string ApiEquipSlotUpgrade = $"{ApiEquipSlot}/upgrade";
        
        // 道具
        private static string ApiItem = $"{Api}/item";
        public static string ApiAddItems = $"{ApiItem}/addItems";
        public static string ApiAddItemsAll = $"{ApiItem}/addItemsAll";
        
        // 天赋
        private static string ApiTalent = $"{Api}/talent";
        public static string ApiTalentUse = $"{ApiTalent}/useTalent";
        public static string ApiUpGradeTalent = $"{ApiTalent}/UpGradeTalent";
        
        // 商店
        private static string ApiShop = $"{Api}/shop";
        public static string ApiBuyGoods = $"{ApiShop}/buyGoods";
        public static string ApiBuyStageGiftPack = $"{ApiShop}/buyStageGiftPack";
        
        // 任务
        private static string ApiTask = $"{Api}/task";
        public static string ApiReceiveTaskReward = $"{ApiTask}/receiveTaskReward";
        public static string ApiReceiveTaskActiveReward = $"{ApiTask}/receiveTaskActiveReward";
        
        
        
        // 视频
        public static string Video = $"/video";
        
        // 挑战 - 融合石游戏
        private static string ApiChallenge = $"{Api}/challenge";
        public static string ApiStartFusionGemGame = $"{ApiChallenge}/startFusionGemGame";
        public static string ApiSettleFusionGemGame = $"{ApiChallenge}/settleFusionGemGame";
        
        // 融合石
        private static string ApiFusionGem = $"{Api}/fusionGem";
        public static string ApiFusionGemWear = $"{ApiFusionGem}/wear";
        public static string ApiFusionGemFragment = $"{ApiFusionGem}/fragmentFusionGem";
        public static string ApiFusionGemSynthesis = $"{ApiFusionGem}/synthesisFusionGem";
        
        public static string ApiFusionGemCompound = $"{ApiFusionGem}/compound";

        // 奥数共鸣
        private static string ApiArcResonance = $"{Api}/arcResonance";
        public static string ApiArcResonanceUpgrade = $"{ApiArcResonance}/upgrade";

        // 城墙
        private static string ApiCityWall = $"{Api}/cityWall";
        public static string ApiCityWallUpgrade = $"{ApiCityWall}/upgrade";
        public static string ApiCityWallSkillActivate = $"{ApiCityWall}/skill/activate";
        public static string ApiCityWallSkillUpgrade = $"{ApiCityWall}/skill/upgrade";
        public static string ApiCityWallSave = $"{ApiCityWall}/save";

        // 无尽塔
        private static string ApiEndlessTower = $"{Api}/endlessTower";
        public static string ApiEndlessTowerNextFloor = $"{ApiEndlessTower}/nextFloor";
        public static string ApiEndlessTowerReceiveDailyReward = $"{ApiEndlessTower}/receiveDailyReward";
        public static string ApiEndlessTowerReceiveChestReward = $"{ApiEndlessTower}/receiveChestReward";
        
        
        // 挑战BOSS
        private static string ApiChallengeBoss = $"{Api}/challengeBoss";
        public static string ApiChallengeBossReceiveDailyReward = $"{ApiChallengeBoss}/receiveDailyReward";
        public static string ApiChallengeBossReceiveScoreReward = $"{ApiChallengeBoss}/receiveScoreReward";

        
        // 排行
        private static string ApiRank = $"{Api}/ranking";
        public static string ApiRankStage = $"{ApiRank}/stage";
        public static string ApiRankEndlessTower = $"{ApiRank}/endlessTower";
        public static string ApiRankChallengeBoss = $"{ApiRank}/challengeBoss";
        
        // 图鉴
        private static string ApiAtlas = $"{Api}/atlas";
        public static string ApiAtlasReceiveMonsterReward = $"{ApiAtlas}/receiveMonsterReward";
        
        // 模块
        private static string ApiModule = $"{Api}/module";
        public static string ApiModuleSaveStatus = $"{ApiModule}/saveStatus";
        
        // 兑换码
        private static string ApiExchange = $"{Api}/exchange";
        public static string ApiCodeExchangeGifts = $"{ApiExchange}/codeExchangeGifts";

        // 聊天
        private static string ApiChat = $"{Api}/chat";
        public static string ApiChatToChannel = $"{ApiChat}/toChannel";
        public static string ApiChatToPlayer = $"{ApiChat}/toPlayer";

        // 好友
        private static string ApiRelation = $"{Api}/relation";
        public static string ApiRelationSearchUser = $"{ApiRelation}/searchUser";
        public static string ApiRelationApply = $"{ApiRelation}/apply";
        public static string ApiRelationApplyList = $"{ApiRelation}/applyList";
        public static string ApiRelationApplyOperate = $"{ApiRelation}/applyOperate";
        public static string ApiRelationFriendOperate = $"{ApiRelation}/friendOperate";
        public static string ApiRelationFriendList = $"{ApiRelation}/friendList";

        // 邮件
        private static string ApiMail = $"{Api}/mail";
        public static string ApiMailList = $"{ApiMail}/list";
        public static string ApiMailBatchRead = $"{ApiMail}/batchRead";
        public static string ApiMailBatchClaim = $"{ApiMail}/batchClaim";
        public static string ApiMailBatchDelete = $"{ApiMail}/batchDelete";

        // 门票
        private static string ApiTicket = $"{Api}/ticket";
        public static string ApiTicketClaim = $"{ApiTicket}/claim";

        // 排行榜
        private static string ApiRanking = $"{Api}/ranking";
        public static string ApiRankingChapter = $"{ApiRanking}/chapter";// 主线（章节）排行榜
        public static string ApiRankingHiddenRealmTower = $"{ApiRanking}/hiddenRealmTower";// 奇幻秘境（隐境之塔）排行榜
        public static string ApiRankingCombatPower = $"{ApiRanking}/combatPower";// 战力排行榜

        // PVP 竞技场
        private static string ApiPvp = $"{Api}/pvp";
        public static string ApiPvpMatchList = $"{ApiPvp}/matchList";// 匹配对手列表
        public static string ApiPvpOpponentInfo = $"{ApiPvp}/opponentInfo";// 对手详情（proto 仅有 Request，暂未接线）
        public static string ApiPvpSeasonInfo = $"{ApiPvp}/seasonInfo";// 赛季信息
        public static string ApiPvpStart = $"{ApiPvp}/start";// 开始对战
        public static string ApiPvpSettle = $"{ApiPvp}/settle";// 结算
        public static string ApiPvpRanking = $"{ApiPvp}/ranking";// 排行榜
        public static string ApiPvpClaimLevelReward = $"{ApiPvp}/claimLevelReward";// 领取段位/等级奖励
        public static string ApiPvpMatchHistory = $"{ApiPvp}/matchHistory";// 对战记录
        public static string ApiPvpTaskProgress = $"{ApiPvp}/taskProgress";// 任务进度
        public static string ApiPvpClaimFreeTicket = $"{ApiPvp}/claimFreeTicket";// 领取免费挑战券
        public static string ApiPvpClaimAfkReward = $"{ApiPvp}/claimAfkReward";// 领取挂机奖励
    }
}