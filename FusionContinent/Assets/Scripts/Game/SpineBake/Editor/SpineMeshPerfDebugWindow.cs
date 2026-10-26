#if UNITY_EDITOR
using FusionContinent;
using FusionContinent.Monster.Animation;
using FusionContinent.SpineBake;
using UnityEditor;
using UnityEngine;

namespace FusionContinent.SpineBake.Editor
{
    /// <summary>
    /// Spine / BakeMesh 性能监控与渲染模式调试（支持 Play Mode）。
    /// </summary>
    public class SpineMeshPerfDebugWindow : EditorWindow
    {
        [MenuItem("Tools/Spine/Spine Performance Debug")]
        public static void Open()
        {
            GetWindow<SpineMeshPerfDebugWindow>("Spine Perf Debug");
        }

        private void OnEnable()
        {
            EditorApplication.update += Repaint;
        }

        private void OnDisable()
        {
            EditorApplication.update -= Repaint;
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Spine 动画性能监控", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            DrawToggle(
                "性能日志 [SpinePerf]",
                "每 3 秒向 Console 输出 CPU 对比（PlayerPrefs 持久化）",
                SpineMeshBakeSettings.EnablePerfLog.Value,
                SpineMeshBakeSettings.SetPerfLogEnabled);

            DrawToggle(
                "局内调试浮层",
                "战斗左上角可拖拽面板，运行时开关 PerfLog / BakeMesh",
                SpineMeshBakeSettings.ShowRuntimeOverlay.Value,
                SpineMeshBakeSettings.SetRuntimeOverlayVisible);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("渲染模式", EditorStyles.boldLabel);

            DrawToggle(
                "BakeMesh 渲染",
                "真值源 MonsterAnimationSettings.GlobalRenderMode",
                SpineMeshBakeSettings.UseBakedMesh,
                enabled => SpineMeshBakeSettings.SetEnabled(enabled));

            EditorGUILayout.LabelField(
                "GlobalRenderMode: " + MonsterAnimationSettings.GlobalRenderMode.Value);

            EditorGUILayout.Space();

            if (GUILayout.Button("立即打印 [SpinePerf]"))
            {
                SpineMeshBakePerfMonitor.LogReportNow();
            }

            if (Application.isPlaying && GamePlayManager.Default != null)
            {
                GamePlayManager.Default.EnsureSpineMeshBakePerfMonitor();
                GamePlayManager.Default.EnsureSpineMeshBakePerfDebugOverlay();
            }

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("进入 Play Mode 后可查看实时怪物 Player 统计。", MessageType.Info);
            }
        }

        private static void DrawToggle(string label, string tooltip, bool value, System.Action<bool> setter)
        {
            EditorGUI.BeginChangeCheck();
            bool next = EditorGUILayout.Toggle(new GUIContent(label, tooltip), value);
            if (EditorGUI.EndChangeCheck())
            {
                setter(next);
            }
        }
    }
}
#endif
