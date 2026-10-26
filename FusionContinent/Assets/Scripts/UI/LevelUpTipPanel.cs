using System;
using System.Collections.Generic;
using System.Linq;
using Config;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using TMPro;

namespace FusionContinent
{
    public class LevelUpTipPanelData : UIPanelData
    {
    }

    public partial class LevelUpTipPanel : UIPanel
    {
        private TbModuleFunctionConfig _moduleFunctionConfig;

        private void Awake()
        {
            _moduleFunctionConfig = DataConfigManager.Tables.TbModuleFunctionConfig;
        }

        protected override void OnInit(IUIData uiData = null)
        {
            mData = uiData as LevelUpTipPanelData ?? new LevelUpTipPanelData();
            QuitBtn.onClick.AddListener(() => { UIKit.HidePanel<LevelUpTipPanel>(); });
            // please add init code here
        }

        protected override void OnOpen(IUIData uiData = null)
        {
            SetLevel();
        }

        private void SetLevel()
        {
            int level = Global.User.Level;
            Leveltxt.text = level.ToString();

            bool hasNewFunction = IsModuleFun(level);
            NewFunOpen.gameObject.SetActive(hasNewFunction);

            if (hasNewFunction)
            {
                ClearContentChildren();
                CreateNewFunctionItems(level);
            }
        }

        private bool IsModuleFun(int level)
        {
            return _moduleFunctionConfig?.DataList?.Any(t => t.UnlockUserLv == level) ?? false;
        }

        private void ClearContentChildren()
        {
            // 从后往前删除子物体，保留第一个（索引0）
            for (int i = Content.childCount - 1; i >= 1; i--)
            {
                Destroy(Content.GetChild(i).gameObject);
            }
        }

        private void CreateNewFunctionItems(int level)
        {
            var newFunList = _moduleFunctionConfig.DataList
                .Where(t => t.UnlockUserLv == level)
                .ToList();

            foreach (var config in newFunList)
            {
                CreateFunctionItem(config);
            }
        }

        private void CreateFunctionItem(ModuleFunctionConfig config)
        {
            GameObject funObj = Instantiate(FunObj.gameObject, Content);
            funObj.transform.localScale = Vector3.one;
            funObj.transform.localPosition = Vector3.zero;
            funObj.gameObject.SetActive(true);

            // 使用 TryGetComponent 避免空引用异常
            Transform iconTransform = funObj.transform.Find("icon");
            if (iconTransform != null && iconTransform.TryGetComponent<Image>(out var iconImage))
            {
                // 这里应该设置实际的sprite，而不是null
                // iconImage.sprite = GetFunctionIcon(config.IconName);
                iconImage.sprite = Global.ItemIconSpriteAtlas.GetSprite(config.Icon); // 暂时保持原样，建议后续替换
            }

            Transform nameTransform = funObj.transform.Find("name");
            if (nameTransform != null && nameTransform.TryGetComponent<TMP_Text>(out var nameText))
            {
                nameText.text = config.Name;
            }
        }

        protected override void OnShow()
        {
        }

        protected override void OnHide()
        {
        }

        protected override void OnClose()
        {
        }
    }
}