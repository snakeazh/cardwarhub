using System.Collections.Generic;
using Config;
using FusionContinent.Base;
using FusionContinent.Helper;
using Helper;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.U2D;

namespace FusionContinent
{
    public class CardSlotData : UIPanelData
    {
        public Sprite Icon;
        /// <summary>散图资源名（GeneralSkill 组），非空时由槽位异步加载，优先于 Icon</summary>
        public string IconName;
        public int CardId = 0;
        public string NameText = "";
        public QualityType QualityType;
        public string LevelText = "";
        public int ManaNum = 0;
        public int StarNum = 0;
        public string Desc;
        public CardState State;
        public bool IsUnlock = true;
        public Vector3 IconScale =  Vector3.one * 0.8f;
        public List<long> ProgressBar = new List<long>();
        public bool CheckRed = false;
        public bool ShowEffect = false;
    }

    public partial class CardSlot : UIPanel
    {
        public TMP_Text cardName;
        public Image cardIcon;
        public Image cardIconBg;
        public Image cardFrame;
        public Image cardTopFrame;
        public Transform lockState;
        public Transform weakIcon;
        
        protected override void OnInit(IUIData uiData = null)
        {
            transform.localScale = Vector3.one;
        }

        public void SetData(CardSlotData data)
        {
            var tweenTarget = transform.Find("TweenTarget");
            
            Color32 qualityColor = Global.QualityTypeColors[data.QualityType];
            SpriteHelper.TryLoadSpriteByAtlas(Global.CarFrameAtlas,$"{data.QualityType}Card",cardFrame);
            SpriteHelper.TryLoadSpriteByAtlas(Global.CarFrameAtlas,$"{data.QualityType}CardFrame",cardTopFrame);
            cardTopFrame.SetNativeSize();
            if (data.QualityType == QualityType.Mythology)
                weakIcon.gameObject.SetActive(true);
            else 
                weakIcon.gameObject.SetActive(false);
            
            if (!string.IsNullOrEmpty(data.IconName))
            {
                SpriteHelper.TryLoadPaintAsync(data.IconName, cardIcon, groupName: "GeneralSkill");
            }
            else
            {
                AsyncTextureManager.Instance.InvalidateImage(cardIcon);
                cardIcon.sprite = data.Icon;
            }
            // cardIcon.SetNativeSize();
            // cardIcon.transform.localScale = data.IconScale;
            cardName.text = $"{data.NameText}";
            
            // 锁定
            var lockColor = new Color32(255, 255, 255, 255);
            if (!data.IsUnlock)
            {
                lockColor = new Color32(155, 155, 155, 255);
            }
            
            cardIconBg.color = lockColor;
            cardIcon.color = lockColor;
            lockState.gameObject.SetActive(!data.IsUnlock);
            if (data.CheckRed)
            {
                // tweenTarget.Find("CardName").GetComponent<RectTransform>().SetLocalRedType(GameDataUtils.GetCardSlotUpgrade(data.CardId));            
            }
            // 进度条
            tweenTarget.Find("ProgressBar").gameObject.SetActive(data.ProgressBar.Count > 0);
            if (tweenTarget.Find("ProgressBar").gameObject.activeSelf)
            {
                // 进度条
                float progress = 1;
                string progressText = "MAX";
                if (data.ProgressBar[0] != -1 && data.ProgressBar.Count >= 2)
                {
                    progressText = $"{data.ProgressBar[0]}/{data.ProgressBar[1]}";
                    progress = data.ProgressBar[0] > 0
                        ? Mathf.Clamp01((float)data.ProgressBar[0] / (float)data.ProgressBar[1])
                        : 0;
                }
                
                tweenTarget.Find("ProgressBar").GetComponent<ProgressBar>().SetProgress(progress, progressText);
            }

            var bgIcon = Global.CardStyleIconSpriteAtlas.GetSprite(data.QualityType.ToString());
            if (bgIcon == null)
            {
                bgIcon = Global.CardStyleIconSpriteAtlas.GetSprite(QualityType.Ordinary.ToString());
            }

            cardIconBg.sprite = bgIcon;
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