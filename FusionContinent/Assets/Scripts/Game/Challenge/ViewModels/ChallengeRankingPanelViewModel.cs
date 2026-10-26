using System.Collections.Generic;
using Cardwar;
using Config;
using Cysharp.Threading.Tasks;
using FusionContinent;
using Game.UserTool;
using UIFramework;
using UIFramework.Services;

namespace Game.Challenge
{
    /// <summary>
    /// 排行榜面板 ViewModel（UIFramework MVVM）。
    /// 打开面板/切换页签时通过 RankService 拉取第 1 页并重建列表，
    /// 下拉刷新时追加下一页；自己排名通过 SelfRank 暴露给 View。
    /// 通过重写 GetUserToolConfig 返回挑战模块资源配置，显示 UserToolPanel。
    /// </summary>
    public class ChallengeRankingPanelViewModel : ViewModel
    {
        public const int PageSize = 10;

        #region 命令
        public Command OnCloseCommand { get; private set; }
        #endregion

        #region 事件
        /// <summary>关闭面板，由 View 订阅后执行 CloseSelf</summary>
        public System.Action OnClosePanel;
        #endregion

        /// <summary>是否正在请求，用于防止重复请求。</summary>
        public BindableProperty<bool> IsLoading { get; } =
            new BindableProperty<bool>(false);

        /// <summary>当前榜单类型，默认主线。</summary>
        public RankingType CurrentType { get; private set; } = RankingType.MainStoryline;

        /// <summary>当前累计的榜单数据（含已追加的页）。</summary>
        public IReadOnlyList<RankInfo> RankList => mRankList;

        /// <summary>自己的排名信息，未上榜时为 null 或 Rank<=0。</summary>
        public RankInfo SelfRank { get; private set; }

        /// <summary>是否还有更多页可拉取。</summary>
        public bool HasMore => mRankList.Count < mTotal;

        private readonly List<RankInfo> mRankList = new List<RankInfo>();
        private int mPage;
        private int mTotal;
        private RankService mRankService;

        public override void OnCreate()
        {
            base.OnCreate();
            OnCloseCommand = new Command(OnClose);
            mRankService = GameServiceManager.Get<RankService>();
        }

        private void OnClose()
        {
            OnClosePanel?.Invoke();
        }

        /// <summary>
        /// 拉取指定类型排行榜第 1 页并重建列表（打开面板、切换页签时调用）。
        /// </summary>
        public async UniTask<bool> RefreshRankingAsync(RankingType type)
        {
            if (mRankService == null || IsLoading.Value)
            {
                return false;
            }

            IsLoading.Value = true;
            try
            {
                var data = await mRankService.FetchRankingAsync(type, 1, PageSize);
                if (data == null)
                {
                    // 请求失败：保持原榜单类型不变
                    return false;
                }

                CurrentType = type;
                mRankList.Clear();
                mRankList.AddRange(data.List);
                SelfRank = data.SelfRank;
                mTotal = data.Total;
                mPage = 1;
                return true;
            }
            finally
            {
                IsLoading.Value = false;
            }
        }

        /// <summary>
        /// 拉取下一页并追加到列表（下拉刷新时调用）。
        /// 返回本次新增条数；没有更多数据返回 0；请求失败返回 -1。
        /// </summary>
        public async UniTask<int> LoadNextPageAsync()
        {
            if (mRankService == null || IsLoading.Value || !HasMore)
            {
                return 0;
            }

            IsLoading.Value = true;
            try
            {
                var data = await mRankService.FetchRankingAsync(CurrentType, mPage + 1, PageSize);
                if (data == null)
                {
                    return -1;
                }

                mPage = data.Page;
                mRankList.AddRange(data.List);
                SelfRank = data.SelfRank ?? SelfRank;
                mTotal = data.Total;
                return data.List.Count;
            }
            finally
            {
                IsLoading.Value = false;
            }
        }

        #region IUserToolConfigurable 实现
        public override UserToolData GetUserToolConfig()
        {
            return GlobalFunc.GetUserResourceConfig(PageType.WorldPage);
        }
        #endregion
    }
}
