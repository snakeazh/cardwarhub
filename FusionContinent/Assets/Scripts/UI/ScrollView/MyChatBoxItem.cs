using FusionContinent.Model;

namespace ScrollView
{
    public class MyChatBoxItem : VariableHeightScrollItem
    {
        protected override void ApplyChatPush(ChatPushModel model)
        {
            SetPlayerInfo(model, true);
        }
    }
}
