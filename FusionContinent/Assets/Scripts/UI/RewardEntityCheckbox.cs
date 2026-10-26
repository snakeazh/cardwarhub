using System;
using Cysharp.Threading.Tasks;
using FusionContinent.UI.Components;
using FusionContinent.Utils;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	public class RewardEntityCheckboxData : UIPanelData
	{
	}
	public partial class RewardEntityCheckbox : UIPanel
	{
		private int _itemId;
		private bool _isCheckbox;
		private Action<int> _callback;
		
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as RewardEntityCheckboxData ?? new RewardEntityCheckboxData();
			// please add init code here
		}
		
		public void SetData(int itemId, long itemNum, Action<int> callback, int style = 0, bool isCheckbox = false)
		{
			_itemId = itemId;
			_callback = callback;
			_isCheckbox = isCheckbox;
			var itemCfg = DataConfigManager.Tables.TbItemConfig.GetOrDefault(_itemId);
			
			// Bg.color = Global.QualityTypeColors[itemCfg.Quality];
			Icon.sprite = Global.ItemIconSpriteAtlas.GetSprite(itemCfg.Icon);
			Frame.sprite = Global.ItemIconSpriteAtlas.GetSprite($"{itemCfg.Quality}Box");
			
			// 边框
			// if (Frame != null) Frame.color = Global.QualityTypeFrameColors[itemCfg.Quality];
			// if (FrameOut != null) FrameOut.color = Global.QualityTypeFrameOutColors[itemCfg.Quality];

			// 状态
			if (Checkbox != null) Checkbox.gameObject.SetActive(_isCheckbox && style == 0);
			if (Checkbox2 != null) Checkbox2.gameObject.SetActive(_isCheckbox && style == 1);
			
			// 道具数量
			var txtColor = itemNum > 0 ? "#FFFFFF" : "#FF7272";
			Num.text = $"<color={txtColor}>{NumberTool.FormatNumber(itemNum)}</color>";
			
			// 点击事件
			GetComponent<UIPointerClickAndLongPress>().onClick.RemoveAllListeners();
			GetComponent<UIPointerClickAndLongPress>().onClick.RemoveAllListeners();
			GetComponent<UIPointerClickAndLongPress>().onClick.AddListener(() =>
			{
				var data = new RewardEntityData
				{
					ItemId = _itemId,
					Num = itemNum,
					Icon = itemCfg.Icon,
					Quality = itemCfg.Quality,
					IconSprite = Global.ItemIconSpriteAtlas.GetSprite(itemCfg.Icon),
					Name = itemCfg.Name,
					Desc = itemCfg.Desc,
					Access = itemCfg.Access,
				};
				ItemShowPanelViewModel vm = new ItemShowPanelViewModel();
				vm.OnCreate();
				vm.Data = data;
				UIFramework.UIManager.OpenPanelAsync<ItemShowPanelNew, ItemShowPanelViewModel>(vm, UIFramework.UILevel.PopUI).Forget();
			});

			
			GetComponent<UIPointerClickAndLongPress>().onClick.AddListener(() =>
			{
				// if (itemNum <= 0) return;
				
				_isCheckbox = !_isCheckbox;
				
				if (_callback != null) _callback(_itemId);
					// [已注释] 点击时不在显隐勾选框
					// if (style == 0 && Checkbox != null) Checkbox.gameObject.SetActive(_isCheckbox);
					// if (style == 1 && Checkbox2 != null) Checkbox2.gameObject.SetActive(_isCheckbox);
			});
		}
		
		public void SetData(RewardEntityData data, Action<int> callback, int style = 0)
		{
			_callback = callback;
			_isCheckbox = false;
			
			Bg.color = Global.QualityTypeFrameColors[data.Quality];
			Icon.sprite = data.IconSprite != null ? data.IconSprite : Global.ItemIconSpriteAtlas.GetSprite(data.Icon);
			
			// 边框
			if (Frame != null) Frame.color = Global.QualityTypeFrameColors[data.Quality];
			if (FrameOut != null) FrameOut.color = Global.QualityTypeFrameOutColors[data.Quality];

			// 状态
			// if (Checkbox != null)  Checkbox.gameObject.SetActive(_isCheckbox);
			// if (Checkbox2 != null)  Checkbox2.gameObject.SetActive(_isCheckbox);
			
			// 道具数量
			var txtColor = data.Num > 0 ? "#FFFFFF" : "#FF7272";
			Num.text = $"<color={txtColor}>{NumberTool.FormatNumber(data.Num)}</color>";
			
			// 点击事件
			GetComponent<UIPointerClickAndLongPress>().onClick.RemoveAllListeners();
			GetComponent<UIPointerClickAndLongPress>().onLongPress.RemoveAllListeners();
			GetComponent<UIPointerClickAndLongPress>().onLongPress.AddListener(() =>
			{
				LogTool.E("长按RewardEntity....");
				ItemShowPanelViewModel vm = new ItemShowPanelViewModel();
				vm.OnCreate();
				vm.Data = data;
				UIFramework.UIManager.OpenPanelAsync<ItemShowPanelNew, ItemShowPanelViewModel>(vm, UIFramework.UILevel.PopUI).Forget();
			});
			GetComponent<UIPointerClickAndLongPress>().onClick.AddListener(() =>
			{
				if (data.Num <= 0) return;
				_isCheckbox = !_isCheckbox;
				
				if (_callback != null) _callback(data.ItemId);
				// if (style == 0 && Checkbox != null)  Checkbox.gameObject.SetActive(_isCheckbox);
				// if (style == 1 && Checkbox2 != null)  Checkbox2.gameObject.SetActive(_isCheckbox);
			});
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
