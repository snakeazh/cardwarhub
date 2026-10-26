using UnityEngine;
using QFramework;
using Config;
using TMPro;
using FusionContinent.Model;
using System.Collections.Generic;
using Assets.Scripts.Localization;
using FusionContinent.Helper;
using Button = UnityEngine.UI.Button;

namespace FusionContinent
{
    public class ShopPanel01Data : UIPanelData
    {
    }

    public partial class ShopPanel01 : UIPanel
    {
    
        protected override void OnInit(IUIData uiData = null)
        {
            mData = uiData as ShopPanel01Data ?? new ShopPanel01Data();
           
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