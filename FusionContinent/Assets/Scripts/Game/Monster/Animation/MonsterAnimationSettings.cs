using Config;
using FusionContinent.SpineBake;
using QFramework;
using UnityEngine;

namespace FusionContinent.Monster.Animation
{
    /// <summary>
    /// 局内怪物动画渲染模式配置（运行时开关）。
    /// </summary>
    public static class MonsterAnimationSettings
    {
        private const string RenderModePrefKey = "MonsterAnimationRenderMode";
        private const string LegacyBakePrefKey = "UseSpineMeshBake";

        public static BindableProperty<AnimationRenderMode> GlobalRenderMode { get; } =
            new BindableProperty<AnimationRenderMode>(AnimationRenderMode.Spine);

        public static BindableProperty<bool> OnlyOrdinaryMonsters { get; } = new BindableProperty<bool>(true);

        static MonsterAnimationSettings()
        {
            if (PlayerPrefs.HasKey(RenderModePrefKey))
            {
                GlobalRenderMode.Value = (AnimationRenderMode)PlayerPrefs.GetInt(RenderModePrefKey, 0);
            }
            else if (PlayerPrefs.GetInt(LegacyBakePrefKey, 0) == 1)
            {
                GlobalRenderMode.Value = AnimationRenderMode.BakeMesh;
            }

            GlobalRenderMode.RegisterWithInitValue(SaveRenderMode);
            GlobalRenderMode.Register(OnRenderModeChanged);
            OnlyOrdinaryMonsters.Register(SyncOnlyOrdinaryMonstersToLegacy);
            SyncOnlyOrdinaryMonstersToLegacy(OnlyOrdinaryMonsters.Value);
        }

        private static void SyncOnlyOrdinaryMonstersToLegacy(bool value)
        {
            SpineMeshBakeSettings.OnlyOrdinaryMonsters.Value = value;
        }

        private static void OnRenderModeChanged(AnimationRenderMode mode)
        {
            if (MonsterManager.Instance == null)
            {
                return;
            }

            for (int i = 0; i < MonsterManager.Instance.enemies.Count; i++)
            {
                MonsterCard monster = MonsterManager.Instance.enemies[i];
                if (monster == null || !monster.IsInit)
                {
                    continue;
                }

                monster.AnimationController.RebindRenderPlayer();
                monster.Play("walk", true);
            }
        }

        public static AnimationRenderMode ResolveRenderMode(MonsterQuality quality, string skeletonName)
        {
            AnimationRenderMode mode = GlobalRenderMode.Value;
            if (mode == AnimationRenderMode.Spine)
            {
                return AnimationRenderMode.Spine;
            }

            if (OnlyOrdinaryMonsters.Value && quality != MonsterQuality.Ordinary)
            {
                return AnimationRenderMode.Spine;
            }

            if (mode == AnimationRenderMode.BakeMesh)
            {
                if (BakeMeshFrameLoader.HasData(skeletonName))
                {
                    return AnimationRenderMode.BakeMesh;
                }

                return AnimationRenderMode.Spine;
            }

            if (mode == AnimationRenderMode.GpuVertex)
            {
                return AnimationRenderMode.GpuVertex;
            }

            return AnimationRenderMode.Spine;
        }

        public static void SetGlobalRenderMode(AnimationRenderMode mode)
        {
            GlobalRenderMode.Value = mode;
        }

        private static void SaveRenderMode(AnimationRenderMode mode)
        {
            PlayerPrefs.SetInt(RenderModePrefKey, (int)mode);
            PlayerPrefs.SetInt(LegacyBakePrefKey, mode == AnimationRenderMode.BakeMesh ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
