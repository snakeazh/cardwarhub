using Config;
using FusionContinent.Monster.Animation;
using QFramework;
using UnityEngine;

namespace FusionContinent.SpineBake
{
    /// <summary>
    /// 局内 Spine 网格预烘焙开关（兼容旧 API，真值源为 <see cref="MonsterAnimationSettings.GlobalRenderMode"/>）。
    /// </summary>
    public static class SpineMeshBakeSettings
    {
        private const string EnablePerfLogPrefKey = "SpineMeshBakeEnablePerfLog";
        private const string ShowRuntimeOverlayPrefKey = "SpineMeshBakeShowPerfOverlay";

        /// <summary>仅对普通怪启用烘焙（Boss/精英仍走 Spine）。</summary>
        public static BindableProperty<bool> OnlyOrdinaryMonsters { get; } = new BindableProperty<bool>(true);

        /// <summary>是否定期打印 Spine 烘焙 vs 骨骼 CPU 对比日志。</summary>
        public static BindableProperty<bool> EnablePerfLog { get; } = new BindableProperty<bool>(false);

        /// <summary>局内是否显示性能调试浮层（可在此开关 PerfLog / BakeMesh）。</summary>
        public static BindableProperty<bool> ShowRuntimeOverlay { get; } = new BindableProperty<bool>(false);

        /// <summary>
        /// 与 <see cref="MonsterAnimationSettings.GlobalRenderMode"/> 同步，禁止单独写 PlayerPrefs。
        /// </summary>
        public static bool UseBakedMesh =>
            MonsterAnimationSettings.GlobalRenderMode.Value == AnimationRenderMode.BakeMesh;

        static SpineMeshBakeSettings()
        {
            OnlyOrdinaryMonsters.Value = true;
            EnablePerfLog.Value = PlayerPrefs.GetInt(EnablePerfLogPrefKey, 0) == 1;
            ShowRuntimeOverlay.Value = PlayerPrefs.GetInt(ShowRuntimeOverlayPrefKey, 0) == 1;
            EnablePerfLog.RegisterWithInitValue(SaveEnablePerfLog);
            ShowRuntimeOverlay.RegisterWithInitValue(SaveShowRuntimeOverlay);
        }

        public static bool ShouldUseForMonster(MonsterQuality quality, string skeletonName)
        {
            return MonsterAnimationSettings.ResolveRenderMode(quality, skeletonName) == AnimationRenderMode.BakeMesh;
        }

        public static void SetEnabled(bool enabled)
        {
            MonsterAnimationSettings.SetGlobalRenderMode(
                enabled ? AnimationRenderMode.BakeMesh : AnimationRenderMode.Spine);
        }

        public static void SetPerfLogEnabled(bool enabled)
        {
            EnablePerfLog.Value = enabled;
        }

        public static void SetRuntimeOverlayVisible(bool visible)
        {
            ShowRuntimeOverlay.Value = visible;
        }

        private static void SaveEnablePerfLog(bool enabled)
        {
            PlayerPrefs.SetInt(EnablePerfLogPrefKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        private static void SaveShowRuntimeOverlay(bool visible)
        {
            PlayerPrefs.SetInt(ShowRuntimeOverlayPrefKey, visible ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
