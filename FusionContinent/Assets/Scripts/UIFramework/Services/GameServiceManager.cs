using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace UIFramework.Services
{
    /// <summary>
    /// Service接口 - 所有Service必须实现
    /// </summary>
    public interface IGameService
    {
        /// <summary>
        /// Service名称
        /// </summary>
        string ServiceName { get; }
        
        /// <summary>
        /// 是否已初始化
        /// </summary>
        bool IsInitialized { get; }
        
        /// <summary>
        /// 初始化（异步）
        /// </summary>
        UniTask InitializeAsync();
        
        /// <summary>
        /// 每帧更新（可选）
        /// </summary>
        void OnUpdate(float deltaTime);
        
        /// <summary>
        /// 销毁清理
        /// </summary>
        void OnDestroy();
        
        /// <summary>
        /// 重置数据（切换账号等）
        /// </summary>
        void Reset();
    }

    /// <summary>
    /// Service基类
    /// </summary>
    public abstract class GameServiceBase : IGameService
    {
        public virtual string ServiceName => GetType().Name;
        public bool IsInitialized { get; protected set; }
        
        public virtual UniTask InitializeAsync()
        {
            IsInitialized = true;
            LogTool.Log($"[GameService] {ServiceName} 初始化完成");
            return UniTask.CompletedTask;
        }
        
        public virtual void OnUpdate(float deltaTime) { }
        
        public virtual void OnDestroy()
        {
            IsInitialized = false;
            LogTool.Log($"[GameService] {ServiceName} 已销毁");
        }
        
        public virtual void Reset()
        {
            LogTool.Log($"[GameService] {ServiceName} 数据已重置");
        }
        
        /// <summary>
        /// 触发数据变更事件
        /// </summary>
        protected void NotifyDataChanged()
        {
            OnDataChanged?.Invoke();
        }
        
        public event Action OnDataChanged;
    }

    /// <summary>
    /// Service管理器 - 统一管理所有Service
    /// </summary>
    public static class GameServiceManager
    {
        private static readonly Dictionary<Type, IGameService> mServices = new Dictionary<Type, IGameService>();
        private static readonly List<IGameService> mPendingServices = new List<IGameService>();
        private static bool mIsInitialized = false;
        private static GameServiceUpdater mUpdater;
        
        /// <summary>
        /// 所有Service初始化完成事件
        /// </summary>
        public static event Action OnAllServicesInitialized;

        /// <summary>
        /// 初始化Service系统（游戏启动时调用一次）
        /// </summary>
        public static async UniTask InitializeAsync()
        {
            if (mIsInitialized) return;
            mIsInitialized = true;

            // 创建Updater MonoBehaviour用于Update
            var updaterObj = new GameObject("GameServiceUpdater");
            UnityEngine.Object.DontDestroyOnLoad(updaterObj);
            mUpdater = updaterObj.AddComponent<GameServiceUpdater>();
            mUpdater.OnUpdateAction = OnUpdate;

            LogTool.Log("[GameServiceManager] 初始化完成");
            await UniTask.CompletedTask;
        }
        
        /// <summary>
        /// 注册Service到列表（不立即初始化，等待InitializeAllAsync统一初始化）
        /// </summary>
        public static void Register<T>(T service) where T : class, IGameService
        {
            Type type = typeof(T);
            
            if (mServices.ContainsKey(type))
            {
                LogTool.LogWarning($"[GameServiceManager] Service {type.Name} 已存在，跳过注册");
                return;
            }
            
            mServices[type] = service;
            mPendingServices.Add(service);
            
            LogTool.Log($"[GameServiceManager] 注册Service: {type.Name}");
        }
        
        /// <summary>
        /// 注册并立即初始化Service
        /// </summary>
        public static void RegisterAndInitialize<T>(T service) where T : class, IGameService
        {
            Register(service);
            InitializeServiceAsync(service);
        }
        
        /// <summary>
        /// 初始化所有已注册的Service
        /// </summary>
        public static async UniTask InitializeAllAsync()
        {
            if (mPendingServices.Count == 0)
            {
                LogTool.Log("[GameServiceManager] 没有待初始化的Service");
                return;
            }
            
            LogTool.Log($"[GameServiceManager] 开始初始化 {mPendingServices.Count} 个Service...");
            
            var initTasks = new List<UniTask>();
            foreach (var service in mPendingServices)
            {
                initTasks.Add(InitializeServiceWithErrorHandling(service));
            }
            
            // 等待所有Service初始化完成
            await UniTask.WhenAll(initTasks);
            
            mPendingServices.Clear();
            
            LogTool.Log("[GameServiceManager] 所有Service初始化完成！");
            OnAllServicesInitialized?.Invoke();
        }
        
        /// <summary>
        /// 获取Service（如果不存在返回null）
        /// </summary>
        public static T Get<T>() where T : class, IGameService
        {
            if (mServices.TryGetValue(typeof(T), out var service))
            {
                return (T)service;
            }
            return null;
        }
        
        /// <summary>
        /// 获取或创建Service（如果不存在则自动创建并初始化）
        /// </summary>
        public static T GetOrCreate<T>() where T : class, IGameService, new()
        {
            var service = Get<T>();
            if (service == null)
            {
                service = new T();
                RegisterAndInitialize(service);
            }
            return service;
        }
        
        /// <summary>
        /// 检查Service是否存在
        /// </summary>
        public static bool Has<T>() where T : class, IGameService
        {
            return mServices.ContainsKey(typeof(T));
        }
        
        /// <summary>
        /// 等待所有Service初始化完成
        /// </summary>
        public static async UniTask WaitForAllInitialized()
        {
            while (mPendingServices.Count > 0)
            {
                await UniTask.Delay(100);
            }
            
            // 等待所有Service的IsInitialized标志
            bool allInitialized = false;
            while (!allInitialized)
            {
                allInitialized = true;
                foreach (var service in mServices.Values)
                {
                    if (!service.IsInitialized)
                    {
                        allInitialized = false;
                        break;
                    }
                }
                
                if (!allInitialized)
                {
                    await UniTask.Delay(100);
                }
            }
        }
        
        /// <summary>
        /// 等待指定Service初始化完成
        /// </summary>
        public static async UniTask WaitForInitialized<T>() where T : class, IGameService
        {
            var service = Get<T>();
            if (service == null) return;
            
            while (!service.IsInitialized)
            {
                await UniTask.Delay(100);
            }
        }
        
        /// <summary>
        /// 释放指定Service
        /// </summary>
        public static void Release<T>() where T : class, IGameService
        {
            Type type = typeof(T);
            if (mServices.TryGetValue(type, out var service))
            {
                service.OnDestroy();
                mServices.Remove(type);
                mPendingServices.Remove(service);
                LogTool.Log($"[GameServiceManager] 释放Service: {type.Name}");
            }
        }
        
        /// <summary>
        /// 重置所有Service数据（切换账号）
        /// </summary>
        public static void ResetAll()
        {
            foreach (var service in mServices.Values)
            {
                service.Reset();
            }
            LogTool.Log("[GameServiceManager] 所有Service已重置");
        }
        
        /// <summary>
        /// 销毁所有Service（退出游戏）
        /// </summary>
        public static void ReleaseAll()
        {
            foreach (var service in mServices.Values)
            {
                service.OnDestroy();
            }
            mServices.Clear();
            mPendingServices.Clear();
            
            if (mUpdater != null)
            {
                UnityEngine.Object.Destroy(mUpdater.gameObject);
                mUpdater = null;
            }
            
            mIsInitialized = false;
            LogTool.Log("[GameServiceManager] 所有Service已销毁");
        }
        
        /// <summary>
        /// 获取所有Service信息
        /// </summary>
        public static List<ServiceInfo> GetAllServiceInfo()
        {
            var list = new List<ServiceInfo>();
            foreach (var kvp in mServices)
            {
                list.Add(new ServiceInfo
                {
                    Type = kvp.Key.Name,
                    Name = kvp.Value.ServiceName,
                    IsInitialized = kvp.Value.IsInitialized
                });
            }
            return list;
        }
        
        private static async UniTask InitializeServiceWithErrorHandling(IGameService service)
        {
            try
            {
                await service.InitializeAsync();
            }
            catch (Exception e)
            {
                LogTool.LogError($"[GameServiceManager] Service初始化失败: {service.ServiceName}, 错误: {e}");
            }
        }
        
        private static async void InitializeServiceAsync(IGameService service)
        {
            await InitializeServiceWithErrorHandling(service);
        }
        
        private static void OnUpdate(float deltaTime)
        {
            foreach (var service in mServices.Values)
            {
                if (service.IsInitialized)
                {
                    service.OnUpdate(deltaTime);
                }
            }
        }
    }

    /// <summary>
    /// Service信息
    /// </summary>
    public struct ServiceInfo
    {
        public string Type;
        public string Name;
        public bool IsInitialized;
    }

    /// <summary>
    /// 用于每帧更新的MonoBehaviour
    /// </summary>
    public class GameServiceUpdater : MonoBehaviour
    {
        public Action<float> OnUpdateAction;
        
        void Update()
        {
            OnUpdateAction?.Invoke(Time.deltaTime);
        }
        
        void OnDestroy()
        {
            OnUpdateAction = null;
        }
    }
}
