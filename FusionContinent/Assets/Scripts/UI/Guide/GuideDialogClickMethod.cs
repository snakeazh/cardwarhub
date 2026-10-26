using Config;
using FusionContinent.Model;
using UnityEngine;
using UnityEngine.UI;

namespace FusionContinent.Guide
{
    public class GuideDialogClickMethod : GuideFingerWaitMethod
    {

        private bool clicked = false;
        protected GuideUIEventCompoent m_button;


        public  override  void Enter()
        {
            var obj  = GetInterObject(_stepListStruct.GuideUI);
            base.Enter();
            this._context.Panel.Mask.gameObject.SetActive(true);
            if (this._stepListStruct.TipType != GuideTipType.StrongGuidanceBoss)
            {
                InitGuideBtn(obj);
            }
            InitDialog();
            
            this._context.Panel.NextBtn.gameObject.SetActive(this._stepListStruct.Type != GuideType.CanBeUpgraded );        
            var trigger = this._context.Panel.NextBtn.GetComponent<Button>();
            if (trigger)
            {
                // Object.DestroyImmediate(trigger);
            }
            if (this._stepListStruct.Type == GuideType.FirstChooseCard|| this._stepListStruct.Type == GuideType.RefreshTip)
            {
                var newPanel = UIFramework.UIManager.GetPanel<SchoolChooseCardPanelNew>();
                newPanel?.SetPause(true);
            }
            if (this._stepListStruct.Type != GuideType.SkillGuide)
            {
                this._context.Panel.Mask.GetComponent<MaskableGraphic>().raycastTarget = false;
                this._context.Panel.MaskCard.gameObject.SetActive(false);
 
            }
            else
            {
                this._context.Panel.Mask.GetComponent<MaskableGraphic>().raycastTarget = false;
                this._context.Panel.MaskCard.gameObject.SetActive(true);
                if (this._stepListStruct.Type == GuideType.RefreshTip)
                {
                    this._context.Panel.NextBtn.gameObject.SetActive(true);   
                }
                else
                {
                    this._context.Panel.NextBtn.gameObject.SetActive(false );   
                }
            }

            trigger.onClick.RemoveAllListeners();
            trigger.onClick.AddListener(() =>
            {
                if (this._stepListStruct.Type != GuideType.SkillGuide)
                {
                    _isDone = true;
                }
            });
            Global.Publisher.Subscribe(MessagePublisher.CHANGE_GUIDE_PROGRESS,OnGuideProgress);
        }

        protected virtual void InitGuideBtn(GameObject target)
        {
            
            m_button = target.AddComponent<GuideButtonCompoent>();
            m_button.guideType = EGuideType.Force;
        }

        void InitDialog()
        {
            Vector2 basePos = Vector2.zero;
            bool addOpt = true;
            if (this._stepListStruct.Type == GuideType.BreakingToughnessWordTip|| this._stepListStruct.Type == GuideType.WeaknessWordTip)
            {
                basePos = this._context.Panel.Maskabled.anchoredPosition;
                if (basePos.y > 0f)
                {
                    addOpt = false;
                }
            }
            this._context.Panel.Dialoge.gameObject.SetActive(true);
            if (addOpt)
            {
                this._context.Panel.Dialoge.anchoredPosition =basePos+
                                                              new Vector2(this._stepListStruct.DialogePosition.X, this._stepListStruct.DialogePosition.Y);
            }
            else
            {
                this._context.Panel.Dialoge.anchoredPosition =basePos-
                                                              new Vector2(this._stepListStruct.DialogePosition.X, this._stepListStruct.DialogePosition.Y);
            }
           
            this._context.Panel.Text.text = _stepListStruct.TipText;
        }

        public override void Tick()
        {
            // throw new System.NotImplementedException();
        }
        
        private void OnGuideProgress(object obj)
        {
            int value = (int)obj;
            
            if (value == (int)this._stepListStruct.Type)
            {
                _isDone = true;
            }
        }
        public override void Exit()
        {
            this._context.Panel.Dialoge.gameObject.SetActive(false);
            base.Exit();
            this._context.Panel.Mask.gameObject.SetActive(false);
            Global.Publisher.Unsubscribe(MessagePublisher.CHANGE_GUIDE_PROGRESS,OnGuideProgress);
            this._context.Panel.MaskCard.gameObject.SetActive(false);
            Object.Destroy(m_button);
        }
    }
}