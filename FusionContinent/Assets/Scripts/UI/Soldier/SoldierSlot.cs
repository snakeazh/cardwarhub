using Config;
using Cysharp.Threading.Tasks;
using FusionContinent.Helper;
using Game.RedDot.Rules;
using Game.Soldier;
using UIFramework.Services;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using TMPro;

namespace FusionContinent
{
    public class SoldierSlotData : UIPanelData
    {
    }
    public partial class SoldierSlot : UIPanel
    {
        private TbSkinConfig _skinConfig;
        private int _soldierId;
        private Button _btn;

        // ActiveFrame 下的子节点（运行时通过 transform.Find 获取，Designer 未声明这两个字段）
        private Image _progressImage;
        private Image _costIconImage;
        private TMP_Text _progressText;
        // Detail 节点上的布局组件，文本变化后需强制重建避免排版错位
        private HorizontalLayoutGroup _detailLayout;
        private bool _isActivating;

        private void Awake()
        {
            _skinConfig = DataConfigManager.Tables.TbSkinConfig;
            _btn = HeadMask;

            // ActiveFrame 下的 Progress/Icon 子节点（Designer 仅声明了 ActiveFrame 本体）
            if (ActiveFrame != null)
            {
                var progressTran = ActiveFrame.transform.Find("TweenTarget/Progress");
                if (progressTran != null) _progressImage = progressTran.GetComponent<Image>();
                var iconTran = ActiveFrame.transform.Find("Detail/Icon");
                if (iconTran != null) _costIconImage = iconTran.GetComponent<Image>();
                var progressTextTran = ActiveFrame.transform.Find("Detail/ProgressText");
                if (progressTextTran != null) _progressText = progressTextTran.GetComponent<TMP_Text>();
                var detailTran = ActiveFrame.transform.Find("Detail");
                if (detailTran != null) _detailLayout = detailTran.GetComponent<HorizontalLayoutGroup>();
                ActiveFrame.onClick.AddListener(OnActiveFrameClicked);
            }
        }

        protected override void OnInit(IUIData uiData = null)
        {
            mData = uiData as SoldierSlotData ?? new SoldierSlotData();
        }

        protected override void OnClose()
        {
            if (ActiveFrame != null)
            {
                ActiveFrame.onClick.RemoveListener(OnActiveFrameClicked);
            }
            if (_btn != null)
            {
                _btn.onClick.RemoveAllListeners();
            }
        }

        public void SetData(int soldierId)
        {
            _soldierId = soldierId;
            var playerCard = Global.GetCardById(soldierId);
            var cardBaseCfg = DataConfigManager.Tables.TbCardBaseConfig.GetOrDefault(soldierId);
            var soldierCfg = DataConfigManager.Tables.TbSoldierBattleConfig.GetOrDefault(soldierId);
            var soldierName = soldierCfg?.Type;
            if (cardBaseCfg == null) return;

            var bigIcon = cardBaseCfg.Icon;
            if (playerCard != null && playerCard.ObjData != null && playerCard.ObjData.Skin != 0)
            {
                var skinCfg = _skinConfig.GetOrDefault(playerCard.ObjData.Skin);
                if (skinCfg != null)
                {
                    bigIcon = skinCfg.Icon;
                }
            }

            SpriteHelper.TryLoadPaintAsync(bigIcon, SoldierHeadIcon, EPaintType.Large, false);

            if (!soldierCfg.PaintingBg.Equals(""))
            {
                SpriteHelper.TryLoadPaintAsync(soldierCfg.PaintingBg, SoldierHeadBg, EPaintType.Medium, false);
            }
            
            TypeIcon.sprite = Global.CareerTypeIconAtlas.GetSpriteExt(soldierCfg.CarType.ToString());
            LogTool.L(soldierName + "Painting");
            if (playerCard == null || !playerCard.IsHave)
            {
                Mask.gameObject.SetActive(true);
                Text.text = "未解锁";
                SetButtonEnabled(false);
                RenderActiveFrame(soldierCfg);
            }
            else
            {
                Mask.gameObject.SetActive(false);
                Text.text = cardBaseCfg.Name;
                SetButtonEnabled(true);
                // 已解锁时隐藏 ActiveFrame（与 Mask 一同只在未解锁时显示）
                if (ActiveFrame != null) ActiveFrame.gameObject.SetActive(false);
            }
        }

        private void SetButtonEnabled(bool enabled)
        {
            if (_btn != null)
            {
                _btn.interactable = enabled;
                _btn.onClick.RemoveAllListeners();
                if (enabled)
                {
                    _btn.onClick.AddListener(OnSlotClicked);
                }
            }
        }

        private void OnSlotClicked()
        {
            if (_soldierId <= 0) return;
            UnlockGuideManager.Send(ModuleFunctionType.SoldierAtlas, 2);
            UnlockGuideManager.Send(ModuleFunctionType.SoldierSkillAtlas, 2);
            var vm = new Game.Soldier.SoldierPanelViewModel();
            vm.OnCreate();
            vm.Initialize(_soldierId);
            UIFramework.UIManager.OpenPanelAsync<SoldierPanelNew, Game.Soldier.SoldierPanelViewModel>(vm, UIFramework.UILevel.PopUI).Forget();

            LogTool.Log($"[SoldierSlot] 打开士兵详情: soldierId={_soldierId}");
        }

        /// <summary>
        /// 渲染未解锁时的 ActiveFrame：显示碎片进度与所需道具图标。
        /// 多个 ActivationCost 时按"全部满足才可解锁"判断可点；进度按第一个消耗物展示。
        /// </summary>
        private void RenderActiveFrame(SoldierBattleConfig soldierCfg)
        {
            if (ActiveFrame == null) return;
            ActiveFrame.gameObject.SetActive(true);

            var costs = soldierCfg != null ? soldierCfg.ActivationCost : null;
            if (costs == null || costs.Count == 0)
            {
                // 无激活消耗配置：不可点，进度 0
                if (_progressImage != null) _progressImage.fillAmount = 0;
                ActiveFrame.interactable = false;
                return;
            }

            // 进度按第一个消耗物展示（UI 上通常只显示一种碎片）
            var firstCost = costs[0];
            long owned = Global.GetItemNumById(firstCost.Id);
            int required = firstCost.Num;
            float progress = required > 0 ? Mathf.Clamp01((float)owned / required) : 0f;
            if (_progressImage != null) _progressImage.fillAmount = progress;

            string ownStr;
            if (owned < required)
            {
                ownStr = $"<color #FF5959>{owned}</color>";
            }
            else
            {
                ownStr = $"<color #35E965>{owned}</color>";
            }
            
            _progressText.text = $"{ownStr}/{required}";
            // 文本长度变化后强制重建 HorizontalLayoutGroup，避免排版错位
            if (_detailLayout != null) LayoutRebuilder.ForceRebuildLayoutImmediate(_detailLayout.transform as RectTransform);
            
            // 道具图标
            var itemCfg = firstCost.Id_Ref ?? DataConfigManager.Tables.TbItemConfig.GetOrDefault(firstCost.Id);
            if (_costIconImage != null && itemCfg != null)
            {
                _costIconImage.sprite = Global.ItemIconSpriteAtlas.GetSpriteExt(itemCfg.Icon);
            }

            // 可点条件：所有消耗物都满足
            ActiveFrame.interactable = SoldierRedDotHelper.CanActivateSoldier(_soldierId);
        }

        private void OnActiveFrameClicked()
        {
            ActivateSoldierAsync().Forget();
        }

        private async UniTask ActivateSoldierAsync()
        {
            if (_isActivating || _soldierId <= 0) return;

            var soldierCfg = DataConfigManager.Tables.TbSoldierBattleConfig.GetOrDefault(_soldierId);
            if (!SoldierRedDotHelper.CanActivateSoldier(_soldierId))
            {
                Toast.Show("碎片不足");
                return;
            }

            var soldierService = GameServiceManager.Get<SoldierService>();
            if (soldierService == null)
            {
                Toast.Show("服务未就绪");
                return;
            }

            _isActivating = true;
            if (ActiveFrame != null)
            {
                ActiveFrame.interactable = false;
            }

            try
            {
                var success = await soldierService.ActivateSoldierCardAsync(_soldierId);
                if (!success)
                {
                    RenderActiveFrame(soldierCfg);
                    return;
                }

                Toast.Show("激活成功", 1f);
                SetData(_soldierId);
            }
            finally
            {
                _isActivating = false;
            }
        }

        protected override void OnOpen(IUIData uiData = null)
        {
        }

        protected override void OnShow()
        {
        }

        protected override void OnHide()
        {
        }
    }
}