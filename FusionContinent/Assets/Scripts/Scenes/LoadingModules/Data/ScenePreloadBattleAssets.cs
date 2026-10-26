using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FusionContinent;
using FusionContinent.Base;
using UIFramework;
using UnityEngine;

/// <summary>
/// 战斗资源预加载模块 - GamePlayManager._resLoader 是纯异步接口（Get 未预载直接抛异常），
/// 全平台都必须在数据阶段完成预载；WebGL 额外把 ObjectPoolManager/MonsterCard/EffectManager
/// 同步读的资源灌进 Global.ResLoader 缓存。需要 SceneInitMonsterConfig 先就绪（_monsterGenCfg）。
/// </summary>
public class ScenePreloadBattleAssets : SceneMapBase
{
    public override string ModuleName => "战斗资源预加载";

    public override void StartLoad()
    {
        StartCoroutine(LoadRoutine());
    }

    protected override void OnStartLoad()
    {
        // 加载逻辑在 LoadRoutine 协程里，StartLoad 不能走基类的同步完成路径
    }

    // GamePlayManager._resLoader 独立缓存（战斗结束 ClearAll）固定读取的预制体
    private static readonly string[] BattleResLoaderPrefabNames =
    {
        "SoldierCard",                // AddSoldierCard
        "CityDamage",                 // 城市/士兵受击飘字（SoldierDamageReceiver/MainCity/SoldierCard）
        "Bullet",                     // LvDouAttackBehaviour
        "guaiwu_gongji_01",           // MeleeImpactAttackBehaviour
        "waixingren_jn1_01",          // AlienLaserAttackBehaviour
        "waixingren_jn1_02",
        "waixingren_jn1_03",
        "shujing_cc_01",              // HamadryadAttackBehaviour
        "conglinqishi_jn1_02",        // SenLingAttackBehaviour
        "conglinqishi_jn1_03",
        "KarenWeapon01",              // AssassinDance
        "KarenSkillAssassinDance02",
        "MagicianWeapen01",           // MagicianLaserCannonAttack
        "fashi_weapen01",             // MagicianAttack
        "GusSkillSkyFlow03",          // TianLiuZhanProjectil
        "AnnaSkillStormPierce02rect", // StormPunctureAttackSkill
        "SkillCardUse01",             // 技能卡释放/冷却特效（BattleSkillsPanelNew/GameLand.SkillCards）
        "SkillCardUse02",
        "RewardEntity",               // GameSuccessPanel 结算
    };

#if UNITY_WEBGL
    // ObjectPoolManager 链路在战斗内同步取池的预制体（名字 = 实际文件名，AddressByFileName 大小写敏感）
    private static readonly string[] PoolPrefabNames =
    {
        "MonsterCard1",
        "SoldierCard",
        "Damage",
        "AnnaSkillArrows01",
        "kapaiposui02",
        "wanjianprefect",
        "AnnaNormalAttackHit01",
    };

    // SchoolChooseCardPanelNew.EffectCard 在面板打开时从 Global.ResLoader 同步读的品质特效
    private static readonly string[] ChooseCardEffectPrefabNames =
    {
        "ChooseCardColor01",
        "ChooseCardColor02",
        "ChooseCardGold01",
        "ChooseCardSilver01",
    };
#endif

    private IEnumerator LoadRoutine()
    {
        var gpm = GamePlayManager.Default;
        if (gpm == null || gpm._resLoader == null)
        {
            ErrorMessage = "GamePlayManager.Default 或其 _resLoader 未就绪";
        }
        else
        {
            // 进战斗前释放登录后预载的常驻面板实例(MainInterface/Shop/CardGroup),
            // 否则新手引导直连战斗时三个全屏面板及其图集整场战斗常驻
            UIManager.ReleaseAllPreloads();

            // 战斗图集预载：战斗内实体（MonsterCard/SoldierCard/GameLandUI 等）直接读 Global 图集字段，
            // 不经面板预加载，必须在进战斗前就绪；失败不阻断进战斗（Android 还有 atlasRequested 懒加载兜底）
            bool atlasOk = false;
            yield return AtlasManager.Instance.PreloadAsync(UIAtlasPreloadTable.BattleAtlasKeys)
                .ToCoroutine(r => atlasOk = r, ex => ErrorMessage = ex.Message);
            if (!atlasOk && string.IsNullOrEmpty(ErrorMessage))
            {
                LogTool.LogError($"[{ModuleName}] 战斗图集预加载不完整，局内图标可能缺失");
            }

            // _resLoader（AsyncResLoader）没有同步加载入口，战斗内 Get 的全部资源必须在这里预载完：
            // 固定预制体 + 出战队伍英灵 Spine + 特效配置全表
            var names = new List<string>(BattleResLoaderPrefabNames);
            names.AddRange(gpm.CollectTeamSoldierSpinePrefabKeys());
            names.AddRange(gpm.CollectBattlePreloadEffectNames());
            names.RemoveAll(name =>
            {
                if (gpm._resLoader.CheckLocationValid(name)) return false;
                LogTool.LogWarning($"[{ModuleName}] 配置引用但包内不存在，跳过预载: {name}");
                return true;
            });

            yield return gpm._resLoader.PreloadAsync<GameObject>(names)
                .ToCoroutine(ex => ErrorMessage = ex.Message);

#if UNITY_WEBGL
            if (string.IsNullOrEmpty(ErrorMessage))
            {
                yield return WebGLPreloadSharedCaches(gpm);
            }
#endif
        }

        progress = 1.0f;
        isDone = true;
        if (string.IsNullOrEmpty(ErrorMessage))
        {
            if (OnLoadComplete != null)
            {
                OnLoadComplete.Invoke();
            }
        }
        else
        {
            LogTool.LogError($"[{ModuleName}] 加载失败: {ErrorMessage}");
            if (OnLoadFailed != null)
            {
                OnLoadFailed.Invoke(ErrorMessage);
            }
        }
    }

#if UNITY_WEBGL
    /// <summary>
    /// WebGL 不支持同步加载：Global.ResLoader / EffectManager 的同步读全部改为命中这里的预载缓存。
    /// </summary>
    private IEnumerator WebGLPreloadSharedCaches(GamePlayManager gpm)
    {
        // 对象池预制体进 Global.ResLoader 缓存，ObjectPoolManager 的同步读命中
        yield return LoadPrefabs(Global.ResLoader, PoolPrefabNames);

        // 流派三选一面板品质特效（EffectCard 同步读 Global.ResLoader）
        yield return LoadPrefabs(Global.ResLoader, ChooseCardEffectPrefabNames);

        // 本关怪物 Spine 预制体（MonsterCard.Init 从 Global.ResLoader 同步读取，与渲染模式无关）
        foreach (var spineName in gpm.CollectStageMonsterSpinePrefabKeys())
        {
            if (!Global.ResLoader.CheckLocationValid(spineName))
            {
                LogTool.LogError($"[{ModuleName}] 资源未收集进包: {spineName}");
                continue;
            }
            yield return Global.ResLoader.LoadAsync<GameObject>(spineName, null);
        }

        // 本关怪物 Spine 烘焙帧字节
        yield return gpm.PreloadStageSpineBakeDataRoutine();

        // 战斗特效（EffectManager 自建缓存与池）
        if (EffectManager.Instance != null)
        {
            yield return EffectManager.Instance.PreloadEffectsAsync(gpm.CollectBattlePreloadEffectNames());
        }
        else
        {
            LogTool.LogError("[ScenePreloadBattleAssets] EffectManager.Instance is null，跳过特效预载");
        }
    }

    private IEnumerator LoadPrefabs(YooAssetLoader loader, IEnumerable<string> names)
    {
        foreach (var name in names)
        {
            if (!loader.CheckLocationValid(name))
            {
                LogTool.LogError($"[{ModuleName}] 资源未收集进包: {name}");
                continue;
            }
            yield return loader.LoadAsync<GameObject>(name, null);
        }
    }
#endif
}
