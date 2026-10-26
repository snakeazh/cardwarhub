using System;
using TMPro;
using UIFramework;
using UnityEngine;

namespace FusionContinent
{
    [PrefabPath("TipPanel")]
    public class TipPanelNew : UIPanel<TipPanelViewModel>
    {
        private TipPanel mOldPanel;

        void Awake()
        {
            mOldPanel = GetComponent<TipPanel>();
            if (mOldPanel == null)
            {
                LogTool.LogError("[TipPanelNew] 找不到 TipPanel 组件");
                return;
            }
            mOldPanel.enabled = false;
        }

        protected override void OnBindViewModel(TipPanelViewModel vm)
        {
            if (mOldPanel == null) return;

            mOldPanel.Mask.onClick.AddListener(() => vm.OnCloseCommand.Execute());
            vm.OnCloseRequested += OnCloseRequested;

            RefreshUI(vm);
        }

        protected override void OnUnbindViewModel(TipPanelViewModel vm)
        {
            if (mOldPanel == null) return;

            mOldPanel.Mask.onClick.RemoveAllListeners();
            vm.OnCloseRequested -= OnCloseRequested;
        }

        private void OnCloseRequested()
        {
            UIFramework.UIManager.ClosePanel(this);
        }

        protected override void OnOpen(IUIData uiData = null)
        {
            if (VM != null)
                RefreshUI(VM);
        }

        /// <summary>
        /// 有标题模式
        /// </summary>
        public void SetData(string title, string detail)
        {
            if (VM == null || mOldPanel == null) return;

            VM.Title = title;
            VM.Detail = detail;
            RefreshUI(VM);
        }

        /// <summary>
        /// 无标题模式
        /// </summary>
        public void SetData(string detail)
        {
            SetData(null, detail);
        }

        private void RefreshUI(TipPanelViewModel vm)
        {
            mOldPanel.Title.gameObject.SetActive(vm.ShowTitle);
            if (vm.ShowTitle)
            {
                mOldPanel.Title.transform.Find("TitleName").GetComponent<TMP_Text>().text = vm.Title;
            }
            mOldPanel.Detail.text = vm.Detail;
        }

        protected override void OnShow() { }
        protected override void OnHide() { }
    }
}
