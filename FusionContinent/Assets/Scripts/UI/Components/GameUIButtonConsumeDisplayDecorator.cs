using FusionContinent.Model;
using Game.Shop;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FusionContinent.UI.Components
{
    /// <summary>
    /// 消耗显示装饰器：只负责显示道具图标、数量和足够/不足颜色。
    /// </summary>
    public class GameUIButtonConsumeDisplayDecorator : GameUIButtonDecorator
    {
        [SerializeField] private Image mConsumeIcon;
        [SerializeField] private TextMeshProUGUI mConsumeNum;
        [SerializeField] private HorizontalLayoutGroup mConsumeLayoutGroup;
        [SerializeField] private int mConsumeItemId;
        [SerializeField] private int mNeedCount = 1;

        public override int Order => -20;

        public int ItemId => mConsumeItemId;
        public int NeedCount => mNeedCount;
        public bool IsEnough => Global.GetItemNumById(mConsumeItemId) >= mNeedCount;

        public override void Refresh()
        {
            if (mConsumeIcon == null || mConsumeNum == null)
            {
                LogTool.LogError("[GameUIButtonConsumeDisplayDecorator] 缺少消耗品组件引用（Icon 或 NumText）");
                return;
            }

            if (mConsumeItemId <= 0)
            {
                return;
            }
            var need = new RewardItem(mConsumeItemId, mNeedCount);
            ShopConsumeRenderer.Render(
                mConsumeIcon,
                mConsumeNum,
                mConsumeLayoutGroup,
                primaryItem: need,
                priorityItem: null,
                times: 1,
                isFree: false);
        }

        public void SetConsume(int itemId, int needCount, bool refresh = true)
        {
            mConsumeItemId = itemId;
            mNeedCount = Mathf.Max(0, needCount);

            if (refresh)
            {
                Owner?.Refresh();
            }
        }
    }
}
