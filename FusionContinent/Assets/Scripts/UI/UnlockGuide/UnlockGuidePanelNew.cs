using System.Collections;
using Config;
using DG.Tweening;
using Game.UnlockGuide;
using QFramework;
using UIFramework;
using UnityEngine;
using UnityEngine.UI;
using IUIData = UIFramework.IUIData;

namespace FusionContinent
{
    /// <summary>
    /// 解锁引导面板（MVVM）。
    /// 复用旧 UnlockGuidePanel 的 UI 引用，对外保留 CurrentStep / ToNextStep /
    /// RunNextStep / RefreshCurrentTarget 供 UnlockGuideManager 驱动。
    /// 打开方式：UIFramework.UIManager.GetOrOpenPanelAsync（UnlockGuideManager.ShowGuide）。
    /// </summary>
    [PrefabPath("UnlockGuidePanel")]
    public class UnlockGuidePanelNew : UIPanel<UnlockGuidePanelViewModel>
    {
        private UnlockGuidePanel mOldPanel;

        /// <summary>当前引导步骤 StepId（真值源在 VM）</summary>
        public int CurrentStep
        {
            get { return VM.CurrentStep.Value; }
        }

        private Canvas mParentCanvas;
        private Canvas lastCanvas;
        private GraphicRaycaster lastGraphicRaycaster;
        private float searchDuration;

        protected override void OnInit(IUIData uiData)
        {
            mOldPanel = GetComponent<UnlockGuidePanel>();
            if (mOldPanel == null)
            {
                LogTool.LogError("[UnlockGuidePanelNew] 缺少 UnlockGuidePanel 组件");
            }

            base.OnInit(uiData);
        }

        protected override void OnBindViewModel(UnlockGuidePanelViewModel vm)
        {
            mOldPanel.SkipBtn.onClick.AddListener(OnSkipButtonClicked);
            mOldPanel.NextBtn.onClick.AddListener(OnNextButtonClicked);
        }

        protected override void OnUnbindViewModel(UnlockGuidePanelViewModel vm)
        {
            mOldPanel.SkipBtn.onClick.RemoveListener(OnSkipButtonClicked);
            mOldPanel.NextBtn.onClick.RemoveListener(OnNextButtonClicked);
        }

        private void OnSkipButtonClicked()
        {
            VM.OnSkipCommand.Execute();
        }

        private void OnNextButtonClicked()
        {
            VM.OnNextCommand.Execute();
        }

        protected override void OnOpen(IUIData uiData = null)
        {
            mParentCanvas = GetComponentInParent<Canvas>();
            SetFingerDepth();

            int startStep = 1;
            if (UnlockGuideManager.Instance != null)
            {
                startStep = UnlockGuideManager.Instance.PendingStartStep;
            }
            if (startStep <= 0)
            {
                startStep = 1;
            }
            VM.CurrentStep.Value = startStep;
            StartCoroutine(StartGuide());
        }

        protected override void OnShow()
        {
        }

        protected override void OnHide()
        {
        }

        void SetFingerDepth()
        {
            var psList = mOldPanel.Finger.GetComponentsInChildren<Renderer>();
            foreach (Renderer rn in psList)
            {
                rn.sortingLayerName = mParentCanvas.sortingLayerName;
                rn.sortingOrder = mParentCanvas.sortingOrder + 20;
            }
        }

        IEnumerator StartGuide()
        {
            mOldPanel.Dialoge.DOKill(false);
            mOldPanel.Dialoge.localScale = Vector3.zero;

            mOldPanel.Dialoge.gameObject.SetActive(false);
            mOldPanel.Finger.gameObject.SetActive(false);
            mOldPanel.Maskabled.sizeDelta = Vector2.zero;
            mOldPanel.NextBtn.gameObject.SetActive(false);

            var config = UnlockGuideManager.Instance.GetModuleStep(CurrentStep);
            if (config == null)
            {
                LogTool.LogError($"UnlockGuidePanel: 找不到步骤配置 StepId={CurrentStep}, Module={UnlockGuideManager.Instance.CurrentModuleFunction}");
                UnlockGuideManager.Instance.CloseGuide();
                yield break;
            }
            if (config.ClickMaskNext == GuideNextEvent.ClickTarget)
            {
                var target = UnlockGuideManager.Instance.GetStepTarget(CurrentStep);
                searchDuration = 0;
                while (target == null || !target.gameObject.activeInHierarchy)
                {
                    yield return null;
                    searchDuration += Time.deltaTime;
                    if (searchDuration >= 100f)
                    {
                        UnlockGuideManager.Instance.CloseGuide();
                        yield break;
                    }
                    target = UnlockGuideManager.Instance.GetStepTarget(CurrentStep);
                }

                yield return new WaitForSeconds(0.1f);
                target = UnlockGuideManager.Instance.GetStepTarget(CurrentStep);
                if (target == null || !target.gameObject.activeInHierarchy)
                {
                    yield break;
                }
                SetTarget(target);
                AddCanvas(target);
                mOldPanel.Finger.gameObject.SetActive(true);
            }
            else if (config.ClickMaskNext == GuideNextEvent.ClickMask)
            {
                mOldPanel.NextBtn.gameObject.SetActive(true);

                var target = UnlockGuideManager.Instance.GetStepTarget(CurrentStep);
                searchDuration = 0;
                while (target == null)
                {
                    yield return null;
                    searchDuration += Time.deltaTime;
                    if (searchDuration >= 100f)
                    {
                        UnlockGuideManager.Instance.CloseGuide();
                        yield break;
                    }
                    target = UnlockGuideManager.Instance.GetStepTarget(CurrentStep);
                }

                yield return new WaitForSeconds(0.1f);
                SetTarget(target);
            }

            mOldPanel.Finger.anchoredPosition = mOldPanel.Maskabled.anchoredPosition;

            if (config.ShowDialoge == GuideShowType.Show)
            {
                mOldPanel.Dialoge.gameObject.SetActive(true);
                mOldPanel.Dialoge.anchoredPosition = new Vector2(0, mOldPanel.Maskabled.anchoredPosition.y) + new Vector2(config.DialogOffset.X, config.DialogOffset.Y);
                mOldPanel.ContentText.text = config.DialogText;
                mOldPanel.Dialoge.DOScale(Vector3.one, 0.3f).SetUpdate(true);
            }
        }

        void SetTarget(RectTransform target)
        {
            target.Layer("UI");
            // 1. 无论 target 的锚点(Anchors)在哪里，target.rect 始终代表其本地矩形范围
            // 使用 rect.size 乘以 localScale 得到缩放后的视觉尺寸
            Vector2 actualSize = target.rect.size;
            mOldPanel.Maskabled.sizeDelta = actualSize * target.localScale;

            // 2. 关键：计算 target 的几何中心在世界坐标系中的位置
            // target.rect.center 是相对于 pivot 的偏移，TransformPoint 将其转为世界坐标
            Vector3 targetCenterWorld = target.TransformPoint(target.rect.center);

            // 3. 将 Maskabled 的世界坐标对齐到这个几何中心
            // 注意：这要求 Maskabled 的 Pivot 必须是 (0.5, 0.5)
            mOldPanel.Maskabled.position = targetCenterWorld;
        }

        public void AddCanvas(RectTransform target)
        {
            lastCanvas = target.GetOrAddComponent<Canvas>();
            lastCanvas.overrideSorting = true;
            lastCanvas.sortingOrder = mParentCanvas.sortingOrder + 10;
            lastGraphicRaycaster = target.GetOrAddComponent<GraphicRaycaster>();
        }

        public void ToNextStep()
        {
            if (lastGraphicRaycaster != null)
            {
                mOldPanel.Finger.gameObject.SetActive(false);
                DestroyImmediate(lastGraphicRaycaster);
                DestroyImmediate(lastCanvas);
                lastGraphicRaycaster = null;
                lastCanvas = null;
            }
        }

        public void RefreshCurrentTarget()
        {
            if (UnlockGuideManager.Instance == null)
            {
                return;
            }

            var target = UnlockGuideManager.Instance.GetStepTarget(CurrentStep);
            if (target == null || !target.gameObject.activeInHierarchy)
            {
                return;
            }

            ToNextStep();
            SetTarget(target);
            AddCanvas(target);
            mOldPanel.Finger.gameObject.SetActive(true);
            mOldPanel.Finger.anchoredPosition = mOldPanel.Maskabled.anchoredPosition;
        }

        public void RunNextStep()
        {
            if (!gameObject.activeSelf)
            {
                return;
            }
            VM.CurrentStep.Value++;
            StartCoroutine(StartGuide());
        }
    }
}
