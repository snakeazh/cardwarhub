using FusionContinent.Base;
using FusionContinent.Helper;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	public class TeamSkillCardSlotData : UIPanelData
	{
	}
	public partial class TeamSkillCardSlot : UIPanel
	{
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as TeamSkillCardSlotData ?? new TeamSkillCardSlotData();
			// please add init code here
		}

		public void SetData(CardSlotData data)
		{
			transform.Find("Mask").gameObject.SetActive(data.CardId != 0);
			if (data.CardId == 0)
			{
				return;
			}

			SpriteHelper.TryLoadSpriteByAtlas(Global.CardStyleIconSpriteAtlas,$"{data.QualityType}2", Bg);
			if (!string.IsNullOrEmpty(data.IconName))
			{
				SpriteHelper.TryLoadPaintAsync(data.IconName, SkillCardIcon, groupName: "GeneralSkill");
			}
			else
			{
				AsyncTextureManager.Instance.InvalidateImage(SkillCardIcon);
				SkillCardIcon.sprite = data.Icon;
			}
			SkillCardIcon.transform.localScale = data.IconScale;
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
