#if UNITY_EDITOR
using System.Collections.Generic;
using FusionContinent.Skill.Config;
using UnityEditor;
using UnityEngine;

namespace FusionContinent.Skill.Editor
{
    [CustomEditor(typeof(SkillBuffTemplateLibrary))]
    public class SkillBuffTemplateLibraryEditor : UnityEditor.Editor
    {
        private SkillBuffTemplateLibrary _target;

        private void OnEnable()
        {
            _target = target as SkillBuffTemplateLibrary;
        }

        public override void OnInspectorGUI()
        {
            if (_target == null) return;

            EditorGUILayout.LabelField("Buff 模板库", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("外部节点只需要填 BuffId。具体多效果内容在这个模板库里维护。", MessageType.Info);
            EditorGUILayout.Space();

            if (GUILayout.Button("+ 新建模板", GUILayout.Height(28)))
            {
                _target.Templates.Add(new SkillBuffTemplateEntry
                {
                    BuffId = GetNextBuffId(),
                    Name = "New Buff Template",
                    Effects = new List<SkillBuffEffectData> { new SkillBuffEffectData() }
                });
                EditorUtility.SetDirty(_target);
            }

            for (int i = 0; i < _target.Templates.Count; i++)
            {
                var template = _target.Templates[i];
                if (template == null) continue;

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                template.BuffId = EditorGUILayout.IntField("BuffId", template.BuffId);
                if (GUILayout.Button("删除", GUILayout.Width(60)))
                {
                    _target.Templates.RemoveAt(i);
                    EditorUtility.SetDirty(_target);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }
                EditorGUILayout.EndHorizontal();

                template.Name = EditorGUILayout.TextField("名称", template.Name ?? "");
                EditorGUILayout.LabelField($"效果数量: {template.Effects?.Count ?? 0}");

                if (GUILayout.Button("编辑模板效果", GUILayout.Height(24)))
                {
                    var effects = template.Effects ?? new List<SkillBuffEffectData>();
                    SkillBuffEditorWindow.Open($"Buff模板 {template.BuffId}", effects, updated =>
                    {
                        template.Effects = updated ?? new List<SkillBuffEffectData>();
                        EditorUtility.SetDirty(_target);
                        AssetDatabase.SaveAssets();
                    });
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(4);
            }

            if (GUI.changed)
            {
                EditorUtility.SetDirty(_target);
            }
        }

        private int GetNextBuffId()
        {
            int max = 1000;
            foreach (var t in _target.Templates)
            {
                if (t != null && t.BuffId > max) max = t.BuffId;
            }
            return max + 1;
        }
    }
}
#endif
