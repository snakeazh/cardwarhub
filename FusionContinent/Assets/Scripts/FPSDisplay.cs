using UnityEngine;
using UnityEngine.UI;

public class FPSDisplay : MonoBehaviour
{
    // 固定的一个时间间隔
    private float time_delta = 0.5f;
    // Time.realtimeSinceStartup: 指的是我们当前从启动开始到现在运行的时间，单位(s)
    private float prev_time = 0.0f; // 上一次统计FPS的时间;
    private float fps = 0.0f; // 计算出来的FPS的值;
    private int i_frames = 0; // 累计我们刷新的帧数;

    // GUI显示;
    private GUIStyle style;

    void Awake()
    {
        // -1, 游戏引擎就会不段的刷新我们的画面，有多高，刷多高; 
        Application.targetFrameRate = 280;
    }

    // Use this for initialization
    void Start()
    {
        this.prev_time = Time.realtimeSinceStartup;
        this.style = new GUIStyle();
        this.style.fontSize = 80;
        this.style.normal.textColor = new Color(0, 0, 0);
    }

    void OnGUI()
    {
        GUI.Label(new Rect(0, Screen.height - 60, 200, 200), "FPS:" + this.fps.ToString("f2"), this.style);
    }

    void Update()
    {
        this.i_frames++;

        if (Time.realtimeSinceStartup >= this.prev_time + this.time_delta)
        {
            this.fps = ((float)this.i_frames) / (Time.realtimeSinceStartup - this.prev_time);
            this.prev_time = Time.realtimeSinceStartup;
            this.i_frames = 0; // 重新累积我们的FPS
        }
    }
}

