using System.Collections.Generic;
using Assets.Scripts.Localization;
using Config;
using FusionContinent.Attribute;
using FusionContinent.Manager;
using FusionContinent.Model;
using Manager;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using ScrollView;
using TMPro;

namespace FusionContinent
{
    public class HeroDetailsData : UIPanelData
    {
    }
    public partial class HeroDetails : UIPanel
    {
        public static AttributePopupPanel Default;
		
        private PlayerCard _cardInfo;
        private SoldierBattleConfig _soldierCfg;
        private TbAttributeConfig _attributeConfig;
        private TbCardBaseConfig _cardBaseConfig;
        private TbSoldierBattleConfig _soldierBattleConfig;
        private List<AttributeDataView> _attributeList = new List<AttributeDataView>();

		
        protected override void OnInit(IUIData uiData = null)
        {
            mData = uiData as HeroDetailsData ?? new HeroDetailsData();
            // please add init code here
            _attributeConfig = DataConfigManager.Tables.TbAttributeConfig;
            _cardBaseConfig = DataConfigManager.Tables.TbCardBaseConfig;
            _soldierBattleConfig = DataConfigManager.Tables.TbSoldierBattleConfig;

            AttrScrollview.SetUpdateFunc(OnUpdateCardItem);
            Mask.onClick.AddListener(() =>
            {
                UIKit.ClosePanel(this);
            });
        }

        public void SetData(SoldierCard soldierCard)
        {
            var cardCfg = _cardBaseConfig.GetOrDefault(soldierCard.cardId);
            var skillCfg = DataConfigManager.Tables.TbSpecialEffectsConfig.GetOrDefault(cardCfg.Id);
            
            // Spine
            for (int i = 0; i < Spine.childCount; i++)
                Destroy(Spine.GetChild(i).gameObject);
            
            Spine.gameObject.SetActive(true);
            var soldierCfg = DataConfigManager.Tables.TbSoldierBattleConfig.GetOrDefault(cardCfg.Id);
            if (soldierCfg != null && soldierCfg.Spine != "")
            {
                var soliderSpine = Global.ResLoader.LoadSync<GameObject>(soldierCfg.Spine).Instantiate().transform;
                soliderSpine.SetParent(Spine);
                soliderSpine.localScale = Vector3.one;
                soliderSpine.localPosition = Vector3.zero;
                Spine.localScale = soldierCfg.SpineScale > 0 ? Vector3.one * soldierCfg.SpineScale : Vector3.one;
            }
            
            SetMainAttributes(soldierCard.BaseAttributes);

            for (int i = 0; i < soldierCfg.ShowAttributeTypes.Count; i++)
            {
                _attributeList.Add(new AttributeDataView()
                {
                    Type = soldierCfg.ShowAttributeTypes[i],
                    Name = soldierCard.BaseAttributes.GetAttr(soldierCfg.ShowAttributeTypes[i]).GetAttrName(),
                    Value = soldierCard.BaseAttributes.GetFinal(soldierCfg.ShowAttributeTypes[i])
                });
            }
			     
            AttrScrollview.ShowList(_attributeList.Count);
            AttrScrollview.ScrollToIndex(0);
            AttrScrollview.PlayScaleAnimationDOTween();
            
        }
		
        private void SetMainAttributes(AttributeSet mainAttrs)
        {
            var attr = mainAttrs.GetAllMainAttributeValues();
            for (int i = 0; i < MainAttributes.transform.childCount; i++)
                MainAttributes.transform.GetChild(i).gameObject.SetActive(false);

            int j = 0;
            //设置属性值
            foreach (var v in attr)
            {
                var attrTran = MainAttributes.transform.childCount > j
                    ? Instantiate(MainAttributes.transform.GetChild(0), MainAttributes.transform)
                    : MainAttributes.transform.GetChild(j);
            	
                attrTran.gameObject.SetActive(true);
                attrTran.Find("AttributeIcon/Icon").GetComponent<Image>().sprite = Global.AttributeIconAtlas.GetSprite(v.Key.ToString());
                attrTran.Find("NumText").GetComponent<TMP_Text>().text = v.Value.ToString();
                j++;
            }
        }
		
        private void OnUpdateCardItem(int dataIndex, ScrollItem scrollItem)
        {
            var attr = _attributeList[dataIndex];
            var attrCfg = _attributeConfig.GetOrDefault(attr.Type);

            // 设置数据
            var attrTran = scrollItem.transform.Find("TweenTarget");
            attrTran.Find("Icon").GetComponent<Image>().sprite = Global.AttributeIconAtlas.GetSprite(attrCfg.Icon);
            attrTran.Find("TitleText").GetComponent<LocalizationText>().SetData(LocalizationModule.UITexts, attrCfg.Name, "");
            attrTran.Find("NumText").GetComponent<RollingNumberManager>().Play(attr.Value);
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