using UIFramework.Services;
using Cysharp.Threading.Tasks;
using UnityEngine;
using System.Collections.Generic;
using Config;
using FusionContinent;

namespace Game.Battle
{
    /// <summary>
    /// 战斗场景城市等级服务
    /// 管理城市等级、经验值、升级逻辑
    /// </summary>
    public class BattleCityLevelService : GameServiceBase
    {
        #region 属性
        /// <summary>
        /// 当前城市等级
        /// </summary>
        public int CurrentLevel { get; private set; } = 1;

        /// <summary>
        /// 当前经验值
        /// </summary>
        public int CurrentExp { get; private set; } = 0;

        /// <summary>
        /// 升级所需经验值
        /// </summary>
        public int RequiredExp { get; private set; } = 100;

        /// <summary>
        /// 经验值进度（0-1）
        /// </summary>
        public float ExpProgress => RequiredExp > 0 ? (float)CurrentExp / RequiredExp : 0f;
        #endregion

        #region 事件
        public event System.Action<int> OnLevelUp;           // 升级事件
        public event System.Action<int> OnExpChanged;        // 经验值变化事件
        public event System.Action<int, int> OnExpAdded;     // 获得经验值事件 (当前值, 增加值)
        #endregion

        #region 配置
        private TbCityLevelConfig mCityLevelConfig;
        #endregion

        public override async UniTask InitializeAsync()
        {
            await base.InitializeAsync();
            
            mCityLevelConfig = DataConfigManager.Tables.TbCityLevelConfig;
            
            // 加载初始数据
            LoadData();
            
            LogTool.Log($"[BattleCityLevelService] 初始化完成，当前等级: {CurrentLevel}");
        }

        #region 公共方法

        /// <summary>
        /// 增加经验值
        /// </summary>
        public void AddExp(int exp)
        {
            if (exp <= 0) return;

            int oldExp = CurrentExp;
            CurrentExp += exp;
            
            OnExpAdded?.Invoke(CurrentExp, exp);
            OnExpChanged?.Invoke(CurrentExp);

            // 检查升级
            CheckLevelUp();

            LogTool.Log($"[BattleCityLevelService] 获得经验值: +{exp}, 当前: {CurrentExp}/{RequiredExp}");
        }

        /// <summary>
        /// 设置等级
        /// </summary>
        public void SetLevel(int level)
        {
            if (level < 1) level = 1;
            
            int oldLevel = CurrentLevel;
            CurrentLevel = level;
            CurrentExp = 0;
            
            // 更新所需经验值
            UpdateRequiredExp();
            
            if (level > oldLevel)
            {
                OnLevelUp?.Invoke(level);
            }

            LogTool.Log($"[BattleCityLevelService] 等级变更: {oldLevel} -> {level}");
        }

        /// <summary>
        /// 重置
        /// </summary>
        public void Reset()
        {
            CurrentLevel = 1;
            CurrentExp = 0;
            UpdateRequiredExp();
            
            LogTool.Log("[BattleCityLevelService] 已重置");
        }

        /// <summary>
        /// 获取等级配置
        /// </summary>
        public CityLevelList GetLevelConfig(int level)
        {
            var levelConfig = mCityLevelConfig.GetOrDefault(Global.GameDifficultyLevel.Value);
            if (levelConfig == null || level <= 0 || level > levelConfig.LevelList.Count)
            {
                return null;
            }
            return levelConfig.LevelList[level - 1];
        }

        #endregion

        #region 私有方法

        private void LoadData()
        {
            // 从全局数据加载
            // 这里可以根据实际需求从存档或配置加载
            UpdateRequiredExp();
        }

        private void UpdateRequiredExp()
        {
            var config = GetLevelConfig(CurrentLevel);
            RequiredExp = config?.CityExp ?? 100;
        }

        private void CheckLevelUp()
        {
            while (CurrentExp >= RequiredExp && RequiredExp > 0)
            {
                CurrentExp -= RequiredExp;
                CurrentLevel++;
                
                UpdateRequiredExp();
                
                OnLevelUp?.Invoke(CurrentLevel);
                
                LogTool.Log($"[BattleCityLevelService] 升级！当前等级: {CurrentLevel}");
            }
        }

        #endregion

        #region 销毁清理

        public override void OnDestroy()
        {
            base.OnDestroy();
            
            // 清理事件监听
            OnLevelUp = null;
            OnExpChanged = null;
            OnExpAdded = null;
            
            LogTool.Log("[BattleCityLevelService] 已销毁清理");
        }

        #endregion
    }
}
