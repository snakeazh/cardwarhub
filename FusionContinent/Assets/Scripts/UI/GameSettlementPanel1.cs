using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Localization;
using Config;
using DG.Tweening;
using FusionContinent.Helper;
using FusionContinent.Model;
using FusionContinent.Sounds;
using Helper;
using Manager;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using TMPro;
using UnityEngine.SceneManagement;
using Cardwar;
using FusionContinent.Utils;
using UserLevelInfo = FusionContinent.Model.UserLevelInfo;

namespace FusionContinent
{
	public class GameSettlementPanel1Data : UIPanelData
	{
	}
	public partial class GameSettlementPanel1 : UIPanel
	{
		protected override void OnInit(IUIData uiData = null)
        {
        }

      
        protected override void OnOpen(IUIData uiData = null)
        {
        }

        protected override void OnShow()
        {
            // this.gameObject.GetComponent<UIBattleEndAnim>().RunAnimation();
        }

        protected override void OnHide()
        {
        }

        protected override void OnClose()
        {
        }
    
	}
}
