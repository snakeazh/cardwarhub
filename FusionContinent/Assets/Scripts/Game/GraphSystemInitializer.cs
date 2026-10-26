using FusionContinent.Skill.Handler;
using UnityEngine;

namespace FusionContinent
{
    /// <summary>
    /// 节点图系统初始化器
    /// </summary>
    public class GraphSystemInitializer : MonoBehaviour
    {
        [SerializeField] private bool _autoInitialize = true;

        private void Awake()
        {
            if (_autoInitialize)
            {
                Initialize();
            }
        }

        /// <summary>
        /// 初始化所有 Handler
        /// </summary>
        public static void Initialize()
        {
            LogTool.Log("[GraphSystemInitializer] 开始初始化...");

            // 初始化 Skill Handler
            SkillHandlerRegistry.Initialize();

            LogTool.Log("[GraphSystemInitializer] 初始化完成!");
        }
    }
}
