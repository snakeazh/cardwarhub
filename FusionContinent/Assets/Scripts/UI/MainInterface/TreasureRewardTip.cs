using FusionContinent.Animation;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using UnityEngine.Events;

namespace FusionContinent
{
	public class TreasureRewardTipData : UIPanelData
	{
	}
	public partial class TreasureRewardTip : UIPanel
	{
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as TreasureRewardTipData ?? new TreasureRewardTipData();
			// please add init code here
		}

		public void SetData(TreasureChestData _data, Vector3 position)
		{
			if (_data.Rewards == null || _data.Rewards.Count == 0) return;
			this.transform.Find("TweenTarget").position = position;
			transform.Find("TweenTarget").localPosition -= new Vector3(0,270, 0);
			// - new Vector3(0,220,0);
			TitleImg.sprite = Global.TreasureChestIconSpriteAtlas.GetSprite(_data.Icon);
			var cardScroll = RewardScroll.transform.Find("Viewport/Content");
			for (int i = 0; i < cardScroll.childCount; i++)
				cardScroll.GetChild(i).gameObject.SetActive(false);
			
			var idx = 0;
			foreach (var rew in _data.Rewards)
			{
				GameObject rewObj = idx >= cardScroll.childCount 
					? Instantiate(cardScroll.Find("RewardEntity").gameObject, cardScroll, true) 
					: cardScroll.GetChild(idx).gameObject;
				
				rewObj.SetActive(true);
				rewObj.GetComponent<RewardEntity>().SetData(rew.Id, rew.Num);
				idx++;
			}
			
			Button quitBtn = Quit.GetComponent<Button>();
			quitBtn.onClick.RemoveAllListeners();
			quitBtn.onClick.AddListener(()=>
			{
				RunAnimation(this.GetAnimationCloseNode, () =>
				{
					UIKit.ClosePanel(this);
				});
			});
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
