using System.Text;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	public class LogShowData : UIPanelData
	{
	}
	public partial class LogShow : UIPanel
	{
		private StringBuilder logBuilder = new StringBuilder(); // 用于存储日志内容
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as LogShowData ?? new LogShowData();
			
			// please add init code here
		}
		
		protected override void OnOpen(IUIData uiData = null)
		{
			Application.logMessageReceived += HandleLog;
		}
		
		protected override void OnShow()
		{
		}
		
		protected override void OnHide()
		{
		}
		private void HandleLog(string logString, string stackTrace, LogType type)
		{
			// 添加新日志并换行
			logBuilder.AppendLine(logString);

			// 如果是错误或警告日志，可以加上标记颜色
			if (type == LogType.Error || type == LogType.Exception)
			{
				logBuilder.AppendLine($"<color=red>{stackTrace}</color>");
			}
			else if (type == LogType.Warning)
			{
				logBuilder.AppendLine($"<color=yellow>{stackTrace}</color>");
			}

			// 更新显示的日志
			LogShowText.text = logBuilder.ToString();
		}

		public void ClearLog()
		{
			// 清空日志
			logBuilder.Clear();
			LogShowText.text = string.Empty;
		}
		
		protected override void OnClose()
		{
			// Application.logMessageReceived -= HandleLog;
		}
		
		void OnDisable()
		{
			Application.logMessageReceived -= HandleLog;
		}
	}
}
