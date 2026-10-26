using System;
using System.Collections.Generic;
using System.Linq;
using UIFramework.Services;
using Unity.Profiling;
using UnityEngine;

namespace FusionContinent.Skill.Config
{
    [Serializable]
    public class SkillBuffTemplateEntry
    {
        public int BuffId;
        public string Name = "New Buff Template";
        public List<SkillBuffEffectData> Effects = new List<SkillBuffEffectData>();
    }

    [CreateAssetMenu(fileName = "SkillBuffTemplateLibrary", menuName = "FusionContinent/Skill/Buff Template Library")]
    public class SkillBuffTemplateLibrary : ScriptableObject
    {
        [SerializeField] private List<SkillBuffTemplateEntry> _templates = new List<SkillBuffTemplateEntry>();

        public List<SkillBuffTemplateEntry> Templates => _templates;

        public bool TryGetTemplate(int buffId, out SkillBuffTemplateEntry entry)
        {
            entry = _templates.FirstOrDefault(t => t != null && t.BuffId == buffId);
            return entry != null;
        }
    }

    public static class SkillBuffTemplateProvider
    {
        private static SkillBuffTemplateLibrary _cachedLibrary;

        public static List<SkillBuffEffectData> GetEffects(int buffId)
        {
            using (BattleSkillProfilerMarkers.Begin(BattleSkillProfilerMarkers.BuffTriggerGetTemplate))
            {
            if (buffId <= 0) return new List<SkillBuffEffectData>();
            var library = GetLibrary();
            if (library == null) return new List<SkillBuffEffectData>();
            if (!library.TryGetTemplate(buffId, out var template) || template.Effects == null)
            {
                return new List<SkillBuffEffectData>();
            }

            List<SkillBuffEffectData> result;
            using (BattleSkillProfilerMarkers.Begin(BattleSkillProfilerMarkers.BuffTriggerTempList))
            {
                result = new List<SkillBuffEffectData>();
            }
            foreach (var effect in template.Effects)
            {
                if (effect == null) continue;
                result.Add(new SkillBuffEffectData
                {
                    BuffTypeId = effect.BuffTypeId,
                    BuffId = effect.BuffId,
                    AttrTypeName = effect.AttrTypeName,
                    Stack = effect.Stack,
                    StackAttrName = effect.StackAttrName,
                    Duration = effect.Duration,
                    DurationAttrName = effect.DurationAttrName,
                    EffectValue = effect.EffectValue,
                    EffectValueAttrName = effect.EffectValueAttrName,
                    PercentEffectValue = effect.PercentEffectValue,
                    PercentEffectValueAttrName = effect.PercentEffectValueAttrName,
                    PercentValueSourceAttrName = effect.PercentValueSourceAttrName,
                    BuffValueSource = effect.BuffValueSource,
                    DamageType = effect.DamageType,
                    DamageTypeAttrName = effect.DamageTypeAttrName,
                    Probability = effect.Probability,
                    ProbabilityAttrName = effect.ProbabilityAttrName,
                    TickInterval = effect.TickInterval,
                    TickIntervalAttrName = effect.TickIntervalAttrName,
                    BuffVfx = effect.BuffVfx,
                    ShieldVfx = effect.ShieldVfx,
                    BreakVfx = effect.BreakVfx,
                });
            }
            return result;
            }
        }

        public static SkillBuffTemplateLibrary GetLibrary()
        {
            if (_cachedLibrary != null)
            {
                return _cachedLibrary;
            }
            if(GamePlayManager.Default!=null)
                _cachedLibrary = GamePlayManager.Default._skillConfigManager.BuffTemplateLibrary;

#if UNITY_EDITOR
            if (_cachedLibrary == null)
            {
                _cachedLibrary = LoadEditorLibrary();
            }
#endif

            return _cachedLibrary;
        }

        public static void SetLibrary(SkillBuffTemplateLibrary library)
        {
            _cachedLibrary = library;
        }

        public static void ClearCache()
        {
            _cachedLibrary = null;
        }

#if UNITY_EDITOR
        private static SkillBuffTemplateLibrary LoadEditorLibrary()
        {
            const string preferredPath = "Assets/Resources/Skill/SkillBuffTemplateLibrary.asset";
            var preferredLibrary = UnityEditor.AssetDatabase.LoadAssetAtPath<SkillBuffTemplateLibrary>(preferredPath);
            if (preferredLibrary != null)
            {
                return preferredLibrary;
            }

            var guids = UnityEditor.AssetDatabase.FindAssets("t:SkillBuffTemplateLibrary");
            foreach (var guid in guids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var library = UnityEditor.AssetDatabase.LoadAssetAtPath<SkillBuffTemplateLibrary>(path);
                if (library != null)
                {
                    return library;
                }
            }

            return null;
        }
#endif
    }
}
