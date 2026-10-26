using System;
using System.Collections.Generic;
using Config;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using TMPro;
using UnityEngine.U2D;

namespace FusionContinent
{
    public class UpgradePanelData : UIPanelData
    {
    }
    public partial class UpgradePanel : UIPanel
    {
        public Action OnClosePanel;
        
        private List<ModuleFunctionConfig> _modifiers;
        private Dictionary<ModuleFunctionType, GameObject> _itemsDic = new Dictionary<ModuleFunctionType, GameObject>();
        private TbModuleFunctionConfig _tbModuleFunctionConfig;
        protected override void OnInit(IUIData uiData = null)
        {
            mData = uiData as UpgradePanelData ?? new UpgradePanelData();
            // please add init code here
            _tbModuleFunctionConfig = DataConfigManager.Tables.TbModuleFunctionConfig;
            ExitBtn.onClick.AddListener(() =>
            {
                //先注释掉以前的动画
                // UIKit.ClosePanel(this);
                LayoutRebuilder.ForceRebuildLayoutImmediate(List);
                DoItemMoveAni();
            });
        }

        /// <summary>
        /// 设置当前等级和解锁功能列表
        /// </summary>
        /// <param name="currentLevel"></param>
        public void SetData(int lastLevel,int currentLevel)
        {
            LevelText.text = currentLevel.ToString();
            _modifiers = new List<ModuleFunctionConfig>();
            for (int i = lastLevel + 1; i <= currentLevel; i++)
            {
                foreach (var moduleFunctionConfig in _tbModuleFunctionConfig.DataList)
                {
                    if ( moduleFunctionConfig.UnlockUserLv == i)
                    {
                        if (moduleFunctionConfig.FlyIcon)
                        {
                            _modifiers.Add(moduleFunctionConfig);
                        }
                    }
                }
            }

            if (!_modifiers.IsNullOrEmpty())
            {
                UnlockList.gameObject.SetActive(true);
                for (int i = 0; i < _modifiers.Count; i++)
                {
                    // GameObject item = i < List.childCount ? List.GetChild(i).gameObject : Instantiate(List.GetChild((0)).gameObject, List);
                    GameObject item = Instantiate(List.GetChild((0)).gameObject, List);
                    item.gameObject.SetActive(true);
                    Sprite s = Global.UpgradeItemAtlas.GetSpriteExt(_modifiers[i].Icon);
                    item.transform.Find("Bg").GetComponent<Image>().sprite = s;
                    item.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = _modifiers[i].Name;
                    _itemsDic.Add(_modifiers[i].ModuleType, item);
                }
                
            }
            else
            {
                UnlockList.gameObject.SetActive(false);
            }
			
        }
	
        private void DoItemMoveAni()
        {
            Sequence sequence = DOTween.Sequence();
            // 统一延迟 0.5秒
            sequence.AppendInterval(0.5f);
    
            foreach (var kvp in _itemsDic)
            {
                ModuleFunctionType type = kvp.Key;
                GameObject item = kvp.Value;
                GameObject target = GetItemTarget(type);
        
                if (target != null)
                {
                    Tween jumpTween = item.transform.DOJump(target.transform.position, 1f, 1, 0.5f);
                    // sequence.Append(jumpTween);
                    // jumpTween.OnComplete(() => Destroy(item));
            
                    sequence.Join(jumpTween);
                }
            }

            sequence.OnComplete(() =>
            {
                UIKit.ClosePanel(this);
            });
        }

        private GameObject GetItemTarget(ModuleFunctionType type)
        {

            var mainInterPanel = UIFramework.UIManager.GetPanel<MainInterfacePanelNew>();
            
            switch (type)
            {
                //主界面的节点获取
                case ModuleFunctionType.SingIn:
                    return mainInterPanel.GetOldPanel().SingInBtn.gameObject;
                    break;
                case ModuleFunctionType.FundCashback:
                    return mainInterPanel.GetOldPanel().FundCashback.gameObject;
                    break;
                
                case ModuleFunctionType.PassCheck:
                    return mainInterPanel.GetOldPanel().PassCheck.gameObject;
                    break;
                
                case ModuleFunctionType.MouthyCard:
                    return mainInterPanel.GetOldPanel().MonthCardBtn.gameObject;
                    break;
                case ModuleFunctionType.FirstReCharge:
                    return mainInterPanel.GetOldPanel().FirstRechargeBtn.gameObject;
                    break;
                case ModuleFunctionType.GiftPack:
                case ModuleFunctionType.ActivityImport:
                    return mainInterPanel.GetOldPanel().ActivityBtn.gameObject;
                    break;

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
		
        protected override void OnOpen(IUIData uiData = null)
        {
        }
		
        protected override void OnShow()
        {
            Global.Player.SetModuleUnlockLv(Global.User.Level);
        }
		
        protected override void OnHide()
        {
        }
		
        protected override void OnClose()
        {
            OnClosePanel?.Invoke();
            // MainInterfacePanel.Default.DailyPopUpLogic();
        }
    }
}