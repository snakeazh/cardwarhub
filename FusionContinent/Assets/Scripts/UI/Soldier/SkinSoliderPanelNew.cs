using Game.Soldier;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using QFramework;
using UIFramework;
using Config;
using FusionContinent.Model;
using FusionContinent;
using FusionContinent.Helper;
using System.Collections.Generic;
using Assets.Scripts.Localization;
using FusionContinent.Animation;
using UnityEngine.Events;
using IUIData = UIFramework.IUIData;

namespace FusionContinent
{
    /// <summary>
    /// 士兵皮肤选择界面 - 新框架版本 (MVVM)
    /// 直接使用 SkinSoliderPanel 的 UI 组件
    /// </summary>
    [UIFramework.PrefabPath("SkinSoliderPanel")]
    public class SkinSoliderPanelNew : UIFramework.UIPanel<SkinSoliderPanelViewModel>
    {
        // UI 组件引用（从 SkinSoliderPanel.Designer.cs）
        private Button bg;
        private Image icon;
        private Button ChooeseBtn;
        private Button closebtn;
        private RectTransform AttributeScrollContent;
        private RectTransform SelectBtnParent;
        private RectTransform CloseAnimation;
        private TMP_Text SkinName;

        // 标题文本（需要在预制体上添加或查找）
        private TMP_Text titleText;

        #region 事件处理器引用（用于解绑）

        private UnityAction mCloseBtnHandler;
        private UnityAction mBgBtnHandler;

        private System.Action<SkinConfig, SkinConfig> mSelectedSkinHandler;
        private System.Action<string, string> mChooseBtnTextHandler;
        private System.Action<bool, bool> mCanChooseHandler;

        #endregion

        void Awake()
        {
            // 尝试从 SkinSoliderPanel 获取组件
            var oldPanel = GetComponent<SkinSoliderPanel>();
            if (oldPanel != null)
            {
                bg = oldPanel.shadow;
                icon = oldPanel.icon;
                ChooeseBtn = oldPanel.ChooeseBtn;
                closebtn = oldPanel.closebtn;
                AttributeScrollContent = oldPanel.AttributeScrollContent;
                SelectBtnParent = oldPanel.SelectBtnParent;
                CloseAnimation = oldPanel.CloseAnimation;
                SkinName = oldPanel.SkinName;
                LogTool.Log("[SkinSoliderPanelNew] 已关联 SkinSoliderPanel 组件");
            }
            
            // 查找标题文本
            titleText = transform.Find("TitleText")?.GetComponent<TMP_Text>();
            
            LogTool.Log($"[SkinSoliderPanelNew] Awake: bg={bg != null}, icon={icon != null}, ChooeseBtn={ChooeseBtn != null}, closebtn={closebtn != null}");
        }

        protected override void OnBindViewModel(SkinSoliderPanelViewModel vm)
        {
            if (bg == null)
            {
                LogTool.LogError("[SkinSoliderPanelNew] UI 组件未初始化！");
                return;
            }

            // 绑定属性
            BindProperties(vm);

            // 绑定 ViewModel 事件
            vm.OnSkinChanged += OnSkinChanged;
            vm.OnCloseRequested += OnCloseRequested;

            // 初始化按钮
            InitButtons(vm);
            
            // 选中当前皮肤（在 UI 初始化完成后）
            SelectCurrentSkin(vm);
        }
        
        /// <summary>
        /// 选中当前穿戴的皮肤
        /// </summary>
        private void SelectCurrentSkin(SkinSoliderPanelViewModel vm)
        {
            if (SelectBtnParent == null || vm.SkinConfigs.Value == null) return;

            int targetSkinId = vm.GlobalSkinId.Value;
            LogTool.Log($"[SkinSoliderPanelNew] SelectCurrentSkin: targetSkinId={targetSkinId}");

            // 如果没有穿戴皮肤，默认选中第一个
            if (targetSkinId == 0 && vm.SkinConfigs.Value.Count > 0)
            {
                targetSkinId = vm.SkinConfigs.Value[0].Id;
            }

            // 查找并选中对应的皮肤按钮
            for (int i = 0; i < SelectBtnParent.childCount; i++)
            {
                var child = SelectBtnParent.GetChild(i);
                var skinBtn = child.GetComponent<SelectSkinBtnobj>();
                if (skinBtn != null && skinBtn.skinId == targetSkinId)
                {
                    LogTool.Log($"[SkinSoliderPanelNew] 选中皮肤: skinId={targetSkinId}, buttonIndex={i}");
                    // 触发按钮点击（会同时触发 OnSelectSkinCommand 和 UpdateSkinDisplay）
                    skinBtn.selectBtn.onClick.Invoke();
                    // 标记选中状态（避免重复更新）
                    UpdateSelectedUI(child.gameObject);
                    return;
                }
            }

            // 如果没找到，默认选中第一个
            if (SelectBtnParent.childCount > 0)
            {
                var firstBtn = SelectBtnParent.GetChild(0).GetComponent<SelectSkinBtnobj>();
                if (firstBtn?.selectBtn != null)
                {
                    LogTool.Log($"[SkinSoliderPanelNew] 默认选中第一个皮肤: skinId={firstBtn.skinId}");
                    // 触发按钮点击（会同时触发 OnSelectSkinCommand 和 UpdateSkinDisplay）
                    firstBtn.selectBtn.onClick.Invoke();
                    // 标记选中状态
                    UpdateSelectedUI(firstBtn.gameObject);
                }
            }
        }

        protected override void OnUnbindViewModel(SkinSoliderPanelViewModel vm)
        {
            // 取消事件订阅
            vm.OnSkinChanged -= OnSkinChanged;
            vm.OnCloseRequested -= OnCloseRequested;

            // 解绑属性
            if (mSelectedSkinHandler != null)
            {
                vm.SelectedSkin.OnValueChanged -= mSelectedSkinHandler;
                mSelectedSkinHandler = null;
            }

            if (mChooseBtnTextHandler != null)
            {
                vm.ChooseBtnText.OnValueChanged -= mChooseBtnTextHandler;
                mChooseBtnTextHandler = null;
            }

            if (mCanChooseHandler != null)
            {
                vm.CanChoose.OnValueChanged -= mCanChooseHandler;
                mCanChooseHandler = null;
            }

            // 移除按钮事件监听
            if (mCloseBtnHandler != null)
            {
                if (closebtn != null)
                    closebtn.onClick.RemoveListener(mCloseBtnHandler);
                if (bg != null)
                    bg.onClick.RemoveListener(mCloseBtnHandler);
                mCloseBtnHandler = null;
            }

            if (mBgBtnHandler != null)
            {
                if (bg != null)
                    bg.onClick.RemoveListener(mBgBtnHandler);
                mBgBtnHandler = null;
            }
        }

        #region 属性绑定

        private void BindProperties(SkinSoliderPanelViewModel vm)
        {
            mSelectedSkinHandler = (oldVal, newVal) =>
            {
                if (newVal != null)
                    UpdateSkinDisplay(newVal);
            };
            vm.SelectedSkin.OnValueChanged += mSelectedSkinHandler;

            mChooseBtnTextHandler = (oldVal, newVal) =>
            {
                if (ChooeseBtn != null)
                {
                    var textComponent = ChooeseBtn.transform.GetChild(0)?.GetComponent<TMP_Text>();
                    if (textComponent != null)
                        textComponent.text = newVal;
                }
            };
            vm.ChooseBtnText.OnValueChanged += mChooseBtnTextHandler;

            mCanChooseHandler = (oldVal, newVal) =>
            {
                if (ChooeseBtn != null)
                    ChooeseBtn.interactable = newVal;
            };
            vm.CanChoose.OnValueChanged += mCanChooseHandler;
        }

        #endregion

        #region 初始化按钮

        private void InitButtons(SkinSoliderPanelViewModel vm)
        {
            // 关闭按钮 - 添加关闭动画
            mCloseBtnHandler = () =>
            {
                vm.OnCloseCommand.Execute();
                // 播放关闭动画
                var closeNode = GetAnimationCloseNode;
                if (closeNode != null)
                {
                    RunAnimation(closeNode, () => CloseSelf());
                }
            };

            if (closebtn != null)
            {
                closebtn.onClick.RemoveListener(mCloseBtnHandler);
                closebtn.onClick.AddListener(mCloseBtnHandler);
            }

            // 背景点击关闭
            if (bg != null)
            {
                bg.onClick.RemoveListener(mCloseBtnHandler);
                bg.onClick.AddListener(mCloseBtnHandler);
            }

            // 穿戴按钮
            if (ChooeseBtn != null)
            {
                ChooeseBtn.onClick.RemoveAllListeners();
                ChooeseBtn.onClick.AddListener(() => vm.OnChooseSkinCommand.Execute());
            }

            // 初始化皮肤列表
            InitSkinList(vm);
        }

        /// <summary>
        /// 初始化皮肤列表
        /// </summary>
        private void InitSkinList(SkinSoliderPanelViewModel vm)
        {
            if (SelectBtnParent == null || vm.SkinConfigs.Value == null) return;

            var skinConfigs = vm.SkinConfigs.Value;
            int count = skinConfigs.Count;
            
            LogTool.Log($"[SkinSoliderPanelNew] InitSkinList: 皮肤数量={count}");

            // 获取模板（第一个子对象）
            Transform template = SelectBtnParent.childCount > 0 ? SelectBtnParent.GetChild(0) : null;
            if (template == null)
            {
                LogTool.LogError("[SkinSoliderPanelNew] SelectBtnParent 没有子对象作为模板");
                return;
            }

            // 确保模板隐藏，用于实例化
            template.gameObject.SetActive(false);

            // 清理多余子对象（保留模板）
            int existingCount = SelectBtnParent.childCount - 1;
            for (int i = existingCount - 1; i >= 0; i--)
            {
                Destroy(SelectBtnParent.GetChild(i).gameObject);
            }

            // 创建皮肤按钮
            for (int j = 0; j < count; j++)
            {
                var skinCfg = skinConfigs[j];
                bool isFirst = (j == 0);

                // 实例化按钮（第一个使用模板）
                var btnObj = isFirst ? template.gameObject : Instantiate(template.gameObject, SelectBtnParent);
                btnObj.transform.localScale = Vector3.one;
                btnObj.SetActive(true);

                // 配置按钮数据
                ConfigureSkinButton(btnObj, skinCfg, vm);
            }

        }

        /// <summary>
        /// 配置单个皮肤按钮
        /// </summary>
        private void ConfigureSkinButton(GameObject btnObj, SkinConfig skinCfg, SkinSoliderPanelViewModel vm)
        {
            var skinBtn = btnObj.GetComponent<SelectSkinBtnobj>();
            if (skinBtn == null)
            {
                LogTool.LogError($"[SkinSoliderPanelNew] 按钮对象缺少 SelectSkinBtnobj 组件: {btnObj.name}");
                return;
            }

            int skinId = skinCfg.Id;

            // 设置皮肤ID
            skinBtn.skinId = skinId;

            // 设置皮肤名称
            if (skinBtn.nameText != null)
                skinBtn.nameText.text = skinCfg.Name;

            // 设置选中指示器初始状态（隐藏）
            if (skinBtn.selectedObj != null)
                skinBtn.selectedObj.gameObject.SetActive(false);

            // 设置皮肤图标 - 使用 HeroIcon，直接从 Atlas 获取 Sprite，避免修改尺寸
            if (skinBtn.heroIcon != null)
            {
                var sprite = Global.HeroIconSpriteAtlas.GetSprite(skinCfg.Icon + "LittleIcon");
                if (sprite != null)
                {
                    skinBtn.heroIcon.sprite = sprite;
                    // 不调用 SetNativeSize()，保持 RectTransform 的预设尺寸
                }
            }

            // 设置"已拥有"状态文本
            UpdateSkinOwnState(skinBtn, skinCfg, vm);

            // 绑定按钮点击事件
            if (skinBtn.selectBtn != null)
            {
                skinBtn.selectBtn.onClick.RemoveAllListeners();
                skinBtn.selectBtn.onClick.AddListener(() =>
                {
                    vm.OnSelectSkinCommand.Execute(skinId);
                    OnSkinButtonSelected(btnObj);
                });
            }
        }

        /// <summary>
        /// 更新皮肤拥有状态
        /// </summary>
        private void UpdateSkinOwnState(SelectSkinBtnobj skinBtn, SkinConfig skinCfg, SkinSoliderPanelViewModel vm)
        {
            if (skinBtn.isHaveText == null) return;

            bool isOwned = vm.GlobalSkinId.Value == skinCfg.Id ||
                           (vm.GlobalSkinId.Value == 0 && skinCfg.Id == vm.SkinConfigs.Value[0].Id);

            // skinBtn.isHaveText.gameObject.SetActive(isOwned);
            skinBtn.isHaveText.SetText(isOwned ? "已拥有" : "未拥有");
        }

        /// <summary>
        /// 皮肤按钮被选中
        /// </summary>
        private void OnSkinButtonSelected(GameObject selectedObj)
        {
            // 隐藏所有按钮的选中指示器
            for (int i = 0; i < SelectBtnParent.childCount; i++)
            {
                var child = SelectBtnParent.GetChild(i);
                var skinBtn = child.GetComponent<SelectSkinBtnobj>();
                if (skinBtn?.selectedObj != null)
                    skinBtn.selectedObj.gameObject.SetActive(false);
            }

            // 显示选中按钮的指示器
            var selectedSkinBtn = selectedObj.GetComponent<SelectSkinBtnobj>();
            if (selectedSkinBtn?.selectedObj != null)
                selectedSkinBtn.selectedObj.gameObject.SetActive(true);
        }

        /// <summary>
        /// 更新选中UI（兼容旧逻辑）
        /// </summary>
        private void UpdateSelectedUI(GameObject selectedObj)
        {
            OnSkinButtonSelected(selectedObj);
        }

        #endregion

        #region UI更新

        /// <summary>
        /// 更新皮肤显示
        /// </summary>
        private void UpdateSkinDisplay(SkinConfig skinCfg)
        {
            if (skinCfg == null) return;

            // 更新图标
            if (icon != null)
            {
                SpriteHelper.TryLoadPaintAsync(skinCfg.Icon, icon, EPaintType.Large,true);
                icon.gameObject.transform.localScale = skinCfg.IllustrationScale * Vector3.one;
            }

            // 更新皮肤名称
            if (SkinName != null)
                SkinName.text = skinCfg.Name;

            // 更新标题
            if (titleText != null)
                titleText.text = skinCfg.Name;

            // 更新穿戴按钮文本
            UpdateChooseBtnText();

            // 更新属性列表
            UpdateAttributeList(skinCfg);
        }

        /// <summary>
        /// 更新穿戴按钮文本
        /// </summary>
        private void UpdateChooseBtnText()
        {
            if (ChooeseBtn == null || VM == null) return;

            var textComponent = ChooeseBtn.transform.GetChild(0)?.GetComponent<TMP_Text>();
            if (textComponent != null)
            {
                textComponent.text = VM.ChooseBtnText.Value;
            }
        }

        /// <summary>
        /// 更新属性列表
        /// </summary>
        private void UpdateAttributeList(SkinConfig skinCfg)
        {
            if (AttributeScrollContent == null) return;

            // 隐藏所有属性项
            for (int i = 0; i < AttributeScrollContent.childCount; i++)
                AttributeScrollContent.GetChild(i).gameObject.SetActive(false);

            if (skinCfg.Attributes.Count == 0)
            {
                // 显示默认提示
                if (AttributeScrollContent.childCount > 0)
                {
                    var defObj = AttributeScrollContent.GetChild(0).gameObject;
                    defObj.SetActive(true);
                    
                    // 优先使用 LocalizationaAutoWidthText，否则使用 LocalizationText 或 TMP_Text
                    var localAutoWidthText = defObj.transform.Find("Text")?.GetComponent<LocalizationaAutoWidthText>();
                    if (localAutoWidthText != null)
                    {
                        localAutoWidthText.SetData(LocalizationModule.UITexts, "UI_ATTR_NOT_BONUS");
                    }
                    else
                    {
                        var localText = defObj.transform.Find("Text")?.GetComponent<LocalizationText>();
                        if (localText != null)
                        {
                            localText.SetData(LocalizationModule.UITexts, "UI_ATTR_NOT_BONUS");
                        }
                        else
                        {
                            var tmpText = defObj.transform.Find("Text")?.GetComponent<TMP_Text>();
                            if (tmpText != null)
                            {
                                tmpText.text = LocalizationManager.Instance.GetText(LocalizationModule.UITexts, "UI_ATTR_NOT_BONUS");
                            }
                        }
                    }
                }
            }
            else
            {
                // 显示属性
                for (int i = 0; i < skinCfg.Attributes.Count; i++)
                {
                    var attrV = skinCfg.Attributes[i];
                    var attrCfg = DataConfigManager.Tables.TbAttributeConfig.GetOrDefault(attrV.Type);
                    if (attrCfg == null)
                    {
                        LogTool.LogWarning($"[SkinSoliderPanelNew] 属性配置未找到: Type={attrV.Type}, SkinId={skinCfg.Id}");
                        continue;
                    }

                    GameObject attrObj;
                    if (i < AttributeScrollContent.childCount)
                    {
                        attrObj = AttributeScrollContent.GetChild(i).gameObject;
                    }
                    else
                    {
                        attrObj = Instantiate(AttributeScrollContent.GetChild(0).gameObject, AttributeScrollContent);
                    }

                    attrObj.SetActive(true);
                    var symbolStr = attrV.Value > 0 ? "+" : "";
                    
                    // 优先使用 LocalizationaAutoWidthText，否则使用 LocalizationText 或 TMP_Text
                    var localAutoWidthText = attrObj.transform.Find("Text")?.GetComponent<LocalizationaAutoWidthText>();
                    if (localAutoWidthText != null)
                    {
                        localAutoWidthText.SetData(LocalizationModule.UITexts, attrCfg.Name, $"{symbolStr}{attrV.Value}");
                    }
                    else
                    {
                        var localText = attrObj.transform.Find("Text")?.GetComponent<LocalizationText>();
                        if (localText != null)
                        {
                            localText.SetData(LocalizationModule.UITexts, attrCfg.Name, $"{symbolStr}{attrV.Value}");
                        }
                        else
                        {
                            var tmpText = attrObj.transform.Find("Text")?.GetComponent<TMP_Text>();
                            if (tmpText != null)
                            {
                                var localizedName = LocalizationManager.Instance.GetText(LocalizationModule.UITexts, attrCfg.Name);
                                tmpText.text = $"{localizedName}{symbolStr}{attrV.Value}";
                            }
                        }
                    }
                }
            }
        }

        #endregion

        #region 事件处理

        private void OnSkinChanged(int skinId)
        {
            LogTool.Log($"[SkinSoliderPanelNew] 皮肤变更: skinId={skinId}");
        }

        private void OnCloseRequested()
        {
            // 关闭逻辑移到了按钮处理中，由 RunAnimation 调用 CloseSelf()
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 设置数据（供外部调用）
        /// </summary>
        public void SetSkin(int cardId)
        {
            LogTool.Log($"[SkinSoliderPanelNew] SetSkin: cardId={cardId}");
            
            if (VM != null)
            {
                VM.Initialize(cardId);
            }
        }

        #endregion

        #region 生命周期

        protected override void OnOpen(UIFramework.IUIData uiData = null)
        {
            LogTool.Log($"[SkinSoliderPanelNew] OnOpen: VM={VM != null}, CardId={VM?.CardId.Value}");

            if (VM != null && VM.SelectedSkin.Value != null)
            {
                UpdateSkinDisplay(VM.SelectedSkin.Value);
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

        protected override void OnShow()
        {
        }

        protected override void OnHide()
        {
        }

        #endregion

        #region 动画相关

        protected GameObject GetAnimationCloseNode
        {
            get => CloseAnimation?.gameObject;
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