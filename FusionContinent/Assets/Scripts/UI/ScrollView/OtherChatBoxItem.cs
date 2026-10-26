using FusionContinent.Model;

namespace ScrollView
{
    public class OtherChatBoxItem : VariableHeightScrollItem
    {
        protected override void ApplyChatPush(ChatPushModel model)
        {
            SetPlayerInfo(model, true);
        }
    }
}
