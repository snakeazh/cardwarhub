using Config;
using FusionContinent.Model;
using QFramework;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FusionContinent.Guide
{
    public class NewGuideWeakCardMethod : GuideFingerWaitMethod
    {
        public override void Enter()
        {
            base.Enter();
            InitDialog();
            // Global.Publisher.Subscribe(MessagePublisher.CHANGE_GUIDE_PROGRESS,OnGuideProgress);
        }
        
        void InitDialog()
        {
            this._context.Panel.Mask.gameObject.SetActive(true);
            this._context.Panel.Dialoge.gameObject.SetActive(true);
            this._context.Panel.Text.text = _stepListStruct.TipText;
            // var trigger = this._context.Panel.NextBtn.GetComponent<UIEventListener>();
            // if (trigger)
            // {
            //     Object.DestroyImmediate(trigger);
            // }
            // UIEventListener.Get(this._context.Panel.NextBtn.gameObject).onPointerUp+= OnPointerDownEvent;
        }

        public override void Tick()
        {
            if (Input.GetMouseButtonDown(0))
            {
           
                var cam = guideCanvas.worldCamera;
                var pos = Input.mousePosition;
                var world = cam.ScreenToWorldPoint(pos);
                var hit = Physics2D.Raycast(world, Vector2.zero);
                if (hit.collider != null && this._context.Panel.NextBtn.gameObject.Equals(hit.collider.gameObject))
                {
                    _isDone = true;
                }
            }
            base.Tick();
        }

        private void OnPointerDownEvent(BaseEventData obj)
        {
            _isDone = true;
        }
        

        private void OnGuideProgress(object obj)
        {
            int value = (int)obj;
            if (value == this._stepListStruct.Step)
            {
                _isDone = true;
            }
        }
        public override void Exit()
        {
            

            // Global.Publisher.Unsubscribe(MessagePublisher.CHANGE_GUIDE_PROGRESS,OnGuideProgress);
            base.Exit();
        }
        
    }
}