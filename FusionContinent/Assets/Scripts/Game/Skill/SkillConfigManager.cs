using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Config;
using Cysharp.Threading.Tasks;
using FusionContinent;
using FusionContinent.Skill.Config;
using UnityEngine;
using YooAsset;
/// <summary>
/// 技能节点配置获取
/// </summary>
public class SkillConfigManager
{
    /// <summary>
    /// 老技能（SkillBase.Cast）：持续场景/周期释放类，不走 SkillGraph asset。
    /// 注意：2110002（咒缚雷电）与 2110004（雷霆领域）LogicType 相同，必须按技能 Id 区分。
    /// </summary>
    private static readonly HashSet<int> LegacyCastOnlySkillIds = new HashSet<int>
    {
        2110001, // 碎骨斩
        2110004, // 雷霆领域
        2110005, // 风舞疾袭
        2110006, // 测试全屏伤害
    };

    public static IReadOnlyCollection<int> GetLegacyCastOnlySkillIds()
    {
        return LegacyCastOnlySkillIds;
    }

    private Dictionary<int, SkillGraphConfig> _map = new();
    public SkillBuffTemplateLibrary BuffTemplateLibrary { get; private set; } 
    
    public async UniTask InitBuffTemplateLibrary()
    {
        var handle = YooAssets.LoadAllAssetsAsync<SkillBuffTemplateLibrary>("SkillGraph_SkillBuffTemplateLibrary");
        await handle.Task;

        if (handle.AllAssetObjects != null)
        {
            foreach (var asset in handle.AllAssetObjects)
            {
                if (asset is SkillBuffTemplateLibrary library)
                {
                    BuffTemplateLibrary = library;
                    SkillBuffTemplateProvider.SetLibrary(library);
                    LogTool.Log($"[SkillService] 已加载 Buff 模板库: {library.name}");

                    // Buff 特效随模板库预载：WebGL 只能同步读已预载资源，
                    // 否则 Buff 生效时 EffectManager 报"未预载"
                    await EffectManager.Instance.PreloadEffectsUniTask(CollectBuffTemplateEffectNames(library));
                    return;
                }
            }
        }

        LogTool.LogWarning("[SkillService] 未加载到 Buff 模板库，请确认资源已纳入 YooAsset 分组并命名为 SkillBuffTemplateLibrary");
    }
    
    
    public static bool IsLegacyCastOnlySkill(int skillId)
    {
        return LegacyCastOnlySkillIds.Contains(skillId);
    }

    public void ClearLegacyCastOnlySkillGraphs()
    {
        var legacySkillIds = _map.Keys.Where(IsLegacyCastOnlySkill).ToList();
        foreach (var skillId in legacySkillIds)
        {
            _map.Remove(skillId);
            LogTool.Log($"[SkillConfigManager] 移除老场景技能图缓存: {skillId}");
        }
    }

    public async UniTask SkillConfigInsert(int GraphNameID)
    {
        if (IsLegacyCastOnlySkill(GraphNameID))
        {
            return;
        }

        if (_map.ContainsKey(GraphNameID))
        {
            LogTool.L("重复 graphId 不需要加了");
            return;
        }

        var handle = YooAssets.LoadAllAssetsAsync<SkillGraphConfig>("SkillGraph_"+GraphNameID);
        await handle.Task;
        if (handle.AllAssetObjects == null)
        {
            handle.Release();
            
            return;
        }
        foreach (var asset in handle.AllAssetObjects)
        {
            var config = asset as SkillGraphConfig;
            if(config == null) continue;
            if (_map.ContainsKey(config.SkillId))
            {
                LogTool.LogError($"重复 graphId: {config.SkillId}");
                continue;
            }

            // 先预载图谱节点引用的特效再开放使用：WebGL 只能同步读已预载资源，
            // 否则技能首次释放时 EffectManager 报"未预载"
            await EffectManager.Instance.PreloadEffectsUniTask(CollectGraphEffectNames(config));

            LogTool.Log($"加入 graphId: {config.SkillId}  graphName: {config.GraphName}");

            _map.Add(config.SkillId, config);
        }
    }

    // 节点参数里存放特效/弹道资源名的 key（与各 Handler 的 GetParamString 调用对应）
    private static readonly string[] EffectParamKeys =
    {
        "vfx", "crater_vfx", "prefab", "hit_effect", "end_effect", "burn_area_effect", "shield_vfx", "break_vfx",
    };

    private static List<string> CollectGraphEffectNames(SkillGraphConfig config)
    {
        var names = new HashSet<string>();
        foreach (var node in config.Nodes)
        {
            if (node == null) continue;
            foreach (var key in EffectParamKeys)
            {
                TryAddEffectName(names, node.GetParamString(key, ""));
            }
        }
        return new List<string>(names);
    }

    private static List<string> CollectBuffTemplateEffectNames(SkillBuffTemplateLibrary library)
    {
        var names = new HashSet<string>();
        foreach (var template in library.Templates)
        {
            if (template == null || template.Effects == null) continue;
            foreach (var effect in template.Effects)
            {
                if (effect == null) continue;
                TryAddEffectName(names, effect.BuffVfx);
                TryAddEffectName(names, effect.ShieldVfx);
                TryAddEffectName(names, effect.BreakVfx);
            }
        }
        return new List<string>(names);
    }

    private static void TryAddEffectName(HashSet<string> names, string value)
    {
        // 非 YooAsset 地址的值（如 Resources 路径默认值）由 Resources 兜底加载，跳过
        if (!string.IsNullOrEmpty(value) && Global.ResLoader.CheckLocationValid(value))
        {
            names.Add(value);
        }
    }

    public SkillGraphConfig Get(int skillId)
    {
        _map.TryGetValue(skillId, out var config);
        return config;
    }

    public void OnDisable()
    {
        _map.Clear();
    }
}
