#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace FusionContinent.Skill.Editor
{
    public partial class SkillNodeEditorWindow
    {
        private void DrawProjectileBurnAreaParams()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("移动留灼烧区", EditorStyles.boldLabel);

            var spawnBurnAreaOnMove = EditorGUILayout.Toggle("启用移动留火区", _nodeConfig.GetParamBool("spawn_burn_area_on_move", false));
            _nodeConfig.SetParam("spawn_burn_area_on_move", spawnBurnAreaOnMove.ToString());

            if (!spawnBurnAreaOnMove)
            {
                return;
            }

            string[] burnAreaTriggerModes = { "Time", "Distance" };
            int burnAreaTriggerIndex = Array.IndexOf(burnAreaTriggerModes, _nodeConfig.GetParamString("burn_area_trigger_mode", "Distance"));
            if (burnAreaTriggerIndex < 0)
            {
                burnAreaTriggerIndex = 1;
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("触发方式", GUILayout.Width(130));
            burnAreaTriggerIndex = EditorGUILayout.Popup(burnAreaTriggerIndex, burnAreaTriggerModes);
            _nodeConfig.SetParam("burn_area_trigger_mode", burnAreaTriggerModes[burnAreaTriggerIndex]);
            EditorGUILayout.EndHorizontal();

            if (burnAreaTriggerModes[burnAreaTriggerIndex] == "Time")
            {
                DrawBurnAreaFloatWithAttr("时间间隔(秒)", "burn_area_time_interval", "burn_area_time_interval_attr", 0.2f);
            }
            else
            {
                DrawBurnAreaFloatWithAttr("距离间隔(米)", "burn_area_distance_interval", "burn_area_distance_interval_attr", 1.5f);
            }

            DrawAssetSelector("灼烧区域特效", "burn_area_effect", "Prefab");

            DrawBurnAreaFloatWithAttr("区域持续时间(秒)", "burn_area_duration", "burn_area_duration_attr", 2f);
            DrawBurnAreaFloatWithAttr("区域半径(米)", "burn_area_radius", "burn_area_radius_attr", 2f);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("区域内伤害", EditorStyles.miniLabel);
            DrawBurnAreaFloatWithAttr("伤害间隔(秒)", "burn_area_tick_interval", "burn_area_tick_interval_attr", 1f);
            DrawBurnAreaFloatWithAttr("固定伤害(每次)", "burn_area_damage", "burn_area_damage_attr", 10f);

            var useSkillDamage = EditorGUILayout.Toggle(
                "使用技能伤害(ProjectileHitEvent)",
                _nodeConfig.GetParamBool("burn_area_use_skill_damage", false));
            _nodeConfig.SetParam("burn_area_use_skill_damage", useSkillDamage.ToString());
            EditorGUILayout.HelpBox(
                "勾选后每次伤害间隔走技能完整伤害公式；未勾选则对范围内敌人造成「固定伤害」。固定伤害≤0 时不造成伤害。",
                MessageType.None);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("区域内 Buff（可选）", EditorStyles.miniLabel);

            var buffId = DrawBuffTemplateIdField("Buff模板Id", _nodeConfig.GetParamInt("burn_area_buff_id", 0));
            _nodeConfig.SetParam("burn_area_buff_id", buffId.ToString());
            EditorGUILayout.HelpBox("填 0 表示区域内不上 Buff，仅按间隔造成伤害。", MessageType.None);

            DrawBurnAreaFloatWithAttr("叠层所需停留(秒)", "burn_area_buff_stay_time", "burn_area_buff_stay_time_attr", 0f);
            EditorGUILayout.HelpBox(
                "每次成功受到区域伤害时累计停留时间（累计量=伤害间隔）。累计达到该秒数后施加 1 层 Buff，可多次叠层；离开区域后计时清零。BuffId=0 或停留≤0 时不挂 Buff。",
                MessageType.None);

            EditorGUILayout.HelpBox(
                "子弹移动过程中按时间/距离留下灼烧区域。区域内敌人按「伤害间隔」持续受伤；满足停留时间后按 Buff 模板叠层。\n" +
                "数值可填固定值；填写对应「属性名」后优先从技能 AttributeType 读取。",
                MessageType.None);
        }

        private void DrawBurnAreaFloatWithAttr(string label, string paramKey, string attrKey, float defaultValue)
        {
            var value = EditorGUILayout.FloatField(label, _nodeConfig.GetParamFloat(paramKey, defaultValue));
            _nodeConfig.SetParam(paramKey, value.ToString());

            var attrName = EditorGUILayout.TextField(label + "属性名", _nodeConfig.GetParamString(attrKey, ""));
            _nodeConfig.SetParam(attrKey, attrName);
        }
    }
}
#endif
