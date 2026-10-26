using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	public class SkillItemData : UIPanelData
	{
	}
	public partial class SkillItem : UIPanel
	{
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as SkillItemData ?? new SkillItemData();
			// please add init code here
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
