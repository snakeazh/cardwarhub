using System;
using System.Collections.Generic;
using System.Linq;
using Config;
using DG.Tweening;
using Manager;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using TMPro;
using Random = UnityEngine.Random;

namespace FusionContinent
{
	public class SurpriseChooseCardPanelData : UIPanelData
	{
	}
	public partial class SurpriseChooseCardPanel : UIPanel
	{
		public static SurpriseChooseCardPanel Default;
		
		private int _totalWeight;
		private int _totalBuffWeight;
		private float _countdownTime; // 倒计时的初始值
		private Dictionary<int, DeBuffConfig> _totalBuffPoolObjs = new Dictionary<int, DeBuffConfig>();
		private Dictionary<int, BattleSurpriseConfig> _totalPoolObjs = new Dictionary<int, BattleSurpriseConfig>();
		
		private TbDeBuffConfig _deBuffConfig;
		private TbBattleSurpriseConfig _surpriseConfig;
		
		protected void Awake()
		{
			Default = this;
			_deBuffConfig = DataConfigManager.Tables.TbDeBuffConfig;
			_surpriseConfig = DataConfigManager.Tables.TbBattleSurpriseConfig;
		}
		
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as SurpriseChooseCardPanelData ?? new SurpriseChooseCardPanelData();
			// please add init code here
			ResetData(false);
		}

		public void ResetData(bool isClose = true)
		{
			// 构建生效卡池
			_totalWeight = 0;
			_totalPoolObjs.Clear();
			foreach (var v in _surpriseConfig.DataList)
			{
				_totalWeight += v.Weight;
				_totalPoolObjs.Add(v.Id, v);
			}
			
			// Buff构建生效卡池
			_totalBuffWeight = 0;
			_totalBuffPoolObjs.Clear();
			foreach (var v in _deBuffConfig.DataList)
			{
				_totalBuffWeight += v.Weight;
				_totalBuffPoolObjs.Add(v.Id, v);
			}
			
			if (isClose) OnClose();
		}

		public void SetData()
		{
			var chooseCardList = RandomChooseCards(ChooseList.childCount);
			if (chooseCardList == null || chooseCardList.Count == 0)
			{
				ChooseOver();
				return;
			}

			var uiObjects = new List<RectTransform>();
			var deBuffList = RandomDeBuffs(ChooseList.childCount);
			for (int i = 0; i < ChooseList.childCount; i++)
			{
				var chooseObj = ChooseList.GetChild(i);
				chooseObj.gameObject.SetActive(chooseCardList.Count > i);
				if (!chooseObj.gameObject.activeSelf) continue;

				var deBuffId = deBuffList.Count > i ? deBuffList[i] : 0;
				InitChooseCard(chooseObj, chooseCardList[i], deBuffId);
				uiObjects.Add(chooseObj.GetComponent<RectTransform>());
			}
			
			// 出现效果
			GetComponent<UIEntryController>().SetUIObjects(uiObjects, false);
			
			// 暂停游戏
			GamePlayManager.Default?.PauseGame();
			
			// 重置倒计时
			_countdownTime = 60f;
			
			GamePlayManager.Default.TriggerGuide(GuideType.KillLittleBoss); // 引导 - 奇遇
		}

		private void InitChooseCard(Transform chooseObj, int id, int deBuffId)
		{
			var surpriseCfg = _surpriseConfig.GetOrDefault(id);
			if (surpriseCfg == null)
			{
				chooseObj.gameObject.SetActive(false);
				return;
			}
			
			var buffCfg = _deBuffConfig.GetOrDefault(deBuffId);
			chooseObj.GetComponent<SurpriseCardSlot>().SetData(id, deBuffId, () =>
			{
				LogTool.Log($"点击选择卡牌: {surpriseCfg.Id}");
				StopCountdownTime();
                
				// 从池子中剃除（保证不重复出现）
				_totalPoolObjs.Remove(id);
				_totalWeight -= surpriseCfg.Weight;
				
				// 从Buff池子中剃除（保证不重复出现）
				if (buffCfg != null)
				{
					_totalBuffPoolObjs.Remove(deBuffId);
					_totalBuffWeight -= buffCfg.Weight;
				}

				// 除自己卡牌隐藏
				for (int i = 0; i < ChooseList.childCount; i++)
				{
					var card = ChooseList.GetChild(i);
					if (card != chooseObj) card.gameObject.SetActive(false);
				}

				// 属性生效
				SurpriseCardManager.Instance.AddSurpriseCard(id, deBuffId);

			}, ChooseOver);
		}

		/**
         * 获取随机三选一卡
         * (随机取出指定数量的卡)
         */
		private List<int> RandomChooseCards(int cardCount = 1)
		{
			var chooseCardList = new List<int>();
			if (_totalWeight <= 0 || _totalPoolObjs.Count == 0) return chooseCardList;

			// 临时池，避免修改原始数据
			var tempPool = new List<BattleSurpriseConfig>(_totalPoolObjs.Values);
			int totalWeight = _totalWeight;

			// 随机数量
			for (int i = 0; i < cardCount && tempPool.Count > 0; i++)
			{
				int randomValue = Random.Range(0, totalWeight); // int版Range是 [min, max)
				int accumulatedWeight = 0;

				for (int j = 0; j < tempPool.Count; j++)
				{
					accumulatedWeight += tempPool[j].Weight;
					if (randomValue < accumulatedWeight)
					{
						var chosenCard = tempPool[j];
						chooseCardList.Add(chosenCard.Id);

						// 从池子里移除，更新权重
						totalWeight -= chosenCard.Weight;
						tempPool.RemoveAt(j);
						break;
					}
				}
			}

			return chooseCardList;
		}
		
		private List<int> RandomDeBuffs(int randomNum = 1)
		{
			var buffList = new List<int>();
			if (_totalBuffWeight <= 0 || _totalBuffPoolObjs.Count == 0) return buffList;

			// 临时池，避免修改原始数据
			var tempPool = new List<DeBuffConfig>(_totalBuffPoolObjs.Values);
			int totalWeight = _totalBuffWeight;

			// 随机数量
			for (int i = 0; i < randomNum && tempPool.Count > 0; i++)
			{
				int randomValue = Random.Range(0, totalWeight); // int版Range是 [min, max)
				int accumulatedWeight = 0;

				for (int j = 0; j < tempPool.Count; j++)
				{
					accumulatedWeight += tempPool[j].Weight;
					if (randomValue < accumulatedWeight)
					{
						var chosenCard = tempPool[j];
						buffList.Add(chosenCard.Id);

						// 从池子里移除，更新权重
						totalWeight -= chosenCard.Weight;
						tempPool.RemoveAt(j);
						break;
					}
				}
			}

			return buffList;
		}

		protected override void OnOpen(IUIData uiData = null)
		{
		}
		
		protected override void OnShow()
		{
		}
		
		protected override void OnHide()
		{
		}
		
		protected override void OnClose()
		{
			gameObject.SetActive(false);
		}
		
		private void Update()
		{
			if (_countdownTime > 0)
			{
				_countdownTime -= Time.deltaTime;
				CountdownText.text = $"{Mathf.CeilToInt(_countdownTime)}"; // 将时间取整显示
				CountdownTimeClose();
			}
		}

		private void StopCountdownTime()
		{
			_countdownTime = 0f;
			CountdownText.text = $"{_countdownTime}";
		}

		private void CountdownTimeClose()
		{
			if (_countdownTime > 0) return;

			for (int i = 0; i < ChooseList.childCount; i++)
			{
				var rdx = Random.Range(0, ChooseList.childCount);
				var chooseObj = ChooseList.GetChild(rdx);
				if (chooseObj != null && chooseObj.gameObject.activeSelf)
				{
					chooseObj.GetComponent<Button>().onClick.Invoke();
					break;
				}
			}
		}
		
		/**
		 * 选择结束
		 */
		private void ChooseOver()
		{
			GamePlayManager.Default?.SurpriseChooseCardOver();
			OnClose();
		}
		
		
	}
}
