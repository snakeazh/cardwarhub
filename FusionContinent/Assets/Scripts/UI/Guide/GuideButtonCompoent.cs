using System;
using UnityEngine;
using UnityEngine.UI;

namespace FusionContinent.Guide
{
    public class GuideButtonCompoent : GuideUIEventCompoent
    {
        private Button _btn;
        protected override void Start()
        {
            base.Start();
            
            _btn = GetButton();
            if (_btn)
            {
                _btn.onClick.AddListener(OnClickGuideBtn);
            }
            
        }

        private void OnDestroy()
        {
            if (_btn)
            {
                _btn.onClick.RemoveListener(OnClickGuideBtn);
            }
        }

        private void OnClickGuideBtn()
        {
            // LogTool.E("引导点击");
            SendGuideEvent();
        }

        private Button GetButton()
        {
            Button btn = gameObject.GetComponent<Button>();
            if (btn == null)
            {
                btn = gameObject.GetComponentInChildren<Button>();
            }

            return btn;
        }
    }
}