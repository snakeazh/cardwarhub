using UIFramework;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UIFramework.Services;
using Config;
using FusionContinent;
using FusionContinent.Model;

namespace Game.Equip
{
    /// <summary>
    /// 融合石面板 ViewModel
    /// </summary>
    public class FusionGemPanelViewModel : ViewModel
    {
        #region 属性
        public BindableProperty<int> SoldierId { get; } = new BindableProperty<int>(0);
        public BindableProperty<List<PlayerFusionGem>> WearFusionGemList { get; } = new BindableProperty<List<PlayerFusionGem>>();
        public BindableProperty<List<PlayerFusionGem>> FusionGemList { get; } = new BindableProperty<List<PlayerFusionGem>>();
        #endregion

        #region 命令
        public Command OnCloseCommand { get; private set; }
        public Command<int> OnOpenDetailsCommand { get; private set; }
        public Command<int> OnOpenFusionCommand { get; private set; }
        public Command OnOpenFragmentCommand { get; private set; }
        #endregion

        #region 依赖
        private EquipService mEquipService;
        #endregion

        public override void OnCreate()
        {
            base.OnCreate();
            mEquipService = GameServiceManager.Get<EquipService>();

            // 初始化命令
            OnCloseCommand = new Command(OnClose);
            OnOpenDetailsCommand = new Command<int>(OnOpenDetails);
            OnOpenFusionCommand = new Command<int>(OnOpenFusion);
            OnOpenFragmentCommand = new Command(OnOpenFragment);

            // 初始化空列表
            WearFusionGemList.Value = new List<PlayerFusionGem>();
            FusionGemList.Value = new List<PlayerFusionGem>();
        }

        /// <summary>
        /// 初始化数据
        /// </summary>
        public void Initialize(int soldierId)
        {
            if (soldierId == 0)
            {
                Toast.Show("英灵未解锁");
                OnCloseRequested?.Invoke();
                return;
            }

            SoldierId.Value = soldierId;

            // 刷新数据
            RefreshData();
        }

        /// <summary>
        /// 刷新数据
        /// </summary>
        public void RefreshData()
        {
            int soldierId = SoldierId.Value;
            if (soldierId == 0) return;

            // 已佩戴的装备列表
            var wearEquipList = Global.PlayerEquips.Values.Where(v => v.WearerId == soldierId).ToList();

            // 已佩戴的融合石（通过装备查找）
            // WearFusionGemList.Value = wearEquipList
            //     .Select(equip => Global.PlayerFusionGems.Values.FirstOrDefault(gem => gem.WearerId == equip.Id))
            //     .Where(gem => gem != null)
            //     .OrderByDescending(x => x.Quality)
            //     .ToList();

            // 未佩戴的融合石（按品质降序排列）
            // FusionGemList.Value = Global.PlayerFusionGems.Values
            //     .Where(v => v.WearerId == 0 && v.Level < v.Config.LevelList.Count)
            //     .OrderByDescending(x => x.Quality)
            //     .ToList();
        }

        #region 命令实现

        private void OnClose()
        {
            OnCloseRequested?.Invoke();
        }

        private void OnOpenDetails(int gemId)
        {
            OnOpenDetailsRequested?.Invoke(gemId, SoldierId.Value);
        }

        private void OnOpenFusion(int soldierId)
        {
            OnOpenFusionRequested?.Invoke(soldierId);
        }

        private void OnOpenFragment()
        {
            OnOpenFragmentRequested?.Invoke(SoldierId.Value);
        }

        #endregion

        #region 事件
        public System.Action OnCloseRequested;
        public System.Action<int, int> OnOpenDetailsRequested;
        public System.Action<int> OnOpenFusionRequested;
        public System.Action<int> OnOpenFragmentRequested;
        #endregion
    }
}
