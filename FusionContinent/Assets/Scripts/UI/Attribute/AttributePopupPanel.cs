using System.Collections.Generic;
using System.Globalization;
using Assets.Scripts.Localization;
using Config;
using FusionContinent.Animation;
using FusionContinent.Manager;
using Manager;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using ScrollView;
using TMPro;
using UnityEngine.Events;

namespace FusionContinent
{
	public class AttributePopupPanelData : UIPanelData
	{
	}
	public partial class AttributePopupPanel : UIPanel
	{
		public static AttributePopupPanel Default;
		
		private TbAttributeConfig _attributeConfig;
		private List<AttributeDataView> _attributeList;
		
		private void Awake()
		{
			Default = this;
			_attributeConfig = DataConfigManager.Tables.TbAttributeConfig;
		}
		
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as AttributePopupPanelData ?? new AttributePopupPanelData();
			Mask.onClick.AddListener(OnClose);
			CloseBtn.onClick.AddListener(OnClose);
			AttrScrollview.SetUpdateFunc(OnUpdateCardItem);
		}
		
		public void SetData(List<AttributeDataView> attributeList)
		{
			_attributeList = attributeList;
			if (_attributeList.Count == 0) return;
			
			AttrScrollview.ShowList(_attributeList.Count);
			AttrScrollview.ScrollToIndex(0);
			AttrScrollview.PlayScaleAnimationDOTween();
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
		
		protected override void RunAnimation(GameObject target, UnityAction complete)
		{
			UIAnimationUtils.PlayAnimations(target, complete);
		}
		
		protected override GameObject GetAnimationCloseNode
		{
			get => this.CloseAnimation.gameObject;
		}

		protected override GameObject GetAnimationOpenNode
		{
			get => this.OpenAnimation.gameObject;
		}
		
		protected override void OnClose()
		{
			RunAnimation(this.GetAnimationCloseNode, () =>
			{
				UIKit.ClosePanel(this);
			});
		}
	}
}
