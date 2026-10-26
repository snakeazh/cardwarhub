using System.Collections.Generic;
using CardDrop;
using Config;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using FusionContinent.Animation;
using Game.CardGroup;
using QFramework;
using TMPro;
using UIFramework;
using UnityEngine;
using UnityEngine.UI;

namespace FusionContinent
{
    /// <summary>
    /// 编队界面 - MVVM 版本。数据与业务逻辑在 <see cref="CardGroupPanelViewModel"/>，
    /// 本类只负责拖拽交互、槽位刷新、Spine 加载与选将面板显示。
    /// （技能/奥术卡子面板功能已下线。）
    /// </summary>
    [PrefabPath("CardGroupPanel")]
    public class CardGroupPanelNew : UIPanel<CardGroupPanelViewModel>
    {
        private static readonly string[] CareerFilterNames = { "全部", "射手", "战士", "法师", "刺客", "辅助" };

        private CardGroupPanel mOldPanel;
        private IChoosePanel mChooseSoliderPanel;

        private DragBootstrap mDragBootstrap;
        private DraggableItem[] mDraggableItems;
        private DragSlot[] mDraggableSlots;

        private readonly Dictionary<int, GameObject> mSoliderSpineCache = new Dictionary<int, GameObject>();
        private bool mSpineCleared;

        private TbCardBaseConfig mCardBaseConfig;
        private TextMeshProUGUI mSoldierNumText;
        private bool mFightNumInitialized;

        void Awake()
        {
            mOldPanel = GetComponent<CardGroupPanel>();
            if (mOldPanel == null)
            {
                LogTool.LogError("[CardGroupPanelNew] 找不到 CardGroupPanel 组件");
                return;
            }

            mCardBaseConfig = DataConfigManager.Tables.TbCardBaseConfig;
        }

        protected override void OnBindViewModel(CardGroupPanelViewModel vm)
        {
            if (mOldPanel == null) return;

            mOldPanel.ReturnBtn.onClick.AddListener(OnReturnBtnClicked);
            mOldPanel.StartGameBtn.onClick.AddListener(OnStartGameBtnClicked);
            mOldPanel.RecommandCardBtn.onClick.AddListener(OnRecommendBtnClicked);
            mOldPanel.SynergyBg.onClick.AddListener(OnSynergyBgClicked);
            mOldPanel.CardBtn.onValueChanged.AddListener(OnCardPanelChange);
            mOldPanel.BattleBtn.onValueChanged.AddListener(OnBattlePanelChange);

            UIEventListener.Get(mOldPanel.ChooseHero.gameObject).onClick += OnChooseHeroClicked;

            vm.LevelName.OnValueChanged += OnLevelNameChanged;
            vm.LevelNameNum.OnValueChanged += OnLevelNameNumChanged;
            vm.FightNum.OnValueChanged += OnFightNumChanged;
            vm.SoldierCount.OnValueChanged += OnSoldierCountChanged;

            vm.OnCloseRequested += OnCloseRequested;
            vm.OnStartBattleRequested += OnStartBattleRequested;
            vm.OnOpenRecommendRequested += OnOpenRecommendRequested;
            vm.OnSoldierListChanged += OnSoldierListChanged;

            // 选将子面板初始化：直接引用本面板
            mChooseSoliderPanel = mOldPanel.ChooseHeroPanel;
            mChooseSoliderPanel.OnInit(this, CardType.Soldier);

            mSoldierNumText = mOldPanel.ChooseHero.Find("Num").GetComponent<TextMeshProUGUI>();

            SetClickAreaEnable(false);
            InitSoldierDragSlots();
            InitDropdown();
            mOldPanel.Dropdown.onValueChanged.AddListener(OnDropdownValueChanged);

            // VM 数据在打开前已 SetData 完成，按当前值初始渲染一次
            mOldPanel.LevelNameText.text = vm.LevelName.Value;
            mOldPanel.LevelNameTextNum.text = vm.LevelNameNum.Value;
            mFightNumInitialized = false;
            OnFightNumChanged(0, vm.FightNum.Value);
            OnSoldierCountChanged(0, vm.SoldierCount.Value);
            RefreshTeamSolider();
        }

        protected override void OnUnbindViewModel(CardGroupPanelViewModel vm)
        {
            if (mOldPanel == null) return;

            mOldPanel.ReturnBtn.onClick.RemoveListener(OnReturnBtnClicked);
            mOldPanel.StartGameBtn.onClick.RemoveListener(OnStartGameBtnClicked);
            mOldPanel.RecommandCardBtn.onClick.RemoveListener(OnRecommendBtnClicked);
            mOldPanel.SynergyBg.onClick.RemoveListener(OnSynergyBgClicked);
            mOldPanel.CardBtn.onValueChanged.RemoveListener(OnCardPanelChange);
            mOldPanel.BattleBtn.onValueChanged.RemoveListener(OnBattlePanelChange);
            mOldPanel.Dropdown.onValueChanged.RemoveListener(OnDropdownValueChanged);

            UIEventListener.Get(mOldPanel.ChooseHero.gameObject).onClick -= OnChooseHeroClicked;

            if (mDragBootstrap != null)
            {
                mDragBootstrap.SlotClicked -= OnSoldierSlotClicked;
            }

            vm.LevelName.OnValueChanged -= OnLevelNameChanged;
            vm.LevelNameNum.OnValueChanged -= OnLevelNameNumChanged;
            vm.FightNum.OnValueChanged -= OnFightNumChanged;
            vm.SoldierCount.OnValueChanged -= OnSoldierCountChanged;

            vm.OnCloseRequested -= OnCloseRequested;
            vm.OnStartBattleRequested -= OnStartBattleRequested;
            vm.OnOpenRecommendRequested -= OnOpenRecommendRequested;
            vm.OnSoldierListChanged -= OnSoldierListChanged;
        }

        #region 按钮与事件回调

        private void OnReturnBtnClicked()
        {
            SyncSlotMappingToVM();
            VM.OnCloseCommand.Execute();
        }

        private void OnStartGameBtnClicked()
        {
            SyncSlotMappingToVM();
            VM.OnStartGameCommand.Execute();
        }

        private void OnRecommendBtnClicked()
        {
            VM.OnRecommendCommand.Execute();
        }

        private void OnCloseRequested()
        {
            CloseSelf();
        }

        private async void OnStartBattleRequested(int stageId)
        {
            await GlobalFunc.StartBattle(stageId, null, null, () =>
            {
                UnlockGuideManager.Send(ModuleFunctionType.SetCardGroup, 3);
            });
        }

        private void OnOpenRecommendRequested()
        {
            UnlockGuideManager.Send(ModuleFunctionType.SetCardGroup, 1);
            UIKit.OpenPanel<CardGroupRecommend>(QFramework.UILevel.PopUI);
        }

        private void OnSoldierListChanged()
        {
            mChooseSoliderPanel.Refresh();
            RefreshTeamSolider();
        }

        private void OnLevelNameChanged(string oldValue, string newValue)
        {
            mOldPanel.LevelNameText.text = newValue;
        }

        private void OnLevelNameNumChanged(string oldValue, string newValue)
        {
            mOldPanel.LevelNameTextNum.text = newValue;
        }

        private void OnFightNumChanged(int oldValue, int newValue)
        {
            if (!mFightNumInitialized)
            {
                mFightNumInitialized = true;
                mOldPanel.PowerNumText.Init(newValue);
            }
            else
            {
                mOldPanel.PowerNumText.Play(newValue);
            }
        }

        private void OnSoldierCountChanged(int oldValue, int newValue)
        {
            mSoldierNumText.text = $"{newValue}/{VM.SoldierMaxCount}";
        }

        private void OnDropdownValueChanged(int index)
        {
            mOldPanel.ChooseHeroPanel.SetCareerFilter(index);
        }

        #endregion

        #region 选将面板切换

        private void OnChooseHeroClicked()
        {
            ShowSoliderPanel();
        }

        public void ShowSoliderPanel()
        {
            mChooseSoliderPanel.Show();
            SetClickAreaEnable(true);
            SetCardConfigToCardType(CardType.Soldier);
        }

        private void SetCardConfigToCardType(CardType cardType)
        {
            if (cardType == CardType.Soldier)
            {
                mOldPanel.CardLayout.SetAnchorPosY(mOldPanel.ChooseHeroPanel.PositionY);
            }

            mOldPanel.ChooseHero.gameObject.SetActive(cardType == CardType.Soldier);
        }

        private void SetCardConfigToCardType()
        {
            mOldPanel.ChooseHero.gameObject.SetActive(true);
        }

        private void SetClickAreaEnable(bool status)
        {
            mOldPanel.ClickArea.gameObject.SetActive(status);
        }

        #endregion

        #region 页签与下拉框

        private void OnBattlePanelChange(bool value)
        {
            if (value)
            {
                mOldPanel.BattleBtn.transform.Find("Bg").gameObject.SetActive(true);
                mOldPanel.BattleBtn.transform.Find("Text").GetComponent<TextMeshProUGUI>().alpha = 1f;
                mOldPanel.BattleInfoPanel.gameObject.SetActive(true);
                mOldPanel.BattleInfoPanel.Show(VM.CurrentStageId);
                mOldPanel.ChooseHeroPanel.gameObject.transform.LocalPositionX(9999);
                mOldPanel.Dropdown.gameObject.SetActive(false);
            }
            else
            {
                mOldPanel.BattleBtn.transform.Find("Bg").gameObject.SetActive(false);
                mOldPanel.BattleBtn.transform.Find("Text").GetComponent<TextMeshProUGUI>().alpha = 0.3f;
                mOldPanel.BattleInfoPanel.gameObject.SetActive(false);
                mOldPanel.ChooseHeroPanel.gameObject.transform.LocalPositionX(0);
                mOldPanel.Dropdown.gameObject.SetActive(true);
            }
        }

        private void OnCardPanelChange(bool value)
        {
            if (value)
            {
                mOldPanel.CardBtn.transform.Find("Bg").gameObject.SetActive(true);
                mOldPanel.CardBtn.transform.Find("Text").GetComponent<TextMeshProUGUI>().alpha = 1f;
                mOldPanel.CardConfigPanel.gameObject.SetActive(true);
                SetCardConfigToCardType();
            }
            else
            {
                mOldPanel.CardBtn.transform.Find("Bg").gameObject.SetActive(false);
                mOldPanel.CardBtn.transform.Find("Text").GetComponent<TextMeshProUGUI>().alpha = 0.3f;
                mOldPanel.CardConfigPanel.gameObject.SetActive(false);
            }
        }

        private void InitDropdown()
        {
            if (mOldPanel.Dropdown == null) return;

            mOldPanel.Dropdown.ClearOptions();
            var options = new List<TMP_Dropdown.OptionData>();
            foreach (var name in CareerFilterNames)
            {
                options.Add(new TMP_Dropdown.OptionData(name));
            }
            mOldPanel.Dropdown.AddOptions(options);
            mOldPanel.Dropdown.value = 0;
            mOldPanel.Dropdown.RefreshShownValue();
        }

        #endregion

        #region 羁绊 Tip

        private void OnSynergyBgClicked()
        {
            bool willShow = !mOldPanel.TipDetail.gameObject.activeSelf;
            mOldPanel.TipDetail.gameObject.SetActive(true);

            if (willShow)
            {
                var tipDetail = mOldPanel.TipDetail.GetComponent<TipDetail>();
                var bindingCfg = VM.GetCurrentBindingConfig(out int activeCount);
                tipDetail.SetData(bindingCfg, activeCount);

                mOldPanel.TipDetail.transform.localScale = Vector3.zero;
                mOldPanel.TipDetail.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);
            }
            else
            {
                mOldPanel.TipDetail.transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack)
                    .OnComplete(() => mOldPanel.TipDetail.gameObject.SetActive(false));
            }
        }

        #endregion

        #region 士兵拖拽槽位

        private void InitSoldierDragSlots()
        {
            var scrollContent = mOldPanel.SoliderContent;
            mDragBootstrap = scrollContent.GetComponent<DragBootstrap>();
            mDragBootstrap.SlotClicked += OnSoldierSlotClicked;

            int slotMax = CardGroupPanelViewModel.SoldierSlotMax;
            mDraggableItems = new DraggableItem[slotMax];
            mDraggableSlots = new DragSlot[slotMax];

            for (int i = 0; i < slotMax; i++)
            {
                mDraggableItems[i] = scrollContent.Find("Item" + i).GetComponent<DraggableItem>();
                mDraggableSlots[i] = scrollContent.GetChild(i).GetComponent<DragSlot>();
                mDraggableSlots[i].SetActive(true);
                scrollContent.GetChild(i).name = i.ToString();
            }

            // VM 已在 SetData 中按 CardsPosition 重排列表并重置映射，这里应用恒等映射
            mDragBootstrap.SetValuesAndRefresh(VM.GetSlotValues(), VM.GetSlotValues());
        }

        private void OnSoldierSlotClicked(DragSlot slot)
        {
            if (mChooseSoliderPanel.Status)
            {
                int index = int.Parse(slot.name);
                int slotValue = mDraggableSlots[index].SlotValue;
                VM.ReCallCardIndex(CardType.Soldier, slotValue);
            }
            else
            {
                ShowSoliderPanel();
            }
        }

        /// <summary>
        /// 拖拽换位后槽位映射以 DragBootstrap 当前状态为准。
        /// 注意：不能在 DragSwapSuccess 事件里同步——该事件在交换执行前触发，
        /// 且此时被拖槽位的 SlotValue 已被清零，读到的是损坏的映射。
        /// </summary>
        private void SyncSlotMappingToVM()
        {
            if (mDragBootstrap != null)
            {
                VM.SetSoldierSlotMapping(mDragBootstrap.GetAllSlotValues());
            }
        }

        public void RefreshTeamSolider()
        {
            foreach (var item in mSoliderSpineCache)
            {
                item.Value.transform.SetParent(mOldPanel.SpineCache);
                item.Value.transform.localPosition = Vector3.zero;
            }

            for (int pos = 0; pos < mDraggableItems.Length; pos++)
            {
                var posCardId = VM.TeamSoldierIdList[mDraggableItems[pos].ItemValue];
                var cardCfg = posCardId > 0 ? mCardBaseConfig.Get(posCardId) : null;

                var iconTf = mDraggableItems[pos].transform.Find("MoveNode");
                iconTf.gameObject.SetActive(cardCfg != null);

                if (cardCfg != null)
                {
                    LoadSpine(cardCfg.Id, iconTf);
                }
            }

            RefreshSynergyActivation(VM.SoldierCount.Value);
        }

        private void RefreshSynergyActivation(int soldierCount)
        {
            var actTrans = new[] { mOldPanel.Act1, mOldPanel.Act2, mOldPanel.Act3, mOldPanel.Act4 };
            for (int i = 0; i < actTrans.Length; i++)
            {
                if (actTrans[i] == null) continue;
                actTrans[i].gameObject.SetActive(soldierCount > i);
            }
        }

        #endregion

        #region 士兵 Spine

        private async UniTaskVoid LoadSpine(int soldierId, Transform parentTransform)
        {
            var cfg = DataConfigManager.Tables.TbSoldierBattleConfig.Get(soldierId);
            if (mSoliderSpineCache.TryGetValue(soldierId, out var cached))
            {
                cached.transform.SetParent(parentTransform);
                cached.transform.localScale = Vector3.one * cfg.SpineScale;
                cached.transform.localPosition = new Vector3(0, -100);
                return;
            }

            var handle = YooAsset.YooAssets.LoadAssetAsync<GameObject>(cfg.Spine);
            await handle;

            // 加载期间面板已关闭或缓存已清理，丢弃结果
            if (mSpineCleared) return;

            if (!mSoliderSpineCache.ContainsKey(soldierId))
            {
                mSoliderSpineCache.Add(soldierId, Instantiate(handle.GetAssetObject<GameObject>(), mOldPanel.SpineCache));
            }

            mSoliderSpineCache[soldierId].transform.SetParent(parentTransform);
            mSoliderSpineCache[soldierId].transform.localScale = Vector3.one * cfg.SpineScale;
            mSoliderSpineCache[soldierId].transform.localPosition = new Vector3(0, -100);
        }

        private void ClearSpineCache()
        {
            mSpineCleared = true;
            foreach (var kv in mSoliderSpineCache)
            {
                if (kv.Value != null)
                {
                    Destroy(kv.Value);
                }
            }
            mSoliderSpineCache.Clear();
        }

        #endregion

        #region 对外契约（选将子面板/推荐卡组调用）

        public void Deploy(CardType cardType, int cardId, bool needUpdate = true)
        {
            SyncSlotMappingToVM();
            VM.Deploy(cardType, cardId, needUpdate);
        }

        public void SelectCards(List<int> cards)
        {
            SyncSlotMappingToVM();
            VM.SelectCards(cards);
        }

        public int CheckCardInTeam(CardType cardType, int cardId)
        {
            return VM.CheckCardInTeam(cardType, cardId);
        }

        public bool CheckCardSkillSoliderInTeam(int cardId)
        {
            return VM.CheckCardSkillSoliderInTeam(cardId);
        }

        #endregion

        #region 生命周期

        protected override void OnOpen(UIFramework.IUIData uiData = null)
        {
            // UIFramework 首次打开只走 OnOpen 不走 OnShow，选将面板在这里也要初始化
            ShowSoliderPanel();
        }

        protected override void OnShow()
        {
            ShowSoliderPanel();
        }

        protected override void OnClose()
        {
            ClearSpineCache();
            if (mOldPanel != null && mOldPanel.TipDetail != null)
            {
                mOldPanel.TipDetail.transform.DOKill();
            }
        }

        #endregion
    }
}
