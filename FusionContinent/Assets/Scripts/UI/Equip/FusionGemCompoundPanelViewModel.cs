using System.Collections.Generic;
using Config;
using Cysharp.Threading.Tasks;
using FusionContinent.Model;
using Game.Equip.RedDot;
using UIFramework;

namespace FusionContinent
{
    /// <summary>
    /// 融合石合成面板 ViewModel
    /// </summary>
    public class FusionGemCompoundPanelViewModel : ViewModel
    {
        /// <summary>
        /// 主宝石流水 id（外部传入，不参与列表）
        /// </summary>
        public BindableProperty<int> MainGemId { get; } = new BindableProperty<int>(0);

        /// <summary>
        /// 次宝石候选列表（不含主宝石）
        /// </summary>
        public BindableProperty<List<PlayerFusionGem>> FusionGemList { get; } =
            new BindableProperty<List<PlayerFusionGem>>(new List<PlayerFusionGem>());

        /// <summary>
        /// 次宝石选中流水 id
        /// </summary>
        public BindableProperty<int> SecondaryGemId { get; } = new BindableProperty<int>(0);

        #region 命令

        public Command OnCloseCommand { get; private set; }
        public Command OnAdvancedCommand { get; private set; }
        public Command<int> OnSelectGemCommand { get; private set; }

        #endregion

        #region 事件

        public System.Action OnCloseRequested;

        /// <summary>
        /// 主宝石顶部展示统一刷新（图标、名称、进度区、概率文案等）
        /// </summary>
        public System.Action OnMainGemPresentationRefreshRequested;

        #endregion

        private TbGemFusionConfig mTbGemFusionConfig;
        private System.Action<object> mFusionGemWearChangedHandler;

        public override void OnCreate()
        {
            base.OnCreate();

            OnCloseCommand = new Command(OnClose);
            OnAdvancedCommand = new Command(OnAdvanced);
            OnSelectGemCommand = new Command<int>(OnSelectGem);

            mFusionGemWearChangedHandler = OnFusionGemWearChanged;
            Global.Publisher.Subscribe(MessagePublisher.FUSION_GEM_WEAR_CHANGED, mFusionGemWearChangedHandler);
        }

        public override void OnDestroy()
        {
            if (mFusionGemWearChangedHandler != null)
            {
                Global.Publisher.Unsubscribe(MessagePublisher.FUSION_GEM_WEAR_CHANGED, mFusionGemWearChangedHandler);
                mFusionGemWearChangedHandler = null;
            }

            base.OnDestroy();
        }

        public void Initialize(int mainGemId)
        {
            MainGemId.Value = mainGemId;
            RefreshGemList();
            RefreshMainGemPresentation();
        }

        public void RefreshMainGemPresentation()
        {
            OnMainGemPresentationRefreshRequested?.Invoke();
        }

        public void RefreshGemList()
        {
            if (mTbGemFusionConfig == null)
            {
                mTbGemFusionConfig = DataConfigManager.Tables.TbGemFusionConfig;
            }

            var displayed = new List<PlayerFusionGem>();
            var previousSecondaryId = SecondaryGemId.Value;
            var mainGemId = MainGemId.Value;
            var mainGem = TryGetMainGem();
            if (mainGem == null)
            {
                SecondaryGemId.Value = 0;
                FusionGemList.Value = displayed;
                return;
            }

            if (Global.PlayerFusionGems != null && Global.PlayerFusionGems.Count > 0)
            {
                foreach (var gem in Global.PlayerFusionGems.Values)
                {
                    if (gem == null)
                    {
                        continue;
                    }

                    if (mainGemId > 0 && gem.Id == mainGemId)
                    {
                        continue;
                    }

                    if (gem.Config == null && mTbGemFusionConfig != null && gem.GemId > 0)
                    {
                        gem.Config = mTbGemFusionConfig.GetOrDefault(gem.GemId);
                    }

                    if (gem.Config == null)
                    {
                        continue;
                    }

                    gem.Quality = gem.Config.Quality;

                    if (mainGem != null && !FusionGemBreakthroughRedDotHelper.CanBeConsumedAsSecondary(mainGem, gem))
                    {
                        continue;
                    }

                    displayed.Add(gem);
                }
            }

            displayed.Sort((a, b) =>
            {
                var q = b.Quality.CompareTo(a.Quality);
                if (q != 0)
                {
                    return q;
                }

                var l = b.Level.CompareTo(a.Level);
                if (l != 0)
                {
                    return l;
                }

                return b.Id.CompareTo(a.Id);
            });

            if (displayed.Count == 0)
            {
                SecondaryGemId.Value = 0;
                FusionGemList.Value = displayed;
                return;
            }

            var stillSelected = previousSecondaryId > 0 && displayed.Exists(g => g.Id == previousSecondaryId);
            var newSecondaryId = stillSelected ? previousSecondaryId : 0;

            SecondaryGemId.Value = newSecondaryId;
            FusionGemList.Value = displayed;
        }

        private void OnFusionGemWearChanged(object message)
        {
            RefreshGemList();
            RefreshMainGemPresentation();
        }

        private void OnClose()
        {
            OnCloseRequested?.Invoke();
        }

        private void OnAdvanced()
        {
            var mainGem = TryGetMainGem();
            if (mainGem == null)
            {
                Toast.Error("融合石数据无效");
                return;
            }

            if (mainGem.IsMaxLevel())
            {
                Toast.Show("融合石升到最高阶之后无法继续升阶");
                return;
            }

            if (SecondaryGemId.Value <= 0)
            {
                Toast.Show("请选择次宝石");
                return;
            }

            var confirmVm = new FusionCompoundConfirmPanelViewModel();
            confirmVm.OnCreate();
            confirmVm.Initialize(MainGemId.Value, SecondaryGemId.Value);
            UIManager
                .OpenPanelAsync<FusionCompoundConfirmPanelNew, FusionCompoundConfirmPanelViewModel>(
                    confirmVm, UIFramework.UILevel.PopUI)
                .Forget();
        }

        private void OnSelectGem(int gemId)
        {
            if (gemId <= 0)
            {
                return;
            }

            if (gemId == MainGemId.Value)
            {
                return;
            }

            if (gemId == SecondaryGemId.Value)
            {
                return;
            }

            var mainGem = TryGetMainGem();
            if (mainGem == null || !Global.PlayerFusionGems.TryGetValue(gemId, out var candidate) || candidate == null)
            {
                return;
            }

            if (!FusionGemBreakthroughRedDotHelper.CanBeConsumedAsSecondary(mainGem, candidate))
            {
                return;
            }

            SecondaryGemId.Value = gemId;
        }

        private PlayerFusionGem TryGetMainGem()
        {
            if (MainGemId.Value <= 0 || Global.PlayerFusionGems == null)
            {
                return null;
            }

            return Global.PlayerFusionGems.TryGetValue(MainGemId.Value, out var gem) ? gem : null;
        }
    }
}
