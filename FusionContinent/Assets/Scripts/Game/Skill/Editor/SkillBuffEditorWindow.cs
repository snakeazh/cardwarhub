#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Config;
using FusionContinent.Skill.Config;
using UnityEditor;
using UnityEngine;
using ValueType = Config.ValueType;

namespace FusionContinent.Skill.Editor
{
    public class SkillBuffEditorWindow : EditorWindow
    {
        private readonly List<SkillBuffEffectData> _effects = new List<SkillBuffEffectData>();
        private Action<List<SkillBuffEffectData>> _onApply;
        private Vector2 _scroll;
        private int _selectedIndex;

        public static void Open(string title, List<SkillBuffEffectData> effects, Action<List<SkillBuffEffectData>> onApply)
        {
            var w = CreateInstance<SkillBuffEditorWindow>();
            w.titleContent = new GUIContent(title);
            w._onApply = onApply;
            if (effects != null && effects.Count > 0) w._effects.AddRange(effects);
            if (w._effects.Count == 0) w._effects.Add(new SkillBuffEffectData());
            w.minSize = new Vector2(460, 620);
            w.ShowUtility();
        }

        private void OnGUI()
        {
            DrawToolbar();
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawEffectTabs();
            EditorGUILayout.Space(6);
            DrawCurrentEffect();
            EditorGUILayout.EndScrollView();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("应用到当前节点/模块", GUILayout.Height(28)))
                {
                    _onApply?.Invoke(new List<SkillBuffEffectData>(_effects));
                    Close();
                }
                if (GUILayout.Button("关闭", GUILayout.Height(28)))
                {
                    Close();
                }
            }
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("+ 添加效果", EditorStyles.toolbarButton, GUILayout.Width(80)))
                {
                    _effects.Add(CloneEffect(GetCurrentEffect()));
                    _selectedIndex = _effects.Count - 1;
                }

                GUI.enabled = _effects.Count > 1;
                if (GUILayout.Button("- 删除效果", EditorStyles.toolbarButton, GUILayout.Width(80)))
                {
                    _effects.RemoveAt(_selectedIndex);
                    _selectedIndex = Mathf.Clamp(_selectedIndex, 0, _effects.Count - 1);
                }
                GUI.enabled = true;

                GUILayout.FlexibleSpace();
                GUILayout.Label($"共 {_effects.Count} 个效果", EditorStyles.miniLabel);
            }
        }

        private void DrawEffectTabs()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                for (int i = 0; i < _effects.Count; i++)
                {
                    if (GUILayout.Toggle(_selectedIndex == i, $"效果 {i + 1}", "Button"))
                    {
                        _selectedIndex = i;
                    }
                }
            }
        }

        private void DrawCurrentEffect()
        {
            var e = GetCurrentEffect();
            if (e == null) return;

            EditorGUILayout.LabelField("Buff类型", EditorStyles.boldLabel);
            var names = Enum.GetNames(typeof(BuffType));
            var display = new string[names.Length];
            for (int i = 0; i < names.Length; i++) display[i] = $"{GetBuffTypeChineseName(names[i])} ({names[i]})";
            var values = (int[])Enum.GetValues(typeof(BuffType));
            int idx = Array.IndexOf(values, e.BuffTypeId);
            if (idx < 0) idx = 0;

            // 检测Buff类型是否改变
            EditorGUI.BeginChangeCheck();
            e.BuffTypeId = values[EditorGUILayout.Popup("Buff类型", idx, display)];
            if (EditorGUI.EndChangeCheck())
            {
                // Buff类型改变时，清空BuffId和相关字段
                e.BuffId = 0;
                e.AttrTypeName = "";
                e.AttrValueType = ValueType.Number;
                e.PercentBaseAttrName = "";
            }

            e.BuffId = EditorGUILayout.IntField("BuffID", e.BuffId);
            if (e.BuffTypeId == (int)BuffType.Attrbute)
            {
                e.AttrTypeName = EditorGUILayout.TextField("影响的属性值", e.AttrTypeName ?? "");
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("目标配置", EditorStyles.boldLabel);
            string[] src = { "Caster", "Target" };
            int srcIdx = Array.IndexOf(src, e.BuffValueSource ?? "Caster");
            if (srcIdx < 0) srcIdx = 0;
            e.BuffValueSource = src[EditorGUILayout.Popup("Buff数值来源者", srcIdx, new[] { "释放者", "受击者" })];

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("基础配置", EditorStyles.boldLabel);
            e.Stack = EditorGUILayout.IntField("叠加层数", e.Stack);
            e.StackAttrName = EditorGUILayout.TextField("层数属性名", e.StackAttrName ?? "");
            e.Probability = EditorGUILayout.Slider("触发概率", e.Probability, 0f, 1f);
            e.ProbabilityAttrName = EditorGUILayout.TextField("触发概率属性名", e.ProbabilityAttrName ?? "");
            e.Duration = EditorGUILayout.FloatField("持续时间(秒)", e.Duration);
            e.DurationAttrName = EditorGUILayout.TextField("持续时间属性名", e.DurationAttrName ?? "");
            e.TickInterval = EditorGUILayout.FloatField("触发间隔(秒)", e.TickInterval);
            e.TickIntervalAttrName = EditorGUILayout.TextField("触发间隔属性名", e.TickIntervalAttrName ?? "");

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("效果配置", EditorStyles.boldLabel);
            e.EffectValue = EditorGUILayout.FloatField("效果值", e.EffectValue);
            e.EffectValueAttrName = EditorGUILayout.TextField("效果值属性名", e.EffectValueAttrName ?? "");
            e.PercentEffectValue = EditorGUILayout.FloatField("百分比效果值", e.PercentEffectValue);
            e.PercentEffectValueAttrName = EditorGUILayout.TextField("百分比效果值属性名", e.PercentEffectValueAttrName ?? "");
            e.PercentValueSourceAttrName = EditorGUILayout.TextField("百分比数值来源", e.PercentValueSourceAttrName ?? "");
            var damageNames = Enum.GetNames(typeof(TakeDamageType));
            var damageValues = (int[])Enum.GetValues(typeof(TakeDamageType));
            int damageIdx = Array.IndexOf(damageValues, e.DamageType);
            if (damageIdx < 0) damageIdx = 0;
            e.DamageType = damageValues[EditorGUILayout.Popup("伤害类型", damageIdx, damageNames)];
            e.DamageTypeAttrName = EditorGUILayout.TextField("伤害类型属性名", e.DamageTypeAttrName ?? "");

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("特效", EditorStyles.boldLabel);
            e.BuffVfx = EditorGUILayout.TextField("Buff特效", e.BuffVfx ?? "");
            if (e.BuffTypeId == (int)BuffType.MoonlightShield)
            {
                e.ShieldVfx = EditorGUILayout.TextField("护盾特效", e.ShieldVfx ?? "");
                e.BreakVfx = EditorGUILayout.TextField("破裂特效", e.BreakVfx ?? "");
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.HelpBox("一个 Buff 配置现在可以包含多个效果，运行时会逐个生效。", MessageType.Info);
        }

        private SkillBuffEffectData GetCurrentEffect()
        {
            return _effects.Count == 0 ? null : _effects[Mathf.Clamp(_selectedIndex, 0, _effects.Count - 1)];
        }

        private static SkillBuffEffectData CloneEffect(SkillBuffEffectData src)
        {
            if (src == null) return new SkillBuffEffectData();
            return new SkillBuffEffectData
            {
                BuffTypeId = src.BuffTypeId,
                BuffId = src.BuffId,
                AttrTypeName = src.AttrTypeName,
                Stack = src.Stack,
                StackAttrName = src.StackAttrName,
                Duration = src.Duration,
                DurationAttrName = src.DurationAttrName,
                EffectValue = src.EffectValue,
                EffectValueAttrName = src.EffectValueAttrName,
                PercentEffectValue = src.PercentEffectValue,
                PercentEffectValueAttrName = src.PercentEffectValueAttrName,
                PercentValueSourceAttrName = src.PercentValueSourceAttrName,
                BuffValueSource = src.BuffValueSource,
                DamageType = src.DamageType,
                DamageTypeAttrName = src.DamageTypeAttrName,
                Probability = src.Probability,
                ProbabilityAttrName = src.ProbabilityAttrName,
                TickInterval = src.TickInterval,
                TickIntervalAttrName = src.TickIntervalAttrName,
                BuffVfx = src.BuffVfx,
                ShieldVfx = src.ShieldVfx,
                BreakVfx = src.BreakVfx,
            };
        }

        private static string GetBuffTypeChineseName(string n)
        {
            return n switch
            {
                "Burn" => "灼烧",
                "Vertigo" => "冻结",
                "Vulnerable" => "易损",
                "Moderate" => "减速",
                "KnockBack" => "击退",
                "Invincible" => "无敌",
                "Accelerate" => "加速",
                "RequiemChant" => "慰灵圣咏",
                "Disarm" => "缴械",
                "Health" => "血量",
                "Weak" => "虚弱",
                "MoonlightShield" => "月影庇护",
                "EnergyPouring" => "能量灌注",
                "Split" => "分裂",
                "Attrbute" => "属性类",
                "Execute" => "斩杀",
                _ => n
            };
        }
    }
}
#endif
