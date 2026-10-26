using System.Collections.Generic;
using Game.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UIFramework;

namespace FusionContinent
{
    /// <summary>
    /// 特殊效果面板（通用版，供武器/皮肤形象共用）。
    /// 展示当前道具各星级的特殊效果（描述取自 StarLevelInfo.Desc2）。
    /// 列表项使用裸 ScrollRect + EffectItem 模板手动填充。
    /// </summary>
    [PrefabPath("SpecialEPanel")]
    public class StarSpecialEPanelNew : UIPanel<StarSpecialEPanelViewModel>
    {
        private SpecialEPanel mOldPanel;

        // 列表项模板：EffectScrollView.content 下的编辑期 EffectItem 实例
        private EffectItem mEffectItemTemplate;
        private readonly List<EffectItem> mEffectItemInstances = new List<EffectItem>();

        void Awake()
        {
            mOldPanel = GetComponent<SpecialEPanel>();
            if (mOldPanel == null)
            {
                LogTool.LogError("[StarSpecialEPanelNew] 找不到 SpecialEPanel 组件");
                return;
            }

            CacheEffectItemTemplate();
        }

        protected override void OnBindViewModel(StarSpecialEPanelViewModel vm)
        {
            if (mOldPanel == null) return;

            // 退出
            if (mOldPanel.ExitBtn != null)
            {
                mOldPanel.ExitBtn.onClick.AddListener(OnExitClicked);
            }
            if (mOldPanel.Mask != null)
            {
                mOldPanel.Mask.onClick.AddListener(OnExitClicked);
            }

            // 数据绑定
            vm.SkillName.OnValueChanged += OnSkillNameChanged;
            vm.CurrentLevel.OnValueChanged += OnCurrentLevelChanged;
            vm.EffectList.OnValueChanged += OnEffectListChanged;

            // 首次同步一次（避免打开时未触发 OnValueChanged）
            OnSkillNameChanged(vm.SkillName.Value, vm.SkillName.Value);
            RefreshEffectList(vm);
        }

        protected override void OnUnbindViewModel(StarSpecialEPanelViewModel vm)
        {
            if (mOldPanel == null) return;

            if (mOldPanel.ExitBtn != null)
            {
                mOldPanel.ExitBtn.onClick.RemoveListener(OnExitClicked);
            }
            if (mOldPanel.Mask != null)
            {
                mOldPanel.Mask.onClick.RemoveListener(OnExitClicked);
            }

            vm.SkillName.OnValueChanged -= OnSkillNameChanged;
            vm.CurrentLevel.OnValueChanged -= OnCurrentLevelChanged;
            vm.EffectList.OnValueChanged -= OnEffectListChanged;

            ClearEffectItems();
        }

        protected override void OnOpen(IUIData uiData = null) { }

        protected override void OnShow() { }
        protected override void OnHide() { }

        protected override void ReleasePanelResources()
        {
            ClearEffectItems();
            base.ReleasePanelResources();
        }

        private void OnExitClicked()
        {
            CloseSelf();
        }

        private void OnSkillNameChanged(string oldValue, string newValue)
        {
            if (mOldPanel.SkillName != null)
            {
                mOldPanel.SkillName.text = newValue;
            }
        }

        // 当前星级变化也会影响锁定状态，需刷新列表
        private void OnCurrentLevelChanged(int oldValue, int newValue)
        {
            if (VM != null)
            {
                RefreshEffectList(VM);
            }
        }

        private void OnEffectListChanged(List<StarLevelInfo> oldValue, List<StarLevelInfo> newValue)
        {
            if (VM != null)
            {
                RefreshEffectList(VM);
            }
        }

        /// <summary>把 EffectScrollView.content 下的编辑期 EffectItem 作为模板缓存，并隐藏模板本体。</summary>
        private void CacheEffectItemTemplate()
        {
            if (mOldPanel.EffectScrollView == null || mOldPanel.EffectScrollView.content == null)
            {
                LogTool.LogError("[StarSpecialEPanelNew] EffectScrollView.content 为空，无法获取列表项模板");
                return;
            }

            mEffectItemTemplate = mOldPanel.EffectScrollView.content.GetComponentInChildren<EffectItem>(true);
            if (mEffectItemTemplate != null)
            {
                mEffectItemTemplate.gameObject.SetActive(false);
            }
            else
            {
                LogTool.LogError("[StarSpecialEPanelNew] EffectScrollView.content 下找不到 EffectItem 模板");
            }
        }

        private void RefreshEffectList(StarSpecialEPanelViewModel vm)
        {
            if (mEffectItemTemplate == null)
            {
                return;
            }

            ClearEffectItems();

            var list = vm.EffectList.Value;
            var currentLevel = vm.CurrentLevel.Value;
            if (list == null)
            {
                return;
            }

            var content = mOldPanel.EffectScrollView.content;
            for (int i = 0; i < list.Count; i++)
            {
                var star = list[i];
                if (star == null)
                {
                    continue;
                }

                var effectItem = Instantiate(mEffectItemTemplate, content, false);
                effectItem.gameObject.name = "EffectItem_" + i;
                effectItem.gameObject.SetActive(true);
                effectItem.SetData(new EffectItemData
                {
                    skillLevel = star.Level,
                    skillDes = star.Desc2,
                    isLock = star.Level > currentLevel,
                });
                mEffectItemInstances.Add(effectItem);
            }
        }

        private void ClearEffectItems()
        {
            foreach (var instance in mEffectItemInstances)
            {
                if (instance != null)
                {
                    Destroy(instance.gameObject);
                }
            }
            mEffectItemInstances.Clear();
        }
    }
}
