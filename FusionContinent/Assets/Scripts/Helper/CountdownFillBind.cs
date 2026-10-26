using FusionContinent.Manager;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 挂在带 FillAmount 的 Image 上，根据 [startTs, endTs] 服务器时间区间
/// 实时刷新 Image.fillAmount：满进度=1（刚开始），倒计时结束=0。
/// 与 TimeRecordBind 同一套时间基准（SetData 时记录服务器/本地时间偏移）。
/// </summary>
public class CountdownFillBind : MonoBehaviour
{
    private long mStartTimestamp;
    private long mEndTimestamp;
    private long mServerTimeWhenSet;
    private float mLocalTimeWhenSet;
    private Image mFillImage;

    void Awake()
    {
        EnsureFillImage();
    }

    private void EnsureFillImage()
    {
        if (mFillImage == null)
        {
            mFillImage = GetComponent<Image>();
        }
    }

    public void SetData(long startTs, long endTs)
    {
        EnsureFillImage();
        mStartTimestamp = startTs;
        mEndTimestamp = endTs;
        mServerTimeWhenSet = ServerTimeManager.Instance.GetCurrentServerTimestamp();
        mLocalTimeWhenSet = Time.realtimeSinceStartup;
        Refresh();
    }

    void Update()
    {
        if (mEndTimestamp <= 0 || mFillImage == null)
        {
            return;
        }

        Refresh();
    }

    private void Refresh()
    {
        if (mFillImage == null || mEndTimestamp <= 0)
        {
            return;
        }

        long now = mServerTimeWhenSet + (long)(Time.realtimeSinceStartup - mLocalTimeWhenSet);
        long total = mEndTimestamp - mStartTimestamp;
        if (total <= 0)
        {
            mFillImage.fillAmount = 0f;
            return;
        }

        long remaining = mEndTimestamp - now;
        if (remaining <= 0)
        {
            mFillImage.fillAmount = 0f;
            return;
        }

        // 剩余比例 = 剩余时间 / 总时长
        mFillImage.fillAmount = Mathf.Clamp01((float)remaining / total);
    }
}
