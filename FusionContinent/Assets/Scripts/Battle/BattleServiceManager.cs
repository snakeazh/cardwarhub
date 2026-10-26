using UIFramework.Services;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Battle
{
    /// <summary>
    /// 战斗场景服务管理器
    /// 管理战斗场景专用的Service，退出战斗场景时自动清理
    /// </summary>
    public class BattleServiceManager : MonoBehaviour
    {
        #region 单例
        public static BattleServiceManager Instance { get; private set; }
        #endregion

        #region 字段
        // 战斗场景专用的Service列表
        private List<IGameService> mBattleServices = new List<IGameService>();
        
        // 是否已初始化
        private bool mIsInitialized = false;
        #endregion

        #region 属性
        public bool IsInitialized => mIsInitialized;
        #endregion

        #region 生命周期
        void Awake()
        {
            if (Instance != null)
            {
                LogTool.LogWarning("[BattleServiceManager] 已存在实例，销毁重复的");
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            LogTool.Log("[BattleServiceManager] 创建完成");
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
            
            // 确保清理所有Service
            CleanupAllServices();
        }

        void LateUpdate()
        {
            BattleScheduler.Instance.ProcessLateUpdate();
        }
        #endregion

        #region 公共方法

        /// <summary>
        /// 初始化战斗场景服务
        /// 在战斗场景开始时调用
        /// </summary>
        public async UniTask InitializeBattleServices()
        {
            if (mIsInitialized)
            {
                LogTool.LogWarning("[BattleServiceManager] 已经初始化，跳过");
                return;
            }

            LogTool.Log("[BattleServiceManager] 开始初始化战斗场景服务...");

            // 注册战斗场景专用的Service
            RegisterBattleServices();

            // 初始化所有Service
            foreach (var service in mBattleServices)
            {
                if (!service.IsInitialized)
                {
                    await service.InitializeAsync();
                    LogTool.Log($"[BattleServiceManager] Service初始化完成: {service.GetType().Name}");
                }
            }

            mIsInitialized = true;
            BattleScheduler.Instance.ResetForBattle();
            LogTool.Log("[BattleServiceManager] 所有战斗场景服务初始化完成");
        }

        /// <summary>
        /// 清理所有战斗场景服务
        /// 在退出战斗场景时调用
        /// </summary>
        public void CleanupBattleServices()
        {
            if (!mIsInitialized)
            {
                LogTool.LogWarning("[BattleServiceManager] 未初始化，无需清理");
                return;
            }

            LogTool.Log("[BattleServiceManager] 开始清理战斗场景服务...");

            BattleScheduler.Instance.Clear();
            CleanupAllServices();

            mIsInitialized = false;
            LogTool.Log("[BattleServiceManager] 所有战斗场景服务已清理");
        }

        /// <summary>
        /// 获取战斗场景Service
        /// </summary>
        public T GetService<T>() where T : class, IGameService
        {
            foreach (var service in mBattleServices)
            {
                if (service is T t)
                {
                    return t;
                }
            }
            
            LogTool.LogWarning($"[BattleServiceManager] 找不到Service: {typeof(T).Name}");
            return null;
        }

        /// <summary>
        /// 检查是否存在某个Service
        /// </summary>
        public bool HasService<T>() where T : class, IGameService
        {
            foreach (var service in mBattleServices)
            {
                if (service is T)
                {
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 注册战斗场景专用的Service
        /// </summary>
        private void RegisterBattleServices()
        {
            // 清空列表（防止重复）
            mBattleServices.Clear();

            // 注册时间管理Service
            mBattleServices.Add(new BattleTimeService());

            // 注册城市等级Service
            mBattleServices.Add(new BattleCityLevelService());

            // 注册技能管理服务
            mBattleServices.Add(new BattleSkillService());

            // 注册规则管理服务
            mBattleServices.Add(new BattleRuleService());

            // 注册流派系统Service（示例）
            // mBattleServices.Add(new BattleSchoolService());

            // 注册技能卡牌Service（示例）
            // mBattleServices.Add(new BattleSkillCardService());

            // 注册战斗信息Service（示例）
            // mBattleServices.Add(new BattleCombatInfoService());

            LogTool.Log($"[BattleServiceManager] 注册了 {mBattleServices.Count} 个战斗场景Service");
        }

        /// <summary>
        /// 清理所有Service
        /// </summary>
        private void CleanupAllServices()
        {
            // 反向遍历，先清理后注册的Service
            for (int i = mBattleServices.Count - 1; i >= 0; i--)
            {
                var service = mBattleServices[i];
                if (service != null)
                {
                    try
                    {
                        service.OnDestroy();
                        LogTool.Log($"[BattleServiceManager] Service已清理: {service.GetType().Name}");
                    }
                    catch (System.Exception ex)
                    {
                        LogTool.LogError($"[BattleServiceManager] 清理Service失败: {service.GetType().Name}, 错误: {ex}");
                    }
                }
            }

            mBattleServices.Clear();
        }

        #endregion
    }
}
