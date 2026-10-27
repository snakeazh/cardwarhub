using System.Collections.Generic;
using Config;
using DG.Tweening;
using Game.MainInterface;
using TMPro;
using UIFramework;
using UnityEngine;
using UnityEngine.UI;

namespace FusionContinent
{
    [PrefabPath("UpgradePanel")]
    public class UpgradePanelNew : UIPanel<UpgradePanelViewModel>
    {
        private UpgradePanel mOldPanel;
        private readonly Dictionary<ModuleFunctionType, GameObject> mItemsDic = new Dictionary<ModuleFunctionType, GameObject>();

        void Awake()
        {
            mOldPanel = GetComponent<UpgradePanel>();
            if (mOldPanel == null)
            {
                LogTool.LogError("[UpgradePanelNew] 找不到 UpgradePanel 组件");
            }
        }

        protected override void OnBindViewModel(UpgradePanelViewModel vm)
        {
            mOldPanel.LevelText.text = vm.CurrentLevel.ToString();
            BuildUnlockList(vm);
            mOldPanel.ExitBtn.onClick.AddListener(OnExitClicked);
        }

        protected override void OnUnbindViewModel(UpgradePanelViewModel vm)
        {
            mOldPanel.ExitBtn.onClick.RemoveListener(OnExitClicked);
        }

        protected override void OnShow()
        {
            Global.Player.SetModuleUnlockLv(Global.User.Level);
        }

        protected override void OnClose()
        {
            if (VM.OnClosed != null)
            {
                VM.OnClosed.Invoke();
            }
        }

        private void BuildUnlockList(UpgradePanelViewModel vm)
        {
            mOldPanel.UnlockList.gameObject.SetActive(vm.UnlockItems.Count > 0);
            for (int i = 0; i < vm.UnlockItems.Count; i++)
            {
                var item = Instantiate(mOldPanel.List.GetChild(0).gameObject, mOldPanel.List);
                item.gameObject.SetActive(true);
                var unlockItem = vm.UnlockItems[i];
                item.transform.Find("Bg").GetComponent<Image>().sprite = Global.UpgradeItemAtlas.GetSpriteExt(unlockItem.Icon);
                item.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = unlockItem.Name;
                mItemsDic.Add(unlockItem.ModuleType, item);
            }
        }

        private void OnExitClicked()
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(mOldPanel.List);
            DoItemMoveAni();
        }

        private void DoItemMoveAni()
        {
            Sequence sequence = DOTween.Sequence();
            // 统一延迟 0.5秒
            sequence.AppendInterval(0.5f);

            foreach (var kvp in mItemsDic)
            {
                GameObject target = GetItemTarget(kvp.Key);
                if (target != null)
                {
                    Tween jumpTween = kvp.Value.transform.DOJump(target.transform.position, 1f, 1, 0.5f);
                    sequence.Join(jumpTween);
                }
            }

            sequence.OnComplete(OnMoveAniComplete);
        }

        private void OnMoveAniComplete()
        {
            CloseSelf();
        }

        private GameObject GetItemTarget(ModuleFunctionType type)
        {
            var mainInterPanel = UIFramework.UIManager.GetPanel<MainInterfacePanelNew>();

            switch (type)
            {
                //主界面的节点获取
                case ModuleFunctionType.SingIn:
                    return mainInterPanel.GetOldPanel().SingInBtn.gameObject;
                case ModuleFunctionType.FundCashback:
                    return mainInterPanel.GetOldPanel().FundCashback.gameObject;
                case ModuleFunctionType.PassCheck:
                    return mainInterPanel.GetOldPanel().PassCheck.gameObject;
                case ModuleFunctionType.MouthyCard:
                    return mainInterPanel.GetOldPanel().MonthCardBtn.gameObject;
                case ModuleFunctionType.FirstReCharge:
                    return mainInterPanel.GetOldPanel().FirstRechargeBtn.gameObject;
                case ModuleFunctionType.GiftPack:
                case ModuleFunctionType.ActivityImport:
                    return mainInterPanel.GetOldPanel().ActivityBtn.gameObject;

                case ModuleFunctionType.SoldierAtlas:
                case ModuleFunctionType.SoldierSkin:
                case ModuleFunctionType.SoldierAwakening:
                    //英灵节点
                    return UnlockGuideManager.Instance.GetNavigation(GameMainButtonType.Hero).gameObject;

                case ModuleFunctionType.AchievementCollection:
                case ModuleFunctionType.WorldBig:
                case ModuleFunctionType.FusionStoneGamePlay:
                case ModuleFunctionType.WorldBoss:
                case ModuleFunctionType.EndlessTower:
                    return UnlockGuideManager.Instance.GetNavigation(GameMainButtonType.World).gameObject;

                case ModuleFunctionType.Shop:
                    return UnlockGuideManager.Instance.GetNavigation(GameMainButtonType.Shop).gameObject;

                case ModuleFunctionType.Talent:
                case ModuleFunctionType.Equip:
                case ModuleFunctionType.FusionGemSlot:
                case ModuleFunctionType.PlayerAvatar:
                case ModuleFunctionType.PlayerFigure:
                    //轮语者节点
                    return UnlockGuideManager.Instance.GetNavigation(GameMainButtonType.Player).gameObject;

                case ModuleFunctionType.GeneralSkill:
                    //奥术卡节点
                    return UnlockGuideManager.Instance.GetNavigation(GameMainButtonType.GeneralSkill).gameObject;
                default:
                    break;
            }

            return null;
        }
    }
}
