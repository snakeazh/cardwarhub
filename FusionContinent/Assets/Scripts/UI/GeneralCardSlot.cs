using System;
using System.Collections.Generic;
using Config;
using FusionContinent.Base;
using FusionContinent.Helper;
using Helper;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using TMPro;

namespace FusionContinent
{
	public class GeneralCardSlotData : UIPanelData
	{
		public Sprite Icon;
		/// <summary>散图资源名（GeneralSkill 组），非空时由槽位异步加载，优先于 Icon</summary>
		public string IconName;
		public int CardId = 0;
		public string NameText = "";
		public QualityType QualityType;
		public string LevelText = "";
		public int StarNum = 0;
		public string Desc;
		public CardState State;
		public bool IsUnlock = true;
		public Vector3 IconScale =  Vector3.one;
		// public List<int> ProgressBar = new List<int>();
		public bool CheckRed = false;
		public bool ShowEffect = false;
		public bool IsSelected = false;
		public bool IsGeared = false;
		public int Num = -1;
		public int SelectedCount = 0;
	}
	public partial class GeneralCardSlot : UIPanel
	{
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as GeneralCardSlotData ?? new GeneralCardSlotData();
		}
		
		public void SetSelected(bool selected,int selectedCount)
		{
			if (mData == null) return;
			mData.IsSelected = selected;
			mData.SelectedCount = selectedCount;
			UpdateSelectedVisual();
		}
		
		private void UpdateSelectedVisual()
		{
			if (mData.IsSelected)
			{
				this.SelectCount.text = "已选择X" + mData.SelectedCount;
			}
			
			IsSelected.gameObject.SetActive(mData.IsSelected);
			// if (IsChoosed != null)
			// {
			// 	IsChoosed.gameObject.SetActive(mData.IsSelected);
			// }
		}
		
		public void SetData(GeneralCardSlotData data)
		{
			var tweenTarget = transform.Find("TweenTarget");
            
            // SpriteHelper.TryLoadSpriteByAtlas(Global.CardFrameAtlas,$"{data.QualityType}GeneralCard",FrameStyleImg.GetComponent<Image>());
            FrameStyleImg.GetComponent<Image>().sprite = Global.CarFrameAtlas.GetSprite($"{data.QualityType}GeneralCard");
            
            if (!string.IsNullOrEmpty(data.IconName))
            {
                SpriteHelper.TryLoadPaintAsync(data.IconName, CardIcon.GetComponent<Image>(), groupName: "GeneralSkill");
            }
            else
            {
                AsyncTextureManager.Instance.InvalidateImage(CardIcon.GetComponent<Image>());
                CardIcon.GetComponent<Image>().sprite = data.Icon;
            }
            // CardIcon.GetComponent<Image>().SetNativeSize();
            // CardIcon.transform.localScale = data.IconScale;
            
            CardNameText.GetComponent<TMP_Text>().text = $"{data.NameText}";
            
            
            // 锁定
            var lockColor = new Color32(255, 255, 255, 255);
            if (!data.IsUnlock)
            {
                lockColor = new Color32(155, 155, 155, 255);
            }
            
            CardQualityBg.GetComponent<Image>().color = lockColor;
            CardIcon.GetComponent<Image>().color = lockColor;
            FrameStyleImg.GetComponent<Image>().color = lockColor;
            Lock.gameObject.SetActive(!data.IsUnlock);
            if (data.CheckRed)
            {
                // tweenTarget.Find("CardName").GetComponent<RectTransform>().SetLocalRedType(GameDataUtils.GetCardSlotUpgrade(data.CardId));            
            }
            this.NumText.gameObject.SetActive(data.Num>=0);
            // GetCardSlotUpgrade
            if (data.Num>=0)
            {
	            this.NumText.text = $"X{data.Num}";
	            // this.ProgressNumT
            }
            // 进度条
            tweenTarget.Find("ProgressBar")?.gameObject.SetActive(false);
            // if (tweenTarget.Find("ProgressBar").gameObject.activeSelf)
            // {
            //     // 进度条
            //     float progress = 1;
            //     string progressText = "MAX";
            //     if (data.ProgressBar[0] != -1 && data.ProgressBar.Count >= 2)
            //     {
            //         progressText = $"{data.ProgressBar[0]}/{data.ProgressBar[1]}";
            //         progress = data.ProgressBar[0] > 0
            //             ? Mathf.Clamp01((float)data.ProgressBar[0] / (float)data.ProgressBar[1])
            //             : 0;
            //     }
            //     
            //     tweenTarget.Find("ProgressBar").GetComponent<ProgressBar>().SetProgress(progress, progressText);
            // }
            //

            // 星数
            // var starList = tweenTarget.Find("StarList");
            // var starNum = data.StarNum > 0 ? data.StarNum : (int)data.QualityType + 1;
            // for (int i = 0; i < starNum; i++)
            // {
            //     var starObj = starList.GetChild(i);
            //     starObj.gameObject.SetActive(false);
            // }
            //
            // if (starNum - 2 >= 0)
            // {
            //     starList.GetChild(starNum - 2).gameObject.SetActive(true);
            // }
            // else
            // {
            //     starList.GetChild(0).gameObject.SetActive(true);
            // }

            var bgIcon = Global.CardStyleIconSpriteAtlas.GetSprite(data.QualityType.ToString());
            if (bgIcon == null)
            {
                bgIcon = Global.CardStyleIconSpriteAtlas.GetSprite(QualityType.Ordinary.ToString());
            }

            CardQualityBg.GetComponent<Image>().sprite =
                bgIcon;
            if (data.QualityType != QualityType.Default)
            {
                var effectParent = tweenTarget.Find("Effect");
                var effectName = $"SkillCard{data.QualityType.ToString()}01";
                for (int i = 0; i < effectParent.childCount; i++)
                    effectParent.GetChild(i).gameObject.SetActive(false);
                if (effectParent.Find(effectName) == null)
                {
                    var pool = CommonItemPoolManager.Instance.GetGameObjectPool(effectName);
                    if (pool != null)
                    {
                        GameObject skillEffect = pool.Get();
                        skillEffect.name = effectName;
                        skillEffect.gameObject.SetActive(true);
                        skillEffect.transform.SetParent(effectParent);
                        skillEffect.transform.localScale = Vector3.one * 100;
                        skillEffect.transform.localPosition = Vector3.zero;
                        skillEffect.GetOrAddComponent<ParticleClipParent>();
                    }
                }
                else
                {
                    effectParent.Find(effectName).gameObject.SetActive(true);
                }
            }
            
            IsSelected.gameObject.SetActive(data.IsSelected);

            // 已装备标记：该卡牌在任一奥术卡槽位被装备过时显示
            IsGear.gameObject.SetActive(data.IsGeared);
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
