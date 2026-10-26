using UIFramework.Services;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Player
{
    public class PlayerService : GameServiceBase
    {
        public bool IsInitialized { get; private set; }

        public override async UniTask InitializeAsync()
        {
            await base.InitializeAsync();

            IsInitialized = true;
            LogTool.Log("[PlayerService] 初始化完成");
        }
    }
}
