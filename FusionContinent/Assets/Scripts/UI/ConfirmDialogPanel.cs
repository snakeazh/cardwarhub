using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	public class ConfirmDialogPanelData : UIPanelData
	{
	}
	public partial class ConfirmDialogPanel : UIPanel
	{
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as ConfirmDialogPanelData ?? new ConfirmDialogPanelData();
		}
		
		/// <summary>
		/// 设置数据确认框
		/// </summary>
		public void SetData(string title, string message, Action onConfirmCallback, Action onCancelCallback = null, bool noCancel = false)
		{
			// 判断是否弹出提示
			if (Global.ConfirmDialogPanelStatus == 2)
			{
				onConfirmCallback?.Invoke();
				Close();
				return; 
			}

			TitleText.text = title;
			Message.text = message;
			DetailBox.gameObject.SetActive(message.Length > 0);
			BasePlate.Find("Bg").gameObject.SetActive(message.Length > 0);
			BasePlate.Find("TitleBg").gameObject.SetActive(message.Length == 0);
			transform.Find("Canvas/Foot").transform.localPosition = message.Length == 0 ? new Vector3(0, 38f, 0) : new Vector3(0, -143f, 0);

			ConfirmBtn.onClick.RemoveAllListeners();
			ConfirmBtn.onClick.AddListener(() =>
			{
				onConfirmCallback?.Invoke();
				Close();
			});

			CancelBtn.interactable = !noCancel;
			CancelBtn.onClick.RemoveAllListeners();
			CancelBtn.onClick.AddListener(() =>
			{
				onCancelCallback?.Invoke();
				Close();
			});
			
			OptionBtn.onClick.RemoveAllListeners();
			OptionBtn.onClick.AddListener(() =>
			{
				var activeSelf = OptionBtn.transform.Find("Select").gameObject.activeSelf;
				OptionBtn.transform.Find("Select").gameObject.SetActive(!activeSelf);
				Global.ConfirmDialogPanelStatus = !activeSelf ? 2 : 1;
			});
		}
		
		/// <summary>
		/// 关闭确认框
		/// </summary>
		private void Close()
		{
			Destroy(gameObject);
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
