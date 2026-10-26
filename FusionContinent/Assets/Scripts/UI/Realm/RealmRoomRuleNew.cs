using Game.Realm;
using TMPro;
using UIFramework;
using UnityEngine;
using UnityEngine.UI;

namespace FusionContinent
{
    [PrefabPath("RealmRoomRule")]
    public class RealmRoomRuleNew : UIPanel<RealmRoomRuleViewModel>
    {
        private const string TitleNodePath = "TweenTarget/Title";

        private RealmRoomRule mOldPanel;
        private TextMeshProUGUI mTitleText;

        void Awake()
        {
            mOldPanel = GetComponent<RealmRoomRule>();
            if (mOldPanel == null)
            {
                LogTool.LogError("[RealmRoomRuleNew] 找不到 RealmRoomRule 组件");
                return;
            }

            // 禁用旧 Panel 组件，避免生命周期冲突
            mOldPanel.enabled = false;

            var titleTransform = transform.Find(TitleNodePath);
            if (titleTransform == null)
            {
                LogTool.LogError("[RealmRoomRuleNew] 找不到 Title 节点: " + TitleNodePath);
                return;
            }

            mTitleText = titleTransform.GetComponent<TextMeshProUGUI>();
        }

        protected override void OnBindViewModel(RealmRoomRuleViewModel vm)
        {
            mOldPanel.Mask.onClick.AddListener(OnMaskClicked);

            vm.Title.OnValueChanged += OnTitleChanged;
            vm.Desc.OnValueChanged += OnDescChanged;
            vm.OnCloseRequested += OnCloseRequestedHandler;

            SyncBoundTexts(vm);
        }

        protected override void OnUnbindViewModel(RealmRoomRuleViewModel vm)
        {
            mOldPanel.Mask.onClick.RemoveListener(OnMaskClicked);

            vm.Title.OnValueChanged -= OnTitleChanged;
            vm.Desc.OnValueChanged -= OnDescChanged;
            vm.OnCloseRequested -= OnCloseRequestedHandler;
        }

        private void SyncBoundTexts(RealmRoomRuleViewModel vm)
        {
            if (mTitleText != null)
            {
                mTitleText.text = vm.Title.Value;
            }
            mOldPanel.Desc.text = vm.Desc.Value;
        }

        private void OnMaskClicked()
        {
            VM.OnCloseCommand.Execute();
        }

        private void OnCloseRequestedHandler()
        {
            CloseSelf();
        }

        private void OnTitleChanged(string oldValue, string newValue)
        {
            if (mTitleText != null)
            {
                mTitleText.text = newValue;
            }
        }

        private void OnDescChanged(string oldValue, string newValue)
        {
            mOldPanel.Desc.text = newValue;
        }

        protected override void OnOpen(IUIData uiData = null) { }
        protected override void OnShow() { }
        protected override void OnHide() { }
    }
}
