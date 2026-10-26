using FusionContinent.Guide;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	public partial class ChestRotaryTableLevel : ViewController
	{
		public static ChestRotaryTableLevel Default;
		
		private int _openCount = 0;

		public int OpenCount
		{
			get
			{
				return _openCount;
			}
		}
		private void Awake()
		{
			Default = this;
		}

		void Start()
		{
			// 开启转盘
			TableBtn.onClick.AddListener(() =>
			{
				// if (GamePlayManager.Default.GuideIsRunning())
				// {
				// 	return;
				// }
				if (_openCount > 0) return;
				_openCount++;
				
				UIKit.OpenPanel<RotaryTablePanel>(UILevel.PopUI).SetData((goldNum) =>
				{
					GoldCoinManager.Instance.CreateCoin(goldNum, Vector3.zero); // 掉落金币
				});
				
				Icon.color = Color.gray;
				Effect.gameObject.SetActive(false);
			});
			
			Icon.color = Color.white;
			Effect.gameObject.SetActive(true);
			SetLevelText(Global.CityLevel.Value);
		}

		public void SetLevelText(int level)
		{
			LevelText.text = level.ToString();
		}
	}
}
