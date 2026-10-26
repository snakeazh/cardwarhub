using System.Collections.Generic;
using System.Text;
using FusionContinent;
using FusionContinent.Monster.Animation;
using UnityEngine;

namespace FusionContinent.SpineBake
{
    /// <summary>
    /// 定期打印 Spine 烘焙 vs 骨骼实时 的 CPU 采样对比。
    /// </summary>
    public class SpineMeshBakePerfMonitor : MonoBehaviour
    {
        private const float LogIntervalSeconds = 3f;

        private float _elapsed;
        private float _frameTimeAccumMs;
        private int _frameCount;

        private void Update()
        {
            if (!SpineMeshBakeSettings.EnablePerfLog.Value)
            {
                return;
            }

            if (GamePlayManager.Default != null && GamePlayManager.Default.IsPaused())
            {
                return;
            }

            _frameTimeAccumMs += Time.unscaledDeltaTime * 1000f;
            _frameCount++;
            _elapsed += Time.unscaledDeltaTime;
            if (_elapsed < LogIntervalSeconds)
            {
                return;
            }

            LogReport();
            _elapsed = 0f;
            _frameTimeAccumMs = 0f;
            _frameCount = 0;
            SpineMeshBakePerfStats.ResetInterval();
        }

        /// <summary>立即输出一条 [SpinePerf] 日志（不重置累计统计）。</summary>
        public static void LogReportNow()
        {
            if (GamePlayManager.Default == null)
            {
                LogTool.LogWarning("[SpinePerf] GamePlayManager 未就绪，无法输出报告");
                return;
            }

            SpineMeshBakePerfMonitor monitor = GamePlayManager.Default.GetComponent<SpineMeshBakePerfMonitor>();
            if (monitor == null)
            {
                LogTool.LogWarning("[SpinePerf] 未找到 SpineMeshBakePerfMonitor");
                return;
            }

            monitor.LogReport();
        }

        private void LogReport()
        {
            int bakedCount = 0;
            int spineCount = 0;
            int totalMonsters = 0;
            var playerTypeCounts = new Dictionary<string, int>();
            var sampleLines = new StringBuilder(256);
            int sampleLimit = 5;
            int sampleIndex = 0;

            if (MonsterManager.Instance != null)
            {
                for (int i = 0; i < MonsterManager.Instance.enemies.Count; i++)
                {
                    MonsterCard monster = MonsterManager.Instance.enemies[i];
                    if (monster == null || !monster.IsInit)
                    {
                        continue;
                    }

                    totalMonsters++;
                    string playerTypeName = monster.AnimationController.PlayerTypeName;
                    if (!playerTypeCounts.ContainsKey(playerTypeName))
                    {
                        playerTypeCounts[playerTypeName] = 0;
                    }

                    playerTypeCounts[playerTypeName]++;

                    if (monster.AnimationController.UseBakedMesh)
                    {
                        bakedCount++;
                    }
                    else if (monster.AnimationController.HasSkeletonAnimation)
                    {
                        spineCount++;
                    }

                    if (sampleIndex < sampleLimit)
                    {
                        sampleLines.Append(" [");
                        sampleLines.Append(monster.name);
                        sampleLines.Append('=');
                        sampleLines.Append(playerTypeName);
                        sampleLines.Append(" RenderMode=");
                        sampleLines.Append(monster.AnimationController.RenderMode);
                        sampleLines.Append(']');
                        sampleIndex++;
                    }
                }
            }

            float avgFrameMs = _frameCount > 0 ? _frameTimeAccumMs / _frameCount : 0f;
            float intervalSec = LogIntervalSeconds;
            AnimationRenderMode globalMode = MonsterAnimationSettings.GlobalRenderMode.Value;

            var typeSummary = new StringBuilder(128);
            foreach (KeyValuePair<string, int> pair in playerTypeCounts)
            {
                typeSummary.Append(' ');
                typeSummary.Append(pair.Key);
                typeSummary.Append('=');
                typeSummary.Append(pair.Value);
            }

            LogTool.Log(
                "[SpinePerf] 配置GlobalRenderMode=" + globalMode
                + " UseBakedMesh=" + (SpineMeshBakeSettings.UseBakedMesh ? "ON" : "OFF")
                + " | 存活怪=" + totalMonsters + "(RenderMode烘焙=" + bakedCount + " Spine=" + spineCount + ")"
                + " | 实际PlayerType=" + typeSummary
                + " | 抽样" + sampleLines
                + " | 区间" + intervalSec.ToString("0") + "s"
                + " | BakeMesh Total=" + FormatStat(SpineMeshBakePerfStats.BakedMeshUpdateMs, SpineMeshBakePerfStats.BakedMeshUpdateCalls, _frameCount, bakedCount, intervalSec)
                + " | UploadVerts=" + FormatStat(SpineMeshBakePerfStats.UploadVerticesMs, SpineMeshBakePerfStats.UploadVerticesCalls, _frameCount, bakedCount, intervalSec)
                + " | UploadColors=" + FormatStat(SpineMeshBakePerfStats.UploadColorsMs, SpineMeshBakePerfStats.UploadColorsCalls, _frameCount, bakedCount, intervalSec)
                + " | Topology=" + FormatStat(SpineMeshBakePerfStats.TopologyMs, SpineMeshBakePerfStats.TopologyCalls, _frameCount, bakedCount, intervalSec)
                + " | Bounds=" + FormatStat(SpineMeshBakePerfStats.BoundsMs, SpineMeshBakePerfStats.BoundsCalls, _frameCount, bakedCount, intervalSec)
                + " | TintColors=" + FormatStat(SpineMeshBakePerfStats.TintColorsMs, SpineMeshBakePerfStats.TintColorsCalls, _frameCount, bakedCount, intervalSec)
                + " | Spine Total=" + FormatStat(SpineMeshBakePerfStats.SpineRuntimeMs, SpineMeshBakePerfStats.SpineRuntimeCalls, _frameCount, spineCount, intervalSec)
                + " | 平均帧耗时=" + avgFrameMs.ToString("0.00") + "ms");
        }

        private static string FormatStat(double totalMs, int calls, int frameCount, int monsterCount, float intervalSec)
        {
            double avgPerCall = SpineMeshBakePerfStats.GetAvgPerCall(totalMs, calls);
            double avgPerFrame = calls > 0 ? totalMs / (frameCount > 0 ? frameCount : 1) : 0;
            double avgPerMonster = monsterCount > 0 ? totalMs / monsterCount / intervalSec : 0;
            return totalMs.ToString("0.000") + "ms(" + calls + "次)"
                + " AvgPerCall=" + avgPerCall.ToString("0.000") + "ms"
                + " AvgPerFrame=" + avgPerFrame.ToString("0.000") + "ms"
                + " AvgPerMonster=" + avgPerMonster.ToString("0.000") + "ms";
        }
    }
}
