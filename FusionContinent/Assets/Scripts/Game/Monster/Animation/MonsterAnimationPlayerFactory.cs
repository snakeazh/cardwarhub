using FusionContinent;
using FusionContinent.SpineBake;

namespace FusionContinent.Monster.Animation
{
    public static class MonsterAnimationPlayerFactory
    {
        public static IMonsterAnimationPlayer Create(AnimationRenderMode renderMode)
        {
            switch (renderMode)
            {
                case AnimationRenderMode.BakeMesh:
                    return new BakeMeshAnimationPlayer();
                case AnimationRenderMode.GpuVertex:
                    return new GpuVertexAnimationPlayer();
                default:
                    return new SpineAnimationPlayer();
            }
        }

        public static IMonsterAnimationPlayer CreateForMonster(MonsterAnimationPlayerContext context)
        {
            AnimationRenderMode resolvedMode =
                MonsterAnimationSettings.ResolveRenderMode(context.MonsterQuality, context.SkeletonName);

            if (resolvedMode == AnimationRenderMode.BakeMesh && !BakeMeshFrameLoader.HasValidData(context.SkeletonName))
            {
                resolvedMode = AnimationRenderMode.Spine;
            }

            context.RenderMode = resolvedMode;
            IMonsterAnimationPlayer player = Create(resolvedMode);
            player.Initialize(context);

            if (resolvedMode == AnimationRenderMode.BakeMesh && !player.IsInitialized)
            {
                if (player is BakeMeshAnimationPlayer failedBakePlayer)
                {
                    failedBakePlayer.Release();
                }

                resolvedMode = AnimationRenderMode.Spine;
                context.RenderMode = resolvedMode;
                player = Create(resolvedMode);
                player.Initialize(context);
            }

            if (SpineMeshBakeSettings.EnablePerfLog.Value)
            {
                LogTool.Log(
                    "[AnimationPlayer] Create "
                    + context.SkeletonName
                    + " GlobalMode=" + MonsterAnimationSettings.GlobalRenderMode.Value
                    + " ResolvedMode=" + resolvedMode
                    + " Type=" + player.GetType().Name
                    + " Init=" + player.IsInitialized);
            }

            return player;
        }
    }
}
