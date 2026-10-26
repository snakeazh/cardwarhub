using DG.Tweening;
using Game.UI;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UIFramework;
using UIFramework.Services;

namespace FusionContinent
{
    /// <summary>
    /// Toast提示面板 - 使用ShowTipManager的动画效果
    /// 同时只显示一个Toast，新的会替换旧的
    /// </summary>
    [PrefabPath("ToastPanel")]
    public class ToastPanel : UIPanel<ToastViewModel>
    {
        #region UI组件
        [SerializeField]
        public GameObject ToastPrefab;
        [SerializeField]
        public Transform ToastContainer;
        #endregion

        #region 私有字段
        private GameObject mCurrentToast;
        private Sequence mCurrentSequence;
        private ToastService mToastService;
        private const float ToastMoveInY = -198f;
        private const float ToastMoveOutY = 112f;
        private const float ToastAnimationDuration = 0.5f;
        #endregion

        void Awake()
        {
            // 确保容器存在
            if (ToastContainer == null)
            {
                ToastContainer = transform;
            }
        }

        protected override void OnBindViewModel(ToastViewModel vm)
        {
            // 获取ToastService
            mToastService = GameServiceManager.Get<ToastService>();
            if (mToastService == null)
            {
                LogTool.LogError("[ToastPanel] 找不到ToastService！");
                return;
            }

            // 订阅Service事件
            mToastService.OnShowToast += OnShowToast;
            mToastService.OnToastClosed += OnToastClosed;
        }

        protected override void OnUnbindViewModel(ToastViewModel vm)
        {
            // 取消订阅
            if (mToastService != null)
            {
                mToastService.OnShowToast -= OnShowToast;
                mToastService.OnToastClosed -= OnToastClosed;
            }

            // 清理动画
            if (mCurrentSequence != null)
            {
                mCurrentSequence.Kill();
                mCurrentSequence = null;
            }
        }

        #region Toast显示逻辑

        private void OnShowToast(string message, float duration, ToastType type)
        {
            // 清理旧的Toast（如果有的话）
            ClearCurrentToast();

            // 创建新的Toast
            CreateToast(message, duration, type);
        }

        private void OnToastClosed()
        {
            // 清理当前Toast
            ClearCurrentToast();
        }

        private void CreateToast(string message, float duration, ToastType type)
        {
            if (ToastPrefab == null)
            {
                LogTool.LogError("[ToastPanel] ToastPrefab未设置！");
                return;
            }

            // 实例化Toast
            mCurrentToast = Instantiate(ToastPrefab, ToastContainer);
            mCurrentToast.name = "ToastItem";

            // 设置位置（从底部进入，参考ShowTipManager: anchoredPosition = new Vector2(0, -198f)）
            var rectTransform = mCurrentToast.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = new Vector2(0, ToastMoveInY);
            }

            // 设置文本
            var textComponent = mCurrentToast.transform.Find("Canvas/TipTxt")?.GetComponent<TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = message;
            }

            // 获取Image组件（初始透明，参考ShowTipManager: color = new Color(0, 0, 0, 0)）
            var image = mCurrentToast.transform.Find("Canvas/Image")?.GetComponent<Image>();
            if (image != null)
            {
                image.color = new Color(0, 0, 0, 0);
            }
            
            // 设置类型图标（如果有的话）
            SetToastTypeIcon(type);

            // 播放动画（使用ShowTipManager的动画逻辑）
            PlayToastAnimation(duration);
        }
        
        private void SetToastTypeIcon(ToastType type)
        {
            // 根据类型设置不同的图标或颜色
            var iconImage = mCurrentToast?.transform.Find("Canvas/Icon")?.GetComponent<Image>();
            if (iconImage == null) return;
            
            string iconName = type switch
            {
                ToastType.Success => "Icon_Success",
                ToastType.Warning => "Icon_Warning",
                ToastType.Error => "Icon_Error",
                _ => "Icon_Info"
            };
            
            // 尝试加载图标
            var sprite = Global.CommonAtlas?.GetSprite(iconName);
            if (sprite != null)
            {
                iconImage.sprite = sprite;
                iconImage.gameObject.SetActive(true);
            }
            else
            {
                iconImage.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 播放Toast动画
        /// </summary>
        private void PlayToastAnimation(float duration)
        {
            if (mCurrentToast == null) return;

            var rectTransform = mCurrentToast.GetComponent<RectTransform>();
            var canvasTransform = mCurrentToast.transform.Find("Canvas");
            if (canvasTransform == null)
            {
                LogTool.LogError("[ToastPanel] ToastItem缺少Canvas节点");
                ClearCurrentToast();
                return;
            }

            Image image = null;
            var imageTransform = canvasTransform.Find("Image");
            if (imageTransform != null)
            {
                image = imageTransform.GetComponent<Image>();
            }

            TextMeshProUGUI text = null;
            var textTransform = canvasTransform.Find("TipTxt");
            if (textTransform != null)
            {
                text = textTransform.GetComponent<TextMeshProUGUI>();
            }

            if (mCurrentSequence != null)
            {
                mCurrentSequence.Kill();
            }

            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = new Vector2(0, ToastMoveInY);
            }

            if (image != null)
            {
                image.color = new Color(0, 0, 0, 0);
            }

            if (text != null)
            {
                text.color = new Color(text.color.r, text.color.g, text.color.b, 1);
            }

            mCurrentSequence = DOTween.Sequence().SetUpdate(UpdateType.Normal, true);

            bool hasEnterTween = false;
            AppendOrJoinTween(image != null ? image.DOColor(new Color(1, 1, 1, 1), ToastAnimationDuration) : null, ref hasEnterTween);
            AppendOrJoinTween(rectTransform != null ? rectTransform.DOAnchorPos(Vector2.zero, ToastAnimationDuration) : null, ref hasEnterTween);

            mCurrentSequence.Append(DOVirtual.Float(0f, 1f, duration, _ => { }).SetUpdate(UpdateType.Normal, true));

            bool hasExitTween = false;
            AppendOrJoinTween(image != null ? image.DOColor(new Color(0, 0, 0, 0), ToastAnimationDuration) : null, ref hasExitTween);
            AppendOrJoinTween(text != null ? text.DOColor(new Color(text.color.r, text.color.g, text.color.b, 0), ToastAnimationDuration) : null, ref hasExitTween);
            AppendOrJoinTween(rectTransform != null ? rectTransform.DOAnchorPos(new Vector2(0, ToastMoveOutY), ToastAnimationDuration) : null, ref hasExitTween);

            mCurrentSequence.OnComplete(ClearCurrentToast);
        }

        private void AppendOrJoinTween(Tween tween, ref bool hasTween)
        {
            if (tween == null)
            {
                return;
            }

            tween.SetUpdate(UpdateType.Normal, true);
            if (hasTween)
            {
                mCurrentSequence.Join(tween);
            }
            else
            {
                mCurrentSequence.Append(tween);
                hasTween = true;
            }
        }

        private void ClearCurrentToast()
        {
            // 停止动画
            if (mCurrentSequence != null)
            {
                mCurrentSequence.Kill();
                mCurrentSequence = null;
            }

            // 销毁Toast对象
            if (mCurrentToast != null)
            {
                Destroy(mCurrentToast);
                mCurrentToast = null;
            }
        }

        #endregion

        #region 生命周期

        protected override void OnOpen(IUIData uiData = null)
        {
            // ToastPanel是常驻面板，不需要特殊处理
        }

        protected override void OnShow()
        {
            // 显示时清理可能残留的内容
            ClearCurrentToast();
        }

        protected override void OnHide()
        {
            // 隐藏时清理
            ClearCurrentToast();
        }

        #endregion
    }
}
