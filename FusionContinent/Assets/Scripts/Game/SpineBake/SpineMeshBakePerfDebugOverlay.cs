using FusionContinent;
using FusionContinent.Monster.Animation;
using UnityEngine;

namespace FusionContinent.SpineBake
{
    /// <summary>
    /// 局内 Spine/BakeMesh 性能调试浮层（左上角），可通过设置开关显示。
    /// </summary>
    public class SpineMeshBakePerfDebugOverlay : MonoBehaviour
    {
        private const float UiScale = 3f;
        private const float PanelWidth = 300f;

        private bool _panelExpanded = true;
        private Rect _panelRect = new Rect(12f, 12f, PanelWidth, 160f);

        private void OnGUI()
        {
            if (!SpineMeshBakeSettings.ShowRuntimeOverlay.Value)
            {
                return;
            }

            Matrix4x4 previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(UiScale, UiScale, 1f));

            _panelRect.height = _panelExpanded ? 168f : 28f;
            _panelRect = GUI.Window(GetInstanceID(), _panelRect, DrawWindow, "Spine 性能调试");

            GUI.matrix = previousMatrix;
        }

        private void DrawWindow(int windowId)
        {
            if (!_panelExpanded)
            {
                if (GUILayout.Button("展开"))
                {
                    _panelExpanded = true;
                }

                GUI.DragWindow();
                return;
            }

            bool perfLog = GUILayout.Toggle(
                SpineMeshBakeSettings.EnablePerfLog.Value,
                "性能日志 [SpinePerf]（每 3 秒 Console）");
            if (perfLog != SpineMeshBakeSettings.EnablePerfLog.Value)
            {
                SpineMeshBakeSettings.SetPerfLogEnabled(perfLog);
            }

            bool bakeMesh = GUILayout.Toggle(
                SpineMeshBakeSettings.UseBakedMesh,
                "BakeMesh 渲染（GlobalRenderMode）");
            if (bakeMesh != SpineMeshBakeSettings.UseBakedMesh)
            {
                SpineMeshBakeSettings.SetEnabled(bakeMesh);
            }

            GUILayout.Label(
                "GlobalRenderMode: " + MonsterAnimationSettings.GlobalRenderMode.Value
                + " | PerfLog: " + (SpineMeshBakeSettings.EnablePerfLog.Value ? "ON" : "OFF"));

            if (GUILayout.Button("立即打印一次 [SpinePerf]"))
            {
                SpineMeshBakePerfMonitor.LogReportNow();
            }

            if (GUILayout.Button("收起"))
            {
                _panelExpanded = false;
            }

            GUI.DragWindow();
        }
    }
}
