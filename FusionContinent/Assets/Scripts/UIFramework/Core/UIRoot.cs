using UnityEngine;

namespace UIFramework
{
    /// <summary>
    /// UIRoot单例（用于启动协程和访问UIRootConfig）
    /// 挂载在场景中的UIRoot物体上
    /// </summary>
    public class UIRoot : MonoBehaviour
    {
        private static UIRoot mInstance;
        
        public static UIRoot Instance
        {
            get
            {
                return mInstance;
            }
        }
        
        /// <summary>
        /// UIRootConfig配置组件
        /// </summary>
        public UIRootConfig Config { get; private set; }
        
        void Awake()
        {
            if (mInstance != null && mInstance != this)
            {
                Destroy(gameObject);
                return;
            }
            mInstance = this;

            EnsureSingleEventSystem();

            // 获取UIRootConfig
            Config = GetComponent<UIRootConfig>();
            if (Config == null)
            {
                LogTool.LogError("[UIRoot] UIRoot上缺少UIRootConfig组件！请在UIRoot上挂载UIRootConfig脚本并在Inspector中配置层级。");
            }
        }

        /// <summary>
        /// UIRoot 预制体自带 EventSystem（常驻 DontDestroyOnLoad），启动场景（UpdateScene）里也有一个，
        /// 两者并存时 Unity 会持续报警且输入可能重复处理。保留 UIRoot 下的，销毁其余场景的。
        /// </summary>
        private void EnsureSingleEventSystem()
        {
            var eventSystems = FindObjectsOfType<UnityEngine.EventSystems.EventSystem>(true);
            for (int i = 0; i < eventSystems.Length; i++)
            {
                var es = eventSystems[i];
                if (es.transform.IsChildOf(transform))
                {
                    continue;
                }
                LogTool.Log($"[UIRoot] 检测到重复 EventSystem，销毁场景中的: {es.gameObject.name}");
                Destroy(es.gameObject);
            }
        }
    }
}