using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Config;
using DG.Tweening;
using FusionContinent.Manager;
using FusionContinent.Model;
using FusionContinent.Sounds;
using Manager;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using TMPro;
using UnityEngine.EventSystems;
using Game.RedDot;

namespace FusionContinent
{
    public class EquipUpGradePanelData : UIPanelData
    {
    }
    public partial class EquipUpGradePanel : UIPanel
    {
        private int _id;
        private int _soldierId;
        private bool _worldPanelOpen = false;// 是否从世界界面打开
        private bool _isSetDataRunning = false;
        private readonly List<int> _optionEntryList = new List<int>();
		
        private List<Button> _buttonList = new List<Button>();
        private List<PlayerEquip> _wearEquipList = new List<PlayerEquip>();
        private Dictionary<int, GameObject> _wearEquipSlots = new Dictionary<int, GameObject>();
        private SelectSoldierPopup _selectSoldierPopup;

        private TbEquipEntryConfig _entryConfig;
        private TbCardBaseConfig _cardBaseConfig;
        private TbEquipConfig _equipCardConfig;
        private TbSoldierBattleConfig _soldierBattleConfig;
		
        protected void Awake()
        {
            _entryConfig = DataConfigManager.Tables.TbEquipEntryConfig;
            _cardBaseConfig = DataConfigManager.Tables.TbCardBaseConfig;
            _equipCardConfig = DataConfigManager.Tables.TbEquipConfig;
            _soldierBattleConfig = DataConfigManager.Tables.TbSoldierBattleConfig;
        }
		
        protected override void OnInit(IUIData uiData = null)
        {
            mData = uiData as EquipUpGradePanelData ?? new EquipUpGradePanelData();
			
            GoBackBtn.onClick.AddListener(OnClose);
            _buttonList.Add(GoBackBtn);
			
            UpGradeBtn.onClick.AddListener(() =>
            {
                UnlockGuideManager.Send(ModuleFunctionType.EquipPhase,4);
                // UnlockGuideManager.Send(ModuleFunctionType.Equip,6);
                // GameMainPanel.Default.ShowConfirm("确认锻造", "", UpPhaseEquip);
            });
			
        }

        public void ChangePage(int index)
        {
            _buttonList[index]?.onClick.Invoke();

            // 锻造
            if (index == 1)
            {
                // 引导 - 选择锻造按钮
                //GuideFlow.Instance.SetStep(GuideType.EquipUpgradePhase, 2, UpGradeBtn.transform);
            }
        }

        public void SetData()
        {
            _selectSoldierPopup = UIKit.OpenPanel<SelectSoldierPopup>();
            _selectSoldierPopup.SetData((soldierId) =>
            {
                _wearEquipList = Global.PlayerEquips.Values.Where(v => v.WearerId == soldierId).ToList();
                if (_wearEquipList.IsNullOrEmpty())
                {
                    UIKit.ClosePanel<SelectSoldierPopup>();
                } 
                
                if (SetData(soldierId))
                    Global.Publisher.Publish(MessagePublisher.CHANGE_GAME_MAIN_FOOT_BTN_ENABLED, false);
            });
            _selectSoldierPopup.transform.SetParent(transform.Find("Canvas"));
            _selectSoldierPopup.transform.localScale = Vector3.one;
            InitWearEquipSlot();
            _worldPanelOpen = true;
        }

        private GameObject GuideAttr;
        public bool SetData(int soldierId, int id = 0)
        {
            GuideAttr = null;
            
            if (_isSetDataRunning) return false;
            _isSetDataRunning = true;
            // _worldPanelOpen = false;

            _wearEquipList = Global.PlayerEquips.Values.Where(v => v.WearerId == soldierId).ToList();
            if (_wearEquipList.IsNullOrEmpty())
            {
                FusionContinent.Toast.Show("角色未佩戴装备");
                _isSetDataRunning = false;
                UIKit.ClosePanel<EquipUpGradePanel>();
                return false;
            }

            if (id == 0)// 默认选中第一个
            {
                id = _wearEquipList.First().Id;
            }

            _id = id;
            _soldierId = soldierId;
            InitWearEquipSlot();

            var equip = Global.PlayerEquips[_id];
            var equipCfg = _equipCardConfig.GetOrDefault(equip.EquipId);
			
            // 装备
            NameText.text = equipCfg.Name;
            Equip.GetComponent<RewardEntity>().SetData(new RewardEntityData()
            {
                Quality = equip.Quality,
                IconSprite = Global.EquipIconSpriteAtlas.GetSprite(equipCfg.Icon),
                StyleType = RewardStyleType.Equip,
                IsEffect = false,
                OnClick = () => {}
            });
			
            // 等级
            var nextPhase = equip.Phase + 1;
            Level.gameObject.SetActive(true);
            Level.Find("LevelText").GetComponent<TMP_Text>().text = $"Lv{equip.Phase}";
            Level.Find("NextLevelText").GetComponent<TMP_Text>().text = $"Lv{nextPhase}";
			
         
			         //
            // // 属性
            // var phaseCfg = equip.GetPhaseCfg();
            // var nextPhaseCfg = equip.GetPhaseCfg(equip.Phase);
            // var mainAttribute = UpGrade.Find("MainAttribute");
            // for (int i = 0; i < mainAttribute.childCount; i++)
            // {
            //     var attrObj = mainAttribute.GetChild(i).gameObject;
            //     attrObj.SetActive(phaseCfg.MainAttributes.Count > i);
            //     if (attrObj.activeSelf)
            //     {
            //         var currentCfg = phaseCfg.MainAttributes[i];
            //         attrObj.transform.Find("Attr/Text").GetComponent<RollingNumberManager>().Play((int)currentCfg.Value);
            //         // attrObj.transform.Find("Attr/Text").GetComponent<TMP_Text>().text = $"{currentCfg.Value}";
            //         attrObj.transform.Find("Attr/Icon").GetComponent<Image>().sprite = Global.CardIconSpriteAtlas.GetSpriteExt(currentCfg.Type.ToString());
				        //
            //         var nextCfg = nextPhaseCfg.MainAttributes[i];
            //         attrObj.transform.Find("NextAttr/Text").GetComponent<RollingNumberManager>().Play((int)nextCfg.Value);
            //         // attrObj.transform.Find("NextAttr/Text").GetComponent<TMP_Text>().text = $"{nextCfg.Value}";
            //         attrObj.transform.Find("NextAttr/Icon").GetComponent<Image>().sprite = Global.CardIconSpriteAtlas.GetSpriteExt(nextCfg.Type.ToString());
            //     }
            // }

            // // 消耗
            // UpGradeBtn.interactable = RefreshConsumes(UpGrade.Find("Consumes"), phaseCfg.UpPhaseConsumes);
            // if (phaseCfg.UpPhaseConsumes.Count == 0)
            // {
            //     UpGradeBtn.interactable = false;
            //     Level.gameObject.SetActive(false);
            // }
            // UpGradeBtn.transform.Find("Text").GetComponent<TMP_Text>().text = (phaseCfg.Quality != nextPhaseCfg.Quality) ? "突破" : "锻造";
            //
            // _isSetDataRunning = false;
            return true;
        }

        private bool RefreshConsumes(Transform consumes, List<RewardAndNum> consumeItems, int redouble = 1)
        {
            var isEnough = true;
            for (int i = 0; i < consumes.childCount; i++)
            {
                var consumeObj = consumes.GetChild(i).gameObject;
                consumeObj.SetActive(consumeItems.Count > i);
                if (!consumeObj.activeSelf) continue;

                var item = consumeItems[i];
                if (!consumeObj.GetComponent<RewardEntity>().SetData(item.Id, item.Num * redouble, RewardStyleType.Consume))
                {
                    isEnough = false; 
                }
            }

            return isEnough;
        }

        private void InitWearEquipSlot()
        {
            if (_wearEquipList.Count == 0) return;
			
            var wearEquipScroll = WearEquipScroll.Find("Viewport/Content");
            foreach (var equip in _wearEquipList)
            {
                var equipCfg = _equipCardConfig.GetOrDefault(equip.EquipId);
                if (!_wearEquipSlots.TryGetValue(equip.Id, out GameObject slotObj))
                {
                    slotObj = Instantiate(wearEquipScroll.Find("RewardEntity").gameObject, wearEquipScroll, true);
                    _wearEquipSlots.Add(equip.Id, slotObj);
                }

                slotObj.SetActive(true);
                // if (CheckEquipRedPoint(equip.GetPhaseCfg().UpPhaseConsumes) && equip.GetPhaseCfg().UpPhaseConsumes.Count != 0)
                // {
                //     slotObj.GetComponent<LocalRedDot>().SetRed(1);
                // }
                // else
                // {
                //     slotObj.GetComponent<LocalRedDot>().SetRed(0);
                // }

                var button = slotObj.GetComponent<Button>();
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() =>
                {
                    if (_id == equip.Id) return;
                    SetData(_soldierId, equip.Id);
                });

                slotObj.GetComponent<RewardEntity>().SetData(new RewardEntityData()
                {
                    Quality = equip.Quality,
                    IconSprite = Global.EquipIconSpriteAtlas.GetSprite(equipCfg.Icon),
                    StyleType = RewardStyleType.Equip,
                    IsEffect = false,
                    Check = _id == equip.Id,
                    OnClick = null
                });
            }
        }

        private bool CheckEquipRedPoint(List<RewardAndNum> consumeItems)
        {
            foreach (var v in consumeItems)
            {
                if (v.Num == 0) return false;
                var itemNum = Global.GetItemNumById(v.Id);
                if (itemNum < v.Num) return false;
            }
            return true;
        }
        //
        // private async void UpPhaseEquip()
        // {
        //     try
        //     {
        //         UpGradeBtn.interactable = false;
        //         var response = await WebDataFun.SendPostRequest(ApiKeys.ApiEquipUpPhase, new Dictionary<string, string>
        //         {
        //             {"Ids", $"[{_id}]"}
        //         });
        //         
        //         if (!string.IsNullOrEmpty(response))
        //         {
        //             var result = JsonConvert.DeserializeObject<UpPhaseResult>(response);
        //             foreach (var v in result.Equips)
        //             {
        //                 Global.PlayerEquips[v.Id] = new PlayerEquip(v);
        //             }
        //             
        //             // 同步奖励
        //             Items.Update(result.Items);
        //
        //             // 刷新数据
        //             UpGradeEffect(() => SetData(_soldierId, _id));
					   //
        //             // 重新计算战力
        //             UserModel.SumFightingNum();
        //             CombatTipController.Instance.SetCombat();
        //             
        //             // 完成引导
        //             GuideFlow.Instance.SetStepComplete(GuideType.EquipUpgradePhase);
        //             Global.Publisher.Publish(MessagePublisher.CHANGE_RED_TYPE+RedNodeType.EquipUpGradeBtn,null);
        //         }
        //     }
        //     catch (System.Exception ex)
        //     {
        //         LogTool.LogError($"装备升级失败：{ex.Message}");
        //         FusionContinent.Toast.Show($"操作失败：{ex.Message}", 5f);
        //     }
        //     finally
        //     {
        //         UpGradeBtn.interactable = true;
        //     }
        // }
		      //

        protected override void OnOpen(IUIData uiData = null)
        {
            ChangePage(1);
        }
        
        protected override void OnShow()
        {
            _worldPanelOpen = false;
            StartCoroutine(WaitGuide());
        }

        private IEnumerator WaitGuide()
        {
            yield return null;
            yield return null;
            if (UpGradeBtn != null&& UpGradeBtn.interactable)
            {
                // UnlockGuideManager.Instance.SetUnlockTypeStep(ModuleFunctionType.Equip,6,UpGradeBtn.GetComponent<RectTransform>());
            }
            else
            {
                // UnlockGuideManager.Instance.SetErrorTypeStep(ModuleFunctionType.Equip,6);
            }
            // UnlockGuideManager.Instance.SetUnlockTypeStep(ModuleFunctionType.EquipEntry,4,EntryBtn.GetComponent<RectTransform>());
            if (GuideAttr != null)
            {
                UnlockGuideManager.Instance.SetUnlockTypeStep(ModuleFunctionType.EquipEntry,5,GuideAttr.GetComponent<RectTransform>());
            }
            else
            {
                UnlockGuideManager.Instance.SetErrorTypeStep(ModuleFunctionType.EquipEntry,5);
                yield break;
            }
            // UnlockGuideManager.Instance.SetUnlockTypeStep(ModuleFunctionType.EquipEntry,6,RefreshEntryBtn.GetComponent<RectTransform>());

            
            
        }
        protected override void OnHide()
        {
        }
		
        protected override void OnClose()
        {
            gameObject.SetActive(false);
            if (_worldPanelOpen)
                Global.Publisher.Publish(MessagePublisher.CHANGE_GAME_MAIN_FOOT_BTN_ENABLED, true);
        }

        private void UpGradeEffect(Action callback)
        {
            ForgingEffectHammer01.gameObject.SetActive(true);
            DOVirtual.DelayedCall(0.4f, () =>
            {
                callback();
                SoundManager.Instance.PlayVoice(SoundType.SoundEquipForging);
				
                ForgingEffectUpgrade01.gameObject.SetActive(true);
                DOVirtual.DelayedCall(1f, () =>
                {
                    ForgingEffectHammer01.gameObject.SetActive(false);
                    ForgingEffectUpgrade01.gameObject.SetActive(false);
                });
            });
        }
    }
}