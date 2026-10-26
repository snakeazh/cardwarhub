using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	public class ChestRollPanelData : UIPanelData
	{
	}
	public partial class ChestRollPanel : UIPanel
	{
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as ChestRollPanelData ?? new ChestRollPanelData();
			// please add init code here
			Quit.onClick.AddListener(OnClose);
		}

		void Update()
		{
			if (Input.GetKeyUp(KeyCode.R))
			{
				transform.Find("Canvas/RewardRollPanel").GetComponent<RewardRollPanel>().StartRoll(1);

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
			gameObject.SetActive(false);
		}
	}
}
