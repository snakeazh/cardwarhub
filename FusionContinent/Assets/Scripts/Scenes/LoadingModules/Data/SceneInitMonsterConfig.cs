using System;
using System.Collections.Generic;
using System.Linq;
using Config;
using FusionContinent;
using FusionContinent.Attribute;
using Game.Realm;
using UIFramework.Services;
using UnityEngine;
using ValueType = Config.ValueType;
/// <summary>
/// 怪物配置初始化模块 - 加载刷怪配置和关卡配置
/// </summary>
public class SceneInitMonsterConfig : SceneMapBase
{
    public override string ModuleName => "怪物配置初始化";

    protected override void OnStartLoad()
    {
        var gpm = GamePlayManager.Default;
        if (gpm == null)
        {
            ErrorMessage = "GamePlayManager.Default is null";
            return;
        }

        // 获取怪物关卡生成配置
        gpm._monsterGenCfg = gpm._monsterGenConfig.GetOrDefault(Global.StageId.Value);
        if (gpm._monsterGenCfg == null)
        {
            ErrorMessage = $"关卡[{Global.StageId.Value}]，刷怪配置不存在";
            return;
        }

        // 获取关卡数据
        gpm.StageCfg = DataConfigManager.Tables.TbStageBattleConfig.GetOrDefault(Global.StageId.Value);
        if (gpm.StageCfg == null)
        {
            ErrorMessage = $"关卡[{Global.StageId.Value}]，配置不存在";
            return;
        }

        // 大秘境特殊处理：使用服务端下发的对应层词条数据
        if (gpm.StageCfg.StageType == StageType.GreaterRift)
        {
            gpm.MonsterEntries = LoadFloorMonsterEntries();
        }

        // 怪物属性修正
        ApplyMonsterAttributeModifiers(gpm);

        gpm.PreloadStageSpineBakeData();

        LogTool.Log($"当前关卡ID: {Global.StageId.Value}");
        progress = 1.0f;
    }

    private List<MonsterEntryLevelStruct> LoadFloorMonsterEntries()
    {
        var realmService = GameServiceManager.Get<RealmService>();
        var floorConfig = realmService.GetFloorConfigByStageId(Global.StageId.Value);
        if (floorConfig == null)
        {
            LogTool.LogError($"秘境塔未找到关卡[{Global.StageId.Value}]对应的层配置");
            return new List<MonsterEntryLevelStruct>();
        }

        return realmService.GetFloorMonsterEntries(floorConfig.Id);
    }

    private void ApplyMonsterAttributeModifiers(GamePlayManager gpm)
    {
        // 关卡怪物属性修正
        foreach (var attr in gpm.StageCfg.MonsterAttributes)
        {
            var attrModifier = new AttributeModifier
            {
                SourceKey = $"{gpm.StageCfg.Id}",
                SourceType = AttributeSourceType.Stage,
                FlatAdd = attr.ValueType == ValueType.Number ? attr.Value : 0,
                PercentAdd = attr.ValueType == ValueType.Percentage ? attr.Value : 0
            };
            gpm.MonsterAttributes.GetAttr(attr.Type).AddModifier(attrModifier, true);
        }

        // 大秘境怪物词条属性修正
        foreach (var entry in gpm.MonsterEntries)
        {
            foreach (var attr in entry.Attributes)
            {
                var attrModifier = new AttributeModifier
                {
                    SourceKey = $"{gpm.StageCfg.Id}",
                    SourceType = AttributeSourceType.RealmRoom,
                    FlatAdd = attr.ValueType == ValueType.Number ? attr.Value : 0,
                    PercentAdd = attr.ValueType == ValueType.Percentage ? attr.Value : 0
                };
                gpm.MonsterAttributes.GetAttr(attr.Type).AddModifier(attrModifier, true);
            }
        }
    }
}
