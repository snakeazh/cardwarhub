using System;
using DG.Tweening;
using FusionContinent;
using UnityEngine;
using UnityEngine.UI;

public class LevelCard : MonoBehaviour
{
    public Image levelImage;
    public int levelId;

    private Button button;

    private LevelSliderController scrollSnap; // 引用ScrollSnap脚本
    private Tween scaleTween;
    public bool IsUpGrade;
    public bool IsTalent;
    public bool IsSkillRank;
    private void Awake()
    {
        button = GetComponentInChildren<Button>();
        if (button != null)
            button.onClick.AddListener(OnClick);
        ScaleDown();
    }

    public void SetLevel(int id, Sprite sprite, LevelSliderController snap = null, int valId = 0)
    {
        levelId = id;
        scrollSnap = snap;

        if (IsUpGrade)
        {
            var skillCfg = DataConfigManager.Tables.TbSpecialEffectsConfig.GetOrDefault(valId);
            var skillCard = Global.GetCardById(valId);
            if (skillCfg == null || skillCard == null)
            {
                throw new Exception($"技能ID不存在[{valId}]");
            }
            
            var baseCfg = DataConfigManager.Tables.TbCardBaseConfig.GetOrDefault(skillCfg.Id);
            var skillLvCfg = skillCfg.LevelPropertyList[skillCard.Lv - 1 < 0 ? 0 : skillCard.Lv - 1];
            levelImage.transform.parent.GetComponent<CardSlot>().SetData(new CardSlotData()
            {
                Icon = sprite,
                NameText = baseCfg.Name,
                QualityType = skillLvCfg.Quality,
                ManaNum = skillCfg.NeedGameMana,
                Desc = baseCfg.Desc,
            });
            return;
        }
        
        if (!IsTalent && !IsUpGrade&&!IsSkillRank)
        {
            Transform spinetransform = transform.Find("Spine");
            for (int i = 0; i < spinetransform.childCount; i++)
            {
                spinetransform.GetChild(i).gameObject.SetActive(false);
            }
            spinetransform.Find(DataConfigManager.Tables.TbChapterStageConfig.GetOrDefault(levelId + 1).Icon)
                .gameObject.SetActive(true);
        }

        if (sprite != null)
            levelImage.sprite = sprite;
    }

    private void OnClick()
    {
        if (scrollSnap != null)
        {
            scrollSnap.IsChooseClick = true;
            scrollSnap.SnapToCard(this);
            scrollSnap.onCardClicked?.Invoke(this); 
        }
    }

    public void ClickAction()
    {
        scrollSnap.onCardClicked?.Invoke(this); 
    }
    // 放大图片动画
    public void ScaleUp()
    {
        if(IsSkillRank) return;
        scaleTween?.Kill();
        if (IsUpGrade)
        {
            scaleTween = levelImage.transform.parent.GetComponent<RectTransform>().DOScale(1.1f, 0.1f).SetEase(Ease.OutQuad);;
            // scaleTween = levelImage.transform.parent.GetComponent<RectTransform>().DOScale(1.2f, 0.1f).SetEase(Ease.OutQuad);;
        }
        else
        {
            scaleTween = levelImage.rectTransform.DOScale(1.2f, 0.1f).SetEase(Ease.OutQuad);
        }

        if (IsTalent)
        {
            scaleTween = levelImage.transform.parent.parent.GetComponent<RectTransform>().DOScale(1f, 0.1f).SetEase(Ease.OutQuad);
        }
        if (IsSkillRank)
        {
            //scaleTween = levelImage.transform.parent.GetComponent<RectTransform>().DOScale(0.8f, 0.2f).SetEase(Ease.InQuad);
        }
    }

    // 缩小图片动画
    public void ScaleDown()
    {
        if(IsSkillRank) return;
        scaleTween?.Kill();
        if (IsUpGrade)
        {
            scaleTween = levelImage.transform.parent.GetComponent<RectTransform>().DOScale(0.8f, 0.1f).SetEase(Ease.OutQuad);;
        }
        else
        {
            scaleTween = levelImage.rectTransform.DOScale(0.7f, 0.1f).SetEase(Ease.OutQuad);
        }
        if (IsTalent)
        {
            scaleTween = levelImage.transform.parent.parent.GetComponent<RectTransform>().DOScale(0.8f, 0.1f).SetEase(Ease.OutQuad);
        }
        if (IsSkillRank)
        {
           // scaleTween = levelImage.transform.parent.GetComponent<RectTransform>().DOScale(0.6f, 0.2f).SetEase(Ease.InQuad);
        }
    }
    public void ScaleDown2()
    {
        scaleTween?.Kill();
        if (IsUpGrade)
        {
            scaleTween = levelImage.transform.parent.GetComponent<RectTransform>().DOScale(0.5f, 0.1f).SetEase(Ease.OutQuad);;
        }
        else
        {
            scaleTween = levelImage.rectTransform.DOScale(0.5f, 0.1f).SetEase(Ease.OutQuad);
        }
    }
}