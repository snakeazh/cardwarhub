using Assets.Scripts.Localization;
using Config;
using FusionContinent.Animation;
using Game.Login;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UIFramework;
using UIFramework.Services;

namespace FusionContinent
{
    /// <summary>
    /// 语言切换面板 - 新框架版本 (MVVM)
    /// 复用原 LanuageChangePanel 的 UI 组件
    /// </summary>
    [PrefabPath("LanuageChangePanel")]
    public class LanguageChangePanelNew : UIPanel<LanguageChangeViewModel>
    {
        // 引用原 Panel，复用其 UI 组件
        private LanuageChangePanel mOldPanel;
        
        // 额外查找的组件
        private Button mQuitBtn;
        
        // 动态生成的按钮列表
        private System.Collections.Generic.List<GameObject> mLanguageButtons = new System.Collections.Generic.List<GameObject>();

        void Awake()
        {
            // 获取原 Panel 引用
            mOldPanel = GetComponent<LanuageChangePanel>();
            if (mOldPanel == null)
            {
                LogTool.LogError("[LanguageChangePanelNew] 找不到 LanuageChangePanel 组件！请确保预制体上同时挂载了两个脚本");
                return;
            }

            // 查找退出按钮（在关闭动画节点上）
            if (mOldPanel.CloseAnimation != null)
            {
                mQuitBtn = mOldPanel.quitBtn;
            }
            
            LogTool.Log("[LanguageChangePanelNew] 已关联 LanuageChangePanel");
        }

        protected override void OnBindViewModel(LanguageChangeViewModel vm)
        {
            if (mOldPanel == null) return;
            
            // 绑定 ViewModel 事件
            vm.OnClosePanel += OnClosePanel;
            vm.OnLanguageChanged += OnLanguageChanged;
            
            // 绑定退出按钮
            if (mQuitBtn != null)
            {
                mQuitBtn.onClick.AddListener(() =>
                {
                    PlayCloseAnimation(() => vm.OnCloseCommand.Execute());
                });
            }

            // 初始化语言按钮
            InitLanguageButtons(vm);
        }

        protected override void OnUnbindViewModel(LanguageChangeViewModel vm)
        {
            // 取消事件订阅
            vm.OnClosePanel -= OnClosePanel;
            vm.OnLanguageChanged -= OnLanguageChanged;
            
            if (mOldPanel == null) return;
            
            // 清理退出按钮监听
            if (mQuitBtn != null) mQuitBtn.onClick.RemoveAllListeners();
            
            // 清理所有语言按钮监听
            foreach (var button in mLanguageButtons)
            {
                if (button != null)
                {
                    var btn = button.GetComponent<Button>();
                    if (btn != null) btn.onClick.RemoveAllListeners();
                }
            }
        }

        /// <summary>
        /// 初始化语言按钮
        /// </summary>
        private void InitLanguageButtons(LanguageChangeViewModel vm)
        {
            if (mOldPanel?.LanuageChangeScroll == null) return;
            
            // 清除旧按钮
            foreach (var button in mLanguageButtons)
            {
                if (button != null && button != mOldPanel.LanuageBtn1?.gameObject)
                {
                    Destroy(button);
                }
            }
            mLanguageButtons.Clear();
            
            // 获取语言列表
            var languageList = vm.LanguageList.Value;
            
            for (int i = 0; i < languageList.Count; i++)
            {
                var cfg = languageList[i];
                int index = i;
                
                // 创建或获取按钮
                GameObject buttonObj;
                if (i == 0 && mOldPanel.LanuageBtn1 != null)
                {
                    buttonObj = mOldPanel.LanuageBtn1.gameObject;
                }
                else if (i < mOldPanel.LanuageChangeScroll.childCount)
                {
                    buttonObj = mOldPanel.LanuageChangeScroll.GetChild(i).gameObject;
                }
                else
                {
                    buttonObj = Instantiate(mOldPanel.LanuageBtn1?.gameObject, mOldPanel.LanuageChangeScroll);
                }
                
                if (buttonObj == null) continue;
                
                buttonObj.SetActive(true);
                
                // 设置按钮文本
                var selectedText = buttonObj.transform.Find("SelectedText")?.GetComponent<TextMeshProUGUI>();
                var unSelectedText = buttonObj.transform.Find("UnSelectedText")?.GetComponent<TextMeshProUGUI>();
                
                if (selectedText != null) selectedText.text = cfg.Name;
                if (unSelectedText != null) unSelectedText.text = cfg.Name;
                
                // 绑定点击事件
                var button = buttonObj.GetComponent<Button>();
                if (button != null)
                {
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() =>
                    {
                        vm.OnSelectLanguageCommand.Execute(index);
                    });
                }
                
                // 初始化选中状态
                UpdateButtonVisual(buttonObj, i == vm.SelectedLanguageIndex.Value);
                
                mLanguageButtons.Add(buttonObj);
            }
            
            LogTool.Log($"[LanguageChangePanelNew] 初始化了 {mLanguageButtons.Count} 个语言按钮");
        }

        /// <summary>
        /// 更新按钮视觉状态
        /// </summary>
        private void UpdateButtonVisual(GameObject buttonObj, bool isSelected)
        {
            var selectedImg = buttonObj.transform.Find("Selected")?.gameObject;
            var selectedText = buttonObj.transform.Find("SelectedText")?.gameObject;
            var unSelectedText = buttonObj.transform.Find("UnSelectedText")?.gameObject;
            
            if (selectedImg != null) selectedImg.SetActive(isSelected);
            if (selectedText != null) selectedText.SetActive(isSelected);
            if (unSelectedText != null) unSelectedText.SetActive(!isSelected);
        }

        /// <summary>
        /// 语言切换回调
        /// </summary>
        private void OnLanguageChanged(int index)
        {
            // 更新所有按钮的视觉状态
            for (int i = 0; i < mLanguageButtons.Count; i++)
            {
                if (mLanguageButtons[i] != null)
                {
                    UpdateButtonVisual(mLanguageButtons[i], i == index);
                }
            }
            
            LogTool.Log($"[LanguageChangePanelNew] 语言切换到索引: {index}");
        }

        private void OnClosePanel()
        {
            CloseSelf();
            // 播放关闭动画后关闭面板
            // PlayCloseAnimation(() => CloseSelf());
        }

        #region 动画

        private void PlayOpenAnimation()
        {
            if (mOldPanel?.OpenAnimation != null)
            {
                UIAnimationUtils.PlayAnimations(mOldPanel.OpenAnimation.gameObject, null);
            }
        }

        private void PlayCloseAnimation(System.Action onComplete)
        {
            if (mOldPanel?.CloseAnimation != null)
            {
                UIAnimationUtils.PlayAnimations(mOldPanel.CloseAnimation.gameObject, () => onComplete?.Invoke());
            }
            else
            {
                onComplete?.Invoke();
            }
        }

        #endregion

        protected override void OnOpen(IUIData uiData = null)
        {
            // 播放打开动画
            PlayOpenAnimation();
            
            // 触发当前选中语言的按钮点击（恢复上次选择）
            if (VM != null && VM.SelectedLanguageIndex.Value < mLanguageButtons.Count)
            {
                int index = VM.SelectedLanguageIndex.Value;
                if (index < mLanguageButtons.Count && mLanguageButtons[index] != null)
                {
                    var btn = mLanguageButtons[index].GetComponent<Button>();
                    btn?.onClick.Invoke();
                }
            }
        }

        protected override void OnShow()
        {
            // 面板显示时的逻辑
        }

        protected override void OnHide()
        {
            // 面板隐藏时的逻辑
        }
    }
}
