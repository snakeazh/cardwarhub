using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Config;
using FusionContinent.Model;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using TMPro;
using UIFramework;

namespace FusionContinent
{
	public class EquipPanelData : UIPanelData
	{
	}

	public partial class EquipPanel : QFramework.UIPanel
	{
		public static EquipPanel Default;
		
		private int _soldierId;
		private List<PlayerEquip> _wearEquipList = new List<PlayerEquip>();
		private readonly Dictionary<int, GameObject> _gemSlots = new Dictionary<int, GameObject>();
		private readonly Dictionary<EquipPartType, EquipCardSlotData> _wearPartSlots = new Dictionary<EquipPartType, EquipCardSlotData>();
		public Dictionary<EquipPartType, EquipCardSlotData> GetWearPartSlots() => _wearPartSlots;
		
		private TbEquipConfig _equipCardConfig;
		
		protected void Awake()
		{
			Default = this;
			_equipCardConfig = DataConfigManager.Tables.TbEquipConfig;
		}
		
		protected override void OnInit(QFramework.IUIData uiData = null)
		{
			mData = uiData as EquipPanelData ?? new EquipPanelData();
		}

		public void SetData(int soldierId)
		{
			// _soldierId = soldierId;
			// if (_soldierId == 0)
			// {
			// 	FusionContinent.Toast.Show("英灵未解锁");
			// 	return;
			// }
			//
			// var cardCfg = _cardBaseConfig.GetOrDefault(soldierId);
			// var soldierCfg = _soldierBattleConfig.GetOrDefault(soldierId);
			// if (_soldierId == 0 || cardCfg == null || soldierCfg == null)
			// 	throw new Exception("角色ID错误");
			//
			// // 已佩戴的装备列表
			// _wearEquipList = Global.PlayerEquips.Values.Where(v => v.WearerId == _soldierId).ToList();
			//
			// // 角色信息
			// ManType.transform.Find("NameText").GetComponent<TMP_Text>().text = soldierCfg.Name;
			// ManType.transform.Find("SoldierName").GetComponent<TMP_Text>().text = cardCfg.Name;
			// ManType.transform.Find("Icon").GetComponent<Image>().sprite = Global.CardIconSpriteAtlas.GetSpriteExt(soldierCfg.MainAttrType.ToString());
			//
			// // Spine
			// for (int i = 0; i < Spine.childCount; i++)
			// 	Destroy(Spine.GetChild(i).gameObject);
			//
			// var soliderSpine = Global.ResLoader.LoadSync<GameObject>(soldierCfg.Spine).Instantiate().transform;
			// soliderSpine.SetParent(Spine);
			// soliderSpine.localScale = Vector3.one;
			// soliderSpine.localPosition = Vector3.zero;
			// Spine.localScale = soldierCfg.SpineScale > 0 ? Vector3.one * soldierCfg.SpineScale : Vector3.one;
			//
			// InitGemSlot();
			// SetFightText();
			// InitEquipSlot();
			// InitWearEquipSlot();
			var equipPanel = UIFramework.UIManager.GetPanel<EquipPanelNew>();
			equipPanel?.VM?.RefreshData();
		}

		private void InitWearEquipSlot()
		{
			_wearPartSlots.Clear();
			var partNameList = DataConfigManager.Tables.TbGameBattleConst.EquipPartTypeList;
			for (int i = 0; i < WearEquips.childCount; i++)
			{
				var partType = (EquipPartType)i;
				var slotObj = WearEquips.GetChild(i).gameObject;
				slotObj.transform.Find("Text").GetComponent<TMP_Text>().text = partNameList[i];
				slotObj.transform.Find("RewardEntity").gameObject.SetActive(false);
				var slotData = new EquipCardSlotData()
				{
					SlotObj = slotObj,
					PartType = partType,
				};
				
				// 融合石卡槽
				var gemSlot = slotObj.transform.Find("GemSlot");
				slotData.LockFusionGem = !ModuleFunction.CheckOpen(ModuleFunctionType.FusionGemSlot, false);
				gemSlot.Find("Lock").gameObject.SetActive(slotData.LockFusionGem);
				
				// 已佩戴装备
				var equip = _wearEquipList.FirstOrDefault(v => v.PartType == partType);
				if (equip != null)
				{
					slotData.EquipOnlyId = equip.Id;
					var equipCfg = _equipCardConfig.GetOrDefault(equip.EquipId);
					slotObj.transform.Find("RewardEntity").gameObject.SetActive(true);
					slotObj.transform.Find("RewardEntity").GetComponent<RewardEntity>().SetData(new RewardEntityData()
					{
						Quality = equip.Quality,
						IconSprite = Global.EquipIconSpriteAtlas.GetSprite(equipCfg.Icon),
						StyleType = RewardStyleType.Equip,
						IsEffect = false,
						OnClick = () => GlobalFunc.OpenEquipDetailsPanel(equip.Id, _soldierId),
					});
					
					// // 融合石
					// if (!gemSlot.Find("Lock").gameObject.activeSelf)// 已解锁
					// {
					// 	var gem = Global.PlayerFusionGems.Values.FirstOrDefault(v => v.WearerId == equip.Id);
					// 	gemSlot.Find("Icon").gameObject.SetActive(gem != null);
					// 	if (gem != null)
					// 	{
					// 		slotData.FusionGem = gem;
					// 		gemSlot.Find("Icon").GetComponent<Image>().sprite = Global.FusionGemIconSpriteAtlas.GetSprite(gem.Config.Icon);
					// 		if (_gemSlots.TryGetValue(gem.Id, out var gemV)) // 已经佩戴的宝石，在列表里隐藏
					// 			gemV.gameObject.SetActive(false);
					// 	}
					// 	
					// 	// 点击事件
					// 	var gemSlotBtn = gemSlot.GetComponent<Button>();
					// 	gemSlotBtn.onClick.RemoveAllListeners();
					// 	gemSlotBtn.onClick.AddListener(() =>
					// 	{
					// 		if (gem == null) return;
					// 		
					// 		UIKit.OpenPanel<FusionGemDetailsPanel>(UILevel.PopUI).SetData(gem.Id, _soldierId, FusionGemDetailType.Wear);
					// 		LogTool.Log($"点击已佩戴融合石: {gem.Id}");
					// 	});
					// }
				}

				_wearPartSlots.Add(partType, slotData);
			}
		}

		private GameObject GuideEquip;
		/// <summary>
		/// 初始化装备列表（已注释，逻辑迁移到 EquipPanelNew）
		/// </summary>
		private void InitEquipSlot()
		{
			// GuideEquip = null;
			// var cardScroll = EquipCardScroll.Find("Viewport/Content");
			// for (int i = 0; i < cardScroll.childCount; i++)
			// 	cardScroll.GetChild(i).gameObject.SetActive(false);
			
			// var orderByDescList = Global.PlayerEquips.Values.OrderByDescending(x => x.Quality).ToList();
			// foreach (var equip in orderByDescList)
			// {
			// 	var equipCfg = _equipCardConfig.GetOrDefault(equip.EquipId);
			// 	if (!_equipSlots.TryGetValue(equip.Id, out var slotObj))
			// 	{
			// 		slotObj = Instantiate(cardScroll.Find("RewardEntity").gameObject, cardScroll);
			// 		_equipSlots.Add(equip.Id, slotObj);
			// 	}
				
			// 	slotObj.SetActive(!CheckWearEquip(equip.Id));
			// 	if (slotObj.activeSelf)
			// 	{
			// 		if (GuideEquip == null)
			// 		{
			// 			GuideEquip = slotObj;
			// 		}
			// 	}
			// 	slotObj.transform.GetComponent<RewardEntity>().SetData(new RewardEntityData()
			// 	{
			// 		Quality = equip.Quality,
			// 		IconSprite = Global.EquipIconSpriteAtlas.GetSprite(equipCfg.Icon),
			// 		StyleType = RewardStyleType.Equip,
			// 		SoldierId = equip.WearerId,
			// 		IsEffect = false,
			// 		OnClick = () =>
			// 		{
			// 			UnlockGuideManager.Send(ModuleFunctionType.Equip,3);
			// 			UIKit.OpenPanel<EquipDetailsPanel1>(UILevel.PopUI).SetData(equip.Id, _soldierId);
			// 			LogTool.Log($"+>>>>>>> 点击列表装备: {equip.EquipId}");
			// 		}
			// 	});
			// }
		}

		private GameObject GuideGem;
		/// <summary>
		/// 初始化融合石槽位（已注释，逻辑迁移到 EquipPanelNew）
		/// </summary>
		private void InitGemSlot()
		{
			// GuideGem = null;
			// var cardScroll = GemCardScroll.Find("Viewport/Content");
			// for (int i = 0; i < cardScroll.childCount; i++)
			// 	cardScroll.GetChild(i).gameObject.SetActive(false);
			
			// var orderByDescList = Global.PlayerFusionGems.Values.OrderByDescending(x => x.Quality).ToList();
			// foreach (var gem in orderByDescList)
			// {
			// 	if (!_gemSlots.TryGetValue(gem.Id, out var slotObj))
			// 	{
			// 		slotObj = Instantiate(cardScroll.Find("RewardEntity").gameObject, cardScroll);
			// 		_gemSlots.Add(gem.Id, slotObj);
			// 	}

			// 	slotObj.name = $"{gem.Id}";
			// 	slotObj.SetActive(true);
			// 	if (GuideGem == null)
			// 	{
			// 		GuideGem = slotObj;
			// 	}
			// 	slotObj.transform.GetComponent<RewardEntity>().SetData(new RewardEntityData()
			// 	{
			// 		Quality = gem.Quality,
			// 		IconSprite = Global.FusionGemIconSpriteAtlas.GetSprite(gem.Config.Icon),
			// 		StyleType = RewardStyleType.Equip,
			// 		IsEffect = false,
			// 		OnClick = () =>
			// 		{
			// 			UnlockGuideManager.Send(ModuleFunctionType.FusionGemSlot,4);
			// 			UIKit.OpenPanel<FusionGemDetailsPanel>(UILevel.PopUI).SetData(gem.Id, _soldierId);
			// 		}
			// 	});
				
			// 	slotObj.GetComponent<ScrollViewItem>().SetScrollView(GemCardScroll.GetComponent<ScrollRect>());
			// }
		}

		private bool CheckWearEquip(int id)
		{
			return _wearEquipList.Any(v => v.Id == id);
		}


		protected override void OnOpen(QFramework.IUIData uiData = null)
		{
			// 引导 - 选择锻造按钮
			//GuideFlow.Instance.SetStep(GuideType.EquipUpgradePhase, 1, UpGradeBtn.transform);
			Global.Publisher.Publish(MessagePublisher.CHANGE_GAME_MAIN_FOOT_BTN_ENABLED, false);
		}
		
		protected override void OnShow()
		{
			// 引导逻辑已注释，相关功能迁移到 EquipPanelNew
			// StartCoroutine(WaitGuide());
		}

		/// <summary>
		/// 引导相关（已注释，相关功能迁移到 EquipPanelNew）
		/// </summary>
		// private IEnumerator WaitGuide()
		// {
		// 	yield return null;
		// 	yield return null;
		// 	if (GuideEquip == null)
		// 	{
		// 		UnlockGuideManager.Instance.SetErrorTypeStep(ModuleFunctionType.Equip,3);
		// 	}
		// 	else
		// 	{
		// 		UnlockGuideManager.Instance.SetUnlockTypeStep(ModuleFunctionType.Equip, 3,
		// 			GuideEquip.GetComponent<RectTransform>());
		// 	}
			
		// 	UnlockGuideManager.Instance.SetUnlockTypeStep(ModuleFunctionType.EquipPhase, 3,
		// 		UpGradeBtn.GetComponent<RectTransform>());
		// 	UnlockGuideManager.Instance.SetUnlockTypeStep(ModuleFunctionType.EquipEntry, 3,
		// 		UpGradeBtn.GetComponent<RectTransform>());
		// 	if (GuideGem == null)
		// 	{
		// 		UnlockGuideManager.Instance.SetErrorTypeStep(ModuleFunctionType.FusionGemSlot,4);
		// 	}
		// 	else
		// 	{
		// 		UnlockGuideManager.Instance.SetUnlockTypeStep(ModuleFunctionType.FusionGemSlot, 4,
		// 			GuideGem.GetComponent<RectTransform>());
		// 	}

			
		// 	UnlockGuideManager.Instance.SetUnlockTypeStep(ModuleFunctionType.FusionGemSlot, 3,
		// 		Gem);
		// }

		protected override void OnHide()
		{
		}
		
		protected override void OnClose()
		{
			gameObject.SetActive(false);
			Global.Publisher.Publish(MessagePublisher.CHANGE_GAME_MAIN_FOOT_BTN_ENABLED, true);
		}
		
        
		/// <summary>
		/// 融合石合成（已注释，相关功能迁移到 EquipPanelNew）
		/// </summary>
		// private async void SynthesisFusionGem(int targetId, int dragId)
		// {
		// 	if (dragId == 0 || targetId == 0) return;
			
		// 	try
		// 	{
		// 		var synthesisList = new List<List<int>> { new List<int>{targetId, dragId} };
		// 		var response = await WebDataFun.SendPostRequest(ApiKeys.ApiFusionGemSynthesis, new Dictionary<string, string>
		// 		{
		// 			{"SynthesisList", JsonConvert.SerializeObject(synthesisList)}
		// 		});
                
		// 		var result = JsonConvert.DeserializeObject<FusionGemResult>(response);
		// 		foreach (var id in result.DelIds)
		// 		{
		// 			Global.PlayerFusionGems.Remove(id);
		// 		}

		// 		var upGrade = result.FusionGems.First();
		// 		Global.PlayerFusionGems[upGrade.Id] = new PlayerFusionGem(upGrade);
				
		// 		// 刷新数据
		// 		InitGemSlot();
				
		// 		FusionContinent.Toast.Show($"合成成功: Lv{upGrade.Level - 1} >> Lv{upGrade.Level}", 2f);
		// 	}
		// 	catch (System.Exception ex)
		// 	{
		// 		LogTool.LogError($"请求失败：{ex.Message}");
		// 		FusionContinent.Toast.Show($"操作失败：{ex.Message}", 5f);
		// 	}
		// }

	}
}
