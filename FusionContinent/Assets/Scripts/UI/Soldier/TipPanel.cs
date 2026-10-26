using UnityEngine;
using UnityEngine.UI;
using QFramework;
using TMPro;

namespace FusionContinent
{
	public class TipPanelData : UIPanelData
	{
	}
	public partial class TipPanel : UIPanel
	{
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as TipPanelData ?? new TipPanelData();
			// please add init code here
		}
		
		//无标题
		public void SetData(string detail)
		{
			Title.gameObject.SetActive(false);
			Detail.text = detail;
		}

		//有标题
		public void SetData(string title, string detail)
		{
			Title.gameObject.SetActive(true);
			Title.transform.Find("TitleName").GetComponent<TMP_Text>().text = title;
			Detail.text = detail;
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
