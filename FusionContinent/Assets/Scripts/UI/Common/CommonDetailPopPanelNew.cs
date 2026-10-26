using Game.Common;
using UIFramework;
using UnityEngine;

namespace FusionContinent
{
    [PrefabPath("CommonDetailPopPanel")]
    public class CommonDetailPopPanelNew : UIPanel<CommonDetailPopPanelViewModel>
    {
        private const float PosNodeExtraHeight = 201f;
        private const float PosNodeMinHeight = 351f;

        private CommonDetailPopPanel mOldPanel;

        void Awake()
        {
            mOldPanel = GetComponent<CommonDetailPopPanel>();
            if (mOldPanel == null)
            {
                LogTool.LogError("[CommonDetailPopPanelNew] 找不到 CommonDetailPopPanel 组件");
            }
        }

        protected override void OnBindViewModel(CommonDetailPopPanelViewModel vm)
        {
            vm.Title.OnValueChanged += OnTitleChanged;
            mOldPanel.Title.text = vm.Title.Value;

            vm.Detail.OnValueChanged += OnDetailChanged;
            mOldPanel.Detail.text = vm.Detail.Value;
            RefreshPosNodeHeight();

            mOldPanel.Mask.onClick.AddListener(OnMaskClicked);
            vm.OnClosePanel += OnClosePanelRequested;
        }

        protected override void OnUnbindViewModel(CommonDetailPopPanelViewModel vm)
        {
            vm.Title.OnValueChanged -= OnTitleChanged;
            vm.Detail.OnValueChanged -= OnDetailChanged;
            mOldPanel.Mask.onClick.RemoveListener(OnMaskClicked);
            vm.OnClosePanel -= OnClosePanelRequested;
        }

        protected override void OnOpen(IUIData uiData = null)
        {
            // 定位由外部在 onComplete 回调里调用 PlaceBelow 完成
        }

        private void OnTitleChanged(string oldVal, string newVal)
        {
            mOldPanel.Title.text = newVal;
        }

        private void OnDetailChanged(string oldVal, string newVal)
        {
            mOldPanel.Detail.text = newVal;
            RefreshPosNodeHeight();
        }

        private void OnMaskClicked()
        {
            CloseSelf();
        }

        private void OnClosePanelRequested()
        {
            CloseSelf();
        }

        /// <summary>
        /// PosNode 高度 = Detail 文本首选高度 + 额外高度，且不小于 351。
        /// </summary>
        void RefreshPosNodeHeight()
        {
            var detail = mOldPanel.Detail;
            var detailRect = detail.rectTransform;
            float textWidth = detailRect.rect.width > 0f ? detailRect.rect.width : detailRect.sizeDelta.x;
            detail.ForceMeshUpdate();
            var preferred = detail.GetPreferredValues(textWidth, 0f);
            float detailHeight = Mathf.Max(preferred.y, detail.fontSize);
            detailRect.sizeDelta = new Vector2(detailRect.sizeDelta.x, detailHeight);

            var posNode = mOldPanel.PosNode;
            posNode.sizeDelta = new Vector2(posNode.sizeDelta.x, Mathf.Max(detailHeight + PosNodeExtraHeight, PosNodeMinHeight));
        }

        /// <summary>
        /// 将弹窗本体定位到指定锚点正下方，X 方向 clamp 到屏幕内。
        /// </summary>
        public void PlaceBelow(Transform anchor)
        {
            RefreshPosNodeHeight();
            var posNode = mOldPanel.PosNode;
            posNode.position = anchor.position;
            posNode.anchoredPosition -= new Vector2(0, 200);
            ClampPosNodeXToScreen(posNode);
        }

        void ClampPosNodeXToScreen(RectTransform posNode)
        {
            var canvas = posNode.GetComponentInParent<Canvas>();
            var cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var pixelRect = canvas.pixelRect;

            Canvas.ForceUpdateCanvases();

            var corners = new Vector3[4];
            posNode.GetWorldCorners(corners);

            float minX = float.MaxValue;
            float maxX = float.MinValue;
            for (int i = 0; i < 4; i++)
            {
                var x = RectTransformUtility.WorldToScreenPoint(cam, corners[i]).x;
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
            }

            float shiftX = 0f;
            if (minX < pixelRect.xMin)
                shiftX = pixelRect.xMin - minX;
            else if (maxX > pixelRect.xMax)
                shiftX = pixelRect.xMax - maxX;

            if (shiftX == 0f) return;

            var parent = posNode.parent as RectTransform;
            var pivotScreen = RectTransformUtility.WorldToScreenPoint(cam, posNode.position);
            var shiftedScreen = pivotScreen + new Vector2(shiftX, 0f);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, pivotScreen, cam, out var localBefore);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, shiftedScreen, cam, out var localAfter);
            posNode.anchoredPosition += new Vector2(localAfter.x - localBefore.x, 0f);
        }
    }
}
