using FusionContinent.Animation;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using UnityEngine.Events;

namespace FusionContinent
{
	public class CancelAccountPanelData : UIPanelData
	{
	}
	public partial class CancelAccountPanel : UIPanel
	{
		public Button exitBtn;
		
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as CancelAccountPanelData ?? new CancelAccountPanelData();
			// please add init code here
			// exitBtn.onClick.RemoveAllListeners();
			// exitBtn.onClick.AddListener(() =>
			// {
			// 	RunAnimation(this.GetAnimationCloseNode, () =>
			// 	{
			// 		UIKit.ClosePanel(this);
			// 	});
			// });
			//
			//
			// CancelBtn.onClick.RemoveAllListeners();
			// CancelBtn.onClick.AddListener(() =>
			// {
			// 	ClearFromData();
			// 	UIKit.ClosePanel<CancelAccountPanel>();
			// });
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
		
		private void ClearFromData()
		{
			// PlayerPrefs.DeleteKey("Account");
			// PlayerPrefs.DeleteKey("Password");
			// ShowMsg("已退出登录");
		}
		
		private void ShowMsg(string msg)
		{
			// FusionContinent.Toast.Show(msg);
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
