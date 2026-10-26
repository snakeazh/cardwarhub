using Config;
using FusionContinent.Guide;
using UIFramework;

namespace Game.Guide
{
    /// <summary>
    /// 引导面板 ViewModel。
    /// 持有当前引导步骤与执行方法（由 GuideManager 在每次步骤开始前写入），
    /// View 与各 GuideMethod 从这里读取。
    /// </summary>
    public class GuidePanel2ViewModel : ViewModel
    {
        /// <summary>当前引导步骤配置</summary>
        public GuidesStepListStruct Step { get; set; }

        /// <summary>当前引导执行方法</summary>
        public IGuideMethod Current { get; set; }

        /// <summary>最近一次选中的卡牌 Id（指定手指引导用）</summary>
        public int CardId { get; set; }
    }
}
