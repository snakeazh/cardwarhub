using QFramework;
using UnityEngine;

namespace FusionContinent
{
    public partial class GameMap : ViewController
    {
        public static GameMap Default;

        private void Awake()
        {
            Default = this;
        }
        
        private void OnDestroy()
        {
            ClearHeroSlotLockTweens();
            Default = null;
        }

        void Start()
        {
            InitHeroSlotLocks();
        }
    }
}
