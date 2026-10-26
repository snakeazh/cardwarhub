using QFramework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FusionContinent.Guide
{
    public class GuideDialogClickNoInterMethod: GuideFingerWaitMethod
    {
        public  override  void Enter()
        {

            base.Enter();
            this._context.Panel.Mask.gameObject.SetActive(true);

            this._context.Panel.NextBtn.gameObject.SetActive(true);
            InitDialog();
            var trigger = this._context.Panel.NextBtn.GetComponent<Button>();
            if (trigger)
            {
                // Object.DestroyImmediate(trigger);
            }
            
            trigger.onClick.RemoveAllListeners();
            trigger.onClick.AddListener(() =>
            {
                _isDone = true;
            });
            this._context.Panel.MaskCard.gameObject.SetActive(false);
        }



        void InitDialog()
        {
            this._context.Panel.Dialoge.gameObject.SetActive(true);
            this._context.Panel.Text.text = _stepListStruct.TipText;
        }
    }
}