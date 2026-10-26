using System.Linq;
using Config;
using FusionContinent.Animation;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using TMPro;
using UnityEngine.Events;

namespace FusionContinent
{
	public class GuildDetailData : UIPanelData
	{
		public GuideFlowConfig FlowConfig;
	}
	public partial class GuildDetail : UIPanel
	{
		private GuideFlowConfig _currentCfg;
		private Transform searchRoot;
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as GuildDetailData ?? new GuildDetailData();
			// please add init code here
			_currentCfg = mData.FlowConfig;
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
			searchRoot = this.transform.Find("TweenTarget");
			searchRoot.Find("NameText").GetComponent<TMP_Text>().text = _currentCfg.Name;
			this.searchRoot.Find("Desc/Text").GetComponent<TMP_Text>().text = _currentCfg.Desc;
						
			// 奖励
			var rewardList = this.searchRoot.Find("Rewards/Viewport/Content");
			var rewParent = rewardList.Find("RewardEntity");
			for (int r = 0; r < _currentCfg.Rewards.Count; r++)
			{
				var rewChild = r < rewardList.childCount 
					? rewardList.GetChild(r) 
					: Instantiate(rewParent, rewardList);
				rewChild.gameObject.SetActive(true);
				rewChild.GetComponent<RewardEntity>().SetData(_currentCfg.Rewards[r]);
			}
			
			this.transform.Find("Quit").GetComponent<Button>().onClick.AddListener(GuildDetailClose);
			this.searchRoot.Find("SkipBtn").GetComponent<Button>().onClick.AddListener(GuildSkipPath);
		}

		private void GuildSkipPath()
		{
			if (_currentCfg == null) return;
			var skipPath = _currentCfg.StepList.First().SkipPath.First();
			GameMainPanel.Default.GotoSkipPath(skipPath.Type, skipPath.Param);
			UIKit.ClosePanel<GuildDetail>();

		}
		

		private void GuildDetailClose()
		{
			RunAnimation(GetAnimationCloseNode, () =>
			{
				UIKit.ClosePanel<GuildDetail>();
			});
			
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
