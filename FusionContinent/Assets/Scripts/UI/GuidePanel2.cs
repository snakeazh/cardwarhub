using Config;
using DG.Tweening;
using FusionContinent.Guide;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	public class GuidePanel2Data : UIPanelData
	{
		public GuidesStepListStruct Step;
		public IGuideMethod Current;
		public int CardId;
	}
	public partial class GuidePanel2 : UIPanel
	{
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as GuidePanel2Data ?? new GuidePanel2Data();
			// please add init code here
		}
		
		protected override void OnOpen(IUIData uiData = null)
		{
			mData = uiData as GuidePanel2Data ?? new GuidePanel2Data();
			this.GuideTipsMessage1.gameObject.SetActive(false);
			this.GuideTipsMessage2.gameObject.SetActive(false);

			// 引导初始化已由 GuidePanel2New（MVVM）接管，旧组件不再驱动 GuideMethod
		}

		public void PlayAnimation()
		{
			this.Dialoge.DOKill(true);
			this.Dialoge.localScale = Vector3.zero;
			this.Dialoge.DOScale(Vector3.one, 0.3f).SetUpdate(true);
		}
		
		protected override void OnShow()
		{
			PlayAnimation();
		}
		
		protected override void OnHide()
		{
		}
		
		protected override void OnClose()
		{
		}
	}
}
