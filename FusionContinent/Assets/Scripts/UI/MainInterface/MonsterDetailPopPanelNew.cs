using Game.Challenge;
using UIFramework;
using UnityEngine;
using UnityEngine.Events;

namespace FusionContinent
{
    [PrefabPath("MonsterDetailPopPanel")]
    public class MonsterDetailPopPanelNew : UIPanel<MonsterDetailPopPanelViewModel>
    {
        private MonsterDetailPopPanel mOldPanel;

        // 弹窗本体节点（prefab 的 TweenTarget 子节点：Bg/MonsterName/MonsterDetail 都挂在它下面）。
        // 根节点是全屏遮罩层，真正要定位的是 TweenTarget。
        private RectTransform _body;

        private System.Action<string, string> _nameHandler;
        private System.Action<string, string> _detailHandler;

        void Awake()
        {
            mOldPanel = GetComponent<MonsterDetailPopPanel>();
            if (mOldPanel == null)
            {
                LogTool.LogError("[MonsterDetailPopPanelNew] 找不到 MonsterDetailPopPanel 组件");
                return;
            }

            _body = transform.Find("TweenTarget/PosNode") as RectTransform;
            if (_body == null)
            {
                LogTool.LogError("[MonsterDetailPopPanelNew] 找不到 TweenTarget 节点，弹窗本体定位将失效");
            }
        }

        protected override void OnBindViewModel(MonsterDetailPopPanelViewModel vm)
        {
            if (mOldPanel == null) return;

            _nameHandler = OnMonsterNameChanged;
            vm.MonsterName.OnValueChanged += _nameHandler;
            mOldPanel.MonsterName.text = vm.MonsterName.Value;

            _detailHandler = OnMonsterDetailChanged;
            vm.MonsterDetail.OnValueChanged += _detailHandler;
            mOldPanel.MonsterDetail.text = vm.MonsterDetail.Value;

            mOldPanel.Mask.onClick.AddListener(OnMaskClicked);

            vm.OnClosePanel += OnClosePanelRequested;
        }

        protected override void OnUnbindViewModel(MonsterDetailPopPanelViewModel vm)
        {
            if (_nameHandler != null)
            {
                vm.MonsterName.OnValueChanged -= _nameHandler;
                _nameHandler = null;
            }
            if (_detailHandler != null)
            {
                vm.MonsterDetail.OnValueChanged -= _detailHandler;
                _detailHandler = null;
            }

            mOldPanel.Mask.onClick.RemoveListener(OnMaskClicked);

            vm.OnClosePanel -= OnClosePanelRequested;
        }

        protected override void OnOpen(IUIData uiData = null)
        {
            // 定位由外部在 onComplete 回调里调用 PlaceBelow 完成
        }

        private void OnMonsterNameChanged(string oldVal, string newVal)
        {
            mOldPanel.MonsterName.text = newVal;
        }

        private void OnMonsterDetailChanged(string oldVal, string newVal)
        {
            mOldPanel.MonsterDetail.text = newVal;
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
        /// 将弹窗本体定位到指定锚点（通常是怪物卡片）的正下方。
        /// 复用 ChestShowPanelNew 的坐标换算模式：世界坐标 → 屏幕 → 弹窗父节点本地坐标，
        /// 再沿 Y 轴向下偏移（卡片半高 + 弹窗半高 + 间距），使弹窗顶边紧贴卡片底边。
        /// 边缘保护：X 方向 clamp 到屏幕内（左右两侧卡片不溢出）。
        /// 注意：定位的是 TweenTarget（弹窗本体），不是全屏根节点。
        /// </summary>
        public void PlaceBelow(Transform anchor)
        {
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
