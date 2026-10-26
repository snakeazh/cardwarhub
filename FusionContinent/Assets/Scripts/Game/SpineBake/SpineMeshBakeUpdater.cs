using FusionContinent;
using FusionContinent.Monster.Animation;
using UnityEngine;

namespace FusionContinent.SpineBake
{
    /// <summary>
    /// 集中 Tick 需要手动更新的 <see cref="IMonsterAnimationPlayer"/>。
    /// </summary>
    public class SpineMeshBakeUpdater : MonoBehaviour
    {
        public static SpineMeshBakeUpdater Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void LateUpdate()
        {
            if (GamePlayManager.Default != null && GamePlayManager.Default.IsPaused())
            {
                return;
            }

            MonsterAnimationPlayerRegistry.TickAll(Time.deltaTime);
        }
    }
}
