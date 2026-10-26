using Config;
using FusionContinent.Model;
using UnityEngine;

namespace FusionContinent.Guide
{
    public class GuideFingerWeakMethod : GuideFingerWaitMethod
    {
        public override void Enter()
        {
            base.Enter();
            this._context.Panel.Dialoge.gameObject.SetActive(false);
            this._context.Panel.Mask.gameObject.SetActive(false);
            this._context.Panel.NextBtn.gameObject.SetActive(false);
            this._context.Panel.MaskCard.gameObject.SetActive(false);
            this._context.Panel.Maskabled.gameObject.SetActive(false);
            Global.Publisher.Subscribe(MessagePublisher.CHANGE_GUIDE_PROGRESS,OnGuideProgress);
        }

        protected override void SetFingerDepth(RectTransform finger)
        {
            finger.gameObject.SetActive(true);
            // return;
            var psList = finger.GetComponentsInChildren<Renderer>();
            foreach (Renderer rn in psList)
            {
                rn.sortingLayerName = _guideType == EGuideType.Force ? "Guide" : "Default";
            }
        }

        public override void Tick()
        {
            base.Tick();
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
            base.Exit();
            Global.Publisher.Unsubscribe(MessagePublisher.CHANGE_GUIDE_PROGRESS,OnGuideProgress);
        }
    }
}