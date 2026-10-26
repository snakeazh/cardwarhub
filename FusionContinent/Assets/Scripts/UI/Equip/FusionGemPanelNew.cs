using Game.Equip;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using QFramework;
using UIFramework;
using Config;
using FusionContinent.Model;
using System.Collections.Generic;
using System.Linq;
using FusionContinent.Animation;
using UnityEngine.Events;
using IUIData = UIFramework.IUIData;

namespace FusionContinent
{
    /// <summary>
    /// 融合石界面 - MVVM架构
    /// 复用原 FusionGemPanel 的 UI 组件
    /// </summary>
    [UIFramework.PrefabPath("FusionGemPanel")]
    public class FusionGemPanelNew : UIPanel<FusionGemPanelViewModel>
    {
        // 引用原 Panel，复用其 UI 组件
        private FusionGemPanel mOldPanel;

        // 融合石槽位缓存
        private readonly Dictionary<int, GameObject> mGemSlots = new Dictionary<int, GameObject>();
        private readonly Dictionary<int, GameObject> mWearGemSlots = new Dictionary<int, GameObject>();

        #region 动画相关组件引用

        private RectTransform mCloseAnimation;
        private RectTransform mOpenAnimation;

        #endregion

        #region 事件处理器引用（用于解绑）

        private UnityAction mExitBtnHandler;

        #endregion

        void Awake()
        {
            // 获取原 Panel 引用
            mOldPanel = GetComponent<FusionGemPanel>();
            if (mOldPanel == null)
            {
                LogTool.LogError("[FusionGemPanelNew] 找不到 FusionGemPanel 组件！");
                return;
            }

            // 获取动画组件引用
            mCloseAnimation = mOldPanel.CloseAnimation;
            mOpenAnimation = mOldPanel.OpenAnimation;

            LogTool.Log("[FusionGemPanelNew] 已关联 FusionGemPanel");
        }

        protected override void OnBindViewModel(FusionGemPanelViewModel vm)
        {
            if (mOldPanel == null) return;

            // 绑定属性
            BindProperties(vm);

            // 绑定 ViewModel 事件
            vm.OnCloseRequested += OnCloseRequested;
            vm.OnOpenDetailsRequested += OnOpenDetailsRequested;
            vm.OnOpenFusionRequested += OnOpenFusionRequested;
            vm.OnOpenFragmentRequested += OnOpenFragmentRequested;

            // 初始化按钮
            InitButtons(vm);
        }

        protected override void OnUnbindViewModel(FusionGemPanelViewModel vm)
        {
            // 取消事件订阅
            vm.OnCloseRequested -= OnCloseRequested;
            vm.OnOpenDetailsRequested -= OnOpenDetailsRequested;
            vm.OnOpenFusionRequested -= OnOpenFusionRequested;
            vm.OnOpenFragmentRequested -= OnOpenFragmentRequested;

            // 移除按钮事件监听（逐项解绑）
            if (mOldPanel != null)
            {
                if (mExitBtnHandler != null)
                {
                    if (mOldPanel.ExitBg != null)
                        mOldPanel.ExitBg.onClick.RemoveListener(mExitBtnHandler);
                    if (mOldPanel.ExitBtn != null)
                        mOldPanel.ExitBtn.onClick.RemoveListener(mExitBtnHandler);
                    mExitBtnHandler = null;
                }
            }
        }

        #region 属性绑定

        private void BindProperties(FusionGemPanelViewModel vm)
        {
            if (mOldPanel == null) return;

            // 已佩戴融合石列表变化
            vm.WearFusionGemList.OnValueChanged += (oldVal, newVal) =>
            {
                InitWearGemSlot(vm);
            };

            // 融合石列表变化
            vm.FusionGemList.OnValueChanged += (oldVal, newVal) =>
            {
                InitGemScroll(vm);
            };
        }

        #endregion

        #region 初始化按钮

        private void InitButtons(FusionGemPanelViewModel vm)
        {
            if (mOldPanel == null) return;

            // 关闭按钮 - 添加关闭动画
            mExitBtnHandler = () =>
            {
                vm.OnCloseCommand.Execute();
                // 播放关闭动画
                var closeNode = GetAnimationCloseNode;
                if (closeNode != null)
                {
                    RunAnimation(closeNode, () => CloseSelf());
                }
            };

            if (mOldPanel.ExitBg != null)
            {
                mOldPanel.ExitBg.onClick.RemoveListener(mExitBtnHandler);
                mOldPanel.ExitBg.onClick.AddListener(mExitBtnHandler);
            }

            if (mOldPanel.ExitBtn != null)
            {
                mOldPanel.ExitBtn.onClick.RemoveListener(mExitBtnHandler);
                mOldPanel.ExitBtn.onClick.AddListener(mExitBtnHandler);
            }
        }

        #endregion

        #region 已佩戴融合石

        /// <summary>
        /// 初始化已佩戴融合石槽位
        /// </summary>
        private void InitWearGemSlot(FusionGemPanelViewModel vm)
        {
            if (mOldPanel?.WearGem == null) return;

            mWearGemSlots.Clear();
            var wearGemList = vm.WearFusionGemList.Value;

            for (int i = 0; i < mOldPanel.WearGem.childCount; i++)
            {
                var slotObj = mOldPanel.WearGem.GetChild(i).gameObject;

                // 获取该槽位对应的融合石
                var gem = i < wearGemList.Count ? wearGemList[i] : null;

                if (gem != null)
                {
                    mWearGemSlots.Add(gem.Id, slotObj);

                    // 显示融合石图标
                    var iconObj = slotObj.transform.Find("Icon")?.gameObject;
                    if (iconObj != null)
                    {
                        iconObj.SetActive(true);
                        var iconImage = iconObj.GetComponent<Image>();
                        if (iconImage != null && gem.Config != null)
                        {
                            iconImage.sprite = Global.FusionGemIconSpriteAtlas.GetSprite(gem.Config.Icon);
                        }
                    }

                    // 点击事件
                    var slotBtn = slotObj.GetComponent<Button>();
                    if (slotBtn != null)
                    {
                        int gemId = gem.Id;
                        slotBtn.onClick.RemoveAllListeners();
                        slotBtn.onClick.AddListener(() =>
                        {
                            vm.OnOpenDetailsCommand.Execute(gemId);
                        });
                    }
                }
                else
                {
                    // 隐藏图标
                    var iconObj = slotObj.transform.Find("Icon")?.gameObject;
                    if (iconObj != null) iconObj.SetActive(false);
                }
            }
        }

        #endregion

        #region 融合石列表

        /// <summary>
        /// 初始化融合石列表
        /// </summary>
        private void InitGemScroll(FusionGemPanelViewModel vm)
        {
            if (mOldPanel?.GemCardScroll == null) return;

            var cardScroll = mOldPanel.GemCardScroll.Find("Viewport/Content");
            if (cardScroll == null) return;

            // 隐藏所有子对象
            for (int i = 0; i < cardScroll.childCount; i++)
                cardScroll.GetChild(i).gameObject.SetActive(false);

            var gemList = vm.FusionGemList.Value;

            foreach (var gem in gemList)
            {
                if (gem.Config == null) continue;

                // 获取或创建槽位对象
                if (!mGemSlots.TryGetValue(gem.Id, out var slotObj))
                {
                    var template = cardScroll.Find("RewardEntity");
                    if (template == null) continue;

                    slotObj = Instantiate(template.gameObject, cardScroll);
                    mGemSlots.Add(gem.Id, slotObj);
                }

                // 显示
                slotObj.SetActive(true);

                // 设置融合石数据
                var rewardEntity = slotObj.GetComponent<RewardEntity>();
                if (rewardEntity != null)
                {
                    rewardEntity.SetData(new RewardEntityData()
                    {
                        Quality = gem.Quality,
                        IconSprite = Global.FusionGemIconSpriteAtlas.GetSprite(gem.Config.Icon),
                        StyleType = RewardStyleType.Equip,
                        IsEffect = false,
                        OnClick = () =>
                        {
                            vm.OnOpenDetailsCommand.Execute(gem.Id);
                        }
                    });
                }
            }
        }

        #endregion

        #region 事件处理

        private void OnCloseRequested()
        {
            // 关闭逻辑移到了按钮处理中，由 RunAnimation 调用 CloseSelf()
        }

        private void OnOpenDetailsRequested(int gemId, int soldierId)
        {
            // UIKit.OpenPanel<FusionGemDetailsPanel>(QFramework.UILevel.PopUI).SetData(gemId, soldierId);
        }

        private void OnOpenFusionRequested(int soldierId)
        {
            // UIKit.OpenPanel<SynthesisFusionGemPanel>().SetData(soldierId);
        }

        private void OnOpenFragmentRequested(int soldierId)
        {
            UIKit.OpenPanel<FusionGemFragmentPanel>().SetData(soldierId);
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 设置数据（供外部调用）
        /// </summary>
        public void SetData(int soldierId)
        {
            if (VM != null)
            {
                VM.Initialize(soldierId);
            }
        }

        #endregion

        #region 生命周期

        protected override void OnOpen(IUIData uiData = null)
        {
            LogTool.Log($"[FusionGemPanelNew] OnOpen: VM={VM != null}, SoldierId={VM?.SoldierId.Value}");

            // 播放打开动画
            var openNode = GetAnimationOpenNode;
            if (openNode != null)
            {
                RunAnimation(openNode, null);
            }

            // 初始化显示
            if (VM != null && VM.SoldierId.Value > 0)
            {
                // 刷新数据
                VM.RefreshData();

                // 初始化列表显示
                InitWearGemSlot(VM);
                InitGemScroll(VM);
            }
        }

        public override void Show()
        {
            base.Show();

            // 播放打开动画
            var openNode = GetAnimationOpenNode;
            if (openNode != null)
            {
                RunAnimation(openNode, null);
            }
        }

        public override void Hide()
        {
            var closeNode = GetAnimationCloseNode;
            if (closeNode != null)
            {
                RunAnimation(closeNode, HidePanelInternal);
                return;
            }

            HidePanelInternal();
        }

        protected override void OnClose()
        {
        }

        #endregion

        #region 动画相关

        protected GameObject GetAnimationCloseNode
        {
            get => mCloseAnimation?.gameObject;
        }

        protected GameObject GetAnimationOpenNode
        {
            get => mOpenAnimation?.gameObject;
        }

        protected void RunAnimation(GameObject target, UnityAction complete)
        {
            if (target == null)
            {
                complete?.Invoke();
                return;
            }

            var animations = target.GetComponents<UIAnimation>();
            if (animations == null || animations.Length == 0)
            {
                complete?.Invoke();
                return;
            }

            int completedCount = 0;
            int totalCount = animations.Length;

            void OnComplete()
            {
                completedCount++;
                if (completedCount >= totalCount)
                {
                    complete?.Invoke();
                }
            }

            foreach (var anim in animations)
            {
                anim.Play(OnComplete);
            }
        }

        private void HidePanelInternal()
        {
            base.Hide();
        }

        #endregion
    }
}
