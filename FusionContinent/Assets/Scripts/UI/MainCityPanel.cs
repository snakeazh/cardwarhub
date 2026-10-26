using System;
using Config;
using DG.Tweening;
using FusionContinent.Model;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
    public class MainCityPanelData : UIPanelData
    {
    }

    public partial class MainCityPanel : UIPanel
    {
        private int ChangeId = 1;
        public GameObject CreatePlayerMaincityPanel;
        private Tween _moveTween; // 保存动画引用
        private Tween _fillTween; // 图片填充动画
        public static MainCityPanel Default;

        private void Awake()
        {
            Default = this;
        }

        private void OnDestroy()
        {
            Default = null;
        }

        protected override void OnInit(IUIData uiData = null)
        {
            mData = uiData as MainCityPanelData ?? new MainCityPanelData();
            // 页签切换
            for (int i = 0; i < Change.childCount - 2; i++)
            {
                int idx = i;
                Transform changeBtn = Change.GetChild(idx);
                Change.GetChild(idx).Find("Image").localScale = Vector3.zero;
                Change.GetChild(idx).Find("underline").localScale = Vector3.zero;
                Change.GetChild(idx).Find("Text").GetComponent<RectTransform>()
                        .anchoredPosition =
                    new Vector2(0, 0f);
                changeBtn.GetComponent<Button>().onClick.AddListener(() =>
                {
                    for (int j = 0; j < Change.childCount - 2; j++)
                    {
                        if (idx != j)
                        {
                            Change.GetChild(j).Find("Image").localScale = Vector3.zero;
                            Change.GetChild(j).Find("underline").localScale = Vector3.zero;
                            Change.GetChild(j).GetComponent<RectTransform>()
                                .DOSizeDelta(new Vector2(197f, 106.56f), 0.5f);
                            Change.GetChild(j).Find("line").GetComponent<RectTransform>().anchoredPosition = 
                                new Vector2(193, Change.GetChild(j).Find("line").GetComponent<RectTransform>().anchoredPosition.y);
                            Change.GetChild(j).Find("Text").GetComponent<RectTransform>()
                                    .anchoredPosition =
                                new Vector2(0, 0f);
                        }
                    }

                    changeBtn.Find("Image").localScale = Vector3.one;
                    changeBtn.Find("underline").localScale = Vector3.one;
                    changeBtn.GetComponent<RectTransform>().DOSizeDelta(new Vector2(197 + 30f, 106.56f), 0.5f);
                    RectTransform Line = changeBtn.Find("line").GetComponent<RectTransform>();
                    Line.GetComponent<RectTransform>().anchoredPosition = new Vector2(242, Line.GetComponent<RectTransform>().anchoredPosition.y);
                    RectTransform rect = changeBtn.Find("Text").GetComponent<RectTransform>();
                    RectTransform selectImage = changeBtn.Find("Image").GetComponent<RectTransform>();
                    Image underlineImage = changeBtn.Find("underline").GetComponent<Image>();

                    // 杀掉已有动画
                    if (_moveTween != null && _moveTween.IsActive()) _moveTween.Kill();
                    if (_fillTween != null && _fillTween.IsActive()) _fillTween.Kill();

                    // 设置初始状态
                    rect.anchoredPosition = new Vector2(0, 0); // 初始 Y 偏移
                    underlineImage.fillAmount = 0f;

                    // 位置过渡动画
                    _moveTween = rect.DOAnchorPos(new Vector2(0, 18f), 0.3f).SetEase(Ease.OutQuad);
                    _fillTween = selectImage.DOAnchorPos(new Vector2(0, 20f), 0.3f).SetEase(Ease.OutQuad);
                    // if (idx == 1)
                    // {
                    //     Transform cardGroupPanel = UIKit.OpenPanel<CardGroupPanel01>().transform;
                    //     cardGroupPanel.SetParent(this.transform);
                    //     cardGroupPanel.SetSiblingIndex(0);
                    //     CreatePlayerMaincityPanel.SetActive(false);
                    // }
                    // else
                    // {
                    //     UIKit.HidePanel<CardGroupPanel01>();
                    // }

                    if (idx == 2)
                    {
                        CreatePlayerMaincityPanel.SetActive(true);
                        CreatePlayerMaincityPanel.transform.SetSiblingIndex(0);
                    }
                    else
                    {
                        CreatePlayerMaincityPanel.SetActive(false);
                    }

                    if (idx == 0)
                    {
                        Transform TalentPanel = UIKit.OpenPanel<TalentPanel>().transform;
                        TalentPanel.SetParent(this.transform);
                        TalentPanel.SetSiblingIndex(0);
                        CreatePlayerMaincityPanel.SetActive(false);
                    }
                    else
                    {
                        UIKit.HidePanel<TalentPanel>();
                    }
                });
                // 默认选中第一个
                if (idx == 0)
                {
                    changeBtn.GetComponent<Button>().onClick.Invoke();
                }
            }

            Transform TalentPanel = UIKit.OpenPanel<TalentPanel>().transform;
            TalentPanel.SetParent(this.transform);
            TalentPanel.SetSiblingIndex(0);
            CreatePlayerMaincityPanel.SetActive(false);
            Change.GetChild(0).Find("Image").localScale = Vector3.one;
            Change.GetChild(1).Find("Image").localScale = Vector3.zero;
            Change.GetChild(2).Find("Image").localScale = Vector3.zero;
            Change.GetChild(0).Find("underline").localScale = Vector3.one;

            // please add init code here
        }

        protected override void OnOpen(IUIData uiData = null)
        {
        }

        public void ChangePage(int pageId)
        {
            Change.transform.GetChild(pageId - 1).GetComponent<Button>().onClick.Invoke();
            switch (pageId)
            {
                case 1: // 天赋
                    var talentPanel = UIKit.OpenPanel<TalentPanel>();
                   // talentPanel.talentObjects[0].transform.Find("icon/EnterShowbtn").GetComponent<Button>().onClick
                   //     .Invoke();
                    break;

                case 2: // 卡组
                    // var cardGroupPanel = UIKit.OpenPanel<CardGroupPanel>();
                    // var cardSlot = cardGroupPanel.Cards.Find("Viewport/Content").GetChild(0);
                    // // 引导 - 角色
                    // GuideFlow.Instance.SetStep(GuideType.SetCardGroup, 1, cardSlot.Find("SoldierBtn"));
                    break;
            }
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