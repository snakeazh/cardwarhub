using Cysharp.Threading.Tasks;
using QFramework;
using UnityEngine;
using UnityEngine.UI;

namespace FusionContinent
{
    public class SchoolChooseCardPanelData : UIPanelData
    {
    }

    public partial class SchoolChooseCardPanel : QFramework.UIPanel
    {
        public TMPro.TextMeshProUGUI numberText;
        public static SchoolChooseCardPanel Default;
        public int singleChooseCount = 3;

        protected void Awake()
        {
            Default = this;
            ResetData();
        }

        protected override void OnInit(QFramework.IUIData uiData = null)
        {
            mData = uiData as SchoolChooseCardPanelData ?? new SchoolChooseCardPanelData();
        }

        public void ResetData()
        {
            if (GetAllBtn != null)
            {
                GetAllBtn.interactable = true;
            }

            if (CountdownText != null)
            {
                CountdownText.text = "60";
            }

            if (ChooseList != null)
            {
                for (int i = 0; i < ChooseList.childCount; i++)
                {
                    var child = ChooseList.GetChild(i);
                    if (child.name.StartsWith("ChoseCard"))
                    {
                        child.gameObject.SetActive(false);
                    }
                }
            }
        }

        public void OpenSingleChoose(int openCount = 0)
        {
            var vm = new Game.SchoolChooseCard.SchoolChooseCardViewModel();
            vm.OnCreate();
            vm.Initialize(openCount);
            UIFramework.UIManager.OpenPanelAsync<SchoolChooseCardPanelNew, Game.SchoolChooseCard.SchoolChooseCardViewModel>(
                vm,
                UIFramework.UILevel.PopUI).Forget();
        }

        public void AddSpecialSchoolChooseCards(int cardCount = 1)
        {
            UIFramework.Services.GameServiceManager.Get<Game.Services.SchoolChooseCardService>()
                .AddSpecialSchoolChooseCards(cardCount);
        }

        protected override void OnOpen(QFramework.IUIData uiData = null)
        {
            if (numberText != null)
            {
                numberText.text = Global.GameGold.Value.ToString();
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

        public void PauseTime(bool _pause)
        {
            var newPanel = GetComponent<SchoolChooseCardPanelNew>();
            newPanel?.SetPause(_pause);
        }
    }
}