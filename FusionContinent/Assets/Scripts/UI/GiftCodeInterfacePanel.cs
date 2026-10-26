using System;
using System.Collections.Generic;
using Cardwar;
using FusionContinent.Animation;
using FusionContinent.Model;
using FusionContinent.Utils;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using UnityEngine.Events;

namespace FusionContinent
{
	public class GiftCodeInterfacePanelData : UIPanelData
	{
	}
	public partial class GiftCodeInterfacePanel : UIPanel
	{
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as GiftCodeInterfacePanelData ?? new GiftCodeInterfacePanelData();
			// please add init code here
			
			//退出按钮
			Shadow.onClick.AddListener(() =>
			{
				RunAnimation(this.GetAnimationCloseNode, () =>
				{
					UIKit.ClosePanel(this);
				});
			});
			
			// ReceiveBtn.onClick.AddListener(CodeExchangeServer);
		}
		
		protected override void RunAnimation(GameObject target, UnityAction complete)
		{
			UIAnimationUtils.PlayAnimations(target,complete);
		}

		protected override GameObject GetAnimationCloseNode
		{
			get => this.CloseAnimation.gameObject;
		}

		protected override GameObject GetAnimationOpenNode
		{
			get => this.OpenAnimation.gameObject;
		}
		
		protected override void OnOpen(IUIData uiData = null)
		{
		}
		
		// // 设置模块状态（服务器）
		// private async void CodeExchangeServer()
		// {
		// 	try
		// 	{
		// 		string codeField = CodeField.text.Trim();
		// 		if (codeField == "")
		// 		{
		// 			throw new Exception("礼包码不能为空");
		// 		}
		//
		// 		var codeCfg = DataConfigManager.Tables.TbCodeExchangeGiftsConfig.GetOrDefault(codeField);
		// 		if (codeCfg == null)
		// 		{
		// 			throw new Exception("礼包码不存在");
		// 		}
		//
		// 		if (Global.PlayerExchangeGift.CodeExchangesMap.TryGetValue(codeField, out var status) || status > 0)
		// 		{
		// 			throw new Exception("礼包码已兑换");
		// 		}
		//
		// 		var request = new CodeExchangeGiftsRequest { Code = codeField };
		// 		var proto = await WebDataFun.SendProtoPostRequest<CodeExchangeGiftsResponse>(ApiKeys.ApiCodeExchangeGifts, request);
		// 		if (proto != null && proto.ExchangeGift != null)
		// 		{
		// 			Global.PlayerExchangeGift = ProtoMappers.MapPlayerExchangeGift(proto.ExchangeGift);
		//
		// 			// 同步奖励
		// 			Items.Update(proto.Items);
		//
		// 			var rewardsDict = new Dictionary<string, int>();
		// 			if (proto.Rewards != null)
		// 			{
		// 				foreach (var kv in proto.Rewards)
		// 				{
		// 					rewardsDict[kv.Key.ToString()] = (int)kv.Value;
		// 				}
		// 			}
		//
		// 			// 奖励
		// 			if (rewardsDict.Count > 0)
		// 			{
		// 				RewardTips rewardTips = UIKit.OpenPanel<RewardTips>(UILevel.PopUI);
		// 				rewardTips.SetData(rewardsDict);
		// 			}
		// 		}
		// 		
		// 	}
		// 	catch (System.Exception ex)
		// 	{
		// 		var errorMsg = $"{ex.Message}";
		// 		if (GameMainPanel.Default != null) 
		// 			FusionContinent.Toast.Show(errorMsg);
		// 		LogTool.LogError(errorMsg);
		// 	}
		// }
		//
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
