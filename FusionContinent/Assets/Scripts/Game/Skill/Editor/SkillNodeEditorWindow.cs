#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Config;
using FusionContinent.Skill.Config;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FusionContinent.Skill.Editor
{
    /// <summary>
    /// 技能节点编辑器窗口 - 用于编辑节点参数
    /// </summary>
    public partial class SkillNodeEditorWindow : EditorWindow
    {
        private SkillNodeConfig _nodeConfig;
        private Action _onCloseCallback;
        private Vector2 _scrollPosition;
        private string _selectedNodeType;
        private string _currentPickerParamKey;

        // 缓存的字典字段引用
        private SerializedObject _serializedObject;
        private SerializedProperty _paramsProperty;

        public static void OpenWindow(SkillNodeConfig nodeConfig, Action onCloseCallback = null)
        {
            var window = GetWindow<SkillNodeEditorWindow>();
            window.titleContent = new GUIContent($"编辑节点: {nodeConfig.NodeType}");
            window._nodeConfig = nodeConfig;
            window._onCloseCallback = onCloseCallback;
            window._selectedNodeType = nodeConfig.NodeType;
            window.minSize = new Vector2(350, 400);
            window.Show();
        }

        private void OnEnable()
        {
            if (_nodeConfig != null)
            {
                _serializedObject = new SerializedObject(_nodeConfig);
                _paramsProperty = _serializedObject.FindProperty("_params");
            }
        }

        private void OnGUI()
        {
            if (_nodeConfig == null)
            {
                EditorGUILayout.HelpBox("节点配置为空", MessageType.Warning);
                return;
            }

            _serializedObject?.Update();

            // 处理资源选择器回调
            HandleObjectPicker();

            EditorGUILayout.Space(10);

            // 节点类型标题
            EditorGUILayout.LabelField("节点类型", _nodeConfig.NodeType, EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // 节点 ID
            EditorGUILayout.LabelField("节点 ID", _nodeConfig.NodeId);
            EditorGUILayout.Space(10);

            // 根据节点类型显示参数编辑器
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawNodeSpecificParams(_selectedNodeType);

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(10);

            // 保存按钮
            if (GUILayout.Button("应用更改", GUILayout.Height(30)))
            {
                ApplyChanges();
            }

            EditorGUILayout.Space(5);

            // 预设按钮
            DrawPresetButtons();

            _serializedObject?.ApplyModifiedProperties();
        }

        private void DrawNodeSpecificParams(string nodeType)
        {
            EditorGUILayout.LabelField("参数配置", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            switch (nodeType)
            {
                case "Cast":
                    DrawCastParams();
                    break;

                case "Projectile":
                    DrawProjectileParams();
                    break;

                case "AoeDamage":
                    DrawAoeDamageParams();
                    break;

                case "AoeSelect":
                    DrawAoeSelectParams();
                    break;

                case "SelectTarget":
                    DrawSelectTargetParams();
                    break;

                case "PullMonster":
                    DrawPullMonsterParams();
                    break;

                case "Delay":
                    DrawDelayParams();
                    break;

                case "PlayMixAnimation":
                    DrawPlayMixAnimationParams();
                    break;

                case "StopMixAnimation":
                    DrawStopMixAnimationParams();
                    break;

                case "ReturnAnimation":
                    DrawReturnAnimationParams();
                    break;

                case "Effect":
                    DrawEffectParams();
                    break;

                case "AnimatedEffect":
                    DrawAnimatedEffectParams();
                    break;

                case "Sound":
                    DrawSoundParams();
                    break;

                case "ResetOffset":
                    DrawResetOffsetParams();
                    break;

                case "RecycleEffect":
                    DrawRecycleEffectParams();
                    break;

                case "ApplyBuff":
                    DrawApplyBuffParams();
                    break;

                case "Branch":
                    DrawBranchParams();
                    break;

                case "Loop":
                    DrawLoopParams();
                    break;

                case "LoopSequential":
                    DrawLoopSequentialParams();
                    break;

                case "Fork":
                    DrawForkParams();
                    break;

                case "MultiProjectile":
                    DrawMultiProjectileParams();
                    break;

                case "MultiTargetProjectile":
                    DrawMultiTargetProjectileParams();
                    break;

                case "WaveProjectile":
                    DrawWaveProjectileParams();
                    break;

                case "FanProjectile":
                    DrawFanProjectileParams();
                    break;

                default:
                    EditorGUILayout.HelpBox($"未知节点类型: {nodeType}", MessageType.Warning);
                    DrawGenericParams();
                    break;
            }
        }

        /// <summary>
        /// 绘制资源选择器（文本框 + 浏览按钮）
        /// </summary>
        private void DrawAssetSelector(string label, string paramKey, string assetFilter)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(label, GUILayout.Width(130));

                var currentValue = _nodeConfig.GetParamString(paramKey, "");
                var newValue = EditorGUILayout.TextField(currentValue);

                if (newValue != currentValue)
                {
                    _nodeConfig.SetParam(paramKey, newValue);
                }

                if (GUILayout.Button("浏览", GUILayout.Width(50)))
                {
                    ShowAssetPicker(paramKey, assetFilter);
                }

                // 清除按钮
                if (!string.IsNullOrEmpty(currentValue) && GUILayout.Button("×", GUILayout.Width(25)))
                {
                    _nodeConfig.SetParam(paramKey, "");
                }
            }
        }

        private void ShowAssetPicker(string paramKey, string filter)
        {
            _currentPickerParamKey = paramKey;

            // 获取当前已选择的资源
            var currentPath = _nodeConfig.GetParamString(paramKey, "");
            Object currentAsset = null;

            if (!string.IsNullOrEmpty(currentPath))
            {
                currentAsset = Resources.Load(currentPath);
            }

            // 弹出资源选择器
            EditorGUIUtility.ShowObjectPicker<Object>(currentAsset, false, filter, 0);
        }

        private void HandleObjectPicker()
        {
            if (Event.current.commandName == "ObjectSelectorClosed")
            {
                var selectedObject = EditorGUIUtility.GetObjectPickerObject();
                if (selectedObject != null && !string.IsNullOrEmpty(_currentPickerParamKey))
                {
                    // 转换为 Resources 相对路径
                    var path = AssetDatabase.GetAssetPath(selectedObject);
                    var resourcesPath = GetResourcesPath(path);
                    _nodeConfig.SetParam(_currentPickerParamKey, resourcesPath);
                }
                _currentPickerParamKey = null;
            }
        }

        private string GetResourcesPath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return "";

            // 移除 Assets/Resources/ 前缀
            var resourcesPrefix = "Resources/";
            var index = assetPath.IndexOf(resourcesPrefix, StringComparison.OrdinalIgnoreCase);

            if (index >= 0)
            {
                return assetPath.Substring(index + resourcesPrefix.Length);
            }

            // 如果不在 Resources 下，尝试移除 Assets/ 前缀
            if (assetPath.StartsWith("Assets/"))
            {
                return assetPath.Substring("Assets/".Length);
            }

            return assetPath;
        }


        private void DrawBuffEditorLauncher(string buttonText, Action onClick)
        {
            EditorGUILayout.Space(4);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(buttonText, GUILayout.Width(180), GUILayout.Height(24)))
                {
                    onClick?.Invoke();
                }
            }
            EditorGUILayout.HelpBox("试用独立 Buff 编辑器。确认顺手后，再迁移老的内联编辑。", MessageType.None);
            EditorGUILayout.Space(4);
        }

        private List<SkillBuffEffectData> CreateApplyBuffEditorData()
        {
            var multiRaw = _nodeConfig.GetParamString("buff_effects", "");
            var multi = SkillBuffEffectSerializer.Deserialize(multiRaw);
            if (multi.Count > 0)
            {
                return multi;
            }

            return new List<SkillBuffEffectData>
            {
                new SkillBuffEffectData
                {
                    BuffTypeId = _nodeConfig.GetParamInt("buff_id", 0),
                    BuffId = _nodeConfig.GetParamInt("buff_instance_id", _nodeConfig.GetParamInt("buff_id", 0)),
                    AttrTypeName = _nodeConfig.GetParamString("attr_type_name", ""),
                    Stack = _nodeConfig.GetParamInt("stack", 1),
                    StackAttrName = _nodeConfig.GetParamString("stack_attr", ""),
                    Duration = _nodeConfig.GetParamFloat("duration", -1f),
                    DurationAttrName = _nodeConfig.GetParamString("duration_attr", ""),
                    EffectValue = _nodeConfig.GetParamFloat("effect_value", 0f),
                    EffectValueAttrName = _nodeConfig.GetParamString("effect_value_attr", ""),
                    PercentEffectValue = _nodeConfig.GetParamFloat("percent_effect_value", 0f),
                    PercentEffectValueAttrName = _nodeConfig.GetParamString("percent_effect_value_attr", ""),
                    PercentValueSourceAttrName = _nodeConfig.GetParamString("percent_value_source_attr", ""),
                    BuffValueSource = _nodeConfig.GetParamString("buff_value_source", "Caster"),
                    DamageType = _nodeConfig.GetParamInt("damage_type", 0),
                    DamageTypeAttrName = _nodeConfig.GetParamString("damage_type_attr", ""),
                    Probability = _nodeConfig.GetParamFloat("probability", 1f),
                    ProbabilityAttrName = _nodeConfig.GetParamString("probability_attr", ""),
                    TickInterval = _nodeConfig.GetParamFloat("tick_interval", 0f),
                    TickIntervalAttrName = _nodeConfig.GetParamString("tick_interval_attr", ""),
                    BuffVfx = _nodeConfig.GetParamString("buff_vfx", ""),
                    ShieldVfx = _nodeConfig.GetParamString("shield_vfx", "EliaSkillMoonshadowWard01"),
                    BreakVfx = _nodeConfig.GetParamString("break_vfx", "EliaSkillMoonshadowWard02"),
                }
            };
        }

        private void ApplyBuffEditorData(List<SkillBuffEffectData> effects)
        {
            _nodeConfig.SetParam("buff_effects", SkillBuffEffectSerializer.Serialize(effects));
            if (effects == null || effects.Count == 0)
            {
                return;
            }

            var data = effects[0];
            _nodeConfig.SetParam("buff_id", data.BuffTypeId.ToString());
            _nodeConfig.SetParam("buff_instance_id", data.BuffId.ToString());
            _nodeConfig.SetParam("attr_type_name", data.AttrTypeName ?? "");
            _nodeConfig.SetParam("stack", data.Stack.ToString());
            _nodeConfig.SetParam("stack_attr", data.StackAttrName ?? "");
            _nodeConfig.SetParam("duration", data.Duration.ToString());
            _nodeConfig.SetParam("duration_attr", data.DurationAttrName ?? "");
            _nodeConfig.SetParam("effect_value", data.EffectValue.ToString());
            _nodeConfig.SetParam("effect_value_attr", data.EffectValueAttrName ?? "");
            _nodeConfig.SetParam("percent_effect_value", data.PercentEffectValue.ToString());
            _nodeConfig.SetParam("percent_effect_value_attr", data.PercentEffectValueAttrName ?? "");
            _nodeConfig.SetParam("percent_value_source_attr", data.PercentValueSourceAttrName ?? "");
            _nodeConfig.SetParam("buff_value_source", data.BuffValueSource ?? "Caster");
            _nodeConfig.SetParam("damage_type", data.DamageType.ToString());
            _nodeConfig.SetParam("damage_type_attr", data.DamageTypeAttrName ?? "");
            _nodeConfig.SetParam("probability", data.Probability.ToString());
            _nodeConfig.SetParam("probability_attr", data.ProbabilityAttrName ?? "");
            _nodeConfig.SetParam("tick_interval", data.TickInterval.ToString());
            _nodeConfig.SetParam("tick_interval_attr", data.TickIntervalAttrName ?? "");
            _nodeConfig.SetParam("buff_vfx", data.BuffVfx ?? "");
            _nodeConfig.SetParam("shield_vfx", data.ShieldVfx ?? "EliaSkillMoonshadowWard01");
            _nodeConfig.SetParam("break_vfx", data.BreakVfx ?? "EliaSkillMoonshadowWard02");
        }

        private List<SkillBuffEffectData> CreateAttachBuffEditorData(HitModuleEntry entry)
        {
            var multi = SkillBuffEffectSerializer.Deserialize(entry.StringParam5 ?? "");
            if (multi.Count > 0)
            {
                return multi;
            }

            return new List<SkillBuffEffectData>
            {
                new SkillBuffEffectData
                {
                    BuffTypeId = entry.IntParam1,
                    BuffId = entry.IntParam5 > 0 ? entry.IntParam5 : entry.IntParam1,
                    AttrTypeName = entry.StringParam3 ?? "",
                    Stack = entry.IntParam2,
                    StackAttrName = entry.IntParam2Attr ?? "",
                    Duration = entry.FloatParam1,
                    DurationAttrName = entry.FloatParam1Attr ?? "",
                    EffectValue = entry.FloatParam2,
                    EffectValueAttrName = entry.FloatParam2Attr ?? "",
                    PercentEffectValue = entry.FloatParam5,
                    PercentEffectValueAttrName = entry.FloatParam5Attr ?? "",
                    PercentValueSourceAttrName = entry.StringParam2 ?? "",
                    BuffValueSource = entry.BoolParam2 ? "Target" : "Caster",
                    DamageType = entry.IntParam4,
                    DamageTypeAttrName = entry.IntParam4Attr ?? "",
                    Probability = entry.FloatParam4,
                    ProbabilityAttrName = entry.FloatParam4Attr ?? "",
                    TickInterval = entry.FloatParam3,
                    TickIntervalAttrName = entry.FloatParam3Attr ?? "",
                    BuffVfx = entry.StringParam1 ?? ""
                }
            };
        }

        private HitModuleEntry ApplyAttachBuffEditorData(HitModuleEntry entry, List<SkillBuffEffectData> effects)
        {
            entry.StringParam5 = SkillBuffEffectSerializer.Serialize(effects);
            if (effects == null || effects.Count == 0)
            {
                return entry;
            }

            var data = effects[0];
            entry.IntParam1 = data.BuffTypeId;
            entry.IntParam5 = data.BuffId;
            entry.StringParam3 = data.AttrTypeName ?? "";
            entry.IntParam2 = data.Stack;
            entry.IntParam2Attr = data.StackAttrName ?? "";
            entry.FloatParam1 = data.Duration;
            entry.FloatParam1Attr = data.DurationAttrName ?? "";
            entry.FloatParam2 = data.EffectValue;
            entry.FloatParam2Attr = data.EffectValueAttrName ?? "";
            entry.FloatParam5 = data.PercentEffectValue;
            entry.FloatParam5Attr = data.PercentEffectValueAttrName ?? "";
            entry.StringParam2 = data.PercentValueSourceAttrName ?? "";
            entry.BoolParam2 = data.BuffValueSource == "Target";
            entry.IntParam4 = data.DamageType;
            entry.IntParam4Attr = data.DamageTypeAttrName ?? "";
            entry.FloatParam4 = data.Probability;
            entry.FloatParam4Attr = data.ProbabilityAttrName ?? "";
            entry.FloatParam3 = data.TickInterval;
            entry.FloatParam3Attr = data.TickIntervalAttrName ?? "";
            entry.StringParam1 = data.BuffVfx ?? "";
            return entry;
        }

        private int DrawBuffTemplateIdField(string label, int currentBuffId)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(130));
            currentBuffId = EditorGUILayout.IntField(currentBuffId);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox("这里填 Buff 模板库中的 BuffId。具体效果在 Buff 模板库里维护。", MessageType.None);
            DrawBuffTemplateSummary(currentBuffId);
            return currentBuffId;
        }

        private void DrawOpenBuffLibraryButton(int buffId)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("打开 Buff 模板库", GUILayout.Width(160), GUILayout.Height(24)))
                {
                    OpenBuffTemplateLibrary(buffId);
                }
            }
        }

        private void OpenBuffTemplateLibrary(int buffId)
        {
            const string preferredPath = "Assets/Resources/Skill/SkillBuffTemplateLibrary.asset";
            var library = AssetDatabase.LoadAssetAtPath<SkillBuffTemplateLibrary>(preferredPath);
            if (library == null)
            {
                library = SkillBuffTemplateProvider.GetLibrary();
            }

            if (library == null)
            {
                EditorUtility.DisplayDialog("Buff模板库", "未找到 Buff 模板库资源。", "确定");
                return;
            }

            Selection.activeObject = library;
            EditorGUIUtility.PingObject(library);

            if (buffId > 0)
            {
                LogTool.Log($"[SkillNodeEditorWindow] 已定位 Buff 模板库，可检查 BuffId={buffId}");
            }
        }


        private void DrawBuffTemplateSummary(int buffId)
        {
            if (buffId <= 0)
            {
                EditorGUILayout.HelpBox("未填写 Buff模板Id。", MessageType.Warning);
                return;
            }

            var effects = SkillBuffTemplateProvider.GetEffects(buffId);
            var library = SkillBuffTemplateProvider.GetLibrary();
            string templateName = $"BuffId={buffId}";
            if (library != null && library.TryGetTemplate(buffId, out var template) && !string.IsNullOrEmpty(template.Name))
            {
                templateName = $"{template.Name} (BuffId={buffId})";
            }

            if (effects.Count == 0)
            {
                EditorGUILayout.HelpBox($"未找到 Buff 模板：{templateName}", MessageType.Warning);
                return;
            }

            var lines = new List<string> { $"模板: {templateName}" };
            for (int i = 0; i < effects.Count; i++)
            {
                var effect = effects[i];
                var buffTypeName = Enum.GetName(typeof(BuffType), effect.BuffTypeId) ?? effect.BuffTypeId.ToString();
                var buffTypeLabel = GetBuffTypeChineseName(buffTypeName);
                lines.Add($"效果{i + 1}: {buffTypeLabel} | BuffId={effect.BuffId}");
            }

            EditorGUILayout.HelpBox(string.Join("\n", lines), MessageType.Info);
        }

        private void DrawCastParams()
        {
            EditorGUILayout.LabelField("动画配置", EditorStyles.miniLabel);

            var animation = EditorGUILayout.TextField("动画名称", _nodeConfig.GetParamString("animation", "Attack"));
            _nodeConfig.SetParam("animation", animation);

            var castTime = EditorGUILayout.FloatField("引导时间(秒)", _nodeConfig.GetParamFloat("cast_time", 0.3f));
            _nodeConfig.SetParam("cast_time", castTime.ToString());

            var moveSpeed = EditorGUILayout.FloatField("移动速度倍率", _nodeConfig.GetParamFloat("move_speed", 0f));
            _nodeConfig.SetParam("move_speed", moveSpeed.ToString());

            EditorGUILayout.Space(5);

            EditorGUILayout.HelpBox(
                "Cast 节点用于播放施法动画和引导时间。\n" +
                "- animation: 播放的动画名称\n" +
                "- cast_time: 引导时间（秒）\n" +
                "- move_speed: 施法时移动速度倍率（0=禁止移动）",
                MessageType.Info);
        }

        private void DrawSlashParams()
        {
            EditorGUILayout.LabelField("基础配置", EditorStyles.miniLabel);
            DrawAssetSelector("预制体路径", "prefab", "Prefab");

            var scale = EditorGUILayout.FloatField("缩放(%)", _nodeConfig.GetParamFloat("scale", 100f));
            _nodeConfig.SetParam("scale", scale.ToString());

            var duration = EditorGUILayout.FloatField("持续时间(秒)", _nodeConfig.GetParamFloat("duration", 1f));
            _nodeConfig.SetParam("duration", duration.ToString());

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("数量与间隔", EditorStyles.miniLabel);

            var count = EditorGUILayout.IntField("斩击次数", _nodeConfig.GetParamInt("count", 1));
            _nodeConfig.SetParam("count", count.ToString());

            var interval = EditorGUILayout.FloatField("间隔时间(秒)", _nodeConfig.GetParamFloat("interval", 0.1f));
            _nodeConfig.SetParam("interval", interval.ToString());

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("方向控制", EditorStyles.miniLabel);

            var directionMode = EditorGUILayout.TextField("方向模式", _nodeConfig.GetParamString("direction_mode", "Same"));
            _nodeConfig.SetParam("direction_mode", directionMode);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("预设:", GUILayout.Width(50));
                if (GUILayout.Button("Same")) { _nodeConfig.SetParam("direction_mode", "Same"); }
                if (GUILayout.Button("Rotate")) { _nodeConfig.SetParam("direction_mode", "Rotate"); }
                if (GUILayout.Button("Random")) { _nodeConfig.SetParam("direction_mode", "Random"); }
                if (GUILayout.Button("Fork")) { _nodeConfig.SetParam("direction_mode", "Fork"); }
            }

            var angleOffset = EditorGUILayout.FloatField("角度偏移(度)", _nodeConfig.GetParamFloat("angle_offset", 0f));
            _nodeConfig.SetParam("angle_offset", angleOffset.ToString());

            var forkAngle = EditorGUILayout.FloatField("分叉角度(度)", _nodeConfig.GetParamFloat("fork_angle", 0f));
            _nodeConfig.SetParam("fork_angle", forkAngle.ToString());

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("分裂前进 (Fork模式)", EditorStyles.miniLabel);

            var forwardSteps = EditorGUILayout.IntField("前进段数", _nodeConfig.GetParamInt("forward_steps", 1));
            _nodeConfig.SetParam("forward_steps", forwardSteps.ToString());

            var forwardStep = EditorGUILayout.FloatField("段距离(米)", _nodeConfig.GetParamFloat("forward_step", 2f));
            _nodeConfig.SetParam("forward_step", forwardStep.ToString());

            var stepInterval = EditorGUILayout.FloatField("段间隔(秒)", _nodeConfig.GetParamFloat("step_interval", 0.2f));
            _nodeConfig.SetParam("step_interval", stepInterval.ToString());

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("蓄力阶段", EditorStyles.miniLabel);

            var chargeTime = EditorGUILayout.FloatField("蓄力时间(秒)", _nodeConfig.GetParamFloat("charge_time", 0f));
            _nodeConfig.SetParam("charge_time", chargeTime.ToString());

            DrawAssetSelector("蓄力特效路径", "charge_effect", "Prefab");

            var canMove = EditorGUILayout.Toggle("蓄力时可移动", _nodeConfig.GetParamBool("can_move", false));
            _nodeConfig.SetParam("can_move", canMove.ToString());

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("突进位移 (瞬袭斩)", EditorStyles.miniLabel);

            var moveDistance = EditorGUILayout.FloatField("突进距离(厘米)", _nodeConfig.GetParamFloat("move_distance", 0f));
            _nodeConfig.SetParam("move_distance", moveDistance.ToString());

            var moveSpeed = EditorGUILayout.FloatField("突进速度(米/秒)", _nodeConfig.GetParamFloat("move_speed", 10f));
            _nodeConfig.SetParam("move_speed", moveSpeed.ToString());

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("伤害效果", EditorStyles.miniLabel);

            var knockback = EditorGUILayout.FloatField("击退力", _nodeConfig.GetParamFloat("knockback", 0f));
            _nodeConfig.SetParam("knockback", knockback.ToString());

            var knockup = EditorGUILayout.FloatField("击飞力", _nodeConfig.GetParamFloat("knockup", 0f));
            _nodeConfig.SetParam("knockup", knockup.ToString());

            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox(
                "Slash 斩击节点支持：\n" +
                "- 单斩/多重斩: count + interval\n" +
                "- 蓄力斩: charge_time\n" +
                "- 瞬袭斩: move_distance + Slash\n" +
                "- 爆炎斩: direction_mode=Fork + forward_steps",
                MessageType.Info);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("快速预设", EditorStyles.miniLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("单斩"))
                {
                    _nodeConfig.SetParam("count", "1");
                    _nodeConfig.SetParam("interval", "0.1");
                    _nodeConfig.SetParam("direction_mode", "Same");
                    _nodeConfig.SetParam("forward_steps", "1");
                    _nodeConfig.SetParam("charge_time", "0");
                    _nodeConfig.SetParam("move_distance", "0");
                }
                if (GUILayout.Button("多重斩(3连)"))
                {
                    _nodeConfig.SetParam("count", "3");
                    _nodeConfig.SetParam("interval", "0.1");
                    _nodeConfig.SetParam("direction_mode", "Same");
                    _nodeConfig.SetParam("forward_steps", "1");
                    _nodeConfig.SetParam("charge_time", "0");
                    _nodeConfig.SetParam("move_distance", "0");
                }
                if (GUILayout.Button("瞬袭斩"))
                {
                    _nodeConfig.SetParam("count", "1");
                    _nodeConfig.SetParam("interval", "0.1");
                    _nodeConfig.SetParam("direction_mode", "Same");
                    _nodeConfig.SetParam("forward_steps", "1");
                    _nodeConfig.SetParam("charge_time", "0");
                    _nodeConfig.SetParam("move_distance", "200");
                    _nodeConfig.SetParam("move_speed", "10");
                }
                if (GUILayout.Button("爆炎斩"))
                {
                    _nodeConfig.SetParam("count", "1");
                    _nodeConfig.SetParam("interval", "0.1");
                    _nodeConfig.SetParam("direction_mode", "Fork");
                    _nodeConfig.SetParam("fork_angle", "10");
                    _nodeConfig.SetParam("forward_steps", "8");
                    _nodeConfig.SetParam("forward_step", "2");
                    _nodeConfig.SetParam("step_interval", "0.2");
                    _nodeConfig.SetParam("charge_time", "0");
                    _nodeConfig.SetParam("move_distance", "0");
                }
            }
        }

        private void DrawTelegraphParams()
        {
            EditorGUILayout.LabelField("预警配置", EditorStyles.miniLabel);

            var shape = EditorGUILayout.TextField("形状", _nodeConfig.GetParamString("shape", "Circle"));
            _nodeConfig.SetParam("shape", shape);

            var range = EditorGUILayout.FloatField("范围(米)", _nodeConfig.GetParamFloat("range", 3f));
            _nodeConfig.SetParam("range", range.ToString());

            var duration = EditorGUILayout.FloatField("持续时间(秒)", _nodeConfig.GetParamFloat("duration", 0.5f));
            _nodeConfig.SetParam("duration", duration.ToString());

            var color = EditorGUILayout.TextField("颜色", _nodeConfig.GetParamString("color", "#FF4400"));
            _nodeConfig.SetParam("color", color);

            EditorGUILayout.Space(5);

            EditorGUILayout.HelpBox(
                "Telegraph 节点用于显示预警指示器。\n" +
                "- shape: 形状 (Circle/Rectangle/Fan)\n" +
                "- range: 范围大小（米）\n" +
                "- duration: 预警持续时间",
                MessageType.Info);
        }

        private void DrawProjectileParams()
        {
            EditorGUILayout.LabelField("投掷物配置", EditorStyles.miniLabel);

            DrawAssetSelector("预制体路径（可选）:", "prefab", "Prefab");
            DrawAssetSelector("命中特效路径:", "hit_effect", "Prefab");
            DrawAssetSelector("结束特效路径:", "end_effect", "Prefab");
            EditorGUILayout.HelpBox("结束特效在投射物消失时播放（如时间耗尽、距离超出等）", MessageType.None);

            var speed = EditorGUILayout.FloatField("飞行速度(米/秒)", _nodeConfig.GetParamFloat("speed", 10f));
            _nodeConfig.SetParam("speed", speed.ToString());

            var speedAttrName = _nodeConfig.GetParamString("speed_attr", "");
            speedAttrName = EditorGUILayout.TextField("速度属性名", speedAttrName);
            _nodeConfig.SetParam("speed_attr", speedAttrName);
            EditorGUILayout.HelpBox("填写 AttributeType 枚举名称，如 SkillAtkSpeed。留空则使用固定值。", MessageType.None);

            var maxDistance = EditorGUILayout.FloatField("最大距离(米)", _nodeConfig.GetParamFloat("max_distance", 20f));
            _nodeConfig.SetParam("max_distance", maxDistance.ToString());

            var maxTime = EditorGUILayout.FloatField("最大时间(秒)", _nodeConfig.GetParamFloat("max_time", 5f));
            _nodeConfig.SetParam("max_time", maxTime.ToString());

            var maxTimeAttrName = _nodeConfig.GetParamString("max_time_attr", "");
            maxTimeAttrName = EditorGUILayout.TextField("最大时间属性名", maxTimeAttrName);
            _nodeConfig.SetParam("max_time_attr", maxTimeAttrName);
            EditorGUILayout.HelpBox("填写 AttributeType 枚举名称。留空则使用固定值。", MessageType.None);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("生成位置", EditorStyles.miniLabel);

            string[] spawnTypeOptions = { "direction", "caster", "target", "fixed" };
            string[] spawnTypeLabels = { "方向位置(默认)", "施法者位置", "目标位置", "固定位置" };
            var currentSpawnType = _nodeConfig.GetParamString("spawn_type", "direction");
            int spawnTypeIdx = Array.IndexOf(spawnTypeOptions, currentSpawnType);
            if (spawnTypeIdx < 0) spawnTypeIdx = 0;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("生成位置类型", GUILayout.Width(130));
            int newSpawnTypeIdx = EditorGUILayout.Popup(spawnTypeIdx, spawnTypeLabels);
            if (newSpawnTypeIdx != spawnTypeIdx)
            {
                _nodeConfig.SetParam("spawn_type", spawnTypeOptions[newSpawnTypeIdx]);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox("方向位置(默认)：从拖拽释放位置偏移，与旧版 SwiftAssaultSkill 一致。\n施法者位置：从施法者位置偏移。\n目标位置：从目标当前位置偏移。\n固定位置：使用下方固定X/Y坐标。", MessageType.None);

            var spawnOffsetX = EditorGUILayout.FloatField("生成偏移 X", _nodeConfig.GetParamFloat("spawn_offset_x", 0f));
            _nodeConfig.SetParam("spawn_offset_x", spawnOffsetX.ToString("F2"));
            var spawnOffsetY = EditorGUILayout.FloatField("生成偏移 Y", _nodeConfig.GetParamFloat("spawn_offset_y", 0f));
            _nodeConfig.SetParam("spawn_offset_y", spawnOffsetY.ToString("F2"));
            EditorGUILayout.HelpBox("相对「生成位置类型」基准点的世界坐标偏移（米）。例如 spawn_type=target 时在目标点基础上再偏移；瞄准点由「瞄准目标」决定。", MessageType.None);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("瞄准", EditorStyles.miniLabel);
            DrawProjectileAimTargetSelector();

            string[] projectileDirectionOptions = { "Target", "Up", "Down", "Left", "Right" };
            string[] projectileDirectionLabels = { "目标方向", "向上(Y+)", "向下(Y-)", "向左(X-)", "向右(X+)" };
            var currentProjectileDirection = _nodeConfig.GetParamString("projectile_direction", "Target");
            int projectileDirectionIdx = Array.IndexOf(projectileDirectionOptions, currentProjectileDirection);
            if (projectileDirectionIdx < 0) projectileDirectionIdx = 0;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("飞行方向", GUILayout.Width(130));
            int newProjectileDirectionIdx = EditorGUILayout.Popup(projectileDirectionIdx, projectileDirectionLabels);
            if (newProjectileDirectionIdx != projectileDirectionIdx)
            {
                _nodeConfig.SetParam("projectile_direction", projectileDirectionOptions[newProjectileDirectionIdx]);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox("目标方向：从生成点飞向瞄准点。向上/下/左/右：使用世界坐标固定方向飞行。", MessageType.None);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("子弹外观", EditorStyles.miniLabel);

            var bulletLength = EditorGUILayout.FloatField("子弹长度(米)", _nodeConfig.GetParamFloat("bullet_length", 1f));
            _nodeConfig.SetParam("bullet_length", bulletLength.ToString());
            EditorGUILayout.HelpBox("沿子弹飞行方向（Y轴）的缩放倍数。激光类技能可设为 5-10，普通子弹建议 1。会和角色 BulletSizeCorrection 相乘。", MessageType.None);

            var bulletLengthAttrName = _nodeConfig.GetParamString("bullet_length_attr", "");
            bulletLengthAttrName = EditorGUILayout.TextField("子弹长度属性名", bulletLengthAttrName);
            _nodeConfig.SetParam("bullet_length_attr", bulletLengthAttrName);
            EditorGUILayout.HelpBox("填写 AttributeType 枚举名称。留空则使用固定值。", MessageType.None);

            var bulletWidth = EditorGUILayout.FloatField("子弹宽度(米)", _nodeConfig.GetParamFloat("bullet_width", 1f));
            _nodeConfig.SetParam("bullet_width", bulletWidth.ToString());
            EditorGUILayout.HelpBox("垂直子弹飞行方向（X轴）的缩放倍数。普通子弹建议 1。会和角色 BulletSizeCorrection 相乘。", MessageType.None);

            var bulletWidthAttrName = _nodeConfig.GetParamString("bullet_width_attr", "");
            bulletWidthAttrName = EditorGUILayout.TextField("子弹宽度属性名", bulletWidthAttrName);
            _nodeConfig.SetParam("bullet_width_attr", bulletWidthAttrName);
            EditorGUILayout.HelpBox("填写 AttributeType 枚举名称。留空则使用固定值。", MessageType.None);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("朝向", EditorStyles.miniLabel);

            string[] rotationOptions = { "-1", "0", "90", "180", "270" };
            string[] rotationLabels = { "自动（朝向目标）", "向右(0°)", "向上(90°)", "向左(180°)", "向下(270°)" };
            var currentRotation = _nodeConfig.GetParamFloat("fixed_rotation", -1f).ToString("F0");
            int rotationIdx = Array.IndexOf(rotationOptions, currentRotation);
            if (rotationIdx < 0) rotationIdx = 0;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("固定朝向", GUILayout.Width(130));
            int newRotationIdx = EditorGUILayout.Popup(rotationIdx, rotationLabels);
            if (newRotationIdx != rotationIdx)
            {
                _nodeConfig.SetParam("fixed_rotation", rotationOptions[newRotationIdx]);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox("设为自动时子弹朝向目标方向。设为固定角度后子弹保持该方向飞行。", MessageType.None);

            var fixedRotationAttrName = _nodeConfig.GetParamString("fixed_rotation_attr", "");
            fixedRotationAttrName = EditorGUILayout.TextField("固定朝向属性名", fixedRotationAttrName);
            _nodeConfig.SetParam("fixed_rotation_attr", fixedRotationAttrName);
            EditorGUILayout.HelpBox("填写 AttributeType 枚举名称。留空则使用固定朝向。", MessageType.None);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("发射位置", EditorStyles.miniLabel);

            var fixedStartX = _nodeConfig.GetParamFloat("fixed_start_x", -999f);
            var fixedStartY = _nodeConfig.GetParamFloat("fixed_start_y", -999f);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("固定X坐标", GUILayout.Width(130));
            fixedStartX = EditorGUILayout.FloatField(fixedStartX);
            _nodeConfig.SetParam("fixed_start_x", fixedStartX.ToString("F1"));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("固定Y坐标", GUILayout.Width(130));
            fixedStartY = EditorGUILayout.FloatField(fixedStartY);
            _nodeConfig.SetParam("fixed_start_y", fixedStartY.ToString("F1"));
            EditorGUILayout.EndHorizontal();
            
            bool hasFixedStart = fixedStartX > -900f || fixedStartY > -900f;
            EditorGUILayout.HelpBox(hasFixedStart 
                ? $"固定发射点: ({fixedStartX:F1}, {fixedStartY:F1})" 
                : "使用默认：施法者位置", MessageType.None);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("移动方式", EditorStyles.boldLabel);

            DrawMoveTypeSelector();
            DrawProjectileBurnAreaParams();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("命中模块 (HitModule)", EditorStyles.boldLabel);

            var buffHitProbability = EditorGUILayout.Slider("Buff命中概率", _nodeConfig.GetParamFloat("buff_hit_probability", 1f), 0f, 1f);
            _nodeConfig.SetParam("buff_hit_probability", buffHitProbability.ToString());

            var buffHitProbabilityAttr = _nodeConfig.GetParamString("buff_hit_probability_attr", "");
            buffHitProbabilityAttr = EditorGUILayout.TextField("Buff命中概率属性名", buffHitProbabilityAttr);
            _nodeConfig.SetParam("buff_hit_probability_attr", buffHitProbabilityAttr);
            EditorGUILayout.HelpBox("仅对 AttachBuff 命中模块生效。填写 AttributeType 枚举名称，留空则使用固定值。", MessageType.None);

            DrawHitModulesEditor();

            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox(
                "Projectile 节点用于生成投掷物。\n" +
                "- prefab: 投掷物预制体路径\n" +
                "- hit_effect: 命中特效路径（可选）\n" +
                "- speed: 飞行速度\n" +
                "- speed_attr: 从技能卡片属性读取速度\n" +
                "- max_time: 最大存在时间\n" +
                "- max_time_attr: 从技能卡片属性读取最大时间\n" +
                "- bullet_length: 子弹长度\n" +
                "- bullet_length_attr: 从技能卡片属性读取子弹长度\n" +
                "- bullet_width: 子弹宽度\n" +
                "- bullet_width_attr: 从技能卡片属性读取子弹宽度\n" +
                "- fixed_rotation: 固定朝向\n" +
                "- fixed_rotation_attr: 从技能卡片属性读取固定朝向\n" +
                "- projectile_direction: 飞行方向 (Target/Up/Down/Left/Right)\n" +
                "- buff_hit_probability: AttachBuff 的Buff命中概率\n" +
                "- buff_hit_probability_attr: 从技能卡片属性读取 Buff命中概率\n" +
                "- move_type: 移动方式 (Straight/MoveToPosition/Bounce/Homing/Parabola)\n" +
                "- spawn_burn_area_on_move: 移动留火区；各 burn_area_* 支持 *_attr 从技能属性读取\n" +
                "- hit_modules: 命中模块列表 (Penetrate/Explode/Chain)\n" +
                "  点击 + 添加模块，点击 - 删除；各模块参数在模块行下方直接显示",
                MessageType.Info);
        }

        // ============================================================
        // 移动方式选择器
        // ============================================================

        private static readonly string[] MoveTypeOptions = new string[]
        {
            "Straight",    // 直线飞行
            "MoveToPosition", // 飞到指定位置
            "Bounce",      // 墙壁反弹
            "Homing",      // 追踪目标
            "Parabola",    // 抛物线
            "Curve",       // 曲线
            "Spiral",      // 螺旋
            "LaserSweep",  // 激光扫描
        };

        private void DrawMoveTypeSelector()
        {
            string current = _nodeConfig.GetParamString("move_type", "Straight");
            int selectedIndex = Array.IndexOf(MoveTypeOptions, current);
            if (selectedIndex < 0) selectedIndex = 0;

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("移动方式", GUILayout.Width(130));

                int newIndex = EditorGUILayout.Popup(selectedIndex, MoveTypeOptions);
                if (newIndex != selectedIndex)
                {
                    _nodeConfig.SetParam("move_type", MoveTypeOptions[newIndex]);
                }

                // 重置按钮（回到默认）
                if (GUILayout.Button("重置", GUILayout.Width(50)))
                {
                    _nodeConfig.SetParam("move_type", "Straight");
                }
            }

            // 选中后展开该移动类型的专属参数
            string selectedMove = _nodeConfig.GetParamString("move_type", "Straight");
            EditorGUI.indentLevel++;

            switch (selectedMove)
            {
                case "Straight":
                    DrawStraightMoveParams();
                    break;
                case "MoveToPosition":
                    DrawMoveToPositionParams();
                    break;
                case "Bounce":
                    DrawBounceMoveParams();
                    break;
                case "Homing":
                    DrawHomingMoveParams();
                    break;
                case "Parabola":
                    DrawParabolaMoveParams();
                    break;
                case "Curve":
                    DrawCurveMoveParams();
                    break;
                case "Spiral":
                    DrawSpiralMoveParams();
                    break;
                case "LaserSweep":
                    DrawLaserSweepMoveParams();
                    break;
            }

            EditorGUI.indentLevel--;
        }

        private void DrawStraightMoveParams()
        {
            EditorGUILayout.HelpBox("直线飞行：沿初始方向匀速前进，无需额外追踪或曲线控制。", MessageType.Info);

            var angleOffset = EditorGUILayout.FloatField("初始角度偏移(度)", _nodeConfig.GetParamFloat("move_angle_offset", 0f));
            _nodeConfig.SetParam("move_angle_offset", angleOffset.ToString());
            EditorGUILayout.HelpBox("发射时的初始方向偏转角度（度）。正值为顺时针，负值为逆时针。", MessageType.None);
        }

        private void DrawMoveToPositionParams()
        {
            EditorGUILayout.HelpBox("移动到指定位置：投射物飞向 TargetPosition，到点即结束，飞行途中不会进行命中检测。", MessageType.Info);

            var arriveDistance = EditorGUILayout.FloatField("到点判定距离(米)", _nodeConfig.GetParamFloat("arrive_distance", 0.05f));
            _nodeConfig.SetParam("arrive_distance", arriveDistance.ToString());
            EditorGUILayout.HelpBox("距离目标点小于该值时视为到达并结束。", MessageType.None);
        }

        private void DrawBounceMoveParams()
        {
            EditorGUILayout.HelpBox("墙壁反弹：子弹碰到墙壁后按物理反射角反弹，可多次弹跳。", MessageType.Info);

            var bounceCount = EditorGUILayout.IntField("最大反弹次数", _nodeConfig.GetParamInt("bounce_count", 3));
            _nodeConfig.SetParam("bounce_count", bounceCount.ToString());
            EditorGUILayout.HelpBox("子弹可反弹的最大次数。达到上限后子弹销毁。", MessageType.None);

            var bounceResetHit = EditorGUILayout.Toggle("反弹后重置命中列表", _nodeConfig.GetParamBool("bounce_reset_hit", true));
            _nodeConfig.SetParam("bounce_reset_hit", bounceResetHit.ToString());
            EditorGUILayout.HelpBox("开启后：每次反弹清空已命中目标列表，允许反弹后再次伤害之前击中的敌人。", MessageType.None);
        }

        private void DrawHomingMoveParams()
        {
            EditorGUILayout.HelpBox("追踪目标：子弹自动调整方向飞向目标，适合飞镖、导弹类技能。", MessageType.Info);

            var searchSpeed = EditorGUILayout.FloatField("游荡速度(m/s)", _nodeConfig.GetParamFloat("search_speed", 3f));
            _nodeConfig.SetParam("search_speed", searchSpeed.ToString());
            EditorGUILayout.HelpBox("游荡阶段的移动速度。", MessageType.None);

            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("游荡阶段（随机游走，同 Tornado）", EditorStyles.miniLabel);

            var wanderRange = EditorGUILayout.FloatField("游荡半径(米)", _nodeConfig.GetParamFloat("wander_range", 5f));
            _nodeConfig.SetParam("wander_range", wanderRange.ToString());
            EditorGUILayout.HelpBox("以当前位置为中心，随机选择目标点的最大半径。越大游荡范围越广。", MessageType.None);

            var wanderMove = EditorGUILayout.FloatField("游荡移动时间(s)", _nodeConfig.GetParamFloat("wander_move_duration", 2f));
            _nodeConfig.SetParam("wander_move_duration", wanderMove.ToString());
            EditorGUILayout.HelpBox("游荡阶段朝随机目标点移动的持续时间。", MessageType.None);

            var wanderWait = EditorGUILayout.FloatField("游荡等待时间(s)", _nodeConfig.GetParamFloat("wander_wait_duration", 1f));
            _nodeConfig.SetParam("wander_wait_duration", wanderWait.ToString());
            EditorGUILayout.HelpBox("游荡阶段移动到目标点后等待的时间，然后重新随机方向。", MessageType.None);

            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("追踪阶段（朝向目标）", EditorStyles.miniLabel);

            var homingMove = EditorGUILayout.FloatField("追踪移动时间(s)", _nodeConfig.GetParamFloat("homing_move_duration", 2f));
            _nodeConfig.SetParam("homing_move_duration", homingMove.ToString());
            EditorGUILayout.HelpBox("追踪阶段朝目标移动的持续时间。", MessageType.None);

            var homingWait = EditorGUILayout.FloatField("追踪等待时间(s)", _nodeConfig.GetParamFloat("homing_wait_duration", 1f));
            _nodeConfig.SetParam("homing_wait_duration", homingWait.ToString());
            EditorGUILayout.HelpBox("追踪阶段移动后等待的时间，然后重新追踪。", MessageType.None);

            var searchDuration = EditorGUILayout.FloatField("最大游荡时间(s)", _nodeConfig.GetParamFloat("search_duration", 5f));
            _nodeConfig.SetParam("search_duration", searchDuration.ToString());
            EditorGUILayout.HelpBox("无目标时多久换一次随机方向。", MessageType.None);

            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("追踪控制", EditorStyles.miniLabel);

            var homingDelay = EditorGUILayout.FloatField("追踪延迟时间(s)", _nodeConfig.GetParamFloat("homing_delay", 0f));
            _nodeConfig.SetParam("homing_delay", homingDelay.ToString());
            EditorGUILayout.HelpBox("索敌成功后等待多少秒再开始追踪。", MessageType.None);

            var homingStrength = EditorGUILayout.Slider("追踪强度(0-1)", _nodeConfig.GetParamFloat("homing_strength", 0.5f), 0f, 1f);
            _nodeConfig.SetParam("homing_strength", homingStrength.ToString());
            EditorGUILayout.HelpBox("每帧转向目标的程度。", MessageType.None);

            var homingRotSpeed = EditorGUILayout.FloatField("追踪旋转速度(度/秒)", _nodeConfig.GetParamFloat("homing_rot_speed", 180f));
            _nodeConfig.SetParam("homing_rot_speed", homingRotSpeed.ToString());
            EditorGUILayout.HelpBox("子弹每秒最多能转动多少度。", MessageType.None);
        }

        private void DrawParabolaMoveParams()
        {
            EditorGUILayout.HelpBox("抛物线：子弹沿抛物线弧度飞行，适合弓箭、投掷类技能。", MessageType.Info);

            var peakHeight = EditorGUILayout.FloatField("最高点高度(米)", _nodeConfig.GetParamFloat("parabola_peak_height", 3f));
            _nodeConfig.SetParam("parabola_peak_height", peakHeight.ToString());
            EditorGUILayout.HelpBox("抛物线最高点的高度（米）。值越大弧度越高。", MessageType.None);

            var gravity = EditorGUILayout.FloatField("重力加速度", _nodeConfig.GetParamFloat("parabola_gravity", 9.8f));
            _nodeConfig.SetParam("parabola_gravity", gravity.ToString());
            EditorGUILayout.HelpBox("模拟的重力加速度。值越大下坠越快。默认 9.8 近似真实重力。", MessageType.None);
        }

        private void DrawCurveMoveParams()
        {
            EditorGUILayout.HelpBox("曲线飞行：子弹沿贝塞尔曲线轨迹飞行，适合回旋镖、弧线弹道。", MessageType.Info);

            var curveStrength = EditorGUILayout.FloatField("曲线强度", _nodeConfig.GetParamFloat("curve_strength", 1f));
            _nodeConfig.SetParam("curve_strength", curveStrength.ToString());
            EditorGUILayout.HelpBox("曲线的弯曲程度。值越大弯曲越明显。", MessageType.None);

            var curveDir = EditorGUILayout.TextField("曲线方向(L/R)", _nodeConfig.GetParamString("curve_dir", "R"));
            _nodeConfig.SetParam("curve_dir", curveDir);
            EditorGUILayout.HelpBox("L=向左弯曲，R=向右弯曲。", MessageType.None);
        }

        private void DrawSpiralMoveParams()
        {
            EditorGUILayout.HelpBox("螺旋飞行：子弹螺旋前进，适合钻头、旋转镖类技能。", MessageType.Info);

            var spiralRadius = EditorGUILayout.FloatField("螺旋半径(米)", _nodeConfig.GetParamFloat("spiral_radius", 0.5f));
            _nodeConfig.SetParam("spiral_radius", spiralRadius.ToString());
            EditorGUILayout.HelpBox("螺旋的半径（米）。值越大螺旋圈越大。", MessageType.None);

            var spiralSpeed = EditorGUILayout.FloatField("螺旋角速度(度/秒)", _nodeConfig.GetParamFloat("spiral_speed", 360f));
            _nodeConfig.SetParam("spiral_speed", spiralSpeed.ToString());
            EditorGUILayout.HelpBox("每秒转过的角度。360=每秒转一圈，720=每秒转两圈。", MessageType.None);
        }

        private void DrawLaserSweepMoveParams()
        {
            EditorGUILayout.HelpBox("激光扫描：子弹直线飞行并左右来回扫描扇形区域，适合激光、刀刃类技能。", MessageType.Info);

            var sweepAngle = EditorGUILayout.FloatField("扫描角度(度)", _nodeConfig.GetParamFloat("sweep_angle", 50f));
            _nodeConfig.SetParam("sweep_angle", sweepAngle.ToString());
            EditorGUILayout.HelpBox("左右扫描的总角度范围（度）。子弹会在此角度范围内来回摆动。", MessageType.None);

            var sweepDuration = EditorGUILayout.FloatField("扫描周期(秒)", _nodeConfig.GetParamFloat("sweep_duration", 5f));
            _nodeConfig.SetParam("sweep_duration", sweepDuration.ToString());
            EditorGUILayout.HelpBox("一个完整扫描周期的时间（秒）。值越小摆动越快。", MessageType.None);
        }

        // ============================================================
        // HitModule 编辑器（支持多模块、可添加/删除）
        // ============================================================

        private static readonly string[] HitModuleOptions = new string[]
        {
            "Penetrate",   // 穿透模块
            "Explode",     // 爆炸模块
            "Chain",       // 链式弹跳
            "Pierce",      // 穿刺（不减速）
            "DelayedHit",  // 延迟命中
            "AOE",         // 范围命中
            "Rect",        // 矩形检测
            "Split",       // 分裂
            "Catapult",    // 击杀弹射
            "AttachBuff",  // 附加Buff
        };

        /// <summary>
        /// 单个 HitModule 条目数据结构（用于编辑器临时解析）
        /// </summary>
        [Serializable]
        private struct HitModuleEntry
        {
            public string ModuleType;
            public int IntParam1;  // 如穿透次数、链式目标数、分裂数量
            public float FloatParam1; // 如爆炸半径、分裂伤害继承
            public float FloatParam2; // 如延迟时间、再次分裂伤害
            public bool BoolParam1;  // 如AOE单次触发
            public int IntParam2;    // 如分裂次数
            public float FloatParam3; // 如Buff触发间隔
            public bool BoolParam2;  // AttachBuff：false=释放者，true=受击者

            // 属性名：从技能卡片属性读取（留空则使用上方的固定值）
            public string IntParam1Attr;  // IntParam1 对应的属性名
            public string FloatParam1Attr; // FloatParam1 对应的属性名
            public string FloatParam2Attr; // FloatParam2 对应的属性名
            public string IntParam2Attr;   // IntParam2 对应的属性名
            public string FloatParam3Attr; // FloatParam3 对应的属性名

            // 兼容旧版 AttachBuff：0=百分比, 1=固定值；以及百分比基础属性名
            public int IntParam3;
            public string IntParam3Attr;

            // 新版 AttachBuff：伤害类型、触发概率、百分比效果值、特效
            public int IntParam4;
            public string IntParam4Attr;
            public float FloatParam4;
            public string FloatParam4Attr;
            public float FloatParam5;
            public string FloatParam5Attr;
            public int IntParam5;
            public string StringParam1;
            public string StringParam5;
            public string StringParam2;
            public string StringParam3;
            public string StringParam4;

            public override string ToString() => ModuleType;
        }

        /// <summary>
        /// 从参数字符串解析 HitModule 列表
        /// 格式: moduleType|intParam:floatParam1:floatParam2:boolParam:intParam2:floatParam3:attr1:attr2:attr3:attr4:attr5
        /// </summary>
        private List<HitModuleEntry> ParseHitModules(string raw)
        {
            var result = new List<HitModuleEntry>();
            if (string.Equals(raw, "None", StringComparison.OrdinalIgnoreCase)) return result;
            if (string.IsNullOrEmpty(raw)) return result;

            string[] entries = raw.Split(';', StringSplitOptions.RemoveEmptyEntries);
            foreach (var entry in entries)
            {
                var parts = entry.Split('|');
                var e = new HitModuleEntry { ModuleType = parts[0] };

                if (parts.Length > 1)
                {
                    // 必须保留空段：属性名为空时序列化为连续 ::，RemoveEmptyEntries 会吃掉空位导致后续字段错位（如把数字 0 当成属性名）
                    var p = parts[1].Split(':');
                    if (p.Length > 0 && int.TryParse(p[0], out int iv)) e.IntParam1 = iv;
                    if (p.Length > 1 && float.TryParse(p[1], out float fv1)) e.FloatParam1 = fv1;
                    if (p.Length > 2 && float.TryParse(p[2], out float fv2)) e.FloatParam2 = fv2;
                    if (p.Length > 3) e.BoolParam1 = p[3].Equals("1") || p[3].Equals("true", StringComparison.OrdinalIgnoreCase);
                    if (p.Length > 4 && int.TryParse(p[4], out int iv2)) e.IntParam2 = iv2;
                    if (p.Length > 5 && float.TryParse(p[5], out float fv3)) e.FloatParam3 = fv3;
                    if (p.Length > 6) e.IntParam1Attr = NormalizeHitModuleAttrToken(p[6]);
                    if (p.Length > 7) e.FloatParam1Attr = NormalizeHitModuleAttrToken(p[7]);
                    if (p.Length > 8) e.FloatParam2Attr = NormalizeHitModuleAttrToken(p[8]);
                    if (p.Length > 9) e.IntParam2Attr = NormalizeHitModuleAttrToken(p[9]);
                    if (p.Length > 10) e.FloatParam3Attr = NormalizeHitModuleAttrToken(p[10]);
                    if (p.Length > 11 && int.TryParse(p[11], out int iv3)) e.IntParam3 = iv3;
                    if (p.Length > 12) e.IntParam3Attr = NormalizeHitModuleAttrToken(p[12]);
                    if (p.Length > 13 && int.TryParse(p[13], out int iv4)) e.IntParam4 = iv4;
                    if (p.Length > 14) e.IntParam4Attr = NormalizeHitModuleAttrToken(p[14]);
                    if (p.Length > 15 && float.TryParse(p[15], out float fv4)) e.FloatParam4 = fv4;
                    if (p.Length > 16) e.FloatParam4Attr = NormalizeHitModuleAttrToken(p[16]);
                    if (p.Length > 17 && float.TryParse(p[17], out float fv5)) e.FloatParam5 = fv5;
                    if (p.Length > 18) e.FloatParam5Attr = NormalizeHitModuleAttrToken(p[18]);
                    if (p.Length > 19) e.BoolParam2 = p[19].Equals("1") || p[19].Equals("true", StringComparison.OrdinalIgnoreCase);
                    if (p.Length > 20) e.StringParam1 = p[20];
                    if (p.Length > 21) e.StringParam2 = p[21];
                    if (p.Length > 22) e.StringParam3 = p[22];
                    if (p.Length > 23) e.StringParam4 = p[23];
                    if (p.Length > 24 && int.TryParse(p[24], out int iv5)) e.IntParam5 = iv5;
                    if (p.Length > 25) e.StringParam5 = p[25];
                }

                result.Add(e);
            }
            return result;
        }

        /// <summary>
        /// 将 HitModule 列表序列化为参数字符串
        /// </summary>
        private string SerializeHitModules(List<HitModuleEntry> entries)
        {
            var parts = new List<string>();
            foreach (var e in entries)
            {
                string param = $"{e.ModuleType}|{e.IntParam1}:{e.FloatParam1}:{e.FloatParam2}:{(e.BoolParam1 ? "1" : "0")}:{e.IntParam2}:{e.FloatParam3}:{e.IntParam1Attr}:{e.FloatParam1Attr}:{e.FloatParam2Attr}:{e.IntParam2Attr}:{e.FloatParam3Attr}:{e.IntParam3}:{e.IntParam3Attr}:{e.IntParam4}:{e.IntParam4Attr}:{e.FloatParam4}:{e.FloatParam4Attr}:{e.FloatParam5}:{e.FloatParam5Attr}:{(e.BoolParam2 ? "1" : "0")}:{e.StringParam1}:{e.StringParam2}:{e.StringParam3}:{e.StringParam4}:{e.IntParam5}:{e.StringParam5}";
                parts.Add(param);
            }
            return string.Join(";", parts);
        }

        /// <summary>
        /// 命中模块序列化里属性名占位：空或历史错位写入的 "0" 视为未配置
        /// </summary>
        private static string NormalizeHitModuleAttrToken(string token)
        {
            if (string.IsNullOrEmpty(token)) return "";
            if (token == "0") return "";
            return token;
        }

        private void DrawHitModulesEditor()
        {
            string rawModules = _nodeConfig.GetParamString("hit_modules", "");
            var modules = ParseHitModules(rawModules);

            // 无模块时显示提示
            if (modules.Count == 0)
            {
                EditorGUILayout.HelpBox("暂未添加任何命中模块。点击下方按钮添加。可添加多个模块组合使用。", MessageType.Info);
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("命中模块说明:", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox(
                "Penetrate（穿透）：命中后不销毁，穿透敌人继续飞行，可击中多个目标。\n" +
                "Explode（爆炸）：命中时触发范围爆炸伤害。\n" +
                "Chain（链式）：命中后将伤害链式传递到附近目标。\n" +
                "Pierce（穿刺）：命中不减速，穿透护甲类目标。\n" +
                "DelayedHit（延迟命中）：命中延迟触发，用于定时炸弹类技能。\n" +
                "AOE（范围）：直接对范围内所有目标造成伤害，支持持续/间隔检测。\n" +
                "Rect（矩形）：使用矩形（攻击盒）检测，基于子弹宽长，可穿透。\n"+
                "splite（分裂）：使用矩形（攻击盒）检测，基于子弹宽长，不可穿透。\n" +
                "Catapult（弹射）：命中击杀敌人后朝下一个敌人弹射继续攻击。",
                MessageType.None);

            EditorGUILayout.Space(5);

            // 收集待删除的索引，延迟删除（避免循环中修改导致 GUI 布局与重绘不一致）
            var indicesToRemove = new List<int>();

            // 绘制已有模块列表
            for (int i = 0; i < modules.Count; i++)
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    // 模块类型标签
                    string moduleType = modules[i].ModuleType;
                    Color originalColor = GUI.backgroundColor;
                    GUI.backgroundColor = GetModuleColor(moduleType);
                    EditorGUILayout.LabelField($"模块 {i + 1}", moduleType, GUILayout.Width(120));
                    GUI.backgroundColor = originalColor;

                    // 模块类型下拉切换
                    int currentIdx = Array.IndexOf(HitModuleOptions, moduleType);
                    if (currentIdx < 0) currentIdx = 0;

                    EditorGUILayout.LabelField("类型", GUILayout.Width(35));
                    int newIdx = EditorGUILayout.Popup(currentIdx, HitModuleOptions, GUILayout.Width(100));
                    if (newIdx != currentIdx)
                    {
                        modules[i] = CreateDefaultEntry(HitModuleOptions[newIdx]);
                    }

                    GUILayout.FlexibleSpace();

                    // 标记删除（不在循环中直接删除，避免 GUI 重绘阶段控制数量错乱）
                    bool shouldRemove = false;
                    EditorGUI.BeginChangeCheck();
                    shouldRemove = GUILayout.Button("-", GUILayout.Width(25));
                    if (EditorGUI.EndChangeCheck() && shouldRemove)
                    {
                        indicesToRemove.Add(i);
                    }
                }

                // 参数始终内联显示（无需折叠箭头）
                modules[i] = DrawSingleHitModuleParams(i, modules[i]);
            }

            // 统一删除所有待移除的模块（从大到小删除，保持索引有效）
            indicesToRemove.Sort((a, b) => b.CompareTo(a));
            foreach (var idx in indicesToRemove)
            {
                modules.RemoveAt(idx);
            }

            EditorGUILayout.Space(5);

            // 添加模块按钮（带下拉选择）
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("添加命中模块:", GUILayout.Width(120));

                // 循环下拉添加
                int addIdx = EditorGUILayout.Popup(0, HitModuleOptions, GUILayout.Width(120));
                if (GUILayout.Button("+ 添加", GUILayout.Width(60)))
                {
                    modules.Add(CreateDefaultEntry(HitModuleOptions[addIdx]));
                }
            }

            EditorGUILayout.Space(3);

            // 快捷添加按钮（固定两行布局）
            EditorGUILayout.LabelField("快捷添加:", EditorStyles.boldLabel);

            // 第一行：前4个按钮
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("", GUILayout.Width(120));
                for (int si = 0; si < 4 && si < HitModuleOptions.Length; si++)
                {
                    string opt = HitModuleOptions[si];
                    Color originalColor = GUI.backgroundColor;
                    GUI.backgroundColor = GetModuleColor(opt);
                    if (GUILayout.Button(opt, GUILayout.Width(70)))
                    {
                        bool exists = modules.Exists(m => m.ModuleType == opt);
                        if (!exists)
                            modules.Add(CreateDefaultEntry(opt));
                    }
                    GUI.backgroundColor = originalColor;
                }
            }

            // 第二行：中间4个按钮（如果有）
            int remaining = HitModuleOptions.Length - 4;
            if (remaining > 0)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("", GUILayout.Width(120));
                    for (int si = 4; si < Mathf.Min(8, HitModuleOptions.Length); si++)
                    {
                        string opt = HitModuleOptions[si];
                        Color originalColor = GUI.backgroundColor;
                        GUI.backgroundColor = GetModuleColor(opt);
                        if (GUILayout.Button(opt, GUILayout.Width(70)))
                        {
                            bool exists = modules.Exists(m => m.ModuleType == opt);
                            if (!exists)
                                modules.Add(CreateDefaultEntry(opt));
                        }
                        GUI.backgroundColor = originalColor;
                    }
                }
            }

            // 第三行：剩余按钮
            int remaining2 = HitModuleOptions.Length - 8;
            if (remaining2 > 0)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("", GUILayout.Width(120));
                    for (int si = 8; si < HitModuleOptions.Length; si++)
                    {
                        string opt = HitModuleOptions[si];
                        Color originalColor = GUI.backgroundColor;
                        GUI.backgroundColor = GetModuleColor(opt);
                        if (GUILayout.Button(opt, GUILayout.Width(70)))
                        {
                            bool exists = modules.Exists(m => m.ModuleType == opt);
                            if (!exists)
                                modules.Add(CreateDefaultEntry(opt));
                        }
                        GUI.backgroundColor = originalColor;
                    }
                }
            }

            // 重新保存
            var serializedModules = SerializeHitModules(modules);
            if (modules.Count == 0 && !string.IsNullOrEmpty(rawModules))
            {
                serializedModules = "None";
            }
            _nodeConfig.SetParam("hit_modules", serializedModules);
        }

        private HitModuleEntry CreateDefaultEntry(string moduleType)
        {
            return moduleType switch
            {
                "Penetrate" => new HitModuleEntry { ModuleType = moduleType, IntParam1 = 3, FloatParam1 = 0f, FloatParam2 = 0f, BoolParam1 = false, BoolParam2 = false, IntParam1Attr = "", FloatParam1Attr = "", FloatParam2Attr = "", IntParam2Attr = "", FloatParam3Attr = "" },
                "Explode" => new HitModuleEntry { ModuleType = moduleType, IntParam1 = 0, FloatParam1 = 3f, FloatParam2 = 0f, BoolParam1 = false, BoolParam2 = false, IntParam1Attr = "", FloatParam1Attr = "", FloatParam2Attr = "", IntParam2Attr = "", FloatParam3Attr = "" },
                "Chain" => new HitModuleEntry { ModuleType = moduleType, IntParam1 = 2, FloatParam1 = 5f, FloatParam2 = 0f, BoolParam1 = false, BoolParam2 = false, IntParam1Attr = "", FloatParam1Attr = "", FloatParam2Attr = "", IntParam2Attr = "", FloatParam3Attr = "" },
                "Pierce" => new HitModuleEntry { ModuleType = moduleType, IntParam1 = 1, FloatParam1 = 0f, FloatParam2 = 0f, BoolParam1 = false, BoolParam2 = false, IntParam1Attr = "", FloatParam1Attr = "", FloatParam2Attr = "", IntParam2Attr = "", FloatParam3Attr = "" },
                "DelayedHit" => new HitModuleEntry { ModuleType = moduleType, IntParam1 = 0, FloatParam1 = 0f, FloatParam2 = 0.5f, BoolParam1 = false, BoolParam2 = false, IntParam1Attr = "", FloatParam1Attr = "", FloatParam2Attr = "", IntParam2Attr = "", FloatParam3Attr = "" },
                "AOE" => new HitModuleEntry { ModuleType = moduleType, IntParam1 = 0, FloatParam1 = 3f, FloatParam2 = 0f, BoolParam1 = true, BoolParam2 = false, IntParam1Attr = "", FloatParam1Attr = "", FloatParam2Attr = "", IntParam2Attr = "", FloatParam3Attr = "" },
                "Rect" => new HitModuleEntry { ModuleType = moduleType, IntParam1 = 3, FloatParam1 = 1f, FloatParam2 = 1f, FloatParam3 = 0f, BoolParam1 = false, BoolParam2 = false, IntParam1Attr = "", FloatParam1Attr = "", FloatParam2Attr = "", IntParam2Attr = "", FloatParam3Attr = "" },
                "Split" => new HitModuleEntry { ModuleType = moduleType, IntParam1 = 2, FloatParam1 = 1f, FloatParam2 = 0.5f, BoolParam1 = false, IntParam2 = 0, BoolParam2 = false, IntParam1Attr = "", FloatParam1Attr = "", FloatParam2Attr = "", IntParam2Attr = "", FloatParam3Attr = "" },
                "Catapult" => new HitModuleEntry { ModuleType = moduleType, IntParam1 = 3, FloatParam1 = 0f, FloatParam2 = 0f, BoolParam1 = false, BoolParam2 = false, IntParam1Attr = "", FloatParam1Attr = "", FloatParam2Attr = "", IntParam2Attr = "", FloatParam3Attr = "" },
                "AttachBuff" => new HitModuleEntry { ModuleType = moduleType, IntParam1 = 0, FloatParam1 = 0f, FloatParam2 = 0f, BoolParam1 = false, IntParam2 = 1, FloatParam3 = 0f, BoolParam2 = false, IntParam1Attr = "", FloatParam1Attr = "", FloatParam2Attr = "", IntParam2Attr = "", FloatParam3Attr = "", IntParam3 = 1, IntParam3Attr = "", IntParam4 = 0, IntParam4Attr = "", FloatParam4 = 1f, FloatParam4Attr = "", FloatParam5 = 0f, FloatParam5Attr = "", IntParam5 = 0, StringParam1 = "", StringParam2 = "", StringParam3 = "", StringParam4 = "Enemy", StringParam5 = "" },
                _ => new HitModuleEntry { ModuleType = moduleType, IntParam1 = 0, FloatParam1 = 0f, FloatParam2 = 0f, BoolParam1 = false, BoolParam2 = false, IntParam1Attr = "", FloatParam1Attr = "", FloatParam2Attr = "", IntParam2Attr = "", FloatParam3Attr = "" }
            };
        }

        private HitModuleEntry WithEntry(HitModuleEntry e, int i1, float f1, float f2, bool b1, bool b2)
        {
            return new HitModuleEntry
            {
                ModuleType = e.ModuleType,
                IntParam1 = i1,
                FloatParam1 = f1,
                FloatParam2 = f2,
                BoolParam1 = b1,
                BoolParam2 = b2
            };
        }

        private Color GetModuleColor(string moduleType)
        {
            return moduleType switch
            {
                "Penetrate" => new Color(0.2f, 0.6f, 1f),
                "Explode" => new Color(1f, 0.4f, 0.2f),
                "Chain" => new Color(0.4f, 1f, 0.4f),
                "Pierce" => new Color(0.6f, 0.3f, 1f),
                "DelayedHit" => new Color(1f, 0.8f, 0.2f),
                "AOE" => new Color(1f, 0.2f, 0.6f),
                "Rect" => new Color(0.2f, 1f, 0.8f),
                "Split" => new Color(1f, 0.5f, 0f),
                "Catapult" => new Color(0.9f, 0.7f, 0.2f),
                _ => Color.gray
            };
        }

        /// <summary>
        /// 为单个 HitModule 绘制内联参数编辑区域，返回更新后的 entry
        /// </summary>
        private HitModuleEntry DrawSingleHitModuleParams(int index, HitModuleEntry entry)
        {
            EditorGUI.indentLevel++;

            switch (entry.ModuleType)
            {
                case "Penetrate":
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("最大穿透次数", GUILayout.Width(140));
                    entry.IntParam1 = EditorGUILayout.IntField(entry.IntParam1);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.HelpBox("子弹穿透敌人后不销毁，可连续命中的最大敌人数量。设为 1 时穿透后立即销毁。", MessageType.None);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("穿透次数属性名", GUILayout.Width(140));
                    entry.IntParam1Attr = EditorGUILayout.TextField(entry.IntParam1Attr);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.HelpBox("填写 AttributeType 枚举名称。留空则使用固定值。", MessageType.None);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("同目标重复间隔(秒)", GUILayout.Width(140));
                    entry.FloatParam1 = EditorGUILayout.FloatField(entry.FloatParam1);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.HelpBox("对同一目标造成多次伤害的间隔（秒）。0=每个目标只能受伤一次；>0=每间隔此时间允许再次伤害该目标，可实现持续毒伤效果。", MessageType.None);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("重复间隔属性名", GUILayout.Width(140));
                    entry.FloatParam1Attr = EditorGUILayout.TextField(entry.FloatParam1Attr);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.HelpBox("填写 AttributeType 枚举名称。留空则使用固定值。", MessageType.None);
                    break;

                case "Explode":
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("爆炸伤害半径(米)", GUILayout.Width(140));
                    entry.FloatParam1 = EditorGUILayout.FloatField(entry.FloatParam1);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.HelpBox("命中触发时，在命中点周围此半径内的所有敌人都会受到爆炸伤害。", MessageType.None);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("爆炸半径属性名", GUILayout.Width(140));
                    entry.FloatParam1Attr = EditorGUILayout.TextField(entry.FloatParam1Attr);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.HelpBox("填写 AttributeType 枚举名称。留空则使用固定值。", MessageType.None);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("爆炸延迟(秒)", GUILayout.Width(140));
                    entry.FloatParam2 = EditorGUILayout.FloatField(entry.FloatParam2);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.HelpBox("命中后等待多少秒再触发爆炸。设为 0 则立即爆炸。延迟期间子弹继续飞行。", MessageType.None);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("爆炸延迟属性名", GUILayout.Width(140));
                    entry.FloatParam2Attr = EditorGUILayout.TextField(entry.FloatParam2Attr);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.HelpBox("填写 AttributeType 枚举名称。留空则使用固定值。", MessageType.None);
                    break;

                case "Chain":
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("最大链式目标数", GUILayout.Width(140));
                    entry.IntParam1 = EditorGUILayout.IntField(entry.IntParam1);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.HelpBox("命中后自动跳跃到附近目标的次数。1=命中后链到1个额外目标（总计2个），2=链到2个额外目标，以此类推。", MessageType.None);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("链式目标数属性名", GUILayout.Width(140));
                    entry.IntParam1Attr = EditorGUILayout.TextField(entry.IntParam1Attr);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.HelpBox("填写 AttributeType 枚举名称。留空则使用固定值。", MessageType.None);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("链式跳跃距离(米)", GUILayout.Width(140));
                    entry.FloatParam1 = EditorGUILayout.FloatField(entry.FloatParam1);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.HelpBox("每次链式跳跃的最大距离（米）。超过此距离的目标不会被链到。", MessageType.None);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("跳跃距离属性名", GUILayout.Width(140));
                    entry.FloatParam1Attr = EditorGUILayout.TextField(entry.FloatParam1Attr);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.HelpBox("填写 AttributeType 枚举名称。留空则使用固定值。", MessageType.None);
                    break;

                case "Pierce":
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("穿刺层数(不减速)", GUILayout.Width(140));
                    entry.IntParam1 = EditorGUILayout.IntField(entry.IntParam1);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.HelpBox("子弹穿过护甲类目标（如盾牌）的层数。每次穿透护甲不降低子弹速度。", MessageType.None);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("穿刺层数属性名", GUILayout.Width(140));
                    entry.IntParam1Attr = EditorGUILayout.TextField(entry.IntParam1Attr);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.HelpBox("填写 AttributeType 枚举名称。留空则使用固定值。", MessageType.None);
                    break;

                case "DelayedHit":
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("命中延迟时间(秒)", GUILayout.Width(140));
                    entry.FloatParam2 = EditorGUILayout.FloatField(entry.FloatParam2);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.HelpBox("射线命中敌人后，等待此时间才触发实际伤害。用于定时炸弹、延迟引爆等效果。", MessageType.None);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("延迟时间属性名", GUILayout.Width(140));
                    entry.FloatParam2Attr = EditorGUILayout.TextField(entry.FloatParam2Attr);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.HelpBox("填写 AttributeType 枚举名称。留空则使用固定值。", MessageType.None);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("延迟触发半径(米)", GUILayout.Width(140));
                    entry.FloatParam1 = EditorGUILayout.FloatField(entry.FloatParam1);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.HelpBox("延迟触发时，在当前子弹位置周围此半径内的所有敌人都会被命中。设为 0 则只伤害原始命中目标。", MessageType.None);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("触发半径属性名", GUILayout.Width(140));
                    entry.FloatParam1Attr = EditorGUILayout.TextField(entry.FloatParam1Attr);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.HelpBox("填写 AttributeType 枚举名称。留空则使用固定值。", MessageType.None);
                    break;

                case "AOE":
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("AOE 伤害半径(米)", GUILayout.Width(140));
                    float aoeRadius = entry.FloatParam1;
                    aoeRadius = EditorGUILayout.FloatField(aoeRadius);
                    entry.FloatParam1 = aoeRadius;
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.HelpBox("不依赖射线命中，直接对子弹当前位置周围此半径内的所有敌人造成范围伤害。", MessageType.None);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("伤害半径属性名", GUILayout.Width(140));
                    entry.FloatParam1Attr = EditorGUILayout.TextField(entry.FloatParam1Attr);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.HelpBox("填写 AttributeType 枚举名称。留空则使用固定值。", MessageType.None);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("检测间隔(秒)", GUILayout.Width(140));
                    float aoeInterval = entry.FloatParam2;
                    aoeInterval = EditorGUILayout.FloatField(aoeInterval);
                    entry.FloatParam2 = aoeInterval;
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.HelpBox("0=每帧检测，>0=每隔此秒数检测一次（如 0.5 表示每0.5秒检测一次）。", MessageType.None);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("检测间隔属性名", GUILayout.Width(140));
                    entry.FloatParam2Attr = EditorGUILayout.TextField(entry.FloatParam2Attr);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.HelpBox("填写 AttributeType 枚举名称。留空则使用固定值。", MessageType.None);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("单次触发", GUILayout.Width(140));
                    bool aoeSingleTrigger = entry.BoolParam1;
                    aoeSingleTrigger = EditorGUILayout.Toggle(aoeSingleTrigger);
                    if (aoeSingleTrigger != entry.BoolParam1)
                    {
                        entry = new HitModuleEntry
                        {
                            ModuleType = entry.ModuleType,
                            IntParam1 = entry.IntParam1,
                            FloatParam1 = entry.FloatParam1,
                            FloatParam2 = entry.FloatParam2,
                            BoolParam1 = aoeSingleTrigger,
                            IntParam2 = entry.IntParam2,
                            FloatParam3 = entry.FloatParam3,
                            IntParam1Attr = entry.IntParam1Attr,
                            FloatParam1Attr = entry.FloatParam1Attr,
                            FloatParam2Attr = entry.FloatParam2Attr,
                            IntParam2Attr = entry.IntParam2Attr,
                            FloatParam3Attr = entry.FloatParam3Attr,
                            BoolParam2 = entry.BoolParam2
                        };
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.HelpBox("开启后：触发一次伤害后子弹销毁。关闭后：持续造成范围伤害直到子弹消失。", MessageType.None);
                    break;

                case "Rect":
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("最大穿透次数", GUILayout.Width(140));
                    entry.IntParam1 = EditorGUILayout.IntField(entry.IntParam1);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.HelpBox("子弹穿过敌人后不销毁，可连续命中的最大敌人数量。设为 0 表示无限穿透。", MessageType.None);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("穿透次数属性名", GUILayout.Width(140));
                    entry.IntParam1Attr = EditorGUILayout.TextField(entry.IntParam1Attr);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.HelpBox("填写 AttributeType 枚举名称。留空则使用固定值。", MessageType.None);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("矩形宽度(米)", GUILayout.Width(140));
                    entry.FloatParam1 = EditorGUILayout.FloatField(entry.FloatParam1);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.HelpBox("垂直于飞行方向（X轴）的检测宽度。", MessageType.None);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("矩形宽度属性名", GUILayout.Width(140));
                    entry.FloatParam1Attr = EditorGUILayout.TextField(entry.FloatParam1Attr);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.HelpBox("填写 AttributeType 枚举名称。留空则使用固定值。", MessageType.None);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("矩形长度(米)", GUILayout.Width(140));
                    entry.FloatParam2 = EditorGUILayout.FloatField(entry.FloatParam2);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.HelpBox("沿飞行方向（Y轴）的检测长度。", MessageType.None);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("矩形长度属性名", GUILayout.Width(140));
                    entry.FloatParam2Attr = EditorGUILayout.TextField(entry.FloatParam2Attr);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.HelpBox("填写 AttributeType 枚举名称。留空则使用固定值。", MessageType.None);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("重复命中间隔(秒)", GUILayout.Width(140));
                    entry.FloatParam3 = EditorGUILayout.FloatField(entry.FloatParam3);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.HelpBox("对同一目标再次造成伤害所需的时间间隔。设为 0 表示每次碰撞都造成伤害（无间隔限制）。", MessageType.None);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("重复命中间隔属性名", GUILayout.Width(140));
                    entry.FloatParam3Attr = EditorGUILayout.TextField(entry.FloatParam3Attr);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.HelpBox("填写 AttributeType 枚举名称。留空则使用固定值。", MessageType.None);
                    break;

                case "Split":
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("分裂数量", GUILayout.Width(140));
                    entry.IntParam1 = EditorGUILayout.IntField(entry.IntParam1);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.HelpBox("命中后分裂的子弹数量。", MessageType.None);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("分裂数量属性名", GUILayout.Width(140));
                    entry.IntParam1Attr = EditorGUILayout.TextField(entry.IntParam1Attr);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.HelpBox("填写 AttributeType 枚举名称。留空则使用固定值。", MessageType.None);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("分裂伤害继承", GUILayout.Width(140));
                    entry.FloatParam1 = EditorGUILayout.FloatField(entry.FloatParam1);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.HelpBox("分裂子弹的伤害继承比例（0-1之间），1表示100%伤害。", MessageType.None);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("分裂伤害属性名", GUILayout.Width(140));
                    entry.FloatParam1Attr = EditorGUILayout.TextField(entry.FloatParam1Attr);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.HelpBox("填写 AttributeType 枚举名称。留空则使用固定值。", MessageType.None);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("再次分裂伤害", GUILayout.Width(140));
                    entry.FloatParam2 = EditorGUILayout.FloatField(entry.FloatParam2);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.HelpBox("第二次分裂时的伤害继承比例。", MessageType.None);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("再次分裂伤害属性名", GUILayout.Width(140));
                    entry.FloatParam2Attr = EditorGUILayout.TextField(entry.FloatParam2Attr);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.HelpBox("填写 AttributeType 枚举名称。留空则使用固定值。", MessageType.None);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("分裂次数", GUILayout.Width(140));
                    entry.IntParam2 = EditorGUILayout.IntField(entry.IntParam2);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.HelpBox("再次分裂的次数：0=不分裂，1=可再分裂1次，2=可再分裂2次。", MessageType.None);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("分裂次数属性名", GUILayout.Width(140));
                    entry.IntParam2Attr = EditorGUILayout.TextField(entry.IntParam2Attr);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.HelpBox("填写 AttributeType 枚举名称。留空则使用固定值。", MessageType.None);
                    break;

                case "Catapult":
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("最大弹射次数", GUILayout.Width(140));
                    entry.IntParam1 = EditorGUILayout.IntField(entry.IntParam1);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.HelpBox("击杀敌人后可弹射攻击下一个敌人的最大次数。设为 0 表示不限制。", MessageType.None);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("弹射次数属性名", GUILayout.Width(140));
                    entry.IntParam1Attr = EditorGUILayout.TextField(entry.IntParam1Attr);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.HelpBox("填写 AttributeType 枚举名称。留空则使用固定值。", MessageType.None);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("搜索距离(米)", GUILayout.Width(140));
                    entry.FloatParam1 = EditorGUILayout.FloatField(entry.FloatParam1);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.HelpBox("搜索下一个弹射目标的最大距离。设为 0 表示不限制。", MessageType.None);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("搜索距离属性名", GUILayout.Width(140));
                    entry.FloatParam1Attr = EditorGUILayout.TextField(entry.FloatParam1Attr);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.HelpBox("填写 AttributeType 枚举名称。留空则使用固定值。", MessageType.None);
                    break;

                case "AttachBuff":
                    int attachBuffId = entry.IntParam5 > 0 ? entry.IntParam5 : entry.IntParam1;
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Buff模板Id", GUILayout.Width(140));
                    attachBuffId = EditorGUILayout.IntField(attachBuffId);
                    entry.IntParam5 = attachBuffId;
                    entry.IntParam1 = attachBuffId;
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.HelpBox("AttachBuff 现在为模板引用模式：外部只填写 Buff模板Id。", MessageType.Info);
                    DrawOpenBuffLibraryButton(attachBuffId);
                    DrawBuffTemplateSummary(attachBuffId);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("作用目标", GUILayout.Width(140));
                    int applySideIdx = (entry.StringParam4 ?? "Enemy") == "Self" ? 1 : 0;
                    applySideIdx = EditorGUILayout.Popup(applySideIdx, new[] { "敌方", "己方" });
                    entry.StringParam4 = applySideIdx == 1 ? "Self" : "Enemy";
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.HelpBox("仅保留命中后作用到敌方还是施法者自身。其它 Buff 行为全部以模板库为准。", MessageType.None);
                    break;
            }

            EditorGUI.indentLevel--;
            return entry;
        }

        private void DrawAoeDamageParams()
        {
            EditorGUILayout.LabelField("范围伤害配置", EditorStyles.miniLabel);
            DrawAssetSelector("命中特效路径:", "hit_effect", "Prefab");
            EditorGUILayout.Space(5);
            DrawAoeShapeParams();
            EditorGUILayout.Space(2);
            EditorGUILayout.HelpBox(
                "AoeDamage 节点对范围内所有敌人造成伤害（发布 ProjectileHitEvent）。\n" +
                "后续 ApplyBuff / PullMonster 若选 target=Hit，可复用本节点写入的命中列表。",
                MessageType.Info);
        }

        private void DrawAoeSelectParams()
        {
            EditorGUILayout.LabelField("范围选取配置", EditorStyles.miniLabel);

            bool clearHitList = _nodeConfig.GetParamBool("clear_hit_list", true);
            bool newClearHitList = EditorGUILayout.Toggle("选取前清空命中列表", clearHitList);
            if (newClearHitList != clearHitList)
            {
                _nodeConfig.SetParam("clear_hit_list", newClearHitList ? "true" : "false");
            }
            EditorGUILayout.HelpBox(
                "开启后每次执行会重置 HitMonsters，仅保留本次范围内目标；\n" +
                "关闭则追加到已有命中列表（与 AoeDamage 默认行为一致）。",
                MessageType.None);

            EditorGUILayout.Space(5);
            DrawAoeShapeParams();
            EditorGUILayout.Space(2);
            EditorGUILayout.HelpBox(
                "AoeSelect 仅检测范围内目标并写入 HitMonsters，不造成伤害。\n" +
                "典型链路：AoeSelect → ApplyBuff（target=命中目标）或 PullMonster（target=Hit）。",
                MessageType.Info);
        }

        private void DrawSelectTargetParams()
        {
            EditorGUILayout.LabelField("目标选取配置", EditorStyles.miniLabel);

            string[] targetOptions = { "Target", "Self", "EnemyGroup", "AllyGroup", "RandomAlly", "RandomAllyExcludeSelf", "RandomEnemy", "Hit" };
            string[] targetLabels = { "目标", "自身", "敌方群体", "友方群体", "随机友方(包含自己)", "随机友方(不包含自己)", "随机敌方", "命中目标" };
            var currentTarget = _nodeConfig.GetParamString("target", "Target");
            int targetIdx = Array.IndexOf(targetOptions, currentTarget);
            if (targetIdx < 0) targetIdx = 0;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("选取规则", GUILayout.Width(130));
            int newTargetIdx = EditorGUILayout.Popup(targetIdx, targetLabels);
            if (newTargetIdx != targetIdx)
            {
                _nodeConfig.SetParam("target", targetOptions[newTargetIdx]);
            }
            EditorGUILayout.EndHorizontal();

            bool clearSelectedList = _nodeConfig.GetParamBool("clear_selected_list", true);
            bool newClearSelectedList = EditorGUILayout.Toggle("选取前清空已选列表", clearSelectedList);
            if (newClearSelectedList != clearSelectedList)
            {
                _nodeConfig.SetParam("clear_selected_list", newClearSelectedList ? "true" : "false");
            }

            bool excludePrevious = _nodeConfig.GetParamBool("exclude_previous_selected", true);
            bool newExcludePrevious = EditorGUILayout.Toggle("随机不重复已选目标", excludePrevious);
            if (newExcludePrevious != excludePrevious)
            {
                _nodeConfig.SetParam("exclude_previous_selected", newExcludePrevious ? "true" : "false");
            }
            EditorGUILayout.HelpBox(
                "循环内使用 RandomAlly / RandomEnemy 时建议开启：每次 SelectTarget 会避开本轮技能已随机过的目标。\n" +
                "候选用尽后自动重新允许重复。",
                MessageType.None);

            EditorGUILayout.HelpBox(
                "将解析出的目标写入 SelectedTargets，并同步到 context.Target / TargetPosition。\n" +
                "下游节点即使使用默认「技能目标」瞄准，也会跟随本次选取结果。\n\n" +
                "后续可配置「已选取目标 / Selected」的节点：\n" +
                "• ApplyBuff — 施加目标 = 已选取目标\n" +
                "• Effect — 位置 = 已选取目标\n" +
                "• AnimatedEffect — 位置 = 已选取目标（含慰灵圣咏，仅飞向该目标）\n" +
                "• Projectile — 瞄准目标 = 已选取目标\n" +
                "• FanProjectile — 瞄准目标 = 已选取目标\n\n" +
                "典型链路：SelectTarget → Effect / AnimatedEffect + ApplyBuff（均选已选取目标）。",
                MessageType.Info);
        }

        private void DrawSelectedTargetConsumerHint()
        {
            EditorGUILayout.HelpBox(
                "「已选取目标」读取前置 SelectTarget 节点写入的 SelectedTargets。\n" +
                "请在本节点之前执行 SelectTarget；若无已选目标，运行时本节点会跳过或无效。",
                MessageType.Warning);
        }

        private void DrawProjectileAimTargetSelector()
        {
            string[] aimOptions = { "Target", "Selected" };
            string[] aimLabels = { "技能目标", "已选取目标" };
            var currentAim = _nodeConfig.GetParamString("aim_target", "Target");
            int aimIdx = Array.IndexOf(aimOptions, currentAim);
            if (aimIdx < 0) aimIdx = 0;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("瞄准目标", GUILayout.Width(130));
            int newAimIdx = EditorGUILayout.Popup(aimIdx, aimLabels);
            if (newAimIdx != aimIdx)
            {
                _nodeConfig.SetParam("aim_target", aimOptions[newAimIdx]);
                currentAim = aimOptions[newAimIdx];
            }
            EditorGUILayout.EndHorizontal();

            if (currentAim == "Selected")
            {
                DrawSelectedTargetConsumerHint();
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "技能目标：使用 context.Target 位置，无 Target 时用 TargetPosition。\n" +
                    "已选取目标：使用 SelectTarget 写入的第一个目标（可与随机敌方等规则配合）。",
                    MessageType.None);
            }
        }

        private void DrawPullMonsterParams()
        {
            EditorGUILayout.LabelField("拉拽怪物配置", EditorStyles.miniLabel);

            string[] targetOptions = { "Hit", "Aoe" };
            string[] targetLabels = { "命中列表(Hit)", "本节点范围检测(Aoe)" };
            var currentTarget = _nodeConfig.GetParamString("target", "Hit");
            int targetIdx = System.Array.IndexOf(targetOptions, currentTarget);
            if (targetIdx < 0) targetIdx = 0;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("目标来源", GUILayout.Width(130));
            int newTargetIdx = EditorGUILayout.Popup(targetIdx, targetLabels);
            if (newTargetIdx != targetIdx)
            {
                _nodeConfig.SetParam("target", targetOptions[newTargetIdx]);
                currentTarget = targetOptions[newTargetIdx];
            }
            EditorGUILayout.EndHorizontal();

            if (currentTarget == "Hit")
            {
                EditorGUILayout.HelpBox(
                    "使用前置节点写入的 HitMonsters。\n" +
                    "可接在 AoeSelect 或 AoeDamage 之后（二者都会填充命中列表）。",
                    MessageType.None);
            }
            else
            {
                EditorGUILayout.Space(5);
                DrawAoeShapeParams();
            }

            EditorGUILayout.Space(5);

            string[] pullToOptions = { "ReleasePoint", "Caster" };
            string[] pullToLabels = { "拖拽释放点", "施法者位置" };
            var currentPullTo = _nodeConfig.GetParamString("pull_to", "ReleasePoint");
            int pullToIdx = System.Array.IndexOf(pullToOptions, currentPullTo);
            if (pullToIdx < 0) pullToIdx = 0;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("拉拽终点", GUILayout.Width(130));
            int newPullToIdx = EditorGUILayout.Popup(pullToIdx, pullToLabels);
            if (newPullToIdx != pullToIdx)
            {
                _nodeConfig.SetParam("pull_to", pullToOptions[newPullToIdx]);
            }
            EditorGUILayout.EndHorizontal();

            var duration = EditorGUILayout.FloatField("拉拽时长(秒)", _nodeConfig.GetParamFloat("duration", 0.3f));
            _nodeConfig.SetParam("duration", duration.ToString());

            var pullRatio = EditorGUILayout.Slider("拉拽比例(0~1)", _nodeConfig.GetParamFloat("pull_ratio", 1f), 0f, 1f);
            _nodeConfig.SetParam("pull_ratio", pullRatio.ToString());

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("拖拽幅度", EditorStyles.miniLabel);

            var dragAmplitude = EditorGUILayout.FloatField("沿拖拽方向偏移(米)", _nodeConfig.GetParamFloat("drag_amplitude", 0f));
            _nodeConfig.SetParam("drag_amplitude", dragAmplitude.ToString());

            bool scaleByDistance = _nodeConfig.GetParamBool("scale_drag_amplitude_by_distance", false);
            bool newScaleByDistance = EditorGUILayout.Toggle("偏移 × 拖拽距离", scaleByDistance);
            if (newScaleByDistance != scaleByDistance)
            {
                _nodeConfig.SetParam("scale_drag_amplitude_by_distance", newScaleByDistance ? "true" : "false");
            }

            EditorGUILayout.HelpBox(
                "拖拽幅度：在拉拽终点上沿「施法者→释放点」方向追加偏移。\n" +
                "- 固定偏移：关闭「偏移×拖拽距离」，填写米数。\n" +
                "- 随拖拽变长：开启后 实际偏移 = 偏移值 × 拖拽距离（拖得越远拉得越远）。\n" +
                "拉拽比例小于 1 时只移动部分距离（如 0.5 拉到一半）。",
                MessageType.Info);
        }

        private void DrawAoeShapeParams()
        {
            EditorGUILayout.LabelField("形状配置", EditorStyles.miniLabel);

            string[] shapeOptions = { "Circle", "Rectangle", "Fan" };
            string[] shapeLabels = { "圆形", "矩形", "扇形" };
            var currentShape = _nodeConfig.GetParamString("shape", "Circle");
            int currentIdx = Array.IndexOf(shapeOptions, currentShape);
            if (currentIdx < 0) currentIdx = 0;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("形状", GUILayout.Width(130));
            int newIdx = EditorGUILayout.Popup(currentIdx, shapeLabels);
            if (newIdx != currentIdx)
            {
                _nodeConfig.SetParam("shape", shapeOptions[newIdx]);
                currentShape = shapeOptions[newIdx];
            }
            EditorGUILayout.EndHorizontal();

            if (currentShape == "Circle")
            {
                var range = EditorGUILayout.FloatField("范围半径(米)", _nodeConfig.GetParamFloat("range", 3f));
                _nodeConfig.SetParam("range", range.ToString());

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("范围属性名", GUILayout.Width(130));
                var rangeAttr = EditorGUILayout.TextField(_nodeConfig.GetParamString("range_attr", ""));
                _nodeConfig.SetParam("range_attr", rangeAttr);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.HelpBox("填写 AttributeType 枚举名称，留空则使用固定值。", MessageType.None);
            }
            else if (currentShape == "Rectangle")
            {
                EditorGUILayout.HelpBox("Rectangle 使用长(width)和宽(height)来定义尺寸，单位为米。", MessageType.Info);

                var rectWidth = EditorGUILayout.FloatField("矩形长(米)", _nodeConfig.GetParamFloat("rect_width", 10f));
                _nodeConfig.SetParam("rect_width", rectWidth.ToString());

                var rectHeight = EditorGUILayout.FloatField("矩形宽(米)", _nodeConfig.GetParamFloat("rect_height", 2f));
                _nodeConfig.SetParam("rect_height", rectHeight.ToString());
            }
            else if (currentShape == "Fan")
            {
                var range = EditorGUILayout.FloatField("扇形半径(米)", _nodeConfig.GetParamFloat("range", 3f));
                _nodeConfig.SetParam("range", range.ToString());

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("范围属性名", GUILayout.Width(130));
                var rangeAttr = EditorGUILayout.TextField(_nodeConfig.GetParamString("range_attr", ""));
                _nodeConfig.SetParam("range_attr", rangeAttr);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.HelpBox("填写 AttributeType 枚举名称，留空则使用固定值。", MessageType.None);

                var fanAngle = EditorGUILayout.FloatField("扇形角度(度)", _nodeConfig.GetParamFloat("fan_angle", 60f));
                _nodeConfig.SetParam("fan_angle", fanAngle.ToString());
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("中心点配置", EditorStyles.miniLabel);

            string[] centerOptions = { "Caster", "Target", "Position" };
            string[] centerLabels = { "施法者", "目标", "固定坐标" };
            var currentCenter = _nodeConfig.GetParamString("center_source", "Caster");
            int centerIdx = Array.IndexOf(centerOptions, currentCenter);
            if (centerIdx < 0) centerIdx = 0;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("中心点来源", GUILayout.Width(130));
            int newCenterIdx = EditorGUILayout.Popup(centerIdx, centerLabels);
            if (newCenterIdx != centerIdx)
            {
                _nodeConfig.SetParam("center_source", centerOptions[newCenterIdx]);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox("Caster=施法者位置为中心，Target=目标位置为中心，Position=固定坐标为中心。", MessageType.None);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("朝向配置", EditorStyles.miniLabel);

            bool useTargetFacing = _nodeConfig.GetParamString("use_target_facing", "false") == "true";
            bool newUseTargetFacing = EditorGUILayout.Toggle("使用目标朝向", useTargetFacing);
            if (newUseTargetFacing != useTargetFacing)
            {
                _nodeConfig.SetParam("use_target_facing", newUseTargetFacing ? "true" : "false");
            }
            EditorGUILayout.HelpBox(
                "开启后：矩形/扇形的朝向跟随目标 facing 方向。\n" +
                "关闭后：矩形朝右(0°)，扇形默认朝右。\n" +
                "注意：特效 up 朝向目标时，碰撞盒 X+ 也需偏转90°对齐。",
                MessageType.None);
        }

        private void DrawDelayParams()
        {
            EditorGUILayout.LabelField("延迟配置", EditorStyles.miniLabel);

            var duration = EditorGUILayout.FloatField("延迟时间(秒)", _nodeConfig.GetParamFloat("duration", 1f));
            _nodeConfig.SetParam("duration", duration.ToString());

            var durationAttrName = _nodeConfig.GetParamString("duration_attr", "");
            durationAttrName = EditorGUILayout.TextField("延迟属性名", durationAttrName);
            _nodeConfig.SetParam("duration_attr", durationAttrName);
            EditorGUILayout.HelpBox("填写 AttributeType 枚举名称，如 SkillDuration。留空则使用固定值。", MessageType.None);

            EditorGUILayout.Space(5);

            EditorGUILayout.HelpBox(
                "Delay 节点用于等待指定时间。\n" +
                "- duration: 等待时间（秒）\n" +
                "- duration_attr: 从技能卡片属性读取延迟时间，配置后覆盖固定值",
                MessageType.Info);
        }

        private void DrawPlayMixAnimationParams()
        {
            EditorGUILayout.LabelField("混合动画", EditorStyles.miniLabel);

            var animName = EditorGUILayout.TextField("动画名", _nodeConfig.GetParamString("anim_name", ""));
            _nodeConfig.SetParam("anim_name", animName);

            var track = EditorGUILayout.IntField("轨道", _nodeConfig.GetParamInt("track", 1));
            _nodeConfig.SetParam("track", track.ToString());

            var alpha = EditorGUILayout.Slider("混合权重", _nodeConfig.GetParamFloat("alpha", 1f), 0f, 1f);
            _nodeConfig.SetParam("alpha", alpha.ToString("F2"));

            var loop = EditorGUILayout.Toggle("循环", _nodeConfig.GetParamBool("loop", true));
            _nodeConfig.SetParam("loop", loop.ToString().ToLower());
        }

        private void DrawStopMixAnimationParams()
        {
            EditorGUILayout.LabelField("结束混合动画", EditorStyles.miniLabel);

            var animName = EditorGUILayout.TextField("动画名", _nodeConfig.GetParamString("anim_name", ""));
            _nodeConfig.SetParam("anim_name", animName);

            var fadeDuration = EditorGUILayout.FloatField("淡出时间(秒)", _nodeConfig.GetParamFloat("fade_duration", 0.2f));
            _nodeConfig.SetParam("fade_duration", fadeDuration.ToString());

            EditorGUILayout.HelpBox("填写与 PlayMixAnimation 相同的动画名，会通过 SetEmptyAnimation 淡出关闭。", MessageType.None);
        }

        private void DrawReturnAnimationParams()
        {
            EditorGUILayout.LabelField("Return 动画", EditorStyles.miniLabel);

            var animName = EditorGUILayout.TextField("动画名", _nodeConfig.GetParamString("anim_name", "return"));
            _nodeConfig.SetParam("anim_name", animName);

            EditorGUILayout.HelpBox(
                "需在技能 Spine 动画中配置 hide 事件；hide 后保持姿态，由本节点播放 return。\n" +
                "CanPlayReturn 校验：仍处于 hide 等待且 playId 未被新技能抢占。",
                MessageType.Info);
        }

        private void DrawEffectParams()
        {
            EditorGUILayout.LabelField("特效配置", EditorStyles.miniLabel);

            DrawAssetSelector("VFX 预制体（可选）:", "vfx", "Prefab");

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("位置", EditorStyles.miniLabel);

            string[] positionOptions = { "Target", "Caster", "Enemy", "Selected" };
            string[] positionLabels = { "目标位置", "施法者位置", "敌人位置", "已选取目标" };
            var currentPosition = _nodeConfig.GetParamString("position", "Target");
            int positionIdx = Array.IndexOf(positionOptions, currentPosition);
            if (positionIdx < 0) positionIdx = 0;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("位置", GUILayout.Width(130));
            int newPositionIdx = EditorGUILayout.Popup(positionIdx, positionLabels);
            if (newPositionIdx != positionIdx)
            {
                _nodeConfig.SetParam("position", positionOptions[newPositionIdx]);
                currentPosition = positionOptions[newPositionIdx];
            }
            EditorGUILayout.EndHorizontal();

            if (currentPosition == "Selected")
            {
                DrawSelectedTargetConsumerHint();
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("朝向", EditorStyles.miniLabel);

            string[] rotationOptions = { "None", "FaceCaster", "FaceTarget", "Up", "Down", "Left", "Right" };
            string[] rotationLabels = { "无旋转", "朝向施法者", "朝向目标", "向上(-90°)", "向下(90°)", "向左(180°)", "向右(0°)" };
            var currentRotation = _nodeConfig.GetParamString("rotation", "None");
            int rotationIdx = Array.IndexOf(rotationOptions, currentRotation);
            if (rotationIdx < 0) rotationIdx = 0;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("朝向", GUILayout.Width(130));
            int newRotationIdx = EditorGUILayout.Popup(rotationIdx, rotationLabels);
            if (newRotationIdx != rotationIdx)
            {
                _nodeConfig.SetParam("rotation", rotationOptions[newRotationIdx]);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("位置偏移", EditorStyles.miniLabel);

            var randomOffsetRadius = EditorGUILayout.FloatField("圆形随机偏移半径(米)", _nodeConfig.GetParamFloat("random_offset_radius", 0f));
            _nodeConfig.SetParam("random_offset_radius", randomOffsetRadius.ToString());

            if (randomOffsetRadius > 0f)
            {
                EditorGUILayout.HelpBox($"特效将在半径 {randomOffsetRadius} 米范围内随机偏移", MessageType.Info);
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("X轴锁定", EditorStyles.miniLabel);

            var lockX = EditorGUILayout.Toggle("锁定X坐标", _nodeConfig.GetParamBool("lock_x", false));
            _nodeConfig.SetParam("lock_x", lockX ? "true" : "false");
            if (lockX)
            {
                var lockedXValue = EditorGUILayout.FloatField("锁定X值", _nodeConfig.GetParamFloat("locked_x_value", 0f));
                _nodeConfig.SetParam("locked_x_value", lockedXValue.ToString());
                EditorGUILayout.HelpBox($"开启后特效最终 X 将固定为 {lockedXValue}", MessageType.None);
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("缩放控制", EditorStyles.miniLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("特效缩放(X,Y)", GUILayout.Width(130));
            var scaleRaw = _nodeConfig.GetParamString("vfx_scale", "");
            var newScaleRaw = EditorGUILayout.TextField(scaleRaw);
            if (newScaleRaw != scaleRaw)
            {
                _nodeConfig.SetParam("vfx_scale", newScaleRaw);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox(
                "设置特效缩放，支持三种格式：\n" +
                "  2      → X=Y=2（等比缩放）\n" +
                "  5,2    → X=5, Y=2（独立缩放，可与碰撞体对齐）\n" +
                "  留空   → 使用预制体原始大小",
                MessageType.None);

            var scaleAttrName = _nodeConfig.GetParamString("vfx_scale_attr", "");
            scaleAttrName = EditorGUILayout.TextField("缩放属性名", scaleAttrName);
            _nodeConfig.SetParam("vfx_scale_attr", scaleAttrName);
            EditorGUILayout.HelpBox("填写 AttributeType 枚举名称，如 CriticalRate。留空则使用固定值或预制体原始大小。", MessageType.None);

            EditorGUILayout.Space(5);

            EditorGUILayout.HelpBox(
                "Effect 节点用于播放视觉和音效特效。\n" +
                "- vfx: 特效预制体路径（留空跳过）\n" +
                "- position: 播放位置 (Target/Caster/Enemy/Selected；Selected 需前置 SelectTarget)\n" +
                "- rotation: 朝向控制 (None/FaceCaster/FaceTarget/Up/Down/Left/Right)\n" +
                "- lock_x/locked_x_value: 可将特效 X 坐标固定为指定值\n" +
                "- vfx_scale: 缩放控制，可设为与 AoeDamage 碰撞体相同的尺寸\n" +
                "- vfx_scale_attr: 从技能卡片属性读取缩放值（如 CriticalRate）\n" +
                "- random_offset_radius: 圆形随机偏移半径，0则不偏移",
                MessageType.Info);
        }

        private void DrawAnimatedEffectParams()
        {
            EditorGUILayout.LabelField("动画特效配置", EditorStyles.miniLabel);

            DrawAssetSelector("特效展示1 VFX:", "vfx", "Prefab");
            DrawAssetSelector("特效展示2 VFX:", "crater_vfx", "Prefab");

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("动画类型", EditorStyles.miniLabel);

            var animationType = EditorGUILayout.TextField("类型", _nodeConfig.GetParamString("animation_type", "Fall"));
            _nodeConfig.SetParam("animation_type", animationType);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("预设:", GUILayout.Width(50));
            if (GUILayout.Button("陨石特效")) { _nodeConfig.SetParam("animation_type", "Fall"); }
            if (GUILayout.Button("Rise")) { _nodeConfig.SetParam("animation_type", "Rise"); }
            if (GUILayout.Button("死神挥砍")) { _nodeConfig.SetParam("animation_type", "zhanji"); }
            if (GUILayout.Button("慰灵圣咏")) { _nodeConfig.SetParam("animation_type", "RequiemChant"); }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("位置", EditorStyles.miniLabel);

            string[] positionOptions = { "Target", "Caster", "Enemy", "Selected" };
            string[] positionLabels = { "目标位置", "施法者位置", "敌人位置", "已选取目标" };
            var currentPosition = _nodeConfig.GetParamString("position", "Target");
            int positionIdx = Array.IndexOf(positionOptions, currentPosition);
            if (positionIdx < 0) positionIdx = 0;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("位置", GUILayout.Width(130));
            int newPositionIdx = EditorGUILayout.Popup(positionIdx, positionLabels);
            if (newPositionIdx != positionIdx)
            {
                _nodeConfig.SetParam("position", positionOptions[newPositionIdx]);
                currentPosition = positionOptions[newPositionIdx];
            }
            EditorGUILayout.EndHorizontal();

            if (currentPosition == "Selected")
            {
                DrawSelectedTargetConsumerHint();
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("位置偏移", EditorStyles.miniLabel);

            var randomOffsetRadius = EditorGUILayout.FloatField("落点随机偏移半径(米)", _nodeConfig.GetParamFloat("random_offset_radius", 0f));
            _nodeConfig.SetParam("random_offset_radius", randomOffsetRadius.ToString());

            if (randomOffsetRadius > 0f)
            {
                EditorGUILayout.HelpBox($"落点将在半径 {randomOffsetRadius} 米范围内随机偏移", MessageType.Info);
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("缩放控制", EditorStyles.miniLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("特效缩放(X,Y)", GUILayout.Width(130));
            var scaleRaw = _nodeConfig.GetParamString("vfx_scale", "");
            var newScaleRaw = EditorGUILayout.TextField(scaleRaw);
            if (newScaleRaw != scaleRaw)
            {
                _nodeConfig.SetParam("vfx_scale", newScaleRaw);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox(
                "设置特效缩放，支持三种格式：\n" +
                "  2      → X=Y=2（等比缩放）\n" +
                "  5,2    → X=5, Y=2（独立缩放，可与碰撞体对齐）\n" +
                "  留空   → 使用预制体原始大小",
                MessageType.None);

            var scaleAttrName = _nodeConfig.GetParamString("vfx_scale_attr", "");
            scaleAttrName = EditorGUILayout.TextField("缩放属性名", scaleAttrName);
            _nodeConfig.SetParam("vfx_scale_attr", scaleAttrName);
            EditorGUILayout.HelpBox("填写 AttributeType 枚举名称，如 CriticalRate。留空则使用固定值或预制体原始大小。", MessageType.None);

            EditorGUILayout.Space(5);

            EditorGUILayout.HelpBox(
                "AnimatedEffect 节点用于播放带动画过程的特效（陨石坠落、上升等）。\n" +
                "- vfx: 动画特效预制体路径\n" +
                "- crater_vfx: 动画结束后在落点播放的陨石坑特效\n" +
                "- animation_type: 动画类型（Fall/Rise/Bezier）\n" +
                "- position: 播放位置 (Target/Caster/Enemy/Selected；Selected 需前置 SelectTarget)\n" +
                "- vfx_scale: 缩放控制\n" +
                "- vfx_scale_attr: 从技能卡片属性读取缩放值\n" +
                "- random_offset_radius: 落点随机偏移半径，0则不偏移",
                MessageType.Info);
        }

        private void DrawSoundParams()
        {
            EditorGUILayout.LabelField("音效配置", EditorStyles.miniLabel);

            var soundId = EditorGUILayout.IntField("音效ID", _nodeConfig.GetParamInt("sound", 0));
            _nodeConfig.SetParam("sound", soundId.ToString());

            EditorGUILayout.Space(5);

            EditorGUILayout.HelpBox(
                "Sound 节点用于播放音效。\n" +
                "- sound: 音效ID（0表示跳过）",
                MessageType.Info);
        }

        private void DrawResetOffsetParams()
        {
            EditorGUILayout.LabelField("重置位置偏移", EditorStyles.miniLabel);

            EditorGUILayout.HelpBox(
                "将 context.PositionOffset 重置为 (0, 0)。\n\n" +
                "使用场景：陨石落在 A 位置后，需要在原始目标位置 B 执行 AoeDamage，此时在两者之间插入 ResetOffset 节点。\n\n" +
                "位置关系示意：\n" +
                "AnimatedEffect (偏移至 A) → ResetOffset → Effect/AoeDamage (以 B 为基准)",
                MessageType.Info);
        }

        private void DrawRecycleEffectParams()
        {
            EditorGUILayout.LabelField("特效回收", EditorStyles.miniLabel);

            EditorGUILayout.HelpBox(
                "回收当前技能执行中创建的特效实例。\n\n" +
                "target=All：回收全部已追踪特效（Effect 节点产生的实例）。\n" +
                "target=ByName：只回收名称包含 effectName 的特效。",
                MessageType.Info);

            var target = EditorGUILayout.TextField("回收目标", _nodeConfig.GetParamString("target", "All"));
            _nodeConfig.SetParam("target", target);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("快捷设置:", GUILayout.Width(70));
            if (GUILayout.Button("All")) { _nodeConfig.SetParam("target", "All"); }
            if (GUILayout.Button("ByName")) { _nodeConfig.SetParam("target", "ByName"); }
            EditorGUILayout.EndHorizontal();

            GUI.enabled = _nodeConfig.GetParamString("target", "All") == "ByName";
            var effectName = EditorGUILayout.TextField("特效名称", _nodeConfig.GetParamString("effectName", ""));
            _nodeConfig.SetParam("effectName", effectName);
            GUI.enabled = true;

            EditorGUILayout.HelpBox(
                "使用示例：\n" +
                "Cast → Effect → LoopSequential(body) → Effect → RecycleEffect(All) → Delay → Effect\n" +
                "LoopSequential 循环结束后统一回收全部特效，Delay 后的新特效单独按需回收。",
                MessageType.None);
        }

        private void DrawApplyBuffParams()
        {
            EditorGUILayout.LabelField("Buff配置", EditorStyles.miniLabel);

            string[] targetOptions = { "Target", "Self", "EnemyGroup", "AllyGroup", "RandomAlly", "RandomAllyExcludeSelf", "RandomEnemy", "Hit", "Selected" };
            string[] targetLabels = { "目标", "自身", "敌方群体", "友方群体", "随机友方(包含自己)", "随机友方(不包含自己)", "随机敌方", "命中目标", "已选取目标" };
            var currentTarget = _nodeConfig.GetParamString("target", "Target");
            int targetIdx = Array.IndexOf(targetOptions, currentTarget);
            if (targetIdx < 0) targetIdx = 0;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("施加目标", GUILayout.Width(130));
            int newTargetIdx = EditorGUILayout.Popup(targetIdx, targetLabels);
            if (newTargetIdx != targetIdx)
            {
                _nodeConfig.SetParam("target", targetOptions[newTargetIdx]);
                currentTarget = targetOptions[newTargetIdx];
            }
            EditorGUILayout.EndHorizontal();

            if (currentTarget == "Selected")
            {
                DrawSelectedTargetConsumerHint();
            }
            else
            {
                EditorGUILayout.HelpBox("ApplyBuff 节点负责决定把 Buff 施加给谁，可配置多个目标范围。", MessageType.None);
            }

            var currentBuffId = _nodeConfig.GetParamInt("buff_id", 0);
            DrawOpenBuffLibraryButton(currentBuffId);
            var newBuffId = DrawBuffTemplateIdField("Buff模板Id", currentBuffId);
            if (newBuffId != currentBuffId)
            {
                _nodeConfig.SetParam("buff_id", newBuffId.ToString());
            }

            EditorGUILayout.HelpBox(
                "Buff 模板里的“作用目标”描述 Buff 自身语义；ApplyBuff 上方的“施加目标”决定这次把 Buff 套到谁身上。\n" +
                "如果模板效果没有单独填写目标，就默认使用这里的施加目标。\n" +
                "施加目标选「已选取目标」时，需前置 SelectTarget 节点。",
                MessageType.Info);
        }

        private string GetBuffTypeChineseName(string buffTypeName)
        {
            switch (buffTypeName)
            {
                case "Burn": return "灼烧";
                case "Vertigo": return "冻结";
                case "Vulnerable": return "易损";
                case "Moderate": return "减速";
                case "KnockBack": return "击退";
                case "Invincible": return "无敌";
                case "Accelerate": return "加速";
                case "RequiemChant": return "慰灵圣咏";
                case "Disarm": return "缴械";
                case "Health": return "血量";
                case "Weak": return "虚弱";
                case "MoonlightShield": return "月影庇护";
                case "EnergyPouring": return "能量灌注";
                case "Split" : return "分裂";
                default: return buffTypeName;
            }
        }

        private void DrawBranchParams()
        {
            EditorGUILayout.LabelField("等级分支配置", EditorStyles.miniLabel);

            var condition = EditorGUILayout.TextField("条件类型", _nodeConfig.GetParamString("condition", "SkillLevelGreaterThan"));
            _nodeConfig.SetParam("condition", condition);
            var compareAttr = EditorGUILayout.TextField("比较属性(compare_attr)", _nodeConfig.GetParamString("compare_attr", ""));
            _nodeConfig.SetParam("compare_attr", compareAttr);

            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("技能等级:", EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("> 大于")) { _nodeConfig.SetParam("condition", "SkillLevelGreaterThan"); }
            if (GUILayout.Button("< 小于")) { _nodeConfig.SetParam("condition", "SkillLevelLessThan"); }
            if (GUILayout.Button("= 等于")) { _nodeConfig.SetParam("condition", "SkillLevelEqual"); }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(">= 大于等于")) { _nodeConfig.SetParam("condition", "SkillLevelGreaterOrEqual"); }
            if (GUILayout.Button("<= 小于等于")) { _nodeConfig.SetParam("condition", "SkillLevelLessOrEqual"); }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("英灵等级:", EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("> 大于")) { _nodeConfig.SetParam("condition", "CasterLevelGreaterThan"); }
            if (GUILayout.Button("< 小于")) { _nodeConfig.SetParam("condition", "CasterLevelLessThan"); }
            if (GUILayout.Button("= 等于")) { _nodeConfig.SetParam("condition", "CasterLevelEqual"); }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(">= 大于等于")) { _nodeConfig.SetParam("condition", "CasterLevelGreaterOrEqual"); }
            if (GUILayout.Button("<= 小于等于")) { _nodeConfig.SetParam("condition", "CasterLevelLessOrEqual"); }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("技能属性(compare_attr):", EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("> 大于")) { _nodeConfig.SetParam("condition", "SkillAttrGreaterThan"); }
            if (GUILayout.Button("< 小于")) { _nodeConfig.SetParam("condition", "SkillAttrLessThan"); }
            if (GUILayout.Button("= 等于")) { _nodeConfig.SetParam("condition", "SkillAttrEqual"); }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(">= 大于等于")) { _nodeConfig.SetParam("condition", "SkillAttrGreaterOrEqual"); }
            if (GUILayout.Button("<= 小于等于")) { _nodeConfig.SetParam("condition", "SkillAttrLessOrEqual"); }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("英灵属性(compare_attr):", EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("> 大于")) { _nodeConfig.SetParam("condition", "CasterAttrGreaterThan"); }
            if (GUILayout.Button("< 小于")) { _nodeConfig.SetParam("condition", "CasterAttrLessThan"); }
            if (GUILayout.Button("= 等于")) { _nodeConfig.SetParam("condition", "CasterAttrEqual"); }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(">= 大于等于")) { _nodeConfig.SetParam("condition", "CasterAttrGreaterOrEqual"); }
            if (GUILayout.Button("<= 小于等于")) { _nodeConfig.SetParam("condition", "CasterAttrLessOrEqual"); }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);
            var compareValue = EditorGUILayout.FloatField("比较值(compare_value)", _nodeConfig.GetParamFloat("compare_value", 1f));
            _nodeConfig.SetParam("compare_value", compareValue.ToString());

            EditorGUILayout.Space(5);

            EditorGUILayout.HelpBox(
                "Branch 节点用于技能等级分支判断。\n" +
                "- condition: 条件类型\n" +
                "  * SkillLevelGreaterThan: 技能等级大于\n" +
                "  * SkillLevelLessThan: 技能等级小于\n" +
                "  * SkillLevelEqual: 技能等级等于\n" +
                "  * SkillLevelGreaterOrEqual: 技能等级大于等于\n" +
                "  * SkillLevelLessOrEqual: 技能等级小于等于\n" +
                "  * CasterLevelGreaterThan: 英灵等级大于\n" +
                "  * CasterLevelLessThan: 英灵等级小于\n" +
                "  * CasterLevelEqual: 英灵等级等于\n" +
                "  * CasterLevelGreaterOrEqual: 英灵等级大于等于\n" +
                "  * CasterLevelLessOrEqual: 英灵等级小于等于\n" +
                "  * SkillAttrGreaterThan/SkillAttrLessThan/SkillAttrEqual/SkillAttrGreaterOrEqual/SkillAttrLessOrEqual: 技能属性比较\n" +
                "  * CasterAttrGreaterThan/CasterAttrLessThan/CasterAttrEqual/CasterAttrGreaterOrEqual/CasterAttrLessOrEqual: 英灵属性比较\n" +
                "- compare_attr: 需要比较的属性名（如 SkillCdTime、SoldierAttackPower）\n" +
                "- compare_value: 比较值（支持小数）\n" +
                "  * true 分支：条件成立时执行\n" +
                "  * false 分支：条件不成立时执行",
                MessageType.Info);
        }

        private void DrawDashParams()
        {
            EditorGUILayout.LabelField("突进位移配置", EditorStyles.miniLabel);

            var distance = EditorGUILayout.FloatField("突进距离(厘米)", _nodeConfig.GetParamFloat("distance", 200f));
            _nodeConfig.SetParam("distance", distance.ToString());

            var speed = EditorGUILayout.FloatField("突进速度(米/秒)", _nodeConfig.GetParamFloat("speed", 10f));
            _nodeConfig.SetParam("speed", speed.ToString());

            var easing = EditorGUILayout.TextField("缓动曲线", _nodeConfig.GetParamString("easing", "OutQuad"));
            _nodeConfig.SetParam("easing", easing);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("预设曲线:", GUILayout.Width(70));
            if (GUILayout.Button("OutQuad")) { _nodeConfig.SetParam("easing", "OutQuad"); }
            if (GUILayout.Button("Linear")) { _nodeConfig.SetParam("easing", "Linear"); }
            if (GUILayout.Button("OutExpo")) { _nodeConfig.SetParam("easing", "OutExpo"); }
            if (GUILayout.Button("InOutQuad")) { _nodeConfig.SetParam("easing", "InOutQuad"); }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            EditorGUILayout.HelpBox(
                "Dash 节点用于突进位移。\n" +
                "- distance: 突进距离（厘米），会转换为米计算\n" +
                "- speed: 突进速度（米/秒）\n" +
                "- easing: DOTween缓动曲线\n" +
                "配合 Slash 节点可实现瞬袭斩效果",
                MessageType.Info);
        }

        private void DrawLoopParams()
        {
            EditorGUILayout.LabelField("循环执行配置", EditorStyles.miniLabel);

            var count = EditorGUILayout.IntField("循环次数", _nodeConfig.GetParamInt("count", 3));
            _nodeConfig.SetParam("count", count.ToString());

            var interval = EditorGUILayout.FloatField("间隔时间(秒)", _nodeConfig.GetParamFloat("interval", 0.2f));
            _nodeConfig.SetParam("interval", interval.ToString());

            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField("固定时长分配模式（可选）", EditorStyles.miniLabel);

            var duration = EditorGUILayout.FloatField("持续时间(秒)", _nodeConfig.GetParamFloat("duration", 0f));
            _nodeConfig.SetParam("duration", duration.ToString());

            var timesPerSecond = EditorGUILayout.FloatField("每秒次数", _nodeConfig.GetParamFloat("times_per_second", 0f));
            _nodeConfig.SetParam("times_per_second", timesPerSecond.ToString());
            EditorGUILayout.HelpBox(
                "同时配置 duration 与 times_per_second 时，会按固定时长均匀分配循环：\n" +
                "count = duration × times_per_second，interval = 1 / times_per_second。\n" +
                "例：duration=3、times_per_second=2 → 3 秒内触发 6 次，间隔 0.5 秒。",
                MessageType.None);

            var durationLoopCount = EditorGUILayout.IntField("固定总次数", _nodeConfig.GetParamInt("duration_loop_count", 0));
            _nodeConfig.SetParam("duration_loop_count", durationLoopCount.ToString());
            EditorGUILayout.HelpBox(
                "同时配置 duration 与 duration_loop_count 时，会按固定时长均匀分配循环：\n" +
                "count = duration_loop_count，interval = duration / duration_loop_count。\n" +
                "例：duration=2、duration_loop_count=5 → 2 秒内触发 5 次，间隔 0.4 秒。",
                MessageType.None);

            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField("从技能卡片属性读取（可选）", EditorStyles.miniLabel);

            var countAttrName = _nodeConfig.GetParamString("count_attr", "");
            countAttrName = EditorGUILayout.TextField("循环次数属性名", countAttrName);
            _nodeConfig.SetParam("count_attr", countAttrName);
            EditorGUILayout.HelpBox("填写 AttributeType 枚举名称，如 SkillContinuousAtkNum。留空则使用固定值。", MessageType.None);

            var intervalAttrName = _nodeConfig.GetParamString("interval_attr", "");
            intervalAttrName = EditorGUILayout.TextField("间隔时间属性名", intervalAttrName);
            _nodeConfig.SetParam("interval_attr", intervalAttrName);
            EditorGUILayout.HelpBox("填写 AttributeType 枚举名称，如 SkillContinuousAtkSpeed。留空则使用固定值。", MessageType.None);

            var durationAttrName = _nodeConfig.GetParamString("duration_attr", "");
            durationAttrName = EditorGUILayout.TextField("持续时间属性名", durationAttrName);
            _nodeConfig.SetParam("duration_attr", durationAttrName);
            EditorGUILayout.HelpBox("填写 AttributeType 枚举名称，如 SkillDuration。可与 duration 固定值配合使用。", MessageType.None);

            var timesPerSecondAttrName = _nodeConfig.GetParamString("times_per_second_attr", "");
            timesPerSecondAttrName = EditorGUILayout.TextField("每秒次数属性名", timesPerSecondAttrName);
            _nodeConfig.SetParam("times_per_second_attr", timesPerSecondAttrName);
            EditorGUILayout.HelpBox("填写 AttributeType 枚举名称。可与 times_per_second 固定值配合使用。", MessageType.None);

            var durationLoopCountAttrName = _nodeConfig.GetParamString("duration_loop_count_attr", "");
            durationLoopCountAttrName = EditorGUILayout.TextField("固定总次数属性名", durationLoopCountAttrName);
            _nodeConfig.SetParam("duration_loop_count_attr", durationLoopCountAttrName);
            EditorGUILayout.HelpBox("填写 AttributeType 枚举名称。可与 duration_loop_count 固定值配合使用。", MessageType.None);

            var durationScaleByCombo = EditorGUILayout.Toggle(
                "持续时间按Combo倍率缩放",
                _nodeConfig.GetParamBool("duration_scale_by_combo", false));
            _nodeConfig.SetParam("duration_scale_by_combo", durationScaleByCombo.ToString());
            EditorGUILayout.HelpBox(
                "开启后：最终 duration = duration × 当前Combo层数（1~6）。\n" +
                "例：duration=0.5、interval=0.1、Combo×2 → 总时长 1.0s，循环 10 次。",
                MessageType.None);

            EditorGUILayout.Space(5);

            EditorGUILayout.HelpBox(
                "Loop 节点用于循环执行子节点。\n" +
                "- count / interval: 直接指定循环次数与间隔\n" +
                "- duration + times_per_second: 固定总时长，按每秒次数自动分配\n" +
                "- duration + duration_loop_count: 固定总时长与总次数，自动计算间隔\n" +
                "- duration + interval: 固定总时长，按间隔自动计算次数\n" +
                "- *_attr: 对应参数可从技能卡片属性读取\n" +
                "节点输出端口：\n" +
                "  - body: 连接需要循环执行的子节点\n" +
                "  - exec_out: 循环结束后继续执行\n" +
                "配合 Fork 可实现多重斩、爆炎斩等效果",
                MessageType.Info);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("快速预设", EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("2连斩"))
            {
                _nodeConfig.SetParam("count", "2");
                _nodeConfig.SetParam("interval", "0.1");
            }
            if (GUILayout.Button("3连斩"))
            {
                _nodeConfig.SetParam("count", "3");
                _nodeConfig.SetParam("interval", "0.1");
            }
            if (GUILayout.Button("8连斩(爆炎)"))
            {
                _nodeConfig.SetParam("count", "8");
                _nodeConfig.SetParam("interval", "0.2");
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawLoopSequentialParams()
        {
            EditorGUILayout.LabelField("顺序循环执行配置", EditorStyles.miniLabel);

            var count = EditorGUILayout.IntField("循环次数", _nodeConfig.GetParamInt("count", 3));
            _nodeConfig.SetParam("count", count.ToString());

            var interval = EditorGUILayout.FloatField("间隔时间(秒)", _nodeConfig.GetParamFloat("interval", 0.2f));
            _nodeConfig.SetParam("interval", interval.ToString());

            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField("固定时长分配模式（可选）", EditorStyles.miniLabel);

            var duration = EditorGUILayout.FloatField("持续时间(秒)", _nodeConfig.GetParamFloat("duration", 0f));
            _nodeConfig.SetParam("duration", duration.ToString());

            var timesPerSecond = EditorGUILayout.FloatField("每秒次数", _nodeConfig.GetParamFloat("times_per_second", 0f));
            _nodeConfig.SetParam("times_per_second", timesPerSecond.ToString());
            EditorGUILayout.HelpBox(
                "同时配置 duration 与 times_per_second 时，会按固定时长均匀分配循环：\n" +
                "count = duration × times_per_second，interval = 1 / times_per_second。\n" +
                "例：duration=3、times_per_second=2 → 3 秒内触发 6 次，间隔 0.5 秒。",
                MessageType.None);

            var durationLoopCount = EditorGUILayout.IntField("固定总次数", _nodeConfig.GetParamInt("duration_loop_count", 0));
            _nodeConfig.SetParam("duration_loop_count", durationLoopCount.ToString());
            EditorGUILayout.HelpBox(
                "同时配置 duration 与 duration_loop_count 时，会按固定时长均匀分配循环：\n" +
                "count = duration_loop_count，interval = duration / duration_loop_count。\n" +
                "例：duration=2、duration_loop_count=5 → 2 秒内触发 5 次，间隔 0.4 秒。",
                MessageType.None);

            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField("从技能卡片属性读取（可选）", EditorStyles.miniLabel);

            var countAttrName = _nodeConfig.GetParamString("count_attr", "");
            countAttrName = EditorGUILayout.TextField("循环次数属性名", countAttrName);
            _nodeConfig.SetParam("count_attr", countAttrName);
            EditorGUILayout.HelpBox("填写 AttributeType 枚举名称，如 SkillContinuousAtkNum。留空则使用固定值。", MessageType.None);

            var intervalAttrName = _nodeConfig.GetParamString("interval_attr", "");
            intervalAttrName = EditorGUILayout.TextField("间隔时间属性名", intervalAttrName);
            _nodeConfig.SetParam("interval_attr", intervalAttrName);
            EditorGUILayout.HelpBox("填写 AttributeType 枚举名称，如 SkillContinuousAtkSpeed。留空则使用固定值。", MessageType.None);

            var durationAttrName = _nodeConfig.GetParamString("duration_attr", "");
            durationAttrName = EditorGUILayout.TextField("持续时间属性名", durationAttrName);
            _nodeConfig.SetParam("duration_attr", durationAttrName);
            EditorGUILayout.HelpBox("填写 AttributeType 枚举名称，如 SkillDuration。可与 duration 固定值配合使用。", MessageType.None);

            var timesPerSecondAttrName = _nodeConfig.GetParamString("times_per_second_attr", "");
            timesPerSecondAttrName = EditorGUILayout.TextField("每秒次数属性名", timesPerSecondAttrName);
            _nodeConfig.SetParam("times_per_second_attr", timesPerSecondAttrName);
            EditorGUILayout.HelpBox("填写 AttributeType 枚举名称。可与 times_per_second 固定值配合使用。", MessageType.None);

            var durationLoopCountAttrName = _nodeConfig.GetParamString("duration_loop_count_attr", "");
            durationLoopCountAttrName = EditorGUILayout.TextField("固定总次数属性名", durationLoopCountAttrName);
            _nodeConfig.SetParam("duration_loop_count_attr", durationLoopCountAttrName);
            EditorGUILayout.HelpBox("填写 AttributeType 枚举名称。可与 duration_loop_count 固定值配合使用。", MessageType.None);

            var durationScaleByCombo = EditorGUILayout.Toggle(
                "持续时间按Combo倍率缩放",
                _nodeConfig.GetParamBool("duration_scale_by_combo", false));
            _nodeConfig.SetParam("duration_scale_by_combo", durationScaleByCombo.ToString());
            EditorGUILayout.HelpBox(
                "开启后：最终 duration = duration × 当前Combo层数（1~6）。\n" +
                "例：duration=0.5、interval=0.1、Combo×2 → 总时长 1.0s，循环 10 次。",
                MessageType.None);

            EditorGUILayout.Space(5);

            EditorGUILayout.HelpBox(
                "LoopSequential 节点用于顺序循环执行子节点（每次循环按顺序逐个执行）。\n" +
                "- count / interval: 直接指定循环次数与间隔\n" +
                "- duration + times_per_second: 固定总时长，按每秒次数自动分配\n" +
                "- duration + duration_loop_count: 固定总时长与总次数，自动计算间隔\n" +
                "- duration + interval: 固定总时长，按间隔自动计算次数\n" +
                "- *_attr: 对应参数可从技能卡片属性读取\n" +
                "节点输出端口：\n" +
                "  - body: 连接需要循环执行的子节点（顺序执行）\n" +
                "  - exec_out: 循环结束后继续执行\n" +
                "与 Loop 的区别：Loop 为并行执行，LoopSequential 为顺序执行",
                MessageType.Info);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("快速预设", EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("2连"))
            {
                _nodeConfig.SetParam("count", "2");
                _nodeConfig.SetParam("interval", "0.3");
            }
            if (GUILayout.Button("3连"))
            {
                _nodeConfig.SetParam("count", "3");
                _nodeConfig.SetParam("interval", "0.3");
            }
            if (GUILayout.Button("5连"))
            {
                _nodeConfig.SetParam("count", "5");
                _nodeConfig.SetParam("interval", "0.3");
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawForkParams()
        {
            EditorGUILayout.LabelField("分叉执行配置", EditorStyles.miniLabel);

            var count = EditorGUILayout.IntField("分叉数量", _nodeConfig.GetParamInt("count", 3));
            _nodeConfig.SetParam("count", count.ToString());

            EditorGUILayout.Space(5);

            EditorGUILayout.HelpBox(
                "Fork 节点用于同时执行多个并行分支。\n" +
                "- count: 分叉数量（决定 fork_0 ~ fork_N 端口数量）\n" +
                "节点输出端口：fork_0, fork_1, ... fork_N\n" +
                "所有分叉会并行执行（Task.WhenAll）\n" +
                "配合 Loop 可实现爆炎斩：Loop(8) -> Fork(3) -> Slash",
                MessageType.Info);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("快速预设", EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("2路分叉"))
            {
                _nodeConfig.SetParam("count", "2");
            }
            if (GUILayout.Button("3路分叉"))
            {
                _nodeConfig.SetParam("count", "3");
            }
            if (GUILayout.Button("5路分叉"))
            {
                _nodeConfig.SetParam("count", "5");
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawGenericParams()
        {
            EditorGUILayout.HelpBox("使用通用参数编辑器", MessageType.Info);

            // 显示所有参数
            var keys = GetAllParamKeys();
            foreach (var key in keys)
            {
                var value = EditorGUILayout.TextField(key, _nodeConfig.GetParamString(key, ""));
                _nodeConfig.SetParam(key, value);
            }
        }

        private List<string> GetAllParamKeys()
        {
            var keys = new List<string>();
            var fields = _paramsProperty;

            if (fields != null && fields.isArray)
            {
                // 遍历字典键
            }
            return keys;
        }

        private void DrawPresetButtons()
        {
            EditorGUILayout.LabelField("快速预设", EditorStyles.miniLabel);

            EditorGUILayout.BeginHorizontal();

            switch (_selectedNodeType)
            {
                case "AoeDamage":
                    if (GUILayout.Button("近战攻击"))
                    {
                        SetAoePreset("Circle", 1.5f, 50f, "Physical");
                    }
                    if (GUILayout.Button("范围爆炸"))
                    {
                        SetAoePreset("Circle", 3f, 100f, "Magic");
                    }
                    break;

                case "Effect":
                    if (GUILayout.Button("爆炸特效"))
                    {
                        _nodeConfig.SetParam("vfx", "VFX/Explosion");
                        _nodeConfig.SetParam("sfx", "Audio/Explosion");
                    }
                    if (GUILayout.Button("治疗特效"))
                    {
                        _nodeConfig.SetParam("vfx", "VFX/Heal");
                        _nodeConfig.SetParam("sfx", "Audio/Heal");
                    }
                    break;

                case "AnimatedEffect":
                    if (GUILayout.Button("陨石坠落"))
                    {
                        _nodeConfig.SetParam("vfx", "VFX/MeteorFall");
                        _nodeConfig.SetParam("crater_vfx", "VFX/MeteorCrater");
                        _nodeConfig.SetParam("animation_type", "Fall");
                        _nodeConfig.SetParam("random_offset_radius", "0");
                    }
                    if (GUILayout.Button("能量上升"))
                    {
                        _nodeConfig.SetParam("vfx", "VFX/EnergyRise");
                        _nodeConfig.SetParam("crater_vfx", "");
                        _nodeConfig.SetParam("animation_type", "Rise");
                        _nodeConfig.SetParam("random_offset_radius", "0");
                    }
                    break;

                case "ApplyBuff":
                    if (GUILayout.Button("灼烧 (Burn)"))
                    {
                        SetBuffPreset((int)global::Config.BuffType.Burn, 3f, 50f, 0.5f, 1f);
                    }
                    if (GUILayout.Button("冻结 (Vertigo)"))
                    {
                        SetBuffPreset((int)global::Config.BuffType.Vertigo, 2f, 0f, 1f);
                    }
                    if (GUILayout.Button("无敌 (Invincible)"))
                    {
                        SetBuffPreset((int)global::Config.BuffType.Invincible, 5f, 0f, 1f);
                    }
                    if (GUILayout.Button("加速 (Accelerate)"))
                    {
                        SetBuffPreset((int)global::Config.BuffType.Accelerate, -1f, 30f, 1f);
                    }
                    if (GUILayout.Button("虚弱 (Weak)"))
                    {
                        SetBuffPreset((int)global::Config.BuffType.Weak, 10f, 20f, 1f);
                    }
                    break;

                case "Projectile":
                    if (GUILayout.Button("普通弹"))
                    {
                        _nodeConfig.SetParam("move_type", "Straight");
                        _nodeConfig.SetParam("hit_modules", "");
                        _nodeConfig.SetParam("speed", "15");
                        _nodeConfig.SetParam("max_distance", "20");
                    }
                    if (GUILayout.Button("追踪弹"))
                    {
                        _nodeConfig.SetParam("move_type", "Homing");
                        _nodeConfig.SetParam("hit_modules", "");
                        _nodeConfig.SetParam("speed", "12");
                        _nodeConfig.SetParam("max_distance", "15");
                    }
                    if (GUILayout.Button("穿透弹"))
                    {
                        _nodeConfig.SetParam("move_type", "Straight");
                        _nodeConfig.SetParam("hit_modules", "Penetrate|3:0:0:0");
                        _nodeConfig.SetParam("speed", "20");
                        _nodeConfig.SetParam("max_distance", "25");
                    }
                    if (GUILayout.Button("反弹弹"))
                    {
                        _nodeConfig.SetParam("move_type", "Bounce");
                        _nodeConfig.SetParam("hit_modules", "");
                        _nodeConfig.SetParam("bounce_count", "3");
                        _nodeConfig.SetParam("speed", "15");
                        _nodeConfig.SetParam("max_distance", "20");
                    }
                    if (GUILayout.Button("链式弹"))
                    {
                        _nodeConfig.SetParam("move_type", "Straight");
                        _nodeConfig.SetParam("hit_modules", "Chain|2:5:0:0");
                        _nodeConfig.SetParam("speed", "15");
                        _nodeConfig.SetParam("max_distance", "20");
                    }
                    if (GUILayout.Button("爆炸弹"))
                    {
                        _nodeConfig.SetParam("move_type", "Straight");
                        _nodeConfig.SetParam("hit_modules", "Explode|0:3:0:0");
                        _nodeConfig.SetParam("speed", "12");
                        _nodeConfig.SetParam("max_distance", "20");
                    }
                    if (GUILayout.Button("激光扫描弹"))
                    {
                        _nodeConfig.SetParam("move_type", "LaserSweep");
                        _nodeConfig.SetParam("hit_modules", "");
                        _nodeConfig.SetParam("speed", "10");
                        _nodeConfig.SetParam("sweep_angle", "50");
                        _nodeConfig.SetParam("sweep_duration", "5");
                        _nodeConfig.SetParam("max_distance", "20");
                    }
                    break;
            }

            EditorGUILayout.EndHorizontal();
        }

        private void SetAoePreset(string shape, float range, float damage, string damageType)
        {
            _nodeConfig.SetParam("shape", shape);
            _nodeConfig.SetParam("range", range.ToString());
            _nodeConfig.SetParam("damage", damage.ToString());
            _nodeConfig.SetParam("damage_type", damageType);
        }

        private void SetBuffPreset(int buffId, float duration, float effectValue, float probability, float tickInterval = 0f)
        {
            _nodeConfig.SetParam("buff_id", buffId.ToString());
            _nodeConfig.SetParam("duration", duration.ToString());
            _nodeConfig.SetParam("effect_value", effectValue.ToString());
            _nodeConfig.SetParam("probability", probability.ToString());
            _nodeConfig.SetParam("tick_interval", tickInterval.ToString());
        }

        private void DrawMultiProjectileParams()
        {
            EditorGUILayout.LabelField("基本配置", EditorStyles.miniLabel);

            DrawAssetSelector("预制体", "prefab", "t:GameObject");
            DrawAssetSelector("命中特效", "hit_effect", "t:GameObject");
            DrawAssetSelector("结束特效", "end_effect", "t:GameObject");
            EditorGUILayout.HelpBox("结束特效在投射物消失时播放", MessageType.None);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("循环配置", EditorStyles.miniLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("循环次数", GUILayout.Width(130));
            var loopCount = EditorGUILayout.IntField(_nodeConfig.GetParamInt("loop_count", 1));
            _nodeConfig.SetParam("loop_count", loopCount.ToString());
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox("循环次数，偏移列表会按此次数重复执行", MessageType.None);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("间隔时间(s)", GUILayout.Width(130));
            var interval = EditorGUILayout.FloatField(_nodeConfig.GetParamFloat("interval", 0.1f));
            _nodeConfig.SetParam("interval", interval.ToString());
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox("每个投射物发射的间隔时间", MessageType.None);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("偏移配置", EditorStyles.miniLabel);

            // 配表键选择
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("配表键名", GUILayout.Width(130));
            string[] configOptions = { "", "SkillSwiftAssaultOffset" };
            string[] configLabels = { "(不使用)", "SkillSwiftAssaultOffset" };
            var currentConfigKey = _nodeConfig.GetParamString("offset_config_key", "");
            int configIdx = Array.IndexOf(configOptions, currentConfigKey);
            if (configIdx < 0) configIdx = 0;
            int newConfigIdx = EditorGUILayout.Popup(configIdx, configLabels);
            if (newConfigIdx != configIdx)
            {
                _nodeConfig.SetParam("offset_config_key", configOptions[newConfigIdx]);
            }
            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(currentConfigKey))
            {
                EditorGUILayout.HelpBox($"从配表 {currentConfigKey} 读取偏移数据，配表修改后自动生效", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("格式: X,Y,角度,翻转; X,Y,角度,翻转\n例如: 3.5,-5,-36,1; -6,0,0,-1", MessageType.Info);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("偏移列表", GUILayout.Width(130));
                var offsetList = EditorGUILayout.TextField(_nodeConfig.GetParamString("offset_list", ""));
                if (offsetList != _nodeConfig.GetParamString("offset_list", ""))
                {
                    _nodeConfig.SetParam("offset_list", offsetList);
                }
                EditorGUILayout.EndHorizontal();
            }

            // 预览偏移
            var offsets = _nodeConfig.GetParamString("offset_list", "");
            if (!string.IsNullOrEmpty(offsets))
            {
                var entries = offsets.Split(';', StringSplitOptions.RemoveEmptyEntries);
                int.TryParse(_nodeConfig.GetParamString("loop_count", "1"), out int lc);
                EditorGUILayout.LabelField($"共 {entries.Length} 个偏移点 × {lc} 次 = {entries.Length * lc} 个投射物", EditorStyles.miniLabel);
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("发射位置", EditorStyles.miniLabel);

            // 生成位置类型选择
            string[] spawnTypeOptions = { "direction", "caster", "target", "fixed" };
            string[] spawnTypeLabels = { "方向位置(默认)", "施法者位置", "目标位置", "固定位置" };
            var currentSpawnType = _nodeConfig.GetParamString("spawn_type", "direction");
            int spawnTypeIdx = Array.IndexOf(spawnTypeOptions, currentSpawnType);
            if (spawnTypeIdx < 0) spawnTypeIdx = 0;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("生成位置类型", GUILayout.Width(130));
            int newSpawnTypeIdx = EditorGUILayout.Popup(spawnTypeIdx, spawnTypeLabels);
            if (newSpawnTypeIdx != spawnTypeIdx)
            {
                _nodeConfig.SetParam("spawn_type", spawnTypeOptions[newSpawnTypeIdx]);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox("方向位置(默认)：从拖拽释放位置偏移，与旧版 SwiftAssaultSkill 一致。\n施法者位置：从施法者位置偏移。\n目标位置：从目标当前位置偏移。\n固定位置：使用下方固定X/Y坐标。", MessageType.None);

            var multiSpawnOffsetX = EditorGUILayout.FloatField("生成偏移 X", _nodeConfig.GetParamFloat("spawn_offset_x", 0f));
            _nodeConfig.SetParam("spawn_offset_x", multiSpawnOffsetX.ToString("F2"));
            var multiSpawnOffsetY = EditorGUILayout.FloatField("生成偏移 Y", _nodeConfig.GetParamFloat("spawn_offset_y", 0f));
            _nodeConfig.SetParam("spawn_offset_y", multiSpawnOffsetY.ToString("F2"));
            EditorGUILayout.HelpBox("相对「生成位置类型」基准点的世界坐标偏移（米）。瞄准点仍为技能目标位置。", MessageType.None);

            var fixedStartX = _nodeConfig.GetParamFloat("fixed_start_x", -999f);
            var fixedStartY = _nodeConfig.GetParamFloat("fixed_start_y", -999f);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("固定X坐标", GUILayout.Width(130));
            fixedStartX = EditorGUILayout.FloatField(fixedStartX);
            _nodeConfig.SetParam("fixed_start_x", fixedStartX.ToString("F1"));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("固定Y坐标", GUILayout.Width(130));
            fixedStartY = EditorGUILayout.FloatField(fixedStartY);
            _nodeConfig.SetParam("fixed_start_y", fixedStartY.ToString("F1"));
            EditorGUILayout.EndHorizontal();

            bool hasFixedStart = fixedStartX > -900f || fixedStartY > -900f;
            EditorGUILayout.HelpBox(hasFixedStart
                ? $"固定发射点: ({fixedStartX:F1}, {fixedStartY:F1})"
                : "使用默认：基准位置 + 偏移", MessageType.None);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("命中检测", EditorStyles.miniLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("最大穿透数", GUILayout.Width(130));
            var maxPenetrate = EditorGUILayout.IntField(_nodeConfig.GetParamInt("max_penetrate", 0));
            _nodeConfig.SetParam("max_penetrate", maxPenetrate.ToString());
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox("穿透次数：0表示无限穿透，大于0表示最多命中目标数。碰撞检测由预制体上的 BulletDamageDetector 负责。", MessageType.None);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("命中间隔(s)", GUILayout.Width(130));
            var hitTargetDuration = EditorGUILayout.FloatField(_nodeConfig.GetParamFloat("hit_target_duration", 0f));
            _nodeConfig.SetParam("hit_target_duration", hitTargetDuration.ToString());
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox("对同一目标重复命名的间隔时间(秒)，0表示每个目标只能命中一次", MessageType.None);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("快速预设", EditorStyles.miniLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("瞬袭斩(配表)"))
            {
                _nodeConfig.SetParam("offset_config_key", "SkillSwiftAssaultOffset");
                _nodeConfig.SetParam("loop_count", "3");
                _nodeConfig.SetParam("interval", "0.1");
                _nodeConfig.SetParam("max_penetrate", "0");
            }
            if (GUILayout.Button("十字射击"))
            {
                _nodeConfig.SetParam("offset_config_key", "");
                _nodeConfig.SetParam("offset_list", "0,3,90,1; 0,-3,270,1; 3,0,0,1; -3,0,180,1");
                _nodeConfig.SetParam("loop_count", "1");
                _nodeConfig.SetParam("interval", "0.05");
                _nodeConfig.SetParam("max_penetrate", "0");
            }
            if (GUILayout.Button("扇形发射"))
            {
                _nodeConfig.SetParam("offset_config_key", "");
                _nodeConfig.SetParam("offset_list", "-1.5,0,0,1; -0.75,0,22.5,1; 0,0,45,1; 0.75,0,67.5,1; 1.5,0,90,1");
                _nodeConfig.SetParam("loop_count", "1");
                _nodeConfig.SetParam("interval", "0.02");
                _nodeConfig.SetParam("max_penetrate", "0");
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawMultiTargetProjectileParams()
        {
            EditorGUILayout.LabelField("多目标配置", EditorStyles.miniLabel);

            var targetCount = EditorGUILayout.IntField("目标数量", _nodeConfig.GetParamInt("target_count", 3));
            _nodeConfig.SetParam("target_count", targetCount.ToString());

            var targetCountAttr = EditorGUILayout.TextField("目标数量属性名", _nodeConfig.GetParamString("target_count_attr", ""));
            _nodeConfig.SetParam("target_count_attr", targetCountAttr);
            EditorGUILayout.HelpBox("填写 AttributeType 枚举名称。配置后优先于上方固定「目标数量」。", MessageType.None);

            var includePrimaryTarget = EditorGUILayout.Toggle("优先包含主目标",
                _nodeConfig.GetParamBool("include_primary_target", true));
            _nodeConfig.SetParam("include_primary_target", includePrimaryTarget.ToString().ToLower());

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("投射物配置", EditorStyles.miniLabel);

            DrawAssetSelector("预制体路径（可选）:", "prefab", "Prefab");
            DrawAssetSelector("命中特效路径:", "hit_effect", "Prefab");
            DrawAssetSelector("结束特效路径:", "end_effect", "Prefab");

            var speed = EditorGUILayout.FloatField("飞行速度(米/秒)", _nodeConfig.GetParamFloat("speed", 10f));
            _nodeConfig.SetParam("speed", speed.ToString());

            var speedAttrName = EditorGUILayout.TextField("速度属性名", _nodeConfig.GetParamString("speed_attr", ""));
            _nodeConfig.SetParam("speed_attr", speedAttrName);

            var maxTime = EditorGUILayout.FloatField("最大时间(秒)", _nodeConfig.GetParamFloat("max_time", 10f));
            _nodeConfig.SetParam("max_time", maxTime.ToString());

            var maxTimeAttrName = EditorGUILayout.TextField("最大时间属性名", _nodeConfig.GetParamString("max_time_attr", ""));
            _nodeConfig.SetParam("max_time_attr", maxTimeAttrName);

            var bulletLength = EditorGUILayout.FloatField("子弹长度(米)", _nodeConfig.GetParamFloat("bullet_length", 1f));
            _nodeConfig.SetParam("bullet_length", bulletLength.ToString());

            var bulletLengthAttrName = EditorGUILayout.TextField("子弹长度属性名", _nodeConfig.GetParamString("bullet_length_attr", ""));
            _nodeConfig.SetParam("bullet_length_attr", bulletLengthAttrName);

            var bulletWidth = EditorGUILayout.FloatField("子弹宽度(米)", _nodeConfig.GetParamFloat("bullet_width", 1f));
            _nodeConfig.SetParam("bullet_width", bulletWidth.ToString());

            var bulletWidthAttrName = EditorGUILayout.TextField("子弹宽度属性名", _nodeConfig.GetParamString("bullet_width_attr", ""));
            _nodeConfig.SetParam("bullet_width_attr", bulletWidthAttrName);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("移动方式", EditorStyles.boldLabel);
            DrawMoveTypeSelector();
            DrawProjectileBurnAreaParams();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("命中模块 (HitModule)", EditorStyles.boldLabel);

            var buffHitProbability = EditorGUILayout.Slider("Buff命中概率", _nodeConfig.GetParamFloat("buff_hit_probability", 1f), 0f, 1f);
            _nodeConfig.SetParam("buff_hit_probability", buffHitProbability.ToString());

            var buffHitProbabilityAttr = _nodeConfig.GetParamString("buff_hit_probability_attr", "");
            buffHitProbabilityAttr = EditorGUILayout.TextField("Buff命中概率属性名", buffHitProbabilityAttr);
            _nodeConfig.SetParam("buff_hit_probability_attr", buffHitProbabilityAttr);

            DrawHitModulesEditor();

            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox(
                "MultiTargetProjectile 同时向多个最近敌人各发射一枚弹道。\n" +
                "- target_count: 固定发射数量（target_count_attr 未配置时使用）\n" +
                "- target_count_attr: 配置后优先从技能属性读取数量\n" +
                "- include_primary_target: 是否优先把技能主目标算作第一发\n" +
                "- move_type: 移动方式（Homing 时各弹道锁定各自目标）\n" +
                "- 其余敌人按距离从近到远选取，同一轮不重复",
                MessageType.Info);
        }

        private void DrawWaveProjectileParams()
        {
            EditorGUILayout.LabelField("基本配置", EditorStyles.miniLabel);

            DrawAssetSelector("预制体", "prefab", "t:GameObject");
            DrawAssetSelector("命中特效", "hit_effect", "t:GameObject");
            DrawAssetSelector("结束特效", "end_effect", "t:GameObject");
            EditorGUILayout.HelpBox("结束特效在投射物消失时播放", MessageType.None);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("波纹配置", EditorStyles.miniLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("分叉数量", GUILayout.Width(130));
            var forkCount = EditorGUILayout.IntField(_nodeConfig.GetParamInt("fork_count", 1));
            _nodeConfig.SetParam("fork_count", forkCount.ToString());
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox("分叉数量：1=正中，2=+左，3=+右（左右各一个）", MessageType.None);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("总轮数", GUILayout.Width(130));
            var totalRounds = EditorGUILayout.IntField(_nodeConfig.GetParamInt("total_rounds", 5));
            _nodeConfig.SetParam("total_rounds", totalRounds.ToString());
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox("总共打几轮，每轮沿方向往前推进一次", MessageType.None);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("每轮推进距离", GUILayout.Width(130));
            var forwardStep = EditorGUILayout.FloatField(_nodeConfig.GetParamFloat("forward_step", 2f));
            _nodeConfig.SetParam("forward_step", forwardStep.ToString());
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox("每轮往前推进的距离（米）", MessageType.None);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("分叉角度", GUILayout.Width(130));
            var forkAngle = EditorGUILayout.FloatField(_nodeConfig.GetParamFloat("fork_angle", 15f));
            _nodeConfig.SetParam("fork_angle", forkAngle.ToString());
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox("左右分叉的角度（度）", MessageType.None);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("每轮间隔(s)", GUILayout.Width(130));
            var roundInterval = EditorGUILayout.FloatField(_nodeConfig.GetParamFloat("round_interval", 0.2f));
            _nodeConfig.SetParam("round_interval", roundInterval.ToString());
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox("每轮发射的间隔时间（秒）", MessageType.None);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("外观", EditorStyles.miniLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("缩放(%)", GUILayout.Width(130));
            var scale = EditorGUILayout.FloatField(_nodeConfig.GetParamFloat("scale", 100f));
            _nodeConfig.SetParam("scale", scale.ToString());
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("命中检测", EditorStyles.miniLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("最大穿透数", GUILayout.Width(130));
            var maxPenetrate = EditorGUILayout.IntField(_nodeConfig.GetParamInt("max_penetrate", 0));
            _nodeConfig.SetParam("max_penetrate", maxPenetrate.ToString());
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("命中间隔(s)", GUILayout.Width(130));
            var hitTargetDuration = EditorGUILayout.FloatField(_nodeConfig.GetParamFloat("hit_target_duration", 0f));
            _nodeConfig.SetParam("hit_target_duration", hitTargetDuration.ToString());
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("快速预设", EditorStyles.miniLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("爆炎斩预设"))
            {
                _nodeConfig.SetParam("fork_count", "3");
                _nodeConfig.SetParam("total_rounds", "8");
                _nodeConfig.SetParam("forward_step", "2");
                _nodeConfig.SetParam("fork_angle", "15");
                _nodeConfig.SetParam("round_interval", "0.2");
                _nodeConfig.SetParam("scale", "100");
                _nodeConfig.SetParam("spawn_type", "direction");
                _nodeConfig.SetParam("max_penetrate", "0");
            }
            if (GUILayout.Button("单路推进"))
            {
                _nodeConfig.SetParam("fork_count", "1");
                _nodeConfig.SetParam("total_rounds", "5");
                _nodeConfig.SetParam("forward_step", "3");
                _nodeConfig.SetParam("fork_angle", "0");
                _nodeConfig.SetParam("round_interval", "0.15");
                _nodeConfig.SetParam("scale", "100");
                _nodeConfig.SetParam("spawn_type", "direction");
                _nodeConfig.SetParam("max_penetrate", "0");
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox(
                "WaveProjectile 波纹投射节点：\n" +
                "类似爆炎斩效果，沿拖拽方向每轮往前推进生成投射物。\n" +
                "- fork_count: 分叉数量（1/2/3）\n" +
                "- total_rounds: 总轮数\n" +
                "- forward_step: 每轮推进距离（米）\n" +
                "- fork_angle: 分叉角度（度）\n" +
                "- round_interval: 每轮间隔（秒）\n" +
                "- scale: 投射物缩放（%）",
                MessageType.Info);
        }

        /// <summary>
        /// 扇形投射节点参数
        /// </summary>
        private void DrawFanProjectileParams()
        {
            EditorGUILayout.LabelField("基本配置", EditorStyles.miniLabel);

            DrawAssetSelector("预制体", "prefab", "t:GameObject");
            DrawAssetSelector("命中特效", "hit_effect", "t:GameObject");
            DrawAssetSelector("结束特效", "end_effect", "t:GameObject");

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("扇形配置", EditorStyles.miniLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("扇形角度(°)", GUILayout.Width(130));
            var fanAngle = EditorGUILayout.FloatField(_nodeConfig.GetParamFloat("fan_angle", 90f));
            _nodeConfig.SetParam("fan_angle", fanAngle.ToString());
            EditorGUILayout.EndHorizontal();

            var fanAngleAttrName = _nodeConfig.GetParamString("fan_angle_attr", "");
            fanAngleAttrName = EditorGUILayout.TextField("扇形角度属性名", fanAngleAttrName);
            _nodeConfig.SetParam("fan_angle_attr", fanAngleAttrName);
            EditorGUILayout.HelpBox("填写 AttributeType 枚举名称，如 SkillSalvoAngle。留空则使用固定值。", MessageType.None);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("投射物数量", GUILayout.Width(130));
            var projectileCount = EditorGUILayout.IntField(_nodeConfig.GetParamInt("projectile_count", 5));
            _nodeConfig.SetParam("projectile_count", projectileCount.ToString());
            EditorGUILayout.EndHorizontal();

            var projectileCountAttrName = _nodeConfig.GetParamString("projectile_count_attr", "");
            projectileCountAttrName = EditorGUILayout.TextField("投射物数量属性名", projectileCountAttrName);
            _nodeConfig.SetParam("projectile_count_attr", projectileCountAttrName);
            EditorGUILayout.HelpBox("填写 AttributeType 枚举名称，如 SkillSalvosNum。留空则使用固定值。", MessageType.None);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("总轮数", GUILayout.Width(130));
            var totalRounds = EditorGUILayout.IntField(_nodeConfig.GetParamInt("total_rounds", 1));
            _nodeConfig.SetParam("total_rounds", totalRounds.ToString());
            EditorGUILayout.EndHorizontal();

            var totalRoundsAttrName = _nodeConfig.GetParamString("total_rounds_attr", "");
            totalRoundsAttrName = EditorGUILayout.TextField("总轮数属性名", totalRoundsAttrName);
            _nodeConfig.SetParam("total_rounds_attr", totalRoundsAttrName);
            EditorGUILayout.HelpBox("填写 AttributeType 枚举名称。留空则使用固定值。", MessageType.None);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("每轮间隔(s)", GUILayout.Width(130));
            var roundInterval = EditorGUILayout.FloatField(_nodeConfig.GetParamFloat("round_interval", 0.1f));
            _nodeConfig.SetParam("round_interval", roundInterval.ToString());
            EditorGUILayout.EndHorizontal();

            var roundIntervalAttrName = _nodeConfig.GetParamString("round_interval_attr", "");
            roundIntervalAttrName = EditorGUILayout.TextField("每轮间隔属性名", roundIntervalAttrName);
            _nodeConfig.SetParam("round_interval_attr", roundIntervalAttrName);
            EditorGUILayout.HelpBox("填写 AttributeType 枚举名称。留空则使用固定值。", MessageType.None);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("瞄准", EditorStyles.miniLabel);
            DrawProjectileAimTargetSelector();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("投射物组件配置", EditorStyles.boldLabel);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("移动方式", EditorStyles.miniLabel);

            DrawMoveTypeSelector();

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("命中模块 (HitModule)", EditorStyles.miniLabel);

            var buffHitProbability = EditorGUILayout.Slider("Buff命中概率", _nodeConfig.GetParamFloat("buff_hit_probability", 1f), 0f, 1f);
            _nodeConfig.SetParam("buff_hit_probability", buffHitProbability.ToString());

            var buffHitProbabilityAttr = _nodeConfig.GetParamString("buff_hit_probability_attr", "");
            buffHitProbabilityAttr = EditorGUILayout.TextField("Buff命中概率属性名", buffHitProbabilityAttr);
            _nodeConfig.SetParam("buff_hit_probability_attr", buffHitProbabilityAttr);
            EditorGUILayout.HelpBox("仅对 AttachBuff 命中模块生效。填写 AttributeType 枚举名称，留空则使用固定值。", MessageType.None);

            DrawHitModulesEditor();

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("外观与时间", EditorStyles.miniLabel);

            var speed = EditorGUILayout.FloatField("速度(米/秒)", _nodeConfig.GetParamFloat("speed", 10f));
            _nodeConfig.SetParam("speed", speed.ToString());

            var speedAttrName = _nodeConfig.GetParamString("speed_attr", "");
            speedAttrName = EditorGUILayout.TextField("速度属性名", speedAttrName);
            _nodeConfig.SetParam("speed_attr", speedAttrName);
            EditorGUILayout.HelpBox("填写 AttributeType 枚举名称。留空则使用固定值。", MessageType.None);

            var maxTime = EditorGUILayout.FloatField("最大存在时间(秒)", _nodeConfig.GetParamFloat("max_time", 10f));
            _nodeConfig.SetParam("max_time", maxTime.ToString());

            var maxTimeAttrName = _nodeConfig.GetParamString("max_time_attr", "");
            maxTimeAttrName = EditorGUILayout.TextField("最大时间属性名", maxTimeAttrName);
            _nodeConfig.SetParam("max_time_attr", maxTimeAttrName);
            EditorGUILayout.HelpBox("填写 AttributeType 枚举名称。留空则使用固定值。", MessageType.None);

            var bulletLength = EditorGUILayout.FloatField("子弹长度(米)", _nodeConfig.GetParamFloat("bullet_length", 1f));
            _nodeConfig.SetParam("bullet_length", bulletLength.ToString());
            EditorGUILayout.HelpBox("沿子弹飞行方向（Y轴）的缩放倍数。会和角色 BulletSizeCorrection 相乘。", MessageType.None);

            var bulletLengthAttrName = _nodeConfig.GetParamString("bullet_length_attr", "");
            bulletLengthAttrName = EditorGUILayout.TextField("子弹长度属性名", bulletLengthAttrName);
            _nodeConfig.SetParam("bullet_length_attr", bulletLengthAttrName);
            EditorGUILayout.HelpBox("填写 AttributeType 枚举名称。留空则使用固定值。", MessageType.None);

            string[] rotationOptions = { "-1", "0", "90", "180", "270" };
            string[] rotationLabels = { "自动（朝向目标）", "向右(0°)", "向上(90°)", "向左(180°)", "向下(270°)" };
            var currentRotation = _nodeConfig.GetParamFloat("fixed_rotation", -1f).ToString("F0");
            int rotationIdx = Array.IndexOf(rotationOptions, currentRotation);
            if (rotationIdx < 0) rotationIdx = 0;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("固定朝向", GUILayout.Width(130));
            int newRotationIdx = EditorGUILayout.Popup(rotationIdx, rotationLabels);
            if (newRotationIdx != rotationIdx)
            {
                _nodeConfig.SetParam("fixed_rotation", rotationOptions[newRotationIdx]);
            }
            EditorGUILayout.EndHorizontal();

            var fixedRotationAttrName = _nodeConfig.GetParamString("fixed_rotation_attr", "");
            fixedRotationAttrName = EditorGUILayout.TextField("固定朝向属性名", fixedRotationAttrName);
            _nodeConfig.SetParam("fixed_rotation_attr", fixedRotationAttrName);
            EditorGUILayout.HelpBox("填写 AttributeType 枚举名称。留空则使用固定朝向。", MessageType.None);

            var scale = EditorGUILayout.FloatField("缩放(%)", _nodeConfig.GetParamFloat("scale", 100f));
            _nodeConfig.SetParam("scale", scale.ToString());

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("快速预设", EditorStyles.miniLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("扇形射击(5支/90°)"))
            {
                _nodeConfig.SetParam("fan_angle", "90");
                _nodeConfig.SetParam("projectile_count", "5");
                _nodeConfig.SetParam("total_rounds", "1");
                _nodeConfig.SetParam("round_interval", "0");
                _nodeConfig.SetParam("move_type", "Straight");
                _nodeConfig.SetParam("hit_modules", "Penetrate|3:0");
                _nodeConfig.SetParam("speed", "10");
                _nodeConfig.SetParam("max_time", "10");
                _nodeConfig.SetParam("bullet_length", "1");
                _nodeConfig.SetParam("fixed_rotation", "-1");
                _nodeConfig.SetParam("scale", "100");
            }
            if (GUILayout.Button("扇形射击(7支/120°)"))
            {
                _nodeConfig.SetParam("fan_angle", "120");
                _nodeConfig.SetParam("projectile_count", "7");
                _nodeConfig.SetParam("total_rounds", "1");
                _nodeConfig.SetParam("round_interval", "0");
                _nodeConfig.SetParam("move_type", "Straight");
                _nodeConfig.SetParam("hit_modules", "Penetrate|3:0");
                _nodeConfig.SetParam("speed", "10");
                _nodeConfig.SetParam("max_time", "10");
                _nodeConfig.SetParam("bullet_length", "1");
                _nodeConfig.SetParam("fixed_rotation", "-1");
                _nodeConfig.SetParam("scale", "100");
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("扇形散射(3轮/60°)"))
            {
                _nodeConfig.SetParam("fan_angle", "60");
                _nodeConfig.SetParam("projectile_count", "5");
                _nodeConfig.SetParam("total_rounds", "3");
                _nodeConfig.SetParam("round_interval", "0.2");
                _nodeConfig.SetParam("move_type", "Straight");
                _nodeConfig.SetParam("hit_modules", "Penetrate|3:0");
                _nodeConfig.SetParam("speed", "10");
                _nodeConfig.SetParam("max_time", "10");
                _nodeConfig.SetParam("bullet_length", "1");
                _nodeConfig.SetParam("fixed_rotation", "-1");
                _nodeConfig.SetParam("scale", "100");
            }
            if (GUILayout.Button("360°环绕"))
            {
                _nodeConfig.SetParam("fan_angle", "360");
                _nodeConfig.SetParam("projectile_count", "12");
                _nodeConfig.SetParam("total_rounds", "1");
                _nodeConfig.SetParam("round_interval", "0");
                _nodeConfig.SetParam("move_type", "Straight");
                _nodeConfig.SetParam("hit_modules", "Penetrate|3:0");
                _nodeConfig.SetParam("speed", "10");
                _nodeConfig.SetParam("max_time", "10");
                _nodeConfig.SetParam("bullet_length", "1");
                _nodeConfig.SetParam("fixed_rotation", "-1");
                _nodeConfig.SetParam("scale", "100");
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox(
                "FanProjectile 扇形投射节点：\n" +
                "从施法者位置向扇形范围内发射多个投射物。\n" +
                "- fan_angle: 扇形总角度（度）\n" +
                "- projectile_count: 投射物数量\n" +
                "- total_rounds: 总轮数\n" +
                "- round_interval: 每轮间隔（秒）\n" +
                "- move_type: 移动方式 (Straight/Bounce/Homing 等)\n" +
                "- buff_hit_probability: AttachBuff 的Buff命中概率\n" +
                "- buff_hit_probability_attr: 从技能卡片属性读取 Buff命中概率\n" +
                "- hit_modules: 命中模块（可视化编辑，点击 + 添加或快捷按钮）\n" +
                "- speed/max_time/bullet_length: 投射物组件配置\n" +
                "- scale: 投射物缩放（%）",
                MessageType.Info);
        }

        private void ApplyChanges()
        {
            EditorUtility.SetDirty(_nodeConfig);
            AssetDatabase.SaveAssets();
            LogTool.Log($"[SkillNodeEditor] 已应用更改: {_nodeConfig.NodeType}");
            _onCloseCallback?.Invoke();
        }

        private void OnDestroy()
        {
            _onCloseCallback?.Invoke();
        }
    }
}
#endif
