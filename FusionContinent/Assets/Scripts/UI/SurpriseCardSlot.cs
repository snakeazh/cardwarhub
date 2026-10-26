using System;
using System.Collections.Generic;
using Config;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using TMPro;
using Random = UnityEngine.Random;

namespace FusionContinent
{
	public class SurpriseCardSlotData : UIPanelData
	{
	}
	public partial class SurpriseCardSlot : UIPanel
	{
		private int _totalWeight;
		private Dictionary<int, DeBuffConfig> _totalPoolObjs = new Dictionary<int, DeBuffConfig>();
		
		private TbDeBuffConfig _deBuffConfig;
		private TbBattleSurpriseConfig _surpriseConfig;
		
		protected void Awake()
		{
			_deBuffConfig = DataConfigManager.Tables.TbDeBuffConfig;
			_surpriseConfig = DataConfigManager.Tables.TbBattleSurpriseConfig;
			
			foreach (var v in _deBuffConfig.DataList)
			{
				_totalWeight += v.Weight;
				_totalPoolObjs.Add(v.Id, v);
			}
		}
		
		
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as SurpriseCardSlotData ?? new SurpriseCardSlotData();
			// please add init code here
		}

		public void SetData(int id, int buffId, Action onClick, Action applyCall, int style = 0)
		{
			var surpriseCfg = _surpriseConfig.GetOrDefault(id);
			if (surpriseCfg == null)
			{
				transform.gameObject.SetActive(false);
				return;
			}
			
			transform.Find("Title").GetComponent<TMP_Text>().text = surpriseCfg.Name;
			transform.Find("Description").GetComponent<TMP_Text>().text = surpriseCfg.Tip;
			transform.Find("Icon").GetComponent<Image>().sprite = Global.SurpriseChooseCardIconSpriteAtlas.GetSprite(surpriseCfg.Icon);
			transform.Find("Bg2").gameObject.SetActive(style == 0);
			
			// 确认按钮
			var applyBtn = transform.Find("ApplyBtn").GetComponent<Button>();
			applyBtn.gameObject.SetActive(false);
			applyBtn.onClick.RemoveAllListeners();
			applyBtn.onClick.AddListener(() => applyCall());
			
			// 点击选择
			var isFront = true;
			var chooseBtn = transform.GetComponent<Button>();
			chooseBtn.onClick.RemoveAllListeners();
			chooseBtn.onClick.AddListener(() =>
			{
				if (!isFront || style == 1) return; // 背面不允许点击
                
				// 翻牌效果 - 先缩小 X 缩放到 0
				transform.DOScaleX(0, 0.5f / 2).SetEase(Ease.InQuad).OnComplete(() =>
				{
					// 切换显示的图片
					isFront = !isFront;
					transform.Find("Bg2").gameObject.SetActive(false);
					transform.Find("Description").GetComponent<TMP_Text>().text = surpriseCfg.Desc;

					// 再恢复缩放
					transform.DOScaleX(1, 0.5f / 2).SetEase(Ease.OutQuad).OnComplete(() =>
					{
						applyBtn.gameObject.SetActive(true);
					});
				});
				
				if (onClick != null) onClick();
			});

			// 样式1，直接显示描述
			if (style == 1) transform.Find("Description").GetComponent<TMP_Text>().text = surpriseCfg.Desc;
			
			// 显示DeBuff
			var buffCfg = _deBuffConfig.GetOrDefault(buffId);
			DeBuffCard.gameObject.SetActive(buffCfg != null);
			DeBuffCard.Find("Description").GetComponent<TMP_Text>().text = buffCfg?.Desc;
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
		}
	}
}
