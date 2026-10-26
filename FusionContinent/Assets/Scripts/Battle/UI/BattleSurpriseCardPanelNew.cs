using Game.Battle;
using UIFramework;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Config;
using DG.Tweening;
using QFramework;
using TMPro;
using IUIData = UIFramework.IUIData;
using UIRoot = UIFramework.UIRoot;

namespace FusionContinent
{
    [PrefabPath("BattleSurpriseCardPanel")]
    /// <summary>
    /// 战斗惊喜卡牌子界面 - 新框架版本
    /// 管理惊喜卡牌的显示
    /// 作为 BattlePanelNew 的子界面，不参与 UIManager 堆栈管理
    /// </summary>
    public class BattleSurpriseCardPanelNew : UIPanel<BattleSurpriseCardViewModel>
    {
        /// <summary>
        /// 作为子界面，不参与UIManager堆栈管理
        /// </summary>
        public override PanelStackMode StackMode => PanelStackMode.Unmanaged;

        // 引用 BattleSurpriseCardPanel 的 UI 组件
        private BattleSurpriseCardPanel mOldPanel;

        // 缓存惊喜卡牌对象
        private Dictionary<int, GameObject> _surpriseCards = new Dictionary<int, GameObject>();

        // 事件处理器
        private System.Action<int> _surpriseCardAddedHandler;
        private System.Action<int> _surpriseCardUpdatedHandler;

        void Awake()
        {
            // 获取 BattleSurpriseCardPanel 引用
            mOldPanel = GetComponent<BattleSurpriseCardPanel>();
            if (mOldPanel == null)
            {
                LogTool.LogError("[BattleSurpriseCardPanelNew] BattleSurpriseCardPanel 组件未找到");
            }
        }

        protected override void OnBindViewModel(BattleSurpriseCardViewModel vm)
        {
            if (mOldPanel == null) return;

            // 初始化现有惊喜卡牌
            InitializeSurpriseCards(vm);

            // 绑定事件
            BindEvents(vm);

            LogTool.Log("[BattleSurpriseCardPanelNew] 绑定完成");
        }

        protected override void OnUnbindViewModel(BattleSurpriseCardViewModel vm)
        {
            UnbindEvents(vm);
        }

        /// <summary>
        /// 初始化惊喜卡牌显示
        /// </summary>
        private void InitializeSurpriseCards(BattleSurpriseCardViewModel vm)
        {
            foreach (var surpriseId in vm.SurpriseCardIds.Value)
            {
                CreateOrUpdateSurpriseCard(vm, surpriseId);
            }
        }

        /// <summary>
        /// 绑定事件
        /// </summary>
        private void BindEvents(BattleSurpriseCardViewModel vm)
        {
            _surpriseCardAddedHandler = (surpriseId) =>
            {
                CreateOrUpdateSurpriseCard(vm, surpriseId);
            };
            vm.OnSurpriseCardAdded += _surpriseCardAddedHandler;

            _surpriseCardUpdatedHandler = (surpriseId) =>
            {
                UpdateSurpriseCard(vm, surpriseId);
            };
            vm.OnSurpriseCardUpdated += _surpriseCardUpdatedHandler;
        }

        /// <summary>
        /// 解绑事件
        /// </summary>
        private void UnbindEvents(BattleSurpriseCardViewModel vm)
        {
            if (_surpriseCardAddedHandler != null)
            {
                vm.OnSurpriseCardAdded -= _surpriseCardAddedHandler;
                _surpriseCardAddedHandler = null;
            }

            if (_surpriseCardUpdatedHandler != null)
            {
                vm.OnSurpriseCardUpdated -= _surpriseCardUpdatedHandler;
                _surpriseCardUpdatedHandler = null;
            }
        }

        /// <summary>
        /// 创建或更新惊喜卡牌
        /// </summary>
        private void CreateOrUpdateSurpriseCard(BattleSurpriseCardViewModel vm, int surpriseId)
        {
            if (mOldPanel?.SurpriseCards == null) return;

            var scrollCont = mOldPanel.SurpriseCards?.Find("Viewport/Content");
            if (scrollCont == null)
            {
                LogTool.LogError("[BattleSurpriseCardPanelNew] 找不到 Viewport/Content");
                return;
            }

            GameObject cardObj;

            if (!_surpriseCards.TryGetValue(surpriseId, out cardObj))
            {
                // 创建新卡牌
                var cardTemplate = scrollCont.Find("Card");
                if (cardTemplate == null)
                {
                    LogTool.LogError("[BattleSurpriseCardPanelNew] 找不到 Card 模板");
                    return;
                }

                cardObj = Instantiate(cardTemplate.gameObject, scrollCont);
                _surpriseCards.Add(surpriseId, cardObj);
            }

            // 更新显示
            UpdateSurpriseCardUI(vm, surpriseId, cardObj);

            // 绑定点击事件
            BindSurpriseCardClickEvent(vm, surpriseId, cardObj);
        }

        /// <summary>
        /// 更新惊喜卡牌
        /// </summary>
        private void UpdateSurpriseCard(BattleSurpriseCardViewModel vm, int surpriseId)
        {
            if (!_surpriseCards.TryGetValue(surpriseId, out var cardObj) || cardObj == null)
            {
                CreateOrUpdateSurpriseCard(vm, surpriseId);
                return;
            }

            // 更新显示
            UpdateSurpriseCardUI(vm, surpriseId, cardObj);
        }

        /// <summary>
        /// 更新惊喜卡牌UI
        /// </summary>
        private void UpdateSurpriseCardUI(BattleSurpriseCardViewModel vm, int surpriseId, GameObject cardObj)
        {
            cardObj.SetActive(true);
            cardObj.transform.localScale = Vector3.one;

            // 获取配置
            var surpriseCfg = vm.GetSurpriseConfig(surpriseId);
            if (surpriseCfg == null)
            {
                LogTool.LogWarning($"[BattleSurpriseCardPanelNew] 找不到惊喜卡牌配置: {surpriseId}");
                return;
            }

            // 更新图标
            var iconImage = cardObj.transform.Find("Icon")?.GetComponent<Image>();
            if (iconImage != null)
            {
                if (!string.IsNullOrEmpty(surpriseCfg.Icon))
                {
                    iconImage.sprite = Global.SurpriseChooseCardIconSpriteAtlas.GetSpriteExt(surpriseCfg.Icon);
                }
            }
        }

        /// <summary>
        /// 绑定惊喜卡牌点击事件 - 显示详情
        /// </summary>
        private void BindSurpriseCardClickEvent(BattleSurpriseCardViewModel vm, int surpriseId, GameObject cardObj)
        {
            var button = cardObj.GetComponent<Button>();
            if (button == null) return;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                // 播放音效
                var audioSource = UIRoot.Instance?.transform.Find("Audio Source")?.GetComponent<AudioSource>();
                if (audioSource != null) audioSource.Play();

                ShowSurpriseDetail(vm, surpriseId);
            });
        }

        /// <summary>
        /// 显示惊喜卡牌详情
        /// </summary>
        private void ShowSurpriseDetail(BattleSurpriseCardViewModel vm, int surpriseId)
        {
            // // 通过 UIKit 打开 SurpriseDetail 面板
            // var surpriseDetail = UIKit.GetPanel<SurpriseDetail>();
            // if (surpriseDetail == null)
            // {
            //     LogTool.LogWarning("[BattleSurpriseCardPanelNew] SurpriseDetail 面板不存在");
            //     return;
            // }
            //
            // surpriseDetail.gameObject.SetActive(true);
            //
            // // 获取 DeBuffId
            // var deBuffId = vm.GetDeBuffId(surpriseId);
            //
            // // 设置数据
            // var surpriseCardSlot = surpriseDetail.GetComponent<SurpriseCardSlot>();
            // if (surpriseCardSlot != null)
            // {
            //     surpriseCardSlot.SetData(surpriseId, deBuffId, null, null, 1);
            // }
            //
            // // 绑定关闭按钮
            // var quitBtn = surpriseDetail.transform.Find("Quit")?.GetComponent<Button>();
            // if (quitBtn != null)
            // {
            //     quitBtn.onClick.RemoveAllListeners();
            //     quitBtn.onClick.AddListener(() =>
            //     {
            //         surpriseDetail.gameObject.SetActive(false);
            //     });
            // }

            // LogTool.Log($"[BattleSurpriseCardPanelNew] 显示惊喜卡牌详情: {surpriseId}, deBuffId: {deBuffId}");
        }

        /// <summary>
        /// 公开方法：添加惊喜卡牌（供 SurpriseCardManager 调用）
        /// </summary>
        public void AddSurpriseCardView(int surpriseId, int deBuffId)
        {
            if (VM != null)
            {
                VM.AddSurpriseCard(surpriseId, deBuffId);
            }
        }

        protected override void OnOpen(IUIData uiData = null)
        {
            gameObject.SetActive(true);
        }

        protected override void OnShow()
        {
            gameObject.SetActive(true);
        }

        protected override void OnHide()
        {
            gameObject.SetActive(false);
        }

        protected override void OnClose()
        {
            base.OnClose();
        }
    }
}
