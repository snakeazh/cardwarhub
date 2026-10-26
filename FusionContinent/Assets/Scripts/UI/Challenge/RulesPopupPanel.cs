using QFramework;

namespace FusionContinent
{
	public class RulesPopupPanelData : UIPanelData
	{
	}
	public partial class RulesPopupPanel : UIPanel
	{
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as RulesPopupPanelData ?? new RulesPopupPanelData();
			// please add init code here
			ApplyBtn.onClick.AddListener(OnClose);
		}

		public void SetData(string titleText, string descText)
		{
			TitleText.text = titleText;
			ContentText.text = descText;
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
	}
}
