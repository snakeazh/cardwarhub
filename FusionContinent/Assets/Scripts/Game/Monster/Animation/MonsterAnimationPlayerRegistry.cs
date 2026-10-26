using System.Collections.Generic;

namespace FusionContinent.Monster.Animation
{
    /// <summary>
    /// 需要手动 Tick 的播放器注册表（BakeMesh / 未来 GpuVertex）。
    /// </summary>
    public static class MonsterAnimationPlayerRegistry
    {
        private static readonly List<IMonsterAnimationPlayer> ManualTickPlayers = new List<IMonsterAnimationPlayer>();

        public static void Register(IMonsterAnimationPlayer player)
        {
            if (player == null || player.RenderMode == AnimationRenderMode.Spine)
            {
                return;
            }

            if (!ManualTickPlayers.Contains(player))
            {
                ManualTickPlayers.Add(player);
            }
        }

        public static void Unregister(IMonsterAnimationPlayer player)
        {
            if (player == null)
            {
                return;
            }

            ManualTickPlayers.Remove(player);
        }

        public static void TickAll(float deltaTime)
        {
            for (int i = 0; i < ManualTickPlayers.Count; i++)
            {
                IMonsterAnimationPlayer player = ManualTickPlayers[i];
                if (player.IsInitialized)
                {
                    player.Update(deltaTime);
                }
            }
        }

        public static void Clear()
        {
            ManualTickPlayers.Clear();
        }
    }
}
