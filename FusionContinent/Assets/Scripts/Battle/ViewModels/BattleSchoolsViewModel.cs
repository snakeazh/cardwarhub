using UIFramework;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Config;
using FusionContinent;
using FusionContinent.Model;

namespace Game.Battle
{
    /// <summary>
    /// 战斗流派 ViewModel
    /// 管理流派卡牌的显示和更新（按 SchoolCardConfig.Id）
    /// </summary>
    public class BattleSchoolsViewModel : ViewModel
    {
        private const int SpecialSchoolTypeId = 102;

        #region 属性
        /// <summary>
        /// 流派ID列表 - 与 GameLandUI 一致
        /// </summary>
        public BindableProperty<List<int>> SchoolIds { get; } = 
            new BindableProperty<List<int>>(new List<int>());
        #endregion

        #region 依赖
        private TbSchoolCardConfig _schoolCardConfig;
        #endregion

        #region 事件
        public System.Action<int> OnSchoolAdded;      // 添加流派
        public System.Action<int> OnSchoolUpdated;    // 更新流派
        public System.Action<int, QualityType> OnSchoolQualityChanged; // 流派品质变化
        #endregion

        public override void OnCreate()
        {
            base.OnCreate();

            // 加载配置
            _schoolCardConfig = DataConfigManager.Tables.TbSchoolCardConfig;

            // 初始化流派数据
            InitializeSchools();

            // 监听流派变化
            RegisterSchoolListeners();

            LogTool.Log("[BattleSchoolsViewModel] 创建完成");
        }

        /// <summary>
        /// 初始化流派数据 - 与 GameLandUI 一致
        /// </summary>
        private void InitializeSchools()
        {
            var schoolCardCounts = SchoolManager.Instance.GetSchoolCardCountAll();
            var schoolIdList = new List<int>();
            
            foreach (var kv in schoolCardCounts)
            {
                if (kv.Value <= 0) continue;
                if (!ShouldDisplaySchoolCard(kv.Key)) continue;
                schoolIdList.Add(kv.Key);
            }
            
            SchoolIds.Value = schoolIdList;
            LogTool.Log($"[BattleSchoolsViewModel] 初始化流派完成: count={schoolIdList.Count}, ids={string.Join(",", schoolIdList)}");
        }

        /// <summary>
        /// 注册流派监听
        /// </summary>
        private void RegisterSchoolListeners()
        {
            // 监听流派星级变化（通过 MessagePublisher）
            Global.Publisher.Subscribe(MessagePublisher.CHANGE_SCHOOL_STAR, OnSchoolStarChanged);
        }

        /// <summary>
        /// 流派星级变化回调
        /// </summary>
        private void OnSchoolStarChanged(object obj)
        {
            var info = obj as ChangeSchoolStar;
            if (info == null) return;
            
            // 当流派星级变化时，更新显示
            OnSchoolUpdated?.Invoke(info.SchoolId);
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            // 取消订阅
            Global.Publisher.Unsubscribe(MessagePublisher.CHANGE_SCHOOL_STAR, OnSchoolStarChanged);
        }

        /// <summary>
        /// 获取流派卡配置
        /// </summary>
        public SchoolCardConfig GetSchoolCardConfig(int schoolCardId)
        {
            return _schoolCardConfig.GetOrDefault(schoolCardId);
        }

        public int GetSchoolCardCount(int schoolCardId)
        {
            return SchoolManager.Instance.GetSchoolCardCount(schoolCardId);
        }

        /// <summary>
        /// 获取流派卡品质
        /// </summary>
        public QualityType GetSchoolQuality(int schoolCardId)
        {
            var schoolCardCfg = GetSchoolCardConfig(schoolCardId);
            return schoolCardCfg?.Quality ?? QualityType.Default;
        }

        /// <summary>
        /// 添加流派
        /// </summary>
        public void AddSchool(int schoolCardId)
        {
            var haveCardCount = GetSchoolCardCount(schoolCardId);
            LogTool.Log($"[BattleSchoolsViewModel] AddSchool 调用: schoolCardId={schoolCardId}, haveCardCount={haveCardCount}");

            if (!ShouldDisplaySchoolCard(schoolCardId))
            {
                LogTool.Log($"[BattleSchoolsViewModel] 跳过特殊流派卡显示: schoolCardId={schoolCardId}");
                return;
            }

            OnSchoolAdded?.Invoke(schoolCardId);

            if (SchoolIds.Value.Contains(schoolCardId))
            {
                LogTool.Log($"[BattleSchoolsViewModel] 刷新流派卡: {schoolCardId}");
                return;
            }
            
            SchoolIds.Value.Add(schoolCardId);
            SchoolIds.Value = new List<int>(SchoolIds.Value); // 触发通知
            LogTool.Log($"[BattleSchoolsViewModel] 添加流派卡: {schoolCardId}");
        }

        private bool ShouldDisplaySchoolCard(int schoolCardId)
        {
            var cfg = GetSchoolCardConfig(schoolCardId);
            if (cfg == null) return false;
            return cfg.Schools == null || !cfg.Schools.Contains(SpecialSchoolTypeId);
        }
    }
}
