using Cardwar;
using Config;
using Cysharp.Threading.Tasks;
using Game.Challenge;
using ScrollView;
using UnityEngine.Events;
using IUIData = UIFramework.IUIData;

namespace FusionContinent
{
    /// <summary>
    /// 排行榜面板 - 新框架版本 (MVVM)
    /// 复用原 ChallengeRankingPanel 的 UI 组件。
    /// 打开/切换页签时请求第 1 页并刷新列表与底部自己排名；
    /// 列表底部上拉超过阈值松手追加下一页，滚动位置保持不变。
    /// </summary>
    [UIFramework.PrefabPath("ChallengeRankingPanel")]
    public class ChallengeRankingPanelNew : UIFramework.UIPanel<ChallengeRankingPanelViewModel>
    {
        private ChallengeRankingPanel mOldPanel;
        private UIVerticalScrollViewLockRank mRankSC;
        private UnityAction mExitBtnHandler;
        private System.Action mClosePanelHandler;
        private UnityAction<bool> mStageToggleHandler;
        private UnityAction<bool> mFightToggleHandler;
        private UnityAction<bool> mRealmToggleHandler;

        void Awake()
        {
            mOldPanel = GetComponent<ChallengeRankingPanel>();
            if (mOldPanel == null)
            {
                LogTool.LogError("[ChallengeRankingPanelNew] 找不到 ChallengeRankingPanel 组件");
                return;
            }

            mRankSC = mOldPanel.RankSCView as UIVerticalScrollViewLockRank;
            if (mRankSC == null)
            {
                LogTool.LogError("[ChallengeRankingPanelNew] RankSCView 不是 UIVerticalScrollViewLockRank 组件");
            }
        }

        protected override void OnBindViewModel(ChallengeRankingPanelViewModel vm)
        {
            if (mOldPanel == null) return;

            mExitBtnHandler = () => vm.OnCloseCommand.Execute();
            mOldPanel.ExitBtn.onClick.AddListener(mExitBtnHandler);

            mClosePanelHandler = CloseSelf;
            vm.OnClosePanel += mClosePanelHandler;

            // 页签切换：选中时重新请求对应类型的排行榜
            mStageToggleHandler = isOn => { if (isOn) RefreshRanking(RankingType.MainStoryline).Forget(); };
            mFightToggleHandler = isOn => { if (isOn) RefreshRanking(RankingType.FightNum).Forget(); };
            mRealmToggleHandler = isOn => { if (isOn) RefreshRanking(RankingType.FantasyRealm).Forget(); };
            mOldPanel.StageRanking.onValueChanged.AddListener(mStageToggleHandler);
            mOldPanel.FightPowerRanking.onValueChanged.AddListener(mFightToggleHandler);
            mOldPanel.RealmRanking.onValueChanged.AddListener(mRealmToggleHandler);

            if (mRankSC != null)
            {
                mRankSC.SetUpdateFunc(OnUpdateRankItem);
                mRankSC.SetPullRefreshCallback(() => LoadNextPage().Forget());
            }
        }

        protected override void OnUnbindViewModel(ChallengeRankingPanelViewModel vm)
        {
            if (mOldPanel == null) return;

            if (mExitBtnHandler != null)
            {
                mOldPanel.ExitBtn.onClick.RemoveListener(mExitBtnHandler);
                mExitBtnHandler = null;
            }
            if (mClosePanelHandler != null)
            {
                vm.OnClosePanel -= mClosePanelHandler;
                mClosePanelHandler = null;
            }
            if (mStageToggleHandler != null)
            {
                mOldPanel.StageRanking.onValueChanged.RemoveListener(mStageToggleHandler);
                mStageToggleHandler = null;
            }
            if (mFightToggleHandler != null)
            {
                mOldPanel.FightPowerRanking.onValueChanged.RemoveListener(mFightToggleHandler);
                mFightToggleHandler = null;
            }
            if (mRealmToggleHandler != null)
            {
                mOldPanel.RealmRanking.onValueChanged.RemoveListener(mRealmToggleHandler);
                mRealmToggleHandler = null;
            }
        }

        protected override void OnOpen(IUIData uiData = null)
        {
            if (VM == null || mOldPanel == null) return;

            // 打开时按当前选中的页签请求服务器数据，默认主线榜
            var type = RankingType.MainStoryline;
            if (mOldPanel.FightPowerRanking.isOn)
            {
                type = RankingType.FightNum;
            }
            else if (mOldPanel.RealmRanking.isOn)
            {
                type = RankingType.FantasyRealm;
            }
            else if (!mOldPanel.StageRanking.isOn)
            {
                // prefab 中三个页签均未选中，默认选中主线页签（不触发回调，避免重复请求）
                mOldPanel.StageRanking.SetIsOnWithoutNotify(true);
            }

            RefreshRanking(type).Forget();
        }

        /// <summary>拉取第 1 页并整体刷新列表与自己排名栏；请求失败时恢复原页签选中态。</summary>
        private async UniTask RefreshRanking(RankingType type)
        {
            var success = await VM.RefreshRankingAsync(type);
            if (!success)
            {
                RestoreToggle(VM.CurrentType);
                return;
            }

            mRankSC?.ShowList(VM.RankList.Count);
            RefreshSelfBar();
        }

        /// <summary>请求失败时把页签选中态恢复到指定类型（不触发回调，避免重复请求）。</summary>
        private void RestoreToggle(RankingType type)
        {
            if (mOldPanel == null) return;

            if (type == RankingType.FightNum)
            {
                mOldPanel.FightPowerRanking.SetIsOnWithoutNotify(true);
            }
            else if (type == RankingType.FantasyRealm)
            {
                mOldPanel.RealmRanking.SetIsOnWithoutNotify(true);
            }
            else
            {
                mOldPanel.StageRanking.SetIsOnWithoutNotify(true);
            }
        }

        /// <summary>底部上拉加载：拉取下一页并追加，滚动位置保持不变。</summary>
        private async UniTask LoadNextPage()
        {
            if (mRankSC == null) return;

            var added = await VM.LoadNextPageAsync();
            if (added > 0)
            {
                mRankSC.AppendData(added);
            }
            else
            {
                // 0 = 没有更多数据，-1 = 请求失败
                mRankSC.EndRefresh();
            }
        }

        private void OnUpdateRankItem(int dataIndex, ScrollItem scrollItem)
        {
            if (VM == null || scrollItem == null) return;
            if (dataIndex < 0 || dataIndex >= VM.RankList.Count) return;

            var info = VM.RankList[dataIndex];
            var itemShow = scrollItem.GetComponent<RankItemShow>();
            if (info == null || itemShow == null) return;

            itemShow.SetData(ToRankItemData(info));
        }

        /// <summary>刷新底部自己排名栏。</summary>
        private void RefreshSelfBar()
        {
            var selfRank = VM.SelfRank;
            var ranked = selfRank != null && selfRank.Rank > 0;

            mOldPanel.NotInRank.gameObject.SetActive(!ranked);
            mOldPanel.RankNum.gameObject.SetActive(ranked);

            if (ranked)
            {
                mOldPanel.RankNum.text = selfRank.Rank.ToString();
                mOldPanel.PlayerHead.SetData(new PlayerHeadData
                {
                    Avatar = selfRank.UserInfo.Avatar,
                    AvatarFrame = selfRank.UserInfo.AvatarFrame,
                });
                mOldPanel.PlayerName.text = selfRank.UserInfo.PlayerName;
                mOldPanel.DetailData.text = selfRank.Score.ToString();
            }
            else
            {
                // 未上榜：显示本地玩家头像与名字
                mOldPanel.PlayerHead.SetData(Global.User);
                mOldPanel.PlayerName.text = Global.User.UserName;
                mOldPanel.DetailData.text = "";
            }
        }

        private static RankItemData ToRankItemData(RankInfo info)
        {
            return new RankItemData
            {
                rankNum = info.Rank,
                headData = new PlayerHeadData
                {
                    Avatar = info.UserInfo.Avatar,
                    AvatarFrame = info.UserInfo.AvatarFrame,
                },
                playerName = info.UserInfo.PlayerName,
                detailData = info.Score.ToString(),
            };
        }

        protected override void OnShow() { }
        protected override void OnHide() { }
        protected override void OnClose() { }
    }
}
