using UnityEngine;
using Cysharp.Threading.Tasks;

namespace Game.Battle
{
    /// <summary>
    /// 战斗场景生命周期管理器
    /// 负责战斗场景的初始化和清理
    /// </summary>
    public class BattleSceneLifecycle : MonoBehaviour
    {
        [Header("配置")]
        [SerializeField] private bool mAutoInitialize = true;
        [SerializeField] private bool mAutoCleanupOnDestroy = true;

        // 是否已初始化
        private bool mIsInitialized = false;

        #region 生命周期

        async void Start()
        {
            if (mAutoInitialize)
            {
                await InitializeBattleScene();
            }
        }

        void OnDestroy()
        {
            if (mAutoCleanupOnDestroy && mIsInitialized)
            {
                CleanupBattleScene();
            }
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 初始化战斗场景
        /// </summary>
        public async UniTask InitializeBattleScene()
        {
            if (mIsInitialized)
            {
                LogTool.LogWarning("[BattleSceneLifecycle] 战斗场景已经初始化");
                return;
            }

            LogTool.Log("[BattleSceneLifecycle] 开始初始化战斗场景...");

            // 1. 确保 BattleServiceManager 存在
            if (BattleServiceManager.Instance == null)
            {
                var go = new GameObject("BattleServiceManager");
                go.AddComponent<BattleServiceManager>();
            }

            // 2. 初始化战斗场景服务
            await BattleServiceManager.Instance.InitializeBattleServices();

            // 3. 触发战斗开始事件
            OnBattleStarted?.Invoke();

            mIsInitialized = true;
            LogTool.Log("[BattleSceneLifecycle] 战斗场景初始化完成");
        }

        /// <summary>
        /// 清理战斗场景
        /// </summary>
        public void CleanupBattleScene()
        {
            if (!mIsInitialized)
            {
                LogTool.LogWarning("[BattleSceneLifecycle] 战斗场景未初始化，无需清理");
                return;
            }

            LogTool.Log("[BattleSceneLifecycle] 开始清理战斗场景...");

            // 1. 触发战斗结束事件
            OnBattleEnded?.Invoke();

            // 2. 释放战斗图集（与 ScenePreloadBattleAssets 的预载一一对应）
            UIFramework.AtlasManager.Instance.ReleaseBattleAtlases();

            // 3. 场景切换边界清扫：卸载所有引用计数归零的 bundle（战斗预制体/图集纹理随之真正释放）
            var package = YooAsset.YooAssets.GetPackage("DefaultPackage");
            if (package != null)
            {
                package.UnloadUnusedAssetsAsync();
            }

            // 4. 清理战斗场景服务
            if (BattleServiceManager.Instance != null)
            {
                BattleServiceManager.Instance.CleanupBattleServices();
            }

            // 5. 销毁 BattleServiceManager
            if (BattleServiceManager.Instance != null)
            {
                Destroy(BattleServiceManager.Instance.gameObject);
            }

            mIsInitialized = false;
            LogTool.Log("[BattleSceneLifecycle] 战斗场景清理完成");
        }

        /// <summary>
        /// 退出战斗场景
        /// 调用此方法后会清理所有战斗服务并返回主场景
        /// </summary>
        public void ExitBattleScene(string targetScene = "MainScene")
        {
            LogTool.Log($"[BattleSceneLifecycle] 退出战斗场景，目标场景: {targetScene}");
            
            // 先清理战斗场景
            CleanupBattleScene();
            
            // 加载目标场景
            UnityEngine.SceneManagement.SceneManager.LoadScene(targetScene);
        }

        #endregion

        #region 事件

        /// <summary>
        /// 战斗开始事件
        /// </summary>
        public static event System.Action OnBattleStarted;

        /// <summary>
        /// 战斗结束事件
        /// </summary>
        public static event System.Action OnBattleEnded;

        #endregion
    }
}
