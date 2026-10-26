using System;

namespace FusionContinent.Guide
{
    public class GuideLongClickCompoent : GuideUIEventCompoent
    {
        private CardSlotLongClick _btn;
        protected override void Start()
        {
            base.Start();
            
            _btn = GetButton();
            if (_btn)
            {
                _btn.onLongPress.AddListener(OnLongPress);
            }
            
        }
        
        private void OnDestroy()
        {
            if (_btn)
            {
                _btn.onLongPress.RemoveListener(OnLongPress);
            }
        }

        private void OnLongPress()
        {
            // LogTool.E("引导长按");
            SendGuideEvent();
        }


        private CardSlotLongClick GetButton()
        {
            CardSlotLongClick btn = gameObject.GetComponent<CardSlotLongClick>();
            if (btn == null)
            {
                btn = gameObject.GetComponentInChildren<CardSlotLongClick>();
            }

            return btn;
        }
    }
}