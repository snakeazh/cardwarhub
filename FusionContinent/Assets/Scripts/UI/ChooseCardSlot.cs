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
	public class ChooseCardSlotData : UIPanelData
	{
	}
	public partial class ChooseCardSlot : UIPanel
	{
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as ChooseCardSlotData ?? new ChooseCardSlotData();
			// please add init code here
			QuitBtn.onClick.AddListener(() =>
			{
				RunAnimation(this.GetAnimationCloseNode, () =>
				{
					UIKit.ClosePanel(this);
				});
			});
		}
		
		public void SetData(int schoolCardId, bool isNextStar = true, Vector3 position = default(Vector3))
		{
			var schoolCardCfg = DataConfigManager.Tables.TbSchoolCardConfig.GetOrDefault(schoolCardId);
			if (schoolCardCfg == null)
			{
				transform.gameObject.SetActive(false);
				return;
			}
			transform.Find("TweenTarget").transform.position = position;
			transform.Find("TweenTarget").localPosition -= new Vector3(0,425, 0);
			Title.GetComponent<TMP_Text>().text = schoolCardCfg.Name;
			Icon.GetComponent<Image>().sprite = Global.SchoolChooseCardIconSpriteAtlas.GetSpriteExt(schoolCardCfg.Icon);
            
			Bg.GetComponent<Image>().sprite = Global.SchoolChooseCardIconSpriteAtlas.GetSpriteExt($"Check{schoolCardCfg.Quality}");
			
			// 流派卡
            var schoolCard = SchoolManager.Instance.GetSchoolCard(schoolCardCfg.Id);
            var categorySkill = schoolCardCfg.Category == SchoolCategoryType.Skill;// 技能类别

            // 描述
            var desc = schoolCardCfg.Desc;
            var starLv = schoolCard.CardStar > 0 ? schoolCard.CardStar : 0;
            if (!isNextStar)
            {
	            starLv = schoolCard.CardStar > schoolCardCfg.StarList.Count ? schoolCardCfg.StarList.Count - 1 : schoolCard.CardStar - 1;
            }

            var starCfg = schoolCardCfg.StarList[starLv];
            for (int i = 1; i <= starCfg.Attributes.Count; i++)
            {
                var attr = starCfg.Attributes[i - 1];
                var val = attr.ValueType == ValueType.Percentage ? attr.Value * 100 : attr.Value;
                if (attr.Type is AttributeType.SoldierCriticalRate or AttributeType.SoldierCriticalDamage) {
                    val = attr.Value * 100;
                }
                desc = desc.Replace("{Attribute" + i + "}", $"{val}");
            }

            if (starCfg.Param1 > 0) desc = desc.Replace("{Param1}", $"{starCfg.Param1}");
            if (starCfg.Param2 > 0) desc = desc.Replace("{Param2}", $"{starCfg.Param2}");
            Description.GetComponent<TMP_Text>().text = desc;

            // 星级
            var cardStar = categorySkill ? schoolCardCfg.StarList.First().AddStar : schoolCard.CardStar;
            SetCardStar(StarList, cardStar);

            // 所属流派
            var belongSchool = BelongSchool;
            belongSchool.gameObject.SetActive(!categorySkill);
            for (int j = 0; j < belongSchool.childCount; j++)
            {
                var belongObj = belongSchool.GetChild(j);
                belongObj.gameObject.SetActive(schoolCardCfg.Schools.Count > j);
                if (belongObj.gameObject.activeSelf)
                {
                    var belongSchoolCfg = DataConfigManager.Tables.TbSchoolConfig.Get(schoolCardCfg.Schools[j]);
                    belongObj.GetComponent<Image>().sprite =
                        Global.SchoolChooseCardIconSpriteAtlas.GetSpriteExt(belongSchoolCfg.Icon2);
                }
            }
		}
		
		/**
         * 卡牌星数
         */
		private void SetCardStar(Transform starList, int starNum)
		{
			for (int s = 0; s < starList.childCount; s++)
			{
				var starObj = starList.GetChild(s);
				starObj.gameObject.SetActive(starNum >= s + 1);
				starObj.Find("Shine").GetComponent<Image>().color = new Color(255f, 255f, 255f, 1f);
			}
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
