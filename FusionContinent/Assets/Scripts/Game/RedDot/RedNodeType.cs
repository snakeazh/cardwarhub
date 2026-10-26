namespace Game.RedDot
{
    public enum RedNodeType
    {
        /**主界面的枚举值号码段是0-100*/
        /// <summary>
        /// 导航栏主页面按钮节点
        /// </summary>
        NavBarBattle = 0,
        /// <summary>
        /// 导航栏奥术卡节点
        /// </summary>
        NavBarGeneralSkill = 1,

        /// <summary>
        /// 导航栏世界根节点
        /// </summary>
        NavBarBigWord = 3,
        
        /// <summary>
        /// 导航栏轮语者根节点
        /// </summary>
        NavBarSpeaker = 4,
        /// <summary>
        /// 导航栏英灵根节点
        /// </summary>
        NavBarSoldier = 5,
        /// <summary>
        /// 主页面挂机节点
        /// </summary>
        MainPageAFKBtn = 11,

        /// <summary>
        /// 挂机页面领取按钮节点
        /// </summary>
        AFKPageClaimBtn = 12,

        /// <summary>
        /// 主页面任务节点
        /// </summary>
        MainPageTaskBtn = 20,

        /// <summary>
        /// 任务页面日任务页签按钮节点
        /// </summary>
        TaskPageDayBtn = 21,

        /// <summary>
        /// 轮语者界面装备锻造入口按钮（聚合轮语者可锻造 + 可洗练）
        /// </summary>
        SpeakerEquipUpGradeBtn = 30,

        /// <summary>
        /// 轮语者装备可锻造
        /// </summary>
        SpeakerEquipForgeRedDot = 31,

        /// <summary>
        /// 轮语者装备可洗练
        /// </summary>
        SpeakerEquipRefineRedDot = 32,

        /// <summary>
        /// 轮语者装备可替换节点（背包存在比当前穿戴更高战力的同部位装备时亮，聚合到 NavBarSpeaker）
        /// </summary>
        SpeakerEquipReplaceRedDot = 34,

        /// <summary>
        /// 轮语者神器/武器可激活（未激活且材料足够），聚合到 SpeakerWeaponBtn
        /// </summary>
        SpeakerWeaponActivateBtn = 35,

        /// <summary>
        /// 轮语者皮肤可激活（未激活且材料足够），聚合到 NavBarSpeaker；SpeakWhellPanel SkinBtn
        /// </summary>
        SpeakerSkinActivateBtn = 36,

        /// <summary>
        /// 轮语者神器入口（可激活或可养成任一亮），聚合到 NavBarSpeaker；SpeakWhellPanel ArtifactBtn
        /// </summary>
        SpeakerWeaponBtn = 37,

        /// <summary>
        /// 轮语者神器/武器可养成（已激活且升星材料足够），聚合到 SpeakerWeaponBtn
        /// </summary>
        SpeakerWeaponUpgradeBtn = 38,

        /// <summary>
        /// 机关入口（可激活/可升级/可装备任一亮），聚合到 NavBarSpeaker；SpeakWhellPanel HeroSeatBtn
        /// </summary>
        SpeakerHeroSeatBtn = 39,

        /// <summary>
        /// 机关可激活（未激活且材料足够），聚合到 SpeakerHeroSeatBtn
        /// </summary>
        SpeakerHeroSeatActivateBtn = 40,

        /// <summary>
        /// 机关可升级（已激活、未满级且材料足够），聚合到 SpeakerHeroSeatBtn
        /// </summary>
        SpeakerHeroSeatUpgradeBtn = 41,

        /// <summary>
        /// 城墙可升级（未满级且材料足够），聚合到 SpeakerHeroSeatBtn；HeroSeatPanel UpGradeBtn
        /// </summary>
        SpeakerHeroSeatWallUpgradeBtn = 42,

        /// <summary>
        /// 轮语者融合石可替换（背包存在比当前部位镶嵌更高战力的融合石），聚合到 SpeakerFusionGemBtn
        /// </summary>
        SpeakerFusionGemReplaceRedDot = 43,

        /// <summary>
        /// 轮语者融合石可突破（未满阶且存在可吞噬的未装备次石），聚合到 SpeakerFusionGemBtn
        /// </summary>
        SpeakerFusionGemBreakthroughRedDot = 44,

        /// <summary>
        /// 轮语者融合石页签入口（可替换 / 可突破任一亮），聚合到 NavBarSpeaker；SpeakWhellPanel FusionGemBtn
        /// </summary>
        SpeakerFusionGemBtn = 45,

        /**英灵组的枚举值号码段是100-200*/
        /// <summary>
        /// 英灵可升级节点
        /// </summary>
        SoliderUpgradeCard = 101,

        /// <summary>
        /// 英灵养成可领取节点
        /// </summary>
        SoliderDevelopCard = 102,
        /// <summary>
        /// 英灵可升级和可领取养成节点
        /// </summary>
        SoliderCanUpgrade = 104,

        /// <summary>
        /// 英灵可解锁/激活（未拥有且 ActivationCost 满足），聚合到 NavBarSoldier
        /// </summary>
        SoliderCanActivate = 105,

        /// <summary>
        /// 英灵可觉醒（已达觉醒档且材料足够），聚合到 NavBarSoldier
        /// </summary>
        SoliderCanAwakening = 106,

        /// <summary>
        /// 当前选中英灵可觉醒（局部：SoldierPanel AwakeBtn / AwakeningPanel）
        /// </summary>
        SoliderAwakeningCard = 107,

        /**世界的枚举值号码段是300-400*/
        /// <summary>
        /// 怪物图鉴节点
        /// </summary>
        MonsterAtlasBtn = 301,

        /// <summary>
        /// 驿站入口节点（BigWorldPanel StationBtn）
        /// </summary>
        StationBtn = 302,

        /// <summary>
        /// 驿站领取按钮节点（StationPanel ReceiveBtn）
        /// </summary>
        StationReceiveBtn = 303,

        // /// <summary>
        // /// 主界面新手引导奖励节点
        // /// </summary>
        // MainGuideFlowBtn = 26,
        
        /**奥术卡导航栏的枚举值号码段是400-500*/
        /// <summary>
        /// 奥术卡可合成节点（材料足够时亮，聚合到 NavBarGeneralSkill）
        /// </summary>
        ArcCardCombine = 403,

        
        
        /**奥术卡导航栏的枚举值号码段是500-600*/
        /// <summary>
        /// 天赋可升级节点
        /// </summary>
        TalentUpgradeBtn = 501,


    }
}
