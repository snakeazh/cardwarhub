using Config;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	public class CardGroupCardData : UIPanelData
	{
		public Sprite Icon;
		public Sprite Bg;//卡片品质背景
		public int CardId = 0;
		public CardState State;
		public bool IsUnlock = true;
	}
	public partial class CardGroupCard : UIPanel
	{
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as CardGroupCardData ?? new CardGroupCardData();
			// please add init code here
		}

		public void SetData(CardGroupCardData data)
		{
			if (data.Icon != null)
			{
				SkillCardIcon.gameObject.SetActive(true);
				SkillCardIcon.sprite = data.Icon;
			}
			else
			{
				SkillCardIcon.gameObject.SetActive(false);
			}
			Locked.gameObject.SetActive(data.IsUnlock);
			Bg.sprite = data.Bg;
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
