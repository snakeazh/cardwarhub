using FusionContinent.Animation;
using Game.Soldier;
using TMPro;
using UIFramework;
using UnityEngine;

namespace FusionContinent
{
    [PrefabPath("MainAttrDetail")]
    public class MainAttrDetailNew : UIPanel<MainAttrDetailViewModel>
    {
        private MainAttrDetail mOldPanel;
        private Transform mContent;
        private GameObject mTemplate;

        void Awake()
        {
            mOldPanel = GetComponent<MainAttrDetail>();
            if (mOldPanel == null)
            {
                LogTool.LogError("[MainAttrDetailNew] 找不到 MainAttrDetail 组件");
                return;
            }

            // AttrName 在 AttrItem 下，AttrItem 在 Content 下
            var attrItem = mOldPanel.AttrName.transform.parent;
            mContent = attrItem.parent;
            mTemplate = attrItem.gameObject;
            mTemplate.SetActive(false);

            // 关闭按钮 - 播放关闭动画后关闭
            if (mOldPanel.ExitBtn != null)
            {
                mOldPanel.ExitBtn.onClick.AddListener(() =>
                {
                    PlayCloseAnimation(() =>
                    {
                        UIFramework.UIManager.ClosePanel(this);
                    });
                });
            }
        }

        protected override void OnBindViewModel(MainAttrDetailViewModel vm)
        {
            RefreshList(vm);
        }

        protected override void OnUnbindViewModel(MainAttrDetailViewModel vm)
        {
            ClearList();
        }

        private void ClearList()
        {
            if (mContent == null) return;

            for (int i = mContent.childCount - 1; i >= 0; i--)
            {
                var child = mContent.GetChild(i);
                if (child.gameObject != mTemplate)
                {
                    Destroy(child.gameObject);
                }
            }
        }

        private void RefreshList(MainAttrDetailViewModel vm)
        {
            ClearList();
            if (mContent == null || mTemplate == null) return;

            foreach (var row in vm.Rows)
            {
                var item = Instantiate(mTemplate, mContent);
                item.SetActive(true);

                var attrName = item.transform.Find("AttrName")?.GetComponent<TMP_Text>();
                var attrValue = item.transform.Find("AttrValue")?.GetComponent<TMP_Text>();

                if (attrName != null) attrName.text = row.AttrNameText;
                if (attrValue != null) attrValue.text = row.AttrValueText;
            }
        }

        protected override void OnOpen(IUIData uiData = null)
        {
            PlayOpenAnimation();
        }

        protected override void OnShow() { }

        protected override void OnHide()
        {
            PlayCloseAnimation();
        }

        private void PlayOpenAnimation()
        {
            if (mOldPanel?.OpenAnimation != null)
            {
                UIAnimationUtils.PlayAnimations(mOldPanel.OpenAnimation.gameObject);
            }
        }

        private void PlayCloseAnimation(UnityEngine.Events.UnityAction onComplete = null)
        {
            if (mOldPanel?.CloseAnimation != null)
            {
                UIAnimationUtils.PlayAnimations(mOldPanel.CloseAnimation.gameObject, onComplete);
            }
            else
            {
                onComplete?.Invoke();
            }
        }
    }
}
