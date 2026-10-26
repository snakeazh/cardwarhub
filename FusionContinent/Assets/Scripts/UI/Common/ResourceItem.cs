using FusionContinent.Manager;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FusionContinent.UI
{
    /// <summary>
    /// 资源Item组件
    /// 显示单个资源（图标+数量+Add按钮）
    /// </summary>
    public class ResourceItem : MonoBehaviour
    {
        [Header("UI组件")]
        [SerializeField] private Button mAddBtn;
        [SerializeField] private Image mIconImg;
        [SerializeField] private TextMeshProUGUI mNumText;
        
        [Header("配置")]
        [SerializeField] private int mItemId;
        
        // 当前数量
        private long mCurrentNum = 0;
        private System.Func<long, string> mNumFormatter;
        
        // 点击回调
        private System.Action<int> mOnClickCallback;
        private System.Action<int> mOnAddClickCallback;
        
        /// <summary>
        /// 初始化资源Item
        /// </summary>
        public void Initialize(int itemId, Sprite icon = null, long initialNum = 0, System.Func<long, string> numFormatter = null)
        {
            mItemId = itemId;
            mNumFormatter = numFormatter;
            SetIcon(icon);
            SetNum(initialNum);
        }
        
        /// <summary>
        /// 设置图标
        /// </summary>
        public void SetIcon(Sprite icon)
        {
            if (mIconImg != null && icon != null)
            {
                mIconImg.sprite = icon;
            }
        }
        
        /// <summary>
        /// 设置数量
        /// </summary>
        public void SetNum(long num)
        {
            mCurrentNum = num;
            if (mNumText != null)
            {
                mNumText.text = FormatNumber(num);
                // 同步 RollingNumberManager 基准值，避免后续 Play 动画从错误起点插值导致数字回跳
                var rollingManager = mNumText.GetComponent<RollingNumberManager>();
                if (rollingManager != null)
                {
                    rollingManager.SyncBaseline(num);
                }
            }
        }

        /// <summary>
        /// 播放数字滚动动画
        /// </summary>
        /// <param name="duration">大于 0 时覆盖默认滚动时长（秒）</param>
        public void PlayNumAnimation(long targetNum, float duration = -1f)
        {
            if (mNumFormatter != null)
            {
                SetNum(targetNum);
                return;
            }

            var rollingManager = mNumText?.GetComponent<RollingNumberManager>();
            if (rollingManager != null)
            {
                rollingManager.Play(targetNum, false, duration);
            }
            else
            {
                SetNum(targetNum);
            }
        }
        
        /// <summary>
        /// 设置点击回调
        /// </summary>
        public void SetClickCallback(System.Action<int> callback)
        {
            mOnClickCallback = callback;
        }
        
        /// <summary>
        /// 设置Add按钮点击回调
        /// </summary>
        public void SetAddClickCallback(System.Action<int> callback)
        {
            mOnAddClickCallback = callback;
        }
        
        /// <summary>
        /// 显示/隐藏Add按钮
        /// </summary>
        public void ShowAddButton(bool show)
        {
            if (mAddBtn != null)
            {
                mAddBtn.gameObject.SetActive(show);
            }
        }
        
        /// <summary>
        /// 获取道具ID
        /// </summary>
        public int GetItemId()
        {
            return mItemId;
        }
        
        /// <summary>
        /// 获取当前数量
        /// </summary>
        public long GetNum()
        {
            return mCurrentNum;
        }
        
        void Awake()
        {
            // 绑定按钮事件
            if (mAddBtn != null)
            {
                mAddBtn.onClick.AddListener(() => mOnAddClickCallback?.Invoke(mItemId));
            }
            
            // 整个Item点击
            var btn = GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(() => mOnClickCallback?.Invoke(mItemId));
            }
        }
        
        void OnDestroy()
        {
            if (mAddBtn != null)
                mAddBtn.onClick.RemoveAllListeners();
            
            var btn = GetComponent<Button>();
            if (btn != null)
                btn.onClick.RemoveAllListeners();
        }
        
        /// <summary>
        /// 格式化数字（超过10000显示为1万）
        /// </summary>
        private string FormatNumber(long num)
        {
            if (mNumFormatter != null)
            {
                return mNumFormatter(num);
            }

            return GlobalFunc.FormatNumber(num);
        }
    }
}
