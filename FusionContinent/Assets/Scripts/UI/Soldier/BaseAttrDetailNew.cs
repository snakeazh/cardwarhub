using FusionContinent.Animation;
using Game.Soldier;
using TMPro;
using UIFramework;
using UnityEngine;

namespace FusionContinent
{
    [PrefabPath("BaseAttrDetail")]
    public class BaseAttrDetailNew : UIPanel<BaseAttrDetailViewModel>
    {
        private BaseAttrDetail mOldPanel;
        private Transform mContent;
        private GameObject mTemplate;

        void Awake()
        {
            mOldPanel = GetComponent<BaseAttrDetail>();
            if (mOldPanel == null)
            {
                LogTool.LogError("[BaseAttrDetailNew] 找不到 BaseAttrDetail 组件");
                return;
            }

            // AttrValue1 在 AttrItem 下，AttrItem 在 Content 下
            var attrItem = mOldPanel.AttrValue1.transform.parent;
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

        protected override void OnBindViewModel(BaseAttrDetailViewModel vm)
        {
            RefreshList(vm);
        }

        protected override void OnUnbindViewModel(BaseAttrDetailViewModel vm)
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

        private void RefreshList(BaseAttrDetailViewModel vm)
        {
            ClearList();
            if (mContent == null || mTemplate == null) return;

            foreach (var row in vm.Rows)
            {
                var item = Instantiate(mTemplate, mContent);
                item.SetActive(true);

                var attrValue1 = item.transform.Find("AttrValue1")?.GetComponent<TMP_Text>();
                var attrValue1Num = item.transform.Find("AttrValue1Num")?.GetComponent<TMP_Text>();

                if (attrValue1 != null) attrValue1.text = row.AttrName;
                if (attrValue1Num != null) attrValue1Num.text = row.AttrValue;
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
