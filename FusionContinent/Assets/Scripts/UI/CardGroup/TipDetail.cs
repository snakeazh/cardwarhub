using System;
using Config;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	public class TipDetailData : UIPanelData
	{
	}
	public partial class TipDetail : UIPanel
	{
		private Transform mTemplate;
		private static readonly Color InactiveColor = new Color(0x78 / 255f, 0x6B / 255f, 0x58 / 255f);

		private void Awake()
		{
			ExitBtn.onClick.AddListener(() =>
			{
				transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack)
					.OnComplete(() => gameObject.SetActive(false));
			});

			// 缓存模板行
			if (Content != null && Content.childCount > 0)
				mTemplate = Content.GetChild(0);
		}

		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as TipDetailData ?? new TipDetailData();
		}

		/// <summary>
		/// 根据 BindingConfig 填充内容
		/// </summary>
		public void SetData(BindingConfig cfg, int activeCount = 0)
		{
			if (cfg == null || mTemplate == null) return;

			NameText.text = cfg.Name;

			var carCountList = cfg.CarCountList;
			if (carCountList == null || carCountList.Count == 0) return;

			// 清理旧行（保留模板）
			for (int i = Content.childCount - 1; i >= 0; i--)
			{
				var child = Content.GetChild(i);
				if (child == mTemplate) continue;
				Destroy(child.gameObject);
			}

			// 填充数据行
			for (int i = 0; i < carCountList.Count; i++)
			{
				var item = carCountList[i];
				Transform row;

				if (i == 0)
				{
					row = mTemplate;
				}
				else
				{
					row = Instantiate(mTemplate, Content);
				}
				row.gameObject.SetActive(true);

				var isActive = item.CarCount <= activeCount;

				// CardCountText: "(1)", "(2)", ...
				var cardCountText = row.Find("CardCountText/CardCountText")?.GetComponent<TMP_Text>();
				if (cardCountText != null)
				{
					cardCountText.text = $"({i + 1})";
					cardCountText.color = isActive ? Color.white : InactiveColor;
				}

				// DescText
				var descText = row.Find("Property/DescText")?.GetComponent<TMP_Text>();
				if (descText != null)
				{
					descText.text = item.Desc;
					descText.color = isActive ? Color.white : InactiveColor;
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
