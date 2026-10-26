using Config;
using FusionContinent.Guide;

namespace FusionContinent.Guide
{
    public interface IGuideMethod
    {
        void Init(GuideContext ctx, GuidesStepListStruct step);
        void Enter();
        void Tick();
        bool IsDone();
        void Exit();
    }
}
