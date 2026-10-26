using Config;
using FusionContinent.Model;
using QFramework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FusionContinent.Guide
{
    public class GuideDialogMethod : IGuideMethod
    {
        private GuidesStepListStruct _stepListStruct;
        private GuideContext _context;
        public void Init(GuideContext ctx, GuidesStepListStruct step)
        {
            _stepListStruct = step;
            _context = ctx;
        }
        bool isClick = false;
        public void Enter()
        {
            this._context.Panel.Dialoge.gameObject.SetActive(true);
            this._context.Panel.Text.text = _stepListStruct.TipText;
            this._context.Panel.NextBtn.gameObject.SetActive(true);
            // this._context.Panel.NextBtn.onClick.RemoveAllListeners();
            this._context.Panel.NextBtn.GetComponent<Graphic>().raycastTarget = true;
            // this._context.Panel.NextBtn.onClick.AddListener(Next);
            this._context.Panel.Mask.gameObject.SetActive(true);
            this._context.Panel.Finger.gameObject.SetActive(false);
            this._context.Panel.Maskabled.gameObject.SetActive(false);
        }

        void Next()
        {
            isClick = true;
        }

        public void Tick()
        {
        }

        public bool IsDone()
        {
            return isClick;
        }

        public void Exit()
        {
            //this._context.Panel.NextBtn.GetComponent<Graphic>().raycastTarget = false;
            // this._context.Panel.Dialoge.gameObject.SetActive(false);
            // this._context.Panel.NextBtn.gameObject.SetActive(false);
        }
    }
}