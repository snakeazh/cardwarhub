using FusionContinent.Manager;
using TMPro;
using UnityEngine;

public class TimeRecordBind : MonoBehaviour
{
    private long mTargetTimestamp;
    private long mServerTimeWhenSet;
    private float mLocalTimeWhenSet;
    private TextMeshProUGUI mTimeText;

    void Awake()
    {
        EnsureTimeText();
    }

    private void EnsureTimeText()
    {
        if (mTimeText == null)
        {
            mTimeText = GetComponent<TextMeshProUGUI>();
        }
    }

    public void SetData(long timestamp)
    {
        EnsureTimeText();
        mTargetTimestamp = timestamp;
        mServerTimeWhenSet = ServerTimeManager.Instance.GetCurrentServerTimestamp();
        mLocalTimeWhenSet = Time.realtimeSinceStartup;
        Refresh();
    }

    void Update()
    {
        if (mTargetTimestamp <= 0)
        {
            return;
        }

        Refresh();
    }

    private void Refresh()
    {
        if (mTargetTimestamp <= 0)
        {
            gameObject.SetActive(false);
            return;
        }

        long now = mServerTimeWhenSet + (long)(Time.realtimeSinceStartup - mLocalTimeWhenSet);
        long remaining = mTargetTimestamp - now;
        if (remaining <= 0)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        int hours = (int)(remaining / 3600);
        int minutes = (int)(remaining % 3600 / 60);
        int seconds = (int)(remaining % 60);
        mTimeText.text = string.Format("{0:D2}:{1:D2}:{2:D2}", hours, minutes, seconds);
    }
}
