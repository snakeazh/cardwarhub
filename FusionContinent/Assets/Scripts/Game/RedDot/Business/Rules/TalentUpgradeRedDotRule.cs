using Config;
using Game.RedDot;
using FusionContinent;
using FusionContinent.Model;

namespace Game.RedDot.Business.Rules
{
    public sealed class TalentUpgradeRedDotRule : RedDotRuleBase
    {
        TbTalentConfig _talentConfig;
        readonly int _talentId = 101;

        public override RedNodeType NodeType => RedNodeType.TalentUpgradeBtn;

        protected override void OnBind()
        {
            _talentConfig = DataConfigManager.Tables.TbTalentConfig;
            Global.Publisher.Subscribe(MessagePublisher.CHANGE_RED_TYPE + RedNodeType.TalentUpgradeBtn, OnRedTypeChange);
            Global.Publisher.Subscribe(MessagePublisher.CHANGE_USER_LEVEL_INFO, OnUserInfoChange);
        }

        protected override void OnUnbind()
        {
            Global.Publisher.Unsubscribe(MessagePublisher.CHANGE_RED_TYPE + RedNodeType.TalentUpgradeBtn, OnRedTypeChange);
            Global.Publisher.Unsubscribe(MessagePublisher.CHANGE_USER_LEVEL_INFO, OnUserInfoChange);
        }

        void OnUserInfoChange(object obj)
        {
            Context.MarkDirty();
        }

        void OnRedTypeChange(object obj)
        {
            Context.MarkDirty();
        }

        public override int Evaluate()
        {
            if (!ModuleFunction.CheckOpen(ModuleFunctionType.Talent, false))
            {
                return 0;
            }

            int talentLevel = Global.PlayerTalents[_talentId].Lv;
            int talentExtraLevel = Global.PlayerTalents[_talentId].ExtraLv;
            TalentListStruct[] displayTalentListStruct = _talentConfig.GetOrDefault(_talentId).TalentList.ToArray();
            int redCount = 0;
            for (int i = 0; i < displayTalentListStruct.Length; i++)
            {
                TalentListStruct data = displayTalentListStruct[i];
                if (data.Level == talentLevel + 1 && data.UnlockLv <= Global.User.Level)
                {
                    bool money = true;
                    for (int j = 0; j < data.ConsumeItems.Count; j++)
                    {
                        long itemnum = Global.GetItemNumById(data.ConsumeItems[j].Id);
                        if (itemnum < data.ConsumeItems[j].Num)
                        {
                            money = false;
                            break;
                        }
                    }

                    if (money)
                    {
                        redCount++;
                        break;
                    }
                }
            }

            for (int i = 0; i < displayTalentListStruct.Length; i++)
            {
                TalentListStruct data = displayTalentListStruct[i];
                if (talentLevel >= data.Level && data.Level > talentExtraLevel
                    && (data.ExtraAttributes.Count > 0 || data.ExtraMainAttributes.Count > 0))
                {
                    bool money = true;
                    for (int j = 0; j < data.ExtraConsumeItems.Count; j++)
                    {
                        long itemnum = Global.GetItemNumById(data.ExtraConsumeItems[j].Id);
                        if (itemnum < data.ExtraConsumeItems[j].Num)
                        {
                            money = false;
                            break;
                        }
                    }

                    if (money)
                    {
                        redCount++;
                        break;
                    }
                }
            }

            return redCount;
        }
    }
}
