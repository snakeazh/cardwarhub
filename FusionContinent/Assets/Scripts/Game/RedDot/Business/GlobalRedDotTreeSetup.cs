namespace Game.RedDot.Business
{
    public static class GlobalRedDotTreeSetup
    {
        public static void Build(RedDotTree tree)
        {
            const RedDotAggregateMode any = RedDotAggregateMode.Any;
            const RedDotAggregateMode sum = RedDotAggregateMode.Sum;

            void Add(RedNodeType type, RedDotAggregateMode mode, bool hasParent = false, RedNodeType parent = default)
            {
                string path = type.ToString();
                tree.RegisterNodeType(type, path);
                tree.SetAggregateMode(type, mode);
                if (hasParent)
                {
                    tree.SetParent(path, parent.ToString());
                }
            }

            // 轮语者 Tab（父节点必须先于子节点注册，否则 RedNodeType 映射会丢失）
            Add(RedNodeType.NavBarSpeaker, any);
            Add(RedNodeType.SpeakerEquipUpGradeBtn, any, true, RedNodeType.NavBarSpeaker);
            Add(RedNodeType.SpeakerEquipForgeRedDot, sum, true, RedNodeType.SpeakerEquipUpGradeBtn);
            Add(RedNodeType.SpeakerEquipRefineRedDot, sum, true, RedNodeType.SpeakerEquipUpGradeBtn);
            // 装备可替换：背包存在比当前穿戴更高战力的同部位装备时亮，聚合到 NavBarSpeaker（任一子亮即亮）
            Add(RedNodeType.SpeakerEquipReplaceRedDot, sum, true, RedNodeType.NavBarSpeaker);
            // 融合石页签：可替换 / 可突破 → SpeakerFusionGemBtn → NavBarSpeaker
            Add(RedNodeType.SpeakerFusionGemBtn, any, true, RedNodeType.NavBarSpeaker);
            Add(RedNodeType.SpeakerFusionGemReplaceRedDot, sum, true, RedNodeType.SpeakerFusionGemBtn);
            Add(RedNodeType.SpeakerFusionGemBreakthroughRedDot, sum, true, RedNodeType.SpeakerFusionGemBtn);
            // 天赋可养成（材料+金币足够）：SpeakWhellPanel/Left/TalentBtn，聚合到 NavBarSpeaker
            Add(RedNodeType.TalentUpgradeBtn, sum, true, RedNodeType.NavBarSpeaker);
            // 神器入口：可激活 / 可养成 → SpeakWhellPanel ArtifactBtn，聚合到 NavBarSpeaker
            Add(RedNodeType.SpeakerWeaponBtn, any, true, RedNodeType.NavBarSpeaker);
            Add(RedNodeType.SpeakerWeaponActivateBtn, sum, true, RedNodeType.SpeakerWeaponBtn);
            Add(RedNodeType.SpeakerWeaponUpgradeBtn, sum, true, RedNodeType.SpeakerWeaponBtn);
            // 皮肤可激活：SpeakWhellPanel SkinBtn，聚合到 NavBarSpeaker
            Add(RedNodeType.SpeakerSkinActivateBtn, sum, true, RedNodeType.NavBarSpeaker);
            // 机关入口：可激活 / 可升级机关 / 城墙可升级 → SpeakWhellPanel HeroSeatBtn，聚合到 NavBarSpeaker
            Add(RedNodeType.SpeakerHeroSeatBtn, any, true, RedNodeType.NavBarSpeaker);
            Add(RedNodeType.SpeakerHeroSeatActivateBtn, sum, true, RedNodeType.SpeakerHeroSeatBtn);
            Add(RedNodeType.SpeakerHeroSeatUpgradeBtn, sum, true, RedNodeType.SpeakerHeroSeatBtn);
            Add(RedNodeType.SpeakerHeroSeatWallUpgradeBtn, sum, true, RedNodeType.SpeakerHeroSeatBtn);

            // 英灵 Tab（GameMainPanel Btn(3) → NavBarSoldier）
            Add(RedNodeType.NavBarSoldier, any);
            Add(RedNodeType.SoliderCanActivate, sum, true, RedNodeType.NavBarSoldier);
            Add(RedNodeType.SoliderCanUpgrade, sum, true, RedNodeType.NavBarSoldier);
            Add(RedNodeType.SoliderCanAwakening, sum, true, RedNodeType.NavBarSoldier);

            // 商店 Tab（GameMainPanel Btn → NavBarShop）
            // 单机化：抽卡页签已移除，NavBarShop 暂无子节点，不再注册

            // 奥术卡 Tab（GameMainPanel Btn(1) → NavBarGeneralSkill）
            Add(RedNodeType.NavBarGeneralSkill, any);
            // 奥术卡可合成（材料足够）
            Add(RedNodeType.ArcCardCombine, sum, true, RedNodeType.NavBarGeneralSkill);

            // 世界 Tab（GameMainPanel Btn(4) → NavBarBigWord）
            Add(RedNodeType.NavBarBigWord, any);
            Add(RedNodeType.MonsterAtlasBtn, sum, true, RedNodeType.NavBarBigWord);
            Add(RedNodeType.StationBtn, sum, true, RedNodeType.NavBarBigWord);
            Add(RedNodeType.StationReceiveBtn, sum, true, RedNodeType.StationBtn);
        }
    }
}
