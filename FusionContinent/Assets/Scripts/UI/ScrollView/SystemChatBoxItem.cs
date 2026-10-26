using System.Collections;
using System.Collections.Generic;
using FusionContinent.Model;
using UnityEngine;


namespace ScrollView
{
    public class SystemChatBoxItem : VariableHeightScrollItem
    {


        protected override void ApplyChatPush(ChatPushModel model)
        {
            MeasureText.text = GetMeasureContent(model);
        }
    }
    
}
