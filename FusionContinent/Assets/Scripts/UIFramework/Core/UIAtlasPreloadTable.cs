using System;
using System.Collections.Generic;

namespace UIFramework
{
    /// <summary>
    /// UI 图集预加载硬编码表（唯一真值源）。
    /// AtlasKey 为短名，运行时拼接为 YooAsset 地址 SpriteAtlas_{Key}。
    /// PanelKey 使用面板 Type.Name（含 *New）。
    /// </summary>
    public static class UIAtlasPreloadTable
    {
        /// <summary>
        /// 启动常驻图集，不随面板关闭释放。收录原则：
        /// 1. 登录链路必需（Login）；
        /// 2. 全局通用组件（按钮底图、Toast、奖励/卡牌/头像等散布大量面板的组件）；
        /// 3. fallback 链闭包（Global.InitGameConst 启动时一次性注册替补链，链主在 Common 则链成员必须同在，
        ///    否则注册时链成员为 null 被静默跳过：ItemIcon←WeaponIcon；CardIcon←ConnectionIcon/HeroHead/HeroPaintHead/SpecialSkillIcon）；
        /// 4. 被多个 UIKit 旧面板打开且 WebGL 下 UIKit 同步预载必失败的（BigWorldIcon：TalentPanel 的宿主遍布主城与战斗）。
        /// </summary>
        public static readonly string[] CommonAtlasKeys =
        {
            // 登录链路
            "Login",
            // 全局通用组件：所有按钮底图（UICommonBtn）、Toast 图标（Common）
            "UICommonBtn",
            "Button",
            "Common",
            // 遍布 20+ 面板的通用业务图集：奖励/道具/卡牌展示组件（RewardEntity、CardSlot 等）
            "ItemIcon",
            "ItemQualityFrame",
            "CardIcon",
            "CarFrame",
            "CardStyleIcon",
            // 玩家头像组件（PlayerHead/HeadEntity）内嵌于主界面/结算/设置/背包/排行/换头像等 5+ 面板
            "PlayerHead",
            "PlayerHeadFrame",
            // 主界面与 BottomBar（GameMainPanelNew 无面板条目）的序列化 sprite 引用，全局大部分时间都在屏上，
            // 面板级管理会在进出战斗时反复装卸（实测回主界面后图标丢失），故常驻
            "Activity",
            "ActivityIcon",
            // 关卡宝箱组件（TreasureChest）内嵌于主界面/挑战Boss/无尽塔/任务/成就等 7+ 处，战斗内 NextDay 也用
            "TreasureChestIcon",
            // TalentPanel（内嵌 Talentshow）从多个 UIKit 旧面板打开，WebGL 下 UIKit 同步预载必失败
            "BigWorldIcon",
            // fallback 链成员（见上方注释）
            "WeaponIcon",
            "ConnectionIcon",
            "HeroHead",
            "HeroPaintHead",
            "SpecialSkillIcon",
        };

        /// <summary>
        /// 战斗图集：进战斗时由 ScenePreloadBattleAssets 预载，退出战斗场景时由 BattleSceneLifecycle 释放。
        /// 战斗内实体（MonsterCard/SoldierCard/GameLandUI 等）直接读 Global 图集字段，不经面板预加载，必须在进战斗前就绪。
        /// AttributeIcon：战斗内长按士兵卡打开 HeroDetails（UIKit 面板，WebGL 无法同步预载）需要。
        /// </summary>
        public static readonly string[] BattleAtlasKeys =
        {
            "MonsterHeadIcon",
            "ComboIcon",
            "SurpriseChooseCardIcon",
            "GeneralSkillFrameIcon",
            "SchoolIcon",
            "GameMap1",
            "GameMap2",
            "PassCheck",
            "BattleHead",
            "DamageTypeIcon",
            "HealthIcon",
            "AttributeIcon",
            // 战斗场景实体 GemStoneEntity 也取用（同时挂在融合石面板条目上，引用计数兼容）
            "FusionGemIcon",
            "GemQualityFrame",
        };

        private static readonly HashSet<string> sCommonSet =
            new HashSet<string>(CommonAtlasKeys, StringComparer.Ordinal);

        // 面板级图集条目。注意两类特殊结构：
        // - 「名义面板实际内嵌组件」（如 BattleInfoPanel 由 CardGroupPanelNew SetActive 激活、NormalShopList01New
        //   是 ShopPanel01New 的子面板）：条目必须挂在真实宿主面板上；
        // - UIKit 旧面板经 UIKitAtlasPreloadHook 同步预载，WebGL 下必失败，仅 Android/Editor 生效。
        private static readonly Dictionary<string, string[]> sPanelAtlasKeys =
            new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                // 启动预加载 / 高频 New 面板
                // Activity/ActivityIcon 已升入 CommonAtlasKeys 常驻（主界面+BottomBar 全程在屏），主界面不再占条目
                { "MainInterfacePanelNew", System.Array.Empty<string>() },
                { "BigWorldPanelNew", new[] { "BigWordIcon" } },
                { "ShopPanel01New", new[] { "ShopIcon", "ShopGoodsIcon", "ShopItem", "CardPackStyle" } },
                // CardGroupPanelNew 内嵌 BattleInfoPanel（SetActive 激活，不经打开流程）
                { "CardGroupPanelNew", new[] { "SchoolIcon", "MonsterIcon", "MonsterCardSlotTag" } },
                { "CardGroupPanel", new[] { "MonsterIcon", "MonsterCardSlotTag" } },
                { "CardGroupRecommend", new[] { "SchoolIcon" } },
                { "SoldierPanelNew", new[] { "AttributeIcon", "CareerTypeIcon" } },
                // SoldierSlot 的真实宿主是两个 SoldierMainPanel
                { "SoldierMainPanelNew", new[] { "CareerTypeIcon" } },
                { "SoldierMainPanel", new[] { "CareerTypeIcon" } },

                // 装备/融合石模块（GemWearSlot、EquipSlotDisplay、GemStoneEntity 为槽位通用组件，归入宿主面板）
                { "EquipPanelNew", new[] { "FusionGemBg", "FusionGemStore" } },
                { "EquipUpGradePanelNew", new[] { "EquipPartIcon", "FusionGemStore" } },
                { "EquipDetailsPanel1New", new[] { "EquipBgStyle", "FusionGemIcon" } },
                // SpeakWhellPanelNew 内嵌 GemStoneEntity（需 FusionGemIcon）与 SpeakWheelSlot（需 SpeakWheelFrame）
                { "SpeakWhellPanelNew", new[] { "EquipPartIcon", "FusionGemStore", "GemQualityFrame", "SpeakWheelFrame", "FusionGemIcon" } },
                { "FusionGemPanelNew", new[] { "FusionGemIcon" } },
                { "FusionGemCompoundPanelNew", new[] { "FusionGemIcon", "GemQualityFrame" } },
                { "FusionCompoundResultPanelNew", new[] { "FusionGemIcon" } },
                { "FusionGemDetailPanelNew", new[] { "FusionGemIcon", "EquipPartIcon", "FusionGemBg", "FusionGemStore" } },
                { "GameGemPanelNew", new[] { "FusionGemIcon" } },
                { "ItemShowPanelNew", new[] { "EquipBgStyle", "ItemShowPanelBg" } },
                { "WeaponPanelNew", new[] { "WeaponQualityFrame" } },
                { "UpgradePanelNew", new[] { "UpgradeItemIcon" } },

                // 商店列表面板（新旧并存；子面板按自己的类名查表，条目必须独立存在）
                { "NormalShopList01New", new[] { "ShopGoodsIcon", "ShopItem" } },
                { "NormalShopList01", new[] { "ShopIcon", "ShopGoodsIcon" } },
                { "normalShopList", new[] { "ShopIcon" } },
                { "DailyShopListNew", new[] { "ShopGoodsIcon", "ShopItem" } },
                { "ChallengeShopListNew", new[] { "ShopGoodsIcon" } },
                { "ChallengeShopList", new[] { "ShopGoodsIcon" } },
                { "SoldierShopList", new[] { "ShopGoodsIcon" } },
                // 挑战 Boss 面板内嵌宝箱组件 + 内嵌打开 NormalShopList01
                { "ChallengeBossPanel", new[] { "ShopIcon", "ShopGoodsIcon" } },

                // 怪物图鉴/预览
                { "MonsterAtlasPanelNew", new[] { "MonsterIcon", "MonsterCardSlotTag" } },
                { "MonsterAtlasDetailPanelNew", new[] { "MonsterLineArt" } },
                { "MonsterPreViewPanelNew", new[] { "MonsterIcon", "MonsterCardSlotTag" } },

                // 其余面板
                { "RealmRoomNew", new[] { "MonsterEntry" } },
                { "GameSettlementPanel1New", new[] { "GameSettlementBgIcon" } },
                { "EndlessTowerPanel", new[] { "EndlessTowerIcon" } },
                { "SchoolChooseCardPanelNew", new[] { "SchoolChooseCardIcon" } },
                { "GamePausePanelNew", new[] { "SchoolChooseCardIcon" } },
                { "GamePausePanel", new[] { "SchoolChooseCardIcon" } },
                { "SoldierGetPanel", new[] { "AttributeIcon" } },
                { "AttributePopupPanel", new[] { "AttributeIcon" } },
                // HeroDetails 仅战斗内打开（长按士兵卡），WebGL 由 BattleAtlasKeys 兜底，本条目服务 Android/Editor
                { "HeroDetails", new[] { "AttributeIcon" } },
                { "RotaryTablePanel", new[] { "RotaryTableIcon" } },
            };

        public static bool IsCommon(string atlasKey)
        {
            if (string.IsNullOrWhiteSpace(atlasKey))
            {
                return false;
            }

            return sCommonSet.Contains(atlasKey);
        }

        public static bool TryGet(string panelKey, out string[] atlasKeys)
        {
            atlasKeys = null;
            if (string.IsNullOrWhiteSpace(panelKey))
            {
                return false;
            }

            return sPanelAtlasKeys.TryGetValue(panelKey, out atlasKeys);
        }

        public static bool TryGet(Type panelType, out string[] atlasKeys)
        {
            atlasKeys = null;
            if (panelType == null)
            {
                return false;
            }

            return TryGet(panelType.Name, out atlasKeys);
        }
    }
}
