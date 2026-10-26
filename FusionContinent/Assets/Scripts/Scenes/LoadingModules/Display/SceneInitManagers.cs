using FusionContinent;
using FusionContinent.Guide;
using UnityEngine;

/// <summary>
/// 管理器初始化模块 - 初始化引导等管理器
/// </summary>
public class SceneInitManagers : SceneMapBase
{
    public override string ModuleName => "管理器初始化";

    protected override void OnStartLoad()
    {
        var gpm = GamePlayManager.Default;
        if (gpm == null)
        {
            ErrorMessage = "GamePlayManager.Default is null";
            return;
        }

        // 初始化引导管理器
        var guide = new GameObject("GuideManager",typeof(GuideManager));
        guide.transform.SetParent(this.transform);
        // 初始化场景引导和UI引导
        var gameGuide = GameObject.Find("GameGuide");
        if (gameGuide != null)
        {
            var guidePanel = gameGuide.transform.Find("InGameGuidePanel");
            if (guidePanel != null)
            {
                var guidePanel2 = guidePanel.GetComponent<GuidePanel2New>();
                if (guidePanel2 != null)
                {
                    GuideManager.Instance.SetInWarPanel(guidePanel2);
                }
            }
        }

        progress = 1.0f;
    }
}
