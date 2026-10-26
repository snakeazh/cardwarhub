using System.Collections.Generic;
using FusionContinent.Base;
using QFramework;
using UnityEngine;
using UnityEngine.U2D;

namespace FusionContinent
{
    public partial class Global : MonoBehaviour
    {
         // 常量配置
        public static YooAssetLoader ResLoader;
       // [SpriteAtlasTarget("TalentIcon")]
        // public static SpriteAtlas TalentIconAtlas;// 羁绊流派图集
        [SpriteAtlasTarget("SchoolIcon")]
        public static SpriteAtlas SchoolIconAtlas;// 羁绊流派图集
        [SpriteAtlasTarget("ItemIcon")]
        public static SpriteAtlas ItemIconSpriteAtlas;// 道具图集
        [SpriteAtlasTarget("CardIcon")]
        public static SpriteAtlas CardIconSpriteAtlas;// 卡牌图集
        [SpriteAtlasTarget("ShopIcon")]
        public static SpriteAtlas ShopIconSpriteAtlas;// 玩家形象图集
        [SpriteAtlasTarget("WeaponIcon")]
        public static SpriteAtlas EquipIconSpriteAtlas;// 装备图集

        [SpriteAtlasTarget("HealthIcon")]
        public static SpriteAtlas HealthIconSpriteAtlas;// 血条图集
        [SpriteAtlasTarget("MonsterIcon")]
        public static SpriteAtlas MonsterIconSpriteAtlas;// 怪物图集
        [SpriteAtlasTarget("MonsterLineArt")]
        public static SpriteAtlas MonsterLineArtSpriteAtlas;//  怪物线稿图集
        [SpriteAtlasTarget("ActivityIcon")]
        public static SpriteAtlas ActivityIconSpriteAtlas;// 活动图集
        [SpriteAtlasTarget("CardStyleIcon")]
        public static SpriteAtlas CardStyleIconSpriteAtlas;// 卡牌风格图集
        [SpriteAtlasTarget("FusionGemIcon")]
        public static SpriteAtlas FusionGemIconSpriteAtlas;// 融合石图集
        [SpriteAtlasTarget("RotaryTableIcon")]
        public static SpriteAtlas RotaryTableIconSpriteAtlas;// 转盘图集
        [SpriteAtlasTarget("MonsterHeadIcon")]
        public static SpriteAtlas MonsterHeadIconSpriteAtlas;// 怪物头像图集
        // [SpriteAtlasTarget("PlayerFigureIcon")]
        // public static SpriteAtlas PlayerFigureIconSpriteAtlas;// 玩家形象图集
        [SpriteAtlasTarget("EndlessTowerIcon")]
        public static SpriteAtlas EndlessTowerIconSpriteAtlas;// 无尽爬塔图集
        
        
        [SpriteAtlasTarget("GeneralSkillFrameIcon")]
        public static SpriteAtlas GeneralSkillFrameIconSpriteAtlas;//  通用卡品质框图集
        
        [SpriteAtlasTarget("TreasureChestIcon")]
        public static SpriteAtlas TreasureChestIconSpriteAtlas;// 宝箱图集
        [SpriteAtlasTarget("SchoolChooseCardIcon")]
        public static SpriteAtlas SchoolChooseCardIconSpriteAtlas;// 流派三选一图集
        [SpriteAtlasTarget("SurpriseChooseCardIcon")]
        public static SpriteAtlas SurpriseChooseCardIconSpriteAtlas;// 惊喜三选一图集
        [SpriteAtlasTarget("EquipDetailQualityIcon")]
        public static SpriteAtlas EquipDetailQualityIconAtlas;//装备品质栏图集
        [SpriteAtlasTarget("ItemShowPanelBg")]
        public static SpriteAtlas ItemShowPanelBgAtlas;//装备品质栏图集
        
        // --新拆的独立功能图集---///
        [SpriteAtlasTarget("ConnectionIcon")]
        public static SpriteAtlas ConnectionIconAtlas;
        [SpriteAtlasTarget("HeroHead")]
        public static SpriteAtlas HeroHeadIconAtlas;//英雄头像图集
        [SpriteAtlasTarget("HeroPaintHead")]
        public static SpriteAtlas HeroIconSpriteAtlas;//英雄icon图集
        [SpriteAtlasTarget("SpecialSkillIcon")]
        public static SpriteAtlas SpecialSkillIconAtlas;// 技能图标图集
        // public static SpriteAtlas ChestBookIconAtlas;// 抽卡图书图标图集
        [SpriteAtlasTarget("UpgradeItemIcon")]
        public static SpriteAtlas UpgradeItemAtlas;//升级界面显示图标图集
        
        //商店icon图集
        [SpriteAtlasTarget("ShopGoodsIcon")]
        public static SpriteAtlas ShopGoodsIconAtlas;//商城icon图集
        
        //大世界图集
        [SpriteAtlasTarget("BigWorldIcon")]
        public static SpriteAtlas BigWorldIconAtlas;//大世界动态图集
        
        //大世界图集
        [SpriteAtlasTarget("Common")]
        public static SpriteAtlas CommonAtlas;//大世界动态图集
        // 伤害类型图集
        [SpriteAtlasTarget("DamageTypeIcon")]
        public static SpriteAtlas DamageTypeIconAtlas;//伤害类型动态图集
        
        // 属性图集
        [SpriteAtlasTarget("AttributeIcon")]
        public static SpriteAtlas AttributeIconAtlas;//属性动态图集

        [SpriteAtlasTarget("GameSettlementBgIcon")] 
        public static SpriteAtlas GameSettlementBgIconAtlas;
        
        //连击图集
        [SpriteAtlasTarget("ComboIcon")] 
        public static SpriteAtlas ComboIconAtlas;
        
        //局内使用的战斗头像图集
        [SpriteAtlasTarget("BattleHead")] 
        public static SpriteAtlas BattleHeadAtlas;
        
        [SpriteAtlasTarget("PassCheck")] 
        public static SpriteAtlas PassCheckAtlas;
        
        
        [SpriteAtlasTarget("GameMap1")] 
        public static SpriteAtlas GameMap1Atlas;
        
        [SpriteAtlasTarget("GameMap2")] 
        public static SpriteAtlas GameMap2Atlas;
    }
}