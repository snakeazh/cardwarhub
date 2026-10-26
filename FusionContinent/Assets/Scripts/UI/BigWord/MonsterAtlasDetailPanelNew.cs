using System.Collections.Generic;
using Assets.Scripts.Localization;
using Config;
using FusionContinent.Animation;
using FusionContinent.Model;
using Game.Challenge;
using QFramework;
using UIFramework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using IUIData = UIFramework.IUIData;

namespace FusionContinent
{
    [PrefabPath("MonsterAtlasDetailPanel")]
    public class MonsterAtlasDetailPanelNew : UIPanel<MonsterAtlasDetailPanelViewModel>
    {
        private MonsterAtlasDetailPanel mOldPanel;

        // 具名回调，便于成对解绑
        private UnityAction mMaskHandler;
        private UnityAction mCloseBtnHandler;
        private UnityAction mReceiveBtnHandler;
        private System.Action mDataReadyHandler;
        private System.Action<RewardStatus, RewardStatus> mAtlasStatusHandler;
        private System.Action mReceiveStartedHandler;
        private System.Action mReceiveFinishedHandler;
        private System.Action<string, float> mShowTipHandler;

        private bool mIsClosing;

        void Awake()
        {
            mOldPanel = GetComponent<MonsterAtlasDetailPanel>();
            if (mOldPanel == null)
            {
                LogTool.LogError("[MonsterAtlasDetailPanelNew] 找不到 MonsterAtlasDetailPanel 组件");
            }
        }

        protected override void OnBindViewModel(MonsterAtlasDetailPanelViewModel vm)
        {
            if (mOldPanel == null) return;

            // 关闭入口
            mMaskHandler = OnCloseButtonClicked;
            mOldPanel.Mask.onClick.AddListener(mMaskHandler);
            mCloseBtnHandler = OnCloseButtonClicked;
            mOldPanel.CloseBtn.onClick.AddListener(mCloseBtnHandler);

            // 领取奖励
            mReceiveBtnHandler = () => vm.OnReceiveCommand.Execute();
            mOldPanel.ReceiveBtn.onClick.AddListener(mReceiveBtnHandler);

            // VM 事件
            mDataReadyHandler = OnDataReady;
            vm.OnDataReady += mDataReadyHandler;
            mAtlasStatusHandler = OnAtlasStatusChanged;
            vm.AtlasStatus.OnValueChanged += mAtlasStatusHandler;
            mReceiveStartedHandler = () => { if (mOldPanel.ReceiveBtn != null) mOldPanel.ReceiveBtn.interactable = false; };
            vm.OnReceiveStarted += mReceiveStartedHandler;
            mReceiveFinishedHandler = () => OnReceiveFinished(vm);
            vm.OnReceiveFinished += mReceiveFinishedHandler;
            mShowTipHandler = OnShowTip;
            vm.OnShowTip += mShowTipHandler;

            // 调用方在打开前已 vm.Setup(monsterId)，绑定完成后直接按当前 VM 数据渲染一次
            OnDataReady();
        }

        protected override void OnUnbindViewModel(MonsterAtlasDetailPanelViewModel vm)
        {
            if (mOldPanel == null) return;

            if (mMaskHandler != null)
            {
                mOldPanel.Mask.onClick.RemoveListener(mMaskHandler);
                mMaskHandler = null;
            }
            if (mCloseBtnHandler != null)
            {
                mOldPanel.CloseBtn.onClick.RemoveListener(mCloseBtnHandler);
                mCloseBtnHandler = null;
            }
            if (mReceiveBtnHandler != null)
            {
                mOldPanel.ReceiveBtn.onClick.RemoveListener(mReceiveBtnHandler);
                mReceiveBtnHandler = null;
            }

            if (mDataReadyHandler != null)
            {
                vm.OnDataReady -= mDataReadyHandler;
                mDataReadyHandler = null;
            }
            if (mAtlasStatusHandler != null)
            {
                vm.AtlasStatus.OnValueChanged -= mAtlasStatusHandler;
                mAtlasStatusHandler = null;
            }
            if (mReceiveStartedHandler != null)
            {
                vm.OnReceiveStarted -= mReceiveStartedHandler;
                mReceiveStartedHandler = null;
            }
            if (mReceiveFinishedHandler != null)
            {
                vm.OnReceiveFinished -= mReceiveFinishedHandler;
                mReceiveFinishedHandler = null;
            }
            if (mShowTipHandler != null)
            {
                vm.OnShowTip -= mShowTipHandler;
                mShowTipHandler = null;
            }
        }

        protected override void OnOpen(IUIData uiData = null)
        {
            if (mOldPanel == null || VM == null) return;
            mIsClosing = false;

            // OnOpen 时 uiData 携带 monsterId（由调用方构造 VM 时已 Setup，这里无需再处理）
            // 应用当前 VM 状态（按钮/IsReceived 显隐）
            ApplyReceiveVisual(VM.AtlasStatus.Value);
        }

        // ---------- 数据就绪渲染 ----------

        private void OnDataReady()
        {
            var vm = VM;
            if (vm == null || mOldPanel == null) return;

            // 清理并重建 Spine（避免重复刷新时叠加）
            ClearSpine();
            RenderSpine(vm.SpineName.Value, vm.SpineScale.Value);

            // 线稿
            RenderLineArt(vm.LineArtSpriteName.Value);

            // 文案
            mOldPanel.MonsterNameText.SetData(LocalizationModule.UITexts, vm.MonsterName.Value);
            mOldPanel.DescText.SetData(LocalizationModule.UITexts, vm.MonsterDesc.Value);
            mOldPanel.AbilityDescText.SetData(LocalizationModule.UITexts, vm.AbilityDesc.Value);

            // 奖励 Content（沿用原有 RewardEntity 池逻辑）
            RenderRewards(vm.AtlasRewards.Value);

            // 按钮/IsReceived 状态
            ApplyReceiveVisual(vm.AtlasStatus.Value);
        }

        // ---------- 交互 ----------

        private void OnCloseButtonClicked()
        {
            if (mIsClosing) return;
            mIsClosing = true;
            PlayCloseAnimation(() => CloseSelf());
        }

        private void OnAtlasStatusChanged(RewardStatus oldVal, RewardStatus newVal)
        {
            ApplyReceiveVisual(newVal);
        }

        /// <summary>
        /// 按 RewardStatus 切换领取按钮与"已领取"标识的显隐。
        /// - Received：隐藏 ReceiveBtn，显示 IsReceived
        /// - CanReceive（且有奖励）：显示 ReceiveBtn 且可点，文案"领取"
        /// - 其它：显示 ReceiveBtn 但不可点，文案"已领取"
        /// </summary>
        private void ApplyReceiveVisual(RewardStatus status)
        {
            if (mOldPanel == null) return;

            bool received = status == RewardStatus.Received;
            if (mOldPanel.ReceiveBtn != null)
            {
                mOldPanel.ReceiveBtn.gameObject.SetActive(!received);
                bool hasReward = VM != null && VM.AtlasRewards.Value != null && VM.AtlasRewards.Value.Count > 0;
                mOldPanel.ReceiveBtn.interactable = (status == RewardStatus.CanReceive) && hasReward;
                var txt = mOldPanel.ReceiveBtn.transform.Find("Text");
                if (txt != null)
                {
                    var loc = txt.GetComponent<LocalizationText>();
                    if (loc != null)
                    {
                        loc.SetData(LocalizationModule.UITexts,
                            status == RewardStatus.CanReceive ? "UI_BTN_REWARD_CLAIM" : "UI_BTN_REWARD_CLAIMED");
                    }
                }
            }
            if (mOldPanel.IsReceived != null)
            {
                mOldPanel.IsReceived.gameObject.SetActive(received);
            }
        }

        private void OnReceiveFinished(MonsterAtlasDetailPanelViewModel vm)
        {
            // 领取结束后，按当前状态恢复按钮 interactable（ApplyReceiveVisual 会处理显隐）
            ApplyReceiveVisual(vm.AtlasStatus.Value);
        }


        private void OnShowTip(string message, float duration)
        {
            Toast.Show(message, duration);
        }

        // ---------- 渲染辅助 ----------

        private void RenderSpine(string spineName, float spineScale)
        {
            if (string.IsNullOrEmpty(spineName)) return;

            var soliderSpine = Global.ResLoader.LoadSync<GameObject>(spineName).Instantiate().transform;
            soliderSpine.SetParent(mOldPanel.MonsterSpine);
            soliderSpine.localScale = Vector3.one;
            soliderSpine.localPosition = Vector3.zero;
            soliderSpine.GetChild(0).localPosition = Vector3.zero;
            soliderSpine.GetChild(0).Layer("UI");
            soliderSpine.GetChild(0).localScale = Vector3.one * 80;
            soliderSpine.GetChild(0).GetComponent<MeshRenderer>().sortingLayerName = "UI";
            mOldPanel.MonsterSpine.localScale = spineScale > 0 ? Vector3.one * spineScale : Vector3.one;
        }

        private void ClearSpine()
        {
            var spineParent = mOldPanel.MonsterSpine;
            if (spineParent == null) return;
            for (int i = spineParent.childCount - 1; i >= 0; i--)
            {
                Destroy(spineParent.GetChild(i).gameObject);
            }
        }

        private void RenderLineArt(string spriteName)
        {
            if (string.IsNullOrEmpty(spriteName) || mOldPanel.MonsterLineArt == null) return;

            mOldPanel.MonsterLineArt.sprite = Global.MonsterLineArtSpriteAtlas.GetSpriteExt(spriteName);
            mOldPanel.MonsterLineArt.SetNativeSize();
            mOldPanel.MonsterLineArt.transform.localScale = Vector3.one * 0.6f;
            var lineArtRect = mOldPanel.MonsterLineArt.rectTransform;
            lineArtRect.anchorMin = new Vector2(1, 0);
            lineArtRect.anchorMax = new Vector2(1, 0);
            lineArtRect.pivot = new Vector2(1, 0);
            lineArtRect.anchoredPosition = Vector2.zero;
        }

        private void RenderRewards(List<RewardAndNum> rewards)
        {
            var content = mOldPanel.Content;
            if (content == null || rewards == null) return;

            for (int i = 0; i < content.childCount; i++)
            {
                content.GetChild(i).gameObject.SetActive(false);
            }

            for (int i = 0; i < rewards.Count; i++)
            {
                var rewObj = i >= content.childCount
                    ? Instantiate(content.GetChild(0).gameObject, content)
                    : content.GetChild(i).gameObject;

                rewObj.SetActive(true);
                rewObj.GetComponent<RewardEntity>().SetData(rewards[i]);
            }
        }

        // ---------- 刷新 ----------

        private void PlayCloseAnimation(UnityAction onComplete)
        {
            if (mOldPanel.CloseAnimation == null)
            {
                onComplete();
                return;
            }
            UIAnimationUtils.PlayAnimations(mOldPanel.CloseAnimation.gameObject, onComplete);
        }
    }
}
