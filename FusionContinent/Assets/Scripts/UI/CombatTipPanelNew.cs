using DG.Tweening;
using Game.UI;
using TMPro;
using UIFramework;
using UIFramework.Services;
using UnityEngine;
using FusionContinent.Helper;
using FusionContinent.Manager;

namespace FusionContinent
{
    [PrefabPath("CombatTipPanel")]
    /// <summary>
    /// 战力提示面板 - UIFramework版本
    /// 复用 CombatTipPanel 作为容器，CombatTip 作为实际提示内容。
    /// </summary>
    public class CombatTipPanelNew : UIPanel<CombatTipViewModel>
    {
        public override PanelStackMode StackMode => PanelStackMode.Unmanaged;

        private const string CombatTipPoolName = "CombatTip";
        private Transform mContainer;
        private CombatTip mCombatTip;
        private Tween mAutoHideTween;
        private System.Action mShowHandler;
        private System.Action mHideHandler;

        private readonly Color32 mDefaultMainColor = new Color32(255, 255, 255, 255);
        private Color mAddColor;
        private Color mSubColor;

        void Awake()
        {
            mContainer = transform.Find("Contatiner");
            if (mContainer == null)
            {
                LogTool.LogError("[CombatTipPanelNew] 找不到 Contatiner 节点");
            }
        }

        protected override void OnBindViewModel(CombatTipViewModel vm)
        {
            EnsureTipCreated();
            CacheColors();

            mShowHandler = () => ShowTip(vm);
            mHideHandler = HideTip;
            vm.OnShowRequested += mShowHandler;
            vm.OnHideRequested += mHideHandler;

            gameObject.SetActive(false);
        }

        protected override void OnUnbindViewModel(CombatTipViewModel vm)
        {
            if (mShowHandler != null)
            {
                vm.OnShowRequested -= mShowHandler;
                mShowHandler = null;
            }

            if (mHideHandler != null)
            {
                vm.OnHideRequested -= mHideHandler;
                mHideHandler = null;
            }

            mAutoHideTween?.Kill();
            mAutoHideTween = null;

            if (mCombatTip != null)
            {
                var pool = CommonItemPoolManager.Instance?.GetGameObjectPool(CombatTipPoolName);
                pool?.Return(mCombatTip.gameObject);
                mCombatTip = null;
            }
        }

        private void EnsureTipCreated()
        {
            if (mCombatTip != null)
            {
                return;
            }

            if (mContainer == null)
            {
                return;
            }

            var pool = CommonItemPoolManager.Instance?.GetGameObjectPoolOrCreate(CombatTipPoolName);
            var tipObj = pool?.Get();
            if (tipObj == null)
            {
                LogTool.LogError("[CombatTipPanelNew] 无法从对象池获取 CombatTip");
                return;
            }

            tipObj.transform.SetParent(mContainer, false);
            tipObj.transform.localPosition = Vector3.zero;
            tipObj.transform.localScale = Vector3.one;
            tipObj.name = CombatTipPoolName;

            mCombatTip = tipObj.GetComponent<CombatTip>();
            if (mCombatTip == null)
            {
                LogTool.LogError("[CombatTipPanelNew] CombatTip 组件不存在");
                pool?.Return(tipObj);
            }
        }

        private void CacheColors()
        {
            ColorUtility.TryParseHtmlString("#" + DataConfigManager.Tables.TbGameBattleConst.BattleNumberUp, out mAddColor);
            ColorUtility.TryParseHtmlString("#" + DataConfigManager.Tables.TbGameBattleConst.BattleNumberDown, out mSubColor);
        }

        private void ShowTip(CombatTipViewModel vm)
        {
            EnsureTipCreated();
            if (mCombatTip == null)
            {
                return;
            }

            gameObject.SetActive(true);
            mCombatTip.gameObject.SetActive(true);
            mCombatTip.transform.localScale = Vector3.one;
            mCombatTip.transform.localPosition = Vector3.zero;

            var symbolText = mCombatTip.combatNumTextNew.transform.childCount > 0
                ? mCombatTip.combatNumTextNew.transform.GetChild(0).GetComponent<TMP_Text>()
                : null;

            mCombatTip.combatNumText.text = vm.StartValue.Value.ToString();
            mCombatTip.combatNumTextNew.text = vm.ChangeValue.Value.ToString();
            SpriteHelper.TryLoadPaintAsync("EnhanceCombatPower1", mCombatTip.combatTipBg);

            if (symbolText != null)
            {
                symbolText.text = vm.IsAdd.Value ? "+" : "-";
                symbolText.color = vm.IsAdd.Value ? mAddColor : mSubColor;
            }

            mCombatTip.combatNumText.color = mDefaultMainColor;
            mCombatTip.combatNumTextNew.color = vm.IsAdd.Value ? mAddColor : mSubColor;

            var rollingNumber = mCombatTip.combatNumText.GetComponent<RollingNumberManager>();
            if (rollingNumber != null)
            {
                rollingNumber.Play(vm.EndValue.Value);
            }
            else
            {
                mCombatTip.combatNumText.text = vm.EndValue.Value.ToString();
            }

            mAutoHideTween?.Kill();
            mAutoHideTween = DOTween.Sequence()
                .AppendInterval(2f)
                .AppendCallback(() => vm.HideTip());
        }

        private void HideTip()
        {
            gameObject.SetActive(false);
        }

        protected override void OnOpen(IUIData uiData = null)
        {
            gameObject.SetActive(false);
        }

        protected override void OnShow()
        {
        }

        protected override void OnHide()
        {
            gameObject.SetActive(false);
        }

        protected override void OnClose()
        {
            gameObject.SetActive(false);
        }
    }
}
