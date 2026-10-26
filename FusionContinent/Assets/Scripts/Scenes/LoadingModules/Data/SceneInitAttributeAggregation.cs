using System.Collections.Generic;
using System.Linq;
using System.Text;
using Config;
using FusionContinent;
using FusionContinent.Attribute;
using FusionContinent.Model;
using FusionContinent.Utils;
using Game.Equip;
using Game.HeroSeat;
using Game.Player;
using UIFramework.Services;
using UnityEngine;

/// <summary>
/// 属性聚合初始化模块 - 整合外部养成属性
/// </summary>
public class SceneInitAttributeAggregation : SceneMapBase
{
    public override string ModuleName => "属性聚合初始化";

    protected override void OnStartLoad()
    {
        var gpm = GamePlayManager.Default;
        if (gpm == null)
        {
            ErrorMessage = "GamePlayManager.Default is null";
            return;
        }

        // 整合外部养成属性
        SetAttributeAggregation(gpm);
        
        // // 自动战斗状态
        // var autoReleaseSkillState = PlayerPrefs.GetString("AutoReleaseSkillState");
        // if (autoReleaseSkillState == "") autoReleaseSkillState = "False";
        // Global.AutoReleaseSkillState.Value = autoReleaseSkillState == "True";

        // 贪婪预支卡检测已移至 SceneInitGreedyAdvance 模块（在 SceneInitCardTeams 之后执行）
        
        // 最大人口上限
        Global.MaxCityPopulation.Value = Talent.GetPopulation();
        
        // 引导状态
        bool isStageGuide = !Global.Player.CheckGuideStatus(GuideType.GuideStage);
        gpm.SetIsStageGuide(isStageGuide);
        if (isStageGuide) 
        {
            Global.MaxCityPopulation.Value += 10; // 引导关卡人口+10
        }

        progress = 1.0f;
    }

    private void SetAttributeAggregation(GamePlayManager gpm)
    {
        var attributeSources = new List<(string SourceName, AttributeAggregation Aggregation)>
        {
            ("轮语者皮肤", GameServiceManager.Get<SpeakerService>().GetAttributes()),
            ("天赋", Talent.GetAttributes()),
            ("装备", Equip.GetAttributes()),
            ("轮语者武器", GameServiceManager.Get<WeaponService>().GetAttributes()),
            ("城墙", GameServiceManager.Get<SeatService>().GetWallAttributes()),
            ("城墙槽位技能", GameServiceManager.Get<SeatService>().GetEquippedSlotSkillAttributes()),
        };

        gpm.ModuleAttributes = new AttributeSet();
        gpm.ModuleMainAttributes = new Dictionary<MainAttributeType, float>();

        foreach (var (sourceName, attrAggregate) in attributeSources)
        {
            LogAttributeAggregationSource(sourceName, attrAggregate);

            gpm.ModuleAttributes.AddRangeBaseValue(attrAggregate.Attributes);

            foreach (var attr in attrAggregate.MainAttributes)
            {
                gpm.ModuleMainAttributes.TryAdd(attr.Type, 0);
                gpm.ModuleMainAttributes[attr.Type] += attr.Value;
            }
        }

        gpm.RefreshCarBindingAttributes();
    }

    private static void LogAttributeAggregationSource(string sourceName, AttributeAggregation aggregation)
    {
        var detailBuilder = new StringBuilder();
        var hasDetail = false;

        foreach (var kv in aggregation.Attributes.GetAll())
        {
            if (kv.Value.BaseValue == 0f)
            {
                continue;
            }

            if (hasDetail)
            {
                detailBuilder.Append(", ");
            }

            detailBuilder.Append(kv.Key);
            detailBuilder.Append('=');
            detailBuilder.Append(kv.Value.BaseValue);
            hasDetail = true;
        }

        foreach (var mainAttr in aggregation.MainAttributes)
        {
            if (hasDetail)
            {
                detailBuilder.Append(", ");
            }

            detailBuilder.Append("Main[");
            detailBuilder.Append(mainAttr.Type);
            detailBuilder.Append("]=");
            detailBuilder.Append(mainAttr.Value);
            hasDetail = true;
        }

        if (!hasDetail)
        {
            LogTool.Log($"[SceneInitAttributeAggregation] {sourceName}: (无属性)");
            return;
        }

        LogTool.Log($"[SceneInitAttributeAggregation] {sourceName}: {detailBuilder}");
    }
}
