using System.Collections.Generic;
using Config;
using Game.Battle;
using Manager;
using QFramework;
using UIFramework;
using UnityEngine;
using UnityEngine.UI;
using IUIData = UIFramework.IUIData;

namespace FusionContinent
{
    [PrefabPath("GamePausePanel")]
    public class GamePausePanelNew : UIPanel<GamePausePanelViewModel>
    {
        private GamePausePanel mOldPanel;
        private readonly Dictionary<int, GameObject> mSchoolCards = new Dictionary<int, GameObject>();
        private readonly List<GameObject> mRewardCards = new List<GameObject>();

        private TbSchoolCardConfig mSchoolCardConfig;
        private TbMonsterGenConfig mMonsterGenConfig;

        private UnityEngine.Events.UnityAction mBackBtnHandler;
        private UnityEngine.Events.UnityAction mContinueBtnHandler;
        private UnityEngine.Events.UnityAction mSchoolBtnHandler;
        private UnityEngine.Events.UnityAction mRewardBtnHandler;

        void Awake()
        {
            mOldPanel = GetComponent<GamePausePanel>();
            if (mOldPanel == null)
            {
                LogTool.LogError("[GamePausePanelNew] 找不到 GamePausePanel 组件");
                return;
            }
            mOldPanel.enabled = false;
            mSchoolCardConfig = DataConfigManager.Tables.TbSchoolCardConfig;
            mMonsterGenConfig = DataConfigManager.Tables.TbMonsterGenConfig;
        }

        protected override void OnBindViewModel(GamePausePanelViewModel vm)
        {
            if (mOldPanel == null) return;

            // 撤退按钮
            mBackBtnHandler = () =>
            {
                DialogManager.ShowConfirm("确认撤退?", "", () =>
                {
                    GamePlayManager.Default.BackHome();
                    vm.OnBackCommand.Execute();
                    CloseSelf();
                });
            };
            mOldPanel.BackBtn.onClick.AddListener(mBackBtnHandler);

            // 继续按钮
            mContinueBtnHandler = () =>
            {
                vm.OnContinueCommand.Execute();
                CloseSelf();
                GamePlayManager.Default.StartGame();
            };
            mOldPanel.ContineBtn.onClick.AddListener(mContinueBtnHandler);

            // 学院卡牌 Tab
            mSchoolBtnHandler = () => vm.OnSelectSchoolTabCommand.Execute();
            mOldPanel.SchoolBtn.onClick.AddListener(mSchoolBtnHandler);

            // 奖励预览 Tab
            mRewardBtnHandler = () => vm.OnSelectRewardTabCommand.Execute();
            mOldPanel.RewardBtn.onClick.AddListener(mRewardBtnHandler);

            vm.OnSelectSchoolTabRequested += RefreshSchoolTab;
            vm.OnSelectRewardTabRequested += RefreshRewardTab;
        }

        protected override void OnUnbindViewModel(GamePausePanelViewModel vm)
        {
            if (mOldPanel == null) return;

            if (mOldPanel.BackBtn != null && mBackBtnHandler != null)
                mOldPanel.BackBtn.onClick.RemoveListener(mBackBtnHandler);
            if (mOldPanel.ContineBtn != null && mContinueBtnHandler != null)
                mOldPanel.ContineBtn.onClick.RemoveListener(mContinueBtnHandler);
            if (mOldPanel.SchoolBtn != null && mSchoolBtnHandler != null)
                mOldPanel.SchoolBtn.onClick.RemoveListener(mSchoolBtnHandler);
            if (mOldPanel.RewardBtn != null && mRewardBtnHandler != null)
                mOldPanel.RewardBtn.onClick.RemoveListener(mRewardBtnHandler);

            mBackBtnHandler = null;
            mContinueBtnHandler = null;
            mSchoolBtnHandler = null;
            mRewardBtnHandler = null;

            vm.OnSelectSchoolTabRequested -= RefreshSchoolTab;
            vm.OnSelectRewardTabRequested -= RefreshRewardTab;
        }

        protected override void OnOpen(IUIData uiData = null)
        {
            // 默认打开学院卡牌页签，直接刷新避免 ViewModel 守卫拦截
            RefreshSchoolTab();
        }

        protected override void OnShow()
        {
        }

        protected override void OnHide()
        {
            ReleaseRewardCards();
        }

        private void RefreshSchoolTab()
        {
            if (mOldPanel == null) return;

            mOldPanel.SchoolBtn.transform.Find("Select").localScale = Vector3.one;
            mOldPanel.RewardBtn.transform.Find("Select").localScale = Vector3.zero;
            mOldPanel.SchoolCardScroll.gameObject.SetActive(true);
            mOldPanel.RewardCardScroll.gameObject.SetActive(false);

            InitSchoolCards();
        }

        private void RefreshRewardTab()
        {
            if (mOldPanel == null) return;

            mOldPanel.SchoolBtn.transform.Find("Select").localScale = Vector3.zero;
            mOldPanel.RewardBtn.transform.Find("Select").localScale = Vector3.one;
            mOldPanel.SchoolCardScroll.gameObject.SetActive(false);
            mOldPanel.RewardCardScroll.gameObject.SetActive(true);

            InitRewardCards();
        }

        private void InitSchoolCards()
        {
            var scroll = mOldPanel.SchoolCardScroll.transform.Find("Viewport/Content");
            var schoolCards = SchoolManager.Instance.GetSchoolCardAll();
            if (schoolCards == null || schoolCards.Count <= 0) return;

            var templateTransform = scroll.Find("ChooseCard");
            if (templateTransform == null) return;
            var template = templateTransform.gameObject;

            int index = 0;
            foreach (var v in schoolCards.Values)
            {
                if (!mSchoolCards.TryGetValue(v.CardId, out var obj))
                {
                    // 首张卡复用模板，后续 Instantiate
                    obj = index < scroll.childCount
                        ? template
                        : Instantiate(template, scroll);

                    obj.SetActive(true);
                    mSchoolCards[v.CardId] = obj;
                }

                var schoolCardCfg = mSchoolCardConfig.GetOrDefault(v.CardId);
                if (schoolCardCfg != null)
                {
                    obj.transform.Find("BuffIcon").GetComponent<Image>().sprite =
                        Global.SchoolChooseCardIconSpriteAtlas.GetSpriteExt(schoolCardCfg.Icon);
                    obj.transform.Find("Text").GetComponent<TMPro.TextMeshProUGUI>().text = schoolCardCfg.Name;
                }

                index++;
            }
        }

        private void InitRewardCards()
        {
            ReleaseRewardCards();

            int stageId = Global.StageId.Value;
            int cityLevel = Global.CityLevel.Value;
            var genCfg = mMonsterGenConfig.GetOrDefault(stageId);
            if (genCfg == null || genCfg.StageList.Count < cityLevel) return;

            var rewards = genCfg.StageList[(cityLevel - 1 < 0) ? 0 : cityLevel - 1].Rewards;
            Transform parent = mOldPanel.RewardCardScroll.transform.Find("Viewport/Content");
            if (parent == null) return;

            var template = parent.childCount > 0 ? parent.GetChild(0) : null;
            if (template == null) return;

            for (int i = 0; i < rewards.Count; i++)
            {
                GameObject rewardObj;
                if (i < parent.childCount)
                {
                    rewardObj = parent.GetChild(i).gameObject;
                }
                else
                {
                    rewardObj = Instantiate(template.gameObject, parent);
                }

                rewardObj.SetActive(true);
                rewardObj.transform.localScale = Vector3.one;
                rewardObj.transform.localPosition = Vector3.zero;
                rewardObj.GetComponent<RewardEntity>().SetData(rewards[i].Id, rewards[i].Num);
                mRewardCards.Add(rewardObj);
            }
        }

        private void ReleaseRewardCards()
        {
            if (mRewardCards.Count == 0) return;

            var pool = CommonItemPoolManager.Instance.GetGameObjectPoolOrCreate("RewardEntity");
            for (int i = mRewardCards.Count - 1; i >= 0; i--)
            {
                if (mRewardCards[i] != null)
                {
                    mRewardCards[i].SetActive(false);
                    pool.Return(mRewardCards[i]);
                }
            }
            mRewardCards.Clear();
        }
    }
}
