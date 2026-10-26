using System;
using System.Collections.Generic;
using Config;
using DG.Tweening;
using FusionContinent.Helper;
using FusionContinent.Model;
using FusionContinent.UI.Components;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using TMPro;
using UnityEngine.U2D;

namespace FusionContinent
{
	public class TreasureChestData : UIPanelData
	{
		public string Icon = "";
		public Action OnClick = null;
		public bool IsOnClick = true;
		public bool IsLongOnClick = false;
		public TreasureChestStyleType StyleType = TreasureChestStyleType.Reward;
		public int ChestCondition = (int)ConditionStatus.Normal;
		public bool CanReceive;
		public bool StartAnim;// 开始动画
		public List<RewardAndNum> Rewards;
		public string ProgressText = "";
	}
	public partial class TreasureChest : UIPanel
	{
		public static TreasureChest Default;

		private TreasureChestData _data = new TreasureChestData();
		private TbSchoolChooseCardConfig _schoolChooseCardConfig;

		private void Awake()
		{
			Default = this;
			_schoolChooseCardConfig = DataConfigManager.Tables.TbSchoolChooseCardConfig;
		}

		private void Start()
		{
			TreasureChestIcon.GetComponent<UIPointerClickAndLongPress>().onClick.AddListener(() =>
			{
				switch (_data.StyleType)
				{
					case TreasureChestStyleType.Reward:
						if (!_data.CanReceive)
						{
							// ShowRewardTip();
							UIKit.OpenPanel<TreasureRewardTip>(UILevel.PopUI).SetData(_data,this.transform.position);
							return;
						}
						
						if (_data.OnClick != null)
						{
							OpenEffect(_data.OnClick);
							// _data.OnClick();
						}
						break;
					
					default:
						ShowTreasureChestTip();
						break;
				}
			});
		}
		public enum AnimationState
		{
			Idle = 1,
			CanReceive = 2,
			Received = 3,
		}
		public void SetData(TreasureChestData data)
		{
			_data = data;
			if (data.Icon != "") TreasureChestIcon.sprite = Global.TreasureChestIconSpriteAtlas.GetSprite(data.Icon);
			
			// 进度
			// Progress.gameObject.SetActive(data.ProgressText != "" || data.StyleType == TreasureChestStyleType.ChooseCard);
			Progress.gameObject.SetActive(false);
			if (data.ProgressText != "")
			{
				ProgressText.SetData(LocalizationModule.UITexts, data.ProgressText);
			}
			else
			{
				ProgressText.GetComponent<TMP_Text>().text = data.ProgressText;
			}

			// 点击 or 长按
			TreasureChestIcon.GetComponent<UIPointerClickAndLongPress>().enabled = data.IsOnClick;
			
			// 初始动画
			// var iconAnim = IconAnim.GetComponent<Animator>();
			// iconAnim.enabled = false;
			IconAnim.Animator.enabled = true;
			
			if (data.StartAnim)
			{
				IconAnim.Animator.Play("ani_zjm_zc_baoxiang01");
				// IconAnim.PlayAnimation("ani_zjm_zc_baoxiang01",true, () =>
				// {
				// 	// iconAnim.enabled = false;
				// 	// if (data.CanReceive)
				// 	// {
				// 	// 	IconAnim.PlayAnimation("ani_baoxiang_open_03",false);
				// 	// 	// iconAnim.enabled = true;
				// 	// 	// iconAnim.Play("ani_baoxiang_open_03", 0, 0f);
				// 	// }
				// });
				// iconAnim.enabled = true;
				// iconAnim.Play("ani_zjm_zc_baoxiang01", 0, 0f);
				if (data.CanReceive)
				{
					IconAnim.Animator.SetInteger("State",(int)AnimationState.CanReceive);
				}
				else
				{
					IconAnim.Animator.SetInteger("State",(int)AnimationState.Idle);
				}
			}
			else
			{
				if (data.CanReceive)
				{
					IconAnim.Animator.Play("canreceive");
				}
				else
				{
					IconAnim.Animator.Play("idle");
				}
			}

		}
		//
		// private Tween delayTween;
		public void SetProgress(float progress)
		{
			ProgressFull.fillAmount = progress;
			ProgressText.GetComponent<TMP_Text>().text = $"{Mathf.CeilToInt(progress * 100)}%";
		}

		private void ShowTreasureChestTip()
		{
			TreasureChestIcon.GetComponent<Animator>().enabled = false;
			TreasureChestIcon.transform.parent.GetComponent<Animator>().enabled = false;
			TreasureChestTip.gameObject.SetActive(true);
			TreasureChestTipIcon.sprite = TreasureChestIcon.sprite;
			var chooseCfg = _schoolChooseCardConfig.GetOrDefault(Global.CityLevel.Value+1);
			NextTreasureChestTipIcon.sprite = Global.TreasureChestIconSpriteAtlas.GetSprite(chooseCfg.ChestIcon);
			Tip.text = "距离升级宝箱还需" +(int)((1-ProgressFull.fillAmount)*100)+ "%";
			DOTween.To(v => { }, 0, 0, 0.5f).onComplete += () =>
			{
				TreasureChestIcon.GetComponent<Animator>().enabled = true;
				TreasureChestIcon.transform.parent.GetComponent<Animator>().enabled = true;
			};
		}

		private void Update()
		{
			if (TreasureChestTip.gameObject.activeSelf)
			{
				if (Input.GetMouseButtonDown(0))
				{
					TreasureChestIcon.GetComponent<Animator>().enabled = true;
					TreasureChestIcon.transform.parent.GetComponent<Animator>().enabled = true;
					TreasureChestTip.gameObject.SetActive(false);
				}
			}
		}

		protected override void OnInit(IUIData uiData = null)
		{
		}

		public void UpgradeEffect()
		{
			var chooseCfg = _schoolChooseCardConfig.GetOrDefault(Global.CityLevel.Value);
			if (chooseCfg == null) return;

			IconAnim.GetComponent<Animator>().enabled = true;
			IconAnim.GetComponent<Animator>().Play("ani_baoxiang_open_01", 0, 0f);
			transform.Find("ChestEffectUpgrade01").gameObject.SetActive(false);
			DOTween.To(v => { }, 0, 0, 0.7f).onComplete += () =>
			{
				transform.Find("ChestEffectUpgrade01").gameObject.SetActive(true);
				TreasureChestIcon.sprite = Global.TreasureChestIconSpriteAtlas.GetSprite(chooseCfg.ChestIcon);
			};
		}

		public void OpenEffect(Action callback)
		{
			var chooseCfg = _schoolChooseCardConfig.GetOrDefault(Global.CityLevel.Value);
			if (chooseCfg == null) return;
			
			IconAnim.GetComponent<Animator>().enabled = true;
			IconAnim.GetComponent<Animator>().Play("ani_baoxiang_open_01", 0, 0f);
			transform.Find("ChestEffectOpen02").gameObject.SetActive(false);
			DOTween.To(v => { }, 0, 0, 0.7f).onComplete += () =>
			{
				TreasureChestIcon.sprite = Global.TreasureChestIconSpriteAtlas.GetSprite($"{chooseCfg.ChestIcon}Open");
				transform.Find("ChestEffectOpen02").gameObject.SetActive(true);
				DOTween.To(v => { }, 0, 0, 0.9f).onComplete += () =>
				{
					transform.Find("ChestEffectOpen02").gameObject.SetActive(false);

				};
				// ChestEffectOpen02
				
				callback();
			};
		}

		public void CloseEffect()
		{
			var chooseCfg = _schoolChooseCardConfig.GetOrDefault(Global.CityLevel.Value);
			if (chooseCfg == null) return;
			
			TreasureChestIcon.sprite = Global.TreasureChestIconSpriteAtlas.GetSprite(chooseCfg.ChestIcon);
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

		private void OnEnable()
		{
			SetData(_data);
		}

		private void OnDisable()
		{
			// IconAnim.GetComponent<Animator>().enabled = false;
		}

		protected override void OnClose()
		{
		}
	}
	
	
	/**
	 * 宝箱样式类型
	 */
	public enum TreasureChestStyleType
	{
		/// <summary>
		/// 三选一
		/// </summary>
		ChooseCard = 0,

		/// <summary>
		/// 奖励
		/// </summary>
		Reward = 1,
	}
}
