using UnityEngine;
using QFramework;

namespace FusionContinent
{
    public class ItemShowPanelData : UIPanelData
    {
    }

    /// <summary>
    /// 旧界面逻辑已迁移至 ItemShowPanelNew，此组件仅保留 prefab 节点绑定
    /// </summary>
    public partial class ItemShowPanel : UIPanel
    {
        public Transform panelBG;

        protected override void OnClose()
        {
        }
    }
}
