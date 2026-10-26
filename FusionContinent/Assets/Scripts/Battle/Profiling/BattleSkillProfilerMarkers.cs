using System;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Unity.Profiling;
#endif

/// <summary>
/// 技能释放链路 Profiler 采样点。仅在 Editor / Development Build 下采集数据。
/// Release 包中 <see cref="Begin"/> 为 no-op，无采样开销。
/// 注意：Marker 不得包裹 await（UniTask），否则 EndSample 会跨帧导致报错。
/// </summary>
public static class BattleSkillProfilerMarkers
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public static readonly ProfilerMarker UpdateSkill = new ProfilerMarker("Battle.UpdateSkill");
    public static readonly ProfilerMarker SkillRelease = new ProfilerMarker("Skill.Release");
    public static readonly ProfilerMarker TargetSearch = new ProfilerMarker("TargetSearch");
    public static readonly ProfilerMarker DamagePipeline = new ProfilerMarker("DamagePipeline");
    public static readonly ProfilerMarker EffectSpawn = new ProfilerMarker("EffectSpawn");
    public static readonly ProfilerMarker BulletSpawn = new ProfilerMarker("BulletSpawn");
    public static readonly ProfilerMarker BuffTrigger = new ProfilerMarker("BuffTrigger");
    public static readonly ProfilerMarker BuffTriggerGetTemplate = new ProfilerMarker("BuffTrigger.GetTemplate");
    public static readonly ProfilerMarker BuffTriggerResolveTargets = new ProfilerMarker("BuffTrigger.ResolveTargets");
    public static readonly ProfilerMarker BuffTriggerTriggerCondition = new ProfilerMarker("BuffTrigger.TriggerCondition");
    public static readonly ProfilerMarker BuffTriggerApplyBuff = new ProfilerMarker("BuffTrigger.ApplyBuff");
    public static readonly ProfilerMarker BuffTriggerIterateBuff = new ProfilerMarker("BuffTrigger.IterateBuff");
    public static readonly ProfilerMarker BuffTriggerCreateBuff = new ProfilerMarker("BuffTrigger.CreateBuff");
    public static readonly ProfilerMarker BuffTriggerAttributeModify = new ProfilerMarker("BuffTrigger.AttributeModify");
    public static readonly ProfilerMarker BuffTriggerDamageModifier = new ProfilerMarker("BuffTrigger.DamageModifier");
    public static readonly ProfilerMarker BuffTriggerCreateContext = new ProfilerMarker("BuffTrigger.CreateContext");
    public static readonly ProfilerMarker BuffTriggerTempList = new ProfilerMarker("BuffTrigger.TempList");
    public static readonly ProfilerMarker BuffTriggerEventDispatch = new ProfilerMarker("BuffTrigger.EventDispatch");
    public static readonly ProfilerMarker BuffTriggerRuleCheck = new ProfilerMarker("BuffTrigger.RuleCheck");
    public static readonly ProfilerMarker BuffTriggerCounterUpdate = new ProfilerMarker("BuffTrigger.CounterUpdate");
    public static readonly ProfilerMarker BuffTriggerMoonlightBundle = new ProfilerMarker("BuffTrigger.MoonlightBundle");
    public static readonly ProfilerMarker RuleExecute = new ProfilerMarker("RuleExecute");
    public static readonly ProfilerMarker CounterUpdate = new ProfilerMarker("CounterUpdate");
    public static readonly ProfilerMarker TweenPlay = new ProfilerMarker("TweenPlay");
    public static readonly ProfilerMarker ObjectPoolGet = new ProfilerMarker("ObjectPool.Get");
    public static readonly ProfilerMarker ObjectPoolRelease = new ProfilerMarker("ObjectPool.Release");

    public static ProfilerMarker.AutoScope Begin(ProfilerMarker marker)
    {
        return marker.Auto();
    }
#else
    public static readonly object UpdateSkill = null;
    public static readonly object SkillRelease = null;
    public static readonly object TargetSearch = null;
    public static readonly object DamagePipeline = null;
    public static readonly object EffectSpawn = null;
    public static readonly object BulletSpawn = null;
    public static readonly object BuffTrigger = null;
    public static readonly object BuffTriggerGetTemplate = null;
    public static readonly object BuffTriggerResolveTargets = null;
    public static readonly object BuffTriggerTriggerCondition = null;
    public static readonly object BuffTriggerApplyBuff = null;
    public static readonly object BuffTriggerIterateBuff = null;
    public static readonly object BuffTriggerCreateBuff = null;
    public static readonly object BuffTriggerAttributeModify = null;
    public static readonly object BuffTriggerDamageModifier = null;
    public static readonly object BuffTriggerCreateContext = null;
    public static readonly object BuffTriggerTempList = null;
    public static readonly object BuffTriggerEventDispatch = null;
    public static readonly object BuffTriggerRuleCheck = null;
    public static readonly object BuffTriggerCounterUpdate = null;
    public static readonly object BuffTriggerMoonlightBundle = null;
    public static readonly object RuleExecute = null;
    public static readonly object CounterUpdate = null;
    public static readonly object TweenPlay = null;
    public static readonly object ObjectPoolGet = null;
    public static readonly object ObjectPoolRelease = null;

    public static IDisposable Begin(object marker)
    {
        return null;
    }
#endif
}
