using Config;
using UnityEngine;

namespace FusionContinent.UI.Components
{
    /// <summary>
    /// 消耗校验装饰器：不足时可禁用按钮，或允许点击但拦截逻辑并显示不足提示。
    /// </summary>
    public class GameUIButtonConsumeValidatorDecorator : GameUIButtonDecorator
    {
        [SerializeField] private GameUIButtonConsumeDisplayDecorator mConsumeDisplay;

        [Tooltip("勾选后不足时禁用Button；不勾选时按钮仍可点，但只弹不足提示，不触发逻辑点击。")]
        [SerializeField] private bool mDisableWhenNotEnough = false;

        [SerializeField] private string mNotEnoughTipOverride = "";

        private bool _isEnough;

        public override int Order => -10;

        public override bool AllowButtonInteractable => !mDisableWhenNotEnough || _isEnough;

        public override void Initialize(GameUIButton owner)
        {
            base.Initialize(owner);
            if (mConsumeDisplay == null)
            {
                mConsumeDisplay = GetComponent<GameUIButtonConsumeDisplayDecorator>();
            }
        }

        public override void Refresh()
        {
            _isEnough = mConsumeDisplay == null || mConsumeDisplay.IsEnough || mConsumeDisplay.ItemId <= 0;
        }

        public override bool BeforeLogicClick()
        {
            Refresh();
            if (_isEnough)
            {
                return true;
            }

            Toast.Show(GetNotEnoughTip(), 1.0f);
            return false;
        }

        private string GetNotEnoughTip()
        {
            if (!string.IsNullOrWhiteSpace(mNotEnoughTipOverride))
            {
                return mNotEnoughTipOverride;
            }

            if (mConsumeDisplay == null)
            {
                return "道具不足";
            }

            var itemCfg = DataConfigManager.Tables.TbItemConfig.GetOrDefault(mConsumeDisplay.ItemId);
            string itemName = itemCfg != null ? itemCfg.Name : mConsumeDisplay.ItemId.ToString();
            return $"{itemName}不足";
        }
    }
}
