using Config;
using QFramework;
using UnityEngine;

namespace FusionContinent
{
    public class CardGroupPanelData : UIPanelData
    {
    }

    public class CardGroupData : UIPanelData
    {
        public int CardId;
        public readonly CardType CardType;
        public readonly Transform CardTran;

        public CardGroupData(int cardId, Transform cardTran, CardType cardType)
        {
            CardId = cardId;
            CardType = cardType;
            CardTran = cardTran;
        }
    }

    /// <summary>
    /// 旧编队面板：业务逻辑已迁移到 CardGroupPanelNew（MVVM）。
    /// 本类仅作为组件载体保留（Designer 字段、Act1~4 序列化绑定），无任何业务代码。
    /// </summary>
    public partial class CardGroupPanel : UIPanel
    {
        [SerializeField] public UnityEngine.RectTransform Act1;
        [SerializeField] public UnityEngine.RectTransform Act2;
        [SerializeField] public UnityEngine.RectTransform Act3;
        [SerializeField] public UnityEngine.RectTransform Act4;

        protected override void OnClose()
        {
        }
    }
}
