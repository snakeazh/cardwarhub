using UnityEngine;

namespace FusionContinent.Guide
{
    public class GuideDialogLongPressMethod : GuideDialogClickMethod
    {
        protected override void InitGuideBtn(GameObject target)
        {
            m_button = target.AddComponent<GuideLongClickCompoent>();
            m_button.guideType = EGuideType.Force;
        }
    }
}