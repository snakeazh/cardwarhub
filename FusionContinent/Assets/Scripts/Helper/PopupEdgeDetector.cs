
using System;
using UnityEngine;
using System.Collections;


namespace FusionContinent.Helper
{
    [RequireComponent(typeof(RectTransform))]
    public class PopupEdgeDetector : MonoBehaviour
    {
        [Tooltip("如果不指定，会自动向上查找 Canvas")]
        [SerializeField] private Canvas canvas;

        [Tooltip("弹窗超出屏幕时触发的回调")]
        public Action<ExceedDir> onExceedScreen;

        private RectTransform rect;

        void Awake()
        {
            rect = GetComponent<RectTransform>();
            if (canvas == null)
                canvas = GetComponentInParent<Canvas>();
        }

        void OnEnable()
        {
            // 等一帧并强制刷新布局，避免 LayoutGroup/ContentSizeFitter 引起的延迟
            StartCoroutine(CheckNextFrame());
        }

        private IEnumerator CheckNextFrame()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            yield return null;
            CheckBounds();
        }

        /// <summary>
        /// 检测并在超出时触发 onExceedScreen
        /// </summary>
        public void CheckBounds()
        {
            if (canvas == null)
            {
                LogTool.LogWarning("PopupEdgeDetector_Fix: Canvas 未指定且未找到父级 Canvas。");
                return;
            }

            // world corners
            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners); // world space

            // 选择用于转换的 Camera：Overlay -> null, 其它模式用 canvas.worldCamera
            Camera cam = (canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : canvas.worldCamera;

            // canvas 可视像素矩形
            Rect pixelRect = canvas.pixelRect;

            ExceedDir exceed = ExceedDir.None;

            for (int i = 0; i < 4; i++)
            {
                Vector2 screenPt = RectTransformUtility.WorldToScreenPoint(cam, corners[i]);

                if (screenPt.x < pixelRect.xMin - 0.1f)
                    exceed |= ExceedDir.Left;
                if (screenPt.x > pixelRect.xMax + 0.1f)
                    exceed |= ExceedDir.Right;
                if (screenPt.y < pixelRect.yMin - 0.1f)
                    exceed |= ExceedDir.Bottom;
                if (screenPt.y > pixelRect.yMax + 0.1f)
                    exceed |= ExceedDir.Top;
            }

            if (exceed != ExceedDir.None)
            {
                // LogTool.Log($"{name} 超出屏幕边界: {exceed}");
                onExceedScreen?.Invoke(exceed);
            }
            else
            {
                // LogTool.Log($"{name} 在屏幕内");
            }
        }
        
        
        // 定义超出方向枚举
        [System.Flags]
        public enum ExceedDir
        {
            None  = 0,
            Left  = 1 << 0,
            Right = 1 << 1,
            Top   = 1 << 2,
            Bottom= 1 << 3
        }
        
    }
}